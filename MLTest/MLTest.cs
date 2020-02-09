using UnityEngine;
using XiaWorld;
using Harmony;


namespace MLTest
{
    [HarmonyPatch(typeof(MainManager), "Run")]
    public static class MLTest
    {
        private static GameObject go;
        private static TestComponent tc;
        static void Prefix()
        {
            KLog.Log(KLogLevel.Debug, "[MLTest] Generate GameObject and its component!");
            go = new GameObject();
            tc = go.GetComponent<TestComponent>();
            GameObject.DontDestroyOnLoad(go);
        }
    }
}
