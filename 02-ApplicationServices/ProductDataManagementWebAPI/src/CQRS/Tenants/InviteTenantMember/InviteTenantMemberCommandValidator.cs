using FluentValidation;
using Business.Interfaces.Model;
using Entities.Models;
using Entities.Enums;
using Repositories.Repository.Interfaces;
using Repositiories.Repository.Interfaces;

namespace CQRS.Tenants.InviteTenantMember
{
    public class InviteTenantMemberCommandValidator : AbstractValidator<InviteTenantMemberCommand>
    {
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IRepository<TenantInvitation> invitationRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly ICurrentUser currentUser;

        public InviteTenantMemberCommandValidator(
            IRepository<TenantMember> tenantMemberRepo,
            IRepository<TenantInvitation> invitationRepo,
            IReadRepository<User> userRepo,
            ICurrentUser currentUser)
        {
            this.tenantMemberRepo = tenantMemberRepo;
            this.invitationRepo = invitationRepo;
            this.userRepo = userRepo;
            this.currentUser = currentUser;

            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId is required");
            
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Invalid email format")
                .Must(email =>
                {
                    if (string.IsNullOrWhiteSpace(currentUser.Email)) return true;
                    return !string.Equals(email.Trim(), currentUser.Email.Trim(), StringComparison.OrdinalIgnoreCase);
                })
                .WithMessage("You cannot invite yourself.");

            RuleFor(x => x)
                .MustAsync(UserMustNotBeAlreadyMember)
                .WithMessage("User is already a member of this tenant.");

            RuleFor(x => x)
                .MustAsync(InvitationMustNotExist)
                .WithMessage("An active invitation for this email already exists.");
        }

        private async Task<bool> UserMustNotBeAlreadyMember(InviteTenantMemberCommand command, CancellationToken cancellationToken)
        {
            string normalizedEmail = command.Email.Trim().ToLowerInvariant();
            
            var existingUser = await userRepo.GetFirstBySearch(u => u.Email == normalizedEmail && u.IsActive);
            
            if (existingUser == null)
            {
                return true;
            }

            var existingMembership = await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == command.TenantId && m.UserId == existingUser.Id && m.IsActive);
            
            return existingMembership == null;
        }

        private async Task<bool> InvitationMustNotExist(InviteTenantMemberCommand command, CancellationToken cancellationToken)
        {
            string normalizedEmail = command.Email.Trim().ToLowerInvariant();
            
            var existingInvitation = await invitationRepo.GetFirstBySearch(
                i => i.TenantId == command.TenantId 
                    && i.Email == normalizedEmail 
                    && i.IsActive 
                    && i.Status == InvitationStatus.Pending 
                    && i.ExpiresAt > DateTime.UtcNow);

            return existingInvitation == null;
        }
    }
}
