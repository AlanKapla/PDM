# Cost Estimate API — zmiany: Add/Update GroupField & ItemField

---

## Nowe endpointy: Add/Update GroupField & ItemField

### Dodawanie wartości pola grupy (AddCostEstimateGroupField)

```
POST /api/tenants/{tenantId}/project/{projectId}/cost-estimate/{costEstimateId}/groups/{groupId}/fields
```

**Body:**
```json
{
  "fieldDefinitionId": "{guid}",
  "stringValue": "...",
  "decimalValue": 123.45,
  "boolValue": true,
  "dateTimeValue": "2024-06-01T00:00:00Z"
}
```

**Response:**
- `201 Created` — zwraca `fieldValueId` (Guid)
- `409 Conflict` — pole już istnieje

### Aktualizacja wartości pola grupy (UpdateCostEstimateGroupField)

```
PATCH /api/tenants/{tenantId}/project/{projectId}/cost-estimate/{costEstimateId}/groups/{groupId}/fields/{fieldValueId}
```

**Body:**
```json
{
  "stringValue": "...",
  "decimalValue": 123.45,
  "boolValue": true,
  "dateTimeValue": "2024-06-01T00:00:00Z"
}
```

**Response:**
- `204 No Content` — sukces
- `404 Not Found` — pole nie istnieje

---

### Dodawanie wartości pola pozycji (AddCostEstimateItemField)

```
POST /api/tenants/{tenantId}/project/{projectId}/cost-estimate/{costEstimateId}/items/{itemId}/fields
```

**Body:**
```json
{
  "fieldDefinitionId": "{guid}",
  "stringValue": "...",
  "decimalValue": 123.45,
  "boolValue": true,
  "dateTimeValue": "2024-06-01T00:00:00Z"
}
```

**Response:**
- `201 Created` — zwraca `fieldValueId` (Guid)
- `409 Conflict` — pole już istnieje

### Aktualizacja wartości pola pozycji (UpdateCostEstimateItemField)

```
PATCH /api/tenants/{tenantId}/project/{projectId}/cost-estimate/{costEstimateId}/items/{itemId}/fields/{fieldValueId}
```

**Body:**
```json
{
  "stringValue": "...",
  "decimalValue": 123.45,
  "boolValue": true,
  "dateTimeValue": "2024-06-01T00:00:00Z"
}
```

**Response:**
- `204 No Content` — sukces
- `404 Not Found` — pole nie istnieje

---

## Zasady działania

- **Add**: Tworzy nową wartość pola dla wskazanej definicji (`fieldDefinitionId`). Jeśli już istnieje — zwraca `409 Conflict`.
- **Update**: Aktualizuje istniejącą wartość pola (`fieldValueId`). Jeśli nie istnieje — `404 Not Found`.
- Oba przypadki walidują tenantId, projectId, uprawnienia i istnienie encji.
- Wartości są typowane (`stringValue`, `decimalValue`, `boolValue`, `dateTimeValue`) — tylko jedno pole powinno być ustawione zgodnie z typem definicji pola.
- Po każdej operacji cache wartości pól jest automatycznie odświeżany.

---

## Przykładowy flow UI

1. Użytkownik dodaje nowe pole do szablonu (np. Discount)
2. W istniejącym kosztorysie pole nie ma jeszcze wartości — UI wywołuje **POST** `/fields` z `fieldDefinitionId`
3. Użytkownik edytuje wartość — UI wywołuje **PATCH** `/fields/{fieldValueId}`
4. Backend waliduje typy, uprawnienia, tenantId, projectId
5. Po zmianie wartości UI może wywołać **POST /recalculate** aby przeliczyć sumy
