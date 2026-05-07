using FluentValidation;
using Business.Interfaces.Model;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.RemoveTenantInvitation
{
    public class RemoveTenantInvitationCommandValidator : AbstractValidator<RemoveTenantInvitationCommand>
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

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.InvitationId)
                .NotEmpty().WithMessage("InvitationId is required");

            RuleFor(x => x.TenantId)
                .Must(tenantId => tenantId == currentUser.ActiveTenantId)
                .WithMessage("TenantId must match the active tenant");

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
