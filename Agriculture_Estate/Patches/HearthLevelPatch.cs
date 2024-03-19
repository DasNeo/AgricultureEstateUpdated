using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;

namespace AgricultureEstate
{
    [HarmonyPatch(typeof(Village), "GetHearthLevel")]
    class HearthLevelPatch
    {
        private static bool Prefix(ref Village __instance, ref int __result)
        {
            if ((double)__instance.Hearth < 1000.0)
                return true;
            __result = (int)(((double)__instance.Hearth + 200.0) / 400.0);
            return false;
        }
    }
}
