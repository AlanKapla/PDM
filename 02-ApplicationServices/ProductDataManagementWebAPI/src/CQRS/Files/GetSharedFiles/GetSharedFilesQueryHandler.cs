using Business.Interfaces.Configurations;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.GetSharedFiles
{
    public class GetSharedFilesQueryHandler : IRequestHandler<GetSharedFilesQuery, List<SharedProjectFileWeb>>
    {
        private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
        private readonly ICurrentUser currentUser;
        private readonly IBlobStorageService blobStorageService;

        public GetSharedFilesQueryHandler(
            IRepository<SharedProjectFile> sharedProjectFileRepo,
            ICurrentUser currentUser,
            IBlobStorageService blobStorageService)
        {
            this.sharedProjectFileRepo = sharedProjectFileRepo;
            this.currentUser = currentUser;
            this.blobStorageService = blobStorageService;
        }

        public async Task<List<SharedProjectFileWeb>> Handle(GetSharedFilesQuery request, CancellationToken cancellationToken)
        {
            // Pobierz pliki udostępnione aktualnemu użytkownikowi
            IEnumerable<SharedProjectFile> sharedFiles = await sharedProjectFileRepo.GetBySearch(
                spf => spf.ProjectId == request.ProjectId &&
                       spf.TenantId == request.TenantId &&
                       spf.SharedWithUserId == currentUser.Id,
                include => include
                    .Include(spf => spf.ProjectFile)
                        .ThenInclude(pf => pf.UploadedByUser)
                    .Include(spf => spf.SharedByUser)
            );

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);

            // Mapowanie do Web modelu z SAS URLs
            var result = sharedFiles.Select(spf =>
            {
                Uri sasUri = blobStorageService.GenerateSasUri(
                    containerName, 
                    spf.ProjectFile.BlobPath, 
                    expiresInMinutes: 60);

                return new SharedProjectFileWeb
                {
                    Id = spf.Id,
                    ProjectFileId = spf.ProjectFileId,
                    FileName = spf.ProjectFile.FileName,
                    DisplayName = spf.ProjectFile.DisplayName,
                    PackageName = spf.ProjectFile.PackageName,
                    ContentType = spf.ProjectFile.ContentType,
                    FileSizeBytes = spf.ProjectFile.FileSizeBytes,
                    UploadedAt = spf.ProjectFile.UploadedAt,
                    SharedAt = spf.SharedAt,
                    SharedByUserId = spf.SharedByUserId,
                    SharedByUserName = $"{spf.SharedByUser.FirstName} {spf.SharedByUser.LastName}".Trim(),
                    OriginalOwnerUserId = spf.ProjectFile.UploadedByUserId,
                    OriginalOwnerUserName = $"{spf.ProjectFile.UploadedByUser.FirstName} {spf.ProjectFile.UploadedByUser.LastName}".Trim(),
                    SasUrl = sasUri.ToString()
                };
            })
            .OrderByDescending(spf => spf.SharedAt)
            .ToList();

            return result;
        }
    }
}
