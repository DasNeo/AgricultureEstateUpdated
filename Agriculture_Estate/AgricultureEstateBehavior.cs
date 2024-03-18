using AgricultureEstate.l18n;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;

namespace AgricultureEstate
{
    public class AgricultureEstateBehavior : CampaignBehaviorBase
    {
        private static GauntletLayer? layer;
        private static GauntletMovie? gauntletMovie;
        private static LandManagementVM? landManagementVM;
        private static GauntletLayer? layer2;
        private static GauntletMovie? gauntletMovie2;
        public static EstateListVM? estateListVM;
        public static Dictionary<Settlement, VillageLand> VillageLands = new Dictionary<Settlement, VillageLand>();
        public static int LastDayTotalSales;
        private Random rng = new Random();

        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.MenuItems));
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.HourlyTick));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnNewGameCreated));
            this.setAdditionalPerkDescriptions();
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            VillageLands.Clear();
        }

        private void setAdditionalPerkDescriptions()
        {
            this.AddPerkDescription(DefaultPerks.Riding.MountedPatrols, new TextObject("{=agricultureestate_perk_mountedpatrols}Agriculture Estate : Slave escape chance reduced by 20%").ToString());
            this.AddPerkDescription(DefaultPerks.Steward.ForcedLabor, new TextObject("{=agricultureestate_perk_forcedlabor}Agriculture Estate : Allows use of non bandit prisoners for slave labor").ToString());
            this.AddPerkDescription(DefaultPerks.Roguery.Manhunter, new TextObject("{=agricultureestate_perk_slavetrader}Agriculture Estate : Estates buy slaves at 20% reduced cost").ToString());
            this.AddPerkDescription(DefaultPerks.Trade.InsurancePlans, new TextObject("{=agricultureestate_perk_insuranceplans}Agriculture Estate : Half of the cost of land siezed durring war is returned").ToString());
            this.AddPerkDescription(DefaultPerks.Trade.RapidDevelopment, new TextObject("{=agricultureestate_perk_rapiddevelopment}Agriculture Estate : Half of the cost of land siezed durring war is returned").ToString());
            this.AddPerkDescription(DefaultPerks.Trade.TradeyardForeman, new TextObject("{=agricultureestate_perk_tradeyardforeman}Agriculture Estate : Estates in villages that have primary production clay, iron, cotton, or silver has 20% increased slave output").ToString());
            this.AddPerkDescription(DefaultPerks.Trade.GranaryAccountant, new TextObject("{=agricultureestate_perk_granaryaccountant}Agriculture Estate : Estates in villages that have primary production grain, olives, fish, date has 20% increased slave output").ToString());
            this.AddPerkDescription(DefaultPerks.Riding.Breeder, new TextObject("{=agricultureestate_perk_breeder}Agriculture Estate : Estates in villages that have primary production horses has 10% increased slave output").ToString());
            this.AddPerkDescription(DefaultPerks.Steward.Contractors, new TextObject("{=agricultureestate_perk_contractors}Agriculture Estate : Upgrades in estates cost 15% less").ToString());
            this.AddPerkDescription(DefaultPerks.Engineering.Foreman, new TextObject("{=agricultureestate_perk_foreman}Agriculture Estate : Completing a land clearance project adds 30 hearths to the village").ToString());
        }

        private void AddPerkDescription(PerkObject perk, string description)
        {
            TextObject textObject = new TextObject(perk.Description.ToString() + "\n \n" + description);
            typeof(PropertyObject).GetField("_description", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(perk, textObject);
        }

        public static float CalculateGold(VillageLand villageLand)
        {
            float num1 = 0f;
            VillageLand villageLand2 = villageLand;
            Village? village = villageLand.Village;
            float num2 = villageLand2.OwnedPlots == 0 ? 0.0f : Math.Max(0.0f, (float)((10.0 * villageLand2.OwnedPlots - villageLand2.Prisoners.TotalManCount) / (10.0 * villageLand2.OwnedPlots)));
            if (villageLand2.OwnedPlots > 0 && village?.VillageState != Village.VillageStates.Looted)
                num1 += (float)(((double?)village?.TradeTaxAccumulated ?? 0d) * (double)num2 / 100.0) * villageLand2.OwnedPlots * SubModule.LandRentScale;
            return num1;
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            CharacterObject? bandit = this.getBandit(party);
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
                    bandit = this.getBandit(party);
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
                for (int index = villageLand.OwnedPlots * 10 - villageLand.Prisoners.TotalManCount; bandit != null && index > 0 && num4 + Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(bandit, hero) <= Hero.MainHero.Gold; bandit = this.getBandit(party))
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

        private CharacterObject? getBandit(MobileParty party)
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

        private void DailyTick()
        {
            this.SlaveDeclineTick();
            this.CollectGoldTick();
            this.StartSlaveRebellionTick();
            this.RemoveInHostileTick();
        }

        private void HourlyTick()
        {
            this.productionTick();
            this.projectProgressTick();
        }

        private void RemoveInHostileTick()
        {
            foreach (KeyValuePair<Settlement, VillageLand> villageLand1 in VillageLands)
            {
                Village village = villageLand1.Key.Village;
                VillageLand villageLand2 = villageLand1.Value;
                if (village.Owner.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) && (villageLand2.OwnedPlots > 0 || villageLand2.OwnedUndevelopedPlots > 0))
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        Localization.SetTextVariables("{=agricultureestate_land_lost_due_to_war}Your lands in the village of {SETTLEMENT_NAME} has been siezed due to war with {WAR_TARGET}",
                        new KeyValuePair<string, string?>("SETTLEMENT_NAME", village.Name.ToString()),
                        new KeyValuePair<string, string?>("WAR_TARGET", village.Owner.MapFaction.Name.ToString())).ToString()));

                    villageLand2.AvaliblePlots += villageLand2.OwnedPlots;
                    if (Hero.MainHero.GetPerkValue(DefaultPerks.Trade.RapidDevelopment) || Hero.MainHero.GetPerkValue(DefaultPerks.Trade.InsurancePlans))
                        Hero.MainHero.Gold += (int)(SubModule.PlotSellPrice / 1.25 * villageLand2.OwnedPlots);
                    villageLand2.OwnedPlots = 0;
                    villageLand2.AvalibleUndevelopedPlots += villageLand2.OwnedUndevelopedPlots;
                    if (Hero.MainHero.GetPerkValue(DefaultPerks.Trade.RapidDevelopment) || Hero.MainHero.GetPerkValue(DefaultPerks.Trade.InsurancePlans))
                        Hero.MainHero.Gold += (int)(SubModule.UndevelopedPlotSellPrice / 1.25 * villageLand2.OwnedUndevelopedPlots);
                    villageLand2.OwnedUndevelopedPlots = 0;
                    villageLand2.Prisoners = TroopRoster.CreateDummyTroopRoster();
                }
            }
        }

        private void StartSlaveRebellionTick()
        {
            foreach (KeyValuePair<Settlement, VillageLand> villageLand1 in VillageLands)
            {
                Village village = villageLand1.Key.Village;
                VillageLand villageLand2 = villageLand1.Value;
                if (villageLand2.Prisoners.TotalManCount >= 20 && this.rng.Next(1000) <= 10.0 * (double)villageLand2.SlaveRevoltRisk)
                {
                    MobileParty banditParty = BanditPartyComponent.CreateBanditParty(village.Name.ToString() + " slave revolt", Clan.BanditFactions.First<Clan>(), SettlementHelper.FindNearestHideout(x => x.IsActive).Hideout, false);
                    banditParty.InitializeMobilePartyAroundPosition(new TroopRoster(banditParty.Party), new TroopRoster(banditParty.Party), village.Settlement.Position2D, 1f, 0.0f);
                    banditParty.IsVisible = true;
                    while (banditParty.MemberRoster.TotalManCount < 20 && villageLand2.Prisoners.TotalManCount > 0)
                    {
                        CharacterObject character = TaleWorlds.Core.Extensions.GetRandomElement<TroopRosterElement>(villageLand2.Prisoners.GetTroopRoster()).Character;
                        villageLand2.Prisoners.AddToCounts(character, -1, false, 0, 0, true, -1);
                        banditParty.MemberRoster.AddToCounts(character, 1, false, 0, 0, true, -1);
                    }
                    InformationManager.ShowInquiry(new InquiryData(new TextObject("{=agricultureestate_slave_revolt_title}Slave Revolt").ToString(),
                        Localization.SetTextVariables("{=agricultureestate_slave_revolt_description}The slave at your estate in the village of {SETTLEMENT_NAME} have revolted.",
                        new KeyValuePair<string, string?>("SETTLEMENT_NAME", village.Name.ToString())).ToString(),
                        true, false, new TextObject("{=agricultureestate_slave_revolt_button_text}Not Good").ToString(), "", null, null), false);
                    
                    banditParty.Ai.SetMoveRaidSettlement(village.Settlement);
                }
            }
        }

        private void projectProgressTick()
        {
            foreach (KeyValuePair<Settlement, VillageLand> villageLand1 in VillageLands)
            {
                Village village = villageLand1.Key.Village;
                VillageLand villageLand2 = villageLand1.Value;
                if (villageLand2.CurrentProject != "None")
                {
                    ++villageLand2.ProjectProgress;
                    if (villageLand2.ProjectProgress >= (Settings.Instance?.ProjectTime * 24))
                    {
                        if (villageLand2.CurrentProject == "Land Clearance")
                        {
                            if (villageLand2.OwnedUndevelopedPlots > 0)
                            {
                                --villageLand2.OwnedUndevelopedPlots;
                                ++villageLand2.OwnedPlots;
                                if (Hero.MainHero.GetPerkValue(DefaultPerks.Engineering.Foreman))
                                    village.Hearth += 30f;
                            }
                        }
                        else if (villageLand2.CurrentProject == "Increase Patrols")
                        {
                            if (villageLand2.PatrolLevel < 8)
                                ++villageLand2.PatrolLevel;
                        }
                        else if (villageLand2.CurrentProject == "Expand Storehouse")
                            villageLand2.StorageCapacity += 500;
                        villageLand2.ProjectProgress = 0;
                        if (villageLand2.ProjectQueue == null)
                            villageLand2.ProjectQueue = new Queue<string>();
                        villageLand2.CurrentProject = !TaleWorlds.Core.Extensions.IsEmpty<string>(villageLand2.ProjectQueue) ? villageLand2.ProjectQueue.Dequeue() : new TextObject("{=agricultureestate_none}None").ToString();
                    }
                }
            }
        }

        private void CollectGoldTick()
        {
            LastDayTotalSales = 0;
            foreach (KeyValuePair<Settlement, VillageLand> villageLand1 in VillageLands)
            {
                Village village = villageLand1.Key.Village;
                VillageLand villageLand2 = villageLand1.Value;
                float num1 = 0.0f;
                float num2 = villageLand2.OwnedPlots == 0 ? 0.0f : Math.Max(0.0f, (float)((10.0 * villageLand2.OwnedPlots - villageLand2.Prisoners.TotalManCount) / (10.0 * villageLand2.OwnedPlots)));
                if (villageLand2.OwnedPlots > 0 && village.VillageState != Village.VillageStates.Looted)
                    num1 += (float)(village.TradeTaxAccumulated * (double)num2 / 100.0) * villageLand2.OwnedPlots * SubModule.LandRentScale;
                villageLand2.LastDayIncome = villageLand2.Gold + (int)num1;
                LastDayTotalSales += villageLand2.Gold;
                villageLand2.Gold = 0;
            }
        }

        private void SlaveDeclineTick()
        {
            foreach (KeyValuePair<Settlement, VillageLand> villageLand in VillageLands)
            {
                Village village = villageLand.Key.Village;
                VillageLand land = villageLand.Value;
                if (land.Prisoners.TotalManCount > 0 && village.VillageState != Village.VillageStates.BeingRaided && village.VillageState != Village.VillageStates.Looted)
                    this.SlaveDecline(land);
            }
        }

        private void SlaveDecline(VillageLand land)
        {
            foreach (TroopRosterElement troopRosterElement in land.Prisoners.GetTroopRoster())
            {
                for (int index = 0; index < troopRosterElement.Number; ++index)
                {
                    if (Settings.Instance?.SlaveDeclineModifier == 0)
                        return;
                    if(troopRosterElement.Character != null && land.Prisoners != null)
                        if (this.rng.Next(1000) < ((double)land.SlaveDeclineRate() * Settings.Instance?.SlaveDeclineModifier) * 10.0)
                            land.Prisoners.AddToCounts(troopRosterElement.Character, -1, false, 0, 0, true, -1);
                }
            }
        }

        private void productionTick()
        {
            foreach (KeyValuePair<Settlement, VillageLand> villageLand in VillageLands)
            {
                Village village = villageLand.Key.Village;
                VillageLand land = villageLand.Value;
                try
                {
                    if (land.Prisoners.TotalManCount > 0 && village.VillageState != Village.VillageStates.BeingRaided && village.VillageState != Village.VillageStates.Looted)
                    {
                        foreach ((ItemObject, float) production in (IEnumerable<(ItemObject, float)>)village.VillageType.Productions)
                        {
                            float productionChance = production.Item2 * 10f;
                            if ((village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("grain") || village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("olives") || village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("fish") || village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("date_fruit")) && Hero.MainHero.GetPerkValue(DefaultPerks.Trade.GranaryAccountant))
                                this.produce(land.Prisoners.TotalManCount, production.Item1, 1.2f * productionChance, land);
                            else if ((village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("clay") || village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("iron") || village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("cotton") || village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("silver")) && Hero.MainHero.GetPerkValue(DefaultPerks.Trade.TradeyardForeman))
                                this.produce(land.Prisoners.TotalManCount, production.Item1, 1.2f * productionChance, land);
                            else if (village.VillageType.PrimaryProduction.Type == ItemObject.ItemTypeEnum.Horse && Hero.MainHero.GetPerkValue(DefaultPerks.Riding.Breeder))
                                this.produce(land.Prisoners.TotalManCount, production.Item1, 1.1f * productionChance, land);
                            else
                                this.produce(land.Prisoners.TotalManCount, production.Item1, productionChance, land);
                        }
                    }
                } catch(Exception)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"village: {village.Name}"));
                    InformationManager.DisplayMessage(new InformationMessage($"village.VillageType: {village.VillageType}"));
                    InformationManager.DisplayMessage(new InformationMessage($"village.VillageType.Productions: {village.VillageType.Productions}"));
                }
            }
        }

        private void produce(int slaves, ItemObject item, float productionChance, VillageLand land)
        {
            int num = 0;
            foreach (ItemRosterElement itemRosterElement in land.Stockpile)
                num += itemRosterElement.Amount;
            if (num > land.StorageCapacity && !land.SellToMarket)
                return;
            for (int index = 0; index < slaves; ++index)
            {
                if ((double)productionChance > this.rng.Next((int)(10000.0 / SubModule.SlaveProductionScale)))
                {
                    if (land.SellToMarket)
                    {
                        land.Village?.Settlement.ItemRoster.AddToCounts(item, 1);
                        land.Gold += land.Village?.MarketData.GetPrice(item, MobileParty.MainParty, true, PartyBase.MainParty) ?? 0;
                    }
                    else
                        land.Stockpile.AddToCounts(item, 1);
                    if (MobileParty.MainParty != null)
                        SkillLevelingManager.OnTradeProfitMade(MobileParty.MainParty.Party, Math.Max(1, (land.Village?.MarketData.GetPrice(item, MobileParty.MainParty, true, PartyBase.MainParty) ?? 0) / 10));
                }
            }
        }

        private void MenuItems(CampaignGameStarter campaignGameStarter) => campaignGameStarter.AddGameMenuOption("village", "village_land", new TextObject("{=agricultureestate_gamemenu_land_management}Land Management").ToString(),
            (args) =>
            {
                args.optionLeaveType = TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.LeaveType.Manage;
                return true;
            }, (args) => CreateVMLayer(), false, 1, false);

        public static void CreateVMLayer(VillageLand? village = null)
        {
            if (village is null)
                village = GetVillageLand(Settlement.CurrentSettlement);
            try
            {
                if (layer != null)
                    return;
                layer = new GauntletLayer(1000, "GauntletLayer", false);
                if (landManagementVM == null)
                    landManagementVM = new LandManagementVM(village);
                landManagementVM.RefreshValues();
                gauntletMovie = (GauntletMovie)layer.LoadMovie("LandManagement", landManagementVM);
                layer.InputRestrictions.SetInputRestrictions(true, (InputUsageMask)7);
                ScreenManager.TopScreen.AddLayer(layer);
                layer.IsFocusLayer = true;
                ScreenManager.TrySetFocus(layer);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(ex.ToString()));
                Console.WriteLine(ex);
            }
        }

        public static void DeleteVMLayer()
        {
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (layer != null)
            {
                layer.InputRestrictions.ResetInputRestrictions();
                layer.IsFocusLayer = false;
                if (gauntletMovie != null)
                    layer.ReleaseMovie(gauntletMovie);
                topScreen.RemoveLayer(layer);
            }
            layer = null;
            gauntletMovie = null;
            landManagementVM = null;
        }

        public static void CreateVMLayer2()
        {
            try
            {
                if (layer2 != null)
                    return;
                layer2 = new GauntletLayer(1200, "GauntletLayer", false);
                if (estateListVM == null)
                    estateListVM = new EstateListVM();
                estateListVM.RefreshValues();
                gauntletMovie2 = (GauntletMovie)layer2.LoadMovie("EstateList", estateListVM);
                layer2.InputRestrictions.SetInputRestrictions(true, (InputUsageMask)7);
                ScreenManager.TopScreen.AddLayer(layer2);
                layer2.IsFocusLayer = true;
                ScreenManager.TrySetFocus(layer2);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(ex.ToString()));
                Console.WriteLine(ex);
            }
        }

        public static void DeleteVMLayer2()
        {
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (layer2 != null)
            {
                layer2.InputRestrictions.ResetInputRestrictions();
                layer2.IsFocusLayer = false;
                if (gauntletMovie2 != null)
                    layer2.ReleaseMovie(gauntletMovie2);
                topScreen.RemoveLayer(layer2);
            }
            layer2 = null;
            gauntletMovie2 = null;
            estateListVM = null;
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
