// Decompiled with JetBrains decompiler
// Type: AgricultureEstate.HearthLevelPatch
// Assembly: AgricultureEstate, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A4103D21-1273-439E-B48D-A11FAB56D6B9
// Assembly location: C:\Users\andre\Downloads\AgricultureEstate\bin\Win64_Shipping_Client\AgricultureEstate.dll

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
