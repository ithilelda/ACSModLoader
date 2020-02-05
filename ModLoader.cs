using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Harmony;
using JetBrains.Annotations;
using UnityEngine;

namespace ModLoader
{
	// Token: 0x02000002 RID: 2
	public class ModLoader
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public void Init()
		{
			KLog.Log(0, "ModLoader loaded!", new object[0]);
			DirectoryInfo modsDirectory = ModLoader.GetModsDirectory();
			if (!modsDirectory.Exists)
			{
				Directory.CreateDirectory(modsDirectory.FullName);
			}
			string[] files = Directory.GetFiles(modsDirectory.FullName, "*.dll", SearchOption.AllDirectories);
			List<FileInfo> list = new List<FileInfo>();
			foreach (string fileName in files)
			{
				list.Add(new FileInfo(fileName));
			}
			try
			{
				ModLoader.ApplyHarmonyPatches(ModLoader.PreloadModAssemblies(list));
				KLog.Log(0, "All mods successfully loaded!", new object[0]);
			}
			catch (Exception ex)
			{
				KLog.Log(3, ex.Message, new object[0]);
			}
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002108 File Offset: 0x00000308
		private static DirectoryInfo GetModsDirectory()
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
			KLog.Log(0, "Data dir: " + directoryInfo.FullName, new object[0]);
			KLog.Log(0, "RuntimePlatform: " + Application.platform, new object[0]);
			DirectoryInfo parent = directoryInfo.Parent;
			KLog.Log(0, "Path to mods is: " + Path.Combine((parent != null) ? parent.FullName : null, "DLL_Mods"), new object[0]);
			return new DirectoryInfo(Path.Combine((parent != null) ? parent.FullName : null, "DLL_Mods"));
		}

		// Token: 0x06000003 RID: 3 RVA: 0x000021AC File Offset: 0x000003AC
		private static List<Assembly> ApplyHarmonyPatches([NotNull] List<Assembly> modAssemblies)
		{
			KLog.Log(0, "Applying Harmony patches", new object[0]);
			List<string> list = new List<string>();
			List<Assembly> list2 = new List<Assembly>();
			foreach (Assembly assembly in modAssemblies)
			{
				if (assembly != null)
				{
					try
					{
						KLog.Log(0, "Loading: " + assembly.FullName, new object[0]);
						Assembly assembly2 = Assembly.LoadFrom(assembly.Location);
						HarmonyInstance harmonyInstance = HarmonyInstance.Create(assembly2.FullName);
						if (harmonyInstance != null)
						{
							harmonyInstance.PatchAll(assembly2);
						}
						list2.Add(assembly2);
					}
					catch (Exception ex)
					{
						list.Add(assembly.GetName().ToString());
						KLog.Log(3, "Patching mod " + assembly.GetName() + " failed!", new object[0]);
						KLog.Log(3, ex.Message, new object[0]);
					}
				}
			}
			if (list.Count > 0)
			{
				string text = "\nThe following mods could not be patched:\n" + string.Join("\n\t", list.ToArray());
				KLog.Log(3, text, new object[0]);
			}
			return list2;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000022F4 File Offset: 0x000004F4
		private static List<Assembly> PreloadModAssemblies(List<FileInfo> assemblyFiles)
		{
			KLog.Log(0, "Loading mod assemblies", new object[0]);
			List<Assembly> list = new List<Assembly>();
			List<string> list2 = new List<string>();
			if (assemblyFiles != null)
			{
				foreach (FileInfo fileInfo in assemblyFiles)
				{
					if (!(((fileInfo != null) ? fileInfo.Extension : null) != ".dll") && !(fileInfo.Name.ToLower() == "0harmony") && !(fileInfo.Name.ToLower() == "modloader"))
					{
						try
						{
							Assembly assembly = Assembly.ReflectionOnlyLoadFrom(fileInfo.FullName);
							if (list.Contains(assembly))
							{
								KLog.Log(0, "Skipping duplicate mod " + assembly.FullName, new object[0]);
							}
							else
							{
								KLog.Log(0, "Preloading " + assembly.FullName, new object[0]);
								list.Add(assembly);
							}
						}
						catch (Exception ex)
						{
							list2.Add(fileInfo.Name);
							KLog.Log(3, "Preloading mod " + fileInfo.Name + " failed!", new object[0]);
							KLog.Log(3, ex.Message, new object[0]);
						}
						if (list2.Count > 0)
						{
							string text = "\nThe following mods could not be pre-loaded:\n" + string.Join("\n\t", list2.ToArray());
							KLog.Log(3, text, new object[0]);
						}
					}
				}
			}
			return list;
		}

		// Token: 0x04000001 RID: 1
		private const string MOD_DIR_NAME = "DLL_Mods";
	}
}
