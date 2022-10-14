// Decompiled with JetBrains decompiler
// Type: AgricultureEstate.ClanFiancePatch
// Assembly: AgricultureEstate, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A4103D21-1273-439E-B48D-A11FAB56D6B9
// Assembly location: C:\Users\andre\Downloads\AgricultureEstate\bin\Win64_Shipping_Client\AgricultureEstate.dll

using HarmonyLib;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace AgricultureEstate
{
    [HarmonyPatch(typeof(DefaultClanFinanceModel), "CalculateClanIncomeInternal")]
    class ClanFiancePatch
    {
        private static void Postfix(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals = false)
        {
            float num1 = 0.0f;
            foreach (KeyValuePair<Settlement, VillageLand> villageLand1 in AgricultureEstateBehavior.VillageLands)
            {
                num1 += AgricultureEstateBehavior.CalculateGold(villageLand1.Value);
            }
            goldChange.Add(num1, new TextObject("Rent from unused owned land"));
            goldChange.Add(AgricultureEstateBehavior.LastDayTotalSales);
        }
    }
}
