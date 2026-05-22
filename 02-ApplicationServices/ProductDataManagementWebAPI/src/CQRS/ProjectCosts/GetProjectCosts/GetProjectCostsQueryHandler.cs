using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.ProjectCosts;
using Entities.Models.Costs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.GetProjectCosts
{
    /// <summary>
    /// Handler do pobierania kosztów projektu według zakresu (All, Mine, Shared)
    /// </summary>
    public sealed class GetProjectCostsQueryHandler : IRequestHandler<GetProjectCostsQuery, IEnumerable<ProjectCostListItemWeb>>
    {
        private readonly IReadRepository<ProjectCost> projectCostRepo;
        private readonly IReadRepository<SharedProjectCost> sharedProjectCostRepo;
        private readonly IReadRepository<BaseCostAttachment> attachmentRepository;
        private readonly IUserService userService;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICurrentUser currentUser;
        private readonly IContractorService contractorService;

        private static readonly string ContainerName =
            BlobStorageSettings.GetContainerName(BlobContainerNames.CostTrackers);

        public GetProjectCostsQueryHandler(
            IReadRepository<ProjectCost> projectCostRepo,
            IReadRepository<SharedProjectCost> sharedProjectCostRepo,
            IReadRepository<BaseCostAttachment> attachmentRepository,
            IUserService userService,
            IBlobStorageService blobStorageService,
            IContractorService contractorService,
            ICurrentUser currentUser)
        {
            this.projectCostRepo = projectCostRepo;
            this.sharedProjectCostRepo = sharedProjectCostRepo;
            this.attachmentRepository = attachmentRepository;
            this.userService = userService;
            this.blobStorageService = blobStorageService;
            this.contractorService = contractorService;
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

            List<Guid> contractorIds = costList
                .Where(pc => pc.ContractorId.HasValue)
                .Select(pc => pc.ContractorId!.Value)
                .Distinct()
                .ToList();

            Dictionary<Guid, string> contractorNames = await contractorService.GetNamesByIdsAsync(
                contractorIds, request.TenantId, cancellationToken);

            return costList
                .Select(pc => MapToWeb(pc, attachmentsByCostId[pc.Id].FirstOrDefault(), membersDict, contractorNames))
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
                            && pc.TenantId == request.TenantId
                            && !pc.IsDeleted,
                        query => query.Include(pc => pc.SharedWith));

                case ResourceScope.Mine:
                    return await projectCostRepo.GetBySearch(
                        pc => pc.ProjectId == request.ProjectId
                            && pc.TenantId == request.TenantId
                            && pc.UserId == currentUser.Id
                            && !pc.IsDeleted,
                        query => query.Include(pc => pc.SharedWith));

                case ResourceScope.Shared:
                    IEnumerable<SharedProjectCost> sharedCosts = await sharedProjectCostRepo.GetBySearch(
                        spc => spc.ProjectId == request.ProjectId
                            && spc.TenantId == request.TenantId
                            && spc.SharedWithUserId == currentUser.Id
                            && !spc.ProjectCost.IsDeleted,
                        query => query
                            .Include(spc => spc.ProjectCost).ThenInclude(pc => pc.SharedWith));

                    return sharedCosts
                        .Where(spc => spc.ProjectCost is not null)
                        .Select(spc => spc.ProjectCost)
                        .Distinct()
                        .ToList();

                default:
                    throw new ValidationApiException($"Unsupported scope value: {request.Scope}");
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
            Dictionary<Guid, ProjectMemberUserInfo> membersDict,
            Dictionary<Guid, string> contractorNames)
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
                ContractorId = pc.ContractorId,
                ContractorName = pc.ContractorId.HasValue
                    ? contractorNames.GetValueOrDefault(pc.ContractorId.Value)
                    : null,
                Number = pc.Number,
                Date = pc.Date,
                Description = pc.Description,
                Net = pc.Net,
                Gross = pc.Gross,
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
