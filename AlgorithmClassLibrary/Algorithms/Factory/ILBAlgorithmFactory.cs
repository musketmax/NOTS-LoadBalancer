using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmClassLibrary.Algorithms.Factory
{
    public class ILBAlgorithmFactory
    {
        public ILBAlgorithm GetAlgorithm(string algo)
        {
            if (algo == null || algo == "")
            {
                throw new ArgumentException("No such Algorithm");
            }

            object oInstance = Activator.CreateInstance(GetAlgorithmByName(algo));

            return (oInstance as ILBAlgorithm);
        }

        public static List<string> GetAllAlgoRithms()
        {
            Type tAlgo = typeof(ILBAlgorithm);
            List<string> lstClasses = new List<string>();

            foreach (var type in Assembly.GetAssembly(tAlgo).GetTypes())
            {
                if (tAlgo.IsAssignableFrom(type) && (type != tAlgo))
                {
                    Console.WriteLine($"yay! {type.Name}");
                    lstClasses.Add(type.Name);
                }

                Console.WriteLine($"nay! {type.Name}");
            }

            return lstClasses;
        }

        private Type GetAlgorithmByName(string name)
        {
            Type tAlgo = typeof(ILBAlgorithm);
            object oInstance = null;

            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                if (tAlgo.IsAssignableFrom(type) && (type != tAlgo) && (type.Name == name))
                {
                    oInstance = type;
                }
            }

            return (oInstance as Type);
        }
    }
}
