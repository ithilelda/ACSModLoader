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
        private static bool initialized;
        private static Dictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();

        static ModLoaderLite()
        {
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        }
        public void Load(string path, string assemblyName="", string typeFullName="")
        {
            if(!initialized)
            {
                Init();
                initialized = true;
            }
            if(!patched.TryGetValueOrDefault(path))
            {
                HarmonyLoaderLite.Enter(path, assemblyName, typeFullName);
                patched[path] = true;
            }
        }

        private void Init()
        {
            var thisDir = Assembly.GetExecutingAssembly().Location;
            var thisDlls = Directory.GetFiles(thisDir, "*.dll", SearchOption.AllDirectories);
            foreach(var dllFile in thisDlls)
            {
                if(Path.GetFileNameWithoutExtension(dllFile) != "ModLoaderLite")
                {
                    try
                    {
                        var bytes = File.ReadAllBytes(dllFile);
                        var asm = Assembly.Load(bytes);
                        loadedAssemblies.Add(asm.FullName, asm);
                    }
                    catch(Exception ex)
                    {
                        KLog.Dbg(ex.Message);
                    }
                }
            }
        }
        private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs arg)
        {
            loadedAssemblies.TryGetValue(arg.Name, out var ret);
            return ret;
        }
    }
}
