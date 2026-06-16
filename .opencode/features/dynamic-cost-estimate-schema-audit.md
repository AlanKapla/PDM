# Audyt obecnego systemu kosztorysów — Dynamic Schema Migration

**Data:** 2026-06-11  
**Feature:** Dynamiczne schematy kosztorysów (rezygnacja z globalnych szablonów)

---

## 1. Executive Summary

### Obecny model (Template-based)
System używa **globalnych szablonów** (`CostEstimateTemplate`), które definiują:
- Definicje pól dla grup (GroupFieldDefinitions)
- Definicje pól dla pozycji (SystemFields, CalculatedFields, GenericFields)
- Jednostki miary (Units)
- Kategorie (Categories)

Każdy kosztorys (`CostEstimate`) **musi** być utworzony z szablonu i jest na stałe powiązany z `TemplateId`.

### Nowy model (Dynamic Schema)
Każdy kosztorys ma **własny, wbudowany szablon**, który:
- Tworzy się automatycznie podczas zakładania kosztorysu
- Może być rozbudowywany przez użytkowników z dostępem do kosztorysu
- Eliminuje zależność od globalnych szablonów
- Umożliwia per-kosztorys customization kolumn

---

## 2. Obecna architektura encji (API/Database)

### 2.1 Hierarchia kosztorysu

```
CostEstimate (root)
├── TemplateId (FK → CostEstimateTemplate) ← TO DO USUNIĘCIA
├── AllGroups (ICollection<CostEstimateGroup>)
│   └── RootGroups (computed: ParentGroupId == null)
└── AllItems (ICollection<CostEstimateItem>)
    └── PopulateItemHierarchy() — rekurencyjna konstrukcja Options/Components
```

### 2.2 CostEstimateGroup (etap/podetap)

**Tabela:** `CostEstimateGroups`

| Pole | Typ | Opis |
|------|-----|------|
| `Id` | Guid | PK |
| `CostEstimateId` | Guid | FK → CostEstimate |
| `Name` | string | Nazwa etapu/podetapu |
| `ParentGroupId` | Guid? | FK → CostEstimateGroup (self-ref) |
| `Level` | int | Poziom zagnieżdżenia (0 = root) |
| `Order` | int | Kolejność w grupie |
| `TotalNet`, `TotalGross`, `TotalVat` | decimal? | Wartości obliczane |
| `FieldValues` | ICollection<CostEstimateGroupFieldValue> | Wartości pól nagłówka grupy |
| `Items` | ICollection<CostEstimateItem> | Pozycje w grupie |
| `ChildGroups` | ICollection<CostEstimateGroup> | Podgrupy |

**Obecne pole grupy:**
- `Name` — jedyne pole systemowe

**Pola dodatkowe** (obecnie z szablonu):
- `GroupDescription`, `GroupNumber`, `GroupStartDate`, `GroupEndDate`, `GroupStatus`, `GroupNotes`, `GroupResponsible`, `GroupBudget`, `GroupPriority`
- Wartości w `CostEstimateGroupFieldValue` → `FieldDefinitionId` → `CostEstimateTemplateGroupFieldDefinition`

---

### 2.3 CostEstimateItem (pozycja)

**Tabela:** `CostEstimateItems`

| Pole | Typ | Opis |
|------|-----|------|
| `Id` | Guid | PK |
| `CostEstimateId` | Guid | FK → CostEstimate |
| `GroupId` | Guid | FK → CostEstimateGroup |
| `Name` | string | Nazwa pozycji |
| `ParentItemId` | Guid? | FK → CostEstimateItem (self-ref) |
| `RelationType` | enum | None=0, Option=1, Component=2 |
| `Order` | int | Kolejność w grupie/parente |
| `NetValue`, `GrossValue`, `VatValue` | decimal? | Wartości obliczane |
| `FieldValues` | ICollection<CostEstimateItemFieldValue> | Wartości pól pozycji |
| `Options` | computed | Filtr z AllItems: `RelationType == Option` |
| `Components` | computed | Filtr z AllItems: `RelationType == Component` |

**Pola standardowe pozycji** (obecnie z szablonu):
- **System:** `Name`, `Quantity`, `Unit`, `Selected`, `Files`, `Category`, `IsWorkScope`
- **Calculated:** `UnitPriceNet`, `VatRate`, `UnitPriceGross`, `ValueNet`, `ValueGross`, `UnitVat`, `TotalVat`
- **Generic:** dowolne pola dodane przez usera (string, number, bool, date)

**Wartości** przechowywane w:
- `CostEstimateItemFieldValue` → `FieldDefinitionId` → `CostEstimateTemplateFieldDefinitionBase` (polimorficzne)

---

### 2.4 CostEstimateTemplate (szablon globalny) ← DO USUNIĘCIA

**Tabela:** `CostEstimateTemplates`

| Pole | Typ | Opis |
|------|-----|------|
| `Id` | Guid | PK |
| `OwnerId` | Guid | FK → User |
| `Name` | string | Nazwa szablonu |
| `Description` | string? | Opis |
| `Category` | string? | Kategoria (np. "Budowa") |
| `CanAddGroups` | bool | Czy można dodawać grupy |
| `CanBranchGroups` | bool | Czy można tworzyć podgrupy |
| `MaxGroupLevel` | int? | Max poziom zagnieżdżenia |
| `AutoNumberGroups` | bool | Czy auto-numerować grupy |
| `GroupNumberFormat` | string? | Format numeracji (np. "Etap {0}") |
| `Units` | ICollection<CostEstimateTemplateUnit> | Jednostki miary |
| `Categories` | ICollection<CostEstimateTemplateCategory> | Kategorie |
| `GroupFieldDefinitions` | ICollection<...> | Definicje pól grup |
| `SystemFieldDefinitions` | ICollection<...> | Definicje pól systemowych |
| `CalculatedFieldDefinitions` | ICollection<...> | Definicje pól obliczeniowych |
| `GenericFieldDefinitions` | ICollection<...> | Definicje pól generycznych |

**Problem:** Szablon jest współdzielony między wieloma kosztorysami. Zmiana szablonu wpływa na wszystkie kosztorysy z nim powiązane.

---

### 2.5 CostEstimateTemplateFieldDefinitionBase (definicje pól)

**Tabela:** `CostEstimateTemplateFieldDefinitions` (TPH — Table-Per-Hierarchy)

| Pole | Typ | Opis |
|------|-----|------|
| `Id` | Guid | PK |
| `TemplateId` | Guid | FK → CostEstimateTemplate |
| `FieldName` | Guid | Identyfikator pola (UI-generated) |
| `FieldScope` | enum | Group=0, ItemSystem=1, ItemCalculated=2, ItemGeneric=3 |
| `FieldType` | enum | GroupName=0, ItemSystemName=100, ItemCalculatedUnitPriceNet=200, ItemGenericString=301, etc. |
| `Label` | string | Etykieta w UI |
| `IsSortable` | bool | Czy można sortować |
| `IsFilterable` | bool | Czy można filtrować |
| `IsVisible` | bool | Czy widoczne |
| `IsReadonly` | bool | Czy tylko do odczytu |
| `ParentFieldId` | Guid? | FK → FieldDefinition (dla opcji zagnieżdżonych) |
| `Order` | int | Kolejność wyświetlania |
| `ChildFields` | ICollection<...> | Pola potomne (dla ItemSystemOptions) |

**Typy potomne (discriminator):**
- `CostEstimateTemplateGroupFieldDefinition`
- `CostEstimateTemplateItemSystemFieldDefinition`
- `CostEstimateTemplateItemCalculatedFieldDefinition`
- `CostEstimateTemplateItemGenericFieldDefinition`

**Unified FieldType enum** (z prefiksami):
- **Group:** `GroupName`, `GroupDescription`, `GroupNumber`, `GroupStartDate`, `GroupEndDate`, `GroupStatus`, `GroupNotes`, `GroupResponsible`, `GroupBudget`, `GroupPriority`
- **ItemSystem:** `ItemSystemName`, `ItemSystemQuantity`, `ItemSystemUnit`, `ItemSystemOptions`, `ItemSystemSelected`, `ItemSystemFiles`, `ItemSystemCategory`, `ItemSystemIsWorkScope`
- **ItemCalculated:** `ItemCalculatedUnitPriceNet`, `ItemCalculatedVatRate`, `ItemCalculatedUnitPriceGross`, `ItemCalculatedValueNet`, `ItemCalculatedValueGross`, `ItemCalculatedUnitVat`, `ItemCalculatedTotalVat`
- **ItemGeneric:** `ItemGenericNumber`, `ItemGenericString`, `ItemGenericBoolean`, `ItemGenericDate`, `ItemGenericDateTime`

---

### 2.6 CostEstimateFieldValueBase (wartości pól)

**Tabele:** `CostEstimateGroupFieldValues`, `CostEstimateItemFieldValues`

| Pole | Typ | Opis |
|------|-----|------|
| `Id` | Guid | PK |
| `FieldDefinitionId` | Guid | FK → CostEstimateTemplateFieldDefinitionBase |
| `StringValue` | string? | Dla pól tekstowych |
| `DecimalValue` | decimal? | Dla pól numerycznych |
| `BoolValue` | bool? | Dla pól logicznych |
| `DateTimeValue` | DateTime? | Dla pól daty/czasu |
| `Files` | ICollection<CostEstimateFieldFile> | Dla pól typu Files (tylko ItemSystemFiles) |

---

## 3. Obecne operacje CQRS

### 3.1 Komendy na kosztorysach

| Command | Co robi | Zależność od szablonu |
|---------|---------|----------------------|
| `CreateCostEstimateCommand` | Tworzy kosztorys z wybranego szablonu | ✅ Wymaga `TemplateId` |
| `UpdateCostEstimateCommand` | Aktualizuje nazwę, opis, status, rootGroups | ❌ Nie modyfikuje szablonu |
| `CopyCostEstimateCommand` | Kopiuje kosztorys (z tym samym szablonem) | ⚠️ Kopiuje `TemplateId` |
| `DeleteCostEstimateCommand` | Usuwa kosztorys | ❌ Nie wpływa na szablon |
| `RecalculateCostEstimateCommand` | Przelicza sumy | ❌ Tylko kalkulacje |
| `ShareCostEstimateCommand` | Udostępnia kosztorys użytkownikowi | ❌ Brak wpływu |
| `UpdateCostEstimateSharesCommand` | Aktualizuje listę udostępnień | ❌ Brak wpływu |

### 3.2 Komendy na grupach

| Command | Co robi | Zależność od szablonu |
|---------|---------|----------------------|
| `AddCostEstimateGroupCommand` | Dodaje grupę do kosztorysu | ❌ Tworzy grupę z domyślnymi wartościami |
| `DeleteCostEstimateGroupCommand` | Usuwa grupę | ❌ Brak wpływu |
| `ReorderCostEstimateGroupsCommand` | Zmienia kolejność grup | ❌ Brak wpływu |
| `UpsertCostEstimateGroupFieldCommand` | Aktualizuje wartość pola grupy | ✅ Wymaga `FieldDefinitionId` z szablonu |

### 3.3 Komendy na pozycjach

| Command | Co robi | Zależność od szablonu |
|---------|---------|----------------------|
| `AddCostEstimateItemCommand` | Dodaje pozycję/opcję/komponent | ❌ Tworzy z domyślnymi wartościami |
| `DeleteCostEstimateItemCommand` | Usuwa pozycję | ❌ Brak wpływu |
| `MoveCostEstimateItemCommand` | Przenosi pozycję do innej grupy | ❌ Brak wpływu |
| `ReorderCostEstimateItemsCommand` | Zmienia kolejność pozycji | ❌ Brak wpływu |
| `UpsertCostEstimateItemFieldCommand` | Aktualizuje wartość pola pozycji | ✅ Wymaga `FieldDefinitionId` z szablonu |
| `UploadCostEstimateFieldFilesCommand` | Upload plików do pola typu Files | ✅ Wymaga `FieldDefinitionId` z szablonu |

### 3.4 Queries

| Query | Co zwraca | Zależność od szablonu |
|-------|-----------|----------------------|
| `GetCostEstimatesQuery` | Lista kosztorysów (All/Mine/Shared) | ✅ Zwraca `TemplateId`, `TemplateName` |
| `GetCostEstimateDetailsQuery` | Pełna hierarchia kosztorysu | ✅ Zwraca `templateStructure` (definicje pól) |

---

## 4. Zależności w warstwie Business

### 4.1 Serwisy domenowe

| Serwis | Odpowiedzialność | Zależność od szablonu |
|--------|------------------|----------------------|
| `ICostEstimateTemplateService` | Zarządzanie szablonami | ✅ TO DO USUNIĘCIA lub przekształcenia |
| `ICostEstimateCalculationService` | Kalkulacje wartości netto/brutto/vat | ❌ Działa na FieldValues |
| `ICostEstimateCacheService` | Cache kosztorysów w Redis | ❌ Cache po TemplateId, wymaga refaktoru |
| `ICostEstimateAccessService` | Kontrola dostępu (Full/Restricted/ReadOnly) | ❌ Brak wpływu |
| `ICostEstimateShareService` | Udostępnianie kosztorysów | ❌ Brak wpływu |
| `ICostEstimateAIGeneratorService` | Generowanie kosztorysów przez AI | ⚠️ Używa szablonu jako bazy |

---

## 5. Zależności w warstwie UI

### 5.1 Komponenty React

| Komponent | Odpowiedzialność | Zależność od szablonu |
|-----------|------------------|----------------------|
| `CostEstimateTemplates.tsx` | Lista szablonów | ✅ TO DO USUNIĘCIA |
| `CostEstimateTemplateEditor.tsx` | Edytor szablonu | ✅ TO DO USUNIĘCIA |
| `CostEstimateTemplateSelector.tsx` | Wybór szablonu przy tworzeniu | ✅ TO DO PRZEKSZTAŁCENIA |
| `CreateCostEstimateModal.tsx` | Modal tworzenia kosztorysu | ✅ Wymaga wyboru szablonu |
| `CostEstimateEditPage.tsx` | Główna strona edycji kosztorysu | ⚠️ Używa `templateStructure` do renderowania pól |
| `CostEstimateTableView.tsx` | Widok tabelaryczny (desktop) | ⚠️ Używa `templateStructure` |
| `CostEstimateMobileView.tsx` | Widok mobilny (karty) | ⚠️ Używa `templateStructure` |
| `CostEstimateToolbar.tsx` | Toolbar z akcjami | ❌ Brak wpływu |
| `CostEstimateExcelView.tsx` | Eksport do Excel | ⚠️ Używa `templateStructure` |

### 5.2 Hooki React Query

| Hook | Co robi | Zależność od szablonu |
|------|---------|----------------------|
| `useCostEstimateTemplates` | Fetch szablonów | ✅ TO DO USUNIĘCIA |
| `useCostEstimate` | Fetch details kosztorysu | ⚠️ Zwraca `templateStructure` |
| `useGenerateCostEstimateWithAI` | AI generation | ⚠️ Używa szablonu |

### 5.3 Typy TypeScript

| Typ | Opis | Zależność od szablonu |
|-----|------|----------------------|
| `CostEstimateTemplate` | Typ szablonu | ✅ TO DO USUNIĘCIA |
| `CostEstimateTemplateStructureWeb` | Struktura szablonu (systemFields, calculatedFields, genericFields, groupFields) | ⚠️ TO DO PRZEKSZTAŁCENIA na `CostEstimateSchemaWeb` |
| `FieldDefinitionWeb` | Definicja pola | ⚠️ Obecnie wskazuje na szablon |
| `CostEstimateDetailsWeb` | Response z API | ⚠️ Zawiera `templateId`, `templateName`, `templateStructure` |

---

## 6. Kluczowe znaleziska (Findings)

### 6.1 ✅ CO DZIAŁA DOBRZE

1. **Unified FieldType enum** — wszystkie typy pól w jednym enumie z prefiksami (Group, ItemSystem, ItemCalculated, ItemGeneric)
2. **Typowane wartości** — `StringValue`, `DecimalValue`, `BoolValue`, `DateTimeValue` zamiast jednego pola tekstowego
3. **Polimorficzne definicje** — `CostEstimateTemplateFieldDefinitionBase` z TPH
4. **Hierarchia Item** — Options i Components jako zagnieżdżone Items z `RelationType`
5. **Rekurencja Group** — `ParentGroupId` pozwala na dowolne zagnieżdżenie

### 6.2 ⚠️ CO WYMAGA ZMIANY

1. **Zależność od szablonu** — każdy kosztorys ma `TemplateId`, zmiana szablonu wpływa na wszystkie kosztorysy
2. **Brak per-kosztorys customization** — użytkownik nie może dodawać/ukrywać pól w konkretnym kosztorysie
3. **FieldDefinitionId** — wartości pól wskazują na definicje w szablonie globalnym
4. **Brak wersjonowania** — jeśli szablon się zmieni, stare kosztorysy mogą stracić spójność
5. **UI założenia** — frontend zakłada że `templateStructure` pochodzi z zewnętrznego szablonu

### 6.3 🔴 BLOKERY

1. **Migracja danych** — istniejące kosztorysy mają `TemplateId`, trzeba przenieść definicje pól do każdego kosztorysu
2. **FK constraints** — `CostEstimate.TemplateId`, `CostEstimateFieldValueBase.FieldDefinitionId`
3. **Cache** — `ICostEstimateCacheService` cachuje po `TemplateId`
4. **AI Generator** — tworzy kosztorysy na bazie szablonu

---

## 7. Mapa zależności (Dependency Map)

```
┌──────────────────────────────────────────────────────────────┐
│                     CostEstimateTemplate                      │
│         (TO DO USUNIĘCIA LUB PRZEKSZTAŁCENIA)                 │
└───────────────────────────┬──────────────────────────────────┘
                            │
                            │ FK: TemplateId
                            ▼
                 ┌──────────────────────┐
                 │   CostEstimate       │
                 └──────────┬───────────┘
                            │
           ┌────────────────┼────────────────┐
           │                │                │
           ▼                ▼                ▼
  ┌────────────────┐ ┌────────────┐ ┌────────────────┐
  │ CostEstimate   │ │ CostEstim  │ │ WorkSchedule   │
  │ Group          │ │ ateItem    │ │                │
  └───────┬────────┘ └─────┬──────┘ └────────────────┘
          │                │
          │                │
          ▼                ▼
  ┌──────────────┐  ┌────────────────┐
  │ GroupField   │  │ ItemField      │
  │ Value        │  │ Value          │
  └──────┬───────┘  └────┬───────────┘
         │               │
         │ FK: FieldDefinitionId
         │               │
         └───────┬───────┘
                 ▼
  ┌──────────────────────────────────┐
  │ CostEstimateTemplate             │
  │ FieldDefinitionBase              │
  │ (SystemFields, CalculatedFields, │
  │  GenericFields, GroupFields)     │
  └──────────────────────────────────┘
```

---

## 8. Wymagania nowego modelu

### 8.1 Encje

#### CostEstimate (zmieniony)
- ~~`TemplateId`~~ — **USUNIĘTE**
- `OwnerId` — zachowane
- `Name`, `Description`, `Status` — zachowane
- **NOWE:** `SchemaVersion` (int) — wersja schematu (dla przyszłych migracji)

#### CostEstimateFieldSchema (NOWA encja) ← zastępuje Template
- `Id` (Guid, PK)
- `CostEstimateId` (Guid, FK → CostEstimate) — **jeden do jednego**
- `FieldDefinitions` (ICollection<CostEstimateFieldDefinition>) — definicje pól per kosztorys
- `CreatedAt`, `UpdatedAt`

#### CostEstimateFieldDefinition (NOWA encja) ← zastępuje TemplateFieldDefinitionBase
- `Id` (Guid, PK)
- `SchemaId` (Guid, FK → CostEstimateFieldSchema)
- `FieldName` (Guid) — zachowane
- `FieldScope` (enum) — zachowane
- `FieldType` (enum) — zachowane
- `Label` (string) — zachowane
- `IsSortable`, `IsFilterable`, `IsVisible`, `IsReadonly` — zachowane
- `ParentFieldId` (Guid?) — zachowane
- `Order` (int) — zachowane
- `IsUserDefined` (bool) — **NOWE** — czy pole dodane przez usera (true) czy systemowe (false)
- `CanRename` (bool) — **NOWE** — czy user może zmienić Label (true dla systemowych i user-defined)
- `CanDelete` (bool) — **NOWE** — czy user może usunąć pole (true tylko dla user-defined)

#### CostEstimateGroupFieldValue, CostEstimateItemFieldValue (zmienione)
- ~~`FieldDefinitionId` → CostEstimateTemplateFieldDefinitionBase~~
- **NOWE:** `FieldDefinitionId` → CostEstimateFieldDefinition (per-kosztorys)

### 8.2 Pola standardowe (default schema)

#### Grupy (etapy)
- **Obligatoryjne:** `GroupName` (zawsze widoczne, nie można usunąć)

#### Pozycje
- **Obligatoryjne (zawsze):**
  - `ItemSystemName` — Nazwa
  - `ItemSystemQuantity` — Ilość
  - `ItemSystemUnit` — Jednostka
  - `ItemCalculatedUnitPriceNet` — Cena netto
  - `ItemCalculatedVatRate` — VAT
  - `ItemCalculatedUnitPriceGross` — Cena brutto
  - `ItemCalculatedValueNet` — Wartość netto
  - `ItemCalculatedValueGross` — Wartość brutto
  - `ItemCalculatedTotalVat` — Wartość VAT
  
- **Opcjonalne (zawsze dostępne, można ukryć):**
  - `ItemSystemOptions` — opcje/warianty
  - `ItemSystemSelected` — radio button dla opcji
  - `ItemSystemFiles` — załączniki
  - `ItemSystemCategory` — kategoria
  - `ItemSystemIsWorkScope` — synchronizacja z harmonogramem

- **User-defined:**
  - `ItemGenericString` — pole tekstowe (user może dodać)
  - `ItemGenericNumber` — pole numeryczne
  - `ItemGenericBoolean` — pole checkbox
  - `ItemGenericDate` — pole daty
  - `ItemGenericDateTime` — pole daty i czasu

### 8.3 Operacje na schemacie

#### Zarządzanie kolumnami (UI)
1. **Ukryj/odkryj** — zmiana `IsVisible` na `CostEstimateFieldDefinition`
2. **Zmień nazwę** — zmiana `Label` (dla pól z `CanRename == true`)
3. **Dodaj kolumnę** — dodanie nowej `CostEstimateFieldDefinition` z `IsUserDefined == true`
4. **Usuń kolumnę** — soft delete definicji (tylko dla `IsUserDefined == true && CanDelete == true`)
5. **Zmień kolejność** — zmiana `Order`

#### Dodawanie opcji (by default)
- Każda pozycja może mieć opcje (`ItemSystemOptions` jest zawsze w schemacie, można ukryć)
- Opcje można dodawać dla:
  - Pozycji bez komponentów
  - Komponentów (opcje dla komponentu)
- Zaznaczenie opcji (radio button) → `ItemSystemSelected` kopiuje wartości do pozycji nadrzędnej

---

## 9. Plan migracji danych

### 9.1 Strategia

1. **Krok 1: Dodanie nowych encji** (bez usuwania starych)
   - `CostEstimateFieldSchema`
   - `CostEstimateFieldDefinition`
   - Relacje: `CostEstimate` → `Schema` (1:1, nullable na początek)

2. **Krok 2: Migracja danych** (SQL script lub background job)
   - Dla każdego `CostEstimate`:
     - Pobierz `Template.FieldDefinitions`
     - Skopiuj do nowego `CostEstimateFieldSchema`
     - Utwórz `CostEstimateFieldDefinition` dla każdej definicji z szablonu
     - Zaktualizuj `CostEstimateFieldValueBase.FieldDefinitionId` do nowych definicji

3. **Krok 3: Refaktor kodu** (backend + frontend)
   - Zmiana `GetCostEstimateDetailsQuery` — zwraca `schema` zamiast `templateStructure`
   - Nowe komendy: `AddFieldDefinitionCommand`, `UpdateFieldDefinitionCommand`, `DeleteFieldDefinitionCommand`, `ReorderFieldDefinitionsCommand`
   - UI: zmiana `templateStructure` na `schema`

4. **Krok 4: Usunięcie starych encji** (po weryfikacji)
   - Usunięcie `CostEstimate.TemplateId`
   - Usunięcie tabel szablonów (lub pozostawienie jako "legacy templates" dla backward compatibility)

### 9.2 SQL Pseudo-script

```sql
-- Krok 1: Dodaj nową tabelę CostEstimateFieldSchemas
CREATE TABLE CostEstimateFieldSchemas (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CostEstimateId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL,
    FOREIGN KEY (CostEstimateId) REFERENCES CostEstimates(Id) ON DELETE CASCADE
);

-- Krok 2: Dodaj nową tabelę CostEstimateFieldDefinitions
CREATE TABLE CostEstimateFieldDefinitions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    SchemaId UNIQUEIDENTIFIER NOT NULL,
    FieldName UNIQUEIDENTIFIER NOT NULL,
    FieldScope INT NOT NULL,
    FieldType INT NOT NULL,
    Label NVARCHAR(200) NOT NULL,
    IsSortable BIT NOT NULL,
    IsFilterable BIT NOT NULL,
    IsVisible BIT NOT NULL,
    IsReadonly BIT NOT NULL,
    ParentFieldId UNIQUEIDENTIFIER NULL,
    [Order] INT NOT NULL,
    IsUserDefined BIT NOT NULL DEFAULT 0,
    CanRename BIT NOT NULL DEFAULT 0,
    CanDelete BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (SchemaId) REFERENCES CostEstimateFieldSchemas(Id) ON DELETE CASCADE,
    FOREIGN KEY (ParentFieldId) REFERENCES CostEstimateFieldDefinitions(Id)
);

-- Krok 3: Migracja danych (dla każdego kosztorysu)
-- Pseudo-kod (wymaga procedury T-SQL lub .NET background job)

FOR EACH CostEstimate ce:
    1. CREATE new CostEstimateFieldSchema (schema)
    2. SET schema.CostEstimateId = ce.Id
    3. FOR EACH FieldDefinition fd IN ce.Template.FieldDefinitions:
         - CREATE new CostEstimateFieldDefinition (newFd)
         - COPY all properties from fd to newFd
         - SET newFd.SchemaId = schema.Id
         - SET newFd.IsUserDefined = false (bo pochodzi z szablonu)
         - SET newFd.CanRename = true (pola systemowe można przemianować)
         - SET newFd.CanDelete = false (pola z szablonu nie można usunąć)
    4. FOR EACH FieldValue fv IN ce.AllGroups.FieldValues + ce.AllItems.FieldValues:
         - MAP fv.FieldDefinitionId from old template definition to new schema definition

-- Krok 4 (po weryfikacji): Usuń kolumnę TemplateId
ALTER TABLE CostEstimates DROP CONSTRAINT FK_CostEstimates_Templates;
ALTER TABLE CostEstimates DROP COLUMN TemplateId;
```

---

## 10. Nowa architektura API

### 10.1 Nowe endpointy

#### Zarządzanie schematem kosztorysu

```http
# Get schema kosztorysu (embedded w details)
GET /api/tenants/{tenantId}/projects/{projectId}/cost-estimate/details/{estimateId}
→ Response: CostEstimateDetailsWeb { schema: CostEstimateSchemaWeb }

# Dodaj pole do schematu
POST /api/tenants/{tenantId}/projects/{projectId}/cost-estimate/{estimateId}/schema/fields
Body: { fieldScope, fieldType, label, parentFieldId?, order }
→ Response: Guid (fieldDefinitionId)

# Aktualizuj pole (label, visibility, order)
PUT /api/tenants/{tenantId}/projects/{projectId}/cost-estimate/{estimateId}/schema/fields/{fieldId}
Body: { label?, isVisible?, order? }
→ Response: 204 No Content

# Usuń pole (tylko user-defined)
DELETE /api/tenants/{tenantId}/projects/{projectId}/cost-estimate/{estimateId}/schema/fields/{fieldId}
→ Response: 204 No Content

# Zmień kolejność pól
POST /api/tenants/{tenantId}/projects/{projectId}/cost-estimate/{estimateId}/schema/fields/reorder
Body: { fieldIds: Guid[] }
→ Response: 204 No Content
```

### 10.2 Zmienione endpointy

```http
# Create (bez templateId, schemat generowany automatycznie)
POST /api/tenants/{tenantId}/projects/{projectId}/cost-estimate
Body: { name, description }  ← NIE MA templateId
→ Response: Guid (estimateId)

# Copy (kopiuje też schemat)
POST /api/tenants/{tenantId}/projects/{projectId}/cost-estimate/{estimateId}/copy
Body: { name }
→ Backend kopiuje CostEstimateFieldSchema wraz z definicjami
```

---

## 11. Zmiany w UI

### 11.1 Usunięte/zmienione komponenty

| Komponent (old) | Akcja | Komponent (new) |
|-----------------|-------|-----------------|
| `CostEstimateTemplates.tsx` | ❌ USUNIĘTY | — |
| `CostEstimateTemplateEditor.tsx` | ❌ USUNIĘTY | — |
| `CostEstimateTemplateSelector.tsx` | ⚠️ ZMIENIONY | `CreateCostEstimateModal.tsx` (bez wyboru szablonu) |

### 11.2 Nowe komponenty

| Komponent | Odpowiedzialność |
|-----------|------------------|
| `CostEstimateSchemaManager.tsx` | Zarządzanie kolumnami (ukryj/odkryj, dodaj, usuń, zmień nazwę) |
| `AddFieldModal.tsx` | Modal dodawania nowego pola (fieldType, label, scope) |
| `FieldVisibilityPopover.tsx` | Popover z listą pól (checkbox visibility) |

### 11.3 Zmienione typy

```typescript
// OLD
interface CostEstimateDetailsWeb {
  templateId: string;
  templateName: string;
  templateStructure: CostEstimateTemplateStructureWeb;
  // ...
}

// NEW
interface CostEstimateDetailsWeb {
  // templateId, templateName — USUNIĘTE
  schema: CostEstimateSchemaWeb;  // ← NOWE
  // ...
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

## 12. Backwards Compatibility

### 12.1 Opcja A: Hard Migration (rekomendowane)

- Wszystkie kosztorysy migrowane do nowego modelu
- Szablony zachowane jako "legacy" (read-only, nie można tworzyć nowych kosztorysów z szablonu)
- Frontend wykrywa wersję API i wyświetla odpowiedni UI

### 12.2 Opcja B: Soft Migration (phase-out)

- Stare kosztorysy (z `TemplateId != null`) działają jak dotychczas
- Nowe kosztorysy (z `SchemaId != null`) działają na nowym modelu
- UI wykrywa typ kosztorysu i renderuje odpowiedni widok
- Po migracji wszystkich kosztorysów — usunięcie starego kodu

**Rekomendacja:** **Opcja A** — czysty cut-over, łatwiejsze utrzymanie kodu.

---

## 13. Timeline & Effort Estimate

| Etap | Opis | Czas (dev days) |
|------|------|----------------|
| **1. Design & Review** | Finalizacja architektury, review z team | 2 |
| **2. Backend — Encje** | Dodanie `CostEstimateFieldSchema`, `CostEstimateFieldDefinition` | 2 |
| **3. Backend — Migracja** | SQL script + background job do migracji danych | 3 |
| **4. Backend — CQRS** | Nowe Commands/Queries dla schematu | 3 |
| **5. Backend — Refactor** | Aktualizacja `GetCostEstimateDetailsQuery`, usunięcie `TemplateId` | 3 |
| **6. Backend — Tests** | Unit tests dla nowych handlerów | 2 |
| **7. Frontend — Types** | Aktualizacja typów TypeScript | 1 |
| **8. Frontend — API Client** | Nowe funkcje API dla schematu | 1 |
| **9. Frontend — Schema Manager** | Komponenty zarządzania kolumnami | 4 |
| **10. Frontend — Refactor** | Aktualizacja `CostEstimateEditPage`, usunięcie template selector | 4 |
| **11. Frontend — Tests** | Unit tests dla nowych komponentów | 2 |
| **12. Integration Testing** | E2E testy migracji i nowego flow | 3 |
| **13. Documentation** | Aktualizacja dokumentacji API i user guide | 2 |
| **14. Deployment** | Deploy na dev/ppd/prd + monitoring | 2 |

**Total:** ~34 dev days (~7 tygodni dla 1 dev, ~3.5 tygodnia dla 2 devs)

---

## 14. Ryzyka i mitygacje

| Ryzyko | Prawdopodobieństwo | Impact | Mitygacja |
|--------|-------------------|--------|-----------|
| Utrata danych podczas migracji | Średnie | Krytyczny | Backup bazy przed migracją, dry-run na dev/ppd |
| Problemy z wydajnością (więcej rekordów w tabeli definicji) | Niskie | Średni | Indeksy na `SchemaId`, `FieldName`, cache w Redis |
| Breaking changes dla UI | Wysokie | Wysoki | Feature flag, A/B testing, stopniowe rollout |
| Niezgodność API dla zewnętrznych integracji | Niskie | Średni | Wersjonowanie API (`/api/v2/...`), deprecation notice |
| User confusion (nowy UX) | Średnie | Średni | User guide, tooltips, onboarding modal |

---

## 15. Rekomendacje

### 15.1 Must Have
1. ✅ **Hard migration** — czysty cut-over, łatwiejsze utrzymanie
2. ✅ **Backup i rollback plan** — przed migracją na produkcji
3. ✅ **Feature flag** — możliwość wyłączenia nowego UI w razie problemów
4. ✅ **Unit tests** — pokrycie nowych handlerów i komponentów
5. ✅ **Dokumentacja** — API docs + user guide

### 15.2 Nice to Have
1. ⚠️ **Szablon startowy** — możliwość stworzenia kosztorysu z "szablonem startowym" (preset pól)
2. ⚠️ **Import/export schematu** — zapisz schemat jako JSON, załaduj w innym kosztorysie
3. ⚠️ **History schematu** — wersjonowanie zmian w schemacie (audit log)

---

## 16. Pytania do decision makers

1. **Czy zachować stare szablony jako read-only** lub usunąć całkowicie?
2. **Czy chcemy preset schematów** (np. "Budowa", "Remont") jako starting point?
3. **Czy migracja ma być automatyczna** (podczas deployu) czy manualna (background job uruchamiany przez admina)?
4. **Czy potrzebujemy wersjonowania API** (`/api/v2/cost-estimate`) czy nadpisujemy obecne endpointy?
5. **Czy pozwalamy na import schematu** z innego kosztorysu (copy schema)?

---

## 17. Next Steps

1. ✅ **Review tego audytu** z zespołem
2. ⏳ **Decyzja:** Hard vs Soft migration
3. ⏳ **Stworzenie feature spec** (detale implementacji)
4. ⏳ **Prototyp UI** (Figma/screenshot mockup nowego widoku zarządzania kolumnami)
5. ⏳ **Implementacja backend** (encje + migracja)
6. ⏳ **Implementacja frontend** (schema manager + refactor)
7. ⏳ **Testing + Deployment**

---

**Koniec raportu audytu**
