using FluentValidation;

namespace CQRS.CostTrackers.CreateTrackedCost
{
    public sealed class CreateTrackedCostCommandValidator : AbstractValidator<CreateTrackedCostCommand>
    {
        public CreateTrackedCostCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Tracker ID is required.");

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("Tenant ID is required.");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project ID is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(300).WithMessage("Name cannot exceed 300 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

            RuleFor(x => x.Contractor)
                .MaximumLength(300).WithMessage("Contractor cannot exceed 300 characters.");

            RuleFor(x => x)
                .Must(x => x.Net.HasValue || x.Gross.HasValue)
                .WithMessage("At least net or gross value must be provided.")
                .When(x => x.Net.HasValue || x.Gross.HasValue);

            RuleFor(x => x)
                .Must(x => new[] { x.WorkItemLinkId, x.CostEstimateItemId, x.WorkScheduleStageWorkId }.Count(id => id.HasValue) <= 1)
                .WithMessage("Tylko jedno z pól WorkItemLinkId, CostEstimateItemId, WorkScheduleStageWorkId może być podane jednocześnie.")
                .WithName("WorkItemLinkId");
        }
    }
}
