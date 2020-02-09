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
        public static List<Assembly> PreloadAssemblies(string[] files)
        {
            ModLoader.Log("Pre-Loading assemblies");
            var result = new List<Assembly>();
            var failed = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                var fileName = files[i];
                var fileInfo = new FileInfo(fileName);
                // we exclude errogenous libraries that may be a problem.
                if (!(fileInfo.Name.ToLower() == "0harmony") && !(fileInfo.Name.ToLower() == "acsmodloader") && !(fileInfo.Name.ToLower() == "mono.cecil"))
                {
                    try
                    {
                        var assembly = Assembly.ReflectionOnlyLoadFrom(fileName);
                        if (result.Contains(assembly))
                        {
                            ModLoader.Log($"Skipping duplicate assembly: {fileInfo.Name}");
                        }
                        else
                        {
                            ModLoader.Log($"Preloading: {fileInfo.Name}");
                            result.Add(assembly);
                        }
                    }
                    catch (Exception ex)
                    {
                        failed.Add(fileInfo.Name);
                        ModLoader.Log($"Preloading assembly: {fileInfo.Name} failed!");
                        ModLoader.Log(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following assemblies could not be pre-loaded:\n" + string.Join("\n\t", failed.ToArray());
                ModLoader.Log(text);
            }
            return result;
        }
        public static List<Assembly> SortDependencies(List<Assembly> asms)
        {
            ModLoader.Log("sorting dependencies");
            asms.Sort(new AssemblyComparer());
            return asms;
        }
        public static List<Assembly> LoadAssemblies(List<Assembly> asms)
        {
            ModLoader.Log("Loading assemblies into memory");
            var result = new List<Assembly>();
            var failed = new List<string>();
            foreach(var asm in asms)
            {
                if (asm != null)
                {
                    try
                    {
                        ModLoader.Log($"Loading: {asm.FullName}");
                        var loaded = Assembly.LoadFile(asm.Location);
                        result.Add(loaded);
                    }
                    catch (Exception ex)
                    {
                        failed.Add(asm.GetName().ToString());
                        ModLoader.Log($"loading assembly {asm.GetName()} failed!");
                        ModLoader.Log(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following assemblies could not be loaded:\n" + string.Join("\n\t", failed.ToArray());
                ModLoader.Log(text);
            }
            return result;
        }
        public static bool ApplyPreLoaderPatches(List<Assembly> asms)
        {
            ModLoader.Log("Applying preloader patches");
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
                                ModLoader.Log($"applying preloader patch: {assembly.FullName}");
                                var target = preloaderAttr.Target + ".dll";
                                var backup = preloaderAttr.Target + ".bck";
                                var targetFile = Path.Combine(ModLoader.ManagedPath, target);
                                var backupFile = Path.Combine(ModLoader.ManagedPath, backup);
                                if (!File.Exists(targetFile))
                                {
                                    throw new Exception("patcher target invalid!"); // if the target is invalid, we throw an exception to goto the catch block.
                                }
                                // if there is no backup file, we have the original dll. We need to copy it to the backup for later restoration.
                                if (!File.Exists(backupFile))
                                {
                                    File.Copy(targetFile, backupFile);
                                }
                                // the entry method is a public static void Enter(ModuleDefinition mod).
                                var entry = type.GetMethod("Enter", BindingFlags.Public | BindingFlags.Static);
                                if (entry == null) throw new Exception("no entry method found in the patcher class!");
                                var asm = AssemblyDefinition.ReadAssembly(targetFile);
                                entry.Invoke(null, new object[] { asm.MainModule });
                                asm.Dispose();
                                break; // once we found the type and called Enter, we break the loop. There is only one enter allowed each patcher.
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failed.Add(assembly.GetName().ToString());
                        ModLoader.Log($"Patching mod {assembly.GetName()} failed!");
                        ModLoader.Log(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following preloaders could not be patched:\n" + string.Join("\n\t", failed.ToArray());
                ModLoader.Log(text);
                return false;
            }
            return true;
        }
        public static bool ApplyHarmonyPatches(List<Assembly> asms)
        {
            ModLoader.Log("Applying Harmony patches");
            var failed = new List<string>();
            foreach (var assembly in asms)
            {
                if (assembly != null)
                {
                    try
                    {
                        var types = assembly.GetTypes();
                        foreach(var type in types)
                        {
                            var harmonyAttr = Attribute.GetCustomAttribute(type, typeof(HarmonyPatch));
                            if (harmonyAttr != null)
                            {
                                ModLoader.Log($"Applying harmony patch: {assembly.FullName}");
                                var harmonyInstance = HarmonyInstance.Create(assembly.FullName);
                                harmonyInstance?.PatchAll(assembly);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failed.Add(assembly.GetName().ToString());
                        ModLoader.Log($"Patching harmony mod {assembly.GetName().Name} failed!");
                        ModLoader.Log(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following mods could not be patched:\n" + string.Join("\n\t", failed.ToArray());
                ModLoader.Log(text);
                return false;
            }
            return true;
        }
    }
}