using Business.Interfaces.Helpers;
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.AI.ParseCostDocument
{
    public sealed class ParseCostDocumentQueryValidator : AbstractValidator<ParseCostDocumentQuery>
    {
        public ParseCostDocumentQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            RuleFor(x => x.File)
                .NotNull().WithMessage("'File' is required.")
                .Must(f => f.Length > 0).WithMessage("'File' must not be empty.")
                .Must(f => FileContentValidator.IsAllowedExtension(f.FileName))
                .WithMessage("'File' must be JPG, PNG or PDF.")
                .Must(f => FileContentValidator.IsAllowedContentType(f.ContentType))
                .WithMessage("'File' must have a valid content type (image/jpeg, image/png or application/pdf).");
        }
    }
}
