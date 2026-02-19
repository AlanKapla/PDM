using FluentValidation;

namespace CQRS.ProjectCosts.ExtractProjectCostsFromFiles;

public class ExtractProjectCostsFromFilesCommandValidator : AbstractValidator<ExtractProjectCostsFromFilesCommand>
{
    private const long MaxTotalSizeBytes = 50 * 1024 * 1024; // 50 MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".pdf" };

    public ExtractProjectCostsFromFilesCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");

        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("ProjectId is required");

        RuleFor(x => x.Files)
            .NotEmpty()
            .WithMessage("At least one file is required")
            .Must(files => files.Sum(f => f.Length) <= MaxTotalSizeBytes)
            .WithMessage($"Total file size must not exceed 50 MB")
            .Must(files => files.All(f => IsAllowedExtension(f.FileName)))
            .WithMessage("Only JPG and PDF files are allowed");
    }

    private static bool IsAllowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }
}
