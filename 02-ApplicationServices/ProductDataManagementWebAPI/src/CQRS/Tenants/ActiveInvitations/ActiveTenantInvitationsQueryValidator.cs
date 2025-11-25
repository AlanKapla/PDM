using Business.Interfaces.Model;
using FluentValidation;

namespace CQRS.Tenants.ActiveInvitations
{
    public class ActiveTenantInvitationsQueryValidator : AbstractValidator<ActiveTenantInvitationsQuery>
    {
        public ActiveTenantInvitationsQueryValidator(ICurrentUser currentUser)
        {
            RuleFor(_ => _)
                .Must(_ => currentUser.IsAuthenticated)
                .WithMessage("User must be authenticated.");

            RuleFor(_ => currentUser.Email)
                .NotEmpty()
                .WithMessage("Authenticated user must have an email.");
        }
    }
}
