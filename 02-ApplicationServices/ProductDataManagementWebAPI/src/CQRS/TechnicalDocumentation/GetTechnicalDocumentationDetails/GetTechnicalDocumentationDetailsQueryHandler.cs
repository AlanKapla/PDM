using Business.Implementation.Helpers;
using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Entities.Enums;
using Entities.Models.TechnicalDocumentation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.TechnicalDocumentation.GetTechnicalDocumentationDetails;

public sealed class GetTechnicalDocumentationDetailsQueryHandler
    : IRequestHandler<GetTechnicalDocumentationDetailsQuery, TechnicalDocumentationDetailsWeb>
{
    private readonly IReadRepository<ProjectTechnicalDocumentation> documentationRepository;
    private readonly IBlobStorageService blobStorageService;

    private static readonly string ContainerName =
        BlobStorageSettings.GetContainerName(BlobContainerNames.TechnicalDocumentation);

    public GetTechnicalDocumentationDetailsQueryHandler(
        IReadRepository<ProjectTechnicalDocumentation> documentationRepository,
        IBlobStorageService blobStorageService)
    {
        this.documentationRepository = documentationRepository;
        this.blobStorageService = blobStorageService;
    }

    public async Task<TechnicalDocumentationDetailsWeb> Handle(
        GetTechnicalDocumentationDetailsQuery request,
        CancellationToken cancellationToken)
    {
        ProjectTechnicalDocumentation documentation = await GetDocumentationAsync(request, cancellationToken);

        ProjectTechnicalDocumentationDetails? details = IsDetailsAvailable(documentation.Status)
            ? TechnicalDocumentationDetailsSerializer.Deserialize(documentation.DetailsJson)
            : null;

        List<TechnicalDocumentationFileWeb> files = documentation.Files
            .OrderBy(f => f.CreatedAt)
            .Select(MapFileToWeb)
            .ToList();

        return new TechnicalDocumentationDetailsWeb
        {
            Id = documentation.Id,
            ProjectId = documentation.ProjectId,
            Name = documentation.Name,
            Description = documentation.Description,
            Status = documentation.Status,
            FileCount = documentation.Files.Count,
            CreatedAt = documentation.CreatedAt,
            CompletedAt = documentation.CompletedAt,
            ErrorMessage = documentation.ErrorMessage,
            Details = details,
            Files = files
        };
    }

    private async Task<ProjectTechnicalDocumentation> GetDocumentationAsync(
        GetTechnicalDocumentationDetailsQuery request,
        CancellationToken cancellationToken)
    {
        ProjectTechnicalDocumentation? documentation = await documentationRepository.GetFirstBySearch(
            d => d.TenantId == request.TenantId
                && d.ProjectId == request.ProjectId
                && d.Id == request.DocumentationId,
            cancellationToken,
            q => q.Include(d => d.Files));

        if (documentation is null)
        {
            throw new NotFoundApiException(
                nameof(ProjectTechnicalDocumentation),
                request.DocumentationId.ToString());
        }

        return documentation;
    }

    private static bool IsDetailsAvailable(TechnicalDocumentationStatus status)
    {
        return status is TechnicalDocumentationStatus.Completed
            or TechnicalDocumentationStatus.CompletedWithWarnings;
    }

    private TechnicalDocumentationFileWeb MapFileToWeb(ProjectTechnicalDocumentationFile file)
    {
        Uri previewUri = blobStorageService.GenerateSasUri(
            ContainerName,
            file.BlobName,
            file.OriginalFileName,
            expiresInMinutes: 60,
            contentDisposition: "inline");

        Uri downloadUri = blobStorageService.GenerateSasUri(
            ContainerName,
            file.BlobName,
            file.OriginalFileName,
            expiresInMinutes: 60,
            contentDisposition: "attachment");

        return new TechnicalDocumentationFileWeb
        {
            Id = file.Id,
            FileName = file.OriginalFileName,
            ContentType = file.ContentType,
            FileSize = file.FileSize,
            SasUriPreview = previewUri.ToString(),
            SasUriDownload = downloadUri.ToString()
        };
    }
}
