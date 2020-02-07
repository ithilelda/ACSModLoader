using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Harmony;

namespace ModLoader
{
	public static class ModLoader
	{
		private static string RootPath;
        private static readonly string MOD_DIR_NAME = "Mods";
		private static StreamWriter Log;
		public static void Main()
		{
			new Thread(() =>
			{
				Thread.Sleep(5000);
				Run();
			}).Start();
		}
		public static void Run()
		{
            Log = new StreamWriter("ModLoader.log");
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
			RootPath = Directory.GetCurrentDirectory();
            Log.WriteLine($"it worked! RootPath: {RootPath}");
			var modPath = Path.Combine(RootPath, MOD_DIR_NAME);
			if (!Directory.Exists(modPath))
			{
				Directory.CreateDirectory(modPath);
			}
			var files = Directory.GetFiles(modPath, "*.dll", SearchOption.AllDirectories);
			try
			{
				ModLoader.ApplyHarmonyPatches(ModLoader.PreloadModAssemblies(files));
                Log.WriteLine("All mods successfully loaded!");
			}
			catch (Exception ex)
			{
                Log.WriteLine(ex.Message);
			}
			finally
			{
				Log.Close();
			}
		}

		// this handler add the game root path to the resolving path.
		private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs args)
		{
            var t = args.Name.Split(',');
			var fileName = t[0].Trim() + ".dll";
			Log.WriteLine($"current resolving assembly is: {fileName}");
			var rootFile = Path.Combine(RootPath, fileName);
			if(File.Exists(rootFile))
			{
				return Assembly.LoadFile(rootFile);
			}
			else return null;
		}

        private static List<Assembly> PreloadModAssemblies(string[] files)
        {
            Log.WriteLine("Pre-Loading mod assemblies");
            var result = new List<Assembly>();
            var failed = new List<string>();
			for(int i = 0; i < files.Length; i++)
			{
				var fileName = files[i];
				var fileInfo = new FileInfo(fileName);
				// we exclude errogenous libraries that may be a problem.
				if (!(fileName.ToLower() == "0harmony") && !(fileName.ToLower() == "acsmodloader"))
				{
					try
					{
						var assembly = Assembly.ReflectionOnlyLoadFrom(fileName);
						if (result.Contains(assembly))
						{
							Log.WriteLine("Skipping duplicate mod " + fileName);
						}
						else
						{
							Log.WriteLine("Preloading " + fileName);
							result.Add(assembly);
						}
					}
					catch (Exception ex)
					{
						failed.Add(fileInfo.Name);
						Log.WriteLine("Preloading mod " + fileInfo.Name + " failed!");
						Log.WriteLine(ex.Message);
					}
				}
			}
            if (failed.Count > 0)
            {
                var text = "\nThe following mods could not be pre-loaded:\n" + string.Join("\n\t", failed.ToArray());
                Log.WriteLine(text);
            }
            Log.WriteLine("Sorting Dependencies");
			result.Sort(new AssemblyComparer());
            return result;
        }
		private static List<Assembly> ApplyHarmonyPatches(List<Assembly> modAssemblies)
		{
            Log.WriteLine("Applying Harmony patches");
            var result = new List<Assembly>();
			var failed = new List<string>();
			foreach (var assembly in modAssemblies)
			{
				if (assembly != null)
				{
					try
					{
                        Log.WriteLine("Loading: " + assembly.FullName);
						var patch = Assembly.LoadFile(assembly.Location);
						var harmonyInstance = HarmonyInstance.Create(patch.FullName);
						if (harmonyInstance != null)
						{
							harmonyInstance.PatchAll(patch);
						}
						result.Add(patch);
					}
					catch (Exception ex)
					{
						failed.Add(assembly.GetName().ToString());
                        Log.WriteLine("Patching mod " + assembly.GetName() + " failed!");
                        Log.WriteLine(ex.Message);
					}
				}
			}
			if (failed.Count > 0)
			{
				var text = "\nThe following mods could not be patched:\n" + string.Join("\n\t", failed.ToArray());
                Log.WriteLine(text);
			}
			return result;
		}
	}
}
