using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using Harmony;


namespace ModLoader
{
    public static class BootStrap
    {
        private static string ModPath;
        public static void Enter()
        {
            var rootPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            ModPath = Path.Combine(rootPath, "ModLoader");
            KLog.Log(KLogLevel.Debug, ModPath);
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
            KLog.Log(KLogLevel.Debug, $"Harmony Patcher in Action!");
            var suc = true; //ApplyHarmonyPatches(ModLoader.LoadedAssemblies);
            if (suc)
            {
                KLog.Log(KLogLevel.Debug, "All harmony patchs successfully loaded!");
            }
            else
            {
                KLog.Log(KLogLevel.Debug, "Some harmony patchs cannot be patched! Please check previous lines for error report!");
            }
        }
        /*
        private static bool ApplyHarmonyPatches(IEnumerable<Assembly> asms)
        {
            KLog.Log(KLogLevel.Debug, "Applying Harmony patches");
            var failed = new List<string>();
            foreach (var assembly in asms)
            {
                if (assembly != null)
                {
                    try
                    {
                        KLog.Log(KLogLevel.Debug, $"Applying harmony patch: {assembly.FullName}");
                        var harmonyInstance = HarmonyInstance.Create(assembly.FullName);
                        harmonyInstance?.PatchAll(assembly);
                    }
                    catch (Exception ex)
                    {
                        failed.Add(assembly.GetName().ToString());
                        KLog.Log(KLogLevel.Debug, $"Patching harmony mod {assembly.GetName().Name} failed!");
                        KLog.Log(KLogLevel.Debug, ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following mods could not be patched:\n" + string.Join("\n\t", failed.ToArray());
                KLog.Log(KLogLevel.Debug, text);
                return false;
            }
            return true;
        }*/
        private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs arg)
        {
            var fileName = new AssemblyName(arg.Name).Name + ".dll";
            KLog.Log(KLogLevel.Debug, $"the current resolving assembly is: {fileName}");
            var file = Path.Combine(ModPath, fileName);
            if (File.Exists(file))
            {
                return Assembly.LoadFrom(file);
            }
            else return null;
        }
    }
}