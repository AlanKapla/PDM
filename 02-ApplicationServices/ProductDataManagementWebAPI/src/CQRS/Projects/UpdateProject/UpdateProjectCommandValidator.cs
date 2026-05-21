using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.UpdateProject
{
    public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
    {
        public UpdateProjectCommandValidator()
        {
            RuleFor(c => c.TenantId).RequiredId();
            RuleFor(c => c.ProjectId).RequiredId();

            RuleFor(c => c.Name)
                .NotEmpty()
                .WithMessage("Project name is required")
                .MaximumLength(200)
                .WithMessage("Project name cannot exceed 200 characters");
        }
    }
}
