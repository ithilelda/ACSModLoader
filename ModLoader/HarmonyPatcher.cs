using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using Harmony;
using log4net;


namespace ModLoader
{
    public static class HarmonyPatcher
    {
        private static ILog Log = LogManager.GetLogger(typeof(HarmonyPatcher));
        public static void Enter()
        {
            var suc = Apply(ModLoader.Harmonies);
            if (suc)
            {
                Log.Debug("All harmony patches successfully loaded!");
            }
            else
            {
                Log.Debug("Some harmony patches cannot be patched! Please check previous lines for error report!");
            }
        }
        private static bool Apply(IEnumerable<string> files)
        {
            Log.Debug("Applying Harmony patches");
            var failed = new List<string>();
            foreach (var file in files)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    Log.Debug($"Applying harmony patch: {assembly.FullName}");
                    var harmonyInstance = HarmonyInstance.Create(assembly.FullName);
                    harmonyInstance?.PatchAll(assembly);
                }
                catch (Exception ex)
                {
                    var filename = Path.GetFileNameWithoutExtension(file);
                    failed.Add(filename);
                    Log.Error($"Patching harmony mod {filename} failed!");
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