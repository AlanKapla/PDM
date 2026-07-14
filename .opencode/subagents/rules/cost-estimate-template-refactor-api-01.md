# API-01: DTO — podział UiConfiguration na groupColumns i itemColumns

## Cel
Zmiana DTO `UiConfigurationWeb` i `UiConfigurationDto` aby wspierały osobne listy kolumn dla pól grup (etapów) i pozycji.

## Pliki do zmiany

### 1. `Business/Interfaces/WebModels/CostEstimateTemplates/CostEstimateTemplateStructureWeb.cs`

**UiConfigurationWeb** — zmień z jednej listy `Columns` na dwie:
```csharp
// OBECNIE:
public record UiConfigurationWeb(List<ColumnConfigurationWeb> Columns);

// PO ZMIANIE:
public record UiConfigurationWeb(
    List<ColumnConfigurationWeb> GroupColumns,
    List<ColumnConfigurationWeb> ItemColumns
);
```

### 2. `Business/Interfaces/WebModels/CostEstimateTemplates/CostEstimateTemplateDtos.cs`

**UiConfigurationDto** — zmień z jednego `ColumnLayout` na dwa:
```csharp
// OBECNIE:
public record UiConfigurationDto(List<Guid>? ColumnLayout);

// PO ZMIANIE:
public record UiConfigurationDto(
    List<Guid>? GroupColumnLayout,
    List<Guid>? ItemColumnLayout
);
```

## Zależności
- Wszystkie miejsca używające `UiConfigurationWeb(columns)` muszą być dostosowane — ale to robią kolejne prompty (API-02, API-03, API-04)
- Wszystkie miejsca używające `UiConfigurationDto.ColumnLayout` muszą być dostosowane — ale to robią kolejne prompty

## Backward compatibility (decyzja usera)
- Gdy frontend wyśle stare `ColumnLayout` (a `GroupColumnLayout` i `ItemColumnLayout` są null), serwer sam rozdziela na grupy/item na podstawie `FieldScope`. Tę logikę dodać w `BuildColumnLayoutOrderMap` w API-02.
