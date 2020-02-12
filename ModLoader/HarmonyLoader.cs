using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Harmony;
using log4net;


namespace ModLoader
{
    public static class HarmonyLoader
    {
        private static ILog Log = LogManager.GetLogger(typeof(HarmonyLoader));
        public static void Enter()
        {
            List<Assembly> asms;
            if(ModLoader.HarmonyAssemblies == null)
            {
                var files = Directory.GetFiles(ModLoader.ModPath, "*.dll", SearchOption.AllDirectories);
                asms = AssemblyLoader.LoadAssemblies(AssemblyLoader.PreLoadAssemblies(files));
            }
            else asms = AssemblyLoader.LoadAssemblies(AssemblyLoader.PreLoadAssemblies(ModLoader.HarmonyAssemblies));
            var suc = Apply(asms);
            if (suc)
            {
                Log.Debug("All harmony patches successfully loaded!");
            }
            else
            {
                Log.Debug("Some harmony patches cannot be patched! Please check previous lines for error report!");
            }
        }
        private static bool Apply(IEnumerable<Assembly> asms)
        {
            Log.Debug("Applying Harmony patches");
            var failed = new List<string>();
            foreach (var assembly in asms)
            {
                try
                {
                    Log.Debug($"Applying harmony patch: {assembly.FullName}");
                    var harmonyInstance = HarmonyInstance.Create(assembly.FullName);
                    harmonyInstance?.PatchAll(assembly);
                }
                catch (Exception ex)
                {
                    failed.Add(assembly.FullName);
                    Log.Error($"Patching harmony mod {assembly.FullName} failed!");
                    Log.Error(ex.Message);
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
    }
}