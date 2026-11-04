using AgricultureEstate.l18n;
using HarmonyLib;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace AgricultureEstate
{
    [HarmonyPatch(typeof(SellPrisonersAction), "ApplyForAllPrisoners")]
    class SellPrisonersPatch
    {
        private static void Prefix(PartyBase sellerParty)
        {
            if (CanBuyFrom(sellerParty))
                BuyFromSale(sellerParty, sellerParty.PrisonRoster.CloneRosterData());
        }

        internal static bool CanBuyFrom(PartyBase? sellerParty)
        {
            var party = sellerParty?.MobileParty;
            var settlement = party?.CurrentSettlement;
            return party != null && party != MobileParty.MainParty && sellerParty!.LeaderHero != null
                && settlement != null && (settlement.IsTown || settlement.IsCastle);
        }

        internal static void BuyFromSale(PartyBase sellerParty, TroopRoster offeredPrisoners)
        {
            var currentSettlement = sellerParty.MobileParty.CurrentSettlement;
            var sellerPrisoners = sellerParty.PrisonRoster;
            foreach (Village boundVillage in currentSettlement.BoundVillages)
            {
                if (!AgricultureEstateBehavior.VillageLands.TryGetValue(boundVillage.Settlement, out var land) || !land.BuySlaves)
                    continue;

                int bought = 0;
                int cost = 0;
                int capacity = land.OwnedPlots * 10 - land.Prisoners.TotalManCount;
                while (capacity > 0)
                {
                    var prisoner = GetEligiblePrisoner(offeredPrisoners, sellerPrisoners);
                    if (prisoner == null)
                        break;
                    int nextCost = cost + Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(prisoner, sellerParty.LeaderHero);
                    if (nextCost > Hero.MainHero.Gold)
                        break;

                    // Remove from both rosters: the native sale must not sell or pay for this prisoner again.
                    sellerPrisoners.AddToCounts(prisoner, -1, false, 0, 0, true, -1);
                    offeredPrisoners.AddToCounts(prisoner, -1, false, 0, 0, true, -1);
                    land.Prisoners.AddToCounts(prisoner, 1, false, 0, 0, true, -1);
                    cost = nextCost;
                    --capacity;
                    ++bought;
                }
                if (bought == 0)
                    continue;

                if (Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.Manhunter))
                    cost = (int)(0.8 * cost);
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, sellerParty.LeaderHero, cost, false);
                InformationManager.DisplayMessage(new InformationMessage(
                    Localization.SetTextVariables("{=agricultureestate_boughtslaves}Your estate in {SETTLEMENT_NAME} bought a load of {SLAVE_BOUGHT_AMOUNT} slaves from {SELLER_LORD_NAME} in the markets of {SELLER_SETTLEMENT_NAME} at a cost of {PRICE}{GOLD_ICON} gold",
                    new KeyValuePair<string, string?>("SETTLEMENT_NAME", boundVillage.Name.ToString()),
                    new KeyValuePair<string, string?>("SLAVE_BOUGHT_AMOUNT", bought.ToString()),
                    new KeyValuePair<string, string?>("SELLER_LORD_NAME", sellerParty.LeaderHero.Name.ToString()),
                    new KeyValuePair<string, string?>("SELLER_SETTLEMENT_NAME", currentSettlement.Name.ToString()),
                    new KeyValuePair<string, string?>("PRICE", cost.ToString()),
                    new KeyValuePair<string, string?>("GOLD_ICON", null)).ToString()));
            }
        }

        private static CharacterObject? GetEligiblePrisoner(TroopRoster offered, TroopRoster seller)
        {
            foreach (TroopRosterElement entry in offered.GetTroopRoster())
            {
                if (entry.Number > 0 && !entry.Character.IsHero
                    && (EstateConfiguration.AllowAllPrisonersAsSlaves || entry.Character.Occupation == Occupation.Bandit)
                    && seller.GetTroopCount(entry.Character) > 0)
                    return entry.Character;
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(SellPrisonersAction), "ApplyForSelectedPrisoners")]
    class SellSelectedPrisonersPatch
    {
        private static void Prefix(PartyBase sellerParty, ref TroopRoster prisoners)
        {
            if (!SellPrisonersPatch.CanBuyFrom(sellerParty) || prisoners == null)
                return;

            // Keep the sale roster independent even if a caller passes the seller's live roster.
            prisoners = prisoners.CloneRosterData();
            SellPrisonersPatch.BuyFromSale(sellerParty, prisoners);
        }
    }
}
