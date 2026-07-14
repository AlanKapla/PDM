using CQRS.Extensions;
using FluentValidation;

namespace CQRS.AI.ParseCostDocument
{
    public sealed class ParseCostDocumentQueryValidator : AbstractValidator<ParseCostDocumentQuery>
    {
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png"];

        public ParseCostDocumentQueryValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            RuleFor(x => x.File)
                .NotNull().WithMessage("'File' is required.")
                .Must(f => f.Length > 0).WithMessage("'File' must not be empty.")
                .Must(f => AllowedExtensions.Contains(
                    Path.GetExtension(f.FileName).ToLowerInvariant()))
                .WithMessage("'File' must be JPG or PNG.");
        }
    }
}
