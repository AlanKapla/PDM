using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Projects.CreateProject
{
    public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
    {
        public CreateProjectCommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Project name is required")
                .MaximumLength(200).WithMessage("Project name cannot exceed 200 characters");
        }
    }
}
