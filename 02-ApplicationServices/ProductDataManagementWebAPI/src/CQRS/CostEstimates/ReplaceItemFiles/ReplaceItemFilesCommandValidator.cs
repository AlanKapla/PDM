using CQRS.Extensions;
using Entities.Models.CostEstimates;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.ReplaceItemFiles
{
    public sealed class ReplaceItemFilesCommandValidator : AbstractValidator<ReplaceItemFilesCommand>
    {
        private const long MaxFileSizeBytes = 50L * 1024 * 1024; // 50 MB
        private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg"];
        private static readonly string[] AllowedContentTypes = ["application/pdf", "image/jpeg"];

        public ReplaceItemFilesCommandValidator(
            IReadRepository<CostEstimate> costEstimateRepo,
            IReadRepository<CostEstimateItem> itemRepo)
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.CostEstimateId).RequiredId();
            RuleFor(x => x.ItemId).RequiredId();

            RuleFor(x => x.Files)
                .NotNull().WithMessage("Files list is required");

            When(x => x.Files is not null && x.Files.Count > 0, () =>
            {
                RuleFor(x => x.Files)
                    .Must(files => files.Count <= 10)
                    .WithMessage("You can upload a maximum of 10 files at once");

                RuleForEach(x => x.Files)
                    .ChildRules(file =>
                    {
                        file.RuleFor(f => f)
                            .NotNull().WithMessage("File is required");

                        file.RuleFor(f => f.Length)
                            .GreaterThan(0).WithMessage("File cannot be empty")
                            .LessThanOrEqualTo(MaxFileSizeBytes)
                            .WithMessage("File cannot be larger than 50 MB")
                            .When(f => f is not null);

                        file.RuleFor(f => f.FileName)
                            .NotEmpty().WithMessage("File name is required")
                            .Must(BeValidExtension)
                            .WithMessage("Allowed file formats are: PDF, JPG")
                            .When(f => f is not null);

                        file.RuleFor(f => f.ContentType)
                            .Must(BeValidContentType)
                            .WithMessage("Allowed content types are: application/pdf, image/jpeg")
                            .When(f => f is not null);
                    });
            });

            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    return await costEstimateRepo.AnyAsync(
                        c => c.Id == command.CostEstimateId &&
                             c.TenantId == command.TenantId &&
                             c.ProjectId == command.ProjectId,
                        cancellation);
                })
                .WithMessage("Cost estimate not found");

            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    return await itemRepo.AnyAsync(
                        i => i.Id == command.ItemId &&
                             i.CostEstimateId == command.CostEstimateId,
                        cancellation);
                })
                .WithMessage("Item not found or does not belong to the specified cost estimate");
        }

        private static bool BeValidExtension(string fileName)
        {
            string? extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
        }

        private static bool BeValidContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType) && AllowedContentTypes.Contains(contentType.ToLowerInvariant());
        }
    }
}
