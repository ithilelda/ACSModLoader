using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using log4net;


namespace ModLoader
{
    public static class AssemblyLoader
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AssemblyLoader));
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
                if (!(fileName.ToLower() == "0harmony") && !(fileName.ToLower() == "acsmodloader") && !(fileName.ToLower() == "mono.cecil"))
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
                        var loaded = Assembly.LoadFile(asm.Location);
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