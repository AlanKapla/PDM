using FluentValidation;

namespace CQRS.CostEstimates.GetCostEstimates
{
    /// <summary>
    /// Validator for GetCostEstimatesQuery
    /// </summary>
    public class GetCostEstimatesQueryValidator : AbstractValidator<GetCostEstimatesQuery>
    {
        public GetCostEstimatesQueryValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("ProjectId is required");

            RuleFor(x => x.Scope)
                .IsInEnum()
                .WithMessage("Invalid ResourceScope value");
        }
    }
}
