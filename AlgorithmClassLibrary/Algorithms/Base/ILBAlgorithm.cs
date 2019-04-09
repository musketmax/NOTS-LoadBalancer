using ServerClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmClassLibrary
{
    public interface ILBAlgorithm
    {
        Server GetServer(List<Server> servers);
    }
}
