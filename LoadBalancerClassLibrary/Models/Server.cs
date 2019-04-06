using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancerClassLibrary
{
    public class Server
    {
        public string NAME { get; set; }
        public bool ALIVE { get; set; }
        public int PORT { get; set; }
        public string HOST { get; set; }
        public string ID { get; set; }

        public TcpClient client;
        public Server()
        {
        }

        public async void AskForHealth()
        {
            using (var stream = client.GetStream())
            {
                client.SendTimeout = 500;
                client.ReceiveTimeout = 1000;

                int bytesRead = 0;
                byte[] buffer = new byte[1024];
                await stream.WriteAsync(buffer, 0, buffer.Length);

                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                }
                catch
                {
                    Console.WriteLine("Oops");
                }

                ASCIIEncoding encoder = new ASCIIEncoding();
                Console.WriteLine(encoder.GetString(buffer, 0, bytesRead));
            }
        }
    }
}
