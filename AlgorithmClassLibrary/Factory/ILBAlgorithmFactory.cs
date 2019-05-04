using BaseAlgorithmClassLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AlgorithmClassLibrary.Algorithms.Factory
{
    public class ILBAlgorithmFactory
    {
        private static List<Type> types = new List<Type>();

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
            types = new List<Type>();

            Type tAlgo = typeof(ILBAlgorithm);
            List<string> lstClasses = new List<string>();

            var _path = @"C:\Users\thomas\source\repos\LB\AlgorithmClassLibrary\Algorithms";
            var dlls = Directory.GetFiles(_path, "*.dll");
            List<Assembly> assemblies = new List<Assembly>();

            foreach (var dll in dlls)
            {
                assemblies.Add(Assembly.LoadFile(Path.GetFullPath(dll)));
            }

            foreach (var assem in assemblies)
            {
                foreach (var type in assem.GetTypes())
                {
                    if (tAlgo.IsAssignableFrom(type) && (type != tAlgo))
                    {
                        types.Add(type);
                        lstClasses.Add(type.Name);
                    }
                }
            }

            return lstClasses;
        }

        private Type GetAlgorithmByName(string name)
        {
            object oInstance = null;

            foreach (var type in types)
            {
                if (type.Name == name)
                {
                    oInstance = type;
                }
            }

            return (oInstance as Type);
        }

    }
}

