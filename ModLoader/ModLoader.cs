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
        public static string ModPath { get; private set; }
		public static string ManagedPath { get; private set; }
        public static string BootstrapperDll { get; private set; }
        public static string BootstrapperTarget { get; private set; }
        public static string BootstrapperEntry { get; private set; }

        public static List<string> DependecyAssemblies;
        public static List<string> PreLoaderAssemblies;
        public static List<string> HarmonyAssemblies;

        private static readonly string DEFAULT_MOD_DIR_NAME = "Mods";
        private static readonly string DEFAULT_BOOTSTRAP_DLL = "Bootstrapper.dll";
        private static readonly string DEFAULT_BOOTSTRAP_TARGET = "UnityEngine.CoreModule";
        private static readonly string DEFAULT_BOOTSTRAP_ENTRY = "UnityEngine.Application";
        private static readonly ILog Log = LogManager.GetLogger(typeof(ModLoader));
        static ModLoader()
        {
            // read configuration of logger.
            var curAsm = typeof(ModLoader).Assembly;
            var loggerConfig = curAsm.GetManifestResourceNames().Single(t => t.EndsWith("logger.xml"));
            XmlConfigurator.Configure(curAsm.GetManifestResourceStream(loggerConfig));
            // get paths.
            var currentApp = Process.GetCurrentProcess().MainModule.FileName;
            var rootPath = Path.GetDirectoryName(currentApp);
            var managedExt = $@"{Path.GetFileNameWithoutExtension(currentApp)}_Data\Managed";
            ManagedPath = Path.Combine(rootPath, managedExt);
            ModLoaderPath = Path.Combine(rootPath, "ModLoader");
            // read configuration.
            var configs = Configuration.ReadAll(ModLoaderPath, "ModLoader");
            if(!configs.TryGetValue("MOD_DIR_NAME", out var dir_name))
            {
                dir_name = DEFAULT_MOD_DIR_NAME;
            }
            if (!configs.TryGetValue("BOOTSTRAP_DLL", out var bootstrap_dll))
            {
                bootstrap_dll = DEFAULT_BOOTSTRAP_DLL;
            }
            if (!configs.TryGetValue("BOOTSTRAP_TARGET", out var bootstrap_target))
            {
                bootstrap_target = DEFAULT_BOOTSTRAP_TARGET;
            }
            if (!configs.TryGetValue("BOOTSTRAP_ENTRY", out var bootstrap_entry))
            {
                bootstrap_entry = DEFAULT_BOOTSTRAP_ENTRY;
            }
            // set the needed static properties.
            ModPath = Path.Combine(rootPath, dir_name);
            BootstrapperDll = bootstrap_dll;
            BootstrapperTarget = bootstrap_target;
            BootstrapperEntry = bootstrap_entry;
            // write configuration.
            configs["MOD_DIR_NAME"] = dir_name;
            configs["BOOTSTRAP_DLL"] = bootstrap_dll;
            configs["BOOTSTRAP_TARGET"] = bootstrap_target;
            configs["BOOTSTRAP_ENTRY"] = bootstrap_entry;
            Configuration.WriteAll(configs, ModLoaderPath, "ModLoader");
        }
        // Entry Point.
        // The particular signature is made to be compatible with most injectors, i.e. UnityDoorStop, UnityAssemblyInjector, etc.
		public static void Main()
		{
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
            Log.Info("Welcome to ModLoader!");
            Log.Debug($"ModLoaderPath: {ModLoaderPath}");
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
                var source_bs = Path.Combine(ModLoaderPath, BootstrapperDll);
                var target_bs = Path.Combine(ManagedPath, BootstrapperDll);
                File.Delete(target_bs);
                File.Copy(source_bs, target_bs);
                // create the mod path if not existed.
                if (!Directory.Exists(ModPath))
                {
                    Directory.CreateDirectory(ModPath);
                }
                // get all dll files.
                var files = Directory.GetFiles(ModPath, "*.dll", SearchOption.AllDirectories);
                // sort them into three different lists.
                AssemblyLoader.SortAssemblies(files, out DependecyAssemblies, out PreLoaderAssemblies, out HarmonyAssemblies);
                // add our patch to the patcher list.
                var patchers = new List<PreLoaderPatch>();
                var patcher = new PreLoaderPatch{name = "ModLoader.Bootstrapper", target = BootstrapperTarget, patch = PatchAssemblyCSharpForHarmony };
                patchers.Add(patcher);
                // add patchers from sorted patcher files.
                patchers.AddRange(PreLoaderPatcher.AddPatchesFromFiles(PreLoaderAssemblies));
                // patch the preload patchers.
				PreLoaderPatcher.Apply(patchers);
                // now we load the dependecies.
                AssemblyLoader.LoadAssemblies(AssemblyLoader.PreLoadAssemblies(DependecyAssemblies));
			}
            catch (Exception ex)
            {
                Log.Debug(ex.Message);
            }
		}
        public static void PatchAssemblyCSharpForHarmony(AssemblyDefinition asm)
        {
            var target_bs = Path.Combine(ManagedPath, BootstrapperDll);
            using (var bootStrapper = AssemblyDefinition.ReadAssembly(target_bs))
            {
                // I want an exception thrown if the entry type is not found.
                var entryType = asm.MainModule.Types.First(t => t.FullName == BootstrapperEntry);
                // then we need to inject code into the static constructor.
                // we will create an cctor for it if it is not there, so we don't throw exceptions.
                var cctor = entryType.Methods.FirstOrDefault(m => m.Name == ".cctor");
                if(cctor == null)
                {
                    cctor = new MethodDefinition(".cctor",
                        MethodAttributes.Static
                        | MethodAttributes.Private
                        | MethodAttributes.HideBySig
                        | MethodAttributes.SpecialName
                        | MethodAttributes.RTSpecialName,
                        asm.MainModule.ImportReference(typeof(void)));
                    entryType.Methods.Add(cctor);
                    cctor.Body.GetILProcessor().Emit(OpCodes.Ret);
                }
                var processor = cctor.Body.GetILProcessor();
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
