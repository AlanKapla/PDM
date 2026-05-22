using Business.Interfaces.Model;
using CQRS.Extensions;
using Entities.Models.Tenants;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.RemoveTenantInvitation
{
    public sealed class RemoveTenantInvitationCommandValidator : AbstractValidator<RemoveTenantInvitationCommand>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IRepository<TenantInvitation> invitationRepo;
        private readonly ICurrentUser currentUser;

        public RemoveTenantInvitationCommandValidator(
            IReadRepository<Tenant> tenantRepo,
            IRepository<TenantInvitation> invitationRepo,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.invitationRepo = invitationRepo;
            this.currentUser = currentUser;

            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.InvitationId).RequiredId();

            RuleFor(x => x)
                .MustAsync(TenantMustExist)
                .WithMessage("Tenant not found");

            RuleFor(x => x)
                .MustAsync(InvitationMustExist)
                .WithMessage("Invitation not found");

            RuleFor(x => x)
                .MustAsync(InvitationMustBelongToTenant)
                .WithMessage("Invitation does not belong to this tenant");
        }

        private async Task<bool> TenantMustExist(RemoveTenantInvitationCommand command, CancellationToken cancellationToken)
        {
            Tenant? tenant = await tenantRepo.GetFirstBySearch(
                t => t.Id == command.TenantId && t.IsActive);
            return tenant != null;
        }

        private async Task<bool> InvitationMustExist(RemoveTenantInvitationCommand command, CancellationToken cancellationToken)
        {
            TenantInvitation? invitation = await invitationRepo.GetFirstBySearch(
                i => i.Id == command.InvitationId);
            return invitation != null;
        }

        private async Task<bool> InvitationMustBelongToTenant(RemoveTenantInvitationCommand command, CancellationToken cancellationToken)
        {
            TenantInvitation? invitation = await invitationRepo.GetFirstBySearch(
                i => i.Id == command.InvitationId && i.TenantId == command.TenantId);
            return invitation != null;
        }
    }
}
