using Business.Interfaces.Configurations;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.GetSharedProjectCosts
{
    public class GetSharedProjectCostsQueryHandler : IRequestHandler<GetSharedProjectCostsQuery, IEnumerable<SharedProjectCostWeb>>
    {
        private readonly IRepository<SharedProjectCost> sharedProjectCostRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICurrentUser currentUser;

        public GetSharedProjectCostsQueryHandler(
            IRepository<SharedProjectCost> sharedProjectCostRepo,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser)
        {
            this.sharedProjectCostRepo = sharedProjectCostRepo;
            this.blobStorageService = blobStorageService;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<SharedProjectCostWeb>> Handle(GetSharedProjectCostsQuery request, CancellationToken cancellationToken)
        {
            // ProjectMemberHandler already validated tenant isolation and project membership

            // Get all costs shared with the current user
            var sharedCosts = await sharedProjectCostRepo.GetBySearch(
                spc => spc.ProjectId == request.ProjectId 
                    && spc.TenantId == request.TenantId 
                    && spc.SharedWithUserId == currentUser.Id,
                query => query
                    .Include(spc => spc.ProjectCost)
                    .Include(spc => spc.SharedWithTenantMember).ThenInclude(tm => tm.User)
                    .Include(spc => spc.SharedByTenantMember).ThenInclude(tm => tm.User));

            // Filter out deleted costs
            var activeCosts = sharedCosts.Where(spc => !spc.ProjectCost.IsDeleted).ToList();

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.ProjectCosts);

            // Map to DTOs
            var result = activeCosts.Select(spc =>
            {
                string? previewSasUrl = null;
                string? downloadSasUrl = null;

                // Generate SAS URLs if document exists
                if (spc.ProjectCost.HasDocument 
                    && !string.IsNullOrWhiteSpace(spc.ProjectCost.DocumentBlobPath) 
                    && !string.IsNullOrWhiteSpace(spc.ProjectCost.DocumentFileName))
                {
                    Uri sasUriPreview = blobStorageService.GenerateSasUri(
                        containerName, 
                        spc.ProjectCost.DocumentBlobPath, 
                        spc.ProjectCost.DocumentFileName, 
                        expiresInMinutes: 60, 
                        contentDisposition: "inline");
                    
                    Uri sasUriDownload = blobStorageService.GenerateSasUri(
                        containerName, 
                        spc.ProjectCost.DocumentBlobPath, 
                        spc.ProjectCost.DocumentFileName, 
                        expiresInMinutes: 60, 
                        contentDisposition: "attachment");

                    previewSasUrl = sasUriPreview.ToString();
                    downloadSasUrl = sasUriDownload.ToString();
                }

                return new SharedProjectCostWeb
                {
                    Id = spc.Id,
                    ProjectCostId = spc.ProjectCostId,
                    SharedWithUserId = spc.SharedWithUserId,
                    SharedWithUserName = $"{spc.SharedWithTenantMember.User.FirstName} {spc.SharedWithTenantMember.User.LastName}",
                    SharedByUserId = spc.SharedByUserId,
                    SharedByUserName = $"{spc.SharedByTenantMember.User.FirstName} {spc.SharedByTenantMember.User.LastName}",
                    SharedAt = spc.SharedAt,
                    CostName = spc.ProjectCost.Name,
                    CostPlace = spc.ProjectCost.Place,
                    CostDate = spc.ProjectCost.Date,
                    CostDescription = spc.ProjectCost.Description,
                    CostNetAmount = spc.ProjectCost.NetAmount,
                    CostVatRate = spc.ProjectCost.VatRate,
                    CostGrossAmount = spc.ProjectCost.GrossAmount,
                    CostIsClosed = spc.ProjectCost.IsClosed,
                    CostHasDocument = spc.ProjectCost.HasDocument,
                    CostDocumentFileName = spc.ProjectCost.DocumentFileName,
                    PreviewSasUrl = previewSasUrl,
                    DownloadSasUrl = downloadSasUrl
                };
            })
            .OrderByDescending(c => c.SharedAt)
            .ToList();

            return result;
        }
    }
}
