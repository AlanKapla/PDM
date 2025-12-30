using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Helpers;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.UploadProjectFileVersion
{
    public class UploadProjectFileVersionCommandHandler : IRequestHandler<UploadProjectFileVersionCommand, Unit>
    {
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IRepository<ProjectFileVersion> projectFileVersionRepo;
        private readonly IRepository<ProjectFileVersionComment> commentRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly IAccessService accessService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UploadProjectFileVersionCommandHandler> logger;

        public UploadProjectFileVersionCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectFileVersion> projectFileVersionRepo,
            IRepository<ProjectFileVersionComment> commentRepo,
            IBlobStorageService blobStorageService,
            IAccessService accessService,
            ICurrentUser currentUser,
            ILogger<UploadProjectFileVersionCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.projectFileVersionRepo = projectFileVersionRepo;
            this.commentRepo = commentRepo;
            this.blobStorageService = blobStorageService;
            this.accessService = accessService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UploadProjectFileVersionCommand request, CancellationToken cancellationToken)
        {
            // Sprawdź uprawnienia do edycji pliku (rola Editor/Admin + właściciel lub udostępniony)
            bool canEdit = await accessService.CanEditProjectFileAsync(
                request.TenantId,
                request.ProjectId,
                request.FileId,
                cancellationToken);

            if (!canEdit)
            {
                throw new ForbiddenApiException(
                    "You do not have permission to upload a new version of this file. " +
                    "You must be a project Editor or Admin and either own the file or have it shared with you.");
            }

            // Pobierz istniejący plik z wersjami
            var projectFiles = await projectFileRepo.GetBySearch(
                pf => pf.Id == request.FileId &&
                      pf.TenantId == request.TenantId &&
                      pf.ProjectId == request.ProjectId &&
                      !pf.IsDeleted,
                include => include.Include(pf => pf.Package)
                                  .Include(pf => pf.Versions.Where(v => !v.IsDeleted))
            );

            ProjectFile? projectFile = projectFiles.FirstOrDefault();

            if (projectFile == null)
            {
                throw new NotFoundApiException(
                    objectType: nameof(ProjectFile),
                    objectId: request.FileId.ToString(),
                    message: $"File with ID {request.FileId} does not exist or has been deleted");
            }

            // Sprawdź czy plik ma takie samo rozszerzenie jak oryginał
            string originalExtension = Path.GetExtension(projectFile.FileName).ToLowerInvariant();
            string newFileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();

            if (originalExtension != newFileExtension)
            {
                throw new ValidationApiException(
                    $"The new version must have the same extension as the original. Expected: {originalExtension}, received: {newFileExtension}");
            }

            // Oblicz numer następnej wersji
            int nextVersionNumber = projectFile.Versions.Any()
                ? projectFile.Versions.Max(v => v.VersionNumber) + 1
                : 1;

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);

            // Normalizacja nazwy paczki dla blob storage
            string packageNameForBlob = FileHelper.NormalizePackageNameForBlobPath(projectFile.Package.Name);

            // Utwórz nową wersję
            ProjectFileVersion newVersion = new ProjectFileVersion
            {
                ProjectFileId = request.FileId,
                VersionNumber = nextVersionNumber,
                CreatedByUserId = currentUser.Id,
                ContentType = request.File.ContentType,
                FileSizeBytes = request.File.Length,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            Guid versionId = newVersion.Id;
            string blobFileName = $"{versionId}{newFileExtension}";
            string blobPath = $"{request.TenantId}/{request.ProjectId}/{packageNameForBlob}/{request.FileId}/{nextVersionNumber}/{blobFileName}";

            newVersion.BlobFileName = blobFileName;
            newVersion.BlobPath = blobPath;

            try
            {
                // Upload pliku do blob storage
                using (Stream stream = request.File.OpenReadStream())
                {
                    await blobStorageService.UploadAsync(
                        containerName,
                        blobPath,
                        stream,
                        request.File.ContentType,
                        cancellationToken);
                }

                // Zapisz wersję do bazy
                await projectFileVersionRepo.Insert(newVersion);

                // Zaktualizuj CurrentVersionId w ProjectFile
                projectFile.CurrentVersionId = versionId;
                await projectFileRepo.Update(projectFile);

                // Dodaj komentarz jeśli został podany
                if (!string.IsNullOrWhiteSpace(request.Comment))
                {
                    ProjectFileVersionComment comment = new ProjectFileVersionComment
                    {
                        ProjectFileVersionId = versionId,
                        UserId = currentUser.Id,
                        Content = request.Comment.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await commentRepo.Insert(comment);
                }

                // Zapisz wszystkie zmiany
                await projectFileRepo.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Created new version {VersionNumber} for file {FileId} in project {ProjectId}. Blob path: {BlobPath}. Comment: {HasComment}",
                    nextVersionNumber, request.FileId, request.ProjectId, blobPath, !string.IsNullOrWhiteSpace(request.Comment));

                return Unit.Value;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "Failed to upload new version for file {FileId} in project {ProjectId}. Version number: {VersionNumber}",
                    request.FileId, request.ProjectId, nextVersionNumber);

                // Próba usunięcia pliku z blob storage jeśli upload się nie powiódł
                try
                {
                    await blobStorageService.DeleteAsync(containerName, blobPath, cancellationToken);
                }
                catch (Exception deleteEx)
                {
                    logger.LogWarning(deleteEx, "Failed to cleanup blob {BlobPath} after upload failure", blobPath);
                }

                throw;
            }
        }
    }
}
