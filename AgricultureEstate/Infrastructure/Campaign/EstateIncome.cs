using AgricultureEstate.Domain;
using TaleWorlds.CampaignSystem.Settlements;

namespace AgricultureEstate
{
    internal static class EstateIncome
    {
        public static float CalculateRent(VillageLand land) => EstateEconomy.CalculateRent(
            land.OwnedPlots, land.Prisoners.TotalManCount, land.Village?.TradeTaxAccumulated ?? 0f,
            EstateConfiguration.LandRentScale, land.Village?.VillageState == Village.VillageStates.Looted);
    }
}
