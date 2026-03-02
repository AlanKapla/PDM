using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UploadCostEstimateFieldFiles
{
    public class UploadCostEstimateFieldFilesCommandValidator : AbstractValidator<UploadCostEstimateFieldFilesCommand>
    {
        private const long MaxFileSizeBytes = 50L * 1024 * 1024; // 50 MB
        private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg"];
        private static readonly string[] AllowedContentTypes = ["application/pdf", "image/jpeg"];

        public UploadCostEstimateFieldFilesCommandValidator(
            IReadRepository<CostEstimate> costEstimateRepo,
            IReadRepository<CostEstimateItem> itemRepo,
            IRepository<CostEstimateTemplateItemSystemFieldDefinition> fieldDefRepo)
        {
            RuleFor(x => x.CostEstimateId)
                .NotEmpty().WithMessage("Cost estimate ID is required");

            RuleFor(x => x.ItemId)
                .NotEmpty().WithMessage("Item ID is required");

            RuleFor(x => x.FieldDefinitionId)
                .NotEmpty().WithMessage("Field definition ID is required");

            When(x => x.Files.Count > 0, () =>
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
                            .When(f => f != null);

                        file.RuleFor(f => f.FileName)
                            .NotEmpty().WithMessage("File name is required")
                            .Must(BeValidExtension)
                            .WithMessage("Allowed file formats are: PDF, JPG")
                            .When(f => f != null);

                        file.RuleFor(f => f.ContentType)
                            .Must(BeValidContentType)
                            .WithMessage("Allowed content types are: application/pdf, image/jpeg")
                            .When(f => f != null);
                    });
            });

            // Check cost estimate exists and belongs to tenant/project
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    return await costEstimateRepo.AnyAsync(
                        c => c.Id == command.CostEstimateId &&
                             c.TenantId == command.TenantId &&
                             c.ProjectId == command.ProjectId &&
                             !c.IsDeleted,
                        cancellation);
                })
                .WithMessage("Cost estimate not found");

            // Check item exists and belongs to the cost estimate
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    return await itemRepo.AnyAsync(
                        i => i.Id == command.ItemId &&
                             i.CostEstimateId == command.CostEstimateId &&
                             !i.IsDeleted,
                        cancellation);
                })
                .WithMessage("Item not found or does not belong to the specified cost estimate");

            // Check field definition exists and is of type ItemSystemFiles
            RuleFor(x => x)
                .MustAsync(async (command, cancellation) =>
                {
                    return await fieldDefRepo.AnyAsync(
                        fd => fd.Id == command.FieldDefinitionId &&
                              fd.FieldType == FieldType.ItemSystemFiles,
                        cancellation);
                })
                .WithMessage("Field definition not found or is not of type ItemSystemFiles");
        }

        private static bool BeValidExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
        }

        private static bool BeValidContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType) && AllowedContentTypes.Contains(contentType.ToLowerInvariant());
        }
    }
}
