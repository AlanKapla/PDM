using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using Repositiories.Repository.Interfaces;

namespace CQRS.Files.DeleteProjectFile
{
    public class DeleteProjectFileCommandHandler : IRequestHandler<DeleteProjectFileCommand, Unit>
    {
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IReadRepository<SharedProjectFile> sharedFileRepo;
        private readonly IRepository<ProjectFileVersion> projectFileVersionRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<DeleteProjectFileCommandHandler> logger;

        public DeleteProjectFileCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IReadRepository<SharedProjectFile> sharedFileRepo,
            IRepository<ProjectFileVersion> projectFileVersionRepo,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser,
            ILogger<DeleteProjectFileCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.sharedFileRepo = sharedFileRepo;
            this.projectFileVersionRepo = projectFileVersionRepo;
            this.blobStorageService = blobStorageService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteProjectFileCommand request, CancellationToken cancellationToken)
        {
            // 1. Pobierz plik
            ProjectFile? file = await projectFileRepo.GetFirstBySearch(
                pf => pf.Id == request.FileId &&
                      pf.ProjectId == request.ProjectId &&
                      pf.TenantId == request.TenantId &&
                      !pf.IsDeleted);

            if (file == null)
                throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());

            // 2. Sprawdź ownership - tylko owner może usuwać pliki
            if (file.OwnerId != currentUser.Id)
            {
                throw new ForbiddenApiException("Only file owner can delete the file");
            }

            // 3. Pobierz wersje osobno
            var versions = await projectFileVersionRepo.GetBySearch(
                v => v.ProjectFileId == file.Id && !v.IsDeleted);

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);

            // 4. Usuń blobs i soft delete wersji
            foreach (var version in versions)
            {
                try
                {
                    await blobStorageService.DeleteAsync(containerName, version.BlobPath, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, 
                        "Failed to delete blob {BlobPath} from storage, continuing with database soft delete", 
                        version.BlobPath);
                }

                version.IsDeleted = true;
                version.DeletedAt = DateTime.UtcNow;
                await projectFileVersionRepo.Update(version);
            }

            // 5. Soft delete pliku
            file.IsDeleted = true;
            file.DeletedAt = DateTime.UtcNow;
            file.CurrentVersionId = null;
            await projectFileRepo.Update(file);

            logger.LogInformation(
                "File {FileId} with {VersionCount} versions soft deleted from project {ProjectId} by owner {UserId}",
                request.FileId, versions.Count(), request.ProjectId, currentUser.Id);

            return Unit.Value;
        }
    }
}
