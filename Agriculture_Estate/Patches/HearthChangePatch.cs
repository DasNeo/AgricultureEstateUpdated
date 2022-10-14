// Decompiled with JetBrains decompiler
// Type: AgricultureEstate.HearthChangePatch
// Assembly: AgricultureEstate, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A4103D21-1273-439E-B48D-A11FAB56D6B9
// Assembly location: C:\Users\andre\Downloads\AgricultureEstate\bin\Win64_Shipping_Client\AgricultureEstate.dll

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
