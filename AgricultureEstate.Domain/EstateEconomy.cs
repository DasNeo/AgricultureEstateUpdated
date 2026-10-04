using System;

namespace AgricultureEstate.Domain
{
    public static class EstateEconomy
    {
        public static int CalculateProjectCost(int baseCost, bool contractors) =>
            contractors ? (int)(baseCost * 0.85m) : baseCost;

        public static float CalculateRent(int ownedPlots, int prisonerCount, float tradeTax, float rentScale, bool isLooted)
        {
            if (ownedPlots <= 0 || isLooted)
                return 0f;
            float unusedFraction = Math.Max(0f, (float)((10.0 * ownedPlots - prisonerCount) / (10.0 * ownedPlots)));
            return (float)(tradeTax * (double)unusedFraction / 100.0) * ownedPlots * rentScale;
        }

        public static float CalculateDecline(int patrolLevel, bool mountedPatrols, float modifier) =>
            (float)((5.0 - 0.5 * patrolLevel) * (mountedPatrols ? 0.800000011920929 : 1.0)) * modifier;

        public static float CalculateRevoltRisk(int prisonerCount, float militia, int patrolLevel) =>
            prisonerCount < 5.0 * militia ? 0f :
                (10.0 * militia < prisonerCount ? 3f : 1f) * (float)(1.0 - 0.10000000149011612 * patrolLevel);
    }
}
