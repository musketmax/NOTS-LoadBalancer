using ServerClassLibrary;
using System.Collections.Generic;

namespace BaseAlgorithmClassLibrary
{
    public interface ILBAlgorithm
    {
        Server GetServer(List<Server> servers);
    }
}
