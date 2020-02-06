using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
			var list = new List<FileInfo>();
			foreach (var fileName in files)
			{
				list.Add(new FileInfo(fileName));
			}
			try
			{
				ModLoader.ApplyHarmonyPatches(ModLoader.PreloadModAssemblies(list));
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

		private static List<Assembly> ApplyHarmonyPatches(List<Assembly> modAssemblies)
		{
            Log.WriteLine("Applying Harmony patches");
			var list = new List<string>();
			var list2 = new List<Assembly>();
			foreach (var assembly in modAssemblies)
			{
				if (assembly != null)
				{
					try
					{
                        Log.WriteLine("Loading: " + assembly.FullName);
						var assembly2 = Assembly.LoadFrom(assembly.Location);
						var harmonyInstance = HarmonyInstance.Create(assembly2.FullName);
						if (harmonyInstance != null)
						{
							harmonyInstance.PatchAll(assembly2);
						}
						list2.Add(assembly2);
					}
					catch (Exception ex)
					{
						list.Add(assembly.GetName().ToString());
                        Log.WriteLine("Patching mod " + assembly.GetName() + " failed!");
                        Log.WriteLine(ex.Message);
					}
				}
			}
			if (list.Count > 0)
			{
				var text = "\nThe following mods could not be patched:\n" + string.Join("\n\t", list.ToArray());
                Log.WriteLine(text);
			}
			return list2;
		}

		private static List<Assembly> PreloadModAssemblies(List<FileInfo> assemblyFiles)
		{
            Log.WriteLine("Loading mod assemblies");
            var list = new List<Assembly>();
			var list2 = new List<string>();
			if (assemblyFiles != null)
			{
				foreach (var fileInfo in assemblyFiles)
				{
					if (!(((fileInfo != null) ? fileInfo.Extension : null) != ".dll") && !(fileInfo.Name.ToLower() == "0harmony") && !(fileInfo.Name.ToLower() == "modloader"))
					{
						try
						{
							var assembly = Assembly.ReflectionOnlyLoadFrom(fileInfo.FullName);
							if (list.Contains(assembly))
							{
                                Log.WriteLine("Skipping duplicate mod " + assembly.FullName);
							}
							else
							{
                                Log.WriteLine("Preloading " + assembly.FullName);
                                list.Add(assembly);
							}
						}
						catch (Exception ex)
						{
							list2.Add(fileInfo.Name);
                            Log.WriteLine("Preloading mod " + fileInfo.Name + " failed!");
                            Log.WriteLine(ex.Message);
						}
						if (list2.Count > 0)
						{
							var text = "\nThe following mods could not be pre-loaded:\n" + string.Join("\n\t", list2.ToArray());
                            Log.WriteLine(text);
						}
					}
				}
			}
			return list;
		}
	}
}
