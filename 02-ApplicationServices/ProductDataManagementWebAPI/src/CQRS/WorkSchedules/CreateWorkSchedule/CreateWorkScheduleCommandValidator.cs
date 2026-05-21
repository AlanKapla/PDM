using CQRS.Extensions;
using Entities.Models.WorkSchedules;
using Entities.Models.CostEstimates;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public sealed class CreateWorkScheduleCommandValidator : AbstractValidator<CreateWorkScheduleCommand>
    {
        public CreateWorkScheduleCommandValidator(
            IRepository<CostEstimate> costEstimateRepo,
            IRepository<WorkSchedule> workScheduleRepo)
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);

            When(x => x.CostEstimateId.HasValue, () =>
            {
                RuleFor(x => x.CostEstimateId)
                    .MustAsync(async (command, id, cancellationToken) =>
                        await costEstimateRepo.AnyAsync(
                            ce => ce.Id == id!.Value
                                  && ce.TenantId == command.TenantId
                                  && ce.ProjectId == command.ProjectId,
                            cancellationToken))
                    .WithMessage("Cost estimate not found or does not belong to this project");

                RuleFor(x => x.CostEstimateId)
                    .MustAsync(async (command, id, cancellationToken) =>
                        !await workScheduleRepo.AnyAsync(
                            ws => ws.TenantId == command.TenantId
                                  && ws.ProjectId == command.ProjectId
                                  && ws.CostEstimateId == id!.Value,
                            cancellationToken))
                    .WithMessage("A work schedule for this cost estimate already exists");
            });
        }
    }
}
