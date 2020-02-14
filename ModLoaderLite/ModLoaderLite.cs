using System;

namespace ModLoader
{
    public static class ModLoaderLite
    {
        public static string GamePath { get; private set; }
        public static string WorkShopPath { get; private set; }
        public static Action<string> Log { get; private set; }

        public static void Init(string gamePath, string workShopPath, Action<string> log)
        {
            GamePath = gamePath ?? throw new ArgumentNullException("gamePath");
            WorkShopPath = workShopPath ?? throw new ArgumentNullException("workShopPath");
            Log = log ?? throw new ArgumentNullException("log");
        }

        public static void Start()
        {
            HarmonyLoaderLite.Enter();
        }
    }
}
