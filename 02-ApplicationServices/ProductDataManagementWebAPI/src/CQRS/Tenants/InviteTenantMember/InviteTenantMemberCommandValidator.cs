using FluentValidation;
using Business.Interfaces.Model;

namespace CQRS.Tenants.InviteTenantMember
{
    public class InviteTenantMemberCommandValidator : AbstractValidator<InviteTenantMemberCommand>
    {
        public InviteTenantMemberCommandValidator(ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .Must(email =>
                {
                    if (string.IsNullOrWhiteSpace(currentUser.Email)) return true; // brak claimu email nie blokuje innych walidacji
                    return !string.Equals(email.Trim(), currentUser.Email.Trim(), StringComparison.OrdinalIgnoreCase);
                })
                .WithMessage("You cannot invite yourself.");
        }
    }
}
