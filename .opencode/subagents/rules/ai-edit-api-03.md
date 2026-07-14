# Prompt: ai-edit-api-03 — CQRS: GenerateCostEstimateAIEditCommand + ApplyCostEstimateAIEditCommand

## Cel

Stworzyć dwie nowe komendy CQRS:
1. `GenerateCostEstimateAIEditCommand` — generuje propozycję edycji przez AI (nie zapisuje)
2. `ApplyCostEstimateAIEditCommand` — aplikuje zatwierdzone zmiany do DB

## Pliki do utworzenia

### 1. `CQRS/CostEstimates/GenerateCostEstimateAIEdit/GenerateCostEstimateAIEditCommand.cs`

```csharp
using Business.Interfaces.WebModels.AI;

namespace CQRS.CostEstimates.GenerateCostEstimateAIEdit;

public sealed record GenerateCostEstimateAIEditCommand
    : CostEstimateCommandBase, IRequestCommand<AICostEditPreviewWeb>
{
    public string UserRequest { get; init; } = string.Empty;
    public override string PermissionCode => PermissionCodes.ProjectEstimates;
}
```

### 2. `CQRS/CostEstimates/GenerateCostEstimateAIEdit/GenerateCostEstimateAIEditCommandHandler.cs`

Wzoruj się na `GenerateCostEstimateAIPreviewCommandHandler.cs`.

**Logika handlera:**
1. Load cost estimate z cache (`ceCacheService.GetCostEstimateAsync`)
2. Check access — tylko Full może generować preview (`ceAccessService.GetAccessLevelAsync` → must be Full)
3. Load template z cache (`ceCacheService.GetTemplateAsync`)
4. Load wszystkie kolekcje z cache:
   - `GetGroupsDictionaryAsync`
   - `GetGroupFieldValuesDictionaryAsync`
   - `GetItemsDictionaryAsync`
   - `GetItemFieldValuesDictionaryAsync`
5. Wywołaj `aiEditService.GenerateEditPreviewAsync(costEstimate, template, groupsDict, groupFieldValuesDict, itemsDict, itemFieldValuesDict, request.UserRequest, ct)`
6. Zwróć preview

### 3. `CQRS/CostEstimates/GenerateCostEstimateAIEdit/GenerateCostEstimateAIEditCommandValidator.cs`

```csharp
namespace CQRS.CostEstimates.GenerateCostEstimateAIEdit;

public sealed class GenerateCostEstimateAIEditCommandValidator 
    : AbstractValidator<GenerateCostEstimateAIEditCommand>
{
    public GenerateCostEstimateAIEditCommandValidator()
    {
        RuleFor(x => x.UserRequest)
            .NotEmpty()
            .MaximumLength(2000)
            .WithMessage("Opis zmian jest wymagany (max 2000 znaków).");
    }
}
```

### 4. `CQRS/CostEstimates/ApplyCostEstimateAIEdit/ApplyCostEstimateAIEditCommand.cs`

```csharp
using Business.Interfaces.WebModels.AI;

namespace CQRS.CostEstimates.ApplyCostEstimateAIEdit;

public sealed record ApplyCostEstimateAIEditCommand
    : CostEstimateCommandBase, IRequestCommand<Unit>
{
    public AICostEditPreviewWeb Preview { get; init; } = default!;
    public override string PermissionCode => PermissionCodes.ProjectEstimates;
}
```

### 5. `CQRS/CostEstimates/ApplyCostEstimateAIEdit/ApplyCostEstimateAIEditCommandHandler.cs`

**WZÓR: `CreateCostEstimateFromAIPreviewCommandHandler.cs`** — to jest DOKŁADNY wzorzec.
Apply będzie działał BEZPOŚREDNIO przez repozytoria (nie przez istniejące CQRS commandy), w jednej transakcji.

**Logika handlera:**

```csharp
public sealed class ApplyCostEstimateAIEditCommandHandler 
    : IRequestHandler<ApplyCostEstimateAIEditCommand, Unit>
{
    // DI: repositories, cache service, field validator, current user, logger

    public async Task<Unit> Handle(ApplyCostEstimateAIEditCommand request, CancellationToken ct)
    {
        // 1. Load cost estimate z cache
        // 2. Check access — EnsureCanModifyStructure() (wymaga Full)
        // 3. Load template z cache + wszystkie field definitions
        // 4. Load aktualny stan kolekcji (groups, items, field values)
        // 5. Zbuduj słownik field definitions (BuildFieldDefDictionary)
        
        // 6. Ustal: które grupy są NOWE (guid == Empty), które DO USUNIĘCIA (istnieją ale nie ma w preview)
        HashSet<Guid> previewGroupIds = request.Preview.Groups
            .Where(g => g.Id != Guid.Empty)
            .Select(g => g.Id)
            .ToHashSet();
        
        // 7. DELETE groups które są w DB ale nie w preview (soft-delete + kaskadowo)
        foreach (CostEstimateGroup existingGroup in allExistingGroups.Values)
        {
            if (!previewGroupIds.Contains(existingGroup.Id))
            {
                // Delete group + children + items + field values (jak DeleteCostEstimateGroupCommandHandler)
                await SoftDeleteGroupAsync(existingGroup, ct);
            }
        }
        
        // 8. UPSERT groups: dla każdej grupy z preview
        foreach (AIGroupPreviewWeb groupPreview in request.Preview.Groups.OrderBy(g => g.Order))
        {
            if (groupPreview.Id == Guid.Empty)
            {
                // NOWA grupa → insert z nowym Guid
                var newGroup = new CostEstimateGroup { ... };
                await groupRepository.Insert(newGroup, ct);
                // Insert group field values
            }
            else
            {
                // ISTNIEJĄCA grupa → update name/order jeśli się zmieniły
                CostEstimateGroup existing = allExistingGroups[groupPreview.Id];
                if (existing.Name != groupPreview.Name || existing.Order != groupPreview.Order)
                {
                    existing.Name = groupPreview.Name;
                    existing.Order = groupPreview.Order;
                }
                // Upsert group field values
            }
            
            // 9. Dla każdej pozycji w grupie — analogicznie: delete/insert/update
        }
        
        // 10. Update name/description jeśli suggestedName nie jest null
        if (request.Preview.SuggestedName is not null)
            costEstimate.Name = request.Preview.SuggestedName;
        if (request.Preview.SuggestedDescription is not null)
            costEstimate.Description = request.Preview.SuggestedDescription;
        
        // 11. Recaluclate totals (przez CostEstimateCalculationService)
        await calculationService.RecalculateAsync(costEstimate.Id, ct);
        
        // 12. SaveChangesAsync
        await costEstimateRepository.SaveChangesAsync(ct);
        
        // 13. Invalidate ALL cache
        await ceCacheService.InvalidateCostEstimateAsync(tenantId, projectId, costEstimate.Id, ct);
        
        return Unit.Value;
    }
}
```

**Kluczowe implementacje pomocnicze:**

- `SoftDeleteGroupAsync` — soft-delete grupy + wszystkie child grupy + itemy + field values + pliki (wzoruj się na `DeleteCostEstimateGroupCommandHandler`)
- `UpsertItemFieldValuesAsync` — dla każdego field value w preview: jeśli już istnieje dla danego item+fieldDef → update wartości; jeśli nie → insert. Wzór na podstawie `UpsertCostEstimateItemFieldCommandHandler` ale uproszczony (bez notyfikacji).
- `UpsertGroupFieldValuesAsync` — analogicznie jak item field values ale dla grup.

**Ważne:** Użyj `CostEstimateFieldValueValidator` do walidacji field values (jak w `CreateCostEstimateFromAIPreviewCommandHandler.IsValidForInsert`).

### 6. `CQRS/CostEstimates/ApplyCostEstimateAIEdit/ApplyCostEstimateAIEditCommandValidator.cs`

```csharp
namespace CQRS.CostEstimates.ApplyCostEstimateAIEdit;

public sealed class ApplyCostEstimateAIEditCommandValidator 
    : AbstractValidator<ApplyCostEstimateAIEditCommand>
{
    public ApplyCostEstimateAIEditCommandValidator()
    {
        RuleFor(x => x.Preview)
            .NotNull()
            .WithMessage("Preview edycji jest wymagane.");
            
        RuleFor(x => x.Preview.Groups)
            .NotEmpty()
            .WithMessage("Kosztorys musi zawierać co najmniej jedną grupę.");
    }
}
```

## Weryfikacja

1. Wszystkie 6 plików istnieje w odpowiednich lokalizacjach CQRS
2. Build API przechodzi
3. Handlery mają poprawne DI (wszystkie zależności zarejestrowane)
