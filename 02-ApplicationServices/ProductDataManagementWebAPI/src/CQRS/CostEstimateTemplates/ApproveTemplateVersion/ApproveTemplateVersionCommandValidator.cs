using FluentValidation;

namespace CQRS.CostEstimateTemplates.ApproveTemplateVersion
{
    /// <summary>
    /// Walidator dla ApproveTemplateVersionCommand
    /// </summary>
    public class ApproveTemplateVersionCommandValidator : AbstractValidator<ApproveTemplateVersionCommand>
    {
        public ApproveTemplateVersionCommandValidator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty().WithMessage("Template ID is required");

            RuleFor(x => x.VersionId)
                .NotEmpty().WithMessage("Version ID is required");
        }
    }
}
