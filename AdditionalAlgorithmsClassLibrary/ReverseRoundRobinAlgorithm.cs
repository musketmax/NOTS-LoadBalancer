using BaseAlgorithmClassLibrary;
using ServerClassLibrary;
using System;
using System.Collections.Generic;

namespace AdditionalAlgorithmsClassLibrary
{
    public class ReverseRoundRobinAlgorithm : ILBAlgorithm
    {
        private int count;

        public ReverseRoundRobinAlgorithm()
        {
            count = 1;
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
                Console.WriteLine(Math.Abs(count - servers.Count));
                if (i == Math.Abs(count - servers.Count))
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
