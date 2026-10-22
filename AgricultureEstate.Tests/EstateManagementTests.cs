using System.Collections.Generic;
using AgricultureEstate.Application;
using AgricultureEstate.Domain;
using Xunit;

namespace AgricultureEstate.Tests
{
    public class EstateManagementTests
    {
        [Theory]
        [InlineData(false, 800)]
        [InlineData(true, 400)]
        public void PurchaseUsesCorrectPriceAndConservesPlots(bool undeveloped, int price)
        {
            var adapter = new MemoryAdapter(1000);
            Assert.Equal(EstateCommandResult.Success, Create(adapter).BuyPlot(undeveloped));
            Assert.Equal(1000 - price, adapter.Gold);
            Assert.Equal(1, undeveloped ? adapter.State.OwnedUndevelopedPlots : adapter.State.OwnedPlots);
            Assert.Equal(9, undeveloped ? adapter.State.AvailableUndevelopedPlots : adapter.State.AvailablePlots);
        }

        [Theory]
        [InlineData(false, 150)]
        [InlineData(true, 75)]
        public void SaleUsesCorrectPriceAndConservesPlots(bool undeveloped, int price)
        {
            var adapter = new MemoryAdapter(0);
            adapter.State.OwnedPlots = adapter.State.OwnedUndevelopedPlots = 1;
            var management = new EstateManagement(adapter, adapter, new EstatePrices { PlotSellPrice = 150, UndevelopedPlotSellPrice = 75 });
            Assert.Equal(EstateCommandResult.Success, management.SellPlot(undeveloped));
            Assert.Equal(price, adapter.Gold);
            Assert.Equal(0, undeveloped ? adapter.State.OwnedUndevelopedPlots : adapter.State.OwnedPlots);
            Assert.Equal(11, undeveloped ? adapter.State.AvailableUndevelopedPlots : adapter.State.AvailablePlots);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RejectedPurchaseHasNoSideEffects(bool undeveloped)
        {
            var adapter = new MemoryAdapter(0);
            Assert.Equal(EstateCommandResult.NotEnoughGold, Create(adapter).BuyPlot(undeveloped));
            Assert.Equal(0, adapter.Transactions);
            Assert.Equal(0, adapter.Saves);
            adapter.Gold = 1000;
            adapter.State.AvailablePlots = adapter.State.AvailableUndevelopedPlots = 0;
            Assert.Equal(EstateCommandResult.NoAvailablePlots, Create(adapter).BuyPlot(undeveloped));
            Assert.Equal(0, adapter.Transactions);
        }

        [Fact]
        public void SaleProtectsPrisonerCapacityAndClearanceReservations()
        {
            var adapter = new MemoryAdapter(0);
            adapter.State.OwnedPlots = 2;
            adapter.State.PrisonerCount = 11;
            Assert.Equal(EstateCommandResult.PrisonerCapacityExceeded, Create(adapter).SellPlot(false));
            adapter.State.OwnedUndevelopedPlots = 2;
            adapter.State.CurrentProject = EstateProjects.LandClearance;
            adapter.State.ProjectQueue.Enqueue(EstateProjects.LandClearance);
            Assert.Equal(EstateCommandResult.LandReservedForClearance, Create(adapter).SellPlot(true));
            Assert.Equal(0, adapter.Transactions);
            Assert.Equal(0, adapter.Saves);
        }

        [Theory]
        [InlineData(false, 20000)]
        [InlineData(true, 17000)]
        public void ProjectUsesProjectCostAndContractorsDiscount(bool contractors, int price)
        {
            var adapter = new MemoryAdapter(25000);
            Assert.Equal(EstateCommandResult.Success, Create(adapter).StartProject(EstateProjects.ExpandStorehouse, 0, contractors));
            Assert.Equal(25000 - price, adapter.Gold);
            Assert.Equal(EstateProjects.ExpandStorehouse, adapter.State.CurrentProject);
        }

        [Fact]
        public void StewardSkillIncreasesQueueLimit()
        {
            var adapter = new MemoryAdapter(100000);
            adapter.State.CurrentProject = EstateProjects.ExpandStorehouse;
            for (int i = 0; i < 10; i++) adapter.State.ProjectQueue.Enqueue(EstateProjects.ExpandStorehouse);
            Assert.Equal(EstateCommandResult.QueueFull, Create(adapter).StartProject(EstateProjects.ExpandStorehouse, 0, false));
            Assert.Equal(0, adapter.Transactions);
            Assert.Equal(EstateCommandResult.Success, Create(adapter).StartProject(EstateProjects.ExpandStorehouse, 10, false));
        }

        [Fact]
        public void UpgradeLimitsIncludeActiveAndQueuedProjects()
        {
            var adapter = new MemoryAdapter(50000);
            adapter.State.PatrolLevel = 6;
            adapter.State.CurrentProject = EstateProjects.IncreasePatrols;
            adapter.State.ProjectQueue.Enqueue(EstateProjects.IncreasePatrols);
            Assert.Equal(EstateCommandResult.MaximumPatrolLevel, Create(adapter).StartProject(EstateProjects.IncreasePatrols, 0, false));
            adapter.State.OwnedUndevelopedPlots = 1;
            adapter.State.CurrentProject = EstateProjects.LandClearance;
            Assert.Equal(EstateCommandResult.NotEnoughUndevelopedPlots, Create(adapter).StartProject(EstateProjects.LandClearance, 0, false));
            Assert.Equal(0, adapter.Transactions);
        }

        [Fact]
        public void CancellationStartsNextProjectAtZeroProgressWithoutRefund()
        {
            var adapter = new MemoryAdapter(0);
            adapter.State.CurrentProject = EstateProjects.LandClearance;
            adapter.State.ProjectProgress = 200;
            adapter.State.ProjectQueue.Enqueue(EstateProjects.IncreasePatrols);
            Create(adapter).CancelProject();
            Assert.Equal(EstateProjects.IncreasePatrols, adapter.State.CurrentProject);
            Assert.Equal(0, adapter.State.ProjectProgress);
            Assert.Empty(adapter.State.ProjectQueue);
            Assert.Equal(0, adapter.Transactions);
        }

        [Fact]
        public void ClearanceCompletesAtConfiguredDurationAndSignalsPerkEffect()
        {
            var adapter = new MemoryAdapter(0);
            adapter.State.CurrentProject = EstateProjects.LandClearance;
            adapter.State.OwnedUndevelopedPlots = 1;
            adapter.State.ProjectProgress = 22;
            Assert.False(Create(adapter).AdvanceProject(24));
            Assert.Equal(1, adapter.State.OwnedUndevelopedPlots);
            Assert.True(Create(adapter).AdvanceProject(24));
            Assert.Equal(1, adapter.State.OwnedPlots);
            Assert.Equal(0, adapter.State.OwnedUndevelopedPlots);
            Assert.Equal(0, adapter.State.ProjectProgress);
            Assert.Equal(EstateProjects.None, adapter.State.CurrentProject);
        }

        [Fact]
        public void StorehouseCompletionStartsQueueWithStableIdentifier()
        {
            var adapter = new MemoryAdapter(0);
            adapter.State.CurrentProject = EstateProjects.ExpandStorehouse;
            adapter.State.ProjectQueue.Enqueue(EstateProjects.IncreasePatrols);
            Create(adapter).AdvanceProject(1);
            Assert.Equal(1000, adapter.State.StorageCapacity);
            Assert.Equal(EstateProjects.IncreasePatrols, adapter.State.CurrentProject);
            Assert.Equal(0, adapter.State.ProjectProgress);
        }

        [Fact]
        public void IdleAndInvalidProjectsHaveNoGameplayEffects()
        {
            var adapter = new MemoryAdapter(50000);
            Assert.False(Create(adapter).AdvanceProject(240));
            Assert.Equal(0, adapter.State.ProjectProgress);
            Assert.Equal(EstateCommandResult.InvalidProject, Create(adapter).StartProject("Unknown", 0, false));
            Assert.Equal(0, adapter.Transactions);
        }

        private static EstateManagement Create(MemoryAdapter adapter) => new EstateManagement(adapter, adapter, new EstatePrices());

        [Fact]
        public void ExactDiscountedBalanceCanAffordProject()
        {
            var adapter = new MemoryAdapter(17000);
            Assert.Equal(EstateCommandResult.Success, Create(adapter).StartProject(EstateProjects.ExpandStorehouse, 0, true));
            Assert.Equal(0, adapter.Gold);
        }

        [Fact]
        public void FailedProjectHasNoSideEffects()
        {
            var adapter = new MemoryAdapter(19999);
            Assert.Equal(EstateCommandResult.NotEnoughGold, Create(adapter).StartProject(EstateProjects.ExpandStorehouse, 0, false));
            Assert.Equal(EstateProjects.None, adapter.State.CurrentProject);
            Assert.Equal(0, adapter.Transactions);
            Assert.Equal(0, adapter.Saves);
        }

        [Fact]
        public void CompletedPatrolUpgradeDoesNotExceedCap()
        {
            var adapter = new MemoryAdapter(0);
            adapter.State.PatrolLevel = 8;
            adapter.State.CurrentProject = EstateProjects.IncreasePatrols;
            Create(adapter).AdvanceProject(1);
            Assert.Equal(8, adapter.State.PatrolLevel);
            Assert.Equal(EstateProjects.None, adapter.State.CurrentProject);
        }

        private sealed class MemoryAdapter : IEstateStore, IEstateAccount
        {
            public MemoryAdapter(int gold) => Gold = gold;
            public int Gold { get; set; }
            public int Transactions { get; private set; }
            public int Saves { get; private set; }
            public EstateState State { get; private set; } = new EstateState { AvailablePlots = 10, AvailableUndevelopedPlots = 10, StorageCapacity = 500 };
            public void Spend(int amount) { Gold -= amount; Transactions++; }
            public void Receive(int amount) { Gold += amount; Transactions++; }
            public EstateState Load() => new EstateState
            {
                AvailablePlots = State.AvailablePlots, AvailableUndevelopedPlots = State.AvailableUndevelopedPlots,
                OwnedPlots = State.OwnedPlots, OwnedUndevelopedPlots = State.OwnedUndevelopedPlots,
                PrisonerCount = State.PrisonerCount, PatrolLevel = State.PatrolLevel, StorageCapacity = State.StorageCapacity,
                CurrentProject = State.CurrentProject, ProjectProgress = State.ProjectProgress,
                ProjectQueue = new Queue<string>(State.ProjectQueue)
            };
            public void Save(EstateState estate) { State = estate; Saves++; }
        }
    }
}
