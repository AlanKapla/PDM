using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.GenerateScheduleFromEstimateAI
{
    public sealed record GenerateScheduleFromEstimateAICommand : WorkScheduleCommandBase, IRequestCommand<WorkScheduleDetailsWeb>
    {
        public DateTime OverallStartDate { get; init; }
        public DateTime OverallEndDate { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
