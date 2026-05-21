using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Files.DeleteProjectFile
{
    public sealed class DeleteProjectFileCommandValidator : AbstractValidator<DeleteProjectFileCommand>
    {
        public DeleteProjectFileCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.FileId).RequiredId();
        }
    }
}
