using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;


namespace ModLoader
{
    public static class Bootstrapper
    {
        private static string ModLoaderPath;

        static Bootstrapper()
        {
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        }
        public static void Init()
        {
            //KLog.Dbg("Initializing Bootstrapper...");
            var currentApp = Process.GetCurrentProcess().MainModule.FileName;
            var rootPath = Path.GetDirectoryName(currentApp);
            ModLoaderPath = Path.Combine(rootPath, "ModLoader");
        }
        public static void Start()
        {
            HarmonyLoader.Enter();
        }
        static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs arg)
        {
            //KLog.Dbg($"Calling the resolver to resolve : {arg.Name}!");
            var fileName = new AssemblyName(arg.Name).Name + ".dll";
            var fileInModLoader = Path.Combine(ModLoaderPath, fileName);
            if (File.Exists(fileInModLoader))
            {
                return Assembly.LoadFrom(fileInModLoader);
            }
            else return null;
        }
    }
}