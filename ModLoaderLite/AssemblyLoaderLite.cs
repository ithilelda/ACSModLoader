using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Harmony;


namespace ModLoader
{
    public static class AssemblyLoaderLite
    {
        public static List<Assembly> PreLoadAssemblies(IEnumerable<string> files)
        {
            ModLoaderLite.Log("Pre-Loading assemblies");
            var result = new List<Assembly>();
            var failed = new List<string>();
            foreach(var file in files)
            {
                var fileName = Path.GetFileName(file);
                // we exclude errogenous libraries that may be a problem.
                if (!(fileName.ToLower() == "0harmony") && !(fileName.ToLower().Contains("mono.cecil")) && !(fileName.ToLower().Contains("acsmodloader")))
                {
                    try
                    {
                        var assembly = Assembly.ReflectionOnlyLoadFrom(file);
                        if (result.Contains(assembly))
                        {
                            ModLoaderLite.Log($"Skipping duplicate assembly: {fileName}");
                        }
                        else
                        {
                            ModLoaderLite.Log($"Pre-Loading assembly: {fileName}");
                            result.Add(assembly);
                        }
                    }
                    catch (Exception ex)
                    {
                        failed.Add(fileName);
                        ModLoaderLite.Log($"Pre-Loading assembly: {fileName} failed!");
                        ModLoaderLite.Log(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following assemblies could not be pre-loaded:\n" + string.Join("\n\t", failed.ToArray());
                ModLoaderLite.Log(text);
            }
            return result;
        }
        public static List<Assembly> LoadAssemblies(IEnumerable<Assembly> asms)
        {
            ModLoaderLite.Log("Loading assemblies into memory");
            var result = new List<Assembly>();
            var failed = new List<string>();
            foreach (var asm in asms)
            {
                if (asm != null)
                {
                    try
                    {
                        ModLoaderLite.Log($"Loading: {asm.FullName}");
                        var loaded = Assembly.LoadFrom(asm.Location);
                        result.Add(loaded);
                    }
                    catch (Exception ex)
                    {
                        failed.Add(asm.GetName().ToString());
                        ModLoaderLite.Log($"loading assembly {asm.GetName()} failed!");
                        ModLoaderLite.Log(ex.Message);
                    }
                }
            }
            if (failed.Count > 0)
            {
                var text = "\nThe following assemblies could not be loaded:\n" + string.Join("\n\t", failed.ToArray());
                ModLoaderLite.Log(text);
            }
            return result;
        }
    }
}