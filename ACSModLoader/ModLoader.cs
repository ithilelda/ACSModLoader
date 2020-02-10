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
        public static List<Assembly> LoadedAssemblies;
        private static readonly ILog Log = LogManager.GetLogger(typeof(ModLoader));
        private static readonly string MOD_DIR_NAME = "Mods";
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
                // patch what we need to patch.
                PatchAssemblyCSharp();
                // load all correct assemblies and separate them into the lists.
                var modPath = Path.Combine(RootPath, MOD_DIR_NAME);
                LoadedAssemblies = LoadAll(modPath);
                // patch the preload patchers.
				PreLoaderPatcher.Enter();
			}
            catch (Exception ex)
            {
                Log.Debug(ex.Message);
            }
		}
        private static void PatchAssemblyCSharp()
        {
            // house keeping the assembly-csharp.dll file.
            var dll_file = Path.Combine(ManagedPath, "Assembly-CSharp.dll");
            if(!File.Exists(dll_file)) throw new Exception("Assembly-CSharp.dll cannot be found! Check your folder!");
            var bck_file = Path.ChangeExtension(dll_file, "bck");
            if(!File.Exists(bck_file)) File.Copy(dll_file, bck_file);
            var tmp_file = Path.ChangeExtension(dll_file, "tmp");
            File.Delete(tmp_file);
            File.Copy(dll_file, tmp_file);
            
            try
            {
                // house keeping the ACSModLoader.BootStrap.dll file.
                var source_bs = Path.Combine(ModLoaderPath, "ACSModLoader.BootStrap.dll");
                if(!File.Exists(source_bs)) throw new Exception("the bootstrapper dll does not exist! Check your installation!");
                var target_bs = Path.Combine(ManagedPath, "ACSModLoader.BootStrap.dll");
                File.Delete(target_bs);
                File.Copy(source_bs, target_bs);

                // reading the assemblies.
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(ModLoaderPath);
                resolver.AddSearchDirectory(ManagedPath);
                var readerParam = new ReaderParameters { AssemblyResolver = resolver };
                var assembly_csharp = AssemblyDefinition.ReadAssembly(tmp_file, readerParam);
                var bootstrapper = AssemblyDefinition.ReadAssembly(target_bs, readerParam);

                // get the patcher in bootstrapper.
                var bootstrapType = bootstrapper.MainModule.Types.First(t => t.Name == "BootStrap");
                var entryPoint = bootstrapType.Methods.First(m => m.Name == "Enter");
                var callHarmony = assembly_csharp.MainModule.ImportReference(entryPoint);
                // get the target method in assembly-csharp.
                var mainManagerType = assembly_csharp.MainModule.Types.First(t => t.Name == "LuaMgr");
                var initMethod = mainManagerType.Methods.First(m => m.Name == "Init");
                if (initMethod == null) throw new Exception("Init method of LuaMgr not found!");
                var processor = initMethod.Body.GetILProcessor();
                var first = processor.Body.Instructions.First();
                processor.InsertBefore(first, processor.Create(OpCodes.Call, callHarmony));
                assembly_csharp.Write(dll_file);
                assembly_csharp.Dispose();
                bootstrapper.Dispose();
            }
            catch(Exception e)
            {
                throw e; // propagate up.
            }
            finally
            {
                File.Delete(tmp_file); // this is to ensure that the tmp file is always deleted.
            }
            
        }
		private static List<Assembly> LoadAll(string path)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			var files = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
            var asms = AssemblyLoader.PreLoadAssemblies(files);
            return AssemblyLoader.LoadAssemblies(asms);
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
