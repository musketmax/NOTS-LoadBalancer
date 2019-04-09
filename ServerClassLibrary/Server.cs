using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ServerClassLibrary
{
    public class Server
    {
        public string NAME { get; set; }
        public bool ALIVE { get; set; }
        public int PORT { get; set; }
        public string HOST { get; set; }
        public System.Guid ID { get; set; }

        public TcpClient client;

        public async void AskForHealthActive()
        {
            if (!client.Connected)
            {
                ALIVE = false;
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("GET / HTTP/1.1");
            builder.AppendLine($"Host: {HOST}");
            builder.AppendLine("Connection: close");
            builder.AppendLine();
            var header = Encoding.ASCII.GetBytes(builder.ToString());

            try
            {
                using (NetworkStream stream = client.GetStream())
                using (MemoryStream memstream = new MemoryStream())
                {
                    await stream.WriteAsync(header, 0, header.Length);

                    if (stream.DataAvailable)
                    {
                        await stream.CopyToAsync(memstream);
                        string response = Encoding.ASCII.GetString(memstream.GetBuffer());

                        if (!response.Contains("200 OK")) ALIVE = false;
                    }
                }
            }
            catch { }
        }

        public async void AskForReconnect()
        {
            if (client.Connected)
            {
                return;
            }

            try
            {
                client = new TcpClient();
                await client.ConnectAsync(HOST, PORT);
                if (client.Connected) ALIVE = true;
            }
            catch { }
        }
    }
}
