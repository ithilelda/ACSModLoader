using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;


namespace ModLoader
{
    public static class Bootstrapper
    {
        static Bootstrapper()
        {
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        }
        public static void Enter()
        {
            HarmonyLoader.Enter();
        }
        private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs arg)
        {
            KLog.Dbg($"Calling the resolver to resolve : {arg.Name}!");
            var fileName = new AssemblyName(arg.Name).Name + ".dll";
            var currentApp = Process.GetCurrentProcess().MainModule.FileName;
            var rootPath = Path.GetDirectoryName(currentApp);
            var modLoaderPath = Path.Combine(rootPath, "ModLoader");
            var file = Path.Combine(modLoaderPath, fileName);
            KLog.Dbg($"loading file: {file}!");
            if (File.Exists(file))
            {
                return Assembly.LoadFrom(file);
            }
            else return null;
        }
    }
}