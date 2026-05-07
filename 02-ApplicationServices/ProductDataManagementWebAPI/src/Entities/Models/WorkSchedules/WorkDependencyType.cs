namespace Entities.Models.WorkSchedules
{
    public enum WorkDependencyType
    {
        /// <summary>Successor cannot start until predecessor finishes (most common, default)</summary>
        FinishToStart = 0,

        /// <summary>Successor cannot start until predecessor starts</summary>
        StartToStart = 1,

        /// <summary>Successor cannot finish until predecessor finishes</summary>
        FinishToFinish = 2,

        /// <summary>Successor cannot finish until predecessor starts (rare)</summary>
        StartToFinish = 3
    }
}
