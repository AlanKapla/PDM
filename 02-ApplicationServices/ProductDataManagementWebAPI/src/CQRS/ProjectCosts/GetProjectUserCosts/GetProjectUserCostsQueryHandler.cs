using Business.Interfaces.Configurations;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.GetProjectUserCosts
{
    public class GetProjectUserCostsQueryHandler : IRequestHandler<GetProjectUserCostsQuery, IEnumerable<ProjectCostListItemWeb>>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICurrentUser currentUser;

        public GetProjectUserCostsQueryHandler(
            IRepository<ProjectCost> projectCostRepo,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser)
        {
            this.projectCostRepo = projectCostRepo;
            this.blobStorageService = blobStorageService;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<ProjectCostListItemWeb>> Handle(GetProjectUserCostsQuery request, CancellationToken cancellationToken)
        {
            // ProjectMemberHandler already validated tenant isolation and project membership

            // Get all costs for the current user in the project with shared relationships
            var costs = await projectCostRepo.GetBySearch(
                pc => pc.ProjectId == request.ProjectId 
                    && pc.TenantId == request.TenantId 
                    && pc.UserId == currentUser.Id
                    && !pc.IsDeleted,
                query => query.Include(pc => pc.SharedWith));

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.ProjectCosts);

            // Map to DTOs
            var result = costs.Select(pc =>
            {
                string? previewSasUrl = null;
                string? downloadSasUrl = null;

                // Generate SAS URLs if document exists
                if (pc.HasDocument && !string.IsNullOrWhiteSpace(pc.DocumentBlobPath) && !string.IsNullOrWhiteSpace(pc.DocumentFileName))
                {
                    Uri sasUriPreview = blobStorageService.GenerateSasUri(
                        containerName, 
                        pc.DocumentBlobPath, 
                        pc.DocumentFileName, 
                        expiresInMinutes: 60, 
                        contentDisposition: "inline");
                    
                    Uri sasUriDownload = blobStorageService.GenerateSasUri(
                        containerName, 
                        pc.DocumentBlobPath, 
                        pc.DocumentFileName, 
                        expiresInMinutes: 60, 
                        contentDisposition: "attachment");

                    previewSasUrl = sasUriPreview.ToString();
                    downloadSasUrl = sasUriDownload.ToString();
                }

                // Get shared user IDs from navigation collection
                var sharedWithUserIds = pc.SharedWith
                    .Select(spc => spc.SharedWithUserId)
                    .ToList();

                return new ProjectCostListItemWeb
                {
                    Id = pc.Id,
                    UserId = pc.UserId,
                    UserName = $"{currentUser.FirstName} {currentUser.LastName}",
                    Name = pc.Name,
                    Place = pc.Place,
                    Date = pc.Date,
                    Description = pc.Description,
                    NetAmount = pc.NetAmount,
                    VatRate = pc.VatRate,
                    GrossAmount = pc.GrossAmount,
                    IsClosed = pc.IsClosed,
                    HasDocument = pc.HasDocument,
                    DocumentFileName = pc.DocumentFileName,
                    PreviewSasUrl = previewSasUrl,
                    DownloadSasUrl = downloadSasUrl,
                    SharedWithUserIds = sharedWithUserIds,
                    CreatedAt = pc.CreatedAt
                };
            })
            .OrderByDescending(c => c.Date)
            .ThenByDescending(c => c.CreatedAt)
            .ToList();

            return result;
        }
    }
}
