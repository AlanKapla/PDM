# API-04: GetCostEstimateDetailsQueryHandler — filtrowanie IsVisible na obu listach

## Cel
Dostosowanie handlera `GetCostEstimateDetailsQueryHandler` do nowej struktury `UiConfigurationWeb` z osobnymi listami `GroupColumns` i `ItemColumns`.

## Plik do zmiany

### `CQRS/CostEstimates/GetCostEstimateDetails/GetCostEstimateDetailsQueryHandler.cs`

**OBECNIE (linie 122-131):**
```csharp
if (accessLevel is CostEstimateAccessLevel.Restricted or CostEstimateAccessLevel.ReadOnly && templateStructure.UiConfiguration is not null)
{
    var visibleColumns = templateStructure.UiConfiguration.Columns
        .Where(c => c.IsVisible)
        .ToList();

    templateStructure = templateStructure with
    {
        UiConfiguration = new UiConfigurationWeb(visibleColumns)
    };
}
```

**PO ZMIANIE:**
```csharp
if (accessLevel is CostEstimateAccessLevel.Restricted or CostEstimateAccessLevel.ReadOnly && templateStructure.UiConfiguration is not null)
{
    var visibleGroupColumns = templateStructure.UiConfiguration.GroupColumns
        .Where(c => c.IsVisible)
        .ToList();
    var visibleItemColumns = templateStructure.UiConfiguration.ItemColumns
        .Where(c => c.IsVisible)
        .ToList();

    templateStructure = templateStructure with
    {
        UiConfiguration = new UiConfigurationWeb(visibleGroupColumns, visibleItemColumns)
    };
}
```

## Zależności
- Wymaga API-01 (nowa struktura `UiConfigurationWeb`)
- Requires API-02 (nowe listy z `BuildTemplateStructureAsync`)
