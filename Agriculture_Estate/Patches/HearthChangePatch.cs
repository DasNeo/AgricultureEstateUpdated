using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace AgricultureEstate
{
    //[HarmonyPatch(typeof(Village), "HearthChangeExplanation")]
    class HearthChangePatch
    {
        private static void Postfix(ref Village __instance, ref ExplainedNumber __result)
        {
            VillageLand villageLand;
            if (AgricultureEstateBehavior.VillageLands.TryGetValue(__instance.Settlement, out villageLand) && villageLand.AvaliblePlots + villageLand.OwnedPlots > 10)
            {
                int num = villageLand.OwnedPlots + villageLand.AvaliblePlots - 10;
                __result.Add(num * 0.1f, new TextObject("Land Clearance"));
            }
            if ((double)__instance.Hearth < 600.0)
                return;
            __result.Add((float)(((double)__instance.Hearth - 600.0) / -1000.0), new TextObject("Overpopulation and Land Scarcity"));
        }
    }
}
