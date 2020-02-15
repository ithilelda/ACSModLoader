using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace ModLoaderLite
{
    public static class Extensions
    {
        public static bool TryGetValueOrDefault(this Dictionary<string, bool> d, string key) => d.TryGetValue(key, out var ret) ? ret : default;
    }
    public class ModLoaderLite
    {
        private static Dictionary<string, bool> patched = new Dictionary<string, bool>();
        private static Dictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();

        static ModLoaderLite()
        {
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        }
        public void Load(string path, string assemblyName="", string typeFullName="")
        {
            if(!patched.TryGetValueOrDefault(path))
            {
                HarmonyLoaderLite.Enter(path, assemblyName, typeFullName);
                patched[path] = true;
            }
        }

        private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs arg)
        {
            if(!loadedAssemblies.TryGetValue(arg.Name, out var ret))
            {
                var name = new AssemblyName(arg.Name).Name + ".dll";
                var thisDir = Assembly.GetExecutingAssembly().Location;
                var askedFile = Path.Combine(thisDir, name);
                if(File.Exists(askedFile))
                {
                    var asm = Assembly.LoadFrom(askedFile);
                    loadedAssemblies.Add(arg.Name, asm);
                }
            }
            return ret;
        }
    }
}
