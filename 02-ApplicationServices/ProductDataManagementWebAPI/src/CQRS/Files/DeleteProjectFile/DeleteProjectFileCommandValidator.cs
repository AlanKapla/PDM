using Business.Interfaces.Model;
using Entities.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.DeleteProjectFile
{
    public class DeleteProjectFileCommandValidator : AbstractValidator<DeleteProjectFileCommand>
    {
        public DeleteProjectFileCommandValidator(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            RuleFor(x => x.FileId)
                .NotEmpty().WithMessage("FileId is required");
        }
    }
}
