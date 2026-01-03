using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Enums;
using Entities.Models;
using FluentValidation;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.UpdateTenantMemberRole
{
    public class UpdateTenantMemberRoleCommandValidator : AbstractValidator<UpdateTenantMemberRoleCommand>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IReadRepository<Role> roleRepo;
        private readonly ICurrentUser currentUser;

        public UpdateTenantMemberRoleCommandValidator(
            IReadRepository<Tenant> tenantRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IReadRepository<Role> roleRepo,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.roleRepo = roleRepo;
            this.currentUser = currentUser;

            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId is required");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required");

            RuleFor(x => x.RoleId)
                .NotEmpty()
                .WithMessage("RoleId is required")
                .MustAsync(RoleExistsAndIsTenantScope)
                .WithMessage("Role not found or is not a Tenant-scoped role");

            RuleFor(x => x.TenantId)
                .MustAsync(TenantExists)
                .WithMessage("Tenant not found or inactive");

            RuleFor(x => x)
                .MustAsync(TenantMemberExists)
                .WithMessage("Tenant member not found or inactive");

            RuleFor(x => x.UserId)
                .Must(x => x != currentUser.Id)
                .WithMessage("Cannot change your own role");
        }

        private async Task<bool> RoleExistsAndIsTenantScope(Guid roleId, CancellationToken cancellationToken)
        {
            Role? role = await roleRepo.GetFirstBySearch(
                r => r.Id == roleId && r.Scope == RoleScope.Tenant && r.IsActive,
                cancellationToken);

            return role is not null;
        }

        private async Task<bool> TenantExists(Guid tenantId, CancellationToken cancellationToken)
        {
            Tenant? tenant = await tenantRepo.GetFirstBySearch(
                t => t.Id == tenantId && t.IsActive);

            return tenant is not null;
        }

        private async Task<bool> TenantMemberExists(UpdateTenantMemberRoleCommand command, CancellationToken cancellationToken)
        {
            TenantMember? tenantMember = await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == command.TenantId 
                    && m.UserId == command.UserId 
                    && m.IsActive);

            return tenantMember is not null;
        }
    }
}
