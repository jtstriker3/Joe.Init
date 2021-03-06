﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Joe.Initialize
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class Init : Attribute
    {
        /// <summary>
        /// Fuction to call to preform starup logic
        /// Defaults to Init
        /// </summary>
        public String Function { get; set; }
        public IEnumerable<Type> GenericArguments { get; private set; }

        public Init(params Type[] genericArguments)
        {
            GenericArguments = genericArguments;
            Function = "Init";
        }

        public static void RunInitFunctions()
        {
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;

            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes().Where(type => type.GetCustomAttributes(typeof(Init), false).Count() > 0);
                }
                catch
                {
                    //Do Nothing Do not Stop program from running if assembly cannot be loaded
                }
                return new List<Type>();
            });

            RunInitFunctions(types);
        }

        protected static void RunInitFunctions(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                var initAttrList = type.GetCustomAttributes(typeof(Init), false) as IEnumerable<Init>;

                foreach (var initAttr in initAttrList)
                {

                    Type fullType = type;
                    if (initAttr.GenericArguments.Count() > 0)
                        fullType = type.MakeGenericType(initAttr.GenericArguments.ToArray());

                    var function = fullType.GetMethod(initAttr.Function);
                    if (function.IsStatic)
                        if (function.GetParameters().Count() == 0)
                            function.Invoke(null, null);
                        else
                            throw new Exception("Function must take 0 Parameters");
                    else throw new Exception("Function Declared in Init Attribute is not Static");
                }
            }
        }

        static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            RunInitFunctions(args.LoadedAssembly.GetTypes());
        }
    }
}
