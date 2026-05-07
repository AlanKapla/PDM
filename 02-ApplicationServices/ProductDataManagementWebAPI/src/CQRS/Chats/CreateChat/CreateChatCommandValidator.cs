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
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Chats.CreateChat
{
    public class CreateChatCommandValidator : AbstractValidator<CreateChatCommand>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public CreateChatCommandValidator(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.currentUser = currentUser;

            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

            RuleFor(x => x.MemberUserIds)
                .NotEmpty().WithMessage("At least one member is required")
                .Must(ids => ids.Count >= 1).WithMessage("Chat must have at least one member");

            When(x => !x.IsGroupChat, () =>
            {
                RuleFor(x => x.MemberUserIds)
                    .Must(ids => ids.Count == 1).WithMessage("Direct chat must have exactly one other member");
            });

            RuleFor(x => x)
                .MustAsync(ProjectMustExist)
                .WithMessage("Project not found");

            RuleFor(x => x.ProjectId)
                .MustAsync(UserMustBeProjectMember)
                .WithMessage("User is not a member of this project");

            RuleFor(x => x)
                .MustAsync(AllMembersMustBeProjectMembers)
                .WithMessage("All members must be project members");
        }

        private async Task<bool> ProjectMustExist(CreateChatCommand command, CancellationToken cancellationToken)
        {
            var project = await projectRepo.GetFirstBySearch(
                p => p.Id == command.ProjectId && p.TenantId == command.TenantId && p.IsActive,
                cancellationToken);

            return project != null;
        }

        private async Task<bool> UserMustBeProjectMember(Guid projectId, CancellationToken cancellationToken)
        {
            var member = await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == projectId && pm.UserId == currentUser.Id);

            return member != null;
        }

        private async Task<bool> AllMembersMustBeProjectMembers(CreateChatCommand command, CancellationToken cancellationToken)
        {
            foreach (var userId in command.MemberUserIds)
            {
                var member = await projectMemberRepo.GetFirstBySearch(
                    pm => pm.ProjectId == command.ProjectId && pm.UserId == userId);

                if (member == null)
                    return false;
            }

            return true;
        }
    }
}
