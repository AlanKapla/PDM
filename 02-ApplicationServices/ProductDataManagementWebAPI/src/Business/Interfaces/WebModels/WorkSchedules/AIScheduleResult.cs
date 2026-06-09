using Entities.Models.WorkSchedules;

namespace Business.Interfaces.WebModels.WorkSchedules
{
    public sealed record AIScheduleResult
    {
        public List<WorkPeriodResult> Periods { get; init; } = [];
        public List<WorkDependencyResult> Dependencies { get; init; } = [];
    }

    public sealed record WorkPeriodResult
    {
        public Guid WorkScheduleStageWorkId { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
    }

    public sealed record WorkDependencyResult
    {
        public Guid PredecessorWorkId { get; init; }
        public Guid SuccessorWorkId { get; init; }
        public WorkDependencyType DependencyType { get; init; }
        public int LagDays { get; init; }
    }
}
