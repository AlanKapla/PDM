# Cost Estimate API — Zmiany dla UI

> Branch: `AK/cost-estimate-refactor`  
> Base URL: `api/tenants/{tenantId}/project/{projectId}/cost-estimate`

---

## Spis treści

1. [Zmiany przełomowe (breaking changes)](#1-zmiany-przełomowe-breaking-changes)
2. [Zmiany w komendach CQRS](#2-zmiany-w-komendach-cqrs)
3. [Dodawanie grupy — POST `/groups`](#3-dodawanie-grupy--post-idguidgroups)
4. [Dodawanie pozycji — POST `/items`](#4-dodawanie-pozycji--post-idguiditems)
5. [Pola grupy — PATCH `/groups/{groupId}/fields`](#5-pola-grupy--patch-idguidgroupsgroupidguidfields)
6. [Pola pozycji — PATCH `/items/{itemId}/fields`](#6-pola-pozycji--patch-idguiditemsitemidguidfields)
7. [Enums referencyjne](#7-enums-referencyjne)
8. [Przykłady wywołań](#8-przykłady-wywołań)

---

## 1. Zmiany przełomowe (breaking changes)

### 1.1 Dodawanie grupy i pozycji — zmiana odpowiedzi

| Endpoint | Stara odpowiedź | Nowa odpowiedź |
|---|---|---|
| `POST /{id}/groups` | `{ groupId, fieldValues[] }` | `"3fa85f64-..."` *(tylko Guid)* |
| `POST /{id}/items` | `{ itemId, fieldValues[] }` | `"3fa85f64-..."` *(tylko Guid)* |

**Uzasadnienie:** API nie tworzy już domyślnych pustych wierszy pól w bazie przy dodaniu grupy/pozycji. Pola będą tworzone przez UI dopiero przy pierwszym zapisie wartości (autosave). `fieldValues[]` nie jest już zwracane — definicje pól należy odczytać z `templateStructure` w odpowiedzi `GET /details/{id}`.

**Wpływ na UI:**
- Po `POST /{id}/groups` → odpowiedź to `Guid` (string w JSON), nie obiekt
- Po `POST /{id}/items` → odpowiedź to `Guid` (string w JSON), nie obiekt
- UI powinien renderować pola na podstawie `templateStructure` z `GET /details/{id}`, nie na podstawie zwróconych `fieldValues`
- Przy pierwszym autosave pola dla nowej grupy/pozycji — `fieldValueId` wysyłamy jako `null`

---

### 1.2 Upsert pól — zastąpienie 4 endpointów przez 2

Usunięte endpointy:

| Stary endpoint | Zastąpiony przez |
|---|---|
| `PATCH /{id}/groups/{groupId}/fields/{fieldValueId}` | `PATCH /{id}/groups/{groupId}/fields` |
| `POST /{id}/groups/{groupId}/fields` | `PATCH /{id}/groups/{groupId}/fields` |
| `PATCH /{id}/items/{itemId}/fields/{fieldValueId}` | `PATCH /{id}/items/{itemId}/fields` |
| `POST /{id}/items/{itemId}/fields` | `PATCH /{id}/items/{itemId}/fields` |

---

## 2. Zmiany w komendach CQRS

### 2.1 Usunięte komendy

Następujące klasy zostały **trwale usunięte** z projektu `CQRS`:

| Usunięta klasa | Powód |
|---|---|
| `AddCostEstimateGroupFieldCommand` | Zastąpiona przez `UpsertCostEstimateGroupFieldCommand` |
| `AddCostEstimateGroupFieldCommandHandler` | j.w. |
| `AddCostEstimateGroupFieldCommandValidator` | j.w. |
| `AddCostEstimateItemFieldCommand` | Zastąpiona przez `UpsertCostEstimateItemFieldCommand` |
| `AddCostEstimateItemFieldCommandHandler` | j.w. |
| `AddCostEstimateItemFieldCommandValidator` | j.w. |
| `UpdateCostEstimateGroupFieldCommand` | Zastąpiona przez `UpsertCostEstimateGroupFieldCommand` |
| `UpdateCostEstimateGroupFieldCommandHandler` | j.w. |
| `UpdateCostEstimateGroupFieldCommandValidator` | j.w. |
| `UpdateCostEstimateItemFieldCommand` | Zastąpiona przez `UpsertCostEstimateItemFieldCommand` |
| `UpdateCostEstimateItemFieldCommandHandler` | j.w. |
| `UpdateCostEstimateItemFieldCommandValidator` | j.w. |

Następujące DTO zostały **usunięte** z projektu `Business.Interfaces`:

| Usunięte DTO | Powód |
|---|---|
| `AddCostEstimateGroupResultWeb` | Handler zwraca teraz `Guid` bezpośrednio |
| `AddCostEstimateItemResultWeb` | j.w. |

---

### 2.2 Zmieniona komenda: `AddCostEstimateGroupCommand`

**Plik:** `CQRS/CostEstimates/AddCostEstimateGroup/AddCostEstimateGroupCommand.cs`

| | Przed | Po |
|---|---|---|
| Typ zwracany | `IRequestCommand<AddCostEstimateGroupResultWeb>` | `IRequestCommand<Guid>` |
| Handler tworzy domyślne `FieldValues` | ✅ tak | ❌ nie |
| Handler wstrzykuje `IRepository<CostEstimateGroupFieldValue>` | ✅ tak | ❌ nie |
| Handler wstrzykuje `ICostEstimateService` | ✅ tak | ❌ nie |

```csharp
// Aktualna sygnatura komendy
public sealed record AddCostEstimateGroupCommand(
    Guid CostEstimateId,    // z route — wstrzykiwane przez kontroler
    Guid? ParentGroupId,    // null = grupa główna
    int Order               // pozycja na liście
) : IRequestCommand<Guid>, IAuthorizableRequest
{
    public Guid TenantId { get; init; }    // z route
    public Guid ProjectId { get; init; }   // z route
}
```

**Pola body (JSON):**

| Pole | Typ | Wymagane | Opis |
|---|---|---|---|
| `parentGroupId` | `guid \| null` | Nie | `null` = grupa główna |
| `order` | `int` | Tak | Pozycja na liście |

> `costEstimateId`, `tenantId`, `projectId` — injektowane z parametrów route przez kontroler, **nie wysyłaj ich w body**.

---

### 2.3 Zmieniona komenda: `AddCostEstimateItemCommand`

**Plik:** `CQRS/CostEstimates/AddCostEstimateItem/AddCostEstimateItemCommand.cs`

| | Przed | Po |
|---|---|---|
| Typ zwracany | `IRequestCommand<AddCostEstimateItemResultWeb>` | `IRequestCommand<Guid>` |
| Handler tworzy domyślne `FieldValues` | ✅ tak | ❌ nie |
| Handler wstrzykuje `IRepository<CostEstimateItemFieldValue>` | ✅ tak | ❌ nie |
| Handler wstrzykuje `ICostEstimateService` | ✅ tak | ❌ nie |

```csharp
// Aktualna sygnatura komendy
public sealed record AddCostEstimateItemCommand(
    Guid CostEstimateId,            // z route — wstrzykiwane przez kontroler
    Guid GroupId,                   // ID grupy do której należy pozycja
    Guid? ParentItemId,             // null = pozycja główna
    ItemRelationType RelationType,  // 0=None, 1=Component, 2=Option
    int Order                       // pozycja na liście
) : IRequestCommand<Guid>, IAuthorizableRequest
{
    public Guid TenantId { get; init; }    // z route
    public Guid ProjectId { get; init; }   // z route
}
```

**Pola body (JSON):**

| Pole | Typ | Wymagane | Opis |
|---|---|---|---|
| `groupId` | `guid` | Tak | Grupa do której należy pozycja |
| `parentItemId` | `guid \| null` | Nie | `null` = pozycja główna (`None`) |
| `relationType` | `int` | Tak | `0`=None, `1`=Component, `2`=Option |
| `order` | `int` | Tak | Pozycja na liście |

---

### 2.4 Nowa komenda: `UpsertCostEstimateGroupFieldCommand`

**Plik:** `CQRS/CostEstimates/UpsertCostEstimateGroupField/UpsertCostEstimateGroupFieldCommand.cs`

Zastępuje dwie usunięte komendy. Jeden endpoint, jedna komenda — handler rozgałęzia się wewnętrznie na podstawie `FieldValueId`.

```csharp
public sealed record UpsertCostEstimateGroupFieldCommand : IRequestCommand<Guid>, IAuthorizableRequest
{
    public Guid CostEstimateId { get; init; }      // z route
    public Guid GroupId { get; init; }             // z route
    public Guid? FieldValueId { get; init; }       // null = utwórz, guid = zaktualizuj
    public Guid? FieldDefinitionId { get; init; }  // wymagane gdy FieldValueId == null
    public string? StringValue { get; init; }
    public decimal? DecimalValue { get; init; }
    public bool? BoolValue { get; init; }
    public DateTime? DateTimeValue { get; init; }
    public Guid TenantId { get; init; }            // z route
    public Guid ProjectId { get; init; }           // z route
}
```

**Pola body (JSON):**

| Pole | Typ | Wymagane | Opis |
|---|---|---|---|
| `fieldValueId` | `guid \| null` | Tak | `null` = utwórz nowe, `guid` = zaktualizuj |
| `fieldDefinitionId` | `guid \| null` | Warunkowo | **Wymagane** gdy `fieldValueId` jest `null` |
| `stringValue` | `string \| null` | Nie | Dla pól tekstowych |
| `decimalValue` | `number \| null` | Nie | Dla pól liczbowych |
| `boolValue` | `boolean \| null` | Nie | Dla pól logicznych |
| `dateTimeValue` | `string (ISO 8601) \| null` | Nie | Dla pól daty |

**Reguły walidacji (FluentValidation):**

| Pole | Reguła |
|---|---|
| `CostEstimateId` | `NotEmpty` — zawsze |
| `GroupId` | `NotEmpty` — zawsze |
| `FieldDefinitionId` | `NotEmpty` — **tylko gdy** `FieldValueId == null` |

**Logika handlera:**

```
if (FieldValueId == null)
    → ścieżka ADD:
        1. Sprawdź istnienie szablonu i definicji pola (FieldDefinitionId)
        2. Sprawdź czy pole już istnieje (→ 409 Conflict)
        3. Utwórz CostEstimateGroupFieldValue
        4. Unieważnij cache grupy
        5. Zwróć nowe fieldValue.Id

else
    → ścieżka UPDATE:
        1. Załaduj CostEstimateGroupFieldValue po FieldValueId + GroupId (Include FieldDefinition)
        2. Zaktualizuj wartość
        3. Unieważnij cache grupy
        4. Zwróć ten sam fieldValue.Id
```

**Zwracany typ:** `Guid` — ID wartości pola (nowej lub zaktualizowanej).

---

### 2.5 Nowa komenda: `UpsertCostEstimateItemFieldCommand`

**Plik:** `CQRS/CostEstimates/UpsertCostEstimateItemField/UpsertCostEstimateItemFieldCommand.cs`

```csharp
public sealed record UpsertCostEstimateItemFieldCommand : IRequestCommand<Guid>, IAuthorizableRequest
{
    public Guid CostEstimateId { get; init; }      // z route
    public Guid ItemId { get; init; }              // z route
    public Guid? FieldValueId { get; init; }       // null = utwórz, guid = zaktualizuj
    public Guid? FieldDefinitionId { get; init; }  // wymagane gdy FieldValueId == null
    public string? StringValue { get; init; }
    public decimal? DecimalValue { get; init; }
    public bool? BoolValue { get; init; }
    public DateTime? DateTimeValue { get; init; }
    public Guid TenantId { get; init; }            // z route
    public Guid ProjectId { get; init; }           // z route
}
```

**Pola body (JSON):**

| Pole | Typ | Wymagane | Opis |
|---|---|---|---|
| `fieldValueId` | `guid \| null` | Tak | `null` = utwórz nowe, `guid` = zaktualizuj |
| `fieldDefinitionId` | `guid \| null` | Warunkowo | **Wymagane** gdy `fieldValueId` jest `null` |
| `stringValue` | `string \| null` | Nie | Dla pól tekstowych |
| `decimalValue` | `number \| null` | Nie | Dla pól liczbowych |
| `boolValue` | `boolean \| null` | Nie | Dla pól logicznych |
| `dateTimeValue` | `string (ISO 8601) \| null` | Nie | Dla pól daty |

**Reguły walidacji (FluentValidation):**

| Pole | Reguła |
|---|---|
| `CostEstimateId` | `NotEmpty` — zawsze |
| `ItemId` | `NotEmpty` — zawsze |
| `FieldDefinitionId` | `NotEmpty` — **tylko gdy** `FieldValueId == null` |

**Logika handlera:**

```
if (FieldValueId == null)
    → ścieżka ADD:
        1. Sprawdź szablon i definicję pola (SystemFields ∪ CalculatedFields ∪ GenericFields)
        2. Walidacja VatRate: jeśli FieldType == ItemCalculatedVatRate → zakres 0.0–1.0
        3. Sprawdź czy pole już istnieje (→ 409 Conflict)
        4. Utwórz CostEstimateItemFieldValue
        5. Unieważnij cache pozycji
        6. Zwróć nowe fieldValue.Id

else
    → ścieżka UPDATE:
        1. Załaduj CostEstimateItemFieldValue po FieldValueId + ItemId (Include FieldDefinition)
        2. Zaktualizuj wartość
        3. Unieważnij cache pozycji
        4. Zwróć ten sam fieldValue.Id
```

**Zwracany typ:** `Guid` — ID wartości pola.

> ⚠️ Specjalne pole: `ItemCalculatedVatRate` — `decimalValue` musi być w zakresie `0.0`–`1.0`. Wartość `0.23` = 23% VAT.

---

## 3. Dodawanie grupy — `POST /{id:guid}/groups`

### Request

```http
POST api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/groups
Authorization: Bearer {token}
Content-Type: application/json
```

```json
{
  "parentGroupId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "order": 1
}
```

| Pole | Typ | Wymagane | Opis |
|---|---|---|---|
| `parentGroupId` | `guid \| null` | Nie | ID grupy nadrzędnej. `null` = grupa główna |
| `order` | `int` | Tak | Pozycja na liście |

### Response

```
HTTP 201 Created
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

Odpowiedź to **bezpośrednio Guid** (string JSON) nowo utworzonej grupy.

---

## 4. Dodawanie pozycji — `POST /{id:guid}/items`

### Request

```http
POST api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/items
Authorization: Bearer {token}
Content-Type: application/json
```

```json
{
  "groupId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "parentItemId": null,
  "relationType": 0,
  "order": 1
}
```

| Pole | Typ | Wymagane | Opis |
|---|---|---|---|
| `groupId` | `guid` | Tak | Grupa do której należy pozycja |
| `parentItemId` | `guid \| null` | Nie | ID pozycji nadrzędnej (dla opcji/komponentów) |
| `relationType` | `int` | Tak | Typ relacji — patrz [enum ItemRelationType](#itemrelationtype) |
| `order` | `int` | Tak | Pozycja na liście |

### Response

```
HTTP 201 Created
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

Odpowiedź to **bezpośrednio Guid** nowo utworzonej pozycji.

---

## 5. Pola grupy — `PATCH /{id:guid}/groups/{groupId:guid}/fields`

Jeden endpoint obsługuje zarówno **tworzenie nowego pola** jak i **aktualizację istniejącego**.

```http
PATCH api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/groups/{groupId}/fields
Authorization: Bearer {token}
Content-Type: application/json
```

### Logika

| `fieldValueId` | Operacja | Wymagane dodatkowe pole |
|---|---|---|
| `null` | **Utwórz** nową wartość pola | `fieldDefinitionId` (wymagane) |
| `"guid"` | **Zaktualizuj** istniejącą wartość pola | — |

### Request Body

```json
{
  "fieldValueId": null,
  "fieldDefinitionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "stringValue": "Nazwa grupy",
  "decimalValue": null,
  "boolValue": null,
  "dateTimeValue": null
}
```

| Pole | Typ | Opis |
|---|---|---|
| `fieldValueId` | `guid \| null` | `null` = utwórz nowe, `guid` = zaktualizuj istniejące |
| `fieldDefinitionId` | `guid \| null` | Wymagane gdy `fieldValueId` jest `null` |
| `stringValue` | `string \| null` | Dla typów: `GroupName`, `GroupDescription`, `GroupNumber`, `GroupStatus`, `GroupNotes`, `GroupResponsible` |
| `decimalValue` | `number \| null` | Dla typów: `GroupBudget`, `GroupPriority` |
| `boolValue` | `boolean \| null` | Brak zastosowania dla grup w obecnej wersji |
| `dateTimeValue` | `string (ISO 8601) \| null` | Dla typów: `GroupStartDate`, `GroupEndDate` |

> ⚠️ Wysyłaj tylko pole pasujące do `FieldType` definicji. Pozostałe ustaw na `null`.

### Response

```
HTTP 200 OK
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

Zawsze zwraca `Guid` wartości pola (nowo utworzonej lub zaktualizowanej).  
**UI powinien zapisać zwrócony `fieldValueId`** — przy kolejnym autosave przekazuje go jako `fieldValueId` w body.

---

## 6. Pola pozycji — `PATCH /{id:guid}/items/{itemId:guid}/fields`

Jeden endpoint obsługuje zarówno **tworzenie nowego pola** jak i **aktualizację istniejącego**. Działa dla pozycji głównych, opcji i komponentów.

```http
PATCH api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/items/{itemId}/fields
Authorization: Bearer {token}
Content-Type: application/json
```

### Logika

| `fieldValueId` | Operacja | Wymagane dodatkowe pole |
|---|---|---|
| `null` | **Utwórz** nową wartość pola | `fieldDefinitionId` (wymagane) |
| `"guid"` | **Zaktualizuj** istniejącą wartość pola | — |

### Request Body

```json
{
  "fieldValueId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fieldDefinitionId": null,
  "stringValue": null,
  "decimalValue": 1250.50,
  "boolValue": null,
  "dateTimeValue": null
}
```

| Pole | Typ | Opis |
|---|---|---|
| `fieldValueId` | `guid \| null` | `null` = utwórz nowe, `guid` = zaktualizuj istniejące |
| `fieldDefinitionId` | `guid \| null` | Wymagane gdy `fieldValueId` jest `null` |
| `stringValue` | `string \| null` | Dla typów: `ItemSystemName`, `ItemSystemUnit`, `ItemGenericString` |
| `decimalValue` | `number \| null` | Dla typów: `ItemSystemQuantity`, `ItemCalculatedUnitPriceNet`, `ItemCalculatedVatRate` *(zakres `0`–`1`)*, `ItemCalculatedUnitPriceGross`, `ItemCalculatedValueNet`, `ItemCalculatedValueGross`, `ItemCalculatedUnitVat`, `ItemCalculatedTotalVat`, `ItemGenericNumber` |
| `boolValue` | `boolean \| null` | Dla typów: `ItemSystemSelected`, `ItemGenericBoolean` |
| `dateTimeValue` | `string (ISO 8601) \| null` | Dla typów: `ItemGenericDate` |

> ⚠️ Pola `ItemSystemFiles` i `ItemSystemOptions` **nie są** obsługiwane przez ten endpoint. Pliki — użyj `POST /{id}/items/{itemId}/files`.

> ⚠️ Ten endpoint **nie wyzwala przeliczenia** kosztorysu. Po zakończeniu edycji pól wywołaj osobno `POST /{id}/recalculate`.

### Response

```
HTTP 200 OK
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

Zawsze zwraca `Guid` wartości pola.

---

## 7. Enums referencyjne

### `ItemRelationType`

| Wartość | Nazwa | Opis |
|---|---|---|
| `0` | `None` | Główna pozycja (praca / pozycja kosztorysowa) |
| `1` | `Component` | Komponent pozycji głównej (tylko pozycja `None` może mieć komponenty) |
| `2` | `Option` | Wariant/opcja (komponenty i pozycje `None` mogą mieć opcje; opcje **nie mogą** mieć własnych opcji) |

### `FieldType` — pola grupy (scope: `Group`, wartości `0`–`9`)

| Wartość | Nazwa | Typ wartości |
|---|---|---|
| `0` | `GroupName` | `stringValue` |
| `1` | `GroupDescription` | `stringValue` |
| `2` | `GroupNumber` | `stringValue` |
| `3` | `GroupStartDate` | `dateTimeValue` |
| `4` | `GroupEndDate` | `dateTimeValue` |
| `5` | `GroupStatus` | `stringValue` |
| `6` | `GroupNotes` | `stringValue` |
| `7` | `GroupResponsible` | `stringValue` |
| `8` | `GroupBudget` | `decimalValue` |
| `9` | `GroupPriority` | `decimalValue` |

### `FieldType` — pola pozycji (scope: `ItemSystem`, `ItemCalculated`, `ItemGeneric`)

| Wartość | Nazwa | Typ wartości | Uwagi |
|---|---|---|---|
| `100` | `ItemSystemName` | `stringValue` | |
| `101` | `ItemSystemQuantity` | `decimalValue` | |
| `102` | `ItemSystemUnit` | `stringValue` | |
| `103` | `ItemSystemOptions` | — | kolekcja, tylko odczyt przez GET |
| `104` | `ItemSystemSelected` | `boolValue` | |
| `105` | `ItemSystemFiles` | — | tylko przez `POST /{id}/items/{itemId}/files` |
| `200` | `ItemCalculatedUnitPriceNet` | `decimalValue` | |
| `201` | `ItemCalculatedVatRate` | `decimalValue` | zakres `0.0`–`1.0` (np. `0.23` = 23%) |
| `202` | `ItemCalculatedUnitPriceGross` | `decimalValue` | |
| `203` | `ItemCalculatedValueNet` | `decimalValue` | |
| `204` | `ItemCalculatedValueGross` | `decimalValue` | |
| `205` | `ItemCalculatedUnitVat` | `decimalValue` | |
| `206` | `ItemCalculatedTotalVat` | `decimalValue` | |
| `300` | `ItemGenericNumber` | `decimalValue` | |
| `301` | `ItemGenericString` | `stringValue` | |
| `302` | `ItemGenericBoolean` | `boolValue` | |

---

## 8. Przykłady wywołań

### Przepływ: dodanie nowej grupy i autosave pierwszego pola

```
1. POST  /{id}/groups
         body: { parentGroupId: null, order: 3 }
         → "aaa-..."  (nowe groupId)

2. PATCH /{id}/groups/aaa.../fields
         body: { fieldValueId: null, fieldDefinitionId: "def-...", stringValue: "Fundamenty" }
         → "bbb-..."  (nowe fieldValueId — zapisz w stanie UI)

3. PATCH /{id}/groups/aaa.../fields
         body: { fieldValueId: "bbb-...", stringValue: "Fundamenty żelbetowe" }
         → "bbb-..."  (kolejny autosave — fieldValueId już znane)
```

### Przepływ: dodanie nowej pozycji i autosave

```
1. POST  /{id}/items
         body: { groupId: "aaa-...", parentItemId: null, relationType: 0, order: 1 }
         → "ccc-..."  (nowe itemId)

2. PATCH /{id}/items/ccc.../fields
         body: { fieldValueId: null, fieldDefinitionId: "def-name", stringValue: "Beton C20/25" }
         → "ddd-..."  (fieldValueId dla pola nazwy — zapisz w stanie UI)

3. PATCH /{id}/items/ccc.../fields
         body: { fieldValueId: null, fieldDefinitionId: "def-qty", decimalValue: 10.5 }
         → "eee-..."  (fieldValueId dla pola ilości — zapisz w stanie UI)

4. POST  /{id}/recalculate   ← po zakończeniu edycji
```

### Przepływ: edycja istniejącego pola (autosave)

```
PATCH /{id}/items/{itemId}/fields
body: { fieldValueId: "eee-...", decimalValue: 12.0 }
→ "eee-..."
```

> `fieldValueId` w stanie UI można pobrać z `GET /details/{id}` → `groups[].items[].fieldValues[].id`


---

## 1. Zmiany przełomowe (breaking changes)

### 1.1 Dodawanie grupy i pozycji — zmiana odpowiedzi

| Endpoint | Stara odpowiedź | Nowa odpowiedź |
|---|---|---|
| `POST /{id}/groups` | `{ groupId, fieldValues[] }` | `"3fa85f64-..."` *(tylko Guid)* |
| `POST /{id}/items` | `{ itemId, fieldValues[] }` | `"3fa85f64-..."` *(tylko Guid)* |

**Uzasadnienie:** API nie tworzy już domyślnych pustych wierszy pól w bazie przy dodaniu grupy/pozycji. Pola będą tworzone przez UI dopiero przy pierwszym zapisie wartości (autosave). `fieldValues[]` nie jest już zwracane — definicje pól należy odczytać z `templateStructure` w odpowiedzi `GET /details/{id}`.

**Wpływ na UI:**
- Po `POST /{id}/groups` → odpowiedź to `Guid` (string w JSON), nie obiekt
- Po `POST /{id}/items` → odpowiedź to `Guid` (string w JSON), nie obiekt
- UI powinien renderować pola na podstawie `templateStructure` z `GET /details/{id}`, nie na podstawie zwróconych `fieldValues`
- Przy pierwszym autosave pola dla nowej grupy/pozycji — `FieldValueId` wysyłamy jako `null`

---

### 1.2 Upsert pól — zastąpienie 4 endpointów przez 2

Usunięte endpointy:

| Stary endpoint | Zastąpiony przez |
|---|---|
| `PATCH /{id}/groups/{groupId}/fields/{fieldValueId}` | `PATCH /{id}/groups/{groupId}/fields` |
| `POST /{id}/groups/{groupId}/fields` | `PATCH /{id}/groups/{groupId}/fields` |
| `PATCH /{id}/items/{itemId}/fields/{fieldValueId}` | `PATCH /{id}/items/{itemId}/fields` |
| `POST /{id}/items/{itemId}/fields` | `PATCH /{id}/items/{itemId}/fields` |

---

## 2. Dodawanie grupy — `POST /{id:guid}/groups`

### Request

```http
POST api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/groups
Authorization: Bearer {token}
Content-Type: application/json
```

```json
{
  "parentGroupId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "order": 1
}
```

| Pole | Typ | Wymagane | Opis |
|---|---|---|---|
| `parentGroupId` | `guid \| null` | Nie | ID grupy nadrzędnej. `null` = grupa główna |
| `order` | `int` | Tak | Pozycja na liście |

### Response

```
HTTP 201 Created
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

Odpowiedź to **bezpośrednio Guid** (string JSON) nowo utworzonej grupy.

---

## 3. Dodawanie pozycji — `POST /{id:guid}/items`

### Request

```http
POST api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/items
Authorization: Bearer {token}
Content-Type: application/json
```

```json
{
  "groupId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "parentItemId": null,
  "relationType": 0,
  "order": 1
}
```

| Pole | Typ | Wymagane | Opis |
|---|---|---|---|
| `groupId` | `guid` | Tak | Grupa do której należy pozycja |
| `parentItemId` | `guid \| null` | Nie | ID pozycji nadrzędnej (dla opcji/komponentów) |
| `relationType` | `int` | Tak | Typ relacji — patrz [enum ItemRelationType](#itemrelationtype) |
| `order` | `int` | Tak | Pozycja na liście |

### Response

```
HTTP 201 Created
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

Odpowiedź to **bezpośrednio Guid** nowo utworzonej pozycji.

---

## 4. Pola grupy — `PATCH /{id:guid}/groups/{groupId:guid}/fields`

Jeden endpoint obsługuje zarówno **tworzenie nowego pola** jak i **aktualizację istniejącego**.

```http
PATCH api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/groups/{groupId}/fields
Authorization: Bearer {token}
Content-Type: application/json
```

### Logika

| `fieldValueId` | Operacja | Wymagane dodatkowe pole |
|---|---|---|
| `null` | **Utwórz** nową wartość pola | `fieldDefinitionId` (wymagane) |
| `"guid"` | **Zaktualizuj** istniejącą wartość pola | — |

### Request Body

```json
{
  "fieldValueId": null,
  "fieldDefinitionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "stringValue": "Nazwa grupy",
  "decimalValue": null,
  "boolValue": null,
  "dateTimeValue": null
}
```

| Pole | Typ | Opis |
|---|---|---|
| `fieldValueId` | `guid \| null` | `null` = utwórz nowe, `guid` = zaktualizuj istniejące |
| `fieldDefinitionId` | `guid \| null` | Wymagane gdy `fieldValueId` jest `null` |
| `stringValue` | `string \| null` | Dla typów: `GroupName`, `GroupDescription`, `GroupNumber`, `GroupStatus`, `GroupNotes`, `GroupResponsible` |
| `decimalValue` | `number \| null` | Dla typów: `GroupBudget` |
| `boolValue` | `boolean \| null` | Brak zastosowania dla grup w obecnej wersji |
| `dateTimeValue` | `string (ISO 8601) \| null` | Dla typów: `GroupStartDate`, `GroupEndDate` |

> ⚠️ Wysyłaj tylko pole pasujące do `FieldType` definicji. Pozostałe ustaw na `null`.

### Response

```
HTTP 200 OK
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

Zawsze zwraca `Guid` wartości pola (nowo utworzonej lub zaktualizowanej).  
**UI powinien zapisać zwrócony `fieldValueId`** — przy kolejnym autosave pola przekazuje go jako `fieldValueId` w body.

---

## 5. Pola pozycji — `PATCH /{id:guid}/items/{itemId:guid}/fields`

Jeden endpoint obsługuje zarówno **tworzenie nowego pola** jak i **aktualizację istniejącego**. Działa dla pozycji głównych, opcji i komponentów.

```http
PATCH api/tenants/{tenantId}/project/{projectId}/cost-estimate/{id}/items/{itemId}/fields
Authorization: Bearer {token}
Content-Type: application/json
```

### Logika

| `fieldValueId` | Operacja | Wymagane dodatkowe pole |
|---|---|---|
| `null` | **Utwórz** nową wartość pola | `fieldDefinitionId` (wymagane) |
| `"guid"` | **Zaktualizuj** istniejącą wartość pola | — |

### Request Body

```json
{
  "fieldValueId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fieldDefinitionId": null,
  "stringValue": null,
  "decimalValue": 1250.50,
  "boolValue": null,
  "dateTimeValue": null
}
```

| Pole | Typ | Opis |
|---|---|---|
| `fieldValueId` | `guid \| null` | `null` = utwórz nowe, `guid` = zaktualizuj istniejące |
| `fieldDefinitionId` | `guid \| null` | Wymagane gdy `fieldValueId` jest `null` |
| `stringValue` | `string \| null` | Dla typów: `ItemSystemName`, `ItemSystemUnit`, `ItemGenericString` |
| `decimalValue` | `number \| null` | Dla typów: `ItemSystemQuantity`, `ItemCalculatedUnitPriceNet`, `ItemCalculatedVatRate` (zakres `0`–`1`), `ItemCalculatedUnitPriceGross`, `ItemCalculatedValueNet`, `ItemCalculatedValueGross`, `ItemGenericNumber` |
| `boolValue` | `boolean \| null` | Dla typów: `ItemSystemSelected`, `ItemGenericBoolean` |
| `dateTimeValue` | `string (ISO 8601) \| null` | Dla typów: `ItemGenericDate` |

> ⚠️ Pola `ItemSystemFiles` i `ItemSystemOptions` **nie są** obsługiwane przez ten endpoint. Pliki — użyj `POST /{id}/items/{itemId}/files`.

> ⚠️ Ten endpoint **nie wyzwala przeliczenia** kosztorysu. Po zakończeniu edycji pól wywołaj osobno `POST /{id}/recalculate`.

### Response

```
HTTP 200 OK
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

Zawsze zwraca `Guid` wartości pola.

---

## 6. Enums referencyjne

### `ItemRelationType`

| Wartość | Nazwa | Opis |
|---|---|---|
| `0` | `None` | Główna pozycja (praca / pozycja kosztorysowa) |
| `1` | `Component` | Komponent pozycji głównej (tylko pozycja `None` może mieć komponenty) |
| `2` | `Option` | Wariant/opcja (komponenty i pozycje `None` mogą mieć opcje; opcje **nie mogą** mieć własnych opcji) |

### `FieldType` — pola grupy (scope: `Group`, wartości `0`–`9`)

| Wartość | Nazwa | Typ wartości |
|---|---|---|
| `0` | `GroupName` | `stringValue` |
| `1` | `GroupDescription` | `stringValue` |
| `2` | `GroupNumber` | `stringValue` |
| `3` | `GroupStartDate` | `dateTimeValue` |
| `4` | `GroupEndDate` | `dateTimeValue` |
| `5` | `GroupStatus` | `stringValue` |
| `6` | `GroupNotes` | `stringValue` |
| `7` | `GroupResponsible` | `stringValue` |
| `8` | `GroupBudget` | `decimalValue` |
| `9` | `GroupPriority` | `decimalValue` |

### `FieldType` — pola pozycji (scope: `ItemSystem`, `ItemCalculated`, `ItemGeneric`)

| Wartość | Nazwa | Typ wartości | Uwagi |
|---|---|---|---|
| `100` | `ItemSystemName` | `stringValue` | |
| `101` | `ItemSystemQuantity` | `decimalValue` | |
| `102` | `ItemSystemUnit` | `stringValue` | |
| `103` | `ItemSystemOptions` | — | kolekcja, tylko odczyt przez GET |
| `104` | `ItemSystemSelected` | `boolValue` | |
| `105` | `ItemSystemFiles` | — | tylko przez `POST /{id}/items/{itemId}/files` |
| `200` | `ItemCalculatedUnitPriceNet` | `decimalValue` | |
| `201` | `ItemCalculatedVatRate` | `decimalValue` | zakres `0.0`–`1.0` (np. `0.23` = 23%) |
| `202` | `ItemCalculatedUnitPriceGross` | `decimalValue` | |
| `203` | `ItemCalculatedValueNet` | `decimalValue` | |
| `204` | `ItemCalculatedValueGross` | `decimalValue` | |
| `205` | `ItemCalculatedUnitVat` | `decimalValue` | |
| `206` | `ItemCalculatedTotalVat` | `decimalValue` | |
| `300` | `ItemGenericNumber` | `decimalValue` | |
| `301` | `ItemGenericString` | `stringValue` | |
| `302` | `ItemGenericBoolean` | `boolValue` | |

---

## 7. Przykłady wywołań

### Przepływ: dodanie nowej grupy i autosave pierwszego pola

```
1. POST  /{id}/groups
         body: { parentGroupId: null, order: 3 }
         → "aaa-..."  (nowe groupId)

2. PATCH /{id}/groups/aaa.../fields
         body: { fieldValueId: null, fieldDefinitionId: "def-...", stringValue: "Fundamenty" }
         → "bbb-..."  (nowe fieldValueId — zapisz w stanie UI)

3. PATCH /{id}/groups/aaa.../fields
         body: { fieldValueId: "bbb-...", stringValue: "Fundamenty żelbetowe" }
         → "bbb-..."  (kolejny autosave — fieldValueId już znane)
```

### Przepływ: dodanie nowej pozycji i autosave

```
1. POST  /{id}/items
         body: { groupId: "aaa-...", parentItemId: null, relationType: 0, order: 1 }
         → "ccc-..."  (nowe itemId)

2. PATCH /{id}/items/ccc.../fields
         body: { fieldValueId: null, fieldDefinitionId: "def-name", stringValue: "Beton C20/25" }
         → "ddd-..."  (fieldValueId dla pola nazwy)

3. PATCH /{id}/items/ccc.../fields
         body: { fieldValueId: null, fieldDefinitionId: "def-qty", decimalValue: 10.5 }
         → "eee-..."  (fieldValueId dla pola ilości)

4. POST  /{id}/recalculate   ← po zakończeniu edycji
```

### Przepływ: edycja istniejącego pola (autosave)

```
PATCH /{id}/items/{itemId}/fields
body: { fieldValueId: "eee-...", decimalValue: 12.0 }
→ "eee-..."
```

> `fieldValueId` w stanie UI można pobrać z `GET /details/{id}` → `groups[].items[].fieldValues[].id`
