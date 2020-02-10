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
            var suc = Apply(ModLoader.LoadedAssemblies);
            if (suc)
            {
                Log.Debug("All harmony patchs successfully loaded!");
            }
            else
            {
                Log.Debug("Some harmony patchs cannot be patched! Please check previous lines for error report!");
            }
        }
        private static bool Apply(IEnumerable<Assembly> asms)
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
    }
}