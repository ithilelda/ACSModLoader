using System;
using System.IO;
using System.Reflection;

namespace ModLoader
{
    public static class ModLoaderLite
    {
        public static string GamePath { get; private set; }
        public static string WorkShopPath { get; private set; }
        public static Action<string> Log { get; private set; }
        private static bool patched;

        public static void Init(string gamePath, string workShopPath, Action<string> log)
        {
            GamePath = gamePath ?? throw new ArgumentNullException("gamePath");
            WorkShopPath = workShopPath ?? throw new ArgumentNullException("workShopPath");
            Log = log ?? throw new ArgumentNullException("log");
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        }
        public static void Start()
        {
            if(!patched)
            {
                HarmonyLoaderLite.Enter();
                patched = true;
            }
        }

        private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs arg)
        {
            var fileName = new AssemblyName(arg.Name).Name + ".dll";
            Log($"the current resolving assembly is: {fileName}");
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
