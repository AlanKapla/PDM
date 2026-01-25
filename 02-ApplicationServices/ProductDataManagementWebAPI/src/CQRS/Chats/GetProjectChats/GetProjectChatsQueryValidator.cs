using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Chats.GetProjectChats
{
    public class GetProjectChatsQueryValidator : AbstractValidator<GetProjectChatsQuery>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetProjectChatsQueryValidator(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.currentUser = currentUser;

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            RuleFor(x => x.ProjectId)
                .MustAsync(ProjectMustExist)
                .WithMessage("Project not found");

            RuleFor(x => x.ProjectId)
                .MustAsync(UserMustBeProjectMember)
                .WithMessage("User is not a member of this project");
        }

        private async Task<bool> ProjectMustExist(Guid projectId, CancellationToken cancellationToken)
        {
            var project = await projectRepo.GetFirstBySearch(
                p => p.Id == projectId && p.IsActive,
                cancellationToken);

            return project != null;
        }

        private async Task<bool> UserMustBeProjectMember(Guid projectId, CancellationToken cancellationToken)
        {
            var member = await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == projectId && pm.UserId == currentUser.Id);

            return member != null;
        }
    }
}
