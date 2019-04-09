using AlgorithmClassLibrary;
using AlgorithmClassLibrary.Algorithms.Factory;
using ServerClassLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace LoadBalancerClassLibrary.Models
{
    public class LoadBalancerModel
    {
        public ObservableCollection<ListBoxItem> Log { get; set; }
        public ObservableCollection<ListBoxItem> ServerList { get; set; }
        public ObservableCollection<ListBoxItem> MethodItems { get; set; }
        public ObservableCollection<ListBoxItem> HealthItems { get; set; }
        public ListBoxItem SelectedItem { get; set; }
        public ListBoxItem SelectedMethod { get; set; }

        public string SelectedMethodString;
        public string SelectedHealthString;
        private string PreviousAlgoString;

        public bool ACTIVE = false;
        public bool STOPPING = false;

        public int PORT = 8080;
        public string IP = "127.0.0.1";
        public int BUFFER_SIZE = 1024;

        public string IP_ADD = "127.0.0.1";
        public int PORT_ADD = 8085;

        private TcpListener _listener;
        private Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        private List<Server> servers;

        private ILBAlgorithmFactory algoFactory;
        private ILBAlgorithm currentAlgorithm;

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
            Log = new ObservableCollection<ListBoxItem>();
            ServerList = new ObservableCollection<ListBoxItem>();
            MethodItems = new ObservableCollection<ListBoxItem>();
            HealthItems = new ObservableCollection<ListBoxItem>();
            SelectedItem = new ListBoxItem();

            InitAlgos();

            AddToHealth("Active");
            AddToHealth("Passive");
        }

        private void InitAlgos()
        {
            MethodItems = new ObservableCollection<ListBoxItem>();
            
            ILBAlgorithmFactory.GetAllAlgoRithms().ForEach((x) =>
            {
                AddToMethods(x);
            });

            if (MethodItems.Count > 0)
            {
                SelectedMethod = MethodItems[0];
            }
        }

        public async void Start()
        {
            ACTIVE = true;
            _listener = new TcpListener(IPAddress.Parse(IP), PORT);
            _listener.Start();
            AddToLog("Started LoadBalancer.");

            try
            {
                InitAlgos();
                await Task.Run(() => ListenForClients());
            }
            catch { }
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
                            bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
                            await memStream.WriteAsync(buffer, 0, bytesRead);
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
                    Console.WriteLine(SelectedMethodString);
                    ILBAlgorithm algo = currentAlgorithm;

                    if (algo == null || (SelectedMethodString != PreviousAlgoString))
                    {
                        algo = currentAlgorithm = algoFactory.GetAlgorithm(SelectedMethodString);
                        PreviousAlgoString = SelectedMethodString;
                    }

                    Server s = algo.GetServer(servers.Where((x) => x.ALIVE == true).ToList());

                    if (s == null)
                    {
                        AddToLog("Sorry, no servers could be found.");
                        byte[] failedBuffer = Encoding.ASCII.GetBytes("Sorry, no servers could be found");
                        await client.GetStream().WriteAsync(failedBuffer, 0, failedBuffer.Length);
                        return;
                    }

                    Server serverToSend = Reconnect(s);

                    byte[] buffer = new byte[BUFFER_SIZE];

                    using (stream)
                    using (NetworkStream clientStream = client.GetStream())
                    using (NetworkStream serverStream = serverToSend.client.GetStream())
                    {
                        serverToSend.client.SendTimeout = 2000;
                        serverToSend.client.ReceiveTimeout = 2000;

                        if (serverStream.CanWrite)
                        {
                            await serverStream.WriteAsync(stream.GetBuffer(), 0, stream.GetBuffer().Length);

                            if (serverStream.DataAvailable)
                            {
                                int bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length);
                                await clientStream.WriteAsync(buffer, 0, bytesRead);

                                AddToLog($"Load balanced to server {serverToSend.NAME}.");
                                AddToLog($"Used {SelectedMethodString}.");
                            }
                        }
                        else
                        {
                            // Try to do task again recursively
                            await LoadBalanceRequest(stream, client);
                        }
                    }
                }
                catch (Exception e)
                {
                    AddToLog(e.Message);
                }
            }
        }

        private Server Reconnect(Server server)
        {
            while (!server.client.Connected)
            {
                try
                {
                    server.client = new TcpClient();
                    server.client.Connect(server.HOST, server.PORT);
                }
                catch (Exception e)
                {
                    AddToLog($"Something went wrong while reconnecting to Server {server.HOST}:{server.PORT}: {e.Message}");
                }
            }

            return server;
        }

        private void InitServers()
        {
            servers = new List<Server>();
            int nrServers = 5;

            for (int i = 1; i <= nrServers; i++)
            {
                servers.Add(new Server() { ID = i.ToString(), NAME = $"#{i}", PORT = 8080 + i, HOST = "127.0.0.1", ALIVE = false, client = new TcpClient() });
            }

            servers.ForEach(async (server) =>
            {
                try
                {
                    await server.client.ConnectAsync(server.HOST, server.PORT);
                    server.ALIVE = true;
                    AddToLog($"{server.HOST}:{server.PORT} is connected.");
                    dispatcher.Invoke(() => AddToServerList($"{server.HOST}:{server.PORT} , #{server.ID}", true));
                }
                catch
                {
                    server.ALIVE = false;
                    AddToLog($"{server.HOST}:{server.PORT} refused connections.");
                    dispatcher.Invoke(() => AddToServerList($"{server.HOST}:{server.PORT} , #{server.ID}", false));
                }
            });
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
                        else if (SelectedHealthString == "Passive")
                        {
                            AddToLog("Doing Health Check: PASSIVE");
                        }
                    }
                }

            });
        }

        private void UpdateServerStatus(Server server)
        {
            ListBoxItem serverItem = ServerList.Where((x) => x.Content.ToString().Contains($"#{server.ID}")).First();

            if (serverItem != null)
            {
                var color = server.ALIVE ? Color.FromRgb(0, 200, 0) : Color.FromRgb(200, 0, 0);
                SolidColorBrush brush = new SolidColorBrush(color);
                serverItem.Foreground = brush;

                string message = server.ALIVE ? $"Server {server.HOST}:{server.PORT} is alive and kicking." : $"Server {server.HOST}:{server.PORT} is dead, RIP.";
                AddToLog(message);
            }
        }

        public async void AddServer()
        {
            if (IP_ADD != null || IP_ADD != "" && PORT_ADD != 0)
            {
                Server newServer = new Server() { ID = (servers.Count + 1).ToString(), HOST = IP_ADD, PORT = PORT_ADD, ALIVE = false, client = new TcpClient(), NAME = "New Server" };
                servers.Add(newServer);

                try
                {
                    await newServer.client.ConnectAsync(newServer.HOST, newServer.PORT);
                    newServer.ALIVE = true;
                    AddToLog($"{newServer.HOST}:{newServer.PORT} is connected.");
                    dispatcher.Invoke(() => AddToServerList($"{newServer.HOST}:{newServer.PORT} , #{newServer.ID}", true));
                }
                catch
                {
                    dispatcher.Invoke(() => AddToServerList($"{newServer.HOST}:{newServer.PORT} , #{newServer.ID}", false));
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
                var ID = content.Split('#')[1];

                Server serverToRemove = servers.Where((server) => server.ID == ID).First();

                if (serverToRemove != null)
                {
                    servers.Remove(serverToRemove);
                    serverToRemove.client.Dispose();
                    AddToLog($"Server with ID #{ID} has been removed!");
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

        public void AddToServerList(string server, bool alive)
        {
            var color = alive ? Color.FromRgb(0, 200, 0) : Color.FromRgb(200, 0, 0);
            SolidColorBrush brush = new SolidColorBrush(color);

            dispatcher.Invoke(() => ServerList.Add(new ListBoxItem() { Content = server, Foreground = brush }));
        }
    }
}
