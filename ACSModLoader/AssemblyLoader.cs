using System;
using Harmony;
using System.IO;
using Mono.Cecil;
using System.Reflection;
using System.Collections.Generic;


namespace ModLoader
{
    public static class AssemblyLoader
    {
        public static List<Assembly> PreLoadAssemblies(string[] files)
        {
            ModLoader.Log.Debug("Pre-Loading assemblies");
            var result = new List<Assembly>();
            var failed = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var fileName = Path.GetFileName(file);
                // we exclude errogenous libraries that may be a problem.
                if (!(fileName.ToLower() == "0harmony") && !(fileName.ToLower() == "acsmodloader") && !(fileName.ToLower() == "mono.cecil"))
                {
                    try
                    {
                        var assembly = Assembly.ReflectionOnlyLoadFrom(file);
                        if (result.Contains(assembly))
                        {
                            ModLoader.Log.Debug($"Skipping duplicate assembly: {fileName}");
                        }
                        else
                        {
                            ModLoader.Log.Debug($"Pre-Loading assembly: {fileName}");
                            result.Add(assembly);
                        }
                    }
                    catch (Exception ex)
                    {
                        failed.Add(fileName);
                        ModLoader.Log.Debug($"Pre-Loading assembly: {fileName} failed!");
                        ModLoader.Log.Debug(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following assemblies could not be pre-loaded:\n" + string.Join("\n\t", failed.ToArray());
                ModLoader.Log.Debug(text);
            }
            return result;
        }
        public static List<Assembly> LoadAssemblies(List<Assembly> asms)
        {
            ModLoader.Log.Debug("Loading assemblies into memory");
            var result = new List<Assembly>();
            var failed = new List<string>();
            foreach (var asm in asms)
            {
                if (asm != null)
                {
                    try
                    {
                        ModLoader.Log.Debug($"Loading: {asm.FullName}");
                        var loaded = Assembly.LoadFile(asm.Location);
                        result.Add(loaded);
                    }
                    catch (Exception ex)
                    {
                        failed.Add(asm.GetName().ToString());
                        ModLoader.Log.Debug($"loading assembly {asm.GetName()} failed!");
                        ModLoader.Log.Debug(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following assemblies could not be loaded:\n" + string.Join("\n\t", failed.ToArray());
                ModLoader.Log.Debug(text);
            }
            return result;
        }
        public static bool ApplyPreLoaderPatches(IEnumerable<Assembly> asms)
        {
            ModLoader.Log.Debug("Applying preloader patches");
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
                                ModLoader.Log.Debug($"applying preloader patch: {assembly.FullName}");
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
                        ModLoader.Log.Debug($"Patching mod {assembly.GetName()} failed!");
                        ModLoader.Log.Debug(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following preloaders could not be patched:\n" + string.Join("\n\t", failed.ToArray());
                ModLoader.Log.Debug(text);
                return false;
            }
            return true;
        }
        public static bool ApplyHarmonyPatches(IEnumerable<Assembly> asms)
        {
            ModLoader.Log.Debug("Applying Harmony patches");
            var failed = new List<string>();
            foreach (var assembly in asms)
            {
                if (assembly != null)
                {
                    try
                    {
                    ModLoader.Log.Debug($"Applying harmony patch: {assembly.FullName}");
                    var harmonyInstance = HarmonyInstance.Create(assembly.FullName);
                    harmonyInstance?.PatchAll(assembly);
                    }
                    catch (Exception ex)
                    {
                        failed.Add(assembly.GetName().ToString());
                        ModLoader.Log.Debug($"Patching harmony mod {assembly.GetName().Name} failed!");
                        ModLoader.Log.Debug(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following mods could not be patched:\n" + string.Join("\n\t", failed.ToArray());
                ModLoader.Log.Debug(text);
                return false;
            }
            return true;
        }
    }
}