# Dokumentacja techniczna: WorkScheduleController

## 1. Informacje ogólne

**Bazowa ścieżka:**
```
api/tenants/{tenantId}/project/{projectId}/work-schedule
```

**Cel kontrolera:** Zarządzanie harmonogramami prac (work schedules) w ramach projektu. Harmonogram składa się z drzewa etapów (`stages`), zakresów prac (`works`) przypisanych do etapów oraz zależności między zakresami (`dependencies`).

**Autoryzacja:** Wyłącznie polityki autoryzacyjne (policy-based). Rola przypisana do żądania jest weryfikowana na poziomie kontrolera oraz ponownie na poziomie handlera (tenant isolation).

---

## 2. Zestawienie endpointów

| Metoda HTTP | Ścieżka | Opis | Polityka |
|-------------|---------|------|----------|
| `POST` | `/` | Tworzy nowy harmonogram prac | `ProjectResourcesWrite` |
| `PUT` | `/{workScheduleId}` | Aktualizuje istniejący harmonogram | `ProjectResourcesWrite` |
| `GET` | `/{scope}` | Pobiera listę harmonogramów wg zakresu | `ProjectView` |
| `GET` | `/details/{workScheduleId}` | Pobiera pełne szczegóły harmonogramu | `ProjectResourcesReadSingle` |
| `DELETE` | `/{workScheduleId}` | Usuwa harmonogram (soft delete) | `ProjectResourcesWrite` |

**Parametry trasy wspólne dla wszystkich endpointów:**

| Parametr | Typ | Opis |
|----------|-----|------|
| `tenantId` | `Guid` | Identyfikator tenanta |
| `projectId` | `Guid` | Identyfikator projektu |

---

## 3. Szczegóły endpointów

### 3.1 `POST /` — Tworzenie harmonogramu

**Odpowiedź sukcesu:** `201 Created` + `WorkScheduleDetailsWeb`

**Body żądania:** `CreateWorkScheduleCommand`

| Pole | Typ | Wymagane | Opis |
|------|-----|----------|------|
| `name` | `string` | ✅ | Nazwa harmonogramu (max 200 znaków) |
| `costEstimateId` | `Guid?` | ❌ | ID powiązanego kosztorysu — włącza tryb synchronizacji (wyklucza ręczne `stages`) |
| `stages` | `WorkScheduleStageDto[]?` | ❌ | Drzewo etapów — używane w trybie ręcznym |
| `dependencies` | `WorkScheduleWorkDependencyDto[]?` | ❌ | Zależności między zakresami prac |

> `tenantId` i `projectId` są wstrzykiwane automatycznie z trasy — nie podawać w body.

---

### 3.2 `PUT /{workScheduleId}` — Aktualizacja harmonogramu

**Dodatkowy parametr trasy:**

| Parametr | Typ | Opis |
|----------|-----|------|
| `workScheduleId` | `Guid` | Identyfikator harmonogramu |

**Odpowiedź sukcesu:** `200 OK` + `WorkScheduleDetailsWeb`

**Body żądania:** `UpdateWorkScheduleCommand`

| Pole | Typ | Wymagane | Opis |
|------|-----|----------|------|
| `name` | `string` | ✅ | Nowa nazwa harmonogramu (max 200 znaków) |
| `stages` | `WorkScheduleStageDto[]?` | ❌ | Pełna lista etapów po aktualizacji — zastępuje istniejące |
| `dependencies` | `WorkScheduleWorkDependencyDto[]?` | ❌ | Pełna lista zależności — zastępuje istniejące |

> Nie można zmienić `costEstimateId` po utworzeniu — powiązanie z kosztorysem jest niezmienne.

---

### 3.3 `GET /{scope}` — Lista harmonogramów

**Dodatkowy parametr trasy:**

| Parametr | Typ | Opis |
|----------|-----|------|
| `scope` | `ResourceScope` | Filtr zakresu (patrz tabela poniżej) |

**Odpowiedź sukcesu:** `200 OK` + `WorkScheduleSummaryWeb[]`

**Wartości `ResourceScope`:**

| Wartość | Opis |
|---------|------|
| `All` | Wszystkie harmonogramy w projekcie |
| `Mine` | Tylko harmonogramy utworzone przez bieżącego użytkownika |
| `Shared` | ⚠️ Nieimplementowane — zawsze zwraca pustą tablicę `[]` |

---

### 3.4 `GET /details/{workScheduleId}` — Szczegóły harmonogramu

**Dodatkowy parametr trasy:**

| Parametr | Typ | Opis |
|----------|-----|------|
| `workScheduleId` | `Guid` | Identyfikator harmonogramu |

**Odpowiedź sukcesu:** `200 OK` + `WorkScheduleDetailsWeb`

---

### 3.5 `DELETE /{workScheduleId}` — Usunięcie harmonogramu

**Dodatkowy parametr trasy:**

| Parametr | Typ | Opis |
|----------|-----|------|
| `workScheduleId` | `Guid` | Identyfikator harmonogramu |

**Odpowiedź sukcesu:** `204 No Content` *(brak body)*

---

## 4. Modele danych wejściowych (DTOs)

### 4.1 `WorkScheduleStageDto` — Etap harmonogramu

| Pole | Typ | Wymagane | Opis |
|------|-----|----------|------|
| `id` | `Guid?` | ❌ | ID istniejącego etapu (aktualizacja); `null` = nowy etap |
| `name` | `string` | ✅ | Nazwa etapu (max 200 znaków) |
| `order` | `int` | ✅ | Kolejność wyświetlania (≥ 0) |
| `works` | `WorkScheduleWorkDto[]?` | ❌ | Zakresy prac należące do etapu |
| `children` | `WorkScheduleStageDto[]?` | ❌ | Podrzędne etapy (struktura rekurencyjna) |

---

### 4.2 `WorkScheduleWorkDto` — Zakres pracy

| Pole | Typ | Wymagane | Opis |
|------|-----|----------|------|
| `id` | `Guid?` | ❌ | ID istniejącego zakresu (aktualizacja); `null` = nowy |
| `tempId` | `Guid?` | ❌ | Tymczasowy UUID przypisany przez klienta — służy do referencji w `dependencies` dla nowych zakresów w tym samym żądaniu |
| `name` | `string` | ✅ | Nazwa zakresu (max 200 znaków) |
| `order` | `int` | ✅ | Kolejność (≥ 0) |
| `colorRgb` | `string` | ✅ | Kolor w formacie `rgb(r,g,b)` lub `#RRGGBB` (max 20 znaków) |
| `isClosed` | `bool` | ✅ | Czy zakres jest zamknięty |
| `periods` | `WorkScheduleWorkPeriodDto[]?` | ❌ | Okresy czasu przypisane do zakresu |
| `assignedUserIds` | `Guid[]?` | ❌ | Lista ID użytkowników przypisanych do zakresu |
| `comments` | `WorkScheduleWorkCommentDto[]?` | ❌ | Komentarze do zakresu pracy |

---

### 4.3 `WorkScheduleWorkPeriodDto` — Okres

| Pole | Typ | Wymagane | Opis |
|------|-----|----------|------|
| `id` | `Guid?` | ❌ | Ignorowane — okresy są zawsze w całości zastępowane |
| `startDate` | `DateTime` | ✅ | Data rozpoczęcia (UTC) |
| `endDate` | `DateTime` | ✅ | Data zakończenia (UTC, ≥ `startDate`) |
| `isClosed` | `bool` | ✅ | Czy okres jest zamknięty |

---

### 4.4 `WorkScheduleWorkCommentDto` — Komentarz

| Pole | Typ | Wymagane | Opis |
|------|-----|----------|------|
| `id` | `Guid?` | ❌ | Ignorowane — komentarze są w całości zastępowane przy każdym zapisie |
| `content` | `string` | ✅ | Treść komentarza (max 2000 znaków) |

> **Uwaga dla UI:** Komentarze nie są edytowalne — każde żądanie PUT/POST zastępuje całą listę. Autor komentarza jest ustawiany automatycznie na podstawie bieżącego użytkownika.

---

### 4.5 `WorkScheduleWorkDependencyDto` — Zależność między zakresami

| Pole | Typ | Wymagane | Opis |
|------|-----|----------|------|
| `predecessorDbId` | `Guid?` | ❌* | ID poprzednika z bazy danych |
| `predecessorTempId` | `Guid?` | ❌* | `TempId` poprzednika (nowy zakres w tym samym żądaniu) |
| `successorDbId` | `Guid?` | ❌* | ID następnika z bazy danych |
| `successorTempId` | `Guid?` | ❌* | `TempId` następnika (nowy zakres w tym samym żądaniu) |
| `dependencyType` | `WorkDependencyType` | ✅ | Typ zależności (enum, patrz §4.6) |
| `lagDays` | `int` | ✅ | Opóźnienie w dniach stosowane do warunku zależności |

> \* Dla **poprzednika** wymagane jest `predecessorDbId` lub `predecessorTempId` (co najmniej jedno). Dla **następnika** analogicznie. Jeśli podane są oba, `DbId` ma pierwszeństwo.

---

### 4.6 `WorkDependencyType` — Typ zależności (enum)

| Wartość int | Nazwa | Opis |
|-------------|-------|------|
| `0` | `FinishToStart` | Następnik nie może się **rozpocząć**, dopóki poprzednik się nie **skończy** *(najczęstszy)* |
| `1` | `StartToStart` | Następnik nie może się **rozpocząć**, dopóki poprzednik się nie **rozpocznie** |
| `2` | `FinishToFinish` | Następnik nie może się **skończyć**, dopóki poprzednik się nie **skończy** |
| `3` | `StartToFinish` | Następnik nie może się **skończyć**, dopóki poprzednik się nie **rozpocznie** *(rzadki)* |

---

## 5. Modele odpowiedzi

### 5.1 `WorkScheduleDetailsWeb` — Pełne szczegóły

> Zwracany przez: `POST /`, `PUT /{workScheduleId}`, `GET /details/{workScheduleId}`

| Pole | Typ | Opis |
|------|-----|------|
| `id` | `Guid` | Identyfikator harmonogramu |
| `tenantId` | `Guid` | Identyfikator tenanta |
| `projectId` | `Guid` | Identyfikator projektu |
| `costEstimateId` | `Guid?` | ID powiązanego kosztorysu (jeśli dotyczy) |
| `name` | `string` | Nazwa harmonogramu |
| `createdAt` | `DateTime` | Data utworzenia (UTC) |
| `createdByUserId` | `Guid` | ID twórcy |
| `createdByUserName` | `string` | Imię i nazwisko twórcy |
| `stages` | `WorkScheduleStageWeb[]` | Drzewo etapów (korzeń) |
| `dependencies` | `WorkScheduleWorkDependencyWeb[]` | Lista zależności |

#### `WorkScheduleStageWeb`

| Pole | Typ | Opis |
|------|-----|------|
| `id` | `Guid` | Identyfikator etapu |
| `name` | `string` | Nazwa |
| `order` | `int` | Kolejność |
| `parentStageId` | `Guid?` | ID etapu nadrzędnego (`null` dla korzeni drzewa) |
| `costEstimateGroupId` | `Guid?` | ID grupy kosztorysu — ustawiony gdy etap pochodzi z synchronizacji |
| `works` | `WorkScheduleStageWorkWeb[]` | Zakresy prac przypisane do etapu |
| `childStages` | `WorkScheduleStageWeb[]` | Podrzędne etapy (rekurencja) |

#### `WorkScheduleStageWorkWeb`

| Pole | Typ | Opis |
|------|-----|------|
| `id` | `Guid` | Identyfikator zakresu |
| `name` | `string` | Nazwa |
| `order` | `int` | Kolejność |
| `colorRgb` | `string` | Kolor |
| `isClosed` | `bool` | Czy zamknięty |
| `periods` | `WorkScheduleStageWorkPeriodWeb[]` | Okresy (posortowane po `startDate`) |
| `assignees` | `WorkScheduleStageWorkAssigneeWeb[]` | Przypisani użytkownicy |
| `comments` | `WorkScheduleStageWorkCommentWeb[]` | Komentarze (posortowane po `createdAt`) |

#### `WorkScheduleStageWorkPeriodWeb`

| Pole | Typ | Opis |
|------|-----|------|
| `startDate` | `DateTime` | Data rozpoczęcia |
| `endDate` | `DateTime` | Data zakończenia |
| `isClosed` | `bool` | Czy zamknięty |

#### `WorkScheduleStageWorkAssigneeWeb`

| Pole | Typ | Opis |
|------|-----|------|
| `userId` | `Guid` | ID użytkownika |
| `userName` | `string` | Imię i nazwisko |

#### `WorkScheduleStageWorkCommentWeb`

| Pole | Typ | Opis |
|------|-----|------|
| `id` | `Guid` | Identyfikator komentarza |
| `content` | `string` | Treść |
| `createdByUserId` | `Guid` | ID autora |
| `createdByUserName` | `string` | Imię i nazwisko autora |
| `createdAt` | `DateTime` | Data dodania (UTC) |

#### `WorkScheduleWorkDependencyWeb`

| Pole | Typ | Opis |
|------|-----|------|
| `id` | `Guid` | Identyfikator zależności |
| `predecessorWorkId` | `Guid` | ID zakresu poprzednika |
| `successorWorkId` | `Guid` | ID zakresu następnika |
| `dependencyType` | `WorkDependencyType` | Typ zależności |
| `lagDays` | `int` | Opóźnienie w dniach |

---

### 5.2 `WorkScheduleSummaryWeb` — Podsumowanie (lista)

> Zwracany przez: `GET /{scope}` (jako tablica)

| Pole | Typ | Opis |
|------|-----|------|
| `id` | `Guid` | Identyfikator |
| `costEstimateId` | `Guid?` | ID powiązanego kosztorysu |
| `name` | `string` | Nazwa |
| `createdAt` | `DateTime` | Data utworzenia (UTC) |
| `createdByUserId` | `Guid` | ID twórcy |
| `createdByUserName` | `string` | Imię i nazwisko twórcy |

---

## 6. Reguły walidacji

### 6.1 Poziom Command (`CreateWorkScheduleCommand` / `UpdateWorkScheduleCommand`)

| Reguła | Komunikat błędu |
|--------|-----------------|
| `name` — wymagane | `"Work schedule name is required"` |
| `name` — max 200 znaków | `"Work schedule name cannot exceed 200 characters"` |
| Wszyscy użytkownicy w `assignedUserIds` muszą być członkami projektu | `"One or more assigned users are not members of the project"` |
| Każde `TempId` użyte w `dependencies` musi wskazywać zakres obecny w `stages` tego żądania | `"One or more dependency TempId references do not match any work item in the provided stages"` |
| `dependencies` nie mogą tworzyć cyklu | `"Dependencies contain a circular reference"` |
| Daty zakresów muszą być spójne z warunkami zależności (jeśli oba zakresy mają okresy) | `"One or more work dependencies conflict with the defined periods"` |

### 6.2 `WorkScheduleStageDto`

| Pole / Reguła | Komunikat błędu |
|---------------|-----------------|
| `name` — wymagane | `"Stage name is required"` |
| `name` — max 200 znaków | `"Stage name cannot exceed 200 characters"` |
| `order` — ≥ 0 | `"Stage order must be greater than or equal to 0"` |
| `children` — rekurencja tych samych reguł | — |

### 6.3 `WorkScheduleWorkDto`

| Pole / Reguła | Komunikat błędu |
|---------------|-----------------|
| `name` — wymagane | `"Work name is required"` |
| `name` — max 200 znaków | `"Work name cannot exceed 200 characters"` |
| `order` — ≥ 0 | `"Work order must be greater than or equal to 0"` |
| `colorRgb` — wymagane | `"Color RGB is required"` |
| `colorRgb` — max 20 znaków | `"Color RGB cannot exceed 20 characters"` |
| `colorRgb` — format `rgb(r,g,b)` lub `#RRGGBB` | `"Color RGB must be in format 'rgb(r,g,b)' or '#RRGGBB'"` |
| `periods` — okresy nie mogą na siebie zachodzić | `"Periods cannot overlap with each other"` |
| Spójność `isClosed`: jeśli wszystkie okresy zamknięte → zakres musi być zamknięty; jeśli jakikolwiek okres otwarty → zakres nie może być zamknięty | `"Work closure status must be consistent with periods: if all periods are closed, work must be closed; if any period is open, work cannot be closed"` |

### 6.4 `WorkScheduleWorkPeriodDto`

| Pole / Reguła | Komunikat błędu |
|---------------|-----------------|
| `startDate` — wymagane | `"Period start date is required"` |
| `endDate` — wymagane | `"Period end date is required"` |
| `endDate` — ≥ `startDate` | `"Period end date cannot be before start date"` |

### 6.5 `WorkScheduleWorkCommentDto`

| Pole / Reguła | Komunikat błędu |
|---------------|-----------------|
| `content` — wymagane | `"Comment content is required"` |
| `content` — max 2000 znaków | `"Comment content cannot exceed 2000 characters"` |

### 6.6 `WorkScheduleWorkDependencyDto`

| Reguła | Komunikat błędu |
|--------|-----------------|
| Wymagane `predecessorDbId` lub `predecessorTempId` | `"A dependency must specify either PredecessorDbId or PredecessorTempId"` |
| Wymagane `successorDbId` lub `successorTempId` | `"A dependency must specify either SuccessorDbId or SuccessorTempId"` |
| Poprzednik i następnik nie mogą być tym samym zakresem | `"A work item cannot be both predecessor and successor in the same dependency"` |
| `dependencyType` musi być poprawną wartością enum | `"Invalid dependency type"` |

---

## 7. Logika biznesowa

### 7.1 Tworzenie harmonogramu

**Tryb ręczny** (brak `costEstimateId`):
- Etapy i zakresy są tworzone wprost z przesłanej struktury.
- Drzewo etapów może być wielopoziomowe poprzez pole `children`.
- Nowe zakresy (bez `id`) mogą być referencjonowane w `dependencies` za pomocą `tempId` (patrz §7.4).

**Tryb synchronizacji z kosztorysem** (`costEstimateId` podane):
- Etapy i zakresy są automatycznie generowane ze struktury grup kosztorysu — pole `stages` jest ignorowane.
- Wymaga, aby bieżący użytkownik miał poziom dostępu `Full` do wskazanego kosztorysu (`costEstimateAccessLevel >= Full`). W przeciwnym razie zwracany jest `403 Forbidden`.

**Powiadomienia:** Po utworzeniu system wysyła powiadomienia do wszystkich nowo przypisanych użytkowników.

---

### 7.2 Aktualizacja harmonogramu

**Uprawnienia:** Aktualizacja możliwa wyłącznie dla właściciela harmonogramu lub administratora tenanta/projektu. Pozostali użytkownicy otrzymują `404 Not Found` (ukryte ze względów bezpieczeństwa).

**Tryb ręczny** (harmonogram bez `costEstimateId`):
- Operacja **pełnego zastąpienia** — etapy i zakresy nieobecne w żądaniu są trwale usuwane.
- Etapy i zakresy z podanym `id` są aktualizowane; bez `id` są tworzone jako nowe.
- Lista `dependencies` jest w całości zastępowana — stare zależności są usuwane.

**Tryb synchronizowany** (harmonogram z `costEstimateId`):
1. Struktura etapów jest najpierw ponownie synchronizowana z aktualnym stanem kosztorysu.
2. Następnie nakładana jest aktualizacja z żądania — **wyłącznie** dla etapów/zakresów bez `costEstimateGroupId` / `costEstimateItemId` (ręcznych).
3. Etapów i zakresów zsynchronizowanych z kosztorysem nie można edytować ręcznie.

**Powiadomienia:** Wysyłane do nowo przypisanych i usuniętych z przypisania użytkowników.

---

### 7.3 Usuwanie harmonogramu

- Operacja logiczna (soft delete) — ustawiane są flagi `isDeleted = true` i `deletedAt = DateTime.UtcNow`. Dane nie są fizycznie usuwane.
- Dozwolone **wyłącznie** dla właściciela harmonogramu lub administratora tenanta/projektu.
- Inny użytkownik → `403 Forbidden`.
- Nieistniejący harmonogram → `404 Not Found`.

---

### 7.4 Mechanizm `TempId`

Pozwala na jednoczesne tworzenie nowych zakresów prac i definiowanie między nimi zależności w ramach **jednego żądania**.

Ponieważ nowe zakresy nie mają jeszcze identyfikatora z bazy danych, klient przypisuje im dowolny UUID jako `tempId`. System po zapisaniu zakresu w bazie mapuje `tempId → rzeczywiste id` i rejestruje zależności z poprawnymi kluczami.

**Scenariusz:**
1. Zakres A → `tempId: "aaa-..."`
2. Zakres B → `tempId: "bbb-..."`
3. Dependency: `predecessorTempId: "aaa-..."` + `successorTempId: "bbb-..."`

Jeśli zakres istnieje już w bazie, należy użyć `id` (pole `id` w `WorkScheduleWorkDto`) i odpowiednio `predecessorDbId` / `successorDbId` w dependency.

---

### 7.5 Spójność zamknięcia (`IsClosed`)

Flaga zamknięcia jest zarządzana na dwóch poziomach (zakres i okres) i musi być spójna:

| Stan okresów | Wymagany stan zakresu |
|-------------|----------------------|
| Wszystkie okresy `isClosed: true` | Zakres **musi** być `isClosed: true` |
| Choć jeden okres `isClosed: false` | Zakres **musi** być `isClosed: false` |
| Brak okresów | Zakres może być dowolny |

Serwer propaguje `isClosed` zakresu na wszystkie jego okresy przy zapisie.

---

### 7.6 Walidacja zależności czasowych

Gdy oba zakresy powiązane zależnością mają co najmniej jeden zdefiniowany okres, system weryfikuje zgodność dat z typem zależności:

| Typ zależności | Warunek naruszenia (błąd walidacji) |
|----------------|-------------------------------------|
| `FinishToStart (0)` | `min(startDate następnika)` < `max(endDate poprzednika)` + `lagDays` |
| `StartToStart (1)` | `min(startDate następnika)` < `min(startDate poprzednika)` + `lagDays` |
| `FinishToFinish (2)` | `max(endDate następnika)` < `max(endDate poprzednika)` + `lagDays` |
| `StartToFinish (3)` | `max(endDate następnika)` < `min(startDate poprzednika)` + `lagDays` |

---

## 8. Kody odpowiedzi HTTP

| Kod | Opis |
|-----|------|
| `200 OK` | Sukces (GET, PUT) |
| `201 Created` | Zasób utworzony (POST) |
| `204 No Content` | Zasób usunięty (DELETE) |
| `400 Bad Request` | Błąd walidacji FluentValidation |
| `403 Forbidden` | Brak uprawnień (inny tenant; nie właściciel/admin przy delete) |
| `404 Not Found` | Zasób nie istnieje lub brak dostępu (świadomie ukryte jako 404) |

---

## 9. Przykłady wywołań

### 9.1 `POST /` — Tworzenie harmonogramu (tryb ręczny)

**Żądanie:**
```
POST api/tenants/11111111-1111-1111-1111-111111111111/project/22222222-2222-2222-2222-222222222222/work-schedule
```

```json
{
  "name": "Harmonogram Q1 2025",
  "stages": [
    {
      "name": "Faza 1 — Przygotowanie",
      "order": 0,
      "works": [
        {
          "tempId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          "name": "Analiza wymagań",
          "order": 0,
          "colorRgb": "#4A90D9",
          "isClosed": false,
          "periods": [
            {
              "startDate": "2025-01-06T00:00:00Z",
              "endDate": "2025-01-17T00:00:00Z",
              "isClosed": false
            }
          ],
          "assignedUserIds": [
            "33333333-3333-3333-3333-333333333333"
          ],
          "comments": []
        },
        {
          "tempId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          "name": "Projekt architektury",
          "order": 1,
          "colorRgb": "#7ED321",
          "isClosed": false,
          "periods": [
            {
              "startDate": "2025-01-20T00:00:00Z",
              "endDate": "2025-01-31T00:00:00Z",
              "isClosed": false
            }
          ],
          "assignedUserIds": [
            "44444444-4444-4444-4444-444444444444"
          ]
        }
      ],
      "children": []
    },
    {
      "name": "Faza 2 — Realizacja",
      "order": 1,
      "children": [
        {
          "name": "Faza 2.1 — Backend",
          "order": 0,
          "works": [
            {
              "tempId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
              "name": "Implementacja API",
              "order": 0,
              "colorRgb": "#F5A623",
              "isClosed": false
            }
          ]
        }
      ]
    }
  ],
  "dependencies": [
    {
      "predecessorTempId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "successorTempId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "dependencyType": 0,
      "lagDays": 0
    },
    {
      "predecessorTempId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "successorTempId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
      "dependencyType": 0,
      "lagDays": 2
    }
  ]
}
```

**Odpowiedź `201 Created`:**

```json
{
  "id": "55555555-5555-5555-5555-555555555555",
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "projectId": "22222222-2222-2222-2222-222222222222",
  "costEstimateId": null,
  "name": "Harmonogram Q1 2025",
  "createdAt": "2025-01-02T10:00:00Z",
  "createdByUserId": "99999999-9999-9999-9999-999999999999",
  "createdByUserName": "Jan Kowalski",
  "stages": [
    {
      "id": "66666666-6666-6666-6666-666666666666",
      "name": "Faza 1 — Przygotowanie",
      "order": 0,
      "parentStageId": null,
      "costEstimateGroupId": null,
      "works": [
        {
          "id": "77777777-7777-7777-7777-777777777777",
          "name": "Analiza wymagań",
          "order": 0,
          "colorRgb": "#4A90D9",
          "isClosed": false,
          "periods": [
            {
              "startDate": "2025-01-06T00:00:00Z",
              "endDate": "2025-01-17T00:00:00Z",
              "isClosed": false
            }
          ],
          "assignees": [
            {
              "userId": "33333333-3333-3333-3333-333333333333",
              "userName": "Anna Nowak"
            }
          ],
          "comments": []
        },
        {
          "id": "88888888-8888-8888-8888-888888888888",
          "name": "Projekt architektury",
          "order": 1,
          "colorRgb": "#7ED321",
          "isClosed": false,
          "periods": [
            {
              "startDate": "2025-01-20T00:00:00Z",
              "endDate": "2025-01-31T00:00:00Z",
              "isClosed": false
            }
          ],
          "assignees": [
            {
              "userId": "44444444-4444-4444-4444-444444444444",
              "userName": "Piotr Wiśniewski"
            }
          ],
          "comments": []
        }
      ],
      "childStages": []
    },
    {
      "id": "aaaabbbb-aaaa-bbbb-aaaa-aaaabbbbaaaa",
      "name": "Faza 2 — Realizacja",
      "order": 1,
      "parentStageId": null,
      "costEstimateGroupId": null,
      "works": [],
      "childStages": [
        {
          "id": "ccccdddd-cccc-dddd-cccc-ccccddddcccc",
          "name": "Faza 2.1 — Backend",
          "order": 0,
          "parentStageId": "aaaabbbb-aaaa-bbbb-aaaa-aaaabbbbaaaa",
          "costEstimateGroupId": null,
          "works": [
            {
              "id": "eeeeffff-eeee-ffff-eeee-eeeeffeeeeee",
              "name": "Implementacja API",
              "order": 0,
              "colorRgb": "#F5A623",
              "isClosed": false,
              "periods": [],
              "assignees": [],
              "comments": []
            }
          ],
          "childStages": []
        }
      ]
    }
  ],
  "dependencies": [
    {
      "id": "dep00001-0000-0000-0000-000000000001",
      "predecessorWorkId": "77777777-7777-7777-7777-777777777777",
      "successorWorkId": "88888888-8888-8888-8888-888888888888",
      "dependencyType": 0,
      "lagDays": 0
    },
    {
      "id": "dep00002-0000-0000-0000-000000000002",
      "predecessorWorkId": "88888888-8888-8888-8888-888888888888",
      "successorWorkId": "eeeeffff-eeee-ffff-eeee-eeeeffeeeeee",
      "dependencyType": 0,
      "lagDays": 2
    }
  ]
}
```

---

### 9.2 `POST /` — Tworzenie harmonogramu (tryb synchronizacji z kosztorysem)

**Żądanie:**
```
POST api/tenants/11111111-1111-1111-1111-111111111111/project/22222222-2222-2222-2222-222222222222/work-schedule
```

```json
{
  "name": "Harmonogram powiązany z kosztorysem",
  "costEstimateId": "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"
}
```

**Odpowiedź `201 Created`:** Pełna struktura `WorkScheduleDetailsWeb` — etapy i zakresy wygenerowane automatycznie ze struktury kosztorysu.

---

### 9.3 `PUT /{workScheduleId}` — Aktualizacja harmonogramu

**Żądanie:**
```
PUT api/tenants/11111111-1111-1111-1111-111111111111/project/22222222-2222-2222-2222-222222222222/work-schedule/55555555-5555-5555-5555-555555555555
```

```json
{
  "name": "Harmonogram Q1 2025 — Aktualizacja",
  "stages": [
    {
      "id": "66666666-6666-6666-6666-666666666666",
      "name": "Faza 1 — Przygotowanie",
      "order": 0,
      "works": [
        {
          "id": "77777777-7777-7777-7777-777777777777",
          "name": "Analiza wymagań — zakończona",
          "order": 0,
          "colorRgb": "#4A90D9",
          "isClosed": true,
          "periods": [
            {
              "startDate": "2025-01-06T00:00:00Z",
              "endDate": "2025-01-17T00:00:00Z",
              "isClosed": true
            }
          ],
          "assignedUserIds": [
            "33333333-3333-3333-3333-333333333333"
          ]
        },
        {
          "id": "88888888-8888-8888-8888-888888888888",
          "name": "Projekt architektury",
          "order": 1,
          "colorRgb": "#7ED321",
          "isClosed": false,
          "periods": [
            {
              "startDate": "2025-01-20T00:00:00Z",
              "endDate": "2025-02-07T00:00:00Z",
              "isClosed": false
            }
          ],
          "assignedUserIds": [
            "44444444-4444-4444-4444-444444444444"
          ],
          "comments": [
            {
              "content": "Termin przesunięty o tydzień ze względu na zmiany wymagań."
            }
          ]
        }
      ]
    }
  ],
  "dependencies": [
    {
      "predecessorDbId": "77777777-7777-7777-7777-777777777777",
      "successorDbId": "88888888-8888-8888-8888-888888888888",
      "dependencyType": 0,
      "lagDays": 0
    }
  ]
}
```

**Odpowiedź `200 OK`:** Pełna struktura `WorkScheduleDetailsWeb`.

---

### 9.4 `GET /{scope}` — Lista harmonogramów

**Żądanie:**
```
GET api/tenants/11111111-1111-1111-1111-111111111111/project/22222222-2222-2222-2222-222222222222/work-schedule/All
```

**Odpowiedź `200 OK`:**

```json
[
  {
    "id": "55555555-5555-5555-5555-555555555555",
    "costEstimateId": null,
    "name": "Harmonogram Q1 2025",
    "createdAt": "2025-01-02T10:00:00Z",
    "createdByUserId": "99999999-9999-9999-9999-999999999999",
    "createdByUserName": "Jan Kowalski"
  },
  {
    "id": "dddddddd-dddd-dddd-dddd-dddddddddddd",
    "costEstimateId": "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
    "name": "Harmonogram powiązany z kosztorysem",
    "createdAt": "2025-01-03T08:30:00Z",
    "createdByUserId": "99999999-9999-9999-9999-999999999999",
    "createdByUserName": "Jan Kowalski"
  }
]
```

---

### 9.5 `GET /details/{workScheduleId}` — Szczegóły harmonogramu

**Żądanie:**
```
GET api/tenants/11111111-1111-1111-1111-111111111111/project/22222222-2222-2222-2222-222222222222/work-schedule/details/55555555-5555-5555-5555-555555555555
```

**Odpowiedź `200 OK`:** Pełna struktura `WorkScheduleDetailsWeb` (jak w przykładzie `POST` §9.1).

---

### 9.6 `DELETE /{workScheduleId}` — Usunięcie harmonogramu

**Żądanie:**
```
DELETE api/tenants/11111111-1111-1111-1111-111111111111/project/22222222-2222-2222-2222-222222222222/work-schedule/55555555-5555-5555-5555-555555555555
```

**Odpowiedź `204 No Content`** *(brak body)*
