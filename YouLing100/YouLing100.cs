using Harmony;
using XiaWorld;


namespace YouLing100
{
    [HarmonyPatch(typeof(ItemThing), "DoSoulCrystalYouPowerUp")]
    internal static class You100
    {
        static void Prefix(ref float badd)
        {
            badd += 1f;
        }
    }
    [HarmonyPatch(typeof(ItemThing), "DoSoulCrystalLingPowerUp")]
    internal static class Ling100
    {
        static void Prefix(ref float badd)
        {
            badd += 1f;
        }
    }
}
