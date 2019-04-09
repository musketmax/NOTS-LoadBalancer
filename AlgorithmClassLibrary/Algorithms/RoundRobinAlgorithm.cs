using ServerClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmClassLibrary.Algorithms
{
    public class RoundRobinAlgorithm : ILBAlgorithm
    {
        private int count;

        public RoundRobinAlgorithm()
        {
            count = 1;
            Console.WriteLine("new instance!");
        }

        public Server GetServer(List<Server> servers)
        {
            Server selectedServer = null;

            if (servers.Count < 1)
            {
                return selectedServer;
            }

            for (int i = 0; i < servers.Count; i++)
            {
                if ((i + 1) == count)
                {
                    count++;
                    if (count > servers.Count)
                    {
                        count = 1;
                    }

                    selectedServer = servers[i];
                    break;
                }
            }

            return selectedServer;
        }
    }
}
