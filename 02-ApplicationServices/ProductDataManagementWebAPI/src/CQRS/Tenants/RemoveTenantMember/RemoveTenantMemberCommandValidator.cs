using FluentValidation;
using Business.Interfaces.Model;
using Entities.Models;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.RemoveTenantMember
{
    public class RemoveTenantMemberCommandValidator : AbstractValidator<RemoveTenantMemberCommand>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public RemoveTenantMemberCommandValidator(
            IReadRepository<Tenant> tenantRepo,
            IRepository<TenantMember> tenantMemberRepo,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required");

            // Walidacja: tenant musi istnieć
            RuleFor(x => x)
                .MustAsync(TenantMustExist)
                .WithMessage("Tenant not found");

            // Walidacja: członek tenanta musi istnieć i być aktywny
            RuleFor(x => x)
                .MustAsync(TenantMemberMustExistAndBeActive)
                .WithMessage("User is not an active member of this tenant");

            // Walidacja: nie można usunąć samego siebie
            RuleFor(x => x.UserId)
                .Must(userId => userId != currentUser.Id)
                .WithMessage("Cannot remove yourself from the tenant");
        }

        private async Task<bool> TenantMustExist(RemoveTenantMemberCommand command, CancellationToken cancellationToken)
        {
            Tenant? tenant = await tenantRepo.GetFirstBySearch(t => t.Id == command.TenantId);
            return tenant != null;
        }

        private async Task<bool> TenantMemberMustExistAndBeActive(RemoveTenantMemberCommand command, CancellationToken cancellationToken)
        {
            TenantMember? member = await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == command.TenantId 
                    && m.UserId == command.UserId 
                    && m.IsActive);

            return member != null;
        }
    }
}
