using Business.Interfaces.Configurations;
using FluentValidation;
using CQRS.Extensions;
using Microsoft.Extensions.Options;

namespace CQRS.AI.SubmitAICostImportBatch
{
    public sealed class SubmitAICostImportBatchCommandValidator
        : AbstractValidator<SubmitAICostImportBatchCommand>
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png"
        };

        public SubmitAICostImportBatchCommandValidator(IOptions<AICostImportOptions> options)
        {
            AICostImportOptions config = options.Value;

            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();

            RuleFor(x => x.Files)
                .NotNull()
                .Must(f => f.Count >= 2)
                .WithMessage("At least 2 files are required for batch import.");

            RuleFor(x => x.Files)
                .Must(f => f.Sum(file => file.Length) <= config.MaxBatchTotalBytes)
                .WithMessage(x =>
                {
                    long total = x.Files.Sum(f => f.Length);
                    return $"Total file size ({total} bytes) exceeds the limit of {config.MaxBatchTotalBytes} bytes.";
                })
                .When(x => x.Files is not null);

            RuleForEach(x => x.Files)
                .Must(file =>
                {
                    string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    return AllowedExtensions.Contains(ext);
                })
                .WithMessage("Allowed formats: JPG, PNG.")
                .When(x => x.Files is not null);
        }
    }
}
