using UnityEngine;
using XiaWorld;
using Harmony;


namespace MLTest
{
    [HarmonyPatch(typeof(MainManager), "Init")]
    public static class MLTest
    {
        private static GameObject go;
        private static TestComponent tc;
        static void Postfix()
        {
            KLog.Log(KLogLevel.Debug, "[MLTest] Generate GameObject and its component!");
            go = new GameObject();
            tc = go.GetComponent<TestComponent>();
            GameObject.DontDestroyOnLoad(go);
        }
    }
}
