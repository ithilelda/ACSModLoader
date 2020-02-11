using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
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
            var currentApp = Process.GetCurrentProcess().MainModule.FileName;
            var rootPath = Path.GetDirectoryName(currentApp);
            var modPath = Path.Combine(rootPath, ModLoader.MOD_DIR_NAME);
            var files = Directory.GetFiles(modPath, "*.dll", SearchOption.AllDirectories);
            var harmonies = AssemblyLoader.SortHarmony(files);
            var asms = AssemblyLoader.LoadAssemblies(AssemblyLoader.PreLoadAssemblies(harmonies));
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