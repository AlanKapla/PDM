using Business.Interfaces.Configurations;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.GetUserUploadedFiles
{
    public class GetUserUploadedFilesQueryHandler : IRequestHandler<GetUserUploadedFilesQuery, List<ProjectFileWeb>>
    {
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly ICurrentUser currentUser;
        private readonly IBlobStorageService blobStorageService;

        public GetUserUploadedFilesQueryHandler(
            IRepository<ProjectFile> projectFileRepo,
            ICurrentUser currentUser,
            IBlobStorageService blobStorageService)
        {
            this.projectFileRepo = projectFileRepo;
            this.currentUser = currentUser;
            this.blobStorageService = blobStorageService;
        }

        public async Task<List<ProjectFileWeb>> Handle(GetUserUploadedFilesQuery request, CancellationToken cancellationToken)
        {
            // Get files uploaded by current user with user information
            IEnumerable<ProjectFile> files = await projectFileRepo.GetBySearch(
                pf => pf.ProjectId == request.ProjectId &&
                      pf.TenantId == request.TenantId &&
                      pf.UploadedByUserId == currentUser.Id,
                include => include.Include(pf => pf.UploadedByUser)
            );

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);

            // Map to Web model with SAS URLs
            var result = files.Select(pf =>
            {
                Uri sasUri = blobStorageService.GenerateSasUri(containerName, pf.BlobPath, expiresInMinutes: 60);
                
                return new ProjectFileWeb
                {
                    Id = pf.Id,
                    FileName = pf.FileName,
                    DisplayName = pf.DisplayName,
                    PackageName = pf.PackageName,
                    ContentType = pf.ContentType,
                    FileSizeBytes = pf.FileSizeBytes,
                    UploadedAt = pf.UploadedAt,
                    UploadedByUserId = pf.UploadedByUserId,
                    UploadedByUserName = $"{pf.UploadedByUser.FirstName} {pf.UploadedByUser.LastName}".Trim(),
                    SasUrl = sasUri.ToString()
                };
            })
            .OrderByDescending(pf => pf.UploadedAt)
            .ToList();

            return result;
        }
    }
}
