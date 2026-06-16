using Business.Implementation.Helpers;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.AI;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.CreateCostEstimateFromAIPreview
{
    public sealed class CreateCostEstimateFromAIPreviewCommandHandler
        : IRequestHandler<CreateCostEstimateFromAIPreviewCommand, Guid>
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateGroup> groupRepository;
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<CostEstimateFieldSchema> fieldSchemaRepository;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<CreateCostEstimateFromAIPreviewCommandHandler> logger;

        private static readonly Guid ItemQuantityFieldId = new Guid("00000000-0000-0000-0000-000000000101");
        private static readonly Guid ItemUnitFieldId = new Guid("00000000-0000-0000-0000-000000000102");
        private static readonly Guid ItemUnitPriceNetFieldId = new Guid("00000000-0000-0000-0000-000000000200");
        private static readonly Guid ItemVatRateFieldId = new Guid("00000000-0000-0000-0000-000000000201");

        public CreateCostEstimateFromAIPreviewCommandHandler(
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateGroup> groupRepository,
            IRepository<CostEstimateItem> itemRepository,
            IRepository<CostEstimateFieldSchema> fieldSchemaRepository,
            ICurrentUser currentUser,
            ILogger<CreateCostEstimateFromAIPreviewCommandHandler> logger)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.groupRepository = groupRepository;
            this.itemRepository = itemRepository;
            this.fieldSchemaRepository = fieldSchemaRepository;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Guid> Handle(
            CreateCostEstimateFromAIPreviewCommand request,
            CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;

            CostEstimate costEstimate = BuildCostEstimate(request, now);
            List<CostEstimateFieldSchema> defaultSchema =
                DefaultCostEstimateFieldSchemaFactory.CreateDefaultSchema(costEstimate.Id, now);

            await costEstimateRepository.Insert(costEstimate);
            await fieldSchemaRepository.InsertRange(defaultSchema);
            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            await CreateGroupsAndItemsAsync(costEstimate.Id, request.Preview.Groups, now, cancellationToken);

            logger.LogInformation(
                "Created cost estimate {CostEstimateId} from AI preview with {GroupCount} groups",
                costEstimate.Id, request.Preview.Groups.Count);

            return costEstimate.Id;
        }

        private CostEstimate BuildCostEstimate(CreateCostEstimateFromAIPreviewCommand request, DateTime now)
        {
            return new CostEstimate
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                OwnerId = currentUser.Id,
                Name = request.Name,
                Description = request.Description,
                Status = CostEstimateStatus.Draft,
                TotalNet = null,
                TotalGross = null,
                TotalVat = null,
                CreatedAt = now,
                IsDeleted = false
            };
        }

        private async Task CreateGroupsAndItemsAsync(
            Guid costEstimateId,
            List<AIGroupPreviewWeb> groups,
            DateTime now,
            CancellationToken cancellationToken)
        {
            Dictionary<string, Guid> tempIdToGroupId = new Dictionary<string, Guid>();
            int totalItemCount = 0;

            foreach (AIGroupPreviewWeb groupPreview in groups.OrderBy(g => g.Order))
            {
                Guid groupId = await CreateGroupAsync(costEstimateId, groupPreview, tempIdToGroupId, now, cancellationToken);
                tempIdToGroupId[groupPreview.TempId] = groupId;
                totalItemCount += await CreateItemsForGroupAsync(costEstimateId, groupId, groupPreview.Items, now, cancellationToken);
            }

            logger.LogInformation(
                "Created {GroupCount} groups and {ItemCount} items for cost estimate {CostEstimateId}",
                groups.Count, totalItemCount, costEstimateId);
        }

        private async Task<Guid> CreateGroupAsync(
            Guid costEstimateId,
            AIGroupPreviewWeb groupPreview,
            Dictionary<string, Guid> tempIdToGroupId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            Guid groupId = Guid.NewGuid();

            CostEstimateGroup group = new CostEstimateGroup
            {
                Id = groupId,
                CostEstimateId = costEstimateId,
                Name = groupPreview.Name ?? string.Empty,
                ParentGroupId = groupPreview.ParentTempId is not null && tempIdToGroupId.ContainsKey(groupPreview.ParentTempId)
                    ? tempIdToGroupId[groupPreview.ParentTempId]
                    : null,
                Level = 0,
                Order = groupPreview.Order,
                CreatedAt = now,
                IsDeleted = false
            };

            await groupRepository.Insert(group);
            await groupRepository.SaveChangesAsync(cancellationToken);

            return groupId;
        }

        private async Task<int> CreateItemsForGroupAsync(
            Guid costEstimateId,
            Guid groupId,
            List<AIItemPreviewWeb> items,
            DateTime now,
            CancellationToken cancellationToken)
        {
            int count = 0;

            foreach (AIItemPreviewWeb itemPreview in items.OrderBy(i => i.Order))
            {
                Guid itemId = await CreateItemAsync(
                    costEstimateId, groupId, null, ItemRelationType.None, itemPreview.Name,
                    itemPreview.FieldValues, itemPreview.Order, now, cancellationToken);

                foreach (AIComponentPreviewWeb componentPreview in itemPreview.Components.OrderBy(c => c.Order))
                {
                    await CreateItemAsync(
                        costEstimateId, groupId, itemId, ItemRelationType.Component, componentPreview.Name,
                        componentPreview.FieldValues, componentPreview.Order, now, cancellationToken);
                }

                count++;
            }

            return count;
        }

        private async Task<Guid> CreateItemAsync(
            Guid costEstimateId,
            Guid groupId,
            Guid? parentItemId,
            ItemRelationType relationType,
            string name,
            List<AIFieldValueWeb> fieldValues,
            int order,
            DateTime now,
            CancellationToken cancellationToken)
        {
            CostEstimateItem item = new CostEstimateItem
            {
                Id = Guid.NewGuid(),
                CostEstimateId = costEstimateId,
                GroupId = groupId,
                ParentItemId = parentItemId,
                RelationType = relationType,
                Order = order,
                Name = name,
                IsSelected = true,
                IsStageWork = relationType == ItemRelationType.None,
                CreatedAt = now,
                IsDeleted = false
            };

            ApplyFieldValues(item, fieldValues);

            await itemRepository.Insert(item);
            await itemRepository.SaveChangesAsync(cancellationToken);

            return item.Id;
        }

        private static void ApplyFieldValues(CostEstimateItem item, List<AIFieldValueWeb> fieldValues)
        {
            foreach (AIFieldValueWeb fv in fieldValues)
            {
                if (fv.FieldDefinitionId == ItemQuantityFieldId)
                {
                    item.Quantity = fv.DecimalValue;
                }
                else if (fv.FieldDefinitionId == ItemUnitFieldId)
                {
                    item.Unit = fv.StringValue;
                }
                else if (fv.FieldDefinitionId == ItemUnitPriceNetFieldId)
                {
                    item.UnitPriceNet = fv.DecimalValue;
                }
                else if (fv.FieldDefinitionId == ItemVatRateFieldId)
                {
                    item.VatRate = fv.DecimalValue;
                }
            }
        }

    }
}
