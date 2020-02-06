using System.Linq;
using Harmony;
using XiaWorld;
using System.IO;
using System.Text.RegularExpressions;
using ModLoader;

namespace FastMakeGold
{
    [HarmonyPatch(typeof(ToilBrokenNeck), "OnStepToil")]
    internal static class FastMakeGold
    {
        static readonly float DEFAULT_MULTI = 50f;
        static float multiplier = DEFAULT_MULTI;
        static bool Prepare()
        {
            var asm_file = typeof(FastMakeGold).Assembly.Location;
            var path = Path.GetDirectoryName(asm_file);
            var configs = Configuration.ReadAll(path, "FastMakeGold");
            string value;
            if(!configs.TryGetValue("multiplier", out value))
            {
                value = $"{DEFAULT_MULTI}";
            }
            multiplier = float.Parse(value);
            configs["multiplier"] = value;
            Configuration.WriteAll(configs, path, "FastMakeGold");
            return true;
        }
        static void Postfix(ToilBrokenNeck __instance, float dt)
        {
            var npc = __instance.npc;
            var curNeck = npc.PropertyMgr.Practice.CurNeck;
            if (curNeck.Kind == g_emGongBottleNeckType.Gold)
            {
                var max_dan = GameDefine.GoldDanLevel.Keys.Max();
                if (__instance.gold > max_dan) // we only speed up after we reached level 1 dan for better accuracy.
                {
                    var practice = npc.PropertyMgr.Practice;
                    var consumption = practice.BaseAbsorbEffectGold() * multiplier;
                    npc.AddLing(-consumption * dt);
                    var addition = practice.GoldEffectSpeed(npc.Key) * multiplier;
                    addition /= __instance.gold / 100000f;
                    __instance.gold += addition * dt;
                    __instance.SetProgress(1f - npc.LingV / npc.MaxLing);
                }
            }
        }
    }
}
