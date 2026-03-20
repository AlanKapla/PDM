# Cost Estimate — Flow, Uprawnienia i Autosave

## Poziomy dostępu do kosztorysu

Każda operacja na kosztorysie przechodzi przez `ICostEstimateAccessService.GetAccessLevelAsync`, który zwraca jeden z trzech poziomów:

| Poziom | Wartość | Kto go otrzymuje |
|---|---|---|
| `Full` | 3 | Owner kosztorysu, TenantAdmin, ProjectAdmin, SuperAdmin |
| `Restricted` | 2 | Użytkownik z udostępnionym kosztorysem (`SharedCostEstimate`) |
| `None` | 0 | Każdy inny członek projektu |

> **Uwaga:** Enum zawiera również `ReadOnly = 1`, ale żaden handler go nie przyznaje ani nie sprawdza — martwy kod.

Wynik jest cachowany w Redis przez 15 minut:
```
ce:access:{tenantId}:{projectId}:level:{userId}:{costEstimateId}
```

---

## 1. Przeglądanie

### Lista kosztorysów
```
GET api/tenants/{tenantId}/project/{projectId}/cost-estimate/{scope}
Policy: ProjectView
```

Parametr `scope`:
- `All` — wszystkie kosztorysy w projekcie
- `Mine` — tylko moje (OwnerId == currentUser)
- `Shared` — udostępnione mi przez innych

**Kto widzi co:**
- Każdy członek projektu z polityką `ProjectView` może wywołać endpoint
- Scope `All` wymaga odpowiedniego uprawnienia (`READ_ALL` via policy) ustawionego wyżej w authorization behaviour
- W scope `Mine` lista zawiera informacje o tym, z kim dany kosztorys jest udostępniony (SharedWithUsers)
- W scope `Shared` flaga `IsSharedWithMe = true`

### Szczegóły kosztorysu
```
GET api/tenants/{tenantId}/project/{projectId}/cost-estimate/details/{id}
Policy: ProjectResourcesReadSingle
```

Handler sprawdza `GetAccessLevelAsync` — przy `None` rzuca `ForbiddenApiException`.  
Restricted i Full mają pełny wgląd w hierarchię grup i pozycji.

---

## 2. Tworzenie

```
POST api/tenants/{tenantId}/project/{projectId}/cost-estimate
Policy: ProjectResourcesWrite
```

- Tworzy kosztorys na podstawie wybranego szablonu
- Tworzący staje się automatycznie **ownerem** (`OwnerId = currentUser.Id`)
- Brak dodatkowego sprawdzenia `GetAccessLevelAsync` — nowy obiekt, nie istnieje jeszcze w systemie

---

## 3. Edycja metadanych

```
PUT api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}
Policy: ProjectResourcesWrite
```

Zmiana nazwy i opisu kosztorysu.

| Poziom dostępu | Rezultat |
|---|---|
| `Full` (owner / admin) | ✅ Dozwolone |
| `Restricted` (shared user) | ❌ `ForbiddenApiException` |
| `None` | ❌ `ForbiddenApiException` |

---

## 4. Autosave — edycja pól

Autosave działa osobno dla pól grup i pól pozycji. Nie triggeruje przeliczenia — należy wywołać recalculate osobno.

### Pole grupy
```
PATCH api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/groups/{groupId}/fields
Policy: ProjectResourcesWriteShared
```

### Pole pozycji
```
PATCH api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/items/{itemId}/fields
Policy: ProjectResourcesWriteShared
```

**Logika dostępu:**

| Poziom dostępu | Pole readonly w szablonie | Rezultat |
|---|---|---|
| `None` | — | ❌ `ForbiddenApiException` |
| `Restricted` | `IsReadonly = true` | ❌ `ForbiddenApiException` |
| `Restricted` | `IsReadonly = false` | ✅ Zapis dozwolony |
| `Full` | dowolne | ✅ Zapis dozwolony |

**Powiadomienia:**  
Jeśli edytuje ktoś inny niż owner (`OwnerId != currentUser.Id`), owner otrzymuje powiadomienie push:
> „{updaterName} zaktualizował pole w kosztorysie"

---

## 5. Operacje struktury (grupy i pozycje)

Wszystkie operacje struktury wymagają policy `ProjectResourcesWrite` na poziomie controllera. Na poziomie handlera wymagany jest wyłącznie poziom `Full` — `Restricted` i `None` skutkują `ForbiddenApiException`.

| Operacja | Endpoint | Poziom wymagany |
|---|---|---|
| Dodaj grupę | `POST /{id}/groups` | Nie `Restricted`, nie `None` |
| Usuń grupę | `DELETE /{id}/groups/{groupId}` | Nie `Restricted`, nie `None` |
| Zmień kolejność grup | `PUT /{id}/groups/reorder` | Nie `Restricted`, nie `None` |
| Dodaj pozycję | `POST /{id}/items` | Nie `Restricted`, nie `None` |
| Usuń pozycję | `DELETE /{id}/items/{itemId}` | Nie `Restricted`, nie `None` |
| Zmień kolejność pozycji | `PUT /{id}/groups/{groupId}/items/reorder` | Nie `Restricted`, nie `None` |
| Przenieś pozycję między grupami | `PATCH /{id}/items/{itemId}/move` | Nie `Restricted`, nie `None` |

Komunikat błędu dla shared usera: `"Shared users cannot modify the cost estimate structure."`

---

## 6. Przeliczanie

```
POST api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/recalculate
Policy: ProjectResourcesWrite
```

Przelicza wartości Net, Gross, VAT dla całego kosztorysu na podstawie formuł szablonu.

| Poziom dostępu | Rezultat |
|---|---|
| `Full` (owner / admin) | ✅ Dozwolone |
| `Restricted` | ❌ `ForbiddenApiException` |
| `None` | ❌ `ForbiddenApiException` |

---

## 7. Pliki

```
POST api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/items/{itemId}/files
Policy: ProjectResourcesWrite
```

Zastępuje wszystkie pliki w polu typu `ItemSystemFiles`. Dozwolone formaty: PDF, JPG. Max 50 MB / plik, max 10 plików.

Request: `multipart/form-data`
- `fieldDefinitionId` — ID definicji pola (wymagane; pole musi być typu `ItemSystemFiles`)
- `files[]` — nowe pliki zastępujące wszystkie istniejące; pusta lista usuwa wszystkie pliki z pola

| Poziom dostępu | Rezultat |
|---|---|
| `Full` (owner / admin) | ✅ Dozwolone |
| `Restricted` | ❌ `ForbiddenApiException` |
| `None` | ❌ `ForbiddenApiException` |

---

## 8. Kopiowanie

```
POST api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/copy
Policy: ProjectResourcesWrite
```

Deep copy kosztorysu do jednego lub więcej projektów. Kopiujący staje się ownerem kopii.

| Poziom dostępu | Rezultat |
|---|---|
| `Full` (owner / admin) | ✅ Dozwolone |
| `Restricted` | ❌ `ForbiddenApiException` |
| `None` | ❌ `ForbiddenApiException` |

---

## 9. Usuwanie

```
DELETE api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}
Policy: ProjectResourcesWrite
```

Soft delete kosztorysu + fizyczne usunięcie wszystkich wpisów `SharedCostEstimate`.

| Poziom dostępu | Rezultat |
|---|---|
| `Full` (owner / admin) | ✅ Dozwolone |
| `Restricted` | ❌ `ForbiddenApiException` |
| `None` | ❌ `ForbiddenApiException` |

---

## 10. Udostępnianie

### Dodaj użytkowników do share
```
POST api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/shares
Policy: ProjectResourcesShare
```
Dodaje nowych użytkowników (nie usuwa istniejących).

### Zastąp pełną listę share
```
PUT api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/shares
Policy: ProjectResourcesShare
```
Ustawia dokładną listę — użytkownicy spoza listy tracą dostęp.

**Kto może udostępniać:**

| Rola | Może udostępniać? |
|---|---|
| Owner kosztorysu | ✅ |
| TenantAdmin / ProjectAdmin | ✅ |
| SuperAdmin | ✅ |
| Shared user (Restricted) | ❌ |
| Zwykły członek projektu | ❌ |

**Powiadomienia:**
- Nowo dodany użytkownik → otrzymuje push: „Udostępniono Ci kosztorys"
- Użytkownik usunięty ze share (tylko PUT) → otrzymuje push: „Cofnięto dostęp do kosztorysu"
- Cache dostępu jest inwalidowany po każdej zmianie share

---

## Tabela zbiorcza — co może kto

| Operacja | Owner / Admin | Shared user | Zwykły członek |
|---|---|---|---|
| Przeglądanie listy | ✅ (All/Mine) | ✅ (Shared) | ✅ (All/Mine, wg policy) |
| Podgląd szczegółów | ✅ | ✅ | ❌ |
| Tworzenie | ✅ | ✅ | ✅ (wg policy) |
| Edycja nazwy / opisu | ✅ | ❌ | ❌ |
| Autosave — pola readonly | ✅ | ❌ | ❌ |
| Autosave — pola edytowalne | ✅ | ✅ | ❌ |
| Dodawanie / usuwanie grup | ✅ | ❌ | ❌ |
| Reorder / przenoszenie grup | ✅ | ❌ | ❌ |
| Dodawanie / usuwanie pozycji | ✅ | ❌ | ❌ |
| Reorder / przenoszenie pozycji | ✅ | ❌ | ❌ |
| Przeliczanie | ✅ | ❌ | ❌ |
| Pliki | ✅ | ❌ | ❌ |
| Kopiowanie | ✅ | ❌ | ❌ |
| Usuwanie | ✅ | ❌ | ❌ |
| Udostępnianie | ✅ | ❌ | ❌ |

---

## Cache kosztorysu (Redis TTL: 15 min)

| Klucz | Zawartość |
|---|---|
| `ce:{id}:data` | Dane kosztorysu |
| `ce:{id}:groups` | Słownik grup |
| `ce:{id}:groups:fv` | Wartości pól grup |
| `ce:{id}:items` | Słownik pozycji |
| `ce:{id}:items:fv` | Wartości pól pozycji |
| `ce:access:{tenantId}:{projectId}:level:{userId}:{ceId}` | Poziom dostępu użytkownika |
| `ce:access:{tenantId}:{projectId}:ids:{userId}:{scope}` | ID dostępnych kosztorysów |
| `ce:access:{tenantId}:{projectId}:shares:{ceId}` | Lista userów ze share |

---

## Appendix — Commands, Queries i Modele

### Mapa endpointów

> Baza URL: `api/tenants/{tenantId}/project/{projectId}/cost-estimate`  
> Pola `TenantId` i `ProjectId` są zawsze injectowane z route — nie wysyłane w body.

| Endpoint | Metoda | Policy (Controller) | PermissionCode (CQRS) | Command / Query | Request Body | Response |
|----------|--------|---------------------|----------------------|-----------------|--------------|----------|
| `/{scope}` | GET | `ProjectView` | `ProjectResourcesReadAll` / `ProjectResourcesRead` / `ProjectResourcesReadShared` | `GetCostEstimatesQuery` | — | `List<CostEstimateListItemWeb>` |
| `/details/{id}` | GET | `ProjectResourcesReadSingle` | `ProjectResourcesReadSingle` | `GetCostEstimateDetailsQuery` | — | `CostEstimateDetailsWeb` |
| `/` | POST | `ProjectResourcesWrite` | `ProjectResourcesWrite` | `CreateCostEstimateCommand` | `CreateCostEstimateCommand` | `Guid` (201) |
| `/{id}` | PUT | `ProjectResourcesWrite` | `ProjectResourcesWrite` | `UpdateCostEstimateCommand` | `UpdateCostEstimateCommand` | 204 |
| `/{id}` | DELETE | `ProjectResourcesWrite` | `ProjectResourcesWrite` | `DeleteCostEstimateCommand` | — | 204 |
| `/{id}/copy` | POST | `ProjectResourcesWrite` | `ProjectResourcesWrite` | `CopyCostEstimateCommand` | `CopyCostEstimateCommand` | `List<Guid>` |
| `/{id}/recalculate` | POST | `ProjectResourcesWrite` | `ProjectResourcesWrite` | `RecalculateCostEstimateCommand` | — | 204 |
| `/{id}/groups` | POST | `ProjectResourcesWrite` | `ProjectResourcesWrite` | `AddCostEstimateGroupCommand` | `AddCostEstimateGroupCommand` | `Guid` (201) |
| `/{id}/groups/{groupId}` | DELETE | `ProjectResourcesWrite` | `ProjectResourcesWrite` | `DeleteCostEstimateGroupCommand` | — | 204 |
| `/{id}/groups/reorder` | PUT | `ProjectResourcesWrite` | `ProjectResourcesWrite` | `ReorderCostEstimateGroupsCommand` | `ReorderCostEstimateGroupsCommand` | 204 |
| `/{id}/items` | POST | `ProjectResourcesWrite` | `ProjectResourcesWrite` | `AddCostEstimateItemCommand` | `AddCostEstimateItemCommand` | `Guid` (201) |
| `/{id}/items/{itemId}` | DELETE | `ProjectResourcesWrite` | `ProjectResourcesWrite` | `DeleteCostEstimateItemCommand` | — | 204 |
| `/{id}/groups/{groupId}/items/reorder` | PUT | `ProjectResourcesWrite` | `ProjectResourcesWrite` | `ReorderCostEstimateItemsCommand` | `ReorderCostEstimateItemsCommand` | 204 |
| `/{id}/items/{itemId}/move` | PATCH | `ProjectResourcesWrite` | `ProjectResourcesWrite` | `MoveCostEstimateItemCommand` | `MoveCostEstimateItemCommand` | 204 |
| `/{id}/groups/{groupId}/fields` | PATCH | `ProjectResourcesWriteShared` | `ProjectResourcesWriteShared` | `UpsertCostEstimateGroupFieldCommand` | `UpsertCostEstimateGroupFieldCommand` | `Guid` |
| `/{id}/items/{itemId}/fields` | PATCH | `ProjectResourcesWriteShared` | `ProjectResourcesWriteShared` | `UpsertCostEstimateItemFieldCommand` | `UpsertCostEstimateItemFieldCommand` | `Guid` |
| `/{id}/items/{itemId}/files` | POST | `ProjectResourcesWrite` | `ProjectResourcesWrite` | `UploadCostEstimateFieldFilesCommand` | `multipart/form-data` | `List<Guid>` |
| `/{id}/shares` | POST | `ProjectResourcesShare` | `ProjectResourcesShare` | `ShareCostEstimateCommand` | `ShareCostEstimateRequestWeb` | 204 |
| `/{id}/shares` | PUT | `ProjectResourcesShare` | `ProjectResourcesShare` | `UpdateCostEstimateSharesCommand` | `UpdateCostEstimateSharesRequestWeb` | 204 |

---

### Web modele (Response DTOs)

#### `CostEstimateListItemWeb`
Zwracany przez: `GET /{scope}`

```csharp
record CostEstimateListItemWeb(
    Guid Id,
    Guid TenantId,
    Guid ProjectId,
    Guid TemplateId,
    string TemplateName,
    string Name,
    string? Description,
    CostEstimateStatus Status,
    decimal? TotalNet,
    decimal? TotalGross,
    decimal? TotalVat,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid OwnerId,
    string OwnerName,
    bool IsSharedWithMe,
    bool IsSharedByMe,
    IReadOnlyList<CostEstimateShareWeb> SharedWithUsers
)
```

#### `CostEstimateDetailsWeb`
Zwracany przez: `GET /details/{id}`

```csharp
record CostEstimateDetailsWeb(
    Guid Id,
    Guid TenantId,
    Guid ProjectId,
    Guid TemplateId,
    string TemplateName,
    Guid SelectedCurrencyId,
    string SelectedCurrencyCode,
    string? SelectedCurrencySymbol,
    string Name,
    string? Description,
    CostEstimateStatus Status,
    List<CostEstimateGroupWeb> RootGroups,
    decimal? TotalNet,
    decimal? TotalGross,
    decimal? TotalVat,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? LastCalculatedAt,
    Guid OwnerId,
    string OwnerName,
    CostEstimateTemplateStructureWeb TemplateStructure,
    CostEstimateAccessLevel AccessLevel,
    IReadOnlyList<CostEstimateShareWeb> SharedWithUsers
)
```

#### `CostEstimateGroupWeb`
Węzeł grupy w hierarchii kosztorysu.

```csharp
record CostEstimateGroupWeb(
    Guid Id,
    Guid? ParentGroupId,
    int Level,
    int Order,
    List<CostEstimateFieldValueWeb> FieldValues,
    decimal? TotalNet,
    decimal? TotalGross,
    decimal? TotalVat,
    DateTime? LastCalculatedAt,
    List<CostEstimateGroupWeb> ChildGroups,
    List<CostEstimateItemWeb> Items,
    DateTime CreatedAt,
    DateTime? UpdatedAt
)
```

#### `CostEstimateItemWeb`
Pozycja kosztorysu. `Options` i `Components` mają max 1 poziom zagnieżdżenia.

```csharp
record CostEstimateItemWeb(
    Guid Id,
    Guid GroupId,
    Guid? ParentItemId,
    int RelationType,           // ItemRelationType: None=0, Option=1, Component=2
    int Order,
    decimal? NetValue,
    decimal? GrossValue,
    decimal? VatValue,
    List<CostEstimateFieldValueWeb> FieldValues,
    List<CostEstimateItemWeb>? Options,
    List<CostEstimateItemWeb>? Components,
    DateTime CreatedAt,
    DateTime? UpdatedAt
)
```

#### `CostEstimateFieldValueWeb`
Wartość pola — wspólna dla grup i pozycji.

```csharp
record CostEstimateFieldValueWeb(
    Guid Id,
    Guid FieldDefinitionId,
    int FieldType,              // FieldType enum jako int
    int FieldScope,             // FieldScope: Group / ItemSystem / ItemCalculated / ItemGeneric
    Guid? FieldName,
    string? FieldLabel,
    string? StringValue,
    decimal? DecimalValue,
    bool? BoolValue,
    DateTime? DateTimeValue,
    List<CostEstimateFieldFileWeb>? Files  // tylko dla FieldType == ItemSystemFiles
)
```

#### `CostEstimateFieldFileWeb`
Plik pola `ItemSystemFiles`.

```csharp
record CostEstimateFieldFileWeb(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    int Order,
    string? SasUriPreview,
    string? SasUriDownload,
    DateTime CreatedAt
)
```

#### `CostEstimateShareWeb`

```csharp
record CostEstimateShareWeb(
    Guid UserId,
    string FullName,
    string Email,
    DateTime SharedAt
)
```

---

### Request modele (Input DTOs)

#### `CreateCostEstimateCommand`
```csharp
record CreateCostEstimateCommand(
    Guid TemplateId,
    Guid SelectedCurrencyId,
    string Name,
    string? Description
)
```

#### `UpdateCostEstimateCommand`
```csharp
record UpdateCostEstimateCommand(
    string Name,
    string? Description
)
```

#### `CopyCostEstimateCommand`
```csharp
record CopyCostEstimateCommand(
    Guid CostEstimateId,        // inject z route
    List<Guid> TargetProjectIds
)
```

#### `AddCostEstimateGroupCommand`
```csharp
record AddCostEstimateGroupCommand(
    Guid CostEstimateId,        // inject z route
    Guid? ParentGroupId,
    int Order
)
```

#### `ReorderCostEstimateGroupsCommand`
```csharp
record ReorderCostEstimateGroupsCommand(
    Guid CostEstimateId,        // inject z route
    List<ReorderGroupDto> Groups
)

record ReorderGroupDto(
    Guid GroupId,
    Guid? ParentGroupId,
    int Order
)
```

#### `AddCostEstimateItemCommand`
```csharp
record AddCostEstimateItemCommand(
    Guid CostEstimateId,            // inject z route
    Guid GroupId,
    Guid? ParentItemId,
    ItemRelationType RelationType,  // None=0, Option=1, Component=2
    int Order
)
```

#### `ReorderCostEstimateItemsCommand`
```csharp
record ReorderCostEstimateItemsCommand(
    Guid CostEstimateId,    // inject z route
    Guid GroupId,           // inject z route
    List<ReorderItemDto> Items
)

record ReorderItemDto(
    Guid ItemId,
    int Order
)
```

#### `MoveCostEstimateItemCommand`
```csharp
record MoveCostEstimateItemCommand(
    Guid CostEstimateId,    // inject z route
    Guid ItemId,            // inject z route
    Guid TargetGroupId
)
```

#### `UpsertCostEstimateGroupFieldCommand`
```csharp
record UpsertCostEstimateGroupFieldCommand
{
    Guid CostEstimateId     // inject z route
    Guid GroupId            // inject z route
    Guid? FieldValueId      // null = utwórz nowy; non-null = aktualizuj istniejący
    Guid? FieldDefinitionId // wymagane gdy FieldValueId == null
    string? StringValue
    decimal? DecimalValue
    bool? BoolValue
    DateTime? DateTimeValue
}
```

#### `UpsertCostEstimateItemFieldCommand`
```csharp
record UpsertCostEstimateItemFieldCommand
{
    Guid CostEstimateId     // inject z route
    Guid ItemId             // inject z route
    Guid? FieldValueId      // null = utwórz nowy; non-null = aktualizuj istniejący
    Guid? FieldDefinitionId // wymagane gdy FieldValueId == null
    string? StringValue
    decimal? DecimalValue
    bool? BoolValue
    DateTime? DateTimeValue
}
```

#### `UploadCostEstimateFieldFilesCommand` (multipart/form-data)
```
fieldDefinitionId : Guid         (form field)
files             : IFormFile[]   (max 10 plików, max 50 MB/plik, dozwolone: PDF, JPG)
```

#### `ShareCostEstimateRequestWeb`
```csharp
record ShareCostEstimateRequestWeb(List<Guid> UserIds)
```

#### `UpdateCostEstimateSharesRequestWeb`
```csharp
record UpdateCostEstimateSharesRequestWeb(List<Guid> UserIds)
```
