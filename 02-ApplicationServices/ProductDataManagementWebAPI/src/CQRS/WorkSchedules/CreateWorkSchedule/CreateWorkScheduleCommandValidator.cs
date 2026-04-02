using CQRS.WorkSchedules.Shared;
using Entities.Models;
using Entities.Models.CostEstimates;
using FluentValidation;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public class CreateWorkScheduleCommandValidator : WorkScheduleCommandValidatorBase<CreateWorkScheduleCommand>
    {
        public CreateWorkScheduleCommandValidator(
            IRepository<ProjectMember> projectMemberRepo,
            IRepository<CostEstimate> costEstimateRepo,
            IRepository<WorkSchedule> workScheduleRepo)
            : base(projectMemberRepo)
        {
            When(x => x.CostEstimateId.HasValue, () =>
            {
                RuleFor(x => x.CostEstimateId)
                    .MustAsync(async (command, id, cancellationToken) =>
                        await costEstimateRepo.AnyAsync(
                            ce => ce.Id == id!.Value
                                  && ce.TenantId == command.TenantId
                                  && ce.ProjectId == command.ProjectId
                                  && !ce.IsDeleted,
                            cancellationToken))
                    .WithMessage("Cost estimate not found or does not belong to this project");

                RuleFor(x => x.CostEstimateId)
                    .MustAsync(async (command, id, cancellationToken) =>
                        !await workScheduleRepo.AnyAsync(
                            ws => ws.CostEstimateId == id!.Value
                                  && ws.TenantId == command.TenantId
                                  && ws.ProjectId == command.ProjectId
                                  && !ws.IsDeleted,
                            cancellationToken))
                    .WithMessage("A work schedule for this cost estimate already exists");
            });
        }

        protected override Expression<Func<CreateWorkScheduleCommand, string>> GetNameSelector()
        {
            return cmd => cmd.Name;
        }

        protected override Func<CreateWorkScheduleCommand, Guid> GetTenantIdSelector()
        {
            return cmd => cmd.TenantId;
        }

        protected override Func<CreateWorkScheduleCommand, Guid> GetProjectIdSelector()
        {
            return cmd => cmd.ProjectId;
        }

        protected override Expression<Func<CreateWorkScheduleCommand, IEnumerable<WorkScheduleStageDto>?>> GetStagesSelector()
        {
            return cmd => cmd.Stages;
        }

        protected override Func<CreateWorkScheduleCommand, IEnumerable<WorkScheduleStageDto>?> GetStagesSelectorFunc()
        {
            return cmd => cmd.Stages;
        }
    }
}
