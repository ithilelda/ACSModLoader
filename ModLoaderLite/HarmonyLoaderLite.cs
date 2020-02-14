using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Harmony;


namespace ModLoader
{
    public static class HarmonyLoaderLite
    {
        public static void Enter()
        {
            var files = Directory.GetFiles(ModLoaderLite.WorkShopPath, "*.dll", SearchOption.AllDirectories);
            var asms = AssemblyLoaderLite.LoadAssemblies(AssemblyLoaderLite.PreLoadAssemblies(files));
            var suc = Apply(asms);
            if (suc)
            {
                ModLoaderLite.Log("All harmony patches successfully loaded!");
            }
            else
            {
                ModLoaderLite.Log("Some harmony patches cannot be patched! Please check previous lines for error report!");
            }
        }
        private static bool Apply(IEnumerable<Assembly> asms)
        {
            ModLoaderLite.Log("Applying Harmony patches");
            var failed = new List<string>();
            foreach (var assembly in asms)
            {
                try
                {
                    ModLoaderLite.Log($"Applying harmony patch: {assembly.FullName}");
                    var harmonyInstance = HarmonyInstance.Create(assembly.FullName);
                    harmonyInstance?.PatchAll(assembly);
                }
                catch (Exception ex)
                {
                    failed.Add(assembly.FullName);
                    ModLoaderLite.Log($"Patching harmony mod {assembly.FullName} failed!");
                    ModLoaderLite.Log(ex.Message);
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following mods could not be patched:\n" + string.Join("\n\t", failed.ToArray());
                ModLoaderLite.Log(text);
                return false;
            }
            return true;
        }
    }
}