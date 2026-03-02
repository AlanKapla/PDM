using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.DeleteCostEstimateItem
{
    public class DeleteCostEstimateItemCommandHandler : IRequestHandler<DeleteCostEstimateItemCommand, Unit>
    {
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<CostEstimateItemFieldValue> itemFieldValueRepository;
        private readonly IRepository<CostEstimateFieldFile> fieldFileRepository;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<DeleteCostEstimateItemCommandHandler> logger;

        public DeleteCostEstimateItemCommandHandler(
            IRepository<CostEstimateItem> itemRepository,
            IRepository<CostEstimateItemFieldValue> itemFieldValueRepository,
            IRepository<CostEstimateFieldFile> fieldFileRepository,
            IBlobStorageService blobStorageService,
            ICostEstimateCacheService cacheService,
            ICurrentUser currentUser,
            ILogger<DeleteCostEstimateItemCommandHandler> logger)
        {
            this.itemRepository = itemRepository;
            this.itemFieldValueRepository = itemFieldValueRepository;
            this.fieldFileRepository = fieldFileRepository;
            this.blobStorageService = blobStorageService;
            this.cacheService = cacheService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteCostEstimateItemCommand request, CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, currentUser.Id, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            var itemsDict = await cacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!itemsDict.ContainsKey(request.ItemId))
            {
                throw new NotFoundApiException(nameof(CostEstimateItem), request.ItemId.ToString());
            }

            var now = DateTime.UtcNow;

            // Collect all descendant item IDs from cached dictionary
            var allItemIds = CollectDescendantItemIds(itemsDict, request.ItemId);
            allItemIds.Add(request.ItemId);

            // Soft-delete files + delete blobs
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
                    "Soft-deleted {FileCount} files and removed blobs for deleted items in cost estimate {CostEstimateId}",
                    filesToDelete.Count, request.CostEstimateId);
            }

            // Delete item field values (hard delete — no IsDeleted column)
            await itemFieldValueRepository.ExecuteDeleteAsync(
                fv => allItemIds.Contains(fv.ItemId), cancellationToken);

            // Soft-delete items
            var itemsToDelete = (await itemRepository.GetBySearch(
                i => allItemIds.Contains(i.Id) && !i.IsDeleted)).ToList();

            foreach (var item in itemsToDelete)
            {
                item.IsDeleted = true;
                item.DeletedAt = now;
            }

            await itemRepository.UpdateRange(itemsToDelete);
            await itemRepository.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            await cacheService.InvalidateCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return Unit.Value;
        }

        private static HashSet<Guid> CollectDescendantItemIds(
            Dictionary<Guid, CostEstimateItem> itemsDict,
            Guid parentItemId)
        {
            var result = new HashSet<Guid>();

            foreach (var kvp in itemsDict)
            {
                if (kvp.Value.ParentItemId == parentItemId)
                {
                    result.Add(kvp.Key);
                    result.UnionWith(CollectDescendantItemIds(itemsDict, kvp.Key));
                }
            }

            return result;
        }
    }
}
