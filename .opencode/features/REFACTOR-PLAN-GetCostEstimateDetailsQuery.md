# Plan Refaktoru GetCostEstimateDetailsQuery

**Status:** Do implementacji  
**Data:** 2026-06-11

---

## Cel refaktoru

Zmiana `GetCostEstimateDetailsQuery` aby zamiast używać `CostEstimateTemplate` używał `CostEstimateFieldSchema`.

---

## Zmiany w GetCostEstimateDetailsQueryHandler.cs

### 1. Usunąć zależności (constructor)

```diff
- private readonly ICostEstimateTemplateService costEstimateTemplateService;
```

### 2. Zmienić Handle() — linie 66-210

#### Obecny kod (linie 78-82):
```csharp
// 3. Get template from cache (for name + structure + currencies)
var template = await ceCacheService.GetTemplateAsync(costEstimate.TemplateId, cancellationToken)
    ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), costEstimate.TemplateId.ToString());
```

#### Nowy kod:
```csharp
// 3. Get schema from cache (brak templateId — używamy CostEstimateId)
// Schema jest inline w CostEstimate (relacja 1:1)
if (costEstimate.Schema is null)
{
    throw new ConflictApiException("Cost estimate does not have a schema");
}
```

**UWAGA:** `ceCacheService` również trzeba zaktualizować aby cachować `Schema` zamiast `Template`.

---

#### Obecny kod (linie 118-136):
```csharp
// 7. Get template structure via existing service
var templateStructure = await costEstimateTemplateService.GetTemplateStructureCachedAsync(
    template, cancellationToken);

// Restricted access widzi tylko kolumny z IsVisible = true.
if (accessLevel is CostEstimateAccessLevel.Restricted or CostEstimateAccessLevel.ReadOnly && templateStructure.UiConfiguration is not null)
{
    List<ColumnConfigurationWeb> visibleGroupColumns = templateStructure.UiConfiguration.GroupColumns
        .Where(c => c.IsVisible)
        .ToList();
    List<ColumnConfigurationWeb> visibleItemColumns = templateStructure.UiConfiguration.ItemColumns
        .Where(c => c.IsVisible)
        .ToList();

    templateStructure = templateStructure with
    {
        UiConfiguration = new UiConfigurationWeb(visibleGroupColumns, visibleItemColumns)
    };
}
```

#### Nowy kod:
```csharp
// 7. Build schema structure from CostEstimateFieldSchema
var schemaWeb = BuildSchemaWeb(costEstimate.Schema);

// Restricted access widzi tylko pola z IsVisible = true
if (accessLevel is CostEstimateAccessLevel.Restricted or CostEstimateAccessLevel.ReadOnly)
{
    schemaWeb = schemaWeb with
    {
        FieldDefinitions = schemaWeb.FieldDefinitions
            .Where(f => f.IsVisible)
            .ToList()
    };
}
```

**NOWA metoda `BuildSchemaWeb`:**
```csharp
private CostEstimateSchemaWeb BuildSchemaWeb(CostEstimateFieldSchema schema)
{
    return new CostEstimateSchemaWeb(
        Id: schema.Id,
        CostEstimateId: schema.CostEstimateId,
        FieldDefinitions: schema.FieldDefinitions
            .OrderBy(f => f.Order)
            .Select(f => new CostEstimateFieldDefinitionWeb(
                Id: f.Id,
                FieldName: f.FieldName,
                FieldScope: (int)f.FieldScope,
                FieldType: (int)f.FieldType,
                Label: f.Label,
                IsSortable: f.IsSortable,
                IsFilterable: f.IsFilterable,
                IsVisible: f.IsVisible,
                IsReadonly: f.IsReadonly,
                ParentFieldId: f.ParentFieldId,
                Order: f.Order,
                IsUserDefined: f.IsUserDefined,
                CanRename: f.CanRename,
                CanDelete: f.CanDelete,
                ChildFields: f.ChildFields
                    .OrderBy(cf => cf.Order)
                    .Select(cf => new CostEstimateFieldDefinitionWeb(
                        Id: cf.Id,
                        FieldName: cf.FieldName,
                        FieldScope: (int)cf.FieldScope,
                        FieldType: (int)cf.FieldType,
                        Label: cf.Label,
                        IsSortable: cf.IsSortable,
                        IsFilterable: cf.IsFilterable,
                        IsVisible: cf.IsVisible,
                        IsReadonly: cf.IsReadonly,
                        ParentFieldId: cf.ParentFieldId,
                        Order: cf.Order,
                        IsUserDefined: cf.IsUserDefined,
                        CanRename: cf.CanRename,
                        CanDelete: cf.CanDelete,
                        ChildFields: null  // Max 1 poziom zagnieżdżenia
                    ))
                    .ToList()
            ))
            .ToList(),
        CreatedAt: schema.CreatedAt,
        UpdatedAt: schema.UpdatedAt
    );
}
```

---

#### Obecny kod (linie 186-210):
```csharp
return new CostEstimateDetailsWeb(
    Id: costEstimate.Id,
    TenantId: costEstimate.TenantId,
    ProjectId: costEstimate.ProjectId,
    TemplateId: costEstimate.TemplateId,           // ← USUNĄĆ
    TemplateName: template.Name,                    // ← USUNĄĆ
    SelectedCurrencyCode: projectCurrency?.Code,
    SelectedCurrencySymbol: projectCurrency?.Symbol,
    Name: costEstimate.Name,
    Description: costEstimate.Description,
    Status: costEstimate.Status,
    WorkScheduleId: workScheduleId,
    RootGroups: rootGroups,
    TotalNet: costEstimate.TotalNet,
    TotalGross: costEstimate.TotalGross,
    TotalVat: costEstimate.TotalVat,
    CreatedAt: costEstimate.CreatedAt,
    UpdatedAt: costEstimate.UpdatedAt,
    LastCalculatedAt: costEstimate.LastCalculatedAt,
    OwnerId: costEstimate.OwnerId,
    OwnerName: $"{costEstimate.Owner.FirstName} {costEstimate.Owner.LastName}",
    TemplateStructure: templateStructure,           // ← ZMIENIĆ na Schema
    AccessLevel: accessLevel,
    SharedWithUsers: sharedWithUsers
);
```

#### Nowy kod:
```csharp
return new CostEstimateDetailsWeb(
    Id: costEstimate.Id,
    TenantId: costEstimate.TenantId,
    ProjectId: costEstimate.ProjectId,
    // TemplateId, TemplateName — USUNIĘTE
    SelectedCurrencyCode: projectCurrency?.Code,
    SelectedCurrencySymbol: projectCurrency?.Symbol,
    Name: costEstimate.Name,
    Description: costEstimate.Description,
    Status: costEstimate.Status,
    WorkScheduleId: workScheduleId,
    RootGroups: rootGroups,
    TotalNet: costEstimate.TotalNet,
    TotalGross: costEstimate.TotalGross,
    TotalVat: costEstimate.TotalVat,
    CreatedAt: costEstimate.CreatedAt,
    UpdatedAt: costEstimate.UpdatedAt,
    LastCalculatedAt: costEstimate.LastCalculatedAt,
    OwnerId: costEstimate.OwnerId,
    OwnerName: $"{costEstimate.Owner.FirstName} {costEstimate.Owner.LastName}",
    Schema: schemaWeb,                              // ← NOWE
    AccessLevel: accessLevel,
    SharedWithUsers: sharedWithUsers
);
```

---

## Zmiany w CostEstimateDetailsWeb (WebModel)

**Plik:** `src/Business/Interfaces/WebModels/CostEstimates/CostEstimateDetailsWeb.cs` (lub podobny)

### Obecny model:
```csharp
public record CostEstimateDetailsWeb(
    Guid Id,
    Guid TenantId,
    Guid ProjectId,
    Guid TemplateId,                              // ← USUNĄĆ
    string TemplateName,                           // ← USUNĄĆ
    string? SelectedCurrencyCode,
    string? SelectedCurrencySymbol,
    string Name,
    string? Description,
    CostEstimateStatus Status,
    Guid? WorkScheduleId,
    List<CostEstimateGroupWeb> RootGroups,
    decimal? TotalNet,
    decimal? TotalGross,
    decimal? TotalVat,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? LastCalculatedAt,
    Guid OwnerId,
    string OwnerName,
    CostEstimateTemplateStructureWeb TemplateStructure,  // ← ZMIENIĆ
    CostEstimateAccessLevel AccessLevel,
    IReadOnlyList<CostEstimateShareWeb> SharedWithUsers
);
```

### Nowy model:
```csharp
public record CostEstimateDetailsWeb(
    Guid Id,
    Guid TenantId,
    Guid ProjectId,
    // TemplateId, TemplateName — USUNIĘTE
    string? SelectedCurrencyCode,
    string? SelectedCurrencySymbol,
    string Name,
    string? Description,
    CostEstimateStatus Status,
    Guid? WorkScheduleId,
    List<CostEstimateGroupWeb> RootGroups,
    decimal? TotalNet,
    decimal? TotalGross,
    decimal? TotalVat,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? LastCalculatedAt,
    Guid OwnerId,
    string OwnerName,
    CostEstimateSchemaWeb Schema,                   // ← NOWE
    CostEstimateAccessLevel AccessLevel,
    IReadOnlyList<CostEstimateShareWeb> SharedWithUsers
);
```

---

## Nowe WebModels (do stworzenia)

### 1. CostEstimateSchemaWeb
**Plik:** `src/Business/Interfaces/WebModels/CostEstimates/CostEstimateSchemaWeb.cs`

```csharp
namespace Business.Interfaces.WebModels.CostEstimates
{
    public record CostEstimateSchemaWeb(
        Guid Id,
        Guid CostEstimateId,
        List<CostEstimateFieldDefinitionWeb> FieldDefinitions,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}
```

### 2. CostEstimateFieldDefinitionWeb
**Plik:** `src/Business/Interfaces/WebModels/CostEstimates/CostEstimateFieldDefinitionWeb.cs`

```csharp
namespace Business.Interfaces.WebModels.CostEstimates
{
    public record CostEstimateFieldDefinitionWeb(
        Guid Id,
        Guid FieldName,
        int FieldScope,      // enum as int
        int FieldType,       // enum as int
        string Label,
        bool IsSortable,
        bool IsFilterable,
        bool IsVisible,
        bool IsReadonly,
        Guid? ParentFieldId,
        int Order,
        bool IsUserDefined,
        bool CanRename,
        bool CanDelete,
        List<CostEstimateFieldDefinitionWeb>? ChildFields  // max 1 poziom
    );
}
```

---

## Zmiany w ICostEstimateCacheService

**Problem:** Obecny cache service cachuje Template po `TemplateId`.  
**Rozwiązanie:** Zmienić aby cachować `Schema` inline w `CostEstimate` (relacja 1:1).

### Obecne metody do USUNIĘCIA:
```csharp
Task<CostEstimateTemplate?> GetTemplateAsync(Guid templateId, CancellationToken cancellationToken);
```

### Nowe metody (jeśli potrzebne):
```csharp
// Schema jest inline w CostEstimate — nie potrzeba osobnej metody
// Wystarczy Include w GetCostEstimateAsync:
// .Include(c => c.Schema)
//     .ThenInclude(s => s.FieldDefinitions)
//         .ThenInclude(f => f.ChildFields)
```

---

## Usunięcie zależności od ICostEstimateTemplateService

**Ten serwis jest używany TYLKO do szablonów globalnych — można go całkowicie usunąć z projektu po migracji.**

Dla tego refaktoru:
- Usunąć inject w konstruktorze Handlera
- Usunąć `using Business.Interfaces.WebModels.CostEstimateTemplates;`
- Usunąć `using Entities.Models.CostEstimateTemplates;`

---

## Testy (do zaktualizowania później)

**Pliki:**
- `tests/CQRS.Tests/CostEstimates/GetCostEstimateDetailsQueryHandlerTests.cs`

**Zmiany:**
- Mock `CostEstimate.Schema` zamiast `TemplateId`
- Usunąć mock `ICostEstimateTemplateService`
- Zaktualizować asercje (sprawdzać `Schema` zamiast `TemplateStructure`)

---

## Frontend (do zaktualizowania później)

**Pliki:**
- `01-Applications/ProjectDataManagementUI/src/types/costEstimate.types.new.ts`
- `01-Applications/ProjectDataManagementUI/src/hooks/queries/useCostEstimate.ts`
- `01-Applications/ProjectDataManagementUI/src/pages/CostEstimateEditPage.tsx`

**Zmiany:**
```diff
interface CostEstimateDetailsWeb {
-  templateId: string;
-  templateName: string;
-  templateStructure: CostEstimateTemplateStructureWeb;
+  schema: CostEstimateSchemaWeb;
}

interface CostEstimateSchemaWeb {
  id: string;
  costEstimateId: string;
  fieldDefinitions: CostEstimateFieldDefinitionWeb[];
  createdAt: string;
  updatedAt?: string;
}

interface CostEstimateFieldDefinitionWeb {
  id: string;
  fieldName: string;  // Guid
  fieldScope: number;
  fieldType: number;
  label: string;
  isSortable: boolean;
  isFilterable: boolean;
  isVisible: boolean;
  isReadonly: boolean;
  parentFieldId?: string;
  order: number;
  isUserDefined: boolean;
  canRename: boolean;
  canDelete: boolean;
  childFields?: CostEstimateFieldDefinitionWeb[];
}
```

---

## Podsumowanie kroków

1. ✅ Stworzyć nowe WebModels (`CostEstimateSchemaWeb`, `CostEstimateFieldDefinitionWeb`)
2. ✅ Zaktualizować `CostEstimateDetailsWeb` (usunąć `TemplateId`, `TemplateName`, `TemplateStructure`, dodać `Schema`)
3. ✅ Zaktualizować `GetCostEstimateDetailsQueryHandler.cs`:
   - Usunąć `ICostEstimateTemplateService` z konstruktora
   - Usunąć `GetTemplateAsync`
   - Dodać `BuildSchemaWeb`
   - Zmienić mapowanie w `Handle()`
4. ✅ Zaktualizować `ICostEstimateCacheService` (Include Schema w GetCostEstimateAsync)
5. ⏳ Zaktualizować testy (później)
6. ⏳ Zaktualizować frontend (później)

---

**Koniec planu refaktoru GetCostEstimateDetailsQuery**
