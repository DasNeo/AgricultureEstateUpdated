using AgricultureEstate.l18n;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AgricultureEstate
{
    internal sealed class EstateSimulation
    {
        private readonly Func<Dictionary<Settlement, VillageLand>> _getEstates;
        private readonly Random rng;
        private Dictionary<Settlement, VillageLand> VillageLands => _getEstates();
        private int LastDayTotalSales;

        public EstateSimulation(Func<Dictionary<Settlement, VillageLand>> getEstates, Random random)
        {
            _getEstates = getEstates;
            rng = random;
        }
        public int DailyTick()
        {
            SlaveDeclineTick();
            CollectGoldTick();
            StartSlaveRebellionTick();
            RemoveInHostileTick();
            return LastDayTotalSales;
        }

        public void HourlyTick()
        {
            ProductionTick();
            ProjectProgressTick();
        }

        private void RemoveInHostileTick()
        {
            if (!Settings.Instance?.DestroyPlotsOnWar ?? false)
                return;
            
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
                        Hero.MainHero.Gold += (int)(EstateConfiguration.PlotSellPrice / 1.25 * villageLand2.OwnedPlots);
                    villageLand2.OwnedPlots = 0;
                    villageLand2.AvalibleUndevelopedPlots += villageLand2.OwnedUndevelopedPlots;
                    if (Hero.MainHero.GetPerkValue(DefaultPerks.Trade.RapidDevelopment) || Hero.MainHero.GetPerkValue(DefaultPerks.Trade.InsurancePlans))
                        Hero.MainHero.Gold += (int)(EstateConfiguration.UndevelopedPlotSellPrice / 1.25 * villageLand2.OwnedUndevelopedPlots);
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
                if (villageLand2.Prisoners.TotalManCount >= 20 && rng.Next(1000) <= 10.0 * (double)villageLand2.SlaveRevoltRisk)
                {
                    PartyTemplateObject partyTemplateObject = new PartyTemplateObject();
                    MobileParty banditParty = BanditPartyComponent.CreateBanditParty(village.Name.ToString() + " slave revolt", Clan.BanditFactions.First<Clan>(), 
                        SettlementHelper.FindNearestHideoutToSettlement(village.Settlement, MobileParty.NavigationType.Default), false, null, village.Settlement.Position);
                    banditParty.InitializeMobilePartyAroundPosition(new TroopRoster(banditParty.Party), new TroopRoster(banditParty.Party), village.Settlement.Position, 1f, 0.0f);
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
                    
                    banditParty.SetMoveRaidSettlement(village.Settlement, MobileParty.NavigationType.Default, false);
                }
            }
        }

        private void ProjectProgressTick()
        {
            foreach (var entry in VillageLands)
            {
                if (EstateComposition.CreateManagement(entry.Value).AdvanceProject(EstateConfiguration.ProjectDurationHours)
                    && Hero.MainHero.GetPerkValue(DefaultPerks.Engineering.Foreman))
                    entry.Key.Village.Hearth += 30f;
            }
        }
        private void CollectGoldTick()
        {
            LastDayTotalSales = 0;
            foreach (KeyValuePair<Settlement, VillageLand> villageLand1 in VillageLands)
            {
                Village village = villageLand1.Key.Village;
                VillageLand villageLand2 = villageLand1.Value;
                float num1 = EstateIncome.CalculateRent(villageLand2);
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
                    SlaveDecline(land);
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
                        if (rng.Next(1000) < land.SlaveDeclineRate() * 10.0)
                            land.Prisoners.AddToCounts(troopRosterElement.Character, -1, false, 0, 0, true, -1);
                }
            }
        }

        private void ProductionTick()
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
                                Produce(land.Prisoners.TotalManCount, production.Item1, 1.2f * productionChance, land);
                            else if ((village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("clay") || village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("iron") || village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("cotton") || village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("silver")) && Hero.MainHero.GetPerkValue(DefaultPerks.Trade.TradeyardForeman))
                                Produce(land.Prisoners.TotalManCount, production.Item1, 1.2f * productionChance, land);
                            else if (village.VillageType.PrimaryProduction.Type == ItemObject.ItemTypeEnum.Horse && Hero.MainHero.GetPerkValue(DefaultPerks.Riding.Breeder))
                                Produce(land.Prisoners.TotalManCount, production.Item1, 1.1f * productionChance, land);
                            else
                                Produce(land.Prisoners.TotalManCount, production.Item1, productionChance, land);
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

        private void Produce(int slaves, ItemObject item, float productionChance, VillageLand land)
        {
            int num = 0;
            foreach (ItemRosterElement itemRosterElement in land.Stockpile)
                num += itemRosterElement.Amount;
            if (num > land.StorageCapacity && !land.SellToMarket)
                return;
            for (int index = 0; index < slaves; ++index)
            {
                if ((double)productionChance > rng.Next((int)(10000.0 / EstateConfiguration.SlaveProductionScale)))
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

    }
}
