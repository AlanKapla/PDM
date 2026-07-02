using CQRS.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace CQRS.TechnicalDocumentation.CreateTechnicalDocumentation;

public sealed class CreateTechnicalDocumentationCommandValidator : AbstractValidator<CreateTechnicalDocumentationCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/octet-stream"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };

    public CreateTechnicalDocumentationCommandValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("'Name' is required.")
            .MaximumLength(200).WithMessage("'Name' must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Files)
            .NotEmpty().WithMessage("At least one file is required.");

        RuleForEach(x => x.Files).ChildRules(file =>
        {
            file.RuleFor(f => f.Length)
                .LessThanOrEqualTo(52_428_800)
                .WithMessage("File size must not exceed 50 MB.");

            file.RuleFor(f => f.ContentType)
                .Must((formFile, contentType) => IsAllowedContentType(formFile))
                .WithMessage("Allowed content types: application/pdf, image/jpeg, image/png (or valid extension with application/octet-stream).");

            file.RuleFor(f => Path.GetExtension(f.FileName))
                .Must(ext => AllowedExtensions.Contains(ext))
                .WithMessage("Allowed file extensions: .pdf, .jpg, .jpeg, .png.");
        });
    }

    private static bool IsAllowedContentType(IFormFile file)
    {
        if (AllowedContentTypes.Contains(file.ContentType))
        {
            string extension = Path.GetExtension(file.FileName);
            if (string.Equals(file.ContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
            {
                return AllowedExtensions.Contains(extension);
            }

            return true;
        }

        return false;
    }
}
