using Business.Interfaces.Model;
using FluentValidation;

namespace CQRS.Tenants.UpdateTenantMemberRole
{
    public class UpdateTenantMemberRoleCommandValidator : AbstractValidator<UpdateTenantMemberRoleCommand>
    {
        private readonly ICurrentUser currentUser;

        public UpdateTenantMemberRoleCommandValidator(ICurrentUser currentUser)
        {
            this.currentUser = currentUser;

            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId is required");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required");

            RuleFor(x => x.RoleId)
                .NotEmpty()
                .WithMessage("RoleId is required");

            RuleFor(x => x.UserId)
                .Must(x => x != currentUser.Id)
                .WithMessage("Cannot change your own role");
        }
    }
}
