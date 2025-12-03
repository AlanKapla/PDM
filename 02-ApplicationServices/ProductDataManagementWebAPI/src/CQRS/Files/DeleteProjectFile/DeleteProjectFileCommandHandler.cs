using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.DeleteProjectFile
{
    public class DeleteProjectFileCommandHandler : IRequestHandler<DeleteProjectFileCommand, Unit>
    {
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ILogger<DeleteProjectFileCommandHandler> logger;

        public DeleteProjectFileCommandHandler(
            IRepository<ProjectFile> projectFileRepo,
            IBlobStorageService blobStorageService,
            ILogger<DeleteProjectFileCommandHandler> logger)
        {
            this.projectFileRepo = projectFileRepo;
            this.blobStorageService = blobStorageService;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteProjectFileCommand request, CancellationToken cancellationToken)
        {
            // Validation is handled by DeleteProjectFileCommandValidator

            // Get file (validated to exist by validator)
            ProjectFile file = (await projectFileRepo.GetFirstBySearch(
                pf => pf.Id == request.FileId &&
                      pf.ProjectId == request.ProjectId &&
                      pf.TenantId == request.TenantId))!;

            // Delete from blob storage
            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);
            try
            {
                await blobStorageService.DeleteAsync(containerName, file.BlobPath, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, 
                    "Failed to delete blob {BlobPath} from storage, continuing with database deletion", 
                    file.BlobPath);
            }

            // Delete from database
            await projectFileRepo.Delete(file);

            logger.LogInformation(
                "File {FileId} deleted from project {ProjectId}",
                request.FileId, request.ProjectId);

            return Unit.Value;
        }
    }
}
