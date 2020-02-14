using System;
using System.IO;
using System.Reflection;

namespace ModLoader
{
    public class ModLoaderLite
    {
        public string GamePath { get; private set; }
        public string[] ModPaths { get; private set; }
        private static bool patched;

        public ModLoaderLite()
        {
            AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve;
        }
        public void Init(string gamePath, string[] modPaths)
        {
            GamePath = gamePath ?? throw new ArgumentNullException("gamePath");
            ModPaths = modPaths ?? throw new ArgumentNullException("workShopPath");
        }
        public void Start()
        {
            if(!patched)
            {
                HarmonyLoaderLite.Enter(GamePath, ModPaths);
                patched = true;
            }
        }

        private static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs arg)
        {
            var fileName = new AssemblyName(arg.Name).Name + ".dll";
            var thisDir = Assembly.GetExecutingAssembly().Location;
            var file = Path.Combine(thisDir, fileName);
            if (File.Exists(file))
            {
                return Assembly.LoadFrom(file);
            }
            else return null;
        }
    }
}
