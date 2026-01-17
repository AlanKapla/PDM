using FluentValidation;

namespace CQRS.Projects.UpdateProject
{
    public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
    {
        public UpdateProjectCommandValidator()
        {
            RuleFor(c => c.TenantId)
                .NotEmpty()
                .WithMessage("TenantId is required");

            RuleFor(c => c.ProjectId)
                .NotEmpty()
                .WithMessage("ProjectId is required");

            RuleFor(c => c.Name)
                .NotEmpty()
                .WithMessage("Project name is required")
                .MaximumLength(200)
                .WithMessage("Project name cannot exceed 200 characters");
        }
    }
}
