using Business.Implementation.Helpers;
using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Entities.Enums;
using Entities.Models.TechnicalDocumentation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

public sealed class TechnicalDocumentationProcessingService : ITechnicalDocumentationProcessingService
{
    private const int MaxErrorMessageLength = 2000;

    private readonly IRepository<ProjectTechnicalDocumentation> documentationRepository;
    private readonly IBlobStorageService blobStorageService;
    private readonly IPdfToImageConverterService pdfConverter;
    private readonly ITechnicalDocumentationOrchestratorService orchestrator;
    private readonly ITechnicalDocumentationDispatcher dispatcher;
    private readonly ILogger<TechnicalDocumentationProcessingService> logger;

    private static readonly string ContainerName =
        BlobStorageSettings.GetContainerName(BlobContainerNames.TechnicalDocumentation);

    public TechnicalDocumentationProcessingService(
        IRepository<ProjectTechnicalDocumentation> documentationRepository,
        IBlobStorageService blobStorageService,
        IPdfToImageConverterService pdfConverter,
        ITechnicalDocumentationOrchestratorService orchestrator,
        ITechnicalDocumentationDispatcher dispatcher,
        ILogger<TechnicalDocumentationProcessingService> logger)
    {
        this.documentationRepository = documentationRepository;
        this.blobStorageService = blobStorageService;
        this.pdfConverter = pdfConverter;
        this.orchestrator = orchestrator;
        this.dispatcher = dispatcher;
        this.logger = logger;
    }

    public async Task ProcessAsync(
        Guid documentationId,
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        ProjectTechnicalDocumentation documentation = await LoadDocumentationAsync(
            documentationId, tenantId, projectId, cancellationToken);

        try
        {
            documentation.Status = TechnicalDocumentationStatus.Processing;
            await documentationRepository.Update(documentation);
            await documentationRepository.SaveChangesAsync(cancellationToken);

            List<TechnicalDocumentationImageInput> images = await BuildImageInputsAsync(
                documentation.Files, cancellationToken);

            ProjectTechnicalDocumentationDetails details = await orchestrator.ProcessImagesAsync(
                images, cancellationToken);

            documentation.DetailsJson = TechnicalDocumentationDetailsSerializer.Serialize(details);
            documentation.Status = TechnicalDocumentationProcessingStatusResolver.Resolve(details);
            documentation.CompletedAt = DateTime.UtcNow;
            documentation.ErrorMessage = null;

            await documentationRepository.Update(documentation);
            await documentationRepository.SaveChangesAsync(cancellationToken);

            await DispatchResultAsync(documentation, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Technical documentation processing failed for {DocumentationId}",
                documentationId);

            documentation.Status = TechnicalDocumentationStatus.Failed;
            documentation.ErrorMessage = TruncateErrorMessage(ex.Message);
            documentation.CompletedAt = DateTime.UtcNow;

            await documentationRepository.Update(documentation);
            await documentationRepository.SaveChangesAsync(cancellationToken);

            await DispatchResultAsync(documentation, cancellationToken);
            throw;
        }
    }

    private async Task<ProjectTechnicalDocumentation> LoadDocumentationAsync(
        Guid documentationId,
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        ProjectTechnicalDocumentation? documentation = await documentationRepository.GetFirstBySearch(
            d => d.TenantId == tenantId
                && d.ProjectId == projectId
                && d.Id == documentationId,
            q => q.Include(d => d.Files));

        if (documentation is null)
        {
            throw new InvalidOperationException(
                $"Technical documentation {documentationId} not found.");
        }

        return documentation;
    }

    private async Task<List<TechnicalDocumentationImageInput>> BuildImageInputsAsync(
        ICollection<ProjectTechnicalDocumentationFile> files,
        CancellationToken cancellationToken)
    {
        List<TechnicalDocumentationImageInput> images = new();

        List<ProjectTechnicalDocumentationFile> orderedFiles = files
            .OrderBy(file => InferSheetSortKey(file.OriginalFileName))
            .ThenBy(file => file.CreatedAt)
            .ToList();

        foreach (ProjectTechnicalDocumentationFile file in orderedFiles)
        {
            byte[] fileBytes = await DownloadFileBytesAsync(file.BlobName, cancellationToken);

            if (IsPdf(file.ContentType, file.OriginalFileName))
            {
                IReadOnlyList<byte[]> pages = await pdfConverter.ConvertAllPagesToJpegAsync(
                    fileBytes, cancellationToken);

                for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
                {
                    images.Add(new TechnicalDocumentationImageInput(
                        pages[pageIndex],
                        file.OriginalFileName,
                        pageIndex + 1,
                        "image/png"));
                }
            }
            else
            {
                images.Add(new TechnicalDocumentationImageInput(
                    fileBytes,
                    file.OriginalFileName,
                    1));
            }
        }

        return images;
    }

    private static string InferSheetSortKey(string? fileName)
    {
        string? sheetNumber = DrawingSheetNumberInferrer.InferFromFileName(fileName);
        if (string.IsNullOrWhiteSpace(sheetNumber))
        {
            return fileName?.ToLowerInvariant() ?? string.Empty;
        }

        return sheetNumber;
    }

    private async Task<byte[]> DownloadFileBytesAsync(
        string blobName,
        CancellationToken cancellationToken)
    {
        BlobDownload download = await blobStorageService.DownloadAsync(
            ContainerName, blobName, cancellationToken);

        using MemoryStream memoryStream = new();
        await download.Content.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    private static bool IsPdf(string contentType, string fileName)
    {
        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private async Task DispatchResultAsync(
        ProjectTechnicalDocumentation documentation,
        CancellationToken cancellationToken)
    {
        TechnicalDocumentationProcessingResultDto payload = new()
        {
            DocumentationId = documentation.Id,
            ProjectId = documentation.ProjectId,
            TenantId = documentation.TenantId,
            Name = documentation.Name,
            Status = documentation.Status,
            ErrorMessage = documentation.ErrorMessage
        };

        await dispatcher.DispatchCompletedAsync(payload, cancellationToken);
    }

    private static string TruncateErrorMessage(string message)
    {
        if (message.Length <= MaxErrorMessageLength)
        {
            return message;
        }

        return message[..MaxErrorMessageLength];
    }
}
