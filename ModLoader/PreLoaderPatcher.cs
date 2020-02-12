using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Mono.Cecil;
using log4net;


namespace ModLoader
{
    public delegate void PatchEntry(AssemblyDefinition asm);
    public class PreLoaderPatch
    {
        public string name;
        public string target;
        public PatchEntry patch;
    }

    public static class PreLoaderPatcher
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PreLoaderPatcher));

        public static List<PreLoaderPatch> AddPatchesFromFiles(IEnumerable<string> files)
        {
            var patchers = new List<PreLoaderPatch>();
            Log.Info("Add preloader patches");
            foreach (var file in files)
            {
                try
                {
                    var asm = Assembly.LoadFrom(file);
                    var types = asm.GetTypes().Where(t => Attribute.GetCustomAttribute(t, typeof(PreLoaderPatchAttribute)) != null);
                    foreach (var type in types)
                    {
                        try
                        {
                            var attr = (PreLoaderPatchAttribute) Attribute.GetCustomAttribute(type, typeof(PreLoaderPatchAttribute));
                            // the entry method signature is public static void Patch(AssemblyDefinition).
                            var entry = (PatchEntry) Delegate.CreateDelegate(typeof(PatchEntry), type, "Patch");
                            Log.Debug($"Found a patcher '{type.AssemblyQualifiedName}' targeting {attr.Target}.dll");
                            patchers.Add(new PreLoaderPatch { name = type.AssemblyQualifiedName, target = attr.Target, patch = entry });
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"adding patcher: {type.AssemblyQualifiedName} failed!");
                            Log.Error(ex.Message);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }
            return patchers;
        }
        public static void Apply(IEnumerable<PreLoaderPatch> patchers)
        {
            Log.Info("Applying preloader patches");
            var grouped = patchers.GroupBy(p => p.target);
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(ModLoader.ModLoaderPath);
            var readerParam = new ReaderParameters { AssemblyResolver = resolver };
            foreach (var group in grouped)
            {
                var targetName = group.Key + ".dll";
                var targetFile = Path.Combine(ModLoader.ManagedPath, targetName);
                if(!File.Exists(targetFile)) continue;
                Log.Debug($"applying preloader patch to: {targetName}");
                var backupFile = Path.ChangeExtension(targetFile, "bck");
                var tmpFile = Path.ChangeExtension(targetFile, "tmp");
                // if there is no backup file, we have the original dll. We need to copy it to the backup for later restoration.
                if (!File.Exists(backupFile))
                {
                    File.Copy(targetFile, backupFile);
                }
                // create the tmp file and read it for patching.
                File.Delete(tmpFile);
                File.Copy(targetFile, tmpFile);
                using (var asm = AssemblyDefinition.ReadAssembly(tmpFile))
                {
                    foreach (var patcher in group)
                    {
                        try
                        {
                            Log.Debug($"applying preloader patch: {patcher.name}");
                            patcher.patch(asm);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"The patcher {patcher.name} failed to patch!");
                            Log.Error(ex.Message);
                        }
                    }
                    asm.Write(targetFile);
                    asm.Dispose();
                }
                File.Delete(tmpFile);
            }
        }
    }
}