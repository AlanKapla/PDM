# Raport audytu walidacji — Kosztorysy (szablony + wypełnianie)

**Data:** 2026-05-31  
**Zakres:** `CostEstimateTemplateController` + `CostEstimateController`  
**Warstwy:** FluentValidation validators + logika handlera

---

## 1. LEGENDA PROBLEMÓW

| Symbol | Waga |
|--------|------|
| 🔴 Krytyczny | Błąd danych / bezpieczeństwo / nieprawidłowe HTTP |
| 🟠 Wysoki | Brak walidacji kluczowego pola, ryzyko błędu w runtime |
| 🟡 Normalny | Niespójność, niepełna walidacja, dobra praktyka |
| 🟢 OK | Walidacja poprawna |

---

## 2. SZABLONY — CostEstimateTemplateController

### 2.1 POST `/api/cost-estimate-template` — CreateCostEstimateTemplate

**Validator:** `CreateCostEstimateTemplateCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `Name` | NotEmpty, MaxLength(200) | 🟢 OK |
| `Description` | MaxLength(2000) gdy not empty | 🟢 OK |

**Luki:**
- 🟡 Brak walidacji unikalności nazwy w ramach użytkownika (duplikat nazwy w DB jest możliwy)
- 🟡 Description MaxLength(2000) — **niespójne z Update** gdzie jest MaxLength(1000) → patrz sekcja 2.3

---

### 2.2 GET `/api/cost-estimate-template` — GetCostEstimateTemplates

**Validator:** ❌ brak  
**Status:** 🟢 OK (GET bez parametrów, autoryzacja przez `[Authorize]`)

---

### 2.3 PUT `/api/cost-estimate-template/{id}` — UpdateCostEstimateTemplate

**Validator:** `UpdateCostEstimateTemplateCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TemplateId` | NotEmpty | 🟢 OK |
| `Name` | NotEmpty, MaxLength(200) | 🟢 OK |
| `Description` | MaxLength(1000) | 🟠 Niespójne z Create (2000) |
| `Category` | MaxLength(100) | 🟢 OK |
| `MaxGroupLevel` | GreaterThan(0) gdy HasValue | 🟢 OK |
| `GroupNumberFormat` | MaxLength(50) | 🟢 OK |
| `GroupHeaderFields` | FieldNames nie puste (Guid != Empty), FieldTypes unikalne, brak child fields | 🟢 OK |
| `SystemFields` | FieldNames nie puste, FieldTypes unikalne, tylko Options mogą mieć dzieci | 🟢 OK |
| `CalculatedFields` | FieldNames nie puste, FieldTypes unikalne, brak child fields, tylko ValueNet/ValueGross/TotalVat mają SumFlags | 🟢 OK |
| `GenericFields` | FieldNames nie puste, brak child fields | 🟢 OK |
| `UiConfiguration.ColumnLayout` | Odwołuje się tylko do istniejących FieldNames (Guid) | 🟢 OK |

**Handler-level (`UpdateCostEstimateTemplateCommandHandler`):**

| Reguła | Status |
|--------|--------|
| Template musi istnieć AND `OwnerId == currentUser.Id` → `NotFoundApiException` | 🟠 Zwraca 404 zamiast 403 gdy szablon należy do kogoś innego |
| Gdy `UpdateStructure=true` — obowiązkowe pola: `GroupName`, `ItemSystemName`, `ItemCalculatedValueNet`, `ItemCalculatedValueGross` | 🟢 OK |

**Luki:**
- 🔴 **Niespójność Description MaxLength** — Create/Duplicate/FromDefault: 2000 znaków, Update: 1000 znaków. Użytkownik może stworzyć szablon z opisem 1500 znaków, a potem nie będzie mógł go zaktualizować.
- 🟠 Handler zwraca `NotFoundApiException` (→ 404) gdy `OwnerId != currentUser.Id`. Powinno być `ForbiddenApiException` (→ 403).

---

### 2.4 DELETE `/api/cost-estimate-template/{id}` — DeleteCostEstimateTemplate

**Validator:** `DeleteCostEstimateTemplateCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TemplateId` | NotEmpty | 🟢 OK |

**Luki:**
- 🟡 Brak sprawdzenia w validatorze czy szablon ma powiązane aktywne kosztorysy (musi obsługiwać handler/serwis)
- 🟡 Nie sprawdzono w validatorze właściciela (async check) — handler robi to przez serwis

---

### 2.5 POST `/api/cost-estimate-template/{id}/duplicate` — DuplicateCostEstimateTemplate

**Validator:** `DuplicateCostEstimateTemplateCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `SourceTemplateId` | NotEmpty | 🟢 OK |
| `Name` | NotEmpty, MaxLength(200) | 🟢 OK |
| `Description` | MaxLength(2000) gdy not empty | 🟢 OK (ale niespójne z Update) |

---

### 2.6 GET `/api/cost-estimate-template/defaults` — GetDefaultTemplates

**Validator:** ❌ brak  
**Status:** 🟢 OK (GET bez parametrów)

---

### 2.7 GET `/api/cost-estimate-template/defaults/{slug}` — GetDefaultTemplateDetails

**Validator:** ❌ brak  
**Luki:**
- 🟡 Brak walidacji `slug` (NotEmpty) — route binding przyjmie pusty segment URL, request trafi do handlera z pustym slugiem

---

### 2.8 POST `/api/cost-estimate-template/defaults/{slug}` — CreateCostEstimateTemplateFromDefault

**Validator:** `CreateCostEstimateTemplateFromDefaultCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `Slug` | NotEmpty | 🟢 OK |
| `Name` | NotEmpty, MaxLength(200) | 🟢 OK |
| `Description` | MaxLength(2000) gdy not empty | 🟢 OK |

---

### 2.9 GET `/api/cost-estimate-template/field-type-configurations` — GetFieldTypeConfigurations

**Validator:** ❌ brak  
**Status:** 🟢 OK (GET bez parametrów)

---

## 3. KOSZTORYSY — CostEstimateController

### 3.1 GET `/{tenantId}/{projectId}/cost-estimate/{scope}` — GetCostEstimates

**Validator:** `GetCostEstimatesQueryValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId` | RequiredId (NotEmpty) | 🟢 OK |
| `ProjectId` | RequiredId | 🟢 OK |
| `Scope` | IsInEnum | 🟢 OK |

---

### 3.2 GET `/{tenantId}/{projectId}/cost-estimate/details/{id}` — GetCostEstimateDetails

**Validator:** `GetCostEstimateDetailsQueryValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId` | RequiredId | 🟢 OK |

---

### 3.3 POST `/{tenantId}/{projectId}/cost-estimate` — CreateCostEstimate

**Validator:** `CreateCostEstimateCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `TemplateId` | RequiredId | 🟢 OK |
| `Name` | NotEmpty, MaxLength(200) | 🟢 OK |
| `Description` | MaxLength(1000) | 🟢 OK |

**Handler-level (`CreateCostEstimateCommandHandler`):**

| Reguła | Status |
|--------|--------|
| Template musi istnieć AND `OwnerId == currentUser.Id` AND `!IsDeleted` → `NotFoundApiException` | 🟠 Zwraca 404 gdy szablon należy do kogoś innego (powinno 403 lub własna wiadomość) |

**Luki:**
- 🟡 Brak walidacji `Currency` / `Locale` (jeśli jest w modelu — nie widać w commandzie)
- 🟡 Brak walidacji czy `TemplateId` należy do tego samego tenant — handler sprawdza przez `OwnerId`, co pośrednio gwarantuje izolację

---

### 3.4 PUT `/{tenantId}/{projectId}/cost-estimate/{id}` — UpdateCostEstimate

**Validator:** `UpdateCostEstimateCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId` | RequiredId | 🟢 OK |
| `Name` | NotEmpty, MaxLength(200) | 🟢 OK |
| `Description` | MaxLength(1000) | 🟢 OK |

**Luki:**
- 🟡 Brak async sprawdzenia istnienia i własności kosztorysu w validatorze — delegowane do handlera

---

### 3.5 DELETE `/{tenantId}/{projectId}/cost-estimate/{id}` — DeleteCostEstimate

**Validator:** `DeleteCostEstimateCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId` | RequiredId | 🟢 OK |

---

### 3.6 POST `/{tenantId}/{projectId}/cost-estimate/{id}/copy` — CopyCostEstimate

**Validator:** `CopyCostEstimateCommandValidator` (async, z repozytoriami)

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId` | RequiredId | 🟢 OK |
| `TargetProjectIds` | NotEmpty, Count > 0, UniqueIds | 🟢 OK |
| CostEstimate exists AND `OwnerId == currentUser.Id` | Async check | 🟠 Tylko właściciel może kopiować — admin projektu nie może skopiować cudzego kosztorysu |
| Wszystkie target projects istnieją w tenant | Async check | 🟢 OK |
| User ma dostęp do każdego target project (member + permissions) | Async check | 🟢 OK |
| Inactive projects: tylko project admin może kopiować do | Async check | 🟢 OK |

**Luki:**
- 🟠 `OwnerId == currentUser.Id` w validatorze — **tenant admin nie może skopiować kosztorysu innego użytkownika** (to może być celowe ograniczenie, ale warto zweryfikować wymaganie)

---

### 3.7 POST `/{tenantId}/{projectId}/cost-estimate/{id}/groups` — AddCostEstimateGroup

**Validator:** `AddCostEstimateGroupCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId` | RequiredId | 🟢 OK |
| `Order` | NonNegativeOrder (≥ 0) | 🟢 OK |
| `ParentGroupId` | ❌ brak walidacji | 🟡 Brak RequiredId gdy nie-null |

**Handler-level (`AddCostEstimateGroupCommandHandler`):**

| Reguła | Status |
|--------|--------|
| AccessLevel `EnsureCanModifyStructure()` | 🟢 OK |
| `template.CanAddGroups` | 🟢 OK |
| ParentGroup existence | 🟢 OK (404) |
| `template.CanBranchGroups` | 🟢 OK |
| `level > template.MaxGroupLevel` | 🟢 OK |

---

### 3.8 DELETE `/{tenantId}/{projectId}/cost-estimate/{id}/groups/{groupId}` — DeleteCostEstimateGroup

**Validator:** `DeleteCostEstimateGroupCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId`, `GroupId` | RequiredId | 🟢 OK |

---

### 3.9 PUT `/{tenantId}/{projectId}/cost-estimate/{id}/groups/reorder` — ReorderCostEstimateGroups

**Validator:** `ReorderCostEstimateGroupsCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId` | RequiredId | 🟢 OK |
| `Groups` | NotNull, Count > 0 | 🟢 OK |
| Per group: `GroupId` | RequiredId | 🟢 OK |
| Per group: `Order` | NonNegativeOrder | 🟢 OK |

**Luki:**
- 🟡 Brak sprawdzenia czy podane `GroupId` należą do wskazanego `CostEstimateId` — delegowane do handlera

---

### 3.10 POST `/{tenantId}/{projectId}/cost-estimate/{id}/items` — AddCostEstimateItem

**Validator:** `AddCostEstimateItemCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId`, `GroupId` | RequiredId | 🟢 OK |
| `Order` | NonNegativeOrder | 🟢 OK |
| `RelationType` | IsInEnum | 🟢 OK |
| `ParentItemId` | NotEmpty gdy `RelationType != None` | 🟢 OK |
| `ParentItemId` | Null gdy `RelationType == None` | 🟢 OK |

**Handler-level (`AddCostEstimateItemCommandHandler`):**

| Reguła | Status |
|--------|--------|
| AccessLevel `EnsureCanModifyStructure()` | 🟢 OK |
| Group existence | 🟢 OK (404) |
| ParentItem existence | 🟢 OK (404) |
| Options nie mogą mieć własnych Options | 🟢 OK (`ValidationApiException`) |
| Components tylko z main positions (RelationType=None) | 🟢 OK (`ValidationApiException`) |

---

### 3.11 DELETE `/{tenantId}/{projectId}/cost-estimate/{id}/items/{itemId}` — DeleteCostEstimateItem

**Validator:** `DeleteCostEstimateItemCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId`, `ItemId` | RequiredId | 🟢 OK |

---

### 3.12 PUT `/{tenantId}/{projectId}/cost-estimate/{id}/groups/{groupId}/items/reorder` — ReorderCostEstimateItems

**Validator:** `ReorderCostEstimateItemsCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId`, `GroupId` | RequiredId | 🟢 OK |
| `Items` | NotNull, Count > 0 | 🟢 OK |
| Per item: `ItemId` | RequiredId | 🟢 OK |
| Per item: `Order` | NonNegativeOrder | 🟢 OK |

**Luki:**
- 🟡 Brak sprawdzenia czy `ItemId` należą do wskazanego `GroupId` / `CostEstimateId`

---

### 3.13 PATCH `/{tenantId}/{projectId}/cost-estimate/{id}/items/{itemId}/move` — MoveCostEstimateItem

**Validator:** `MoveCostEstimateItemCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId`, `ItemId`, `TargetGroupId` | RequiredId | 🟢 OK |

**Luki:**
- 🟡 Brak sprawdzenia czy `TargetGroupId` należy do wskazanego `CostEstimateId`

---

### 3.14 PATCH `/{tenantId}/{projectId}/cost-estimate/{id}/groups/{groupId}/fields` — UpsertCostEstimateGroupField

**Validator:** `UpsertCostEstimateGroupFieldCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId`, `GroupId` | RequiredId | 🟢 OK |
| `FieldDefinitionId` | NotEmpty gdy `FieldValueId is null` (add mode) | 🟢 OK |

**Handler-level (`UpsertCostEstimateGroupFieldCommandHandler`):**

| Reguła | Status |
|--------|--------|
| AccessLevel None → 403 | 🟢 OK |
| AccessLevel ReadOnly → 403 | 🟢 OK |
| AccessLevel Restricted + IsReadonly → 403 | 🟢 OK |
| Group existence w kosztorysie | 🟢 OK (404) |
| FieldDefinition existence w template.GroupFieldDefinitions | 🟢 OK (ValidationApiException) |
| Notyfikacja do właściciela gdy edytuje inny użytkownik | 🟢 OK |

**Luki:**
- 🟠 **Brak walidacji wartości**: nie ma reguły wymagającej podania przynajmniej jednej z: `StringValue`, `DecimalValue`, `BoolValue`, `DateTimeValue` — można zapisać "pusty" update

---

### 3.15 PATCH `/{tenantId}/{projectId}/cost-estimate/{id}/items/{itemId}/fields` — UpsertCostEstimateItemField

**Validator:** `UpsertCostEstimateItemFieldCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId`, `ItemId` | RequiredId | 🟢 OK |
| `FieldDefinitionId` | NotEmpty gdy `FieldValueId is null` (add mode) | 🟢 OK |

**Handler-level (`UpsertCostEstimateItemFieldCommandHandler`):**

| Reguła | Status |
|--------|--------|
| AccessLevel None → 403 | 🟢 OK |
| AccessLevel ReadOnly → 403 | 🟢 OK |
| AccessLevel Restricted + IsReadonly → 403 | 🟢 OK |
| Item existence w kosztorysie | 🟢 OK (404) |
| FieldDefinition existence w template (SystemFields+CalculatedFields+GenericFields) | 🟢 OK (ValidationApiException) |
| **VatRate range [0, 1]** dla `ItemCalculatedVatRate` | 🟢 OK (w handlerze — mogłoby być w validatorze) |
| Jeśli pole już istnieje przy "add" → traktuje jako update (bez ConflictException) | 🟢 OK (idempotentne) |
| Notyfikacja do właściciela gdy edytuje inny użytkownik | 🟢 OK |

**Luki:**
- 🟠 Brak walidacji wartości w validatorze (analogicznie do GroupField)
- 🟡 `VatRate [0,1]` sprawdzany tylko dla Add path — w Update path nie ma analogicznego sprawdzenia (handler `UpdateFieldValue` nie waliduje zakresu)

---

### 3.16 POST `/{tenantId}/{projectId}/cost-estimate/{id}/items/{itemId}/files` — UploadCostEstimateFieldFiles

**Validator:** `UploadCostEstimateFieldFilesCommandValidator` (async, z repozytoriami)

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId`, `ItemId`, `FieldDefinitionId` | RequiredId | 🟢 OK |
| Files count | ≤ 10 gdy files > 0 | 🟢 OK |
| Per file: Length | > 0 i ≤ 50 MB | 🟢 OK |
| Per file: FileName | NotEmpty, rozszerzenie: .pdf / .jpg / .jpeg | 🟢 OK |
| Per file: ContentType | application/pdf lub image/jpeg | 🟢 OK |
| CostEstimate exists w tenant/project | Async | 🟢 OK |
| Item belongs to CostEstimate | Async | 🟢 OK |
| FieldDefinition type == `ItemSystemFiles` | Async | 🟢 OK |

**Luki:**
- 🟡 Brak sprawdzenia duplikatów nazw plików w jednym requescie
- 🟡 Content-Type vs rozszerzenie: walidowane oddzielnie — można wysłać `.pdf` z ContentType `image/jpeg` (nie ma cross-check)

---

### 3.17 POST `/{tenantId}/{projectId}/cost-estimate/{id}/recalculate` — RecalculateCostEstimate

**Validator:** `RecalculateCostEstimateCommandValidator`

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId` | RequiredId | 🟢 OK |

---

### 3.18 POST `/{tenantId}/{projectId}/cost-estimate/{id}/shares` — ShareCostEstimate

**Validator:** `ShareCostEstimateCommandValidator` (async, z repozytoriami)

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId` | RequiredId | 🟢 OK |
| `ShareWithUserIds` | NotEmpty (min 1) | 🟢 OK |
| `ShareWithUserIds` | UniqueIds | 🟢 OK |
| CostEstimate exists AND `!IsDeleted` | Async | 🟢 OK |
| Wszyscy users są project members | Async (batch query) | 🟢 OK |

---

### 3.19 PUT `/{tenantId}/{projectId}/cost-estimate/{id}/shares` — UpdateCostEstimateShares

**Validator:** `UpdateCostEstimateSharesCommandValidator` (async, z repozytoriami)

| Pole | Reguła | Status |
|------|--------|--------|
| `TenantId`, `ProjectId`, `CostEstimateId` | RequiredId | 🟢 OK |
| `UserIds` | UniqueIds gdy > 0 | 🟢 OK |
| `UserIds` | ❌ Brak NotEmpty — pusta lista dozwolona (sets desired state = usuwa wszystkie shares) | 🟢 Celowe |
| CostEstimate exists AND `!IsDeleted` | Async | 🟢 OK |
| Wszyscy provided users są project members | Async (batch query) | 🟢 OK |

---

## 4. ZESTAWIENIE PROBLEMÓW

### 🔴 Krytyczne (1)

| # | Obszar | Problem | Plik |
|---|--------|---------|------|
| K1 | Templates | **Niespójność Description MaxLength**: Create/Duplicate/FromDefault = 2000, Update = 1000. Użytkownik może stworzyć szablon z opisem >1000 znaków, który stanie się niemożliwy do edycji. | `CreateCostEstimateTemplateCommandValidator.cs` / `UpdateCostEstimateTemplateCommandValidator.cs` |

### 🟠 Wysokie (4)

| # | Obszar | Problem | Plik |
|---|--------|---------|------|
| H1 | Templates | Handler zwraca **404 zamiast 403** gdy `OwnerId != currentUser.Id` przy UpdateTemplate i DeleteTemplate | `UpdateCostEstimateTemplateCommandHandler.cs` |
| H2 | Kosztorysy | **UpsertGroupField / UpsertItemField — brak walidacji wartości** w validatorze — można zapisać kompletnie pusty field update | `UpsertCostEstimateGroupFieldCommandValidator.cs` / `UpsertCostEstimateItemFieldCommandValidator.cs` |
| H3 | Kosztorysy | **CopyCostEstimate — tylko właściciel może kopiować** (`OwnerId == currentUser.Id` w validatorze) — tenant admin nie może skopiować cudzego kosztorysu | `CopyCostEstimateCommandValidator.cs` |
| H4 | Kosztorysy | **VatRate [0,1] sprawdzany tylko w Add path** (`AddFieldValue`), nie w `UpdateFieldValue` | `UpsertCostEstimateItemFieldCommandHandler.cs` |

### 🟡 Normalne (7)

| # | Obszar | Problem | Plik |
|---|--------|---------|------|
| N1 | Templates | `CreateCostEstimateTemplate` — brak sprawdzenia unikalności nazwy | `CreateCostEstimateTemplateCommandValidator.cs` |
| N2 | Templates | `GetDefaultTemplateDetails` — brak walidatora dla `slug` (pusty slug przejdzie przez route) | brak validatora |
| N3 | Kosztorysy | `ReorderGroups` / `ReorderItems` — brak sprawdzenia przynależności GroupId/ItemId do CostEstimate | validators |
| N4 | Kosztorysy | `MoveCostEstimateItem` — brak sprawdzenia czy `TargetGroupId` należy do CostEstimate | `MoveCostEstimateItemCommandValidator.cs` |
| N5 | Kosztorysy | `AddCostEstimateGroup` — brak walidacji `ParentGroupId` (format/NotEmpty) w validatorze gdy przekazany | `AddCostEstimateGroupCommandValidator.cs` |
| N6 | Kosztorysy | `UploadFiles` — brak cross-check ContentType vs rozszerzenie pliku; brak sprawdzenia duplikatów nazw | `UploadCostEstimateFieldFilesCommandValidator.cs` |
| N7 | Kosztorysy | `CreateCostEstimate` — handler zwraca 404 (zamiast 403) gdy szablon należy do innego użytkownika | `CreateCostEstimateCommandHandler.cs` |

---

## 5. MAPA ENDPOINT → VALIDATOR → HANDLER

```
TEMPLATES
├── POST   /                        → CreateCostEstimateTemplateCommandValidator         → CreateCostEstimateTemplateCommandHandler
├── GET    /                        → (brak)                                             → GetCostEstimateTemplatesQueryHandler
├── PUT    /{id}                    → UpdateCostEstimateTemplateCommandValidator         → UpdateCostEstimateTemplateCommandHandler
│                                                                                          + ValidateRequiredTemplateFields (handler base)
├── DELETE /{id}                    → DeleteCostEstimateTemplateCommandValidator         → DeleteCostEstimateTemplateCommandHandler
├── POST   /{id}/duplicate          → DuplicateCostEstimateTemplateCommandValidator      → DuplicateCostEstimateTemplateCommandHandler
├── GET    /defaults                → (brak)                                             → GetDefaultCostEstimateTemplatesQueryHandler
├── GET    /defaults/{slug}         → (brak) ⚠️                                         → GetDefaultCostEstimateTemplateDetailsQueryHandler
├── POST   /defaults/{slug}         → CreateCostEstimateTemplateFromDefaultCommandValidator → CreateCostEstimateTemplateFromDefaultCommandHandler
└── GET    /field-type-configurations → (brak)                                          → GetFieldTypeConfigurationsQueryHandler

KOSZTORYSY
├── GET    /{scope}                 → GetCostEstimatesQueryValidator                     → GetCostEstimatesQueryHandler
├── GET    /details/{id}            → GetCostEstimateDetailsQueryValidator               → GetCostEstimateDetailsQueryHandler
├── POST   /                        → CreateCostEstimateCommandValidator                 → CreateCostEstimateCommandHandler (owner check)
├── PUT    /{id}                    → UpdateCostEstimateCommandValidator                 → UpdateCostEstimateCommandHandler
├── DELETE /{id}                    → DeleteCostEstimateCommandValidator                 → DeleteCostEstimateCommandHandler
├── POST   /{id}/copy               → CopyCostEstimateCommandValidator (async)           → CopyCostEstimateCommandHandler
├── POST   /{id}/items/{itemId}/files → UploadCostEstimateFieldFilesCommandValidator (async) → UploadCostEstimateFieldFilesCommandHandler
├── POST   /{id}/groups             → AddCostEstimateGroupCommandValidator               → AddCostEstimateGroupCommandHandler (access+template+level)
├── DELETE /{id}/groups/{groupId}   → DeleteCostEstimateGroupCommandValidator            → DeleteCostEstimateGroupCommandHandler
├── PUT    /{id}/groups/reorder     → ReorderCostEstimateGroupsCommandValidator          → ReorderCostEstimateGroupsCommandHandler
├── POST   /{id}/items              → AddCostEstimateItemCommandValidator                → AddCostEstimateItemCommandHandler (access+nesting)
├── DELETE /{id}/items/{itemId}     → DeleteCostEstimateItemCommandValidator             → DeleteCostEstimateItemCommandHandler
├── PUT    /{id}/groups/{groupId}/items/reorder → ReorderCostEstimateItemsCommandValidator → ReorderCostEstimateItemsCommandHandler
├── PATCH  /{id}/items/{itemId}/move  → MoveCostEstimateItemCommandValidator             → MoveCostEstimateItemCommandHandler
├── PATCH  /{id}/groups/{groupId}/fields → UpsertCostEstimateGroupFieldCommandValidator → UpsertCostEstimateGroupFieldCommandHandler (access+readonly)
├── PATCH  /{id}/items/{itemId}/fields  → UpsertCostEstimateItemFieldCommandValidator   → UpsertCostEstimateItemFieldCommandHandler (access+readonly+vatrate)
├── POST   /{id}/recalculate        → RecalculateCostEstimateCommandValidator            → RecalculateCostEstimateCommandHandler
├── POST   /{id}/shares             → ShareCostEstimateCommandValidator (async)          → ShareCostEstimateCommandHandler
└── PUT    /{id}/shares             → UpdateCostEstimateSharesCommandValidator (async)   → UpdateCostEstimateSharesCommandHandler
```

---

## 6. PRIORYTETY NAPRAWY

| Priorytet | ID | Akcja |
|-----------|-----|-------|
| 1 | K1 | Wyrównać `Description MaxLength` do 2000 w `UpdateCostEstimateTemplateCommandValidator` |
| 2 | H1 | Zmienić logikę w `UpdateCostEstimateTemplateCommandHandler.GetAndValidateTemplateAsync` — rzucać `ForbiddenApiException` gdy template istnieje ale `OwnerId != currentUser.Id` |
| 3 | H4 | Dodać VatRate [0,1] check w `UpdateFieldValue` w `UpsertCostEstimateItemFieldCommandHandler` |
| 4 | H2 | Rozważyć dodanie walidacji że przynajmniej jedno pole wartości jest nie-null w `UpsertGroupField/ItemField` |
| 5 | H3 | Zweryfikować wymaganie biznesowe — czy tenant admin powinien móc kopiować cudze kosztorysy |
| 6 | N7 | Analogicznie do H1 — `CreateCostEstimateCommandHandler` 404 vs 403 |
| 7 | N2 | Dodać validator dla `GetDefaultCostEstimateTemplateDetailsQuery` ze sprawdzeniem `slug` NotEmpty |
| 8 | N6 | Dodać cross-check ContentType vs extension w `UploadCostEstimateFieldFilesCommandValidator` |
