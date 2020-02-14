using System;

namespace ModLoader
{
    public static class ModLoaderLite
    {
        public static string GamePath { get; private set; }
        public static string WorkShopPath { get; private set; }
        public static Action<string> Log { get; private set; }
        private static bool patched;

        public static void Init(string gamePath, string workShopPath, Action<string> log)
        {
            GamePath = gamePath ?? throw new ArgumentNullException("gamePath");
            WorkShopPath = workShopPath ?? throw new ArgumentNullException("workShopPath");
            Log = log ?? throw new ArgumentNullException("log");
        }

        public static void Start()
        {
            if(!patched)
            {
                HarmonyLoaderLite.Enter();
                patched = true;
            }
        }
    }
}
