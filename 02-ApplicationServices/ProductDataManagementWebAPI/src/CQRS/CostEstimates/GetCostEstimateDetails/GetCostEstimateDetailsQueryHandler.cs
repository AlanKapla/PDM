using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Business.Implementation.Helpers;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.GetCostEstimateDetails
{
    /// <summary>
    /// Handler dla pobrania szczegółów kosztorysu
    /// Assembles full hierarchy from cached collections (no EF Include chains)
    /// </summary>
    public sealed class GetCostEstimateDetailsQueryHandler : IRequestHandler<GetCostEstimateDetailsQuery, CostEstimateDetailsWeb>
    {
        private readonly ICostEstimateCacheService ceCacheService;
        private readonly ICostEstimateTemplateService costEstimateTemplateService;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly IReadRepository<SharedCostEstimate> sharedCeRepository;
        private readonly IReadRepository<WorkSchedule> workScheduleRepository;
        private readonly ICurrentUser currentUser;

        private const int SasExpirationMinutes = 60;
        private static readonly TimeSpan SasCacheDuration = TimeSpan.FromMinutes(55);

        public GetCostEstimateDetailsQueryHandler(
            ICostEstimateCacheService ceCacheService,
            ICostEstimateTemplateService costEstimateTemplateService,
            IBlobStorageService blobStorageService,
            ICacheService cacheService,
            ICostEstimateAccessService ceAccessService,
            IReadRepository<SharedCostEstimate> sharedCeRepository,
            IReadRepository<WorkSchedule> workScheduleRepository,
            ICurrentUser currentUser)
        {
            this.ceCacheService = ceCacheService;
            this.costEstimateTemplateService = costEstimateTemplateService;
            this.blobStorageService = blobStorageService;
            this.cacheService = cacheService;
            this.ceAccessService = ceAccessService;
            this.sharedCeRepository = sharedCeRepository;
            this.workScheduleRepository = workScheduleRepository;
            this.currentUser = currentUser;
        }

        public async Task<CostEstimateDetailsWeb> Handle(GetCostEstimateDetailsQuery request, CancellationToken cancellationToken)
        {
            // 1. Get cost estimate from cache
            var costEstimate = await ceCacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            // 2. Verify access
            var accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel == CostEstimateAccessLevel.None)
                throw new ForbiddenApiException("Access to this cost estimate is not allowed.");

            // 3. Get template from cache (for name + structure + currencies)
            var template = await ceCacheService.GetTemplateAsync(costEstimate.TemplateId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), costEstimate.TemplateId.ToString());

            // 4. Get all cached collections sequentially (independent Redis calls)
            var groupsDict = await ceCacheService.GetGroupsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);
            var groupFieldValuesDict = await ceCacheService.GetGroupFieldValuesDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);
            var itemsDict = await ceCacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);
            var itemFieldValuesDict = await ceCacheService.GetItemFieldValuesDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            // 5. Build lookup structures for efficient hierarchy assembly
            var groupFieldValuesByGroupId = groupFieldValuesDict.Values
                .GroupBy(fv => fv.GroupId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var mainItemsByGroupId = itemsDict.Values
                .Where(i => i.RelationType == ItemRelationType.None)
                .GroupBy(i => i.GroupId)
                .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Order).ToList());

            var childItemsByParentId = itemsDict.Values
                .Where(i => i.ParentItemId.HasValue)
                .GroupBy(i => i.ParentItemId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Order).ToList());

            var itemFieldValuesByItemId = itemFieldValuesDict.Values
                .GroupBy(fv => fv.ItemId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 6. Build SAS URIs for files (cached separately in Redis)
            var fileSasUris = await BuildFileSasUrisCachedAsync(
                costEstimate.Id, itemFieldValuesDict, cancellationToken);

            // 7. Get template structure via existing service
            var templateStructure = await costEstimateTemplateService.GetTemplateStructureCachedAsync(
                template, cancellationToken);

            // Restricted access widzi tylko kolumny z IsVisible = true.
            // Full access widzi wszystkie kolumny (IsVisible jest ignorowane).
            if (accessLevel is CostEstimateAccessLevel.Restricted or CostEstimateAccessLevel.ReadOnly && templateStructure.UiConfiguration is not null)
            {
                var visibleColumns = templateStructure.UiConfiguration.Columns
                    .Where(c => c.IsVisible)
                    .ToList();

                templateStructure = templateStructure with
                {
                    UiConfiguration = new Business.Interfaces.WebModels.CostEstimateTemplates.UiConfigurationWeb(visibleColumns)
                };
            }

            // 8. Build hierarchical structure
            var allGroups = groupsDict.Values.ToList();
            var childGroupsByParentId = allGroups
                .Where(g => g.ParentGroupId.HasValue)
                .GroupBy(g => g.ParentGroupId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var rootGroups = BuildGroupHierarchy(
                allGroups.Where(g => g.ParentGroupId == null).ToList(),
                childGroupsByParentId,
                groupFieldValuesByGroupId,
                mainItemsByGroupId,
                childItemsByParentId,
                itemFieldValuesByItemId,
                fileSasUris);

            // 9. Resolve currency from cached template
            CostEstimateTemplateCurrency selectedCurrency = template.Currencies
                .FirstOrDefault(c => c.Id == costEstimate.SelectedCurrencyId)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplateCurrency), costEstimate.SelectedCurrencyId.ToString());

            // 10. Load shares — only for Full access (owner / admin), one query with nav props
            IReadOnlyList<CostEstimateShareWeb> sharedWithUsers = [];
            if (accessLevel == CostEstimateAccessLevel.Full)
            {
                var shares = (await sharedCeRepository.GetBySearch(
                    s => s.CostEstimateId == request.CostEstimateId,
                    q => q.Include(s => s.SharedWithProjectMember)
                              .ThenInclude(pm => pm.TenantMember)
                                  .ThenInclude(tm => tm.User))).ToList();

                sharedWithUsers = shares
                    .Select(s => new CostEstimateShareWeb(
                        UserId: s.SharedWithUserId,
                        FullName: $"{s.SharedWithProjectMember.TenantMember.User.FirstName} {s.SharedWithProjectMember.TenantMember.User.LastName}",
                        Email: s.SharedWithProjectMember.TenantMember.User.Email,
                        SharedAt: s.SharedAt
                    ))
                    .OrderBy(sw => sw.FullName)
                    .ToList();
            }

            // 11. Resolve active work schedule linked to this cost estimate (most recently created)
            IEnumerable<WorkSchedule> linkedSchedules = await workScheduleRepository.GetBySearch(
                ws => ws.CostEstimateId == request.CostEstimateId &&
                      ws.TenantId == request.TenantId &&
                      ws.ProjectId == request.ProjectId &&
                      !ws.IsDeleted);
            WorkSchedule? workSchedule = linkedSchedules.OrderByDescending(ws => ws.CreatedAt).FirstOrDefault();

            return new CostEstimateDetailsWeb(
                Id: costEstimate.Id,
                TenantId: costEstimate.TenantId,
                ProjectId: costEstimate.ProjectId,
                TemplateId: costEstimate.TemplateId,
                TemplateName: template.Name,
                SelectedCurrencyId: costEstimate.SelectedCurrencyId,
                SelectedCurrencyCode: selectedCurrency.Code,
                SelectedCurrencySymbol: selectedCurrency.Symbol,
                Name: costEstimate.Name,
                Description: costEstimate.Description,
                Status: costEstimate.Status,
                WorkScheduleId: workSchedule?.Id,
                RootGroups: rootGroups,
                TotalNet: costEstimate.TotalNet,
                TotalGross: costEstimate.TotalGross,
                TotalVat: costEstimate.TotalVat,
                CreatedAt: costEstimate.CreatedAt,
                UpdatedAt: costEstimate.UpdatedAt,
                LastCalculatedAt: costEstimate.LastCalculatedAt,
                OwnerId: costEstimate.OwnerId,
                OwnerName: $"{costEstimate.Owner.FirstName} {costEstimate.Owner.LastName}",
                TemplateStructure: templateStructure,
                AccessLevel: accessLevel,
                SharedWithUsers: sharedWithUsers
            );
        }

        /// <summary>
        /// Builds SAS URIs for all files in item field values, cached in Redis
        /// </summary>
        private async Task<Dictionary<Guid, CostEstimateFieldFileSasInfo>> BuildFileSasUrisCachedAsync(
            Guid costEstimateId,
            Dictionary<Guid, CostEstimateItemFieldValue> itemFieldValuesDict,
            CancellationToken cancellationToken)
        {
            var allFiles = itemFieldValuesDict.Values
                .SelectMany(fv => fv.Files)
                .Where(f => !f.IsDeleted)
                .ToList();

            if (allFiles.Count == 0)
            {
                return new Dictionary<Guid, CostEstimateFieldFileSasInfo>();
            }

            string cacheKey = $"ce-files-sas:{costEstimateId}";
            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.CostEstimates);

            var cached = await cacheService.GetOrAddAsync(
                cacheKey,
                () =>
                {
                    var sasUris = new Dictionary<Guid, CostEstimateFieldFileSasInfo>();

                    foreach (var file in allFiles)
                    {
                        var previewUri = blobStorageService.GenerateSasUri(
                            containerName, file.BlobName, file.OriginalFileName,
                            SasExpirationMinutes, "inline");

                        var downloadUri = blobStorageService.GenerateSasUri(
                            containerName, file.BlobName, file.OriginalFileName,
                            SasExpirationMinutes, "attachment");

                        sasUris[file.Id] = new CostEstimateFieldFileSasInfo
                        {
                            PreviewUri = previewUri.ToString(),
                            DownloadUri = downloadUri.ToString()
                        };
                    }

                    return Task.FromResult(sasUris);
                },
                SasCacheDuration,
                cancellationToken);

            return cached ?? new Dictionary<Guid, CostEstimateFieldFileSasInfo>();
        }

        private List<CostEstimateGroupWeb> BuildGroupHierarchy(
            List<CostEstimateGroup> currentLevelGroups,
            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParentId,
            Dictionary<Guid, List<CostEstimateGroupFieldValue>> groupFieldValuesByGroupId,
            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId,
            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId,
            Dictionary<Guid, List<CostEstimateItemFieldValue>> itemFieldValuesByItemId,
            Dictionary<Guid, CostEstimateFieldFileSasInfo> fileSasUris)
        {
            return currentLevelGroups
                .OrderBy(g => g.Order)
                .Select(group =>
                {
                    groupFieldValuesByGroupId.TryGetValue(group.Id, out var groupFieldValues);

                    return new CostEstimateGroupWeb(
                        Id: group.Id,
                        ParentGroupId: group.ParentGroupId,
                        Level: group.Level,
                        Order: group.Order,
                        FieldValues: (groupFieldValues ?? []).Select(fv =>
                        {
                            var (stringValue, decimalValue, boolValue, dateTimeValue) =
                                FieldValueConverter.GetTypedValue(fv, (int)fv.FieldDefinition.FieldType);

                            return new CostEstimateFieldValueWeb(
                                Id: fv.Id,
                                FieldDefinitionId: fv.FieldDefinitionId,
                                FieldType: (int)fv.FieldDefinition.FieldType,
                                FieldScope: (int)fv.FieldDefinition.FieldScope,
                                FieldName: null,
                                FieldLabel: fv.FieldDefinition.Label,
                                StringValue: stringValue,
                                DecimalValue: decimalValue,
                                BoolValue: boolValue,
                                DateTimeValue: dateTimeValue
                            );
                        }).ToList(),
                        TotalNet: group.TotalNet,
                        TotalGross: group.TotalGross,
                        TotalVat: group.TotalVat,
                        LastCalculatedAt: group.LastCalculatedAt,
                        ChildGroups: BuildGroupHierarchy(
                            childGroupsByParentId.TryGetValue(group.Id, out var children) ? children : [],
                            childGroupsByParentId,
                            groupFieldValuesByGroupId,
                            mainItemsByGroupId,
                            childItemsByParentId,
                            itemFieldValuesByItemId,
                            fileSasUris),
                        Items: (mainItemsByGroupId.TryGetValue(group.Id, out var items) ? items : [])
                            .Select(item => BuildItemWeb(item, childItemsByParentId, itemFieldValuesByItemId, fileSasUris))
                            .ToList(),
                        CreatedAt: group.CreatedAt,
                        UpdatedAt: group.UpdatedAt
                    );
                })
                .ToList();
        }

        private CostEstimateItemWeb BuildItemWeb(
            CostEstimateItem item,
            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId,
            Dictionary<Guid, List<CostEstimateItemFieldValue>> itemFieldValuesByItemId,
            Dictionary<Guid, CostEstimateFieldFileSasInfo> fileSasUris)
        {
            itemFieldValuesByItemId.TryGetValue(item.Id, out var fieldValues);
            childItemsByParentId.TryGetValue(item.Id, out var childItems);

            var options = (childItems ?? [])
                .Where(c => c.RelationType == ItemRelationType.Option)
                .OrderBy(c => c.Order)
                .Select(c => BuildItemWeb(c, childItemsByParentId, itemFieldValuesByItemId, fileSasUris))
                .ToList();

            var components = (childItems ?? [])
                .Where(c => c.RelationType == ItemRelationType.Component)
                .OrderBy(c => c.Order)
                .Select(c => BuildItemWeb(c, childItemsByParentId, itemFieldValuesByItemId, fileSasUris))
                .ToList();

            return new CostEstimateItemWeb(
                Id: item.Id,
                GroupId: item.GroupId,
                ParentItemId: item.ParentItemId,
                RelationType: (int)item.RelationType,
                Order: item.Order,
                NetValue: item.NetValue,
                GrossValue: item.GrossValue,
                VatValue: item.VatValue,
                FieldValues: (fieldValues ?? []).Select(fv =>
                {
                    var (stringValue, decimalValue, boolValue, dateTimeValue) =
                        FieldValueConverter.GetTypedValue(fv, (int)fv.FieldDefinition.FieldType);

                    List<CostEstimateFieldFileWeb>? files = null;
                    if (fv.FieldDefinition.FieldType == FieldType.ItemSystemFiles && fv.Files.Count > 0)
                    {
                        files = fv.Files
                            .Where(f => !f.IsDeleted)
                            .OrderBy(f => f.Order)
                            .Select(f =>
                            {
                                fileSasUris.TryGetValue(f.Id, out var sasInfo);
                                return new CostEstimateFieldFileWeb(
                                    Id: f.Id,
                                    OriginalFileName: f.OriginalFileName,
                                    ContentType: f.ContentType,
                                    FileSize: f.FileSize,
                                    Order: f.Order,
                                    SasUriPreview: sasInfo?.PreviewUri,
                                    SasUriDownload: sasInfo?.DownloadUri,
                                    CreatedAt: f.CreatedAt
                                );
                            })
                            .ToList();
                    }

                    return new CostEstimateFieldValueWeb(
                        Id: fv.Id,
                        FieldDefinitionId: fv.FieldDefinitionId,
                        FieldType: (int)fv.FieldDefinition.FieldType,
                        FieldScope: (int)fv.FieldDefinition.FieldScope,
                        FieldName: fv.FieldDefinition.FieldName,
                        FieldLabel: fv.FieldDefinition.Label,
                        StringValue: stringValue,
                        DecimalValue: decimalValue,
                        BoolValue: boolValue,
                        DateTimeValue: dateTimeValue,
                        Files: files
                    );
                }).ToList(),
                Options: options,
                Components: components,
                CreatedAt: item.CreatedAt,
                UpdatedAt: item.UpdatedAt
            );
        }
    }
}
