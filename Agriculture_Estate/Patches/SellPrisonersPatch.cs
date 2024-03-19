// Decompiled with JetBrains decompiler
// Type: AgricultureEstate.SellPrisonersPatch
// Assembly: AgricultureEstate, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A4103D21-1273-439E-B48D-A11FAB56D6B9
// Assembly location: C:\Users\andre\Downloads\AgricultureEstate\bin\Win64_Shipping_Client\AgricultureEstate.dll

using AgricultureEstate.l18n;
using HarmonyLib;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AgricultureEstate
{
    [HarmonyPatch(typeof(SellPrisonersAction), "ApplyForAllPrisoners")]
    class SellPrisonersPatch
    {
        private static bool Prefix(
          PartyBase sellerParty,
          PartyBase buyerParty)
        {
            var currentSettlement = sellerParty.MobileParty.CurrentSettlement;
            var prisoners = sellerParty.PrisonRoster;
            if (sellerParty == null || sellerParty.MobileParty == MobileParty.MainParty || sellerParty.LeaderHero == null 
                || currentSettlement == null || !currentSettlement.IsTown && !currentSettlement.IsCastle)
                return true;
            foreach (Village boundVillage in currentSettlement.BoundVillages)
            {
                VillageLand villageLand;
                if (AgricultureEstateBehavior.VillageLands.TryGetValue(boundVillage.Settlement, out villageLand) && villageLand.BuySlaves)
                {
                    int num1 = 0;
                    int num2 = 0;
                    int num3 = villageLand.OwnedPlots * 10 - villageLand.Prisoners.TotalManCount;
                    for (CharacterObject? bandit = getBandit(prisoners); bandit != null && num3 > 0 && num2 + Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(bandit, sellerParty.LeaderHero) <= Hero.MainHero.Gold; bandit = getBandit(prisoners))
                    {
                        num2 += Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(bandit, sellerParty.LeaderHero);
                        prisoners.AddToCounts(bandit, -1, false, 0, 0, true, -1);
                        villageLand.Prisoners.AddToCounts(bandit, 1, false, 0, 0, true, -1);
                        --num3;
                        ++num1;
                    }
                    if (num1 > 0)
                    {
                        if (Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.Manhunter))
                            num2 = (int)(0.8 * num2);
                        GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, sellerParty.LeaderHero, num2, false);
                        InformationManager.DisplayMessage(new InformationMessage(
                            Localization.SetTextVariables("{=agricultureestate_boughtslaves}Your estate in {SETTLEMENT_NAME} bought a load of {SLAVE_BOUGHT_AMOUNT} slaves from {SELLER_LORD_NAME} in the markets of {SELLER_SETTLEMENT_NAME} at a cost of {PRICE}{GOLD_ICON} gold",
                            new KeyValuePair<string, string?>("SETTLEMENT_NAME", boundVillage.Name.ToString()),
                            new KeyValuePair<string, string?>("SLAVE_BOUGHT_AMOUNT", num1.ToString()),
                            new KeyValuePair<string, string?>("SELLER_LORD_NAME", sellerParty.LeaderHero.Name.ToString()),
                            new KeyValuePair<string, string?>("SELLER_SETTLEMENT_NAME", currentSettlement.Name.ToString()),
                            new KeyValuePair<string, string?>("PRICE", num2.ToString()),
                            new KeyValuePair<string, string?>("GOLD_ICON", null)).ToString()));
                    }
                }
            }
            return true;
        }

        private static CharacterObject? getBandit(TroopRoster roster)
        {
            if (Equals(roster, null))
                return null;
            foreach (TroopRosterElement troopRosterElement in roster.GetTroopRoster())
            {
                if (troopRosterElement.Character.Occupation == Occupation.Bandit && !troopRosterElement.Character.IsHero)
                    return troopRosterElement.Character;
            }
            return null;
        }
    }
}
