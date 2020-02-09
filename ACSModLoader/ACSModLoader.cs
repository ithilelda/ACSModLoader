using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
		public static string ManagedPath { get; private set; }
        public static readonly ILog Log = LogManager.GetLogger(typeof(ModLoader));
        private static readonly string MOD_DIR_NAME = "Mods";
        private static readonly string MANAGED_DIR_NAME = "Amazing Cultivation Simulator_Data\\Managed";
        private static List<Assembly> LoadedAssemblies;
		public static void Main()
		{
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += HandleRelefectionOnlyResolve;
            RootPath = Directory.GetCurrentDirectory();
            ManagedPath = Path.Combine(RootPath, MANAGED_DIR_NAME);
            var curAsm = typeof(ModLoader).Assembly;
            var loggerConfig = curAsm.GetManifestResourceNames().Single(t => t.EndsWith("logger.xml"));
            XmlConfigurator.Configure(curAsm.GetManifestResourceStream(loggerConfig));
            Log.Info("Welcome to ModLoader!");
            Log.Debug($"RootPath: {RootPath}");
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
                // patch what we need to patch.
                PatchAssemblyCSharp();
                // load all correct assemblies and separate them into the lists.
                LoadedAssemblies = LoadAll();
                // patch the preload patchers.
				var patcher_suc = AssemblyLoader.ApplyPreLoaderPatches(LoadedAssemblies);
                if (patcher_suc)
                {
                    Log.Debug("All preloader patchers successfully loaded!");
                }
                else
                {
                    Log.Debug("Some patchers cannot be patched! Please check previous lines for error report!");
                }
			}
            catch (Exception ex)
            {
                Log.Debug(ex.Message);
            }
		}
        public static void ApplyHarmony()
        {
            var harmony_suc = AssemblyLoader.ApplyHarmonyPatches(LoadedAssemblies);
            if (harmony_suc)
            {
                Log.Debug("All Harmony mods successfully loaded!");
            }
            else
            {
                Log.Debug("Some mods cannot be patched! Please check previous lines for error report!");
            }
        }
        private static void PatchAssemblyCSharp()
        {
            var dll_file = Path.Combine(ManagedPath, "Assembly-CSharp.dll");
            if(!File.Exists(dll_file)) throw new Exception("Assembly-CSharp.dll cannot be found! Check your folder!");
            var tmp_file = Path.ChangeExtension(dll_file, "tmp");
            File.Delete(tmp_file);
            File.Copy(dll_file, tmp_file);
            var assembly_csharp = AssemblyDefinition.ReadAssembly(tmp_file);
            var mainManagerType = assembly_csharp.MainModule.Types.First(t => t.Name == "MainManager");
            var initMethod = mainManagerType.Methods.First(m => m.Name == "Init");
            if (initMethod == null) throw new Exception("init method of mainmanager not found!");
            var processor = initMethod.Body.GetILProcessor();
            var first = processor.Body.Instructions.First();
            var callHarmony = assembly_csharp.MainModule.ImportReference(typeof(ModLoader).GetMethod("ApplyHarmony"));
            processor.InsertBefore(first, processor.Create(OpCodes.Call, callHarmony));
            assembly_csharp.Write(dll_file);
            assembly_csharp.Dispose();
            File.Delete(tmp_file);
        }
		private static List<Assembly> LoadAll()
		{
			var modPath = Path.Combine(RootPath, MOD_DIR_NAME);
			if (!Directory.Exists(modPath))
			{
				Directory.CreateDirectory(modPath);
			}
			var files = Directory.GetFiles(modPath, "*.dll", SearchOption.AllDirectories);
            var asms = AssemblyLoader.PreLoadAssemblies(files);
            return AssemblyLoader.LoadAssemblies(asms);
		}
        private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs arg)
        {
            var t = arg.Name.Split(',');
            var fileName = t[0].Trim() + ".dll";
            Log.Debug($"the current resolving assembly is: {fileName}");
            var rootFile = Path.Combine(RootPath, fileName);
            if (File.Exists(rootFile))
            {
                return Assembly.LoadFile(rootFile);
            }
            else return null;
        }
		private static Assembly HandleRelefectionOnlyResolve(object sender, ResolveEventArgs arg)
		{
            var t = arg.Name.Split(',');
            var fileName = t[0].Trim() + ".dll";
            Log.Debug($"the current resolving reflection-only assembly is: {fileName}");
            var modPath = Path.Combine(RootPath, MOD_DIR_NAME);
			var file = Path.Combine(modPath, fileName);
            if (File.Exists(file))
            {
                return Assembly.ReflectionOnlyLoadFrom(file);
            }
            else return null;
		}
		
	}
}
