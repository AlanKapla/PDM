using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.GetUserUploadedFiles
{
    public class GetUserUploadedFilesQueryValidator : AbstractValidator<GetUserUploadedFilesQuery>
    {
        public GetUserUploadedFilesQueryValidator(
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("TenantId is required");

            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("ProjectId is required");

            // Validate user is project member
            RuleFor(x => x)
                .MustAsync(async (query, cancellation) =>
                {
                    var membership = await projectMemberRepo.GetFirstBySearch(
                        pm => pm.ProjectId == query.ProjectId &&
                              pm.TenantId == query.TenantId &&
                              pm.UserId == currentUser.Id);
                    return membership != null;
                })
                .WithMessage("User is not a member of the project");
        }
    }
}
