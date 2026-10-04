namespace AgricultureEstate.Application
{
    public enum EstateCommandResult
    {
        Success,
        NotEnoughGold,
        NoAvailablePlots,
        NoOwnedPlots,
        PrisonerCapacityExceeded,
        LandReservedForClearance,
        QueueFull,
        NotEnoughUndevelopedPlots,
        MaximumPatrolLevel,
        InvalidProject
    }
}
