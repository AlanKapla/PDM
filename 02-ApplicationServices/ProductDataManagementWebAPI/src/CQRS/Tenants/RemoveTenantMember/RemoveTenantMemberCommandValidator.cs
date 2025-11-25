using FluentValidation;
using Business.Interfaces.Model;

namespace CQRS.Tenants.RemoveTenantMember
{
    public class RemoveTenantMemberCommandValidator : AbstractValidator<RemoveTenantMemberCommand>
    {
        public RemoveTenantMemberCommandValidator(ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId)
                .NotEmpty()
                .Must(userId => userId != currentUser.Id)
                .WithMessage("You cannot remove yourself from the tenant.");
        }
    }
}
