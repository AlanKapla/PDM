using Business.Interfaces.Configurations;
using CQRS.CostEstimates.GetAdditionalFields;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models.CostEstimates;
using Entities.Models.Projects;
using Entities.Models.WorkSchedules;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.GetCostEstimateDetails
{
    /// <summary>
    /// Handler dla pobrania szczegółów kosztorysu.
    /// Assembles full hierarchy from cached collections (no EF Include chains).
    /// </summary>
    public sealed class GetCostEstimateDetailsQueryHandler : IRequestHandler<GetCostEstimateDetailsQuery, CostEstimateDetailsWeb>
    {
        private readonly ICostEstimateCacheService ceCacheService;
        private readonly IBlobStorageService blobStorageService;
        private readonly ICacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly IReadRepository<SharedCostEstimate> sharedCeRepository;
        private readonly IReadRepository<WorkSchedule> workScheduleRepository;
        private readonly IReadRepository<ProjectCurrency> projectCurrencyRepository;
        private readonly IReadRepository<CostEstimateFieldSchema> fieldSchemaRepository;
        private readonly ICurrentUser currentUser;

        private const int SasExpirationMinutes = 60;
        private static readonly TimeSpan SasCacheDuration = TimeSpan.FromMinutes(55);
        private readonly string costEstimatesContainerName;

        public GetCostEstimateDetailsQueryHandler(
            ICostEstimateCacheService ceCacheService,
            IBlobStorageService blobStorageService,
            ICacheService cacheService,
            ICostEstimateAccessService ceAccessService,
            IReadRepository<SharedCostEstimate> sharedCeRepository,
            IReadRepository<WorkSchedule> workScheduleRepository,
            IReadRepository<ProjectCurrency> projectCurrencyRepository,
            IReadRepository<CostEstimateFieldSchema> fieldSchemaRepository,
            ICurrentUser currentUser)
        {
            this.ceCacheService = ceCacheService;
            this.blobStorageService = blobStorageService;
            this.cacheService = cacheService;
            this.ceAccessService = ceAccessService;
            this.sharedCeRepository = sharedCeRepository;
            this.workScheduleRepository = workScheduleRepository;
            this.projectCurrencyRepository = projectCurrencyRepository;
            this.fieldSchemaRepository = fieldSchemaRepository;
            this.currentUser = currentUser;
            this.costEstimatesContainerName = BlobStorageSettings.GetContainerName(BlobContainerNames.CostEstimates);
        }

        public async Task<CostEstimateDetailsWeb> Handle(GetCostEstimateDetailsQuery request, CancellationToken cancellationToken)
        {
            CostEstimate costEstimate = await ceCacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel == CostEstimateAccessLevel.None)
            {
                throw new ForbiddenApiException("Access to this cost estimate is not allowed.");
            }

            Dictionary<Guid, CostEstimateGroup> groupsDict = await ceCacheService.GetGroupsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            Dictionary<Guid, CostEstimateItem> itemsDict = await ceCacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            List<CostEstimateGroup> allGroups = groupsDict.Values.ToList();

            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParentId = allGroups
                .Where(g => g.ParentGroupId.HasValue)
                .GroupBy(g => g.ParentGroupId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId = itemsDict.Values
                .Where(i => i.RelationType == ItemRelationType.None)
                .GroupBy(i => i.GroupId)
                .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Order).ToList());

            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId = itemsDict.Values
                .Where(i => i.ParentItemId.HasValue)
                .GroupBy(i => i.ParentItemId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Order).ToList());

            List<CostEstimateGroupWeb> rootGroups = BuildGroupHierarchy(
                allGroups.Where(g => g.ParentGroupId == null).ToList(),
                childGroupsByParentId,
                mainItemsByGroupId,
                childItemsByParentId);

            ProjectCurrency? projectCurrency = await projectCurrencyRepository.GetFirstBySearch(
                c => c.ProjectId == costEstimate.ProjectId,
                cancellationToken);

            IReadOnlyList<CostEstimateShareWeb> sharedWithUsers = await LoadSharesAsync(
                request.CostEstimateId, accessLevel, cancellationToken);

            WorkSchedule? linkedWorkSchedule = await workScheduleRepository.GetFirstBySearch(
                ws => ws.CostEstimateId == request.CostEstimateId,
                cancellationToken);

            IEnumerable<CostEstimateFieldSchema> fieldSchemasQuery = await fieldSchemaRepository.GetBySearch(
                f => f.CostEstimateId == request.CostEstimateId);
            List<CostEstimateFieldSchemaWeb> fieldSchemas = fieldSchemasQuery
                .OrderBy(f => f.Order)
                .Select(GetAdditionalFieldsQueryHandler.MapToFieldSchemaWeb)
                .ToList();
            List<CostEstimateAdditionalFieldWeb> additionalFields = fieldSchemasQuery
                .Where(f => f.IsAdditionalField)
                .OrderBy(f => f.Order)
                .Select(GetAdditionalFieldsQueryHandler.MapToAdditionalFieldWeb)
                .ToList();

            return new CostEstimateDetailsWeb(
                Id: costEstimate.Id,
                TenantId: costEstimate.TenantId,
                ProjectId: costEstimate.ProjectId,
                SelectedCurrencyCode: projectCurrency?.Code,
                SelectedCurrencySymbol: projectCurrency?.Symbol,
                Name: costEstimate.Name,
                Description: costEstimate.Description,
                Status: costEstimate.Status,
                WorkScheduleId: linkedWorkSchedule?.Id,
                RootGroups: rootGroups,
                FieldSchemas: fieldSchemas,
                AdditionalFields: additionalFields,
                TotalNet: costEstimate.TotalNet,
                TotalGross: costEstimate.TotalGross,
                TotalVat: costEstimate.TotalVat,
                CreatedAt: costEstimate.CreatedAt,
                UpdatedAt: costEstimate.UpdatedAt,
                LastCalculatedAt: costEstimate.LastCalculatedAt,
                OwnerId: costEstimate.OwnerId,
                OwnerName: $"{costEstimate.Owner.FirstName} {costEstimate.Owner.LastName}",
                AccessLevel: accessLevel,
                SharedWithUsers: sharedWithUsers
            );
        }

        private async Task<IReadOnlyList<CostEstimateShareWeb>> LoadSharesAsync(
            Guid costEstimateId,
            CostEstimateAccessLevel accessLevel,
            CancellationToken cancellationToken)
        {
            if (accessLevel != CostEstimateAccessLevel.Full)
            {
                return [];
            }

            IEnumerable<SharedCostEstimate> shares = await sharedCeRepository.GetBySearch(
                s => s.CostEstimateId == costEstimateId,
                q => q.Include(s => s.SharedWithProjectMember)
                          .ThenInclude(pm => pm.TenantMember)
                              .ThenInclude(tm => tm.User));

            return shares
                .Select(s => new CostEstimateShareWeb(
                    UserId: s.SharedWithUserId,
                    FullName: $"{s.SharedWithProjectMember.TenantMember.User.FirstName} {s.SharedWithProjectMember.TenantMember.User.LastName}",
                    Email: s.SharedWithProjectMember.TenantMember.User.Email,
                    SharedAt: s.SharedAt))
                .OrderBy(sw => sw.FullName)
                .ToList();
        }

        private List<CostEstimateGroupWeb> BuildGroupHierarchy(
            List<CostEstimateGroup> currentLevelGroups,
            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParentId,
            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId,
            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId)
        {
            return currentLevelGroups
                .OrderBy(g => g.Order)
                .Select(group => MapGroup(
                    group,
                    childGroupsByParentId,
                    mainItemsByGroupId,
                    childItemsByParentId))
                .ToList();
        }

        private CostEstimateGroupWeb MapGroup(
            CostEstimateGroup group,
            Dictionary<Guid, List<CostEstimateGroup>> childGroupsByParentId,
            Dictionary<Guid, List<CostEstimateItem>> mainItemsByGroupId,
            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId)
        {
            List<CostEstimateGroup> childGroups = childGroupsByParentId.TryGetValue(group.Id, out List<CostEstimateGroup>? children)
                ? children
                : [];

            List<CostEstimateItem> groupItems = mainItemsByGroupId.TryGetValue(group.Id, out List<CostEstimateItem>? items)
                ? items
                : [];

            return new CostEstimateGroupWeb(
                Id: group.Id,
                ParentGroupId: group.ParentGroupId,
                Level: group.Level,
                Order: group.Order,
                Name: group.Name,
                TotalNet: group.TotalNet,
                TotalGross: group.TotalGross,
                TotalVat: group.TotalVat,
                AdditionalFieldValues: group.AdditionalFieldValues
                    .Select(MapAdditionalFieldValue)
                    .ToList(),
                LastCalculatedAt: group.LastCalculatedAt,
                ChildGroups: BuildGroupHierarchy(childGroups, childGroupsByParentId, mainItemsByGroupId, childItemsByParentId),
                Items: groupItems
                    .Select(item => BuildItemWeb(item, childItemsByParentId))
                    .ToList(),
                CreatedAt: group.CreatedAt,
                UpdatedAt: group.UpdatedAt
            );
        }

        private CostEstimateItemWeb BuildItemWeb(
            CostEstimateItem item,
            Dictionary<Guid, List<CostEstimateItem>> childItemsByParentId)
        {
            childItemsByParentId.TryGetValue(item.Id, out List<CostEstimateItem>? childItems);

            List<CostEstimateItemWeb> options = (childItems ?? [])
                .Where(c => c.RelationType == ItemRelationType.Option)
                .OrderBy(c => c.Order)
                .Select(c => BuildItemWeb(c, childItemsByParentId))
                .ToList();

            List<CostEstimateItemWeb> components = (childItems ?? [])
                .Where(c => c.RelationType == ItemRelationType.Component)
                .OrderBy(c => c.Order)
                .Select(c => BuildItemWeb(c, childItemsByParentId))
                .ToList();

            return new CostEstimateItemWeb(
                Id: item.Id,
                GroupId: item.GroupId,
                ParentItemId: item.ParentItemId,
                RelationType: (int)item.RelationType,
                Order: item.Order,
                Name: item.Name,
                Quantity: item.Quantity,
                Unit: item.Unit,
                UnitPriceNet: item.UnitPriceNet,
                VatRate: item.VatRate,
                UnitPriceGross: item.UnitPriceGross,
                NetValue: item.NetValue,
                GrossValue: item.GrossValue,
                VatValue: item.VatValue,
                IsSelected: item.IsSelected,
                IsStageWork: item.IsStageWork,
                AdditionalFieldValues: item.AdditionalFieldValues
                    .Select(MapAdditionalFieldValue)
                    .ToList(),
                Options: options,
                Components: components,
                Files: item.Files
                    .Where(f => !f.IsDeleted)
                    .Select(MapItemFile)
                    .ToList(),
                CreatedAt: item.CreatedAt,
                UpdatedAt: item.UpdatedAt
            );
        }

        private static CostEstimateAdditionalFieldValueWeb MapAdditionalFieldValue(CostEstimateAdditionalFieldValue v)
        {
            return new CostEstimateAdditionalFieldValueWeb(
                Id: v.Id,
                AdditionalFieldId: v.FieldSchemaId,
                StringValue: v.StringValue,
                DecimalValue: v.DecimalValue,
                BoolValue: v.BoolValue,
                DateTimeValue: v.DateTimeValue
            );
        }

        private CostEstimateItemFileWeb MapItemFile(CostEstimateItemFile f)
        {
            Uri sasUriPreview = blobStorageService.GenerateSasUri(
                costEstimatesContainerName, f.BlobName, f.OriginalFileName,
                SasExpirationMinutes, "inline");

            Uri sasUriDownload = blobStorageService.GenerateSasUri(
                costEstimatesContainerName, f.BlobName, f.OriginalFileName,
                SasExpirationMinutes, "attachment");

            return new CostEstimateItemFileWeb(
                Id: f.Id,
                ItemId: f.ItemId,
                OriginalFileName: f.OriginalFileName,
                ContentType: f.ContentType,
                FileSize: f.FileSize,
                Order: f.Order,
                SasUriPreview: sasUriPreview.ToString(),
                SasUriDownload: sasUriDownload.ToString(),
                CreatedAt: f.CreatedAt
            );
        }

    }
}
