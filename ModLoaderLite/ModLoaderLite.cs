using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace ModLoader
{
    public static class Extensions
    {
        public static bool TryGetValueOrDefault(this Dictionary<string, bool> d, string key) => d.TryGetValue(key, out var ret) ? ret : default;
    }
    public class ModLoaderLite
    {
        private static Dictionary<string, bool> patched = new Dictionary<string, bool>();

        static ModLoaderLite()
        {
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        }
        public void Load(string path)
        {
            if(!patched.TryGetValueOrDefault(path))
            {
                HarmonyLoaderLite.Enter(path);
                patched[path] = true;
            }
        }

        private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs arg)
        {
            var fileName = new AssemblyName(arg.Name).Name + ".dll";
            var thisDir = Assembly.GetExecutingAssembly().Location;
            var file = Path.Combine(thisDir, fileName);
            if (File.Exists(file))
            {
                return Assembly.LoadFrom(file);
            }
            else return null;
        }
    }
}
