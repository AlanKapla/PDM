# Prompt implementacyjny: filter-schedule-items-by-relationtype-api-fix-01

## Cel
Dodanie filtrowania po `ItemRelationType.None` w `WorkScheduleSyncService.SyncWorksFromItemsAsync`, aby tylko pozycje główne kosztorysu trafiały do harmonogramu.

## Plik do zmiany
- `02-ApplicationServices/ProductDataManagementWebAPI/src/Business/Implementation/Services/WorkScheduleSyncService.cs`

## Zakres zmian

### Miejsce: linia 249 (metoda `SyncWorksFromItemsAsync`)

**Przed:**
```csharp
List<CostEstimateItem> workScopeItems = groupItems.Where(IsWorkScopeItem).ToList();
```

**Po:**
```csharp
List<CostEstimateItem> workScopeItems = groupItems
    .Where(i => i.RelationType == ItemRelationType.None && IsWorkScopeItem(i))
    .ToList();
```

### Dodatkowe uwagi
- Import `using Entities.Models.CostEstimates;` — **już istnieje** w pliku (linia 10), nie trzeba dodawać
- `ItemRelationType` jest enumem zdefiniowanym w `Entities.Models.CostEstimates`
- Kolejność warunków: `RelationType == None` pierwszy (szybsze odrzucenie) + `IsWorkScopeItem` drugi

## Weryfikacja
1. `dotnet build --configuration Release` w katalogu `02-ApplicationServices/ProductDataManagementWebAPI` — musi przejść
2. `dotnet test tests/Business.Tests --configuration Release --no-build` — wszystkie testy muszą przejść (istniejące nie używają itemów)
