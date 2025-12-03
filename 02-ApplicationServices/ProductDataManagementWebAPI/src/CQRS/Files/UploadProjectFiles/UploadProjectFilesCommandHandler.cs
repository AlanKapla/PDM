using Business.Interfaces.Configurations;
using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.UploadProjectFiles
{
    public class UploadProjectFilesCommandHandler : IRequestHandler<UploadProjectFilesCommand, List<ProjectFileWeb>>
    {
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UploadProjectFilesCommandHandler> logger;

        public UploadProjectFilesCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser,
            ILogger<UploadProjectFilesCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.blobStorageService = blobStorageService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<List<ProjectFileWeb>> Handle(UploadProjectFilesCommand request, CancellationToken cancellationToken)
        {
            var uploadedFiles = new List<ProjectFileWeb>();
            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);

            foreach (var fileItem in request.Files)
            {
                try
                {
                    var file = fileItem.File;
                    
                    // Generate GUID for blob name
                    Guid fileId = Guid.NewGuid();
                    string fileExtension = Path.GetExtension(file.FileName);
                    string blobFileName = $"{fileId}{fileExtension}";
                    string blobPath = $"{request.TenantId}/{request.ProjectId}/{currentUser.Id}/{request.PackageName}/{blobFileName}";

                    // Upload to blob storage
                    using (var stream = file.OpenReadStream())
                    {
                        await blobStorageService.UploadAsync(
                            containerName,
                            blobPath,
                            stream,
                            file.ContentType,
                            cancellationToken);
                    }

                    // DisplayName: use provided or default to source filename without extension
                    string displayName = !string.IsNullOrWhiteSpace(fileItem.DisplayName)
                        ? fileItem.DisplayName
                        : Path.GetFileNameWithoutExtension(file.FileName);

                    // Save to database
                    var projectFile = new ProjectFile
                    {
                        Id = fileId,
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        UploadedByUserId = currentUser.Id,
                        FileName = file.FileName,  // Original source filename
                        PackageName = request.PackageName,
                        DisplayName = displayName,
                        ContentType = file.ContentType,
                        FileSizeBytes = file.Length,
                        BlobPath = blobPath,
                        UploadedAt = DateTime.UtcNow
                    };

                    await projectFileRepo.Insert(projectFile);

                    // Generate SAS URL for immediate access
                    Uri sasUri = blobStorageService.GenerateSasUri(containerName, blobPath, expiresInMinutes: 60);

                    // Map to Web model
                    uploadedFiles.Add(new ProjectFileWeb
                    {
                        Id = projectFile.Id,
                        FileName = projectFile.FileName,
                        DisplayName = projectFile.DisplayName,
                        PackageName = projectFile.PackageName,
                        ContentType = projectFile.ContentType,
                        FileSizeBytes = projectFile.FileSizeBytes,
                        UploadedAt = projectFile.UploadedAt,
                        UploadedByUserId = projectFile.UploadedByUserId,
                        UploadedByUserName = string.Empty, // Not loaded in upload context
                        SasUrl = sasUri.ToString()
                    });

                    logger.LogInformation(
                        "File {FileName} (ID: {FileId}) uploaded to project {ProjectId} by user {UserId}",
                        file.FileName, fileId, request.ProjectId, currentUser.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Error uploading file {FileName} to project {ProjectId}",
                        fileItem.File.FileName, request.ProjectId);
                    throw;
                }
            }

            return uploadedFiles;
        }
    }
}
