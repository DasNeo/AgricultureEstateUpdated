using System.Collections.Generic;

namespace AgricultureEstate.Domain
{
    // A detached state snapshot. Game-specific persistence stays in the adapter.
    public sealed class EstateState
    {
        public int AvailablePlots { get; set; }
        public int OwnedPlots { get; set; }
        public int AvailableUndevelopedPlots { get; set; }
        public int OwnedUndevelopedPlots { get; set; }
        public int PrisonerCount { get; set; }
        public int PatrolLevel { get; set; }
        public int StorageCapacity { get; set; }
        public string CurrentProject { get; set; } = EstateProjects.None;
        public Queue<string> ProjectQueue { get; set; } = new Queue<string>();
        public int ProjectProgress { get; set; }

        public int CountProjects(string project)
        {
            int count = CurrentProject == project ? 1 : 0;
            foreach (string queuedProject in ProjectQueue)
                if (queuedProject == project)
                    count++;
            return count;
        }

        public void CancelCurrentProject()
        {
            ProjectProgress = 0;
            CurrentProject = ProjectQueue.Count > 0 ? ProjectQueue.Dequeue() : EstateProjects.None;
        }
    }
}
