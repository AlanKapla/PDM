# API Fix 07: Przebudowa CostEstimateCalculationService

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Przebudowa serwisu kalkulacji aby pracował na direct properties zamiast FieldValues.
Dodanie obsługi IsSelected dla pozycji, komponentów i propagacji opcji.

## Do zrobienia

### 1. Przebudowa `CostEstimateCalculationService.cs`

#### `RecalculateCostEstimate(CostEstimate costEstimate)`
- Bez zmian w strukturze — nadal sumuje grupy do kosztorysu

#### `RecalculateGroup(group, allGroups)`
Kluczowa zmiana:
```csharp
private static (decimal Net, decimal Gross, decimal Vat) RecalculateGroup(
    CostEstimateGroup group,
    List<CostEstimateGroup> allGroups)
{
    decimal groupNet = 0m;
    decimal groupGross = 0m;
    decimal groupVat = 0m;

    // Sumuj tylko pozycje główne (RelationType.None) z IsSelected == true
    var mainItems = group.Items
        .Where(i => !i.IsDeleted && i.RelationType == ItemRelationType.None && i.IsSelected)
        .ToList();
    
    foreach (var item in mainItems)
    {
        CalculateItemValues(item);

        if (item.NetValue.HasValue)
            groupNet += item.NetValue.Value;
        if (item.GrossValue.HasValue)
            groupGross += item.GrossValue.Value;
        if (item.VatValue.HasValue)
            groupVat += item.VatValue.Value;
    }

    // Rekurencyjnie podgrupy
    var childGroups = allGroups.Where(g => g.ParentGroupId == group.Id && !g.IsDeleted).ToList();
    foreach (var childGroup in childGroups)
    {
        var (childNet, childGross, childVat) = RecalculateGroup(childGroup, allGroups);
        groupNet += childNet;
        groupGross += childGross;
        groupVat += childVat;
    }

    group.TotalNet = groupNet;
    group.TotalGross = groupGross;
    group.TotalVat = groupVat;
    group.LastCalculatedAt = DateTime.UtcNow;
    group.UpdatedAt = DateTime.UtcNow;

    return (groupNet, groupGross, groupVat);
}
```

#### `CalculateItemValues(CostEstimateItem item)`

Kompletna przebudowa — bez FieldValues:

```csharp
private static void CalculateItemValues(CostEstimateItem item)
{
    if (item.IsDeleted) return;

    // === OPCJE ===
    // Sprawdź czy pozycja ma opcje (child items z RelationType.Option)
    var options = item.Options?.Where(o => !o.IsDeleted).ToList() ?? new List<CostEstimateItem>();
    if (options.Any())
    {
        // Pozycja z opcjami — wartości pochodzą z zaznaczonej opcji
        var selectedOption = options.FirstOrDefault(o => o.IsSelected);
        
        if (selectedOption != null)
        {
            item.NetValue = selectedOption.NetValue;
            item.GrossValue = selectedOption.GrossValue;
            item.VatValue = selectedOption.VatValue;
        }
        else
        {
            // Żadna opcja nie zaznaczona — wartości pozycji są puste
            item.NetValue = null;
            item.GrossValue = null;
            item.VatValue = null;
        }
        return;
    }

    // === KOMPONENTY ===
    var components = item.Components?.Where(c => !c.IsDeleted).ToList() ?? new List<CostEstimateItem>();
    if (components.Any())
    {
        // Sumuj tylko komponenty z IsSelected == true
        decimal? totalNet = null;
        decimal? totalGross = null;
        decimal? totalVat = null;
        
        foreach (var component in components.Where(c => c.IsSelected))
        {
            CalculateItemValues(component); // Rekurencyjnie — komponent też może mieć komponenty? Nie, ale bezpiecznie
            
            if (component.NetValue.HasValue)
                totalNet = (totalNet ?? 0m) + component.NetValue.Value;
            if (component.GrossValue.HasValue)
                totalGross = (totalGross ?? 0m) + component.GrossValue.Value;
            if (component.VatValue.HasValue)
                totalVat = (totalVat ?? 0m) + component.VatValue.Value;
        }
        
        item.NetValue = totalNet;
        item.GrossValue = totalGross;
        item.VatValue = totalVat;
        return;
    }

    // === POZYCJA Z WŁASNYMI WARTOŚCIAMI ===
    // Używamy direct properties zamiast FieldValues
    decimal? quantity = item.Quantity;
    decimal? unitPriceNet = item.UnitPriceNet;
    decimal? vatRate = item.VatRate;
    decimal? valueNetField = item.NetValue;  // User mógł wpisać wartość netto
    decimal? valueGrossField = item.GrossValue; // User mógł wpisać wartość brutto
    decimal? totalVatField = item.VatValue;   // User mógł wpisać vat

    // Oblicz wartości używając tej samej logiki co wcześniej
    // ale teraz operujemy na direct properties
    
    decimal? valueNet = CalculateValueNet(unitPriceNet, quantity, valueNetField);
    decimal? totalVat;
    
    if (valueNet.HasValue && vatRate.HasValue)
    {
        totalVat = valueNet.Value * vatRate.Value;
    }
    else
    {
        totalVat = totalVatField;
    }
    
    decimal? valueGross;
    if (valueNet.HasValue && totalVat.HasValue)
    {
        valueGross = valueNet.Value + totalVat.Value;
    }
    else if (valueNet.HasValue && vatRate.HasValue)
    {
        valueGross = valueNet.Value * (1m + vatRate.Value);
    }
    else
    {
        valueGross = valueGrossField;
    }

    // Oblicz ceny jednostkowe
    if (unitPriceNet.HasValue && vatRate.HasValue)
    {
        item.UnitPriceGross = unitPriceNet.Value * (1m + vatRate.Value);
    }
    else if (valueGross.HasValue && quantity.HasValue && quantity.Value != 0m)
    {
        item.UnitPriceGross = valueGross.Value / quantity.Value;
    }

    // Zapisz obliczone wartości
    item.NetValue = valueNet;
    item.GrossValue = valueGross;
    item.VatValue = totalVat;
}
```

#### Metody pomocnicze (dostosowane)

```csharp
private static decimal? CalculateValueNet(decimal? unitPriceNet, decimal? quantity, decimal? valueNetField)
{
    if (unitPriceNet.HasValue && quantity.HasValue)
        return unitPriceNet.Value * quantity.Value;
    return valueNetField;
}

private static decimal? CalculateTotalVat(decimal? valueNet, decimal? vatRate, decimal? totalVatField)
{
    if (valueNet.HasValue && vatRate.HasValue)
        return valueNet.Value * vatRate.Value;
    return totalVatField;
}

private static decimal? CalculateValueGross(decimal? valueNet, decimal? totalVat, decimal? vatRate, decimal? valueGrossField)
{
    if (valueNet.HasValue && totalVat.HasValue)
        return valueNet.Value + totalVat.Value;
    if (valueNet.HasValue && vatRate.HasValue)
        return valueNet.Value * (1m + vatRate.Value);
    return valueGrossField;
}
```

### 2. Spójność z UI

UI ma w `recalculateCostEstimateDetails.ts` podobną logikę. Po tej zmianie:
- API i UI będą spójne
- Oba używają IsSelected
- Oba propagują opcje
- Oba sumują komponenty z IsSelected=true

**Nie zmieniaj UI w tym prompcie** — to będzie w UI fix.

### Build

```powershell
dotnet build --configuration Release
```
Jeśli build failed, przerwij i zgłoś błędy.
