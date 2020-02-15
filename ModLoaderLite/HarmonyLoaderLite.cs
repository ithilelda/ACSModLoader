using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Harmony;


namespace ModLoader
{
    public static class HarmonyLoaderLite
    {
        public static void Enter(string path)
        {
            var files = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
            var asms = AssemblyLoaderLite.LoadAssemblies(AssemblyLoaderLite.PreLoadAssemblies(files));
            var suc = Apply(asms);
            if (suc)
            {
                KLog.Dbg($"All harmony patches successfully loaded for {path}!");
            }
            else
            {
                KLog.Dbg($"Some harmony patches from {path} cannot be patched!");
            }
        }
        private static bool Apply(IEnumerable<Assembly> asms)
        {
            KLog.Dbg("Applying Harmony patches");
            var failed = new List<string>();
            foreach (var assembly in asms)
            {
                try
                {
                    KLog.Dbg($"Applying harmony patch: {assembly.FullName}");
                    var harmonyInstance = HarmonyInstance.Create(assembly.FullName);
                    harmonyInstance?.PatchAll(assembly);
                }
                catch (Exception ex)
                {
                    failed.Add(assembly.FullName);
                    KLog.Dbg($"Patching harmony mod {assembly.FullName} failed!");
                    KLog.Dbg(ex.Message);
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following mods could not be patched:\n" + string.Join("\n\t", failed.ToArray());
                KLog.Dbg(text);
                return false;
            }
            return true;
        }
    }
}