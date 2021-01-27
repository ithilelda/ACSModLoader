using System;
using System.IO;
using System.Reflection;
using Serilog;


namespace bootstrapper
{
    public static class Bootstrapper
    {
        private readonly static string rootPath = Path.GetDirectoryName(Environment.GetEnvironmentVariable("DOORSTOP_INVOKE_DLL_PATH"));
        static Bootstrapper()
        {
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        }
        public static void Main()
        {
            var logFile = Path.Combine(rootPath, $"log.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFile)
                .CreateLogger();
            Log.Information(" [BootStrapper] The bootstrapper is running in {@ModPath}!", rootPath);
            ModLoader.ModLoader.Init();
        }
        static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs arg)
        {
            var fileName = new AssemblyName(arg.Name).Name + ".dll";
            var fileInModLoader = Path.Combine(rootPath, fileName);
            if (File.Exists(fileInModLoader))
            {
                return Assembly.LoadFrom(fileInModLoader);
            }
            else return null;
        }
    }
}