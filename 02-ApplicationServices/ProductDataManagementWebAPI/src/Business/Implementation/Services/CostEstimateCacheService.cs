using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public class CostEstimateCacheService : ICostEstimateCacheService
    {
        private readonly ICacheService cacheService;
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly IRepository<CostEstimateGroup> groupRepository;
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository;
        private readonly IRepository<CostEstimateItemFieldValue> itemFieldValueRepository;
        private readonly ILogger<CostEstimateCacheService> logger;

        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);

        private static string CostEstimateKey(Guid tenantId, Guid projectId, Guid id)
            => $"ce:{tenantId}:{projectId}:{id}";
        private static string TemplateKey(Guid id) => $"ce-template:{id}";
        private static string GroupsKey(Guid tenantId, Guid projectId, Guid costEstimateId)
            => $"ce-groups:{tenantId}:{projectId}:{costEstimateId}";
        private static string ItemsKey(Guid tenantId, Guid projectId, Guid costEstimateId)
            => $"ce-items:{tenantId}:{projectId}:{costEstimateId}";
        private static string GroupFieldValuesKey(Guid tenantId, Guid projectId, Guid costEstimateId)
            => $"ce-group-fv:{tenantId}:{projectId}:{costEstimateId}";
        private static string ItemFieldValuesKey(Guid tenantId, Guid projectId, Guid costEstimateId)
            => $"ce-item-fv:{tenantId}:{projectId}:{costEstimateId}";

        public CostEstimateCacheService(
            ICacheService cacheService,
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateTemplate> templateRepository,
            IRepository<CostEstimateGroup> groupRepository,
            IRepository<CostEstimateItem> itemRepository,
            IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository,
            IRepository<CostEstimateItemFieldValue> itemFieldValueRepository,
            ILogger<CostEstimateCacheService> logger)
        {
            this.cacheService = cacheService;
            this.costEstimateRepository = costEstimateRepository;
            this.templateRepository = templateRepository;
            this.groupRepository = groupRepository;
            this.itemRepository = itemRepository;
            this.groupFieldValueRepository = groupFieldValueRepository;
            this.itemFieldValueRepository = itemFieldValueRepository;
            this.logger = logger;
        }

        public async Task<CostEstimate?> GetCostEstimateAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetOrAddAsync(
                CostEstimateKey(tenantId, projectId, costEstimateId),
                async () =>
                {
                    var ce = await costEstimateRepository.GetFirstBySearch(
                        c => c.Id == costEstimateId &&
                             c.TenantId == tenantId &&
                             c.ProjectId == projectId &&
                             !c.IsDeleted,
                        q => q.Include(c => c.Owner),
                        q => q.Include(c => c.SelectedCurrency));
                    return ce!;
                },
                DefaultExpiration,
                cancellationToken);

            return costEstimate;
        }

        public async Task<CostEstimateTemplate?> GetTemplateAsync(
            Guid templateId,
            CancellationToken cancellationToken)
        {
            return await cacheService.GetOrAddAsync(
                TemplateKey(templateId),
                async () =>
                {
                    var template = await templateRepository.GetFirstBySearch(
                        t => t.Id == templateId,
                        q => q.Include(t => t.GroupFieldDefinitions),
                        q => q.Include(t => t.SystemFieldDefinitions),
                        q => q.Include(t => t.CalculatedFieldDefinitions),
                        q => q.Include(t => t.GenericFieldDefinitions),
                        q => q.Include(t => t.Currencies));
                    return template!;
                },
                DefaultExpiration,
                cancellationToken);
        }

        public async Task<Dictionary<Guid, CostEstimateGroup>> GetGroupsDictionaryAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            return await cacheService.GetOrAddAsync(
                GroupsKey(tenantId, projectId, costEstimateId),
                async () =>
                {
                    var groups = await groupRepository.GetBySearch(
                        g => g.CostEstimateId == costEstimateId && !g.IsDeleted);
                    return groups.ToDictionary(g => g.Id);
                },
                DefaultExpiration,
                cancellationToken) ?? new Dictionary<Guid, CostEstimateGroup>();
        }

        public async Task<Dictionary<Guid, CostEstimateItem>> GetItemsDictionaryAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            return await cacheService.GetOrAddAsync(
                ItemsKey(tenantId, projectId, costEstimateId),
                async () =>
                {
                    var items = await itemRepository.GetBySearch(
                        i => i.CostEstimateId == costEstimateId && !i.IsDeleted);
                    return items.ToDictionary(i => i.Id);
                },
                DefaultExpiration,
                cancellationToken) ?? new Dictionary<Guid, CostEstimateItem>();
        }

        public async Task<Dictionary<Guid, CostEstimateGroupFieldValue>> GetGroupFieldValuesDictionaryAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            return await cacheService.GetOrAddAsync(
                GroupFieldValuesKey(tenantId, projectId, costEstimateId),
                async () =>
                {
                    // Jedno zapytanie z JOIN zamiast dwóch (najpierw groupIds, potem fieldValues)
                    // HasQueryFilter na CostEstimateGroup (!IsDeleted) stosowany automatycznie
                    var fieldValues = await groupFieldValueRepository.GetBySearch(
                        fv => fv.Group.CostEstimateId == costEstimateId,
                        q => q.Include(fv => fv.FieldDefinition));

                    return fieldValues.ToDictionary(fv => fv.Id);
                },
                DefaultExpiration,
                cancellationToken) ?? new Dictionary<Guid, CostEstimateGroupFieldValue>();
        }

        public async Task<Dictionary<Guid, CostEstimateItemFieldValue>> GetItemFieldValuesDictionaryAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            return await cacheService.GetOrAddAsync(
                ItemFieldValuesKey(tenantId, projectId, costEstimateId),
                async () =>
                {
                    // Jedno zapytanie z JOIN zamiast dwóch (najpierw itemIds, potem fieldValues)
                    // HasQueryFilter na CostEstimateItem (!IsDeleted) stosowany automatycznie
                    var fieldValues = await itemFieldValueRepository.GetBySearch(
                        fv => fv.Item.CostEstimateId == costEstimateId,
                        q => q.Include(fv => fv.FieldDefinition),
                        q => q.Include(fv => fv.Files.Where(f => !f.IsDeleted)));

                    return fieldValues.ToDictionary(fv => fv.Id);
                },
                DefaultExpiration,
                cancellationToken) ?? new Dictionary<Guid, CostEstimateItemFieldValue>();
        }

        public async Task InvalidateCostEstimateAsync(
            Guid costEstimateId,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            await Task.WhenAll(
                cacheService.RemoveCacheByKeyAsync(CostEstimateKey(tenantId, projectId, costEstimateId), cancellationToken),
                cacheService.RemoveCacheByKeyAsync(GroupsKey(tenantId, projectId, costEstimateId), cancellationToken),
                cacheService.RemoveCacheByKeyAsync(ItemsKey(tenantId, projectId, costEstimateId), cancellationToken),
                cacheService.RemoveCacheByKeyAsync(GroupFieldValuesKey(tenantId, projectId, costEstimateId), cancellationToken),
                cacheService.RemoveCacheByKeyAsync(ItemFieldValuesKey(tenantId, projectId, costEstimateId), cancellationToken));

            logger.LogDebug("Invalidated all cache for cost estimate {CostEstimateId}", costEstimateId);
        }

        public async Task InvalidateGroupsAsync(Guid costEstimateId, Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            await cacheService.RemoveCacheByKeyAsync(GroupsKey(tenantId, projectId, costEstimateId), cancellationToken);
        }

        public async Task InvalidateItemsAsync(Guid costEstimateId, Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            await cacheService.RemoveCacheByKeyAsync(ItemsKey(tenantId, projectId, costEstimateId), cancellationToken);
        }

        public async Task InvalidateGroupFieldValuesAsync(Guid costEstimateId, Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            await cacheService.RemoveCacheByKeyAsync(GroupFieldValuesKey(tenantId, projectId, costEstimateId), cancellationToken);
        }

        public async Task InvalidateItemFieldValuesAsync(Guid costEstimateId, Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            await cacheService.RemoveCacheByKeyAsync(ItemFieldValuesKey(tenantId, projectId, costEstimateId), cancellationToken);
        }

        public async Task InvalidateTemplateAsync(Guid templateId, CancellationToken cancellationToken)
        {
            await cacheService.RemoveCacheByKeyAsync(TemplateKey(templateId), cancellationToken);

            logger.LogDebug("Invalidated template cache for template {TemplateId}", templateId);
        }
    }
}
