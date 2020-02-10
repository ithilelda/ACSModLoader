using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Mono.Cecil;
using log4net;


namespace ModLoader
{
    public static class PreLoaderPatcher
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PreLoaderPatcher));
        public static void Enter()
        {
            var suc = Apply(ModLoader.LoadedAssemblies);
            if (suc)
            {
                Log.Debug("All preloader patchers successfully loaded!");
            }
            else
            {
                Log.Debug("Some patchers cannot be patched! Please check previous lines for error report!");
            }
        }
        private static bool Apply(IEnumerable<Assembly> asms)
        {
            Log.Debug("Applying preloader patches");
            var failed = new List<string>();
            foreach (var assembly in asms)
            {
                if (assembly != null)
                {
                    try
                    {
                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            var preloaderAttr = Attribute.GetCustomAttribute(type, typeof(PreLoaderPatcherAttribute)) as PreLoaderPatcherAttribute;
                            if (preloaderAttr != null)
                            {
                                Log.Debug($"applying preloader patch: {assembly.FullName}");
                                var target = preloaderAttr.Target + ".dll";
                                var targetFile = Path.Combine(ModLoader.ManagedPath, target);
                                var backupFile = Path.ChangeExtension(targetFile, "bck");
                                var tmpFile = Path.ChangeExtension(targetFile, "tmp");
                                if (!File.Exists(targetFile))
                                {
                                    throw new Exception("patcher target invalid!"); // if the target is invalid, we throw an exception to goto the catch block.
                                }
                                // if there is no backup file, we have the original dll. We need to copy it to the backup for later restoration.
                                if (!File.Exists(backupFile))
                                {
                                    File.Copy(targetFile, backupFile);
                                }
                                // the entry method is a public static void Enter(ModuleDefinition module).
                                var entry = type.GetMethod("Enter", BindingFlags.Public | BindingFlags.Static);
                                if (entry == null) throw new Exception("no entry method found in the patcher class!");
                                // create the tmp file and read it for patching.
                                File.Delete(tmpFile);
                                File.Copy(targetFile, tmpFile);
                                var asm = AssemblyDefinition.ReadAssembly(tmpFile);
                                entry.Invoke(null, new object[] { asm.MainModule });
                                asm.Write(targetFile);
                                asm.Dispose();
                                File.Delete(tmpFile);
                                break; // once we found the type and called Enter, we break the loop. There is only one enter allowed each patcher.
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failed.Add(assembly.GetName().ToString());
                        Log.Error($"Patching mod {assembly.GetName()} failed!");
                        Log.Error(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following preloaders could not be patched:\n" + string.Join("\n\t", failed.ToArray());
                Log.Warn(text);
                return false;
            }
            return true;
        }
    }
}