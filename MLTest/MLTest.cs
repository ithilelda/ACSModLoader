using UnityEngine;
using ModLoader;

[assembly: Require("YouLing100")]
namespace MLTest
{
    public static class MLTest
    {
        private static GameObject go;
        private static TestComponent tc;
        static bool Prepare()
        {
            KLog.Log(KLogLevel.Debug, "[MLTest] Generate GameObject and its component!");
            go = new GameObject();
            tc = go.GetComponent<TestComponent>();
            return true;
        }
    }
}
