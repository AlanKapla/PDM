using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Entities.Enums;
using Entities.Models.Projects;
using Entities.Models.TechnicalDocumentation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.TechnicalDocumentation.CreateTechnicalDocumentation;

public sealed class CreateTechnicalDocumentationCommandHandler
    : IRequestHandler<CreateTechnicalDocumentationCommand, TechnicalDocumentationCreatedWeb>
{
    private readonly IRepository<ProjectTechnicalDocumentation> documentationRepository;
    private readonly IRepository<ProjectTechnicalDocumentationFile> fileRepository;
    private readonly IReadRepository<Project> projectRepository;
    private readonly IBlobStorageService blobStorageService;
    private readonly IQueuedTechnicalDocumentationSender queueSender;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<CreateTechnicalDocumentationCommandHandler> logger;

    private static readonly string ContainerName =
        BlobStorageSettings.GetContainerName(BlobContainerNames.TechnicalDocumentation);

    public CreateTechnicalDocumentationCommandHandler(
        IRepository<ProjectTechnicalDocumentation> documentationRepository,
        IRepository<ProjectTechnicalDocumentationFile> fileRepository,
        IReadRepository<Project> projectRepository,
        IBlobStorageService blobStorageService,
        IQueuedTechnicalDocumentationSender queueSender,
        ICurrentUser currentUser,
        ILogger<CreateTechnicalDocumentationCommandHandler> logger)
    {
        this.documentationRepository = documentationRepository;
        this.fileRepository = fileRepository;
        this.projectRepository = projectRepository;
        this.blobStorageService = blobStorageService;
        this.queueSender = queueSender;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public async Task<TechnicalDocumentationCreatedWeb> Handle(
        CreateTechnicalDocumentationCommand request,
        CancellationToken cancellationToken)
    {
        await EnsureProjectExistsAsync(request.TenantId, request.ProjectId, cancellationToken);

        ProjectTechnicalDocumentation documentation = BuildDocumentation(request);
        List<string> uploadedBlobPaths = new();

        try
        {
            List<ProjectTechnicalDocumentationFile> files = new();

            foreach (IFormFile file in request.Files)
            {
                ProjectTechnicalDocumentationFile documentationFile = await UploadFileAsync(
                    request, documentation, file, uploadedBlobPaths, cancellationToken);
                files.Add(documentationFile);
            }

            await documentationRepository.Insert(documentation);
            await fileRepository.InsertRange(files);
            await documentationRepository.SaveChangesAsync(cancellationToken);

            await queueSender.EnqueueAsync(
                documentation.Id,
                request.TenantId,
                request.ProjectId,
                currentUser.Id,
                isManualRetry: false,
                cancellationToken);

            return new TechnicalDocumentationCreatedWeb
            {
                Id = documentation.Id,
                Status = TechnicalDocumentationStatus.Pending
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to create technical documentation for project {ProjectId}; compensating {BlobCount} blob(s)",
                request.ProjectId, uploadedBlobPaths.Count);

            await CompensateBlobsAsync(uploadedBlobPaths, cancellationToken);
            throw;
        }
    }

    private async Task EnsureProjectExistsAsync(
        Guid tenantId, Guid projectId, CancellationToken cancellationToken)
    {
        bool exists = await projectRepository.AnyAsync(
            p => p.TenantId == tenantId && p.Id == projectId,
            cancellationToken);

        if (!exists)
        {
            throw new NotFoundApiException(nameof(Project), projectId.ToString());
        }
    }

    private ProjectTechnicalDocumentation BuildDocumentation(CreateTechnicalDocumentationCommand request) =>
        new()
        {
            TenantId = request.TenantId,
            ProjectId = request.ProjectId,
            CreatedByUserId = currentUser.Id,
            Name = request.Name,
            Description = request.Description,
            Status = TechnicalDocumentationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

    private async Task<ProjectTechnicalDocumentationFile> UploadFileAsync(
        CreateTechnicalDocumentationCommand request,
        ProjectTechnicalDocumentation documentation,
        IFormFile file,
        List<string> uploadedBlobPaths,
        CancellationToken cancellationToken)
    {
        Guid fileId = Guid.NewGuid();
        string blobPath = $"{request.TenantId}/{request.ProjectId}/{documentation.Id}/{fileId}/{file.FileName}";

        using (Stream stream = file.OpenReadStream())
        {
            await blobStorageService.UploadAsync(
                ContainerName, blobPath, stream, file.ContentType, cancellationToken);
        }

        uploadedBlobPaths.Add(blobPath);

        return new ProjectTechnicalDocumentationFile
        {
            TechnicalDocumentationId = documentation.Id,
            TenantId = request.TenantId,
            ProjectId = request.ProjectId,
            OriginalFileName = file.FileName,
            BlobName = blobPath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task CompensateBlobsAsync(
        IReadOnlyCollection<string> uploadedBlobPaths,
        CancellationToken cancellationToken)
    {
        foreach (string blobPath in uploadedBlobPaths)
        {
            try
            {
                await blobStorageService.DeleteAsync(ContainerName, blobPath, cancellationToken);
            }
            catch (Exception deleteEx)
            {
                logger.LogWarning(deleteEx, "Failed to cleanup blob {BlobPath} after upload failure", blobPath);
            }
        }
    }
}
