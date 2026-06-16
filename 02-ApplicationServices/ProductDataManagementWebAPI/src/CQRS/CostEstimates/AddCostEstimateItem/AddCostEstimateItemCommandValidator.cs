using CQRS.Extensions;
using Entities.Models.CostEstimates;
using FluentValidation;

namespace CQRS.CostEstimates.AddCostEstimateItem
{
    public sealed class AddCostEstimateItemCommandValidator : AbstractValidator<AddCostEstimateItemCommand>
    {
        public AddCostEstimateItemCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.GroupId).RequiredId();
            RuleFor(x => x.Order).NonNegativeOrder();

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
