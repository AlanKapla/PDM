using FluentValidation;

namespace CQRS.ProjectCosts.GetProjectUserCosts
{
    public class GetProjectUserCostsQueryValidator : AbstractValidator<GetProjectUserCostsQuery>
    {
        public GetProjectUserCostsQueryValidator()
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
