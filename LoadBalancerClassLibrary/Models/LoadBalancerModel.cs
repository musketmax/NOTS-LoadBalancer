using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace LoadBalancerClassLibrary.Models
{
    public class LoadBalancerModel
    {
        public ObservableCollection<ListBoxItem> Log { get; set; }
        public ObservableCollection<ListBoxItem> ServerList { get; set; }
        public ListBoxItem SelectedItem { get; set; }

        public bool ACTIVE = false;
        public bool STOPPING = false;

        public int PORT = 8080;
        public string IP = "127.0.0.1";
        public string IP_ADD = "";
        public int PORT_ADD;

        private TcpListener _listener;
        private Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        private List<Server> servers;

        public LoadBalancerModel()
        {
            Log = new ObservableCollection<ListBoxItem>();
            ServerList = new ObservableCollection<ListBoxItem>();
            SelectedItem = new ListBoxItem();
            InitServers();
            //DoHealthCheck();
        }

        public async void Start()
        {
            ACTIVE = true;
            _listener = new TcpListener(IPAddress.Parse(IP), PORT);
            _listener.Start();
            AddToLog("Started LoadBalancer.");

            try
            {
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

        public async Task ListenForClients()
        {
            while (ACTIVE && !STOPPING)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    await Task.Run(() => HandleClientRequest(client));
                }
                catch (Exception e)
                {
                    AddToLog(e.Message);
                }
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
                        byte[] buffer = new byte[1024];
                        int bytesRead;

                        if (clientStream.DataAvailable)
                        {
                            bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
                            await memStream.WriteAsync(buffer, 0, bytesRead);
                        }

                        await LoadBalanceRequest(memStream, client);
                    }
                    catch (Exception e)
                    {
                        AddToLog(e.Message);
                    }
                }
            }
        }

        private Server GetRandomServer()
        {
            if (servers.Count < 1)
            {
                return null;
            }

            Server s = null;
            Random random = new Random();

            while (s == null)
            {
                string rnd = random.Next(1, servers.Count).ToString();
                s = servers.Where((server) => (server.ID == rnd) && server.ALIVE == true).First();
            }

            return s;
        }

        private async Task LoadBalanceRequest(MemoryStream stream, TcpClient client)
        {
            try
            {
                Server s = GetRandomServer();

                if (s == null)
                {
                    Console.WriteLine("nope");
                    AddToLog("Sorry, no servers could be found.");
                    return;
                }

                Server serverToSend = Reconnect(s);

                byte[] buffer = new byte[1024];

                using (stream)
                using (NetworkStream clientStream = client.GetStream())
                using (NetworkStream serverStream = serverToSend.client.GetStream())
                {

                    if (serverStream.CanWrite)
                    {
                        await serverStream.WriteAsync(stream.GetBuffer(), 0, stream.GetBuffer().Length);

                        if (serverStream.DataAvailable)
                        {
                            int bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length);
                            await clientStream.WriteAsync(buffer, 0, bytesRead);

                            AddToLog($"Load balanced to server {serverToSend.NAME}.");
                        }
                    }
                    else
                    {
                        await LoadBalanceRequest(stream, client);
                    }
                }
            }
            catch (Exception e)
            {
                AddToLog(e.Message);
            }
        }

        private Server Reconnect(Server server)
        {
            while (!server.client.Connected)
            {
                server.client = new TcpClient();
                server.client.Connect(server.HOST, server.PORT);  // = new TcpClient(server.HOST, server.PORT);
                Console.WriteLine($"Reconnected server {server.NAME}");
            }

            return server;
        }

        public void AddToLog(string message)
        {
            dispatcher.Invoke(() => Log.Add(new ListBoxItem { Content = message }));
        }

        public void AddToServerList(string server, bool alive)
        {
            var color = alive ? Color.FromRgb(0, 200, 0) : Color.FromRgb(200, 0, 0);
            SolidColorBrush brush = new SolidColorBrush(color);

            dispatcher.Invoke(() => ServerList.Add(new ListBoxItem() { Content = server, Foreground = brush }));
        }

        private async void InitServers()
        {
            servers = new List<Server>();
            int nrServers = 5;

            for (int i = 1; i <= nrServers; i++)
            {
                servers.Add(new Server() { ID = i.ToString(), NAME = $"#{i}", PORT = 8080 + i, HOST = "127.0.0.1", ALIVE = false, client = new TcpClient() });
            }

            await Task.Run(() => servers.ForEach(async (server) =>
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
            }));
        }

        private void DoHealthCheck()
        {
            while (true)
            {
                if (servers.Count < 1) continue;

                var alive = servers.Where((x) => x.ALIVE == true).ToList();
                alive.ForEach((server) => server.AskForHealth());

                var dead = servers.Where((x) => x.ALIVE != true).ToList();

                dead.ForEach((deadServer) =>
                {
                    AddToLog($"Server {deadServer.NAME} is currently unavailable.");
                });
            }
        }
    }
}
