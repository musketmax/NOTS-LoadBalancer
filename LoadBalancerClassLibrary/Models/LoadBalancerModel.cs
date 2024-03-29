﻿using AdditionalAlgorithmsClassLibrary;
using AlgorithmClassLibrary;
using AlgorithmClassLibrary.Algorithms.Factory;
using BaseAlgorithmClassLibrary;
using ServerClassLibrary;
using StandardAlgorithmsClassLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace LoadBalancerClassLibrary.Models
{
    public class LoadBalancerModel
    {
        #region UI-Items
        public ObservableCollection<ListBoxItem> Log { get; set; }
        public ObservableCollection<ListBoxItem> ServerList { get; set; }
        public ObservableCollection<ListBoxItem> MethodItems { get; set; }
        public ObservableCollection<ListBoxItem> HealthItems { get; set; }
        public ObservableCollection<ListBoxItem> PersistItems { get; set; }
        #endregion

        #region Selected-UI-Items
        public ListBoxItem SelectedItem { get; set; }
        public string SelectedMethodString;
        public string SelectedHealthString;
        public string SelectedPersistString;
        #endregion

        #region Statuses
        public bool ACTIVE = false;
        public bool STOPPING = false;
        public bool PERSIST = false;
        #endregion

        #region Configurations
        public int PORT = 8080;
        public string IP = "127.0.0.1";
        public int BUFFER_SIZE = 1024;
        public string IP_ADD = "127.0.0.1";
        public int PORT_ADD = 8085;
        #endregion

        #region IO-models
        private TcpListener _listener;
        private Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        private List<Server> servers;
        #endregion

        #region Algorithms
        private ILBAlgorithmFactory algoFactory;
        private ILBAlgorithm currentAlgorithm;
        private ReverseRoundRobinAlgorithm a;
        private RoundRobinAlgorithm b;
        private RandomAlgorithm c;
        private string PreviousAlgoString;
        #endregion

        #region Stateful
        private List<Session> sessions;
        private bool COOKIE_ABSENT;
        #endregion

        #region CONSTANTS
        private string COOKIE_BASED = "Cookie Based";
        private string SESSION_BASED = "Session Based";
        private string ACTIVE_PERSISTENCE = "Active";
        private string PASSIVE_PERSISTENCE = "Passive";
        #endregion

        public LoadBalancerModel()
        {
            algoFactory = new ILBAlgorithmFactory();
            currentAlgorithm = null;

            InitLoadBalancer();
            InitServers();
            DoHealthCheck();
        }

        private void InitLoadBalancer()
        {
            COOKIE_ABSENT = false;

            sessions = new List<Session>();

            Log = new ObservableCollection<ListBoxItem>();
            ServerList = new ObservableCollection<ListBoxItem>();
            MethodItems = new ObservableCollection<ListBoxItem>();
            HealthItems = new ObservableCollection<ListBoxItem>();
            PersistItems = new ObservableCollection<ListBoxItem>();
            SelectedItem = new ListBoxItem();

            InitAlgos();

            AddToHealth(ACTIVE_PERSISTENCE);
            AddToHealth(PASSIVE_PERSISTENCE);

            AddToPersist(COOKIE_BASED);
            AddToPersist(SESSION_BASED);
        }

        public void InitAlgos()
        {
            MethodItems = new ObservableCollection<ListBoxItem>();
            ILBAlgorithmFactory.GetAllAlgoRithms().ForEach((x) =>
            {
                AddToMethods(x);
            });
        }

        public async void Start()
        {
            IPAddress address;

            if (IPAddress.TryParse(IP, out address) && (PORT > 0))
            {
                ACTIVE = true;
                _listener = new TcpListener(address, PORT);
                _listener.Start();
                AddToLog("Started LoadBalancer.");

                try
                {
                    await Task.Run(() => ListenForClients());
                }
                catch { }
            }
            else
            {
                AddToLog("An invalid IP Address was entered, or the Port was invalid.");
            }
        }

        public void Stop()
        {
            ACTIVE = false;
            STOPPING = true;
            Task.Run(() =>
                {
                    while (true)
                    {
                        if (_listener.Pending()) continue;
                        _listener.Stop();
                        break;
                    }
                    STOPPING = false;
                    AddToLog("Stopped LoadBalancer.");
                });
        }

        public async Task ListenForClients()
        {
            while (ACTIVE && !STOPPING)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    await Task.Run(() => HandleClientRequest(client));
                }
                catch { }
            }
        }

        public async Task HandleClientRequest(TcpClient client)
        {
            if (ACTIVE && !STOPPING)
            {
                AddToLog("Handling client request..");

                using (NetworkStream clientStream = client.GetStream())
                using (MemoryStream memStream = new MemoryStream())
                {
                    try
                    {
                        byte[] buffer = new byte[BUFFER_SIZE];
                        int bytesRead;

                        if (clientStream.DataAvailable)
                        {
                            do
                            {
                                bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
                                await memStream.WriteAsync(buffer, 0, bytesRead);
                            } while (clientStream.DataAvailable);
                        }

                        await LoadBalanceRequest(memStream, client);
                    }
                    catch (IOException e)
                    {
                        AddToLog(e.Message);
                    }
                }
            }
        }

        private async Task LoadBalanceRequest(MemoryStream stream, TcpClient client)
        {
            if (ACTIVE && !STOPPING)
            {
                try
                {
                    Server s = null;

                    if (PERSIST)
                    {
                        try
                        {
                            AddToLog("Using Persistence for next request..");

                            if (SelectedPersistString == COOKIE_BASED)
                            {
                                s = GetServerForCookie(stream.GetBuffer());
                            }
                            else if (SelectedPersistString == SESSION_BASED)
                            {
                                s = GetServerForSession(client);
                            }

                            if (s == null && !COOKIE_ABSENT)
                            {
                                await DoFailedPersistenceTask(client);
                                return;
                            }
                        }
                        catch
                        {
                            await DoFailedPersistenceTask(client);
                            return;
                        }
                    }

                    if (!PERSIST || COOKIE_ABSENT)
                    {
                        COOKIE_ABSENT = false;
                        s = DetermineAlgorithm();
                    }

                    if (s == null)
                    {
                        await DoFailedAlgoTask(client);
                        return;
                    }

                    Server serverToSend = Reconnect(s);

                    using (stream)
                    using (NetworkStream clientStream = client.GetStream())
                    using (NetworkStream serverStream = serverToSend.client.GetStream())
                    {
                        serverToSend.client.SendTimeout = 500;
                        serverToSend.client.ReceiveTimeout = 2000;
                        byte[] buffer = new byte[BUFFER_SIZE];

                        if (serverStream.CanWrite)
                        {
                            await serverStream.WriteAsync(stream.GetBuffer(), 0, stream.GetBuffer().Length);

                            using (MemoryStream memStream = new MemoryStream())
                            {
                                if (serverStream.DataAvailable)
                                {
                                    //while (true)
                                    //{
                                    //    int bytesReceived = await serverStream.ReadAsync(buffer, 0, buffer.Length);
                                    //    if (bytesReceived == 0) break;
                                    //    await memStream.WriteAsync(buffer, 0, bytesReceived);
                                    //    await clientStream.WriteAsync(buffer, 0, bytesReceived);
                                    //}

                                    await serverStream.ReadAsync(buffer, 0, buffer.Length);

                                    if (SelectedHealthString == "Passive")
                                    {
                                        DoHealthCheckPassive(buffer, serverToSend);
                                    }

                                    byte[] res = buffer;
                                    if (PERSIST)
                                    {
                                        if (SelectedPersistString == COOKIE_BASED)
                                        {
                                            AddToLog("Setting Cookies for Persistence..");
                                            res = SetCookies(buffer, serverToSend);
                                        }
                                        else if (SelectedPersistString == SESSION_BASED)
                                        {
                                            AddToLog("Setting Sessions for persistence..");
                                            SetSession(client, serverToSend);
                                        }
                                    }

                                    await clientStream.WriteAsync(res, 0, res.Length);

                                    AddToLog($"Load balanced to server {serverToSend.NAME}.");
                                    AddToLog($"Used {SelectedMethodString}.");
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    AddToLog(e.Message);
                    await SendGenericErrorCode(client);
                }
            }
        }

        private byte[] GetAndSetHeaders(byte[] buffer, Guid key)
        {
            // Split response into headers and body
            string response = Encoding.ASCII.GetString(buffer);
            string[] lines = response.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            // Make stringbuilders for headers and body
            StringBuilder sbHeaders = new StringBuilder();
            StringBuilder sbBody = new StringBuilder();
            bool body = false;

            foreach (var line in lines)
            {
                // If line is empty string, this marks the end of headers
                if (line.Equals(""))
                {
                    body = true;
                }

                if (!body)
                {
                    sbHeaders.AppendLine(line);
                }
                else
                {
                    sbBody.AppendLine(line);
                }
            }

            // Set header on header stringbuilder
            sbHeaders.AppendLine($"Set-Cookie: serverid='{key}';");

            // Merge the two stringbuilders
            string result = sbHeaders.ToString() + sbBody.ToString();

            // return complete result with key
            return Encoding.ASCII.GetBytes(result);
        }

        private void SetSession(TcpClient client, Server server)
        {
            try
            {
                IPEndPoint ipep = (IPEndPoint)client.Client.RemoteEndPoint;
                IPAddress ipa = ipep.Address;
                sessions.Add(new Session(ipa, server.ID));
            }
            catch
            {
                AddToLog("Session could not be set.");
            }
        }

        private byte[] SetCookies(byte[] buffer, Server server)
        {
            return GetAndSetHeaders(buffer, server.ID);
        }

        private Server GetServerForCookie(byte[] request)
        {
            string[] lines = Encoding.ASCII.GetString(request).Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            string guid = null;
            bool cookie_found = false;

            foreach (var line in lines)
            {
                if (line.Contains("Cookie") && line.Contains("serverid="))
                {
                    cookie_found = true;
                    guid = Regex.Match(line, @"serverid='(.+)'", RegexOptions.Singleline).Groups[1].Value;
                }
            }

            if (!cookie_found) COOKIE_ABSENT = true;

            return guid != null && guid != "" ? servers.Where((x) => x.ALIVE && x.ID == Guid.Parse(guid)).First() : null;
        }

        private Server GetServerForSession(TcpClient client)
        {
            try
            {
                IPEndPoint ipep = (IPEndPoint)client.Client.RemoteEndPoint;
                IPAddress ipa = ipep.Address;

                Session ses = null;
                Server s = null;

                if ((ses = sessions.Where((x) => x.IP.ToString() == ipa.ToString()).First()) != null)
                {
                    s = servers.Where((x) => x.ID == ses.serverid && x.ALIVE == true).First();

                    if (s != null)
                    {
                        return s;
                    }
                }
                else
                {
                    COOKIE_ABSENT = true;
                    return s;
                }
            }
            catch (Exception e)
            {
                AddToLog(e.Message);
                COOKIE_ABSENT = true;
                return null;
            }

            return null;
        }

        private async Task DoFailedAlgoTask(TcpClient client)
        {
            AddToLog("Sorry, no servers could be found.");
            byte[] failedBuffer = Encoding.ASCII.GetBytes("Sorry, no servers could be found.");
            await client.GetStream().WriteAsync(failedBuffer, 0, failedBuffer.Length);

            servers.ForEach((server) =>
            {
                dispatcher.Invoke(() => UpdateServerStatus(server));
            });
        }

        private async Task SendGenericErrorCode(TcpClient client)
        {
            AddToLog("Sorry, an unexpected error occurred.");
            byte[] failedBuffer = Encoding.ASCII.GetBytes("Sorry, an unexpected error occurred.");
            await client.GetStream().WriteAsync(failedBuffer, 0, failedBuffer.Length);

            servers.ForEach((server) =>
            {
                dispatcher.Invoke(() => UpdateServerStatus(server));
            });
        }

        private async Task DoFailedPersistenceTask(TcpClient client)
        {
            AddToLog("ServerID was not found. Returning Error Code.");
            byte[] failedBuffer = Encoding.ASCII.GetBytes("Sorry, your requested persisted Server is not available.");
            await client.GetStream().WriteAsync(failedBuffer, 0, failedBuffer.Length);

            servers.ForEach((server) =>
            {
                dispatcher.Invoke(() => UpdateServerStatus(server));
            });
        }

        private Server DetermineAlgorithm()
        {
            ILBAlgorithm algo = currentAlgorithm;

            if (algo == null || (SelectedMethodString != PreviousAlgoString))
            {
                algo = currentAlgorithm = algoFactory.GetAlgorithm(SelectedMethodString);
                PreviousAlgoString = SelectedMethodString;
            }

            return algo.GetServer(servers.Where((x) => x.ALIVE == true).ToList());
        }

        private Server Reconnect(Server server)
        {
            if (!server.client.Connected)
            {
                try
                {
                    server.client = new TcpClient();
                    server.client.Connect(server.HOST, server.PORT);
                    server.ALIVE = true;
                    dispatcher.Invoke(() => UpdateServerStatus(server));
                }
                catch (Exception e)
                {
                    server.ALIVE = false;
                    dispatcher.Invoke(() => UpdateServerStatus(server));
                    AddToLog($"Something went wrong while reconnecting to Server {server.HOST}:{server.PORT}: {e.Message}");
                }
            }

            return server;
        }

        private void InitServers()
        {
            servers = new List<Server>();
            int nrServers = 6;

            for (int i = 1; i <= nrServers; i++)
            {
                Guid guid = Guid.NewGuid();
                servers.Add(new Server() { ID = guid, NAME = $"{guid}", PORT = 8080 + i, HOST = "127.0.0.1", ALIVE = false, client = new TcpClient() });
            }

            servers.AsParallel().AsOrdered().ForAll(async (server) =>
            {
                try
                {
                    await server.client.ConnectAsync(server.HOST, server.PORT);
                    server.ALIVE = true;
                    AddToLog($"{server.HOST}:{server.PORT} is connected.");
                    dispatcher.Invoke(() => AddToServerList($"{server.HOST}:{server.PORT} ,{server.ID}", true));
                }
                catch
                {
                    server.ALIVE = false;
                    AddToLog($"{server.HOST}:{server.PORT} refused connections.");
                    dispatcher.Invoke(() => AddToServerList($"{server.HOST}:{server.PORT} ,{server.ID}", false));
                }
            });
        }

        private void DoHealthCheckPassive(byte[] buffer, Server serverToSend)
        {
            string response = Encoding.ASCII.GetString(buffer);
            if (!response.Contains("200 OK"))
            {
                serverToSend.ALIVE = false;
                AddToLog($"Sniffed response from {serverToSend.HOST}:{serverToSend.PORT} -> Server is dead!");
            }
            else
            {
                AddToLog($"Sniffed response from {serverToSend.HOST}:{serverToSend.PORT} -> Server is healthy!");
            }
        }

        private async void DoHealthCheck()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(5000);
                    if (!STOPPING && ACTIVE)
                    {
                        if (SelectedHealthString == "Active")
                        {
                            if (servers.Count < 1) continue;

                            AddToLog("Doing Health Check: ACTIVE");
                            servers.Where((x) => x.ALIVE == true).AsParallel().ForAll((server) =>
                            {
                                server.AskForHealthActive();
                            });

                            servers.Where((x) => x.ALIVE == false).AsParallel().ForAll((server) =>
                            {
                                server.AskForReconnect();
                            });

                            servers.ForEach((server) =>
                            {
                                dispatcher.Invoke(() => UpdateServerStatus(server));
                            });
                        }
                    }
                }

            });
        }

        private void UpdateServerStatus(Server server)
        {
            ListBoxItem serverItem = ServerList.Where((x) => x.Content.ToString().Contains($"{server.ID}")).First();

            if (serverItem != null)
            {
                var color = server.ALIVE ? Color.FromRgb(0, 200, 0) : Color.FromRgb(200, 0, 0);
                SolidColorBrush brush = new SolidColorBrush(color);
                serverItem.Foreground = brush;

                //string message = server.ALIVE ? $"Server {server.HOST}:{server.PORT} is alive and kicking." : $"Server {server.HOST}:{server.PORT} is dead, RIP.";
                //AddToLog(message);
            }
        }

        public async void AddServer()
        {
            bool conflict = false;

            if (IP_ADD != null || IP_ADD != "" && PORT_ADD != 0)
            {
                foreach (var server in servers)
                {
                    if (server.PORT == PORT_ADD && server.HOST == IP_ADD)
                    {
                        conflict = true;
                        break;
                    }
                }

                if (conflict)
                {
                    AddToLog("ERROR: A server with this specification already exists.");
                    return;
                }

                Guid guid = Guid.NewGuid();
                Server newServer = new Server() { ID = guid, HOST = IP_ADD, PORT = PORT_ADD, ALIVE = false, client = new TcpClient(), NAME = $"{guid}" };
                servers.Add(newServer);

                try
                {
                    await newServer.client.ConnectAsync(newServer.HOST, newServer.PORT);
                    newServer.ALIVE = true;
                    AddToLog($"{newServer.HOST}:{newServer.PORT} is connected.");
                    dispatcher.Invoke(() => AddToServerList($"{newServer.HOST}:{newServer.PORT} ,{newServer.ID}", true));
                }
                catch
                {
                    dispatcher.Invoke(() => AddToServerList($"{newServer.HOST}:{newServer.PORT} ,{newServer.ID}", false));
                    AddToLog($"{newServer.HOST}:{newServer.PORT} refused connections.");
                }
            }
        }

        public void RemoveServer(object item)
        {
            if (item is ListBoxItem)
            {
                ListBoxItem result = (ListBoxItem)item;
                ServerList.Remove(result);
                var content = (string)result.Content;
                var ID = content.Split(',')[1];

                Server serverToRemove = servers.Where((server) => server.ID == Guid.Parse(ID)).First();

                if (serverToRemove != null)
                {
                    servers.Remove(serverToRemove);
                    serverToRemove.client.Dispose();
                    AddToLog($"Server with ID {ID} has been removed!");
                }
            }
            else
            {
                AddToLog("Something went wrong whilst deleting the server.");
            }
        }

        public void AddToLog(string message)
        {
            dispatcher.Invoke(() => Log.Add(new ListBoxItem { Content = message }));
        }

        public void AddToMethods(string method)
        {
            dispatcher.Invoke(() => MethodItems.Add(new ListBoxItem { Content = method }));
        }

        public void AddToHealth(string health)
        {
            dispatcher.Invoke(() => HealthItems.Add(new ListBoxItem { Content = health }));
        }

        public void AddToPersist(string persistMethod)
        {
            dispatcher.Invoke(() => PersistItems.Add(new ListBoxItem { Content = persistMethod }));
        }

        public void AddToServerList(string server, bool alive)
        {
            var color = alive ? Color.FromRgb(0, 200, 0) : Color.FromRgb(200, 0, 0);
            SolidColorBrush brush = new SolidColorBrush(color);

            dispatcher.Invoke(() => ServerList.Add(new ListBoxItem() { Content = server, Foreground = brush }));
        }
    }
}
