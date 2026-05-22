using Business.Interfaces.Model;
using CQRS.Extensions;
using Entities.Models.Tenants;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.ChangeActiveTenant
{
    public sealed class ChangeActiveTenantCommandValidator : AbstractValidator<ChangeActiveTenantCommand>
    {
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public ChangeActiveTenantCommandValidator(IRepository<TenantMember> tenantMemberRepo, ICurrentUser currentUser)
        {
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;

            RuleFor(_ => currentUser.IsAuthenticated)
                .Equal(true)
                .WithMessage("User must be authenticated.");

            RuleFor(x => x.TenantId)
                .RequiredId()
                .MustAsync(async (tenantId, ct) => await IsMemberAsync(tenantId) || currentUser.IsSuperAdmin)
                .WithMessage("User is not a member of specified tenant or membership inactive");
        }

        private async Task<bool> IsMemberAsync(Guid tenantId)
        {
            if (!currentUser.IsAuthenticated || currentUser.Id == Guid.Empty)
            {
                return false;
            }

            TenantMember? member = await tenantMemberRepo.GetFirstBySearch(m => m.TenantId == tenantId && m.UserId == currentUser.Id && m.IsActive);
            return member != null;
        }
    }
}
