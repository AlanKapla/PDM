using Business.Interfaces.Model;
using Entities.Models.Tenants;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.AcceptProjectInvitation;

public sealed class AcceptProjectInvitationCommandValidator : AbstractValidator<AcceptProjectInvitationCommand>
{
    public AcceptProjectInvitationCommandValidator(
        IRepository<TenantInvitation> invitationRepo,
        ICurrentUser currentUser)
    {
        RuleFor(x => x.Token)
            .NotEmpty();

        RuleFor(x => x.Token)
            .MustAsync(async (token, ct) =>
            {
                TenantInvitation? invitation = await invitationRepo.GetFirstBySearch(
                    i => i.Token == token && i.IsActive && i.ProjectId != null);

                if (invitation is null)
                {
                    return false;
                }

                if (invitation.Status != InvitationStatus.Pending)
                {
                    return false;
                }

                if (invitation.ExpiresAt < DateTime.UtcNow)
                {
                    return false;
                }

                if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.Email))
                {
                    return false;
                }

                if (invitation.InvitedByUserId == currentUser.Id)
                {
                    return false;
                }

                return string.Equals(
                    currentUser.Email.Trim(),
                    invitation.Email.Trim(),
                    StringComparison.OrdinalIgnoreCase);
            })
            .WithMessage("Invalid or expired invitation token, email mismatch, or self-accept is not allowed.");
    }
}
