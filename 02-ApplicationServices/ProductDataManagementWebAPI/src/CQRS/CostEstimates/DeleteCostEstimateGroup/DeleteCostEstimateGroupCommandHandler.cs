using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.DeleteCostEstimateGroup
{
    public class DeleteCostEstimateGroupCommandHandler : IRequestHandler<DeleteCostEstimateGroupCommand, Unit>
    {
        private readonly IRepository<CostEstimateGroup> groupRepository;
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository;
        private readonly IRepository<CostEstimateItemFieldValue> itemFieldValueRepository;
        private readonly IRepository<CostEstimateFieldFile> fieldFileRepository;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<DeleteCostEstimateGroupCommandHandler> logger;

        public DeleteCostEstimateGroupCommandHandler(
            IRepository<CostEstimateGroup> groupRepository,
            IRepository<CostEstimateItem> itemRepository,
            IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository,
            IRepository<CostEstimateItemFieldValue> itemFieldValueRepository,
            IRepository<CostEstimateFieldFile> fieldFileRepository,
            IBlobStorageService blobStorageService,
            ICostEstimateCacheService cacheService,
            ICurrentUser currentUser,
            ILogger<DeleteCostEstimateGroupCommandHandler> logger)
        {
            this.groupRepository = groupRepository;
            this.itemRepository = itemRepository;
            this.groupFieldValueRepository = groupFieldValueRepository;
            this.itemFieldValueRepository = itemFieldValueRepository;
            this.fieldFileRepository = fieldFileRepository;
            this.blobStorageService = blobStorageService;
            this.cacheService = cacheService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteCostEstimateGroupCommand request, CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, currentUser.Id, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            var groupsDict = await cacheService.GetGroupsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!groupsDict.ContainsKey(request.GroupId))
            {
                throw new NotFoundApiException(nameof(CostEstimateGroup), request.GroupId.ToString());
            }

            var now = DateTime.UtcNow;

            // Collect all descendant group IDs from cached dictionary
            var allGroupIds = CollectDescendantGroupIds(groupsDict, request.GroupId);
            allGroupIds.Add(request.GroupId);

            // Collect item IDs from cache
            var itemsDict = await cacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            var allItemIds = itemsDict.Values
                .Where(i => allGroupIds.Contains(i.GroupId))
                .Select(i => i.Id)
                .ToHashSet();

            // Soft-delete files + delete blobs (before DB changes)
            if (allItemIds.Count > 0)
            {
                var filesToDelete = (await fieldFileRepository.GetBySearch(
                    f => f.CostEstimateId == request.CostEstimateId &&
                         allItemIds.Contains(f.FieldValue.ItemId) &&
                         !f.IsDeleted)).ToList();

                if (filesToDelete.Count > 0)
                {
                    string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.CostEstimates);

                    foreach (var file in filesToDelete)
                    {
                        file.IsDeleted = true;
                        file.DeletedAt = now;

                        await blobStorageService.DeleteAsync(containerName, file.BlobName, cancellationToken);
                    }

                    await fieldFileRepository.UpdateRange(filesToDelete);

                    logger.LogInformation(
                        "Soft-deleted {FileCount} files and removed blobs for deleted groups in cost estimate {CostEstimateId}",
                        filesToDelete.Count, request.CostEstimateId);
                }

                // Delete item field values (hard delete — no IsDeleted column)
                await itemFieldValueRepository.ExecuteDeleteAsync(
                    fv => allItemIds.Contains(fv.ItemId), cancellationToken);
            }

            // Delete group field values (hard delete — no IsDeleted column)
            await groupFieldValueRepository.ExecuteDeleteAsync(
                fv => allGroupIds.Contains(fv.GroupId), cancellationToken);

            // Soft-delete items
            if (allItemIds.Count > 0)
            {
                var itemsToDelete = (await itemRepository.GetBySearch(
                    i => allItemIds.Contains(i.Id) && !i.IsDeleted)).ToList();

                foreach (var item in itemsToDelete)
                {
                    item.IsDeleted = true;
                    item.DeletedAt = now;
                }

                await itemRepository.UpdateRange(itemsToDelete);
            }

            // Soft-delete groups
            var groupsToDelete = (await groupRepository.GetBySearch(
                g => allGroupIds.Contains(g.Id) && !g.IsDeleted)).ToList();

            foreach (var g in groupsToDelete)
            {
                g.IsDeleted = true;
                g.DeletedAt = now;
            }

            await groupRepository.UpdateRange(groupsToDelete);
            await groupRepository.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            await cacheService.InvalidateCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return Unit.Value;
        }

        private static HashSet<Guid> CollectDescendantGroupIds(
            Dictionary<Guid, CostEstimateGroup> groupsDict,
            Guid parentGroupId)
        {
            var result = new HashSet<Guid>();

            foreach (var kvp in groupsDict)
            {
                if (kvp.Value.ParentGroupId == parentGroupId)
                {
                    result.Add(kvp.Key);
                    result.UnionWith(CollectDescendantGroupIds(groupsDict, kvp.Key));
                }
            }

            return result;
        }
    }
}
