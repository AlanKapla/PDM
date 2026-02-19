using Business.AIAgent.Services;
using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using Entities.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.ExtractProjectCostsFromFiles;

/// <summary>
/// Handler for extracting project cost data from files using AI with Semantic Kernel
/// Processes multiple files, extracts cost data using Azure OpenAI via Semantic Kernel,
/// and creates project costs in the database
/// </summary>
public class ExtractProjectCostsFromFilesCommandHandler 
    : IRequestHandler<ExtractProjectCostsFromFilesCommand, ExtractProjectCostsFromFilesResponseWeb>
{
    private readonly ProjectCostExtractionService extractionService;
    private readonly IRepository<ProjectCost> projectCostRepository;
    private readonly IBlobStorageService blobStorageService;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<ExtractProjectCostsFromFilesCommandHandler> logger;

    public ExtractProjectCostsFromFilesCommandHandler(
        ProjectCostExtractionService extractionService,
        IRepository<ProjectCost> projectCostRepository,
        IBlobStorageService blobStorageService,
        ICurrentUser currentUser,
        ILogger<ExtractProjectCostsFromFilesCommandHandler> logger)
    {
        this.extractionService = extractionService;
        this.projectCostRepository = projectCostRepository;
        this.blobStorageService = blobStorageService;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<ExtractProjectCostsFromFilesResponseWeb> Handle(
        ExtractProjectCostsFromFilesCommand request, 
        CancellationToken cancellationToken)
    {
        var response = new ExtractProjectCostsFromFilesResponseWeb
        {
            TotalFilesProcessed = request.Files.Count
        };

        var createdIds = new List<Guid>();
        var errors = new List<FileProcessingErrorWeb>();

        // Process each file
        foreach (var file in request.Files)
        {
            try
            {
                var result = await ProcessFileAsync(
                    file, 
                    request, 
                    cancellationToken);

                if (result.Success && result.ProjectCostId.HasValue)
                {
                    createdIds.Add(result.ProjectCostId.Value);
                }
                else
                {
                    errors.Add(new FileProcessingErrorWeb
                    {
                        FileName = file.FileName,
                        ErrorMessage = result.ErrorMessage ?? "Unknown error",
                        ErrorType = "ExtractionFailed"
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing file: {FileName}", file.FileName);
                errors.Add(new FileProcessingErrorWeb
                {
                    FileName = file.FileName,
                    ErrorMessage = ex.Message,
                    ErrorType = "UnexpectedError"
                });
            }
        }

        return response with
        {
            CreatedProjectCostIds = createdIds,
            Errors = errors,
            SuccessCount = createdIds.Count,
            ErrorCount = errors.Count
        };
    }

    private async Task<FileProcessingResult> ProcessFileAsync(
        Microsoft.AspNetCore.Http.IFormFile file,
        ExtractProjectCostsFromFilesCommand request,
        CancellationToken cancellationToken)
    {
        // Read file content
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var fileContent = memoryStream.ToArray();

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        // Extract data using AI with Semantic Kernel
        var extractionResult = await extractionService.ExtractFromFileAsync(
            file.FileName,
            fileContent,
            fileExtension,
            cancellationToken);

        if (!extractionResult.Success || extractionResult.ExtractedData == null)
        {
            return new FileProcessingResult
            {
                Success = false,
                ErrorMessage = extractionResult.ErrorMessage ?? "Extraction failed"
            };
        }

        var extractedData = extractionResult.ExtractedData;

        // Validate extracted data - check if totals are present
        if (extractedData.TotalGross <= 0)
        {
            return new FileProcessingResult
            {
                Success = false,
                ErrorMessage = "No cost data found in the document"
            };
        }

        // Create project cost from extracted data
        var projectCostId = await CreateProjectCostFromExtractedDataAsync(
            request,
            extractedData,
            file,
            fileContent,
            Path.GetExtension(file.FileName),
            cancellationToken);

        return new FileProcessingResult
        {
            Success = true,
            ProjectCostId = projectCostId
        };
    }

    private async Task<Guid> CreateProjectCostFromExtractedDataAsync(
        ExtractProjectCostsFromFilesCommand request,
        ExtractedProjectCostData extractedData,
        Microsoft.AspNetCore.Http.IFormFile file,
        byte[] fileContent,
        string fileExtension,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Use totals directly from AI extraction
        var totalNet = extractedData.TotalNet;
        var totalGross = extractedData.TotalGross;

        // Calculate VAT if both net and gross are present
        decimal? vatRate = null;
        if (totalNet > 0 && totalGross > totalNet)
        {
            var vatAmount = totalGross - totalNet;
            vatRate = Math.Round((vatAmount / totalNet) * 100, 2);
        }

        // Build description from AI purchaseDescription or file name
        var description = extractedData.PurchaseDescription;
        if (string.IsNullOrWhiteSpace(description))
        {
            description = $"Purchase from {file.FileName}";
        }

        // Upload document to blob storage
        var costId = Guid.NewGuid();
        string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.ProjectCosts);
        string blobFileName = $"{costId}{fileExtension}";
        string blobPath = $"{request.TenantId}/{request.ProjectId}/{currentUser.Id}/{costId}/{blobFileName}";

        using (var stream = new MemoryStream(fileContent))
        {
            await blobStorageService.UploadAsync(
                containerName,
                blobPath,
                stream,
                file.ContentType,
                cancellationToken);
        }

        // Name = Document number (invoice/receipt number)
        var costName = !string.IsNullOrWhiteSpace(extractedData.DocumentNumber)
            ? extractedData.DocumentNumber
            : $"Document from {file.FileName}";

        // Place = Vendor name (company/shop name)
        var place = extractedData.VendorName;

        // Parse transaction date if available
        var costDate = now.Date;
        if (!string.IsNullOrWhiteSpace(extractedData.TransactionDate) && 
            DateTime.TryParse(extractedData.TransactionDate, out var parsedDate))
        {
            costDate = parsedDate.Date;
        }

        var projectCost = new ProjectCost
        {
            Id = costId,
            TenantId = request.TenantId,
            ProjectId = request.ProjectId,
            UserId = currentUser.Id,
            Name = costName,
            Place = place,
            Date = costDate,
            Description = description,
            NetAmount = totalNet > 0 ? totalNet : null,
            VatRate = vatRate,
            GrossAmount = totalGross,
            IsClosed = false,
            HasDocument = true,
            DocumentFileName = file.FileName,
            DocumentBlobPath = blobPath,
            DocumentContentType = file.ContentType,
            DocumentSizeBytes = file.Length,
            CreatedAt = now,
            IsDeleted = false
        };

        await projectCostRepository.Insert(projectCost);
        await projectCostRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created project cost {ProjectCostId} from file {FileName} with gross amount {GrossAmount} (vendor: {Vendor}, date: {Date})",
            projectCost.Id, file.FileName, projectCost.GrossAmount, costName, costDate);

        return projectCost.Id;
    }
}

/// <summary>
/// Internal result for file processing
/// </summary>
internal sealed class FileProcessingResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? ProjectCostId { get; init; }
}
