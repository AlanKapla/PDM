using FluentValidation;

namespace CQRS.ProjectCosts.GetProjectCosts
{
    /// <summary>
    /// Validator dla GetProjectCostsQuery
    /// </summary>
    public class GetProjectCostsQueryValidator : AbstractValidator<GetProjectCostsQuery>
    {
        public GetProjectCostsQueryValidator()
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
