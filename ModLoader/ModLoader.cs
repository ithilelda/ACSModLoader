using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using log4net;
using log4net.Config;
using Mono.Cecil;
using Mono.Cecil.Cil;


namespace ModLoader
{
	public static class ModLoader
	{
		public static string RootPath { get; private set; }
        public static string ModLoaderPath { get; private set; }
		public static string ManagedPath { get; private set; }
        public static List<string> Harmonies;
        private static readonly ILog Log = LogManager.GetLogger(typeof(ModLoader));
        private static readonly string MOD_DIR_NAME = "Mods";
        // Entry Point.
        // The particular signature is made to be compatible with most injectors, i.e. UnityDoorStop, UnityAssemblyInjector, etc.
		public static void Main()
		{
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
            var currentApp = Process.GetCurrentProcess().MainModule.FileName;
            RootPath = Path.GetDirectoryName(currentApp);
            var managedExt = $@"{Path.GetFileNameWithoutExtension(currentApp)}_Data\Managed";
            ManagedPath = Path.Combine(RootPath, managedExt);
            ModLoaderPath = Path.Combine(RootPath, "ModLoader");
            var curAsm = typeof(ModLoader).Assembly;
            var loggerConfig = curAsm.GetManifestResourceNames().Single(t => t.EndsWith("logger.xml"));
            XmlConfigurator.Configure(curAsm.GetManifestResourceStream(loggerConfig));
            Log.Info("Welcome to ModLoader!");
            Log.Debug($"RootPath: {RootPath}");
            Log.Debug($"ManagedPath: {ManagedPath}");
			try
			{
				// for every assembly that has a bck file, it is a previously patched file.
				// we remove it and restore the bck file.
                var bcks = Directory.GetFiles(ManagedPath, "*.bck", SearchOption.AllDirectories);
				foreach(var bck in bcks)
				{
					var asmFile = Path.ChangeExtension(bck, "dll");
					File.Delete(asmFile);
					File.Copy(bck, asmFile);
				}
                // create the mod path if not existed.
                var modPath = Path.Combine(RootPath, MOD_DIR_NAME);
                if (!Directory.Exists(modPath))
                {
                    Directory.CreateDirectory(modPath);
                }
                // get all dll files.
                var files = Directory.GetFiles(modPath, "*.dll", SearchOption.AllDirectories);
                // sort them into three different lists.
                AssemblyLoader.SortAssemblies(files, out var deps, out var preloaders, out Harmonies);
                // add our patch to the patcher list.
                var patchers = new List<PreLoaderPatch>();
                var patcher = new PreLoaderPatch{name = "ModLoader.Harmony", target = "Assembly-CSharp", patch = PatchAssemblyCSharpForHarmony };
                patchers.Add(patcher);
                // add patchers from sorted patcher files.
                patchers.AddRange(PreLoaderPatcher.AddPatchesFromFiles(preloaders));
                // patch the preload patchers.
				PreLoaderPatcher.Apply(patchers);
                // now we load the dependecies.
                AssemblyLoader.LoadAssemblies(AssemblyLoader.PreLoadAssemblies(deps));
			}
            catch (Exception ex)
            {
                Log.Debug(ex.Message);
            }
		}
        public static void PatchAssemblyCSharpForHarmony(ref AssemblyDefinition asm)
        {
            var entryPoint = typeof(HarmonyPatcher).GetMethod("Enter");
            var callHarmony = asm.MainModule.ImportReference(entryPoint);
            // get the target method in assembly-csharp.
            var mainManagerType = asm.MainModule.Types.First(t => t.Name == "MainManager");
            var ctor = mainManagerType.Methods.First(m => m.Name == ".ctor");
            if (ctor == null) throw new Exception("constructor of MainManager not found!");
            var processor = ctor.Body.GetILProcessor();
            var last = processor.Body.Instructions.Last();
            processor.InsertBefore(last, processor.Create(OpCodes.Call, callHarmony));
        }
        private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs arg)
        {
            var fileName = new AssemblyName(arg.Name).Name + ".dll";
            Log.Debug($"the current resolving assembly is: {fileName}");
            var file = Path.Combine(ModLoaderPath, fileName);
            if (File.Exists(file))
            {
                return Assembly.LoadFrom(file);
            }
            else return null;
        }
	}
}
