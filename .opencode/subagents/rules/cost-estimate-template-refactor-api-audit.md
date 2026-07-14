# Audyt API: cost-estimate-template-refactor

## 1. Obecny sposób budowania kolumn i problemy

### `BuildTemplateStructureAsync` — obecna implementacja

Plik: `CostEstimateTemplateService.cs`, linie 324-348.

```csharp
var allFieldsList = new List<CostEstimateTemplateFieldDefinitionBase>();
allFieldsList.AddRange(groupHeaderFieldsList);
allFieldsList.AddRange(systemFieldsList);
allFieldsList.AddRange(calculatedFieldsList);
allFieldsList.AddRange(genericFieldsList);

var columns = allFieldsList
    .Where(f => f.ParentFieldId == null)
    .OrderBy(f => f.Order)
    .Select(f => new ColumnConfigurationWeb(...))
    .ToList();

UiConfigurationWeb? uiConfig = columns.Any() 
    ? new UiConfigurationWeb(columns) 
    : null;
```

**Problemy:**

| # | Problem | Szczegóły |
|---|---------|-----------|
| 1 | **Mieszanie pól Group i Item w jednej liście** | `allFieldsList` łączy groupHeaderFields, systemFields, calculatedFields, genericFields — a potem sortuje globalnie po `Order`. Oznacza to, że kolejność pola grupowego i pola pozycji są ze sobą wymieszane. UI nie może wyodrębnić osobnych list. |
| 2 | **Order jest globalny, a nie per-scope** | `Order` w encji jest ustawiany na podstawie pozycji w płaskiej liście `ColumnLayout` (zob. `BuildColumnLayoutOrderMap`). Nie ma separacji między grupami a pozycjami. |
| 3 | **Default templates i DuplicateTemplate używają tego samego wzorca** | `MapDefaultTemplateToStructure` (linia 1034-1050) i `DuplicateTemplateAsync` (linia 1224-1230) również łączą wszystkie typy pól w jedną listę i tworzą płaskie `columns`. |
| 4 | **Niespójne filtrowanie IsVisible** | `BuildTemplateStructureAsync` **nie** filtruje po `IsVisible` (celowo — cache jest neutralny). Natomiast `MapDefaultTemplateToStructure` filtruje po `.Where(f => f.IsVisible)`. To niespójne. |
| 5 | **UiConfigurationDto.ColumnLayout to jedna płaska lista** | Command `UpdateCostEstimateTemplateCommand` przyjmuje `UiConfigurationDto` z jednym `ColumnLayout` (List\<Guid\>?). Nie ma rozróżnienia na group layout vs item layout. |

### Jak to działa dzisiaj (przepływ)

1. **Zapis szablonu** → frontend wysyła `UpdateCostEstimateTemplateCommand` z `UiConfigurationDto.ColumnLayout` — płaską listą GUID-ów wszystkich pól w żądanej kolejności.
2. **BuildColumnLayoutOrderMap** → tworzy słownik `fieldGuid → index` z pozycji w liście.
3. **CollectFieldsForUpsert** → ustawia `field.Order = columnLayoutOrderMap[fieldDto.FieldName]` dla pól nadrzędnych.
4. **Odczyty** → `BuildTemplateStructureAsync` ładuje wszystkie pola, scala w `allFieldsList`, sortuje globalnie po `Order` i produkuje jedną płaską listę `columns`.
5. **UI** → otrzymuje `UiConfigurationWeb.Columns` jako płaską listę i renderuje wszystkie kolumny w jednym wierszu nagłówkowym.

---

## 2. Zalecane zmiany w DTO

### 2.1 `UiConfigurationWeb` — podział na groupColumns i itemColumns

**Plik:** `CostEstimateTemplateStructureWeb.cs`

```csharp
// Obecnie:
public record UiConfigurationWeb(List<ColumnConfigurationWeb> Columns);

// Po refaktorze:
public record UiConfigurationWeb(
    List<ColumnConfigurationWeb> GroupColumns,
    List<ColumnConfigurationWeb> ItemColumns
);
```

Kolumny grupowe zawierają tylko pola `FieldScope == FieldScope.Group` (0), posortowane po `Order` w ramach group.
Kolumny itemowe zawierają tylko pola `FieldScope == FieldScope.ItemSystem/ItemCalculated/ItemGeneric` (1,2,3), posortowane po `Order` w ramach item.

### 2.2 `UiConfigurationDto` — osobne listy dla group i item layout

**Plik:** `CostEstimateTemplateDtos.cs`

```csharp
// Obecnie:
public record UiConfigurationDto(List<Guid>? ColumnLayout);

// Po refaktorze:
public record UiConfigurationDto(
    List<Guid>? GroupColumnLayout,
    List<Guid>? ItemColumnLayout
);
```

### 2.3 `UpdateCostEstimateTemplateCommand` — rozdzielenie layoutu

**Plik:** `UpdateCostEstimateTemplateCommand.cs`

Property `UiConfigurationDto? UiConfiguration` pozostaje, ale zmienia się wewnętrzna struktura DTO (jak wyżej).

### 2.4 `ColumnConfigurationWeb` — zostaje bez zmian

Struktura `ColumnConfigurationWeb` (FieldId, FieldName, FieldType, FieldLabel, FieldScope, Order, IsVisible) pozostaje taka sama — zmienia się tylko to, że teraz będzie używana w dwóch osobnych listach.

### 2.5 `BuildTemplateStructureAsync` — nowa logika

Zamiast scalać wszystkie fields w `allFieldsList`:

```csharp
// Zamiast:
var allFieldsList = new List<...>();
allFieldsList.AddRange(groupHeaderFieldsList);
allFieldsList.AddRange(systemFieldsList);
allFieldsList.AddRange(calculatedFieldsList);
allFieldsList.AddRange(genericFieldsList);

var columns = allFieldsList.Where(f => f.ParentFieldId == null).OrderBy(f => f.Order)...

// Powinno być:
var groupColumns = groupHeaderFieldsList  // tylko Group scope
    .Where(f => f.ParentFieldId == null)
    .OrderBy(f => f.Order)
    .Select(f => new ColumnConfigurationWeb(...))
    .ToList();

var itemFieldsList = new List<CostEstimateTemplateFieldDefinitionBase>();
itemFieldsList.AddRange(systemFieldsList);      // ItemSystem
itemFieldsList.AddRange(calculatedFieldsList);   // ItemCalculated
itemFieldsList.AddRange(genericFieldsList);      // ItemGeneric

var itemColumns = itemFieldsList
    .Where(f => f.ParentFieldId == null)
    .OrderBy(f => f.Order)
    .Select(f => new ColumnConfigurationWeb(...))
    .ToList();

UiConfigurationWeb? uiConfig = (groupColumns.Any() || itemColumns.Any()) 
    ? new UiConfigurationWeb(groupColumns, itemColumns) 
    : null;
```

### 2.6 `MapDefaultTemplateToStructure` — analogiczna zmiana

**Plik:** `CostEstimateTemplateService.cs`, linie 1034-1052.

Zamiast:
```csharp
var allFields = template.GroupHeaderFields.Concat(...).Concat(...).Concat(...).Where(f => f.IsVisible);
var columns = allFields.Select((f, index) => new ColumnConfigurationWeb(...)).ToList();
```

Powinno być:
```csharp
var groupColumns = template.GroupHeaderFields
    .Where(f => f.IsVisible)
    .Select((f, index) => new ColumnConfigurationWeb(...))
    .ToList();

var systemItemFields = template.SystemFields
    .Concat(template.CalculatedFields)
    .Concat(template.GenericFields)
    .Where(f => f.IsVisible)
    .Select((f, index) => new ColumnConfigurationWeb(...))
    .ToList();
```

### 2.7 `DuplicateTemplateAsync` i `CreateTemplateFromDefaultAsync` — osobne layouty

**Plik:** `CostEstimateTemplateService.cs`, linie 1141-1147 i 1224-1230.

Zamiast jednej płaskiej listy `columnLayout`, tworzyć dwie:
```csharp
var groupColumnLayout = groupFields.Where(f => f.IsVisible).Select(f => f.FieldName).ToList();
var itemColumnLayout = systemFields.Concat(calculatedFields).Concat(genericFields)
    .Where(f => f.IsVisible).Select(f => f.FieldName).ToList();
```

---

## 3. Mechanizm wymuszania GroupName i ItemSystemName jako pierwszych

### Obecny stan

**Plik:** `CostEstimateTemplateHandlerBase.cs`

```csharp
private static readonly FieldType[] RequiredFieldTypes =
[
    FieldType.GroupName,          // 0
    FieldType.ItemSystemName,     // 100
    FieldType.ItemCalculatedValueNet,  // 203
    FieldType.ItemCalculatedValueGross, // 204
];

protected static void ValidateRequiredTemplateFields(IEnumerable<FieldType> presentFieldTypes)
{
    List<FieldType> missingFields = RequiredFieldTypes
        .Where(required => !presentFieldTypes.Contains(required))
        .ToList();
    if (missingFields.Count > 0)
    {
        string missingFieldNames = string.Join(", ", missingFields.Select(f => f.ToString()));
        throw new ValidationApiException($"Template is missing required fields: {missingFieldNames}.");
    }
}
```

**Co jest walidowane:**
- Obecność `GroupName` (0) i `ItemSystemName` (100) — tak, są wymagane.
- Obecność `ItemCalculatedValueNet` i `ItemCalculatedValueGross` — też wymagane.

**Czego NIE waliduje:**
- **Kolejności** — nie wymusza, że `GroupName` ma być pierwszy wśród group fields, a `ItemSystemName` pierwszy wśród item fields.
- **Pozycji** — nie sprawdza, czy `Order` tych pól = 0 w ramach scope.

### Zalecana zmiana

Dodać walidację, która wymusza, że:
1. `GroupName` ma `Order == 0` w ramach group fields (lub jest pierwszy w `GroupColumnLayout`)
2. `ItemSystemName` ma `Order == 0` w ramach item fields (lub jest pierwszy w `ItemColumnLayout`)

Można to zrobić:
- W `UpdateCostEstimateTemplateCommandHandler` — po sparsowaniu layoutu
- W `CollectFieldsForUpsert` — przy ustawianiu Order

**Propozycja:** Dodać metodę w `CostEstimateTemplateHandlerBase`:
```csharp
protected static void ValidateFieldOrdering(
    List<FieldDefinitionDto> groupFields,
    List<FieldDefinitionDto> systemFields,
    List<Guid>? groupColumnLayout,
    List<Guid>? itemColumnLayout)
```

Która sprawdza, że:
- `GroupName` (FieldType == 0) istnieje w groupFields i/lub jest pierwszy w groupColumnLayout
- `ItemSystemName` (FieldType == 100) istnieje w systemFields i/lub jest pierwszy w itemColumnLayout

---

## 4. Jak osobne listy kolumn wpłyną na UI

### Co UI otrzyma z API (zmiany w typach)

**Plik:** `01-Applications/ProjectDataManagementUI/src/types/costEstimate.types.ts`

```typescript
// Obecnie:
export interface UiConfigurationWeb {
  columns: ColumnConfigurationWeb[];
}

// Po refaktorze:
export interface UiConfigurationWeb {
  groupColumns: ColumnConfigurationWeb[];
  itemColumns: ColumnConfigurationWeb[];
}
```

### Jak UI będzie używać nowych list

| Obszar UI | Obecnie | Po refaktorze |
|-----------|---------|---------------|
| **Nagłówek tabeli (header row)** | `uiConfiguration.columns` — jedna lista, renderowane wszystkie kolumny | Osobny nagłówek dla `groupColumns` i osobny dla `itemColumns` (lub dwa rzędy nagłówków) |
| **Wiersze grup (group rows)** | Kolumny są renderowane na podstawie pozycji w `columns` — nie ma separacji | Renderuje tylko `groupColumns` |
| **Wiersze pozycji (item rows)** | To samo co grupy | Renderuje tylko `itemColumns` |
| **Collapsible field sections** | Nie istnieje | UI może pokazywać/ukrywać sekcję pól grupowych i itemowych osobno (feature spec pkt 4) |
| **Filtrowanie kolumn Restricted** | `templateStructure.UiConfiguration.Columns.Where(c => c.IsVisible)` | Trzeba filtrować OBIE listy: `GroupColumns.Where(c => c.IsVisible)` i `ItemColumns.Where(c => c.IsVisible)` |
| **Usunięcie kolumny "Pozycja"** | Sticky left column z ETAP/POZYCJA | Ma być usunięta (feature spec pkt 5) — osobne listy ułatwiają, bo każdy wiersz wie, które kolumny wyświetlić |

### Zmiany w `GetCostEstimateDetailsQueryHandler`

**Plik:** Lines 122-131

```csharp
// Obecnie:
if (accessLevel is CostEstimateAccessLevel.Restricted or CostEstimateAccessLevel.ReadOnly 
    && templateStructure.UiConfiguration is not null)
{
    var visibleColumns = templateStructure.UiConfiguration.Columns
        .Where(c => c.IsVisible)
        .ToList();
    templateStructure = templateStructure with
    {
        UiConfiguration = new UiConfigurationWeb(visibleColumns)
    };
}

// Po refaktorze:
if (accessLevel is CostEstimateAccessLevel.Restricted or CostEstimateAccessLevel.ReadOnly 
    && templateStructure.UiConfiguration is not null)
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

---

## 5. Znalezione problemy i ryzyka

| # | Problem | Warstwa | Ryzyko | Rekomendacja |
|---|---------|---------|--------|-------------|
| 1 | **Konieczność backward compatibility** — stare zapisane `UiConfigurationDto` z pojedynczym `ColumnLayout` mogą być używane przez istniejące UI lub cache | API/UI | Wysokie — stare UI wyśle `ColumnLayout` zamiast `GroupColumnLayout`/`ItemColumnLayout` | Dodać obsługę starego formatu w `BuildColumnLayoutOrderMap` (jeśli `ColumnLayout` istnieje, a `GroupColumnLayout` i `ItemColumnLayout` są null, rozdzielić po FieldScope). Po okresie przejściowym usunąć stary format. |
| 2 | **Niespójne filtrowanie IsVisible między BuildTemplateStructureAsync a MapDefaultTemplateToStructure** | API | Średnie — default templates pokazują mniej kolumn niż user-created templates | Ujednolicić: `BuildTemplateStructureAsync` (cache) nie powinien filtrować. Default templates też nie powinny — filtrowanie tylko w handlerze. |
| 3 | **Order nie jest unikalny w ramach scope** | API/DB | Niskie — dwa pola mogą mieć ten sam `Order`, co daje nieokreśloną kolejność. Przy sortowaniu `OrderBy(f => f.Order)` jeśli dwa pola mają ten sam Order, EF użyje kolejności w pamięci. | Po refaktorze Order jest nadawany z osobnych layoutów (group vs item) — wewnątrz scope Order będzie unikalny bo pochodzi z pozycji na liście. Dodać `ThenBy(f => f.FieldName)` jako tiebreaker. |
| 4 | **Zmiana w DTO UiConfigurationWeb wymaga zmiany typu w UI** | UI | Wysokie — wszystkie komponenty używające `uiConfiguration.columns` muszą być zaktualizowane | Frontend musi dodać obsługę `groupColumns` i `itemColumns`, a stare `columns` oznaczyć jako deprecated. |
| 5 | **Zmiana w UiConfigurationDto wymaga zmiany frontendowego api clienta** | UI/API | Średnie — frontend wysyła `columnLayout` w żądaniu update | Frontend musi wysyłać `groupColumnLayout` i `itemColumnLayout` osobno. |
| 6 | **Cache w Redis zawiera stare UiConfigurationWeb** | API | Średnie — po wdrożeniu zmiany, cache może zwrócić starą strukturę z jednym `columns` zamiast dwóch list | Po wdrożeniu zmiany należy unieważnić cache dla wszystkich szablonów (zmienić `CacheKeyPrefix` lub wersjonować strukturę cache). |
| 7 | **MapDefaultTemplateToStructure używa `IsVisible` podczas budowania kolumn, BuildTemplateStructureAsync nie** | API | Niskie — w default templates kolumny są filtrowane po IsVisible już na poziomie cache, co oznacza że Restricted user nie zobaczy różnicy (bo i tak by zostały odfiltrowane). Jednak Full access user zobaczy mniej kolumn niż by powinien. | Usunąć `.Where(f => f.IsVisible)` z `MapDefaultTemplateToStructure` — filtrowanie powinno być tylko w handlerze. |
| 8 | **Order w DefaultTemplate jest nadawany jako `index` (kolejność na liście), a nie pochodzi z encji** | API | Niskie — default templates nie mają encji DB, więc Order = index jest OK | Przy refaktorze, osobne indexy dla group i item fields. |
| 9 | **UpdateCostEstimateTemplateCommandHandler.ExtractFieldTypes nie uwzględnia GenericFields** | API | Niskie — `ExtractFieldTypes` bierze tylko GroupHeaderFields, SystemFields, CalculatedFields. GenericFields nie są sprawdzane pod kątem wymaganych pól, ale to OK bo GenericFields nie mają wymaganych pól. | Brak zmian — to jest poprawne. |
| 10 | **DuplicateTemplateAsync buduje columnLayout tylko z IsVisible = true** | API | Średnie — przy duplikacji, pola z IsVisible = false tracą swoją pozycję w layoutcie. Jeśli user później zmieni IsVisible na true, pole pojawi się na końcu (Order=0). | Po refaktorze: groupColumnLayout i itemColumnLayout powinny zawierać wszystkie pola (nie tylko IsVisible) lub osobna lista hidden fields. Obecne zachowanie można uznać za feature, ale warto być tego świadomym. |

---

## 6. Podsumowanie

| Metryka | Wartość |
|---------|---------|
| Nowe encje | 0 |
| Nowe Commands | 0 |
| Nowe Queries | 0 |
| Nowe endpointy | 0 |
| Zmodyfikowane Commands | 1 (`UpdateCostEstimateTemplateCommand`) |
| Zmodyfikowane DTO | 3 (`UiConfigurationWeb`, `UiConfigurationDto`, `CostEstimateTemplateStructureWeb`) |
| Zmodyfikowane serwisy | 1 (`CostEstimateTemplateService` — `BuildTemplateStructureAsync`, `MapDefaultTemplateToStructure`, `DuplicateTemplateAsync`, `CreateTemplateFromDefaultAsync`, `BuildColumnLayoutOrderMap`, `CollectFieldsForUpsert`) |
| Zmodyfikowane handlery | 2 (`GetCostEstimateDetailsQueryHandler`, `UpdateCostEstimateTemplateCommandHandler`) |
| Zmodyfikowany handler base | 1 (`CostEstimateTemplateHandlerBase` — nowa walidacja kolejności) |
| Wymaga migracji DB | **Nie** |
| Wymaga czyszczenia cache | **Tak** — unieważnić cache wszystkich szablonów po wdrożeniu |
| Zmiany w UI types | 1 (`UiConfigurationWeb.columns` → `groupColumns` + `itemColumns`) |
| Pytania domenowe | 2 |

---

## 7. Pytania domenowe wymagające decyzji

### Pytanie 1: Backward compatibility — jak obsłużyć stare `UiConfigurationDto.ColumnLayout`?

Czy w okresie przejściowym wspierać oba formaty:
- Gdy frontend wyśle `ColumnLayout` (stary), serwer sam rozdziela na group/item na podstawie `FieldScope`
- Gdy frontend wyśle `GroupColumnLayout` + `ItemColumnLayout` (nowy), używa ich wprost

**Proponowana odpowiedź:** Tak — dodać logikę w `BuildColumnLayoutOrderMap`, która jeśli `GroupColumnLayout` i `ItemColumnLayout` są null, a istnieje `ColumnLayout`, rozdziela go na podstawie `FieldScope` pól (pobierając scope z DB lub z listy FieldDefinitionDto).

### Pytanie 2: Czy walidacja kolejności GroupName/ItemSystemName ma być blokująca?

Czy jeśli user próbuje ustawić GroupName nie jako pierwsze pole w grupie:
- Rzucić `ValidationApiException` (blokujące)
- Tylko ostrzec (warning/log)
- Automatycznie wymusić (przesunąć na pierwsze miejsce)

**Proponowana odpowiedź:** Automatycznie wymusić — przy zapisie, `GroupName` zawsze ląduje na `Order = 0` w groupColumnLayout, a `ItemSystemName` na `Order = 0` w itemColumnLayout. User nie może ich przestawiać. To najbezpieczniejsze i wymaga najmniej zmian w UI.
