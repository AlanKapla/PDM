# Cost Estimate Template API — dokumentacja dla UI

> Base URL: `api/cost-estimate-template`

---

## 📋 Pełna lista endpointów

| Metoda | URL | Opis |
|---|---|---|
| `GET` | `/` | Lista szablonów użytkownika |
| `GET` | `/field-type-configurations` | Konfiguracja typów pól (metadata) |
| `GET` | `/defaults` | Lista domyślnych szablonów systemowych |
| `GET` | `/defaults/{slug}` | Szczegóły domyślnego szablonu |
| `GET` | `/{id}` | Szczegóły szablonu z pełną strukturą |
| `POST` | `/` | Utwórz nowy szablon |
| `POST` | `/from-default` | Utwórz szablon z domyślnego (kopiuje strukturę) |
| `PUT` | `/{id}` | Aktualizuj szablon (metadata + struktura pól) |
| **`DELETE`** | **`/{id}`** | **Usuń szablon (soft delete)** ← NOWY |
| **`POST`** | **`/{id}/duplicate`** | **Duplikuj szablon** ← NOWY |

---

## 🆕 Nowy endpoint: Delete Template

```
DELETE /{id}
```

**Opis:** Soft-delete szablonu kosztorysu. Tylko właściciel szablonu może go usunąć. Istniejące kosztorysy korzystające z tego szablonu nie są modyfikowane — nadal działają z zapamiętaną strukturą.

**Request body:** brak

**Response:** `204 No Content`

**Walidacje:**
- Template ID musi być podane (nie może być puste)
- Szablon musi istnieć i nie być już usunięty
- Tylko właściciel (`OwnerId == currentUser.Id`) może usunąć szablon — w przeciwnym razie `404`

**Przykład:**
```http
DELETE /api/cost-estimate-template/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer {token}
```

**Co się dzieje po usunięciu:**
- Szablon oznaczony jako `IsDeleted = true`, `DeletedAt = UTC now`
- Cache szablonu (`platform:template:{id}`) zostaje zinwalidowany
- Szablon nie pojawia się na liście (`GET /`)
- Kosztorysy z tym szablonem nadal działają (FK `Restrict`, soft delete nie usuwa fizycznie)
- Szablon nie może być ponownie użyty do tworzenia nowych kosztorysów

---

## 🔄 Sugerowany flow UI

### Usuwanie szablonu:
```
1. Użytkownik klika "Usuń" na liście szablonów
2. Modal potwierdzenia: "Czy na pewno chcesz usunąć szablon X?"
   (opcjonalnie: informacja ile kosztorysów korzysta z szablonu)
3. DELETE /{id}
4. Response: 204 No Content
5. Odśwież listę szablonów (GET /)
```

### Obsługa błędów:
| HTTP Status | Znaczenie | Akcja UI |
|---|---|---|
| `204` | Sukces | Odśwież listę |
| `404` | Szablon nie istnieje lub nie jest własnością użytkownika | Toast: "Szablon nie został znaleziony" |
| `401` | Brak autoryzacji | Przekieruj do logowania |

---

## 🆕 Nowy endpoint: Duplicate Template

```
POST /{id}/duplicate
```

**Opis:** Tworzy kopię istniejącego szablonu z pełną strukturą (pola, waluty, jednostki). Nowe `fieldName` GUIDs generowane są po stronie serwera. Tylko właściciel szablonu może go duplikować.

**Request body:**
```json
{
  "name": "Kopia — Mój szablon",
  "description": "Opcjonalny opis"
}
```

**Response:** `201 Created`
```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

**Walidacje:**
- `sourceTemplateId` — ustawiany z route `{id}`, nie może być pusty
- `name` — wymagane, max 200 znaków
- `description` — opcjonalne, max 2000 znaków
- Szablon źródłowy musi istnieć, nie być usunięty, i należeć do użytkownika — w przeciwnym razie `404`

**Co jest kopiowane:**
- Waluty (kody, nazwy, symbole, kolejność)
- Jednostki (kody, nazwy, symbole, kategorie)
- Wszystkie definicje pól (group, system, calculated, generic) z hierarchią child fields
- Konfiguracja szablonu (canAddGroups, canBranchGroups, maxGroupLevel, autoNumberGroups, groupNumberFormat, category)
- UI column layout (kolejność kolumn)

**Co NIE jest kopiowane:**
- Kosztorysy utworzone na podstawie szablonu źródłowego
- ID pól (nowe GUIDs generowane po stronie serwera)

**Przykład:**
```http
POST /api/cost-estimate-template/3fa85f64-5717-4562-b3fc-2c963f66afa6/duplicate
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Kopia — Budowlany",
  "description": null
}
```

---

## 🔄 Sugerowany flow UI

### Duplikowanie szablonu:
```
1. Użytkownik klika "Duplikuj" na liście szablonów
2. Modal z polem "Nazwa" (domyślnie: "Kopia — {oryginalName}")
3. POST /{id}/duplicate  →  { name: "Kopia — ...", description: null }
4. Response: 201 Created → nowy templateId
5. Przekieruj do edycji nowego szablonu (GET /{newId})
```
