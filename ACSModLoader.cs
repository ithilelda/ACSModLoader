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
        private static readonly string MOD_DIR_NAME = "DLL_Mods";
		public static void Main(string[] args)
		{
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
			RootPath = Path.GetDirectoryName(args[0]);
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
				//KLog.Log(0, "All mods successfully loaded!", new object[0]);
			}
			catch (Exception)
			{
				//KLog.Log(3, ex.Message, new object[0]);
			}
		}

		private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs args)
		{
			var file = Path.Combine(RootPath, args.Name);
			if(File.Exists(file))
			{
				return Assembly.LoadFile(file);
			}
			else return null;
		}

		private static List<Assembly> ApplyHarmonyPatches(List<Assembly> modAssemblies)
		{
			//KLog.Log(0, "Applying Harmony patches", new object[0]);
			var list = new List<string>();
			var list2 = new List<Assembly>();
			foreach (var assembly in modAssemblies)
			{
				if (assembly != null)
				{
					try
					{
						//KLog.Log(0, "Loading: " + assembly.FullName, new object[0]);
						var assembly2 = Assembly.LoadFrom(assembly.Location);
						var harmonyInstance = HarmonyInstance.Create(assembly2.FullName);
						if (harmonyInstance != null)
						{
							harmonyInstance.PatchAll(assembly2);
						}
						list2.Add(assembly2);
					}
					catch (Exception)
					{
						list.Add(assembly.GetName().ToString());
						//KLog.Log(3, "Patching mod " + assembly.GetName() + " failed!", new object[0]);
						//KLog.Log(3, ex.Message, new object[0]);
					}
				}
			}
			if (list.Count > 0)
			{
				string text = "\nThe following mods could not be patched:\n" + string.Join("\n\t", list.ToArray());
				//KLog.Log(3, text, new object[0]);
			}
			return list2;
		}

		private static List<Assembly> PreloadModAssemblies(List<FileInfo> assemblyFiles)
		{
			//KLog.Log(0, "Loading mod assemblies", new object[0]);
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
								//KLog.Log(0, "Skipping duplicate mod " + assembly.FullName, new object[0]);
							}
							else
							{
								//KLog.Log(0, "Preloading " + assembly.FullName, new object[0]);
								list.Add(assembly);
							}
						}
						catch (Exception)
						{
							list2.Add(fileInfo.Name);
							//KLog.Log(3, "Preloading mod " + fileInfo.Name + " failed!", new object[0]);
							//KLog.Log(3, ex.Message, new object[0]);
						}
						if (list2.Count > 0)
						{
							var text = "\nThe following mods could not be pre-loaded:\n" + string.Join("\n\t", list2.ToArray());
							//KLog.Log(3, text, new object[0]);
						}
					}
				}
			}
			return list;
		}
	}
}
