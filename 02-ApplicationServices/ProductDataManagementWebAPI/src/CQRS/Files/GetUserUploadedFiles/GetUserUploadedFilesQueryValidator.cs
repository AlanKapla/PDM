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
        }
    }
}
