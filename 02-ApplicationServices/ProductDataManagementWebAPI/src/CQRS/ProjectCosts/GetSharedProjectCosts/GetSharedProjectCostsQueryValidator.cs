using FluentValidation;

namespace CQRS.ProjectCosts.GetSharedProjectCosts
{
    public class GetSharedProjectCostsQueryValidator : AbstractValidator<GetSharedProjectCostsQuery>
    {
        public GetSharedProjectCostsQueryValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("ProjectId is required");
        }
    }
}
