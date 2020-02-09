using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Mono.Cecil;

namespace ModLoader
{
	public static class ModLoader
	{
		public static string RootPath { get; private set; }
		public static string ManagedPath { get; private set; }
        private static readonly string MOD_DIR_NAME = "Mods";
        private static readonly string MANAGED_DIR_NAME = "Amazing Cultivation Simulator_Data\\Managed";
		private static StreamWriter log;
		public static void Main()
		{
			log = new StreamWriter("ModLoader.log");
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += HandleRelefectionOnlyResolve;
            RootPath = Directory.GetCurrentDirectory();
            ManagedPath = Path.Combine(RootPath, MANAGED_DIR_NAME);
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
				// load and patch prepatchers.
				var asms = LoadAll();
				var patcher_suc = AssemblyLoader.ApplyPreLoaderPatches(asms);
                if (patcher_suc)
                {
                    Log("All preloader patchers successfully loaded!");
                }
                else
                {
                    Log("Some patchers cannot be patched! Please check previous lines for error report!");
                }
                new Thread(() =>
				{
					Thread.Sleep(5000);
					var harmony_suc = AssemblyLoader.ApplyHarmonyPatches(asms);
					if (harmony_suc)
					{
						Log("All Harmony mods successfully loaded!");
					}
					else
					{
						Log("Some mods cannot be patched! Please check previous lines for error report!");
					}
				}).Start();
			}
            catch (Exception ex)
            {
                Log(ex.Message);
            }
            finally
            {
                log.Close();
            }
		}
        public static void Log(string line)
        {
            log.WriteLine("[ModLoader]" + line);
        }
        
		private static List<Assembly> LoadAll()
		{
            Log($"Welcome to ModLoader! RootPath: {RootPath}");
			var modPath = Path.Combine(RootPath, MOD_DIR_NAME);
			if (!Directory.Exists(modPath))
			{
				Directory.CreateDirectory(modPath);
			}
			var files = Directory.GetFiles(modPath, "*.dll", SearchOption.AllDirectories);
            var asms = AssemblyLoader.PreloadAssemblies(files);
            asms = AssemblyLoader.SortDependencies(asms);
            asms = AssemblyLoader.LoadAssemblies(asms);
			return asms;
		}
        private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs arg)
        {
            var t = arg.Name.Split(',');
            var fileName = t[0].Trim() + ".dll";
            Log($"the current resolving assembly is: {fileName}");
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
            Log($"the current resolving reflection-only assembly is: {fileName}");
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
