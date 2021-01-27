using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Serilog;

namespace ModLoader
{
    public static class ModLoader
    {
        static List<Assembly> assemblies = new List<Assembly>();
        readonly static string rootPath = Path.GetDirectoryName(Environment.GetEnvironmentVariable("DOORSTOP_INVOKE_DLL_PATH"));

        public static ILogger Logger = Log.Logger.ForContext("ReportId", "ModLoader");

        // single time method that loads the assemblies and applies harmony patches.
        // also calls the oninit events that ought to be run only once each game.
        public static void Init()
        {
            //var logFile = Path.Combine(rootPath, "ModLoader.log");
            Logger.Debug("patching the game with ModLoader patches...");
            Logger.Debug("loading assemblies...");
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
                    Logger.Debug($"the mod {modName} cannot be loaded!");
                    Logger.Debug($"the error is: {ex.Message}");
                }
            }
        }

    }
}
