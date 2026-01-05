using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.GetProjectCosts
{
    /// <summary>
    /// Handler do pobierania kosztów projektu według zakresu (All, Mine, Shared)
    /// </summary>
    public class GetProjectCostsQueryHandler : IRequestHandler<GetProjectCostsQuery, IEnumerable<ProjectCostListItemWeb>>
    {
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IRepository<SharedProjectCost> sharedProjectCostRepo;
        private readonly IRepository<User> userRepo;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICurrentUser currentUser;

        public GetProjectCostsQueryHandler(
            IRepository<ProjectCost> projectCostRepo,
            IRepository<SharedProjectCost> sharedProjectCostRepo,
            IRepository<User> userRepo,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser)
        {
            this.projectCostRepo = projectCostRepo;
            this.sharedProjectCostRepo = sharedProjectCostRepo;
            this.userRepo = userRepo;
            this.blobStorageService = blobStorageService;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<ProjectCostListItemWeb>> Handle(GetProjectCostsQuery request, CancellationToken cancellationToken)
        {
            // ProjectMemberHandler already validated tenant isolation and project membership

            IEnumerable<ProjectCost> costs;

            switch (request.Scope)
            {
                case ResourceScope.All:
                    // Get all costs in the project (requires READ_ALL permission)
                    costs = await projectCostRepo.GetBySearch(
                        pc => pc.ProjectId == request.ProjectId 
                            && pc.TenantId == request.TenantId 
                            && !pc.IsDeleted,
                        query => query
                            .Include(pc => pc.SharedWith)
                            .Include(pc => pc.TenantMember).ThenInclude(tm => tm.User));
                    break;

                case ResourceScope.Mine:
                    // Get only costs owned by the current user (requires READ permission)
                    costs = await projectCostRepo.GetBySearch(
                        pc => pc.ProjectId == request.ProjectId 
                            && pc.TenantId == request.TenantId 
                            && pc.UserId == currentUser.Id
                            && !pc.IsDeleted,
                        query => query.Include(pc => pc.SharedWith));
                    break;

                case ResourceScope.Shared:
                    // Get costs shared with the current user (requires READ_SHARED permission)
                    var sharedCosts = await sharedProjectCostRepo.GetBySearch(
                        spc => spc.ProjectId == request.ProjectId 
                            && spc.TenantId == request.TenantId 
                            && spc.SharedWithUserId == currentUser.Id,
                        query => query
                            .Include(spc => spc.ProjectCost).ThenInclude(pc => pc.SharedWith)
                            .Include(spc => spc.ProjectCost).ThenInclude(pc => pc.TenantMember).ThenInclude(tm => tm.User));

                    // Filter out deleted costs and extract ProjectCost entities
                    costs = sharedCosts
                        .Where(spc => !spc.ProjectCost.IsDeleted)
                        .Select(spc => spc.ProjectCost)
                        .Distinct()
                        .ToList();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Scope));
            }

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

                // Get user name - either from navigation property or current user
                string userName;
                if (request.Scope == ResourceScope.All && pc.TenantMember?.User != null)
                {
                    userName = $"{pc.TenantMember.User.FirstName} {pc.TenantMember.User.LastName}";
                }
                else if (pc.UserId == currentUser.Id)
                {
                    userName = $"{currentUser.FirstName} {currentUser.LastName}";
                }
                else
                {
                    // Fallback - fetch user if needed (shouldn't happen often)
                    var user = userRepo.GetBySearch(u => u.Id == pc.UserId).Result.FirstOrDefault();
                    userName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown User";
                }

                // Get shared user IDs from navigation collection
                var sharedWithUserIds = pc.SharedWith
                    .Select(spc => spc.SharedWithUserId)
                    .ToList();

                return new ProjectCostListItemWeb
                {
                    Id = pc.Id,
                    UserId = pc.UserId,
                    UserName = userName,
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
