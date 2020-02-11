using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using log4net;
using Harmony;
using Mono.Cecil;


namespace ModLoader
{
    public static class AssemblyLoader
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AssemblyLoader));
        public static void SortAssemblies(string[] files, out List<string> deps, out List<string> preloaders, out List<string> harmonies)
        {
            deps = new List<string>();
            preloaders = new List<string>();
            harmonies = new List<string>();
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(ModLoader.ModLoaderPath);
            var readerParam = new ReaderParameters { AssemblyResolver = resolver };
            foreach(var file in files)
            {
                try
                {
                    using (var asmDef = AssemblyDefinition.ReadAssembly(file, readerParam))
                    {
                        var has_patcher = asmDef.MainModule.Types.FirstOrDefault(t => t.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == typeof(PreLoaderPatchAttribute).FullName) != null) != null;
                        var refs = asmDef.MainModule.AssemblyReferences.Where(r =>
                        {
                            var name = r.Name.ToLower();
                            return name.Contains("assembly-csharp") || name.Contains("unityengine");
                        });
                        var is_patcher = has_patcher && refs.Count() == 0;
                        var is_harmony = asmDef.MainModule.Types.FirstOrDefault(t => t.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == typeof(HarmonyPatch).FullName) != null) != null;
                        if (is_patcher) preloaders.Add(file);
                        else if (is_harmony) harmonies.Add(file);
                        else deps.Add(file);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message); // we just print message and continue.
                }
            }
        }
        public static List<Assembly> PreLoadAssemblies(string[] files)
        {
            Log.Debug("Pre-Loading assemblies");
            var result = new List<Assembly>();
            var failed = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var fileName = Path.GetFileName(file);
                // we exclude errogenous libraries that may be a problem.
                if (!(fileName.ToLower() == "0harmony") && !(fileName.ToLower().Contains("mono.cecil")) && !(fileName.ToLower().Contains("acsmodloader")))
                {
                    try
                    {
                        var assembly = Assembly.ReflectionOnlyLoadFrom(file);
                        if (result.Contains(assembly))
                        {
                            Log.Debug($"Skipping duplicate assembly: {fileName}");
                        }
                        else
                        {
                            Log.Debug($"Pre-Loading assembly: {fileName}");
                            result.Add(assembly);
                        }
                    }
                    catch (Exception ex)
                    {
                        failed.Add(fileName);
                        Log.Error($"Pre-Loading assembly: {fileName} failed!");
                        Log.Error(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following assemblies could not be pre-loaded:\n" + string.Join("\n\t", failed.ToArray());
                Log.Warn(text);
            }
            return result;
        }
        public static List<Assembly> LoadAssemblies(List<Assembly> asms)
        {
            Log.Debug("Loading assemblies into memory");
            var result = new List<Assembly>();
            var failed = new List<string>();
            foreach (var asm in asms)
            {
                if (asm != null)
                {
                    try
                    {
                        Log.Debug($"Loading: {asm.FullName}");
                        var loaded = Assembly.LoadFrom(asm.Location);
                        result.Add(loaded);
                    }
                    catch (Exception ex)
                    {
                        failed.Add(asm.GetName().ToString());
                        Log.Error($"loading assembly {asm.GetName()} failed!");
                        Log.Error(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following assemblies could not be loaded:\n" + string.Join("\n\t", failed.ToArray());
                Log.Warn(text);
            }
            return result;
        }
    }
}