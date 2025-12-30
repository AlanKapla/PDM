using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositories.Repository.Interfaces;
using Repositiories.Repository.Interfaces;

namespace CQRS.Projects.GetProjectDetails
{
    public class GetProjectDetailsQueryValidator : AbstractValidator<GetProjectDetailsQuery>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetProjectDetailsQueryValidator(
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

            // Walidacja: projekt musi istnieć
            RuleFor(x => x)
                .MustAsync(ProjectMustExist)
                .WithMessage("Project not found");
        }

        private async Task<bool> ProjectMustExist(GetProjectDetailsQuery query, CancellationToken cancellationToken)
        {
            var project = await projectRepo.GetFirstBySearch(
                p => p.Id == query.ProjectId && p.TenantId == query.TenantId,
                cancellationToken);

            return project != null;
        }
    }
}
