using FluentValidation;
using Entities.Models.CostEstimates;

namespace CQRS.CostEstimates.AddCostEstimateItem
{
    public class AddCostEstimateItemCommandValidator : AbstractValidator<AddCostEstimateItemCommand>
    {
        public AddCostEstimateItemCommandValidator()
        {
            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");

            RuleFor(x => x.GroupId)
                .NotEmpty().WithMessage("Group ID is required");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage("Order must be non-negative");

            RuleFor(x => x.RelationType)
                .IsInEnum().WithMessage("Invalid relation type");

            RuleFor(x => x.ParentItemId)
                .NotEmpty()
                .When(x => x.RelationType != ItemRelationType.None)
                .WithMessage("Parent item ID is required for options and components");

            RuleFor(x => x.ParentItemId)
                .Null()
                .When(x => x.RelationType == ItemRelationType.None)
                .WithMessage("Parent item ID must be null for main items");
        }
    }
}
