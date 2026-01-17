using FluentValidation;
using Business.Interfaces.Model;

namespace CQRS.Tenants.RemoveTenantMember
{
    public class RemoveTenantMemberCommandValidator : AbstractValidator<RemoveTenantMemberCommand>
    {
        private readonly ICurrentUser currentUser;

        public RemoveTenantMemberCommandValidator(ICurrentUser currentUser)
        {
            this.currentUser = currentUser;

            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId is required");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required");

            RuleFor(x => x.UserId)
                .Must(userId => userId != currentUser.Id)
                .WithMessage("Cannot remove yourself from the tenant");
        }
    }
}
