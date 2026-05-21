using CQRS.Extensions;
using FluentValidation;

namespace CQRS.CostEstimates.UpdateCostEstimate
{
    public sealed class UpdateCostEstimateCommandValidator : AbstractValidator<UpdateCostEstimateCommand>
    {
        public UpdateCostEstimateCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Cost estimate name is required")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");
        }
    }
}
