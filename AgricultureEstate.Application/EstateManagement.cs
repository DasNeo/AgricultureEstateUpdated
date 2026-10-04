using System;
using AgricultureEstate.Domain;

namespace AgricultureEstate.Application
{
    public sealed class EstateManagement
    {
        private readonly IEstateStore _store;
        private readonly IEstateAccount _account;
        private readonly EstatePrices _prices;

        public EstateManagement(IEstateStore store, IEstateAccount account, EstatePrices prices)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _prices = prices ?? throw new ArgumentNullException(nameof(prices));
            if (prices.PlotBuyPrice < 0 || prices.PlotSellPrice < 0 || prices.UndevelopedPlotBuyPrice < 0 ||
                prices.UndevelopedPlotSellPrice < 0 || prices.ProjectCost < 0)
                throw new ArgumentOutOfRangeException(nameof(prices));
        }

        public EstateCommandResult BuyPlot(bool undeveloped)
        {
            EstateState estate = _store.Load();
            int price = undeveloped ? _prices.UndevelopedPlotBuyPrice : _prices.PlotBuyPrice;
            if (_account.Gold < price)
                return EstateCommandResult.NotEnoughGold;
            if ((undeveloped ? estate.AvailableUndevelopedPlots : estate.AvailablePlots) <= 0)
                return EstateCommandResult.NoAvailablePlots;

            if (undeveloped)
            {
                estate.AvailableUndevelopedPlots--;
                estate.OwnedUndevelopedPlots++;
            }
            else
            {
                estate.AvailablePlots--;
                estate.OwnedPlots++;
            }
            _account.Spend(price);
            _store.Save(estate);
            return EstateCommandResult.Success;
        }

        public EstateCommandResult SellPlot(bool undeveloped)
        {
            EstateState estate = _store.Load();
            if ((undeveloped ? estate.OwnedUndevelopedPlots : estate.OwnedPlots) <= 0)
                return EstateCommandResult.NoOwnedPlots;
            if (undeveloped && estate.CountProjects(EstateProjects.LandClearance) >= estate.OwnedUndevelopedPlots)
                return EstateCommandResult.LandReservedForClearance;
            if (!undeveloped && estate.PrisonerCount > (estate.OwnedPlots - 1) * 10)
                return EstateCommandResult.PrisonerCapacityExceeded;

            if (undeveloped)
            {
                estate.AvailableUndevelopedPlots++;
                estate.OwnedUndevelopedPlots--;
            }
            else
            {
                estate.AvailablePlots++;
                estate.OwnedPlots--;
            }
            _account.Receive(undeveloped ? _prices.UndevelopedPlotSellPrice : _prices.PlotSellPrice);
            _store.Save(estate);
            return EstateCommandResult.Success;
        }

        public EstateCommandResult StartProject(string project, int stewardSkill, bool contractors)
        {
            if (project != EstateProjects.LandClearance && project != EstateProjects.IncreasePatrols && project != EstateProjects.ExpandStorehouse)
                return EstateCommandResult.InvalidProject;
            EstateState estate = _store.Load();
            int cost = EstateEconomy.CalculateProjectCost(_prices.ProjectCost, contractors);
            if (_account.Gold < cost)
                return EstateCommandResult.NotEnoughGold;
            if (estate.ProjectQueue.Count >= QueueLimit(stewardSkill))
                return EstateCommandResult.QueueFull;
            if (project == EstateProjects.LandClearance && estate.CountProjects(project) >= estate.OwnedUndevelopedPlots)
                return EstateCommandResult.NotEnoughUndevelopedPlots;
            if (project == EstateProjects.IncreasePatrols && estate.PatrolLevel + estate.CountProjects(project) >= EstateProjects.MaximumPatrolLevel)
                return EstateCommandResult.MaximumPatrolLevel;

            if (estate.CurrentProject == EstateProjects.None)
                estate.CurrentProject = project;
            else
                estate.ProjectQueue.Enqueue(project);
            _account.Spend(cost);
            _store.Save(estate);
            return EstateCommandResult.Success;
        }

        public void CancelProject()
        {
            EstateState estate = _store.Load();
            estate.CancelCurrentProject();
            _store.Save(estate);
        }

        public bool AdvanceProject(int durationHours)
        {
            if (durationHours <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationHours));
            EstateState estate = _store.Load();
            bool clearedLand = EstateProjects.Advance(estate, durationHours);
            _store.Save(estate);
            return clearedLand;
        }

        public static int QueueLimit(int stewardSkill) => 10 + Math.Max(0, stewardSkill) / 10;
    }
}
