using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Serilog;
using HarmonyLib;

namespace ModLoader
{
    public static class ModLoader
    {
        static List<Assembly> assemblies = new List<Assembly>();
        readonly static string rootPath = Path.GetDirectoryName(Environment.GetEnvironmentVariable("DOORSTOP_INVOKE_DLL_PATH"));


        // single time method that loads the assemblies and applies harmony patches.
        // also calls the oninit events that ought to be run only once each game.
        public static void Init()
        {
            //var logFile = Path.Combine(rootPath, "ModLoader.log");
            Log.Debug("[ModLoader] patching the game with ModLoader patches...");
            Log.Debug("[ModLoader] loading assemblies...");
            var modDirs = Directory.GetDirectories(rootPath);
            foreach (var dir in modDirs)
            {
                var modName = Path.GetFileName(dir);
                var modFiles = Directory.GetFiles(dir, $"{modName}.dll", SearchOption.AllDirectories);
                try
                {
                    var harmony_name = $"{modName}";
                    foreach (var file in modFiles)
                    {
                        var rasm = Utilities.Util.PreLoadAssembly(file);
                        var asm = Utilities.Util.LoadAssembly(rasm);
                        if (asm != null)
                        {
                            assemblies.Add(asm);
                        }
                        Utilities.Util.Call(asm, "OnInit");
                        Utilities.Util.ApplyHarmony(asm, harmony_name);
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"[ModLoader] the mod {modName} cannot be loaded!");
                    Log.Debug($"[ModLoader] the error is: {ex.Message}");
                }
            }
        }

    }
}
