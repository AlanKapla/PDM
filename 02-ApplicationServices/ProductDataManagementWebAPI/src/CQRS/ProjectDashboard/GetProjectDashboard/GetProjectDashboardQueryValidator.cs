using FluentValidation;

namespace CQRS.ProjectDashboard.GetProjectDashboard
{
    public sealed class GetProjectDashboardQueryValidator : AbstractValidator<GetProjectDashboardQuery>
    {
        public GetProjectDashboardQueryValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Project ID is required.");

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("Tenant ID is required.");
        }
    }
}
