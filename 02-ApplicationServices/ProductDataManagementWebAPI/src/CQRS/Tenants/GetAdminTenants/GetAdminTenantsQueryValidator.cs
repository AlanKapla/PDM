using Business.Interfaces.Model;
using FluentValidation;

namespace CQRS.Tenants.GetAdminTenants
{
    public sealed class GetAdminTenantsQueryValidator : AbstractValidator<GetAdminTenantsQuery>
    {
        public GetAdminTenantsQueryValidator(ICurrentUser currentUser)
        {
            RuleFor(x => currentUser.IsAuthenticated)
                .Equal(true)
                .WithMessage("User must be authenticated");

            RuleFor(x => currentUser.Id)
                .NotEqual(Guid.Empty)
                .WithMessage("Invalid user");
        }
    }
}
