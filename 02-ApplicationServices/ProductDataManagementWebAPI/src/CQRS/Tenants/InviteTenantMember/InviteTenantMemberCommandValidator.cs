using Business.Interfaces.Model;
using CQRS.Extensions;
using Entities.Enums;
using Entities.Models.Tenants;
using Entities.Models.Users;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.InviteTenantMember
{
    public sealed class InviteTenantMemberCommandValidator : AbstractValidator<InviteTenantMemberCommand>
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

            RuleFor(x => x.TenantId).RequiredId();

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .MaximumLength(320)
                .WithMessage("Email cannot exceed 320 characters")
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

            User? existingUser = await userRepo.GetFirstBySearch(u => u.Email == normalizedEmail && u.IsActive);

            if (existingUser == null)
            {
                return true;
            }

            TenantMember? existingMembership = await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == command.TenantId && m.UserId == existingUser.Id && m.IsActive);

            return existingMembership == null;
        }

        private async Task<bool> InvitationMustNotExist(InviteTenantMemberCommand command, CancellationToken cancellationToken)
        {
            string normalizedEmail = command.Email.Trim().ToLowerInvariant();

            TenantInvitation? existingInvitation = await invitationRepo.GetFirstBySearch(
                i => i.TenantId == command.TenantId
                    && i.Email == normalizedEmail
                    && i.IsActive
                    && i.Status == InvitationStatus.Pending
                    && i.ExpiresAt > DateTime.UtcNow);

            return existingInvitation == null;
        }
    }
}
