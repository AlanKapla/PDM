using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.AI;
using CQRS.CostEstimates.Validators;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;
using FieldType = Entities.Models.CostEstimates.FieldType;

namespace CQRS.CostEstimates.CreateCostEstimateFromAIPreview
{
    public sealed class CreateCostEstimateFromAIPreviewCommandHandler
        : IRequestHandler<CreateCostEstimateFromAIPreviewCommand, Guid>
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateGroup> groupRepository;
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository;
        private readonly IRepository<CostEstimateItemFieldValue> itemFieldValueRepository;
        private readonly IReadRepository<CostEstimateTemplate> templateRepository;
        private readonly ICurrentUser currentUser;
        private readonly CostEstimateFieldValueValidator fieldValueValidator;
        private readonly ILogger<CreateCostEstimateFromAIPreviewCommandHandler> logger;

        public CreateCostEstimateFromAIPreviewCommandHandler(
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateGroup> groupRepository,
            IRepository<CostEstimateItem> itemRepository,
            IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository,
            IRepository<CostEstimateItemFieldValue> itemFieldValueRepository,
            IReadRepository<CostEstimateTemplate> templateRepository,
            ICurrentUser currentUser,
            CostEstimateFieldValueValidator fieldValueValidator,
            ILogger<CreateCostEstimateFromAIPreviewCommandHandler> logger)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.groupRepository = groupRepository;
            this.itemRepository = itemRepository;
            this.groupFieldValueRepository = groupFieldValueRepository;
            this.itemFieldValueRepository = itemFieldValueRepository;
            this.templateRepository = templateRepository;
            this.currentUser = currentUser;
            this.fieldValueValidator = fieldValueValidator;
            this.logger = logger;
        }

        public async Task<Guid> Handle(
            CreateCostEstimateFromAIPreviewCommand request,
            CancellationToken cancellationToken)
        {
            CostEstimateTemplate template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.Preview.TemplateId
                  && !t.IsDeleted
                  && t.OwnerId == currentUser.Id,
                cancellationToken,
                q => q.Include(t => t.GroupFieldDefinitions),
                q => q.Include(t => t.SystemFieldDefinitions),
                q => q.Include(t => t.CalculatedFieldDefinitions),
                q => q.Include(t => t.GenericFieldDefinitions))
                ?? throw new NotFoundApiException(
                    nameof(CostEstimateTemplate),
                    request.Preview.TemplateId.ToString());

            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allFieldDefs =
                BuildFieldDefDictionary(template);

            DateTime now = DateTime.UtcNow;

            // 1. Utwórz kosztorys
            CostEstimate costEstimate = new CostEstimate
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                TemplateId = request.Preview.TemplateId,
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

            await costEstimateRepository.Insert(costEstimate);

            // 2. Mapowanie tempId → Guid dla grup (potrzebne do relacji parent/child)
            Dictionary<string, Guid> tempIdToGroupId = [];

            // 3. Utwórz grupy (najpierw root, potem podgrupy — sortuj by ParentTempId == null)
            IEnumerable<AIGroupPreviewWeb> orderedGroups = request.Preview.Groups
                .OrderBy(g => g.ParentTempId is null ? 0 : 1)
                .ThenBy(g => g.Order);

            foreach (AIGroupPreviewWeb groupPreview in orderedGroups)
            {
                Guid? parentGroupId = null;
                if (groupPreview.ParentTempId is not null &&
                    tempIdToGroupId.TryGetValue(groupPreview.ParentTempId, out Guid parentId))
                {
                    parentGroupId = parentId;
                }

                int level = parentGroupId.HasValue ? 1 : 0;

                CostEstimateGroup group = new CostEstimateGroup
                {
                    Id = Guid.NewGuid(),
                    CostEstimateId = costEstimate.Id,
                    Name = groupPreview.Name,
                    ParentGroupId = parentGroupId,
                    Level = level,
                    Order = groupPreview.Order,
                    CreatedAt = now,
                    IsDeleted = false
                };

                await groupRepository.Insert(group);
                tempIdToGroupId[groupPreview.TempId] = group.Id;

                // 4. Wartości pól grupy
                await InsertGroupFieldValues(
                    groupPreview.FieldValues, groupPreview.Name, group.Id, allFieldDefs, now);

                // 5. Utwórz pozycje w grupie
                foreach (AIItemPreviewWeb itemPreview in groupPreview.Items.OrderBy(i => i.Order))
                {
                    CostEstimateItem item = new CostEstimateItem
                    {
                        Id = Guid.NewGuid(),
                        CostEstimateId = costEstimate.Id,
                        GroupId = group.Id,
                        Name = itemPreview.Name,
                        Order = itemPreview.Order,
                        RelationType = ItemRelationType.None,
                        CreatedAt = now,
                        IsDeleted = false
                    };

                    await itemRepository.Insert(item);

                    bool hasComponents = itemPreview.Components.Count > 0;

                    if (hasComponents)
                    {
                        // 6a. Pozycja z komponentami — wartości pól trafiają na komponenty, nie na pozycję główną
                        foreach (AIComponentPreviewWeb compPreview in itemPreview.Components.OrderBy(c => c.Order))
                        {
                            CostEstimateItem component = new CostEstimateItem
                            {
                                Id = Guid.NewGuid(),
                                CostEstimateId = costEstimate.Id,
                                GroupId = group.Id,
                                Name = compPreview.Name,
                                ParentItemId = item.Id,
                                Order = compPreview.Order,
                                RelationType = ItemRelationType.Component,
                                CreatedAt = now,
                                IsDeleted = false
                            };

                            await itemRepository.Insert(component);

                            await InsertItemFieldValues(
                                compPreview.FieldValues, compPreview.Name, component.Id, allFieldDefs, now);
                        }
                    }

                    await InsertItemFieldValues(itemPreview.FieldValues, itemPreview.Name, item.Id, allFieldDefs, now);
                }
            }

            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            return costEstimate.Id;
        }

        private async Task InsertGroupFieldValues(
            List<AIFieldValueWeb> fieldValues,
            string groupName,
            Guid groupId,
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allFieldDefs,
            DateTime now)
        {
            HashSet<Guid> providedIds = fieldValues.Select(f => f.FieldDefinitionId).ToHashSet();

            // Auto-uzupełnij GroupName jeśli AI go nie dostarczyło
            CostEstimateTemplateFieldDefinitionBase? nameDef = allFieldDefs.Values
                .FirstOrDefault(d => d.FieldType == FieldType.GroupName && d.ParentFieldId == null && !providedIds.Contains(d.Id));
            if (nameDef is not null && !string.IsNullOrWhiteSpace(groupName))
            {
                await groupFieldValueRepository.Insert(new CostEstimateGroupFieldValue
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    FieldDefinitionId = nameDef.Id,
                    StringValue = groupName,
                    CreatedAt = now
                });
            }

            foreach (AIFieldValueWeb fv in fieldValues)
            {
                if (!allFieldDefs.TryGetValue(fv.FieldDefinitionId, out CostEstimateTemplateFieldDefinitionBase? fieldDef))
                {
                    logger.LogWarning("FieldDefinitionId {Id} not found in template — skipping", fv.FieldDefinitionId);
                    continue;
                }

                if (!IsValidForInsert(fv, fieldDef))
                    continue;

                CostEstimateGroupFieldValue fieldValue = new CostEstimateGroupFieldValue
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    FieldDefinitionId = fv.FieldDefinitionId,
                    StringValue = fv.StringValue,
                    DecimalValue = fv.DecimalValue,
                    BoolValue = fv.BoolValue,
                    DateTimeValue = fv.DateTimeValue,
                    CreatedAt = now
                };

                await groupFieldValueRepository.Insert(fieldValue);
            }
        }

        private async Task InsertItemFieldValues(
            List<AIFieldValueWeb> fieldValues,
            string itemName,
            Guid itemId,
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allFieldDefs,
            DateTime now)
        {
            HashSet<Guid> providedIds = fieldValues.Select(f => f.FieldDefinitionId).ToHashSet();

            // Auto-uzupełnij ItemSystemName jeśli AI go nie dostarczyło
            CostEstimateTemplateFieldDefinitionBase? nameDef = allFieldDefs.Values
                .FirstOrDefault(d => d.FieldType == FieldType.ItemSystemName && d.ParentFieldId == null && !providedIds.Contains(d.Id));
            if (nameDef is not null && !string.IsNullOrWhiteSpace(itemName))
            {
                await itemFieldValueRepository.Insert(new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    ItemId = itemId,
                    FieldDefinitionId = nameDef.Id,
                    StringValue = itemName,
                    CreatedAt = now
                });
            }

            // Auto-ustaw Zaznaczenie (ItemSystemSelected) na true
            CostEstimateTemplateFieldDefinitionBase? selectedDef = allFieldDefs.Values
                .FirstOrDefault(d => d.FieldType == FieldType.ItemSystemSelected && d.ParentFieldId == null && !providedIds.Contains(d.Id));
            if (selectedDef is not null)
            {
                await itemFieldValueRepository.Insert(new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    ItemId = itemId,
                    FieldDefinitionId = selectedDef.Id,
                    BoolValue = true,
                    CreatedAt = now
                });
            }

            // Auto-ustaw Zakres pracy (ItemSystemIsWorkScope) na true
            CostEstimateTemplateFieldDefinitionBase? workScopeDef = allFieldDefs.Values
                .FirstOrDefault(d => d.FieldType == FieldType.ItemSystemIsWorkScope && d.ParentFieldId == null && !providedIds.Contains(d.Id));
            if (workScopeDef is not null)
            {
                await itemFieldValueRepository.Insert(new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    ItemId = itemId,
                    FieldDefinitionId = workScopeDef.Id,
                    BoolValue = true,
                    CreatedAt = now
                });
            }

            foreach (AIFieldValueWeb fv in fieldValues)
            {
                if (!allFieldDefs.TryGetValue(fv.FieldDefinitionId, out CostEstimateTemplateFieldDefinitionBase? fieldDef))
                {
                    logger.LogWarning("FieldDefinitionId {Id} not found in template — skipping", fv.FieldDefinitionId);
                    continue;
                }

                if (!IsValidForInsert(fv, fieldDef))
                    continue;

                CostEstimateItemFieldValue fieldValue = new CostEstimateItemFieldValue
                {
                    Id = Guid.NewGuid(),
                    ItemId = itemId,
                    FieldDefinitionId = fv.FieldDefinitionId,
                    StringValue = fv.StringValue,
                    DecimalValue = fv.DecimalValue,
                    BoolValue = fv.BoolValue,
                    DateTimeValue = fv.DateTimeValue,
                    CreatedAt = now
                };

                await itemFieldValueRepository.Insert(fieldValue);
            }
        }

        private bool IsValidForInsert(
            AIFieldValueWeb fv,
            CostEstimateTemplateFieldDefinitionBase fieldDef)
        {
            CostEstimateFieldValueContext ctx = CostEstimateFieldValueContext.From(
                fieldDef,
                fv.StringValue,
                fv.DecimalValue,
                fv.BoolValue,
                fv.DateTimeValue);

            // Pomiń pola kolekcji i pliki
            if (ctx.FieldTypeConfig.IsCollection || ctx.FieldTypeConfig.IsFile)
                return false;

            ValidationResult result = fieldValueValidator.Validate(ctx);
            if (!result.IsValid)
            {
                logger.LogWarning(
                    "Field '{Label}' [{Type}] failed validation during AI import — skipping. Errors: {Errors}",
                    fieldDef.Label, fieldDef.FieldType,
                    string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
                return false;
            }

            return true;
        }

        private static Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> BuildFieldDefDictionary(
            CostEstimateTemplate template)
        {
            return template.GroupFieldDefinitions
                .Cast<CostEstimateTemplateFieldDefinitionBase>()
                .Concat(template.SystemFieldDefinitions)
                .Concat(template.CalculatedFieldDefinitions)
                .Concat(template.GenericFieldDefinitions)
                .ToDictionary(f => f.Id);
        }
    }
}
