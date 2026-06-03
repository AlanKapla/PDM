# Prompt API-04: CQRS — CreateCostEstimateFromAIPreviewCommand

## Cel
Utwórz Command + Handler + Validator dla zapisu kosztorysu zatwierdzonego przez użytkownika z podglądu AI.
**Atomowo tworzy: kosztorys → grupy → pozycje → wartości pól.**

---

## Lokalizacja plików

```
src/CQRS/CostEstimates/CreateCostEstimateFromAIPreview/
  CreateCostEstimateFromAIPreviewCommand.cs
  CreateCostEstimateFromAIPreviewCommandHandler.cs
  CreateCostEstimateFromAIPreviewCommandValidator.cs
```

---

## Plik 1: Command

### `CreateCostEstimateFromAIPreviewCommand.cs`

```csharp
using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.AI;

namespace CQRS.CostEstimates.CreateCostEstimateFromAIPreview
{
    /// <summary>
    /// Zapisuje kosztorys zatwierdzony przez użytkownika z podglądu AI.
    /// Atomowo tworzy: CostEstimate → Groups → Items → FieldValues.
    /// Zwraca ID nowo utworzonego kosztorysu.
    /// </summary>
    public sealed record CreateCostEstimateFromAIPreviewCommand : CostEstimateRequestBase, IRequestCommand<Guid>
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public AICostEstimatePreviewWeb Preview { get; init; } = default!;

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
```

---

## Plik 2: Handler

### `CreateCostEstimateFromAIPreviewCommandHandler.cs`

Handler tworzy strukturę kosztorysu bezpośrednio przez repozytoria (nie wywołuje innych Command przez mediator).

```csharp
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.AI;
using CQRS.CostEstimates.Validators;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using FluentValidation.Results;
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
                  && t.OwnerId == currentUser.Id)
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
                // Dla głębszego zagnieżdżenia: można rozszerzyć jeśli potrzeba

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
                    groupPreview.FieldValues, group.Id, allFieldDefs, now, cancellationToken);

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

                    // 6. Wartości pól pozycji
                    await InsertItemFieldValues(
                        itemPreview.FieldValues, item.Id, allFieldDefs, now, cancellationToken);
                }
            }

            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            return costEstimate.Id;
        }

        private async Task InsertGroupFieldValues(
            List<AIFieldValueWeb> fieldValues,
            Guid groupId,
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allFieldDefs,
            DateTime now,
            CancellationToken cancellationToken)
        {
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
            Guid itemId,
            Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> allFieldDefs,
            DateTime now,
            CancellationToken cancellationToken)
        {
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
            CostEstimateFieldTypeConfig typeConfig = FieldTypeConfigRegistry.Get(fieldDef.FieldType);

            // Pomiń pola kolekcji i pliki
            if (typeConfig.IsCollection || typeConfig.IsFile)
                return false;

            CostEstimateFieldValueContext ctx = new CostEstimateFieldValueContext(
                FieldType: fieldDef.FieldType,
                FieldLabel: fieldDef.Label,
                FieldTypeConfig: typeConfig,
                StringValue: fv.StringValue,
                DecimalValue: fv.DecimalValue,
                BoolValue: fv.BoolValue,
                DateTimeValue: fv.DateTimeValue);

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
```

---

## Plik 3: Validator

### `CreateCostEstimateFromAIPreviewCommandValidator.cs`

```csharp
using FluentValidation;

namespace CQRS.CostEstimates.CreateCostEstimateFromAIPreview
{
    public sealed class CreateCostEstimateFromAIPreviewCommandValidator
        : AbstractValidator<CreateCostEstimateFromAIPreviewCommand>
    {
        public CreateCostEstimateFromAIPreviewCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Nazwa kosztorysu jest wymagana.")
                .MaximumLength(200)
                .WithMessage("Nazwa kosztorysu nie może przekraczać 200 znaków.");

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .When(x => x.Description is not null)
                .WithMessage("Opis nie może przekraczać 2000 znaków.");

            RuleFor(x => x.Preview)
                .NotNull()
                .WithMessage("Podgląd kosztorysu jest wymagany.");

            RuleFor(x => x.Preview.TemplateId)
                .NotEmpty()
                .When(x => x.Preview is not null)
                .WithMessage("ID szablonu w podglądzie jest wymagany.");

            RuleFor(x => x.Preview.Groups)
                .NotEmpty()
                .When(x => x.Preview is not null)
                .WithMessage("Kosztorys musi zawierać co najmniej jedną grupę.");
        }
    }
}
```

---

## Uwagi implementacyjne

1. **`FieldTypeConfigRegistry`** — sprawdź jak jest zaimplementowany w projekcie (może to być klasa statyczna lub serwis). Jeśli nie istnieje, sprawdź jak `CostEstimateFieldValueContext` jest tworzony w `UpsertCostEstimateItemFieldCommandHandler` i zastosuj ten sam wzorzec.

2. **`template.GenericFieldDefinitions`** — sprawdź czy ta kolekcja istnieje w `CostEstimateTemplate`. Jeśli nie, pomiń ją w `BuildFieldDefDictionary`.

3. **`CostEstimateGroupFieldValue` / `CostEstimateItemFieldValue`** — sprawdź dokładne właściwości tych encji w `Entities.Models.CostEstimates`. Dostosuj mapowanie jeśli kolumny mają inne nazwy.

4. **`SaveChangesAsync`** — wywołaj raz na końcu (nie po każdym Insercie) dla wydajności. Upewnij się że repozytorium to wspiera (wzorzec Unit of Work).

5. Jeśli repozytorium wymaga `SaveChangesAsync` z konkretnego repozytorium — użyj `costEstimateRepository.SaveChangesAsync()`.

---

## Weryfikacja
```
dotnet build src/CQRS/CQRS.csproj
```
Oczekiwany wynik: Build succeeded, 0 errors.
