using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.Costs;
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
        private readonly IReadRepository<BaseCostAttachment> attachmentRepository;
        private readonly IUserService userService;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICurrentUser currentUser;

        private static readonly string ContainerName =
            BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

        public GetProjectCostsQueryHandler(
            IRepository<ProjectCost> projectCostRepo,
            IRepository<SharedProjectCost> sharedProjectCostRepo,
            IReadRepository<BaseCostAttachment> attachmentRepository,
            IUserService userService,
            IBlobStorageService blobStorageService,
            ICurrentUser currentUser)
        {
            this.projectCostRepo = projectCostRepo;
            this.sharedProjectCostRepo = sharedProjectCostRepo;
            this.attachmentRepository = attachmentRepository;
            this.userService = userService;
            this.blobStorageService = blobStorageService;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<ProjectCostListItemWeb>> Handle(
            GetProjectCostsQuery request,
            CancellationToken cancellationToken)
        {
            IEnumerable<ProjectCost> costs = await LoadCostsAsync(request);

            List<ProjectCost> costList = costs.ToList();
            HashSet<Guid> costIds = costList.Select(pc => pc.Id).ToHashSet();

            ILookup<Guid, BaseCostAttachment> attachmentsByCostId = await LoadAttachmentLookupAsync(costIds);

            Dictionary<Guid, ProjectMemberUserInfo> membersDict = (await userService.GetProjectMembersAsync(
                request.TenantId, request.ProjectId, cancellationToken))
                .ToDictionary(m => m.UserId);

            return costList
                .Select(pc => MapToWeb(pc, attachmentsByCostId[pc.Id].FirstOrDefault(), membersDict))
                .OrderByDescending(c => c.Date)
                .ThenByDescending(c => c.CreatedAt)
                .ToList();
        }

        private async Task<IEnumerable<ProjectCost>> LoadCostsAsync(GetProjectCostsQuery request)
        {
            switch (request.Scope)
            {
                case ResourceScope.All:
                    return await projectCostRepo.GetBySearch(
                        pc => pc.ProjectId == request.ProjectId
                            && pc.TenantId == request.TenantId,
                        query => query.Include(pc => pc.SharedWith));

                case ResourceScope.Mine:
                    return await projectCostRepo.GetBySearch(
                        pc => pc.ProjectId == request.ProjectId
                            && pc.TenantId == request.TenantId
                            && pc.UserId == currentUser.Id,
                        query => query.Include(pc => pc.SharedWith));

                case ResourceScope.Shared:
                    IEnumerable<SharedProjectCost> sharedCosts = await sharedProjectCostRepo.GetBySearch(
                        spc => spc.ProjectId == request.ProjectId
                            && spc.TenantId == request.TenantId
                            && spc.SharedWithUserId == currentUser.Id,
                        query => query
                            .Include(spc => spc.ProjectCost).ThenInclude(pc => pc.SharedWith));

                    return sharedCosts
                        .Where(spc => spc.ProjectCost is not null)
                        .Select(spc => spc.ProjectCost)
                        .Distinct()
                        .ToList();

                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Scope));
            }
        }

        private async Task<ILookup<Guid, BaseCostAttachment>> LoadAttachmentLookupAsync(HashSet<Guid> costIds)
        {
            if (costIds.Count == 0)
            {
                return Enumerable.Empty<BaseCostAttachment>().ToLookup(a => a.CostId);
            }

            List<BaseCostAttachment> attachments = (await attachmentRepository.GetBySearch(
                a => costIds.Contains(a.CostId))).ToList();

            return attachments.ToLookup(a => a.CostId);
        }

        private ProjectCostListItemWeb MapToWeb(
            ProjectCost pc,
            BaseCostAttachment? attachment,
            Dictionary<Guid, ProjectMemberUserInfo> membersDict)
        {
            string? previewSasUrl = null;
            string? downloadSasUrl = null;

            if (attachment is not null)
            {
                Uri sasUriPreview = blobStorageService.GenerateSasUri(
                    ContainerName,
                    attachment.BlobName,
                    attachment.OriginalFileName,
                    expiresInMinutes: 60,
                    contentDisposition: "inline");

                Uri sasUriDownload = blobStorageService.GenerateSasUri(
                    ContainerName,
                    attachment.BlobName,
                    attachment.OriginalFileName,
                    expiresInMinutes: 60,
                    contentDisposition: "attachment");

                previewSasUrl = sasUriPreview.ToString();
                downloadSasUrl = sasUriDownload.ToString();
            }

            string userName = membersDict.TryGetValue(pc.UserId, out ProjectMemberUserInfo? member)
                ? member.FullName
                : "Unknown";

            List<Guid> sharedWithUserIds = pc.SharedWith
                .Select(spc => spc.SharedWithUserId)
                .ToList();

            return new ProjectCostListItemWeb
            {
                Id = pc.Id,
                UserId = pc.UserId,
                UserName = userName,
                Name = pc.Name,
                Place = pc.Place,
                Date = pc.Date ?? DateTime.UtcNow,
                Description = pc.Description,
                NetAmount = pc.Net,
                GrossAmount = pc.Gross,
                IsAccepted = pc.IsAccepted,
                HasDocument = attachment is not null,
                DocumentFileName = attachment?.OriginalFileName,
                PreviewSasUrl = previewSasUrl,
                DownloadSasUrl = downloadSasUrl,
                SharedWithUserIds = sharedWithUserIds,
                CreatedAt = pc.CreatedAt
            };
        }
    }
}
