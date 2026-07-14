using Business.Interfaces.Model;
using CQRS.Extensions;
using Entities.Models.Projects;
using Entities.Models.Users;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.InviteProjectMember;

public sealed class InviteProjectMemberCommandValidator : AbstractValidator<InviteProjectMemberCommand>
{
    private readonly IRepository<ProjectMember> projectMemberRepo;
    private readonly IReadRepository<User> userRepo;
    private readonly ICurrentUser currentUser;

    public InviteProjectMemberCommandValidator(
        IRepository<ProjectMember> projectMemberRepo,
        IReadRepository<User> userRepo,
        ICurrentUser currentUser)
    {
        this.projectMemberRepo = projectMemberRepo;
        this.userRepo = userRepo;
        this.currentUser = currentUser;

        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .MaximumLength(320)
            .WithMessage("Email cannot exceed 320 characters")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .Must(email =>
            {
                if (string.IsNullOrWhiteSpace(currentUser.Email))
                {
                    return true;
                }

                return !string.Equals(email.Trim(), currentUser.Email.Trim(), StringComparison.OrdinalIgnoreCase);
            })
            .WithMessage("You cannot invite yourself.");

        RuleFor(x => x)
            .Must(command => command.IsAdmin || command.Modules.Count > 0)
            .WithMessage("At least one module must be selected or the user must be a project admin.");

        RuleFor(x => x)
            .MustAsync(UserMustNotBeProjectMember)
            .WithMessage("User is already a member of this project.");
    }

    private async Task<bool> UserMustNotBeProjectMember(
        InviteProjectMemberCommand command,
        CancellationToken cancellationToken)
    {
        string normalizedEmail = command.Email.Trim().ToLowerInvariant();

        User? existingUser = await userRepo.GetFirstBySearch(
            u => u.Email == normalizedEmail && u.IsActive,
            cancellationToken);

        if (existingUser is null)
        {
            return true;
        }

        ProjectMember? existingMember = await projectMemberRepo.GetFirstBySearch(
            pm => pm.ProjectId == command.ProjectId
                && pm.TenantId == command.TenantId
                && pm.UserId == existingUser.Id
                && pm.IsActive);

        return existingMember is null;
    }
}
