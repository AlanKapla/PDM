using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using Repositiories.Repository.Interfaces;

namespace CQRS.Files.DeleteProjectFile
{
    public class DeleteProjectFileCommandHandler : IRequestHandler<DeleteProjectFileCommand, Unit>
    {
        private readonly IReadRepository<ProjectFile> projectFileRepo;
        private readonly IRepository<ProjectFileVersion> projectFileVersionRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ILogger<DeleteProjectFileCommandHandler> logger;

        public DeleteProjectFileCommandHandler(
            IReadRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectFileVersion> projectFileVersionRepo,
            IBlobStorageService blobStorageService,
            ILogger<DeleteProjectFileCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.projectFileVersionRepo = projectFileVersionRepo;
            this.blobStorageService = blobStorageService;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteProjectFileCommand request, CancellationToken cancellationToken)
        {
            ProjectFile file = (await projectFileRepo.GetFirstBySearch(
                pf => pf.Id == request.FileId &&
                      pf.ProjectId == request.ProjectId &&
                      pf.TenantId == request.TenantId &&
                      !pf.IsDeleted,
                cancellationToken,
                include => include.Include(pf => pf.Versions)))!;

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);

            foreach (var version in file.Versions.Where(v => !v.IsDeleted))
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

            file.IsDeleted = true;
            file.DeletedAt = DateTime.UtcNow;
            file.CurrentVersionId = null;
            await projectFileRepo.Update(file);

            logger.LogInformation(
                "File {FileId} with {VersionCount} versions soft deleted from project {ProjectId}",
                request.FileId, file.Versions.Count, request.ProjectId);

            return Unit.Value;
        }
    }
}
