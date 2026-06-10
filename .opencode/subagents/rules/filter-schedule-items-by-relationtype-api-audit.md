# Audyt API — filter-schedule-items-by-relationtype

**Data:** 2026-06-10
**Agent:** API Audit Agent
**Feature:** Filtrowanie pozycji głównych (`RelationType = None`) przy synchronizacji harmonogramu z kosztorysu

---

## BLOK 1 — Stan obecny

### Encje zaangażowane

| Encja | Rola |
|-------|------|
| `CostEstimateItem` | Źródło danych: pozycje kosztorysu z polem `RelationType` (None/Option/Component) oraz polami systemowymi (`FieldValues`) |
| `WorkScheduleStage` | Etapy harmonogramu, mapowane 1:1 z `CostEstimateGroup` |
| `WorkScheduleStageWork` | Zakresy prac w harmonogramie, mapowane 1:1 z `CostEstimateItem` poprzez `CostEstimateItemId` |

### `ItemRelationType` enum

| Wartość | Znaczenie |
|---------|-----------|
| `None = 0` | Pozycja główna (`ParentItemId == null`) |
| `Option = 1` | Opcja (wariant) pozycji nadrzędnej |
| `Component = 2` | Komponent (składnik) pozycji głównej |

### Endpointy i handlery wywołujące synchronizację

Wszystkie trzy ścieżki prowadzą przez jedyne publiczne wejście — `IWorkScheduleSyncService.SyncFromCostEstimateAsync()`:

| Handler | Linia | Kontekst |
|---------|-------|----------|
| `CreateWorkScheduleCommandHandler` | 41 | Tworzenie nowego harmonogramu z kosztorysu |
| `SyncWorkScheduleWithEstimateCommandHandler` | 60 | Ręczna resynchronizacja |
| `GenerateScheduleFromEstimateAICommandHandler` | 74 | Generowanie harmonogramu przez AI |

### Przepływ synchronizacji

```
SyncFromCostEstimateAsync()
  ├── SoftDeleteObsoleteStagesAsync()     ← grupy usunięte z CE
  ├── ProcessGroupsAsync()                ← tworzy/aktualizuje stage
  ├── SaveChangesAsync()                  ← zapis stage'y
  ├── SyncWorksFromItemsAsync()           ← ← ← MIEJSCE ZMIANY
  └── SaveChangesAsync()                  ← zapis work'i
```

### Obecne filtrowanie w `SyncWorksFromItemsAsync` (linia 249)

```csharp
List<CostEstimateItem> workScopeItems = groupItems.Where(IsWorkScopeItem).ToList();
```

gdzie `IsWorkScopeItem` (linia 353-358):

```csharp
private static bool IsWorkScopeItem(CostEstimateItem item)
{
    return item.FieldValues.Any(fv =>
        fv.FieldDefinition.FieldType == FieldType.ItemSystemIsWorkScope &&
        fv.BoolValue == true);
}
```

**Problem:** `IsWorkScopeItem` sprawdza TYLKO pole `ItemSystemIsWorkScope == true`. Nie sprawdza `RelationType`. To oznacza, że pozycje typu Option i Component, które mają ustawione `IsWorkScope = true` (np. automatycznie przez AI import), są błędnie włączane do harmonogramu.

### Źródło problemu — auto-ustawianie `IsWorkScope` w AI import

W `CreateCostEstimateFromAIPreviewCommandHandler.InsertItemFieldValues()` (linie 274-287):

```csharp
// Auto-ustaw Zakres pracy (ItemSystemIsWorkScope) na true
CostEstimateTemplateFieldDefinitionBase? workScopeDef = allFieldDefs.Values
    .FirstOrDefault(d => d.FieldType == FieldType.ItemSystemIsWorkScope
          && d.ParentFieldId == null && !providedIds.Contains(d.Id));
if (workScopeDef is not null)
{
    await itemFieldValueRepository.Insert(new CostEstimateItemFieldValue
    {
        ItemId = itemId,
        FieldDefinitionId = workScopeDef.Id,
        BoolValue = true,
        ...
    });
}
```

Ta metoda jest wywoływana **zarówno dla pozycji głównych** (`ItemRelationType.None`, linia 175) **jak i dla komponentów** (`ItemRelationType.Component`, linia 170). W efekcie komponenty również dostają `IsWorkScope = true`, co przy obecnym filtrowaniu powoduje ich dodanie do harmonogramu.

---

## BLOK 2 — Luki i braki

| Brak / Luka | Warstwa | Priorytet | Opis |
|-------------|---------|----------|------|
| Brak filtrowania po `RelationType` | Business/Services | WYSOKI | `SyncWorksFromItemsAsync` nie filtruje po `i.RelationType == ItemRelationType.None` |
| `IsWorkScopeItem` nie bierze pod uwagę typu relacji | Business/Services | WYSOKI | Metoda sprawdza tylko bool, pomija `RelationType` co jest niezgodne z intencją |
| AI import ustawia `IsWorkScope` na komponentach | CQRS | ŚREDNI | `CreateCostEstimateFromAIPreviewCommandHandler` ustawia `IsWorkScope = true` automatycznie dla wszystkich items, w tym komponentów |
| Brak testów pokrywających filtrowanie itemów | Tests | WYSOKI | Istniejące testy używają pustych list itemów — brak testów dla logiki filtrowania |

---

## BLOK 3 — Zmiany w encjach/DB

**Brak zmian w encjach ani w bazie danych.**

Zmiana dotyczy wyłącznie logiki filtrowania w C# — nie wymaga migracji EF Core ani zmian schematu.

---

## BLOK 4 — Nowe Commands/Queries

**Brak.** Nie trzeba tworzyć ani modyfikować Command/Query.

---

## BLOK 5 — Zmiany w kontrolerach

**Brak.** Kontrolery nie wymagają zmian.

---

## BLOK 6 — Zmiany w serwisach

### WorkScheduleSyncService.cs — zakres zmian

**Miejsce:** Linia 249 w `SyncWorksFromItemsAsync`

**Obecnie:**
```csharp
List<CostEstimateItem> workScopeItems = groupItems.Where(IsWorkScopeItem).ToList();
```

**Po zmianie:**
```csharp
List<CostEstimateItem> workScopeItems = groupItems
    .Where(i => IsWorkScopeItem(i) && i.RelationType == ItemRelationType.None)
    .ToList();
```

**Alternatywnie** — można zmodyfikować `IsWorkScopeItem` na `IsPrimaryWorkScopeItem` i dodać sprawdzenie `RelationType` wewnątrz metody:
```csharp
private static bool IsPrimaryWorkScopeItem(CostEstimateItem item)
{
    return item.RelationType == ItemRelationType.None
        && item.FieldValues.Any(fv =>
            fv.FieldDefinition.FieldType == FieldType.ItemSystemIsWorkScope &&
            fv.BoolValue == true);
}
```

**Rekomendacja:** Preferowana jest pierwsza opcja (dodanie warunku w `Where`), ponieważ:
- Zachowuje SRP — `IsWorkScopeItem` sprawdza tylko boolowski flag
- `RelationType` to inna odpowiedzialność
- Mniejsza zmiana, czytelniejsza intencja

**Wymagany import:** `using Entities.Models.CostEstimates;` — sprawdzić czy już istnieje (linia 10 w pliku: `using Entities.Models.CostEstimates;` — TAK, istnieje).

---

## BLOK 7 — Problemy i ryzyka

| # | Problem | Warstwa | Ryzyko | Rekomendacja |
|---|---------|---------|--------|-------------|
| 1 | Istniejące dane: harmonogramy zawierają już `WorkScheduleStageWork` utworzone z itemów o `RelationType != None` | Dane | WYSOKIE | Przy pierwszym resecie (`SyncWorkScheduleWithEstimate`) pozycje te zostaną **automatycznie soft-deleted** przez istniejącą logikę w `DeleteObsoleteWorkScopesAsync` — nie trzeba dodatkowej migracji |
| 2 | AI import wciąż ustawia `IsWorkScope = true` na komponentach | CQRS | NISKIE | Po dodaniu filtra nie ma to znaczenia dla harmonogramu, ale jest to zbędne ustawianie flagi. Można rozważyć oddzielną optymalizację, ale wykracza poza zakres tego feature |
| 3 | `GenerateScheduleFromEstimateAICommandHandler` ma własny check `if (workInputs.Count == 0)` (linia 110-114) | CQRS | NISKIE | Jeśli po zmianie filtr wykluczy wszystkie itemy (np. wszystkie pozycje w kosztorysie to Option/Component), handler rzuci `ValidationApiException("No work items found after synchronization...")`. To jest **poprawne zachowanie** — użytkownik otrzyma czytelny komunikat |
| 4 | `IsWorkScopeItem` nazwa staje się nieprecyzyjna (nie odzwierciedla pełnego filtra) | Business/Services | KOSMETYCZNE | Można rozważyć refactor w przyszłości, ale poza zakresem |

---

## Edge case'y — analiza

### 1. Co się stanie, gdy istnieją już pozycje harmonogramu z `RelationType != None`, a potem nastąpi resync?

**Zachowanie:** Istniejące `WorkScheduleStageWork` dla itemów o `RelationType != None` zostaną **soft-deleted** przez istniejącą logikę:

1. `SyncWorksFromItemsAsync` ładuje wszystkie istniejące `existingLinkedWorks` (linia 229-231)
2. Buduje `existingWorkByItemId` słownik (linia 233-234)
3. Iteruje przez itemy — do `activeItemIds` trafiają tylko te, które przejdą nowy filtr (z `RelationType == None`)
4. `worksToDelete` = wszystkie istniejące linki, których `CostEstimateItemId` NIE MA w `activeItemIds` (linia 253-255)
5. Są one soft-deleted przez `DeleteObsoleteWorkScopesAsync` (linia 257)

**Wniosek:** Mechanizm automatycznego czyszczenia DZIAŁA — nie wymaga dodatkowej logiki.

### 2. Czy zmiana wpływa na istniejące dane?

**Bezpośrednio:** Nie. Zmiana wpływa tylko na przyszłe synchronizacje.
**Pośrednio:** Przy pierwszym resecie, niechciane pozycje zostaną usunięte (soft delete). To jest oczekiwane zachowanie (kryterium akceptacji #5).

### 3. Czy jest inna ścieżka, którą pozycje nie-główne mogą trafić do harmonogramu?

**Nie.** `SyncWorksFromItemsAsync` jest jedynym miejscem w całym codebase, które tworzy `WorkScheduleStageWork` z `CostEstimateItem`. Nie ma:
- Bezpośredniego tworzenia work'ów w kontrolerach
- Innych serwisów mapujących itemy na worki
- SQL raw/komend ADO.NET omijających repozytorium
- Innych handlerów CQRS tworzących `WorkScheduleStageWork` z linkiem do `CostEstimateItem`

---

## Testy jednostkowe

### Istniejące testy — analiza wpływu

Wszystkie istniejące testy w `WorkScheduleSyncServiceTests.cs` używają pustych list itemów (`.ReturnsAsync([])`):

| Test | Używa itemów? | Wpływ zmiany |
|------|--------------|--------------|
| `SyncFromCostEstimateAsync_NoCostEstimateId_ThrowsInvalidOperationException` | Nie | **Brak** — rzuca wyjątek przed filtrowaniem |
| `SyncFromCostEstimateAsync_NoGroups_ReturnsEmptyStagesList` | Tak (pusta lista) | **Brak** — pusta lista → puste `itemsByGroupId` → brak iteracji |
| `SyncFromCostEstimateAsync_OneRootGroup_CreatesOneStage` | Tak (pusta lista) | **Brak** |
| `SyncFromCostEstimateAsync_ExistingStageForGroup_UpdatesInsteadOfInserting` | Tak (pusta lista) | **Brak** |
| `SyncFromCostEstimateAsync_ObsoleteStage_SoftDeletesIt` | Tak (pusta lista) | **Brak** |
| `SyncFromCostEstimateAsync_TwoRootGroups_CreatesTwoStagesInOrder` | Tak (pusta lista) | **Brak** |

**Wniosek:** Żaden z istniejących testów nie przestanie przechodzić po zmianie.

### Nowe testy do dodania

Poniższe testy powinny być dodane do `WorkScheduleSyncServiceTests.cs`:

| # | Nazwa testu | Scenariusz | Oczekiwany rezultat |
|---|-------------|------------|---------------------|
| 1 | `SyncFromCostEstimateAsync_WorkScopeItemWithRelationTypeNone_CreatesWork` | Item z `RelationType=None`, `IsWorkScope=true` | Work tworzony |
| 2 | `SyncFromCostEstimateAsync_WorkScopeItemWithRelationTypeOption_SkipsWork` | Item z `RelationType=Option`, `IsWorkScope=true` | Work NIE tworzony |
| 3 | `SyncFromCostEstimateAsync_WorkScopeItemWithRelationTypeComponent_SkipsWork` | Item z `RelationType=Component`, `IsWorkScope=true` | Work NIE tworzony |
| 4 | `SyncFromCostEstimateAsync_NonWorkScopeItemWithRelationTypeNone_SkipsWork` | Item z `RelationType=None`, `IsWorkScope=false` | Work NIE tworzony |
| 5 | `SyncFromCostEstimateAsync_ExistingWorkForNonMainItem_SoftDeletedOnResync` | Istniejący Work dla Option/Component — resync | Stary work soft-deleted |
| 6 | `SyncFromCostEstimateAsync_OnlyNonMainItems_NoWorksCreated` | Tylko Option/Component z `IsWorkScope=true` | Żaden work nie utworzony |

### Struktura nowego testu (przykład dla scenariusza #1 i #2)

```csharp
[Fact]
public async Task SyncFromCostEstimateAsync_WorkScopeItemWithRelationTypeNone_CreatesWork()
{
    // Arrange
    Guid ceId = Guid.NewGuid();
    Guid groupId = Guid.NewGuid();
    Guid scheduleId = Guid.NewGuid();
    Guid fieldDefId = Guid.NewGuid();
    WorkSchedule schedule = new()
    {
        Id = scheduleId,
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        CostEstimateId = ceId
    };

    CostEstimateGroup group = new()
    {
        Id = groupId,
        CostEstimateId = ceId,
        Level = 0,
        Order = 0,
        FieldValues = new List<CostEstimateGroupFieldValue>()
    };

    CostEstimateItem item = new()
    {
        Id = Guid.NewGuid(),
        CostEstimateId = ceId,
        GroupId = groupId,
        RelationType = ItemRelationType.None,  // ← KLUCZOWE: główna pozycja
        Order = 0,
        IsDeleted = false,
        FieldValues = new List<CostEstimateItemFieldValue>
        {
            new()
            {
                FieldDefinition = new CostEstimateTemplateFieldDefinition
                {
                    FieldType = FieldType.ItemSystemIsWorkScope
                },
                BoolValue = true
            }
        }
    };

    // ... setup mocków ...

    // Act
    List<WorkScheduleStage> result = await _sut.SyncFromCostEstimateAsync(schedule, CancellationToken.None);

    // Assert — work powinien być utworzony
    _workRepoMock.Verify(r => r.Insert(It.Is<WorkScheduleStageWork>(w =>
        w.CostEstimateItemId == item.Id)), Times.Once);
}
```

Dla przypadku negatywnego (`RelationType = Option`):
```csharp
// Zmieniamy tylko:
item.RelationType = ItemRelationType.Option;

// Assert — work NIE powinien być utworzony
_workRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStageWork>()), Times.Never);
```

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe encje | 0 |
| Nowe Commands | 0 |
| Nowe Queries | 0 |
| Nowe endpointy | 0 |
| Nowe serwisy | 0 |
| Zmodyfikowane serwisy | 1 (`WorkScheduleSyncService`) |
| Zmodyfikowane handlery | 0 |
| Zmodyfikowani kontrolery | 0 |
| Linie kodu do zmiany | **1** (dodanie warunku w Where) |
| Wymaga migracji DB | **NIE** |
| Wymaga zmiany encji | **NIE** |
| Istniejące testy do poprawy | 0 |
| Nowe testy do dodania | **5–6** |
| Pytania domenowe | 0 |

### Zakres zmian — rekomendacja

**Tylko 1 linia w `WorkScheduleSyncService.cs`** (linia 249):

```csharp
// PRZED:
List<CostEstimateItem> workScopeItems = groupItems.Where(IsWorkScopeItem).ToList();

// PO:
List<CostEstimateItem> workScopeItems = groupItems
    .Where(i => IsWorkScopeItem(i) && i.RelationType == ItemRelationType.None)
    .ToList();
```

### Potencjalne ryzyka

1. **Ryzyko niskie:** Jeśli wszystkie pozycje w kosztorysie to Option/Component z `IsWorkScope=true`, `GenerateScheduleFromEstimateAICommandHandler` rzuci `ValidationApiException("No work items found after synchronization...")`. To jest **poprawne zachowanie** użytkownik zobaczy błąd mówiący, że nie ma pozycji do wygenerowania harmonogramu.

2. **Ryzyko niskie:** Po resecie istniejące niechciane pozycje harmonogramu zostaną soft-deleted. Użytkownik może być zaskoczony, że "zniknęły" pozycje — warto dodać wpis w changelogu.

### Pytania domenowe

**Brak.** Feature jest dobrze zdefiniowany, zmiana jest jednoznaczna i nie wymaga decyzji domenowych.
