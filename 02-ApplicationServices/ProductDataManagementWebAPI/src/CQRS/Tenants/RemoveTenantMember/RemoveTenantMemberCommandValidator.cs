using Business.Interfaces.Model;
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Tenants.RemoveTenantMember
{
    public sealed class RemoveTenantMemberCommandValidator : AbstractValidator<RemoveTenantMemberCommand>
    {
        public RemoveTenantMemberCommandValidator(ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.UserId).RequiredId();
            RuleFor(x => x.UserId).NotCurrentUser(currentUser);
        }
    }
}
