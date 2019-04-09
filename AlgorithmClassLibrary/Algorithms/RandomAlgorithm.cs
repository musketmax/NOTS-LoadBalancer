using ServerClassLibrary;
using System;
using System.Collections.Generic;

namespace AlgorithmClassLibrary.Algorithms
{
    public class RandomAlgorithm : ILBAlgorithm
    {
        public Server GetServer(List<Server> servers)
        {
            Server selectedServer = null;

            Random rnd = new Random();
            int random = rnd.Next(0, servers.Count);
            Console.WriteLine(random);

            for (int i = 0; i < servers.Count; i++)
            {
                if (random == i)
                {
                    selectedServer = servers[i];
                    break;
                }
            }

            return selectedServer;
        }
    }
}
