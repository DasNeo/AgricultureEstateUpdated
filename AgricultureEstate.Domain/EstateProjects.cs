namespace AgricultureEstate.Domain
{
    public static class EstateProjects
    {
        // Keep these identifiers stable: existing saves contain these strings.
        public const string None = "None";
        public const string LandClearance = "Land Clearance";
        public const string IncreasePatrols = "Increase Patrols";
        public const string ExpandStorehouse = "Expand Storehouse";
        public const int MaximumPatrolLevel = 8;

        // Returns whether land was actually cleared, for the game-specific perk effect.
        public static bool Advance(EstateState estate, int durationHours)
        {
            if (estate.CurrentProject == None)
                return false;

            estate.ProjectProgress++;
            if (estate.ProjectProgress < durationHours)
                return false;

            bool clearedLand = false;
            switch (estate.CurrentProject)
            {
                case LandClearance:
                    if (estate.OwnedUndevelopedPlots > 0)
                    {
                        estate.OwnedUndevelopedPlots--;
                        estate.OwnedPlots++;
                        clearedLand = true;
                    }
                    break;
                case IncreasePatrols:
                    if (estate.PatrolLevel < MaximumPatrolLevel)
                        estate.PatrolLevel++;
                    break;
                case ExpandStorehouse:
                    estate.StorageCapacity += 500;
                    break;
            }

            estate.CancelCurrentProject();
            return clearedLand;
        }
    }
}
