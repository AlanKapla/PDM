using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.GetWorkSchedule
{
    public class GetWorkScheduleQueryValidator : AbstractValidator<GetWorkScheduleQuery>
    {
        public GetWorkScheduleQueryValidator(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required")
                .Must(tenantId => tenantId == currentUser.ActiveTenantId);

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.WorkScheduleId)
                .NotEmpty().WithMessage("WorkScheduleId is required")
                .MustAsync(async (query, workScheduleId, cancellationToken) =>
                {
                    var workSchedule = await workScheduleRepo.GetFirstBySearch(
                        ws => ws.Id == workScheduleId &&
                              ws.TenantId == query.TenantId &&
                              ws.ProjectId == query.ProjectId &&
                              ws.CreatedByUserId == currentUser.Id);
                    return workSchedule != null;
                })
                .WithMessage("Work schedule not found");
        }
    }
}
