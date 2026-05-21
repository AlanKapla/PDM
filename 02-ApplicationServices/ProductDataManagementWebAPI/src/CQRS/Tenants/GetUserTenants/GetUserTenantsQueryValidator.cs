using Business.Interfaces.Model;
using FluentValidation;

namespace CQRS.Tenants.GetUserTenants
{
    public sealed class GetUserTenantsQueryValidator : AbstractValidator<GetUserTenantsQuery>
    {
        public GetUserTenantsQueryValidator(ICurrentUser currentUser)
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
