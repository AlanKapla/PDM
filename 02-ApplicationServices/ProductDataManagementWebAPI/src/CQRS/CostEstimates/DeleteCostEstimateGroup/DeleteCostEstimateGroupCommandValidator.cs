using FluentValidation;

namespace CQRS.CostEstimates.DeleteCostEstimateGroup
{
    public class DeleteCostEstimateGroupCommandValidator : AbstractValidator<DeleteCostEstimateGroupCommand>
    {
        public DeleteCostEstimateGroupCommandValidator()
        {
            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");

            RuleFor(x => x.GroupId)
                .NotEmpty().WithMessage("Group ID is required");
        }
    }
}
