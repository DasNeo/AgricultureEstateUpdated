using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using Xunit;

namespace AgricultureEstate.Tests
{
    // Exercise the production prefixes with lightweight game-boundary doubles.
    // The main project build separately checks these calls against real game assemblies.
    public class AutoBuySlavesTests
    {
        private readonly CharacterObject _bandit = new CharacterObject { Occupation = Occupation.Bandit };
        private readonly PartyBase _seller = new PartyBase { MobileParty = new MobileParty(), LeaderHero = new Hero() };
        private readonly VillageLand _land = new VillageLand { BuySlaves = true, OwnedPlots = 1 };

        public AutoBuySlavesTests()
        {
            Hero.MainHero = new Hero { Gold = 1000 };
            EstateConfiguration.AllowAllPrisonersAsSlaves = false;
            AgricultureEstateBehavior.VillageLands.Clear();
            var village = new Village { Settlement = new Settlement() };
            _seller.MobileParty.CurrentSettlement = new Settlement { IsTown = true };
            _seller.MobileParty.CurrentSettlement.BoundVillages.Add(village);
            AgricultureEstateBehavior.VillageLands.Add(village.Settlement, _land);
        }

        [Fact]
        public void SelectedSaleBuysOnlyOfferedPrisonersAndRemovesThemFromNativeSale()
        {
            _seller.PrisonRoster.AddToCounts(_bandit, 5);
            var offered = TroopRoster.CreateDummyTroopRoster();
            offered.AddToCounts(_bandit, 2);
            var remainder = RunSelected(offered);

            Assert.Equal(2, _land.Prisoners.TotalManCount);
            Assert.Equal(3, _seller.PrisonRoster.TotalManCount);
            Assert.Equal(0, remainder.TotalManCount);
            Assert.Equal(980, Hero.MainHero.Gold);
            Assert.Equal(20, _seller.LeaderHero.Gold);
            Assert.Equal(2, offered.TotalManCount); // Caller-owned selection is not mutated.
        }

        [Fact]
        public void SelectedSalePreservesUnboughtPrisonersForNativeSale()
        {
            _land.Prisoners.AddToCounts(_bandit, 9);
            _seller.PrisonRoster.AddToCounts(_bandit, 3);
            var remainder = RunSelected(_seller.PrisonRoster.CloneRosterData());
            Assert.Equal(10, _land.Prisoners.TotalManCount);
            Assert.Equal(2, remainder.TotalManCount);
            Assert.Equal(2, _seller.PrisonRoster.TotalManCount);
        }

        [Fact]
        public void SelectedSaleAcceptsLiveRosterWithoutRemovingPrisonersTwice()
        {
            _seller.PrisonRoster.AddToCounts(_bandit, 3);
            RunSelected(_seller.PrisonRoster);
            Assert.Equal(3, _land.Prisoners.TotalManCount);
            Assert.Equal(0, _seller.PrisonRoster.TotalManCount);
        }

        [Fact]
        public void AllPrisonerSalesRemainSupported()
        {
            _seller.PrisonRoster.AddToCounts(_bandit, 3);
            InvokePrefix(typeof(SellPrisonersPatch), new object[] { _seller });
            Assert.Equal(3, _land.Prisoners.TotalManCount);
            Assert.Equal(0, _seller.PrisonRoster.TotalManCount);
        }

        [Fact]
        public void DisabledBuyingAndInsufficientGoldLeaveSaleUntouched()
        {
            _seller.PrisonRoster.AddToCounts(_bandit, 2);
            _land.BuySlaves = false;
            Assert.Equal(2, RunSelected(_seller.PrisonRoster).TotalManCount);
            _land.BuySlaves = true;
            Hero.MainHero.Gold = 9;
            Assert.Equal(2, RunSelected(_seller.PrisonRoster).TotalManCount);
            Assert.Equal(0, _land.Prisoners.TotalManCount);
            Assert.Equal(9, Hero.MainHero.Gold);
        }

        [Fact]
        public void EligibilityExcludesHeroesAndHonorsAllPrisonersSetting()
        {
            var regular = new CharacterObject();
            var hero = new CharacterObject { IsHero = true, Occupation = Occupation.Bandit };
            _seller.PrisonRoster.AddToCounts(regular, 1);
            _seller.PrisonRoster.AddToCounts(hero, 1);
            Assert.Equal(2, RunSelected(_seller.PrisonRoster).TotalManCount);
            EstateConfiguration.AllowAllPrisonersAsSlaves = true;
            Assert.Equal(1, RunSelected(_seller.PrisonRoster).TotalManCount);
            Assert.Equal(1, _land.Prisoners.GetTroopCount(regular));
            Assert.Equal(0, _land.Prisoners.GetTroopCount(hero));
        }

        [Fact]
        public void BothNativeSaleEntryPointsArePatched()
        {
            Assert.Equal("ApplyForAllPrisoners", typeof(SellPrisonersPatch).GetCustomAttribute<HarmonyLib.HarmonyPatch>()!.Method);
            Assert.Equal("ApplyForSelectedPrisoners", typeof(SellSelectedPrisonersPatch).GetCustomAttribute<HarmonyLib.HarmonyPatch>()!.Method);
        }

        [Fact]
        public void MultipleEstatesShareRemainingSaleAndPlayerGold()
        {
            var village = new Village { Settlement = new Settlement() };
            var secondLand = new VillageLand { BuySlaves = true, OwnedPlots = 1 };
            _seller.MobileParty.CurrentSettlement.BoundVillages.Add(village);
            AgricultureEstateBehavior.VillageLands.Add(village.Settlement, secondLand);
            _land.Prisoners.AddToCounts(_bandit, 9);
            _seller.PrisonRoster.AddToCounts(_bandit, 4);
            Hero.MainHero.Gold = 20;

            var remainder = RunSelected(_seller.PrisonRoster);

            Assert.Equal(10, _land.Prisoners.TotalManCount);
            Assert.Equal(1, secondLand.Prisoners.TotalManCount);
            Assert.Equal(2, remainder.TotalManCount);
            Assert.Equal(2, _seller.PrisonRoster.TotalManCount);
            Assert.Equal(0, Hero.MainHero.Gold);
            Assert.Equal(20, _seller.LeaderHero.Gold);
        }

        [Fact]
        public void NullSettlementAndPlayerSellersAreSkipped()
        {
            Assert.False(SellPrisonersPatch.CanBuyFrom(null));
            Assert.False(SellPrisonersPatch.CanBuyFrom(new PartyBase()));
            Assert.False(SellPrisonersPatch.CanBuyFrom(new PartyBase { MobileParty = MobileParty.MainParty }));
        }

        private TroopRoster RunSelected(TroopRoster offered)
        {
            var args = new object[] { _seller, offered };
            InvokePrefix(typeof(SellSelectedPrisonersPatch), args);
            return (TroopRoster)args[1];
        }

        private static void InvokePrefix(Type patch, object[] args) =>
            patch.GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args);
    }
}
