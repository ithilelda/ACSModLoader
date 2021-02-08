using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using NLog;

namespace ModLoader
{
    public static class ModLoader
    {
        public static readonly string rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static List<Assembly> Mods { get; } = new List<Assembly>();
        public static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        static ModLoader()
        {
            var config = new NLog.Config.LoggingConfiguration();
            var logfile = new NLog.Targets.FileTarget("logfile") {
                FileName = Path.Combine(rootPath, "logs/log.log"),
                ArchiveOldFileOnStartup = true,
                MaxArchiveFiles = 5,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                ArchiveFileName = Path.Combine(rootPath, "logs/{#}.log"),
                ArchiveDateFormat = "yyyy-MM-dd HH_mm_ss",
                KeepFileOpen = true,
                OpenFileCacheTimeout = 30,
            };
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            LogManager.Configuration = config;

            AppDomain.CurrentDomain.AssemblyResolve += RootResolveHandler;
        }

        // single time method that loads the assemblies and applies harmony patches.
        // also calls the oninit events that ought to be run only once each game.
        public static void Main()
        {
            Logger.Info($"ModLoader successfully initiated. Loading mods before game starts...");
            var modDirs = Directory.GetDirectories(rootPath);
            Logger.Info($"{modDirs.Length} mods found.");
            foreach (var dir in modDirs)
            {
                var modName = Path.GetFileName(dir);
                var fullName = Path.Combine(dir, $"{modName}.dll");
                var rasm = Util.PreLoadAssembly(fullName);
                var asm = Util.LoadAssembly(rasm);
                if (asm != null)
                {
                    Util.Call(asm, $"{modName}.{modName}");
                    Mods.Add(asm);
                }
            }
        }

        static Assembly RootResolveHandler(object source, ResolveEventArgs e)
        {
            Logger.Debug("Resolving Dependency: {0}", e.Name);
            var asmInfo = new AssemblyName(e.Name);
            var file = Path.Combine(rootPath, asmInfo.Name + ".dll");
            Logger.Debug("Trying to find this assembly in {0}", file);
            var asm =  Assembly.LoadFile(file);
            Logger.Debug("the asm is found? {0}", asm != null);
            return asm;
        }
    }
}
