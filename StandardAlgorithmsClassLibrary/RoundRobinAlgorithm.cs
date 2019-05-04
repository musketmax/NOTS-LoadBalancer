using BaseAlgorithmClassLibrary;
using ServerClassLibrary;
using System.Collections.Generic;

namespace StandardAlgorithmsClassLibrary
{
    public class RoundRobinAlgorithm : ILBAlgorithm
    {
        private int count;

        public RoundRobinAlgorithm()
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
