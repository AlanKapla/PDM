using FluentValidation;

namespace CQRS.CostEstimateTemplates.DeleteTemplateVersion
{
    /// <summary>
    /// Walidator dla DeleteTemplateVersionCommand
    /// </summary>
    public class DeleteTemplateVersionCommandValidator : AbstractValidator<DeleteTemplateVersionCommand>
    {
        public DeleteTemplateVersionCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty().WithMessage("Template ID is required");

            RuleFor(x => x.VersionId)
                .NotEmpty().WithMessage("Version ID is required");
        }
    }
}
