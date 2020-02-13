using Harmony;
using XiaWorld;
using UnityEngine;

namespace GOTest
{
    [HarmonyPatch(typeof(LuaMgr), "Init")]
    static class GOTest
    {
        static GameObject gameObject;
        static void Postfix()
        {
            gameObject = new GameObject();
            gameObject.AddComponent<GOComponent>();
        }
    }
}
