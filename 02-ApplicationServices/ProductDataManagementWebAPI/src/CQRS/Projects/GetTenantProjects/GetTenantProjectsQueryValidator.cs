using FluentValidation;

namespace CQRS.Projects.GetTenantProjects
{
    public class GetTenantProjectsQueryValidator : AbstractValidator<GetTenantProjectsQuery>
    {
        public GetTenantProjectsQueryValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId jest wymagane");
        }
    }
}