using Entities.Models;
using Entities.Models.CostEstimates;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public class CreateWorkScheduleCommandValidator : AbstractValidator<CreateWorkScheduleCommand>
    {
        public CreateWorkScheduleCommandValidator(
            IRepository<CostEstimate> costEstimateRepo,
            IRepository<WorkSchedule> workScheduleRepo)
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);

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
    }
}
