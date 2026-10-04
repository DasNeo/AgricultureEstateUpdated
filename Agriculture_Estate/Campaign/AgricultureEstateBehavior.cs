using AgricultureEstate.l18n;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AgricultureEstate
{
    public class AgricultureEstateBehavior : CampaignBehaviorBase
    {
        public static Dictionary<Settlement, VillageLand> VillageLands = new Dictionary<Settlement, VillageLand>();
        public static int LastDayTotalSales;
        private readonly EstateSimulation _simulation = new EstateSimulation(() => VillageLands, new Random());

        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, MenuItems);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            PerkDescriptionRegistrar.Register();
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            VillageLands.Clear();
        }

        public static float CalculateGold(VillageLand land) => EstateIncome.CalculateRent(land);

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            CharacterObject? bandit = GetBandit(party);
            VillageLand villageLand;
            if (bandit == null || !VillageLands.TryGetValue(settlement, out villageLand))
                return;
            if (party.ActualClan == Hero.MainHero.Clan && hero != Hero.MainHero && hero != null)
            {
                int num1 = 0;
                int num2 = villageLand.OwnedPlots * 10 - villageLand.Prisoners.TotalManCount;
                while (bandit != null && num2 > 0)
                {
                    party.PrisonRoster.AddToCounts(bandit, -1, false, 0, 0, true, -1);
                    villageLand.Prisoners.AddToCounts(bandit, 1, false, 0, 0, true, -1);
                    bandit = GetBandit(party);
                    --num2;
                    ++num1;
                }
                if (num1 > 0)
                    InformationManager.DisplayMessage(
                        new InformationMessage(
                            Localization.SetTextVariables("{=agricultureestate_lord_brought_prisoners}{LORD_NAME} transfered {PRIOSNER_COUNT} prisoners to your estate in {SETTLEMENT_NAME}",
                            new KeyValuePair<string, string?>("LORD_NAME", hero.Name.ToString()),
                            new KeyValuePair<string, string?>("PRIOSNER_COUNT", num1.ToString()),
                            new KeyValuePair<string, string?>("SETTLEMENT_NAME", settlement.Name.ToString())).ToString()));
            }
            if (party.ActualClan != Hero.MainHero.Clan && villageLand.BuySlaves && hero != null)
            {
                int num3 = 0;
                int num4 = 0;
                for (int index = villageLand.OwnedPlots * 10 - villageLand.Prisoners.TotalManCount; bandit != null && index > 0 && num4 + Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(bandit, hero) <= Hero.MainHero.Gold; bandit = GetBandit(party))
                {
                    num4 += Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(bandit, hero);
                    party.PrisonRoster.AddToCounts(bandit, -1, false, 0, 0, true, -1);
                    villageLand.Prisoners.AddToCounts(bandit, 1, false, 0, 0, true, -1);
                    --index;
                    ++num3;
                }
                if (num3 > 0)
                {
                    if (Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.Manhunter))
                        num4 = (int)(0.8 * num4);
                    GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, hero, num4, false);
                    InformationManager.DisplayMessage(new InformationMessage(
                        Localization.SetTextVariables("{=agricultureestate_lord_sold_prisoners}{LORD_NAME} sold {PRISONER_COUNT} prisoners to your estate in {SETTLEMENT_NAME} for {SELL_PRICE}{GOLD_ICON} gold",
                        new KeyValuePair<string, string?>("LORD_NAME", hero.Name.ToString()),
                        new KeyValuePair<string, string?>("PRISONER_COUNT", num3.ToString()),
                        new KeyValuePair<string, string?>("SETTLEMENT_NAME", settlement.Name.ToString()),
                        new KeyValuePair<string, string?>("SELL_PRICE", num4.ToString()),
                        new KeyValuePair<string, string?>("GOLD_ICON", null)).ToString()));
                }
            }
        }

        private CharacterObject? GetBandit(MobileParty party)
        {
            if (party == null || Equals(party.PrisonRoster, null))
                return null;
            foreach (TroopRosterElement troopRosterElement in party.PrisonRoster.GetTroopRoster())
            {
                if (troopRosterElement.Character.Occupation == Occupation.Bandit && !troopRosterElement.Character.IsHero)
                    return troopRosterElement.Character;
            }
            return null;
        }

        private void DailyTick() => LastDayTotalSales = _simulation.DailyTick();

        private void HourlyTick() => _simulation.HourlyTick();

        private void MenuItems(CampaignGameStarter campaignGameStarter) => campaignGameStarter.AddGameMenuOption("village", "village_land", new TextObject("{=agricultureestate_gamemenu_land_management}Land Management").ToString(),
            (args) =>
            {
                args.optionLeaveType = TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.LeaveType.Manage;
                return true;
            }, (args) => CreateVMLayer(), false, 1, false);

        // Compatibility entry points; screen ownership belongs to Presentation.
        public static void CreateVMLayer(VillageLand? village = null) => EstateScreenController.CreateVMLayer(village);
        public static void DeleteVMLayer() => EstateScreenController.DeleteVMLayer();
        public static void CreateVMLayer2() => EstateScreenController.CreateVMLayer2();
        public static void DeleteVMLayer2() => EstateScreenController.DeleteVMLayer2();
        public static EstateListVM? estateListVM
        {
            get => EstateScreenController.estateListVM;
            set => EstateScreenController.estateListVM = value;
        }

        public static VillageLand GetVillageLand(Settlement settlement)
        {
            VillageLand villageLand1;
            if (VillageLands.TryGetValue(settlement, out villageLand1))
                return villageLand1;
            VillageLand villageLand2 = new VillageLand(settlement.Village);
            VillageLands.Add(settlement, villageLand2);
            return villageLand2;
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (!dataStore.SyncData<Dictionary<Settlement, VillageLand>>("_village_land", ref VillageLands))
                VillageLands.Clear();

            Dictionary<Settlement, VillageLand> newVillageLands = new Dictionary<Settlement, VillageLand>();
            
            foreach (var vil in VillageLands)
            {
                if (!(vil.Value?.Village?.Owner is null))
                    newVillageLands.Add(vil.Key, vil.Value);
            }
            VillageLands = newVillageLands;
        }
    }
}
