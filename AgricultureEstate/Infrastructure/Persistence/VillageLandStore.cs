using System;
using System.Collections.Generic;
using AgricultureEstate.Application;
using AgricultureEstate.Domain;

namespace AgricultureEstate.Infrastructure
{
    internal sealed class VillageLandStore : IEstateStore
    {
        private readonly VillageLand _land;

        public VillageLandStore(VillageLand land) => _land = land ?? throw new ArgumentNullException(nameof(land));

        public EstateState Load() => new EstateState
        {
            AvailablePlots = _land.AvaliblePlots,
            OwnedPlots = _land.OwnedPlots,
            AvailableUndevelopedPlots = _land.AvalibleUndevelopedPlots,
            OwnedUndevelopedPlots = _land.OwnedUndevelopedPlots,
            PrisonerCount = _land.Prisoners.TotalManCount,
            PatrolLevel = _land.PatrolLevel,
            StorageCapacity = _land.StorageCapacity,
            CurrentProject = _land.CurrentProject,
            ProjectQueue = new Queue<string>(_land.ProjectQueue ?? new Queue<string>()),
            ProjectProgress = _land.ProjectProgress
        };

        public void Save(EstateState estate)
        {
            _land.AvaliblePlots = estate.AvailablePlots;
            _land.OwnedPlots = estate.OwnedPlots;
            _land.AvalibleUndevelopedPlots = estate.AvailableUndevelopedPlots;
            _land.OwnedUndevelopedPlots = estate.OwnedUndevelopedPlots;
            _land.PatrolLevel = estate.PatrolLevel;
            _land.StorageCapacity = estate.StorageCapacity;
            _land.CurrentProject = estate.CurrentProject;
            _land.ProjectQueue = new Queue<string>(estate.ProjectQueue);
            _land.ProjectProgress = estate.ProjectProgress;
        }
    }
}
