using AgricultureEstate.Application;
using AgricultureEstate.Infrastructure;

namespace AgricultureEstate
{
    internal static class EstateComposition
    {
        public static EstateManagement CreateManagement(VillageLand land) => new EstateManagement(
            new VillageLandStore(land), new BannerlordEstateAccount(land), new EstatePrices
            {
                PlotBuyPrice = EstateConfiguration.PlotBuyPrice,
                PlotSellPrice = EstateConfiguration.PlotSellPrice,
                UndevelopedPlotBuyPrice = EstateConfiguration.UndevelopedPlotBuyPrice,
                UndevelopedPlotSellPrice = EstateConfiguration.UndevelopedPlotSellPrice,
                ProjectCost = EstateConfiguration.ProjectCost
            });
    }
}
