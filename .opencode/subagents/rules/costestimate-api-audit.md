# Audyt API — Moduł CostEstimate (Kosztorysy)

## 1. Stan obecny — Encje (Entities)

### Podsumowanie struktury danych

Moduł kosztorysów posiada **9 encji** w warstwie `Entities.Models.CostEstimates` oraz **7 konfiguracji EF Core**. Brakuje encji dla sekcji (CostEstimateSection), pozycji jako osobnej encji (pozycje są reprezentowane przez CostEstimateItem), komponentów jako osobnej encji (komponenty to też CostEstimateItem z RelationType=Component), oraz opcji jako osobnej encji (options to też CostEstimateItem). Wszystkie te koncepty są zrealizowane przez hierarchię w obrębie jednej encji `CostEstimateItem` z polem `RelationType`.

### 1.1 CostEstimate (`Entities\Models\CostEstimates\CostEstimate.cs`)
- **Opis**: Główna encja kosztorysu. Dziedziczy po `DeletableEntity` (soft delete).
- **Kluczowe właściwości**: `TenantId`, `ProjectId`, `OwnerId`, `Name`, `Description`, `Status` (enum Draft/InProgress/ReadyForReview/Approved/Rejected/Archived), `TotalNet`, `TotalGross`, `TotalVat`, `LastCalculatedAt`, `SchemaVersion`
- **Nawigacje**: 
  - `AllGroups` → `ICollection<CostEstimateGroup>` (wszystkie grupy)
  - `AllItems` → `ICollection<CostEstimateItem>` (wszystkie pozycje)
  - `Schema` → `CostEstimateFieldSchema` (1:1, schemat pól)
  - `WorkSchedules` → powiązane harmonogramy
  - `RootGroups` → właściwość wyliczana (gdzie `ParentGroupId == null`)

### 1.2 CostEstimateGroup (`Entities\Models\CostEstimates\CostEstimateGroup.cs`)
- **Opis**: Grupa/etap kosztorysu. Hierarchiczna (parent-child). Dziedziczy po `DeletableEntity`.
- **Kluczowe właściwości**: `CostEstimateId`, `Name`, `ParentGroupId`, `Level`, `Order`, `TotalNet`, `TotalGross`, `TotalVat`
- **Nawigacje**: `ParentGroup`, `ChildGroups`, `FieldValues` → `ICollection<CostEstimateGroupFieldValue>`, `Items` → `ICollection<CostEstimateItem>`

### 1.3 CostEstimateItem (`Entities\Models\CostEstimates\CostEstimateItem.cs`)
- **Opis**: Pozycja kosztorysu (work scope item). Może być pozycją główną (RelationType=None), opcją (RelationType=Option) lub komponentem (RelationType=Component). Dziedziczy po `DeletableEntity`.
- **Kluczowe właściwości**: `CostEstimateId`, `Name`, `GroupId`, `ParentItemId`, `RelationType` (enum: None/Option/Component), `Order`, `NetValue`, `GrossValue`, `VatValue`
- **Nawigacje**: `FieldValues` → `ICollection<CostEstimateItemFieldValue>`, `ParentItem`, `Options` (filtrowane), `Components` (filtrowane — ignorowane przez EF)
- **Metody**: `SetChildItems(IEnumerable<CostEstimateItem>)` — ustawia kolekcję _childItems

### 1.4 CostEstimateFieldSchema (`Entities\Models\CostEstimates\CostEstimateFieldSchema.cs`)
- **Opis**: Schemat pól kosztorysu (per kosztorys, 1:1 z CostEstimate). Zastępuje zależność od globalnego szablonu.
- **Kluczowe właściwości**: `CostEstimateId`, `CreatedAt`, `UpdatedAt`
- **Nawigacje**: `FieldDefinitions` → `ICollection<CostEstimateFieldDefinition>`

### 1.5 CostEstimateFieldDefinition (`Entities\Models\CostEstimates\CostEstimateFieldDefinition.cs`)
- **Opis**: Definicja pola (kolumny) w schemacie kosztorysu. Każdy kosztorys ma własne definicje.
- **Kluczowe właściwości**: `SchemaId`, `FieldName` (Guid), `FieldScope` (Group/ItemSystem/ItemCalculated/ItemGeneric), `FieldType` (unified enum), `Label`, `IsSortable`, `IsFilterable`, `IsVisible`, `IsReadonly`, `ParentFieldId`, `Order`, `IsUserDefined`, `CanRename`, `CanDelete`
- **Nawigacje**: `Schema`, `ParentField`, `ChildFields` (self-referencing)

### 1.6 CostEstimateFieldValueBase (`Entities\Models\CostEstimates\CostEstimateFieldValueBase.cs`)
- **Opis**: Bazowa klasa abstrakcyjna dla wartości pól. Zawiera 4 typowane właściwości: `StringValue`, `DecimalValue`, `BoolValue`, `DateTimeValue`.
- **Kluczowe właściwości**: `CreatedAt`, `UpdatedAt`

### 1.7 CostEstimateItemFieldValue (`Entities\Models\CostEstimates\CostEstimateItemFieldValue.cs`)
- **Opis**: Wartość pola na pozycji kosztorysu. Dziedziczy po `CostEstimateFieldValueBase`.
- **Właściwości**: `ItemId`, `FieldDefinitionId`
- **Nawigacje**: `Item`, `FieldDefinition`, `Files` → `ICollection<CostEstimateFieldFile>`

### 1.8 CostEstimateGroupFieldValue (`Entities\Models\CostEstimates\CostEstimateGroupFieldValue.cs`)
- **Opis**: Wartość pola nagłówka grupy. Dziedziczy po `CostEstimateFieldValueBase`.
- **Właściwości**: `GroupId`, `FieldDefinitionId`
- **Nawigacje**: `Group`, `FieldDefinition`

### 1.9 CostEstimateFieldFile (`Entities\Models\CostEstimates\CostEstimateFieldFile.cs`)
- **Opis**: Plik dołączony do pola typu ItemSystemFiles. Dziedziczy po `DeletableEntity`.
- **Właściwości**: `FieldValueId`, `CostEstimateId` (denormalizacja), `OriginalFileName`, `BlobName`, `ContentType`, `FileSize`, `Order`, `CreatedByUserId`
- **Nawigacje**: `FieldValue`, `CostEstimate`, `CreatedByUser`

### 1.10 SharedCostEstimate (`Entities\Models\CostEstimates\SharedCostEstimate.cs`)
- **Opis**: Rekord udostępnienia kosztorysu użytkownikowi.
- **Właściwości**: `TenantId`, `ProjectId`, `CostEstimateId`, `SharedByUserId`, `SharedWithUserId`, `SharedAt`
- **Nawigacje**: `CostEstimate`, `SharedByUser`, `SharedWithUser`, `SharedByTenantMember`, `SharedWithTenantMember`, `SharedByProjectMember`, `SharedWithProjectMember`

### 1.11 CostEstimateEnums (`Entities\Models\CostEstimates\CostEstimateEnums.cs`)
- **`FieldScope`**: Group=0, ItemSystem=1, ItemCalculated=2, ItemGeneric=3
- **`FieldType`**: 31 wartości z zakresami: Group (0-9), ItemSystem (100-107), ItemCalculated (200-206), ItemGeneric (300-304)
- **`CostEstimateStatus`**: Draft=0, InProgress=1, ReadyForReview=2, Approved=3, Rejected=4, Archived=5

## 2. Konfiguracje EF Core

| Plik | Encja | Kluczowe ustawienia |
|------|-------|---------------------|
| `CostEstimateConfiguration.cs` | CostEstimate | HasQueryFilter(!IsDeleted), indexes, 1:1 Schema (Cascade), precision 18,2 |
| `CostEstimateGroupConfiguration.cs` | CostEstimateGroup | Self-referencing hierarchy, Cascade delete FieldValues+Items, unique (GroupId,FieldDefinitionId) |
| `CostEstimateItemConfiguration.cs` | CostEstimateItem | Ignore Options/Components, self-referencing ParentItem, RelationType as string |
| `CostEstimateItemFieldValueConfiguration.cs` | CostEstimateItemFieldValue | Unique (ItemId, FieldDefinitionId), precision 18,6 |
| `CostEstimateGroupFieldValueConfiguration.cs` | CostEstimateGroupFieldValue | Unique (GroupId, FieldDefinitionId), precision 18,6 |
| `CostEstimateFieldSchemaConfiguration.cs` | CostEstimateFieldSchema | Unique CostEstimateId index (1:1), Cascade FieldDefinitions |
| `CostEstimateFieldDefinitionConfiguration.cs` | CostEstimateFieldDefinition | Self-referencing ParentField, enums as int |
| `CostEstimateFieldFileConfiguration.cs` | CostEstimateFieldFile | HasQueryFilter(!IsDeleted), Cascade FieldValue |
| `SharedCostEstimateConfiguration.cs` | SharedCostEstimate | Unique (CostEstimateId, SharedWithUserId), composite FKs |

## 3. Serwisy (Business) — Interfejsy i Implementacje

### 3.1 ICostEstimateCalculationService
- **Plik**: `Business\Interfaces\Services\ICostEstimateCalculationService.cs`
- **Implementacja**: `Business\Implementation\Services\CostEstimateCalculationService.cs`
- **Metody**: 
  - `RecalculateCostEstimate(CostEstimate)` — przelicza TotalNet/TotalGross/TotalVat dla całego kosztorysu (itemy → grupy → CE)
- **Szczegóły**: Oblicza wartości pól kalkulowanych (UnitPriceGross, ValueNet, ValueGross, UnitVat, TotalVat) na podstawie Quantity, UnitPriceNet i VatRate

### 3.2 ICostEstimateCacheService
- **Plik**: `Business\Interfaces\Services\ICostEstimateCacheService.cs`
- **Implementacja**: `Business\Implementation\Services\CostEstimateCacheService.cs`
- **Metody**: 
  - `GetCostEstimateAsync(id, tenantId, projectId)` — z cache lub DB z Includes
  - `GetGroupsDictionaryAsync` — słownik grup per CE
  - `GetItemsDictionaryAsync` — słownik itemów per CE
  - `GetGroupFieldValuesDictionaryAsync` — słownik wartości pól grup
  - `GetItemFieldValuesDictionaryAsync` — słownik wartości pól itemów
  - `InvalidateCostEstimateAsync` — czyści wszystkie cache dla CE
  - `InvalidateGroupsAsync`, `InvalidateItemsAsync`, `InvalidateGroupFieldValuesAsync`, `InvalidateItemFieldValuesAsync`

### 3.3 ICostEstimateAccessService
- **Plik**: `Business\Interfaces\Services\ICostEstimateAccessService.cs`
- **Implementacja**: `Business\Implementation\Services\CostEstimateAccessService.cs`
- **Metody**:
  - `GetAccessibleCostEstimateIdsAsync(currentUser, tenantId, projectId, scope)` — zwraca HashSet<Guid> dla All/Mine/Shared
  - `GetAccessLevelAsync(currentUser, tenantId, projectId, costEstimateId)` — zwraca enum (None/ReadOnly/Restricted/Full)
  - `GetSharedWithUserIdsAsync(tenantId, projectId, costEstimateId)` — lista userów z dostępem
  - `InvalidateAccessCacheAsync`, `InvalidateCostEstimateAccessCacheAsync`

### 3.4 ICostEstimateShareService
- **Plik**: `Business\Interfaces\Services\ICostEstimateShareService.cs`
- **Implementacja**: `Business\Implementation\Services\CostEstimateShareService.cs`
- **Metody**:
  - `ValidateOwnerOrAdminAsync(costEstimate, ct)` — rzuca ForbiddenApiException gdy nie owner/admin
  - `InvalidateAccessCacheAsync(costEstimateId, projectId, tenantId, ct)` — czyści access cache

### 3.5 ICostEstimateAIGeneratorService
- **Plik**: `Business\Interfaces\Services\ICostEstimateAIGeneratorService.cs`
- **Implementacja**: `Business\Implementation\Services\AI\CostEstimateAIGeneratorService.cs`
- **Metody**:
  - `GeneratePreviewAsync(request, ct)` — generuje podgląd kosztorysu przez Azure OpenAI (nie zapisuje do DB)

### 3.6 CostEstimateCacheKeys
- **Plik**: `Business\Implementation\CacheKeys\CostEstimateCacheKeys.cs`
- **Statyczne metody**: `CostEstimate()`, `Groups()`, `Items()`, `GroupFieldValues()`, `ItemFieldValues()` — format `ce:{tenantId}:{projectId}:{id}`
- **TTL**: 30 minut

### 3.7 FieldValueConverter
- **Plik**: `Business\Implementation\Helpers\FieldValueConverter.cs`
- **Statyczne metody**: `SetTypedValue()` i `GetTypedValue()` — konwersja między typowanymi polami a FieldType enum

### 3.8 CostEstimateAccessLevel (stałe)
- **Plik**: `Business\Interfaces\Constants\CostEstimateAccessLevel.cs`
- **Wartości**: None=0, ReadOnly=1, Restricted=2, Full=3

## 4. Web Modele (DTO)

### Warstwa `Business.Interfaces.WebModels.CostEstimates`

| Plik | Typ | Opis |
|------|-----|------|
| `CostEstimateDetailsWeb.cs` | record | Szczegóły kosztorysu: Id, TenantId, ProjectId, Name, Status, RootGroups, TotalNet/Gross/Vat, Schema, AccessLevel, SharedWithUsers |
| `CostEstimateDataWeb.cs` | 3 recordy | `CostEstimateFieldFileWeb` (plik), `CostEstimateFieldValueWeb` (wartość pola z typowanymi wartościami), `CostEstimateItemWeb` (pozycja z Options/Components), `CostEstimateGroupWeb` (grupa z ChildGroups) |
| `CostEstimateListItemWeb.cs` | record | Lista kosztorysów: Id, Name, Status, TotalNet/Gross/Vat, Owner, SharedWithUsers, Currency |
| `CostEstimateSchemaWeb.cs` | record | Schemat: Id, CostEstimateId, FieldDefinitions |
| `CostEstimateFieldDefinitionWeb.cs` | record | Definicja pola: Id, FieldName, FieldScope, FieldType, Label, IsVisible, IsReadonly, etc. |
| `CostEstimateShareWeb.cs` | 3 recordy | `ShareCostEstimateRequestWeb`, `UpdateCostEstimateSharesRequestWeb`, `CostEstimateShareWeb` (UserId, FullName, Email, SharedAt) |
| `CostEstimateMutationDto.cs` | 3 recordy | `CostEstimateFieldValueDto` (FieldDefinitionId + wartości), `CostEstimateItemDto` (z Options/Components), `CostEstimateGroupDto` (z Itemami/ChildGroups) |
| `CostEstimateOperationResultWeb.cs` | 2 recordy | `ReorderGroupDto`, `ReorderItemDto` |

### Warstwa `Business.Interfaces.WebModels.AI` (AI Cost Estimate)

| Plik | Typy | Opis |
|------|------|------|
| `AICostEstimateRequestWeb.cs` | record | Wejście: TemplateId, InvestmentType, FinishingStandard, Budget, Area, Location, AdditionalRequirements |
| `AICostEstimatePreviewWeb.cs` | 5 recordów | `AICostEstimatePreviewWeb` (SuggestedName, Groups, Warnings), `AIGroupPreviewWeb`, `AIItemPreviewWeb`, `AIComponentPreviewWeb`, `AIFieldValueWeb` |
| `CreateCostEstimateFromAIPreviewWeb.cs` | record | Zapis kosztorysu z AI: Name, Description, Preview |

### Warstwa `Business.Interfaces.WebModels.CostTrackers`

| Plik | Opis |
|------|------|
| `CostEstimateSummaryWeb.cs` | Podsumowanie kosztorysu w kontekście CostTrackera: CostEstimateId, CostEstimateName, TotalItemsCount, BudgetCoveredPercent, Timeline dates |

## 5. Kontroler (WebApi)

### CostEstimateController (`WebApi\Controllers\CostEstimateController.cs`)
- **Route**: `api/tenants/{tenantId:guid}/projects/{projectId:guid}/cost-estimate`
- **Auth**: `[Authorize(Policy = PermissionCodes.ProjectEstimates)]`

| Endpoint | Method | Opis |
|----------|--------|------|
| `/{scope}` | GET | Lista kosztorysów wg scope (All/Mine/Shared) |
| `/details/{id:guid}` | GET | Szczegóły kosztorysu z pełną hierarchią |
| `/` | POST | Tworzy nowy kosztorys z domyślnym schematem |
| `/generate-ai-preview` | POST | Generuje podgląd kosztorysu przez AI |
| `/create-from-ai-preview` | POST | Zapisuje kosztorys zatwierdzony z AI |
| `/{id:guid}` | PUT | Aktualizuje metadane kosztorysu (name, description) |
| `/{id:guid}` | DELETE | Miękkie usunięcie kosztorysu |
| `/{id:guid}/copy` | POST | Kopiuje kosztorys do innych projektów |
| `/{id:guid}/items/{itemId:guid}/files` | POST | Zastępuje pliki na polu ItemSystemFiles |
| `/{id:guid}/groups` | POST | Dodaje grupę do kosztorysu |
| `/{id:guid}/groups/{groupId:guid}` | DELETE | Usuwa grupę (soft delete) |
| `/{id:guid}/groups/reorder` | PUT | Zmienia kolejność grup |
| `/{id:guid}/items` | POST | Dodaje pozycję do kosztorysu |
| `/{id:guid}/items/{itemId:guid}` | DELETE | Usuwa pozycję (soft delete) |
| `/{id:guid}/groups/{groupId:guid}/items/reorder` | PUT | Zmienia kolejność pozycji w grupie |
| `/{id:guid}/items/{itemId:guid}/move` | PATCH | Przenosi pozycję do innej grupy |
| `/{id:guid}/recalculate` | POST | Przelicza sumy kosztorysu |
| `/{id:guid}/groups/{groupId:guid}/fields` | PATCH | Upsert wartości pola grupy (autosave) |
| `/{id:guid}/items/{itemId:guid}/fields` | PATCH | Upsert wartości pola pozycji (autosave) |
| `/{id:guid}/shares` | POST | Udostępnia kosztorys użytkownikom |
| `/{id:guid}/shares` | PUT | Ustawia stan udostępnienia (sync) |
| `/{id:guid}/schema/fields` | POST | Dodaje definicję pola do schematu |
| `/{id:guid}/schema/fields/{fieldId:guid}` | PUT | Aktualizuje definicję pola |
| `/{id:guid}/schema/fields/{fieldId:guid}` | DELETE | Usuwa definicję pola (tylko user-defined) |
| `/{id:guid}/schema/fields/reorder` | POST | Zmienia kolejność definicji pól |

## 6. CQRS — Komendy i Query

### 6.1 Query

| Namespace | Pliki | Opis |
|-----------|-------|------|
| `GetCostEstimates` | Query, Handler, Validator | Lista kosztorysów wg scope (All/Mine/Shared) |
| `GetCostEstimateDetails` | Query, Handler, Validator, CostEstimateFieldFileSasInfo | Szczegóły kosztorysu z pełną hierarchią grup/items/field values + SAS URIs dla plików |

### 6.2 Command — CRUD Kosztorysu

| Namespace | Pliki | Opis |
|-----------|-------|------|
| `CreateCostEstimate` | Command, Handler, Validator | Tworzy kosztorys z domyślnym schematem (10 pól systemowych) |
| `UpdateCostEstimate` | Command, Handler, Validator | Aktualizuje Name i Description |
| `DeleteCostEstimate` | Command, Handler, Validator | Miękkie usunięcie |
| `CopyCostEstimate` | Command, Handler, Validator | Głęboka kopia do innych projektów (grupy + items + field values + schema) |

### 6.3 Command — Grupy

| Namespace | Pliki | Opis |
|-----------|-------|------|
| `AddCostEstimateGroup` | Command, Handler, Validator | Dodaje grupę z pustymi wartościami pól |
| `DeleteCostEstimateGroup` | Command, Handler, Validator | Usuwa grupę + child groups + items + field files |
| `ReorderCostEstimateGroups` | Command, Handler, Validator | Zmienia kolejność grup |

### 6.4 Command — Pozycje

| Namespace | Pliki | Opis |
|-----------|-------|------|
| `AddCostEstimateItem` | Command, Handler, Validator | Dodaje pozycję (główną/opcję/komponent) z pustymi wartościami |
| `DeleteCostEstimateItem` | Command, Handler, Validator | Usuwa pozycję + child items + field files |
| `ReorderCostEstimateItems` | Command, Handler, Validator | Zmienia kolejność pozycji w grupie |
| `MoveCostEstimateItem` | Command, Handler, Validator | Przenosi pozycję między grupami |

### 6.5 Command — Field Values

| Namespace | Pliki | Opis |
|-----------|-------|------|
| `UpsertCostEstimateGroupField` | Command, Handler, Validator | Autosave — dodaje/aktualizuje wartość pola grupy |
| `UpsertCostEstimateItemField` | Command, Handler, Validator | Autosave — dodaje/aktualizuje wartość pola pozycji |
| `UploadCostEstimateFieldFiles` | Command, Handler, Validator | Zastępuje pliki na polu ItemSystemFiles |

### 6.6 Command — Field Definitions (Schema Management)

| Namespace | Pliki | Opis |
|-----------|-------|------|
| `AddFieldDefinition` | Command, Handler, Validator | Dodaje user-defined field do schematu |
| `UpdateFieldDefinition` | Command, Handler, Validator | Aktualizuje Label, IsVisible, IsReadonly itp. |
| `DeleteFieldDefinition` | Command, Handler, Validator | Usuwa user-defined field |
| `ReorderFieldDefinitions` | Command, Handler, Validator | Zmiana kolejności pól w schemacie |

### 6.7 Command — Udostępnianie

| Namespace | Pliki | Opis |
|-----------|-------|------|
| `ShareCostEstimate` | Command, Handler, Validator | Udostępnia kosztorys użytkownikom |
| `UpdateCostEstimateShares` | Command, Handler, Validator | Sync udostępnień (dodaje/usuwa) |

### 6.8 Command — Kalkulacja

| Namespace | Pliki | Opis |
|-----------|-------|------|
| `RecalculateCostEstimate` | Command, Handler, Validator | Przelicza wszystkie sumy |

### 6.9 Command — AI

| Namespace | Pliki | Opis |
|-----------|-------|------|
| `GenerateCostEstimateAIPreview` | Command, Handler, Validator | Generuje podgląd kosztorysu przez Azure OpenAI |
| `CreateCostEstimateFromAIPreview` | Command, Handler, Validator | Atomowo tworzy kosztorys z podglądu AI |

### 6.10 Command — WorkSchedule (powiązane z CostEstimate)

| Namespace | Pliki | Opis |
|-----------|-------|------|
| `SyncWorkScheduleWithEstimate` | Command, Handler, Validator | Synchronizuje harmonogram z kosztorysem |
| `GenerateScheduleFromEstimateAI` | Command, Handler, Validator | Generuje harmonogram z kosztorysu przez AI |

### 6.11 Request Base

| Plik | Opis |
|------|------|
| `CostEstimateRequestBase.cs` | Base record z TenantId, ProjectId, PermissionCode, GetResource() |
| `CostEstimateCommandBase.cs` | Rozszerza base o CostEstimateId |
| `CostEstimateAccessLevelExtensions.cs` | Metoda rozszerzająca `EnsureCanModifyStructure()` dla handlerów |

### 6.12 Helpers

| Plik | Opis |
|------|------|
| `CQRS\Helpers\CostEstimateShareValidationRules.cs` | Współdzielone reguły walidacji dla share command (CostEstimateMustExistAsync, AllUsersMustBeProjectMembersAsync) |
| `CQRS\Helpers\CostEstimateItemStructureGuard.cs` | Sprawdza czy item z komponentami nie ma bezpośrednich FieldValues |
| `CQRS\Helpers\CostEstimateFieldUpdateNotificationHelper.cs` | Wysyła powiadomienie do owner'a o aktualizacji pola |

## 7. AI Agent Tools

| Plik | Opis |
|------|------|
| `Business.AIAgent\Tools\CostEstimate\GetCostEstimateTool.cs` | Narzędzie AI do pobierania kosztorysu |
| `Business.AIAgent\Tools\CostEstimate\GetCostEstimateItemsTool.cs` | Narzędzie AI do pobierania pozycji kosztorysu |
| `Business.AIAgent\Tools\WorkSchedule\GetWorkScheduleTool.cs` | Używa `CostEstimateId` (linia 53) |

## 8. DI Registration (ServiceCollectionExtensions)

- Repozytoria (Read + Write): CostEstimate, SharedCostEstimate, CostEstimateGroup, CostEstimateGroupFieldValue, CostEstimateItem, CostEstimateItemFieldValue, CostEstimateFieldFile, CostEstimateFieldSchema, CostEstimateFieldDefinition
- Serwisy: ICostEstimateCalculationService, ICostEstimateCacheService, ICostEstimateAccessService, ICostEstimateShareService, ICostEstimateAIGeneratorService
- Wykomentowane: CostEstimateTemplate + pochodne (usunięte z projektu)

## 9. Podsumowanie ilościowe

| Kategoria | Liczba |
|-----------|--------|
| Encje domenowe | 9 (CostEstimate, Group, Item, FieldSchema, FieldDefinition, FieldValueBase, ItemFieldValue, GroupFieldValue, FieldFile) + SharedCostEstimate |
| Konfiguracje EF | 9 |
| Serwisy (interfejsy) | 5 |
| Serwisy (implementacje) | 5 |
| Web modele | 16 (w 8 plikach CostEstimates + 3 AI + 1 CostTracker) |
| Kontroler | 1 (26 endpointów) |
| Query | 2 |
| Command | 18 |
| Walidatory | 21 (wszystkie Commands/Queries mają walidatory) |
| Handlery | 20 |
| Helpery CQRS | 3 |
| AI Agent Tools | 2 |
| Cache keys | 1 klasa statyczna |
| Enums | 3 (FieldScope, FieldType, CostEstimateStatus) + CostEstimateAccessLevel |
| Stałe | CostEstimateAccessLevel (None/ReadOnly/Restricted/Full) |
| Narzędzia | FieldValueConverter |

## 10. Luki i uwagi

### Znalezione problemy:

1. **Brak dedykowanych encji dla sekcji/etapów** — koncept "etapu" (section/stage) jest reprezentowany przez CostEstimateGroup z hierarchią. Jeśli wymagana jest oddzielna encja CostEstimateSection, to jest to luka.

2. **CostEstimateListItemWeb zawiera TemplateId i TemplateName** — ale Template został usunięty z projektu (wykomentowany w DI). To wskazuje na pozostałość po refactorze, który może powodować problemy przy deserializacji odpowiedzi.

3. **Brak endpointów do bulk operations** — nie ma możliwości zbiorczego dodawania/edycji grup i pozycji w jednym żądaniu (poza AI preview). Wszystkie operacje są pojedyncze (1 grupa, 1 item, 1 field value na request).

4. **Brak dedykowanego walidatora dla schematu** — nie ma walidacji czy FieldType + FieldScope są zgodne przy dodawaniu/aktualizacji FieldDefinition.

5. **Brak testów jednostkowych w audycie** — nie sprawdzono czy istnieją testy dla CostEstimate handlerów/service.

6. **Utrata kontekstu FieldValueContext** — kod zawiera zakomentowane "CostEstimateFieldValueContext validation removed" w UpsertCostEstimateItemFieldCommandHandler.

7. **CostEstimateListItemWeb ma TemplateId/TemplateName** — ale szablony globalne zostały usunięte, co może powodować błąd 500 gdy API próbuje mapować nieistniejące pole.
