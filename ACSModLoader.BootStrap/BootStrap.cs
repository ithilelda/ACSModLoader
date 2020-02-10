using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using Harmony;
using log4net;


namespace ModLoader
{
    public static class BootStrap
    {
        private static string ModPath;
        private static ILog Log = LogManager.GetLogger(typeof(BootStrap));
        static BootStrap()
        {
            var rootPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            ModPath = Path.Combine(rootPath, "ModLoader");
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        }
        public static void Enter()
        {
            var suc = ApplyHarmonyPatches(ModLoader.LoadedAssemblies);
            if (suc)
            {
                Log.Debug("All harmony patchs successfully loaded!");
            }
            else
            {
                Log.Debug("Some harmony patchs cannot be patched! Please check previous lines for error report!");
            }
        }
        private static bool ApplyHarmonyPatches(IEnumerable<Assembly> asms)
        {
            Log.Debug("Applying Harmony patches");
            var failed = new List<string>();
            foreach (var assembly in asms)
            {
                if (assembly != null)
                {
                    try
                    {
                        Log.Debug($"Applying harmony patch: {assembly.FullName}");
                        var harmonyInstance = HarmonyInstance.Create(assembly.FullName);
                        harmonyInstance?.PatchAll(assembly);
                    }
                    catch (Exception ex)
                    {
                        failed.Add(assembly.GetName().ToString());
                        Log.Error($"Patching harmony mod {assembly.GetName().Name} failed!");
                        Log.Error(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following mods could not be patched:\n" + string.Join("\n\t", failed.ToArray());
                Log.Warn(text);
                return false;
            }
            return true;
        }
        private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs arg)
        {
            var fileName = new AssemblyName(arg.Name).Name + ".dll";
            Log.Debug($"the current resolving assembly is: {fileName}");
            var file = Path.Combine(ModPath, fileName);
            if (File.Exists(file))
            {
                return Assembly.LoadFrom(file);
            }
            else return null;
        }
    }
}