# Prompt implementacyjny: filter-schedule-items-by-relationtype-api-fix-02

## Cel
Dodanie testów jednostkowych dla nowego filtrowania po `ItemRelationType.None` w `WorkScheduleSyncService`.

## Plik do zmiany
- `02-ApplicationServices/ProductDataManagementWebAPI/tests/Business.Tests/Services/WorkScheduleSyncServiceTests.cs`

## Wzorzec testów

Każdy test musi:
1. Utworzyć `WorkSchedule` z `CostEstimateId`
2. Utworzyć jedną `CostEstimateGroup`
3. Skonfigurować mocki repozytoriów
4. Wywołać `_sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None)`
5. Asercja na `_workRepoMock.Verify(r => r.Insert(...))` — czy work został utworzony, czy nie

### Wzorcowy setup mocka dla itemów
```csharp
_itemRepoMock
    .Setup(r => r.GetBySearch(
        It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimateItem, bool>>>(),
        It.IsAny<Func<IQueryable<CostEstimateItem>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CostEstimateItem, object>>[]>()))
    .ReturnsAsync([item1, item2, ...]);
```

### Wzorcowy setup mocka dla worków (gdy nie ma istniejących)
```csharp
_workRepoMock
    .Setup(r => r.GetBySearch(
        It.IsAny<System.Linq.Expressions.Expression<Func<WorkScheduleStageWork, bool>>>(),
        It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
    .ReturnsAsync([]);
```

### Wzorcowy setup dla field definition
```csharp
CostEstimateItem item = new()
{
    Id = Guid.NewGuid(),
    CostEstimateId = ceId,
    GroupId = groupId,
    RelationType = ItemRelationType.None, // lub Option / Component
    Order = 0,
    IsDeleted = false,
    FieldValues = new List<CostEstimateItemFieldValue>
    {
        new()
        {
            FieldDefinition = new CostEstimateTemplateFieldDefinition
            {
                Id = Guid.NewGuid(),
                FieldType = FieldType.ItemSystemIsWorkScope
            },
            BoolValue = true // lub false
        }
    }
};
```

## Testy do dodania

### 1. `SyncFromCostEstimateAsync_WorkScopeItemWithRelationTypeNone_CreatesWork`
- Item z `RelationType = None`, `IsWorkScope = true`
- Oczekiwanie: `_workRepoMock.Verify(r => r.Insert(It.Is<WorkScheduleStageWork>(w => w.CostEstimateItemId == item.Id)), Times.Once)`

### 2. `SyncFromCostEstimateAsync_WorkScopeItemWithRelationTypeOption_SkipsWork`
- Item z `RelationType = Option`, `IsWorkScope = true`
- Oczekiwanie: `_workRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStageWork>()), Times.Never)`

### 3. `SyncFromCostEstimateAsync_WorkScopeItemWithRelationTypeComponent_SkipsWork`
- Item z `RelationType = Component`, `IsWorkScope = true`
- Oczekiwanie: `_workRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStageWork>()), Times.Never)`

### 4. `SyncFromCostEstimateAsync_NonWorkScopeItemWithRelationTypeNone_SkipsWork`
- Item z `RelationType = None`, `IsWorkScope = false`
- Oczekiwanie: `_workRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStageWork>()), Times.Never)`

### 5. `SyncFromCostEstimateAsync_ExistingWorkForNonMainItem_SoftDeletedOnResync`
- Istniejący `WorkScheduleStageWork` z `CostEstimateItemId` dla itemu który ma `RelationType = Option` i `IsWorkScope = true`
- Po resecie: istniejący work powinien być soft-deleted
- Oczekiwanie: `_workRepoMock.Verify(r => r.UpdateRange(It.Is<List<WorkScheduleStageWork>>(list => list.Any(w => w.IsDeleted))), Times.Once)`
- LUB sprawdzenie że `work.IsDeleted == true` po wywołaniu

### 6. `SyncFromCostEstimateAsync_OnlyNonMainItems_NoWorksCreated`
- Dwa itemy: jeden z `RelationType = Option`, drugi z `RelationType = Component`, oba `IsWorkScope = true`
- Oczekiwanie: `_workRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStageWork>()), Times.Never)`

## Uwagi
- Użyj istniejących wzorców z pliku testowego (ten sam setup mocków, ten sam styl asercji przez FluentAssertions)
- Nie modyfikuj istniejących testów — tylko dodaj nowe
- Upewnij się, że `FieldType` jest zaimportowane (`using Entities.Models.CostEstimateTemplates;`)
- Użyj `using Entities.Models.CostEstimates;` dla `ItemRelationType`

## Weryfikacja
1. `dotnet build --configuration Release` w katalogu `02-ApplicationServices/ProductDataManagementWebAPI`
2. `dotnet test tests/Business.Tests --configuration Release` — wszystkie testy (stare i nowe) muszą przejść
