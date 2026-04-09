using FluentValidation;

namespace CQRS.CostTrackers.GetCostTrackerByProject
{
    public sealed class GetCostTrackerByProjectQueryValidator : AbstractValidator<GetCostTrackerByProjectQuery>
    {
        public GetCostTrackerByProjectQueryValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project ID is required.");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Tenant ID is required.");
        }
    }
}
