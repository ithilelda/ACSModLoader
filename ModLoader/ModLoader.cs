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
using MethodAttributes = Mono.Cecil.MethodAttributes;


namespace ModLoader
{
	public static class ModLoader
	{
        public static string ModLoaderPath { get; private set; }
		public static string ManagedPath { get; private set; }
        public static readonly string MOD_DIR_NAME = "Mods";
        private static readonly ILog Log = LogManager.GetLogger(typeof(ModLoader));
        static ModLoader()
        {
            var curAsm = typeof(ModLoader).Assembly;
            var loggerConfig = curAsm.GetManifestResourceNames().Single(t => t.EndsWith("logger.xml"));
            XmlConfigurator.Configure(curAsm.GetManifestResourceStream(loggerConfig));
        }
        // Entry Point.
        // The particular signature is made to be compatible with most injectors, i.e. UnityDoorStop, UnityAssemblyInjector, etc.
		public static void Main()
		{
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
            var currentApp = Process.GetCurrentProcess().MainModule.FileName;
            var rootPath = Path.GetDirectoryName(currentApp);
            var managedExt = $@"{Path.GetFileNameWithoutExtension(currentApp)}_Data\Managed";
            ManagedPath = Path.Combine(rootPath, managedExt);
            ModLoaderPath = Path.Combine(rootPath, "ModLoader");
            Log.Info("Welcome to ModLoader!");
            Log.Debug($"RootPath: {rootPath}");
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
                // then, we copy the Bootstrapper.dll file to the managed directory.
                var source_bs = Path.Combine(ModLoaderPath, "Bootstrapper.dll");
                var target_bs = Path.Combine(ManagedPath, "Bootstrapper.dll");
                File.Delete(target_bs);
                File.Copy(source_bs, target_bs);
                // create the mod path if not existed.
                var modPath = Path.Combine(rootPath, MOD_DIR_NAME);
                if (!Directory.Exists(modPath))
                {
                    Directory.CreateDirectory(modPath);
                }
                // get all dll files.
                var files = Directory.GetFiles(modPath, "*.dll", SearchOption.AllDirectories);
                // sort them into two different lists.
                AssemblyLoader.SortAssemblies(files, out var deps, out var preloaders);
                // add our patch to the patcher list.
                var patchers = new List<PreLoaderPatch>();
                var patcher = new PreLoaderPatch{name = "ModLoader.Bootstrapper", target = "Assembly-CSharp", patch = PatchAssemblyCSharpForHarmony };
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
        public static void PatchAssemblyCSharpForHarmony(AssemblyDefinition asm)
        {
            var target_bs = Path.Combine(ManagedPath, "Bootstrapper.dll");
            using (var bootStrapper = AssemblyDefinition.ReadAssembly(target_bs))
            {
                var mainManagerType = asm.MainModule.Types.First(t => t.Name == "MainManager");
                // then we need to inject code into the constructor.
                var ctor = mainManagerType.Methods.First(m => m.Name == ".ctor");
                var processor = ctor.Body.GetILProcessor();
                var last = processor.Body.Instructions.Last();
                var bsType = bootStrapper.MainModule.Types.Single(t => t.Name == "Bootstrapper");
                var initMethod = bsType.Methods.Single(m => m.Name == "Init");
                var startMethod = bsType.Methods.Single(m => m.Name == "Start");
                var callInit = asm.MainModule.ImportReference(initMethod);
                var callStart = asm.MainModule.ImportReference(startMethod);
                processor.InsertBefore(last, processor.Create(OpCodes.Call, callInit));
                processor.InsertBefore(last, processor.Create(OpCodes.Call, callStart));
            }
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
