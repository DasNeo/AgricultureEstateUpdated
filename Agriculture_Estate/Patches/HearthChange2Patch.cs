// Decompiled with JetBrains decompiler
// Type: AgricultureEstate.HearthChange2Patch
// Assembly: AgricultureEstate, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A4103D21-1273-439E-B48D-A11FAB56D6B9
// Assembly location: C:\Users\andre\Downloads\AgricultureEstate\bin\Win64_Shipping_Client\AgricultureEstate.dll

using TaleWorlds.CampaignSystem.Settlements;

namespace AgricultureEstate
{
    //[HarmonyPatch(typeof(Village), "HearthChange")]
    class HearthChange2Patch
    {
        private static void Postfix(ref Village __instance, ref float __result)
        {
            if (AgricultureEstateBehavior.VillageLands == null)
                return;
            VillageLand villageLand;
            if (AgricultureEstateBehavior.VillageLands.TryGetValue(__instance.Settlement, out villageLand) && villageLand.AvaliblePlots + villageLand.OwnedPlots > 10)
            {
                int num = villageLand.OwnedPlots + villageLand.AvaliblePlots - 10;
                __result += num * 0.1f;
            }
            __result -= (float)(((double)__instance.Hearth - 600.0) / 1000.0);
        }
    }
}
