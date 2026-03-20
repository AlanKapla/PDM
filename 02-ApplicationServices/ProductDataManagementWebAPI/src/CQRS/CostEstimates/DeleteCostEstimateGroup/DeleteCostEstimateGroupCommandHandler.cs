using Business.Interfaces.Constants;
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
    public sealed class DeleteCostEstimateGroupCommandHandler : IRequestHandler<DeleteCostEstimateGroupCommand, Unit>
    {
        private readonly IRepository<CostEstimateGroup> groupRepository;
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository;
        private readonly IRepository<CostEstimateItemFieldValue> itemFieldValueRepository;
        private readonly IRepository<CostEstimateFieldFile> fieldFileRepository;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
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
            ICostEstimateAccessService ceAccessService,
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
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteCostEstimateGroupCommand request, CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel == CostEstimateAccessLevel.None)
            {
                throw new ForbiddenApiException("Access to this cost estimate is not allowed.");
            }

            if (accessLevel == CostEstimateAccessLevel.Restricted)
            {
                throw new ForbiddenApiException("Shared users cannot modify the cost estimate structure.");
            }

            var groupsDict = await cacheService.GetGroupsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!groupsDict.ContainsKey(request.GroupId))
            {
                throw new NotFoundApiException(nameof(CostEstimateGroup), request.GroupId.ToString());
            }

            DateTime now = DateTime.UtcNow;

            // Collect all descendant group IDs from cached dictionary
            HashSet<Guid> allGroupIds = CollectDescendantGroupIds(groupsDict, request.GroupId);
            allGroupIds.Add(request.GroupId);

            // Collect item IDs from cache
            var itemsDict = await cacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            HashSet<Guid> allItemIds = itemsDict.Values
                .Where(i => allGroupIds.Contains(i.GroupId))
                .Select(i => i.Id)
                .ToHashSet();

            // Soft-delete files + delete blobs (before DB changes)
            if (allItemIds.Count > 0)
            {
                List<CostEstimateFieldFile> filesToDelete = (await fieldFileRepository.GetBySearch(
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
                List<CostEstimateItem> itemsToDelete = (await itemRepository.GetBySearch(
                    i => allItemIds.Contains(i.Id) && !i.IsDeleted)).ToList();

                foreach (var item in itemsToDelete)
                {
                    item.IsDeleted = true;
                    item.DeletedAt = now;
                }

                await itemRepository.UpdateRange(itemsToDelete);
            }

            // Soft-delete groups
            List<CostEstimateGroup> groupsToDelete = (await groupRepository.GetBySearch(
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
            Guid rootGroupId)
        {
            Dictionary<Guid, List<Guid>> childrenByParentId = groupsDict.Values
                .Where(g => g.ParentGroupId.HasValue)
                .GroupBy(g => g.ParentGroupId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

            HashSet<Guid> result = [];
            Queue<Guid> queue = new();

            if (!childrenByParentId.TryGetValue(rootGroupId, out List<Guid>? directChildren))
            {
                return result;
            }

            foreach (Guid id in directChildren)
            {
                queue.Enqueue(id);
            }

            while (queue.Count > 0)
            {
                Guid id = queue.Dequeue();
                result.Add(id);
                if (childrenByParentId.TryGetValue(id, out List<Guid>? children))
                {
                    foreach (Guid childId in children)
                    {
                        queue.Enqueue(childId);
                    }
                }
            }

            return result;
        }
    }
}
