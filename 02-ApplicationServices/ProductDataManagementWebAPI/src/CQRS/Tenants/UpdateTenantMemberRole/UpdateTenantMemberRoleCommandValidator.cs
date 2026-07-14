using Business.Interfaces.Model;
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Tenants.UpdateTenantMemberRole
{
    public sealed class UpdateTenantMemberRoleCommandValidator : AbstractValidator<UpdateTenantMemberRoleCommand>
    {
        public UpdateTenantMemberRoleCommandValidator(ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.UserId).RequiredId();
            RuleFor(x => x.UserId).NotCurrentUser(currentUser);
        }
    }
}
