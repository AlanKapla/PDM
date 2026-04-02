# Harmonogram prac — Dokumentacja API

Dokumentacja opisuje kontrakt API modułu harmonogramów prac, przeznaczona dla implementacji warstwy UI.

---

## Spis treści

1. [Endpointy](#endpointy)
2. [Dwa tryby harmonogramu](#dwa-tryby-harmonogramu)
3. [POST — Tworzenie harmonogramu](#post--tworzenie-harmonogramu)
4. [PUT — Aktualizacja harmonogramu](#put--aktualizacja-harmonogramu)
5. [GET — Lista harmonogramów](#get--lista-harmonogramów)
6. [GET — Szczegóły harmonogramu](#get--szczegóły-harmonogramu)
7. [Modele odpowiedzi](#modele-odpowiedzi)
8. [Walidacja — zasady po stronie API](#walidacja--zasady-po-stronie-api)
9. [Uprawnienia dostępu](#uprawnienia-dostępu)

---

## Endpointy

| Metoda | Ścieżka | Polityka autoryzacji |
|--------|---------|----------------------|
| `POST` | `api/tenants/{tenantId}/project/{projectId}/work-schedule` | `ProjectResourcesWrite` |
| `PUT` | `api/tenants/{tenantId}/project/{projectId}/work-schedule/{workScheduleId}` | `ProjectResourcesWrite` |
| `GET` | `api/tenants/{tenantId}/project/{projectId}/work-schedule/{scope}` | `ProjectView` |
| `GET` | `api/tenants/{tenantId}/project/{projectId}/work-schedule/details/{workScheduleId}` | `ProjectResourcesReadSingle` |

---

## Dwa tryby harmonogramu

Harmonogram działa w jednym z dwóch trybów, zależnie od wartości pola `costEstimateId` wysłanego przy tworzeniu (`POST`). **Trybu nie można zmienić po utworzeniu harmonogramu.**

| | Tryb ręczny | Tryb powiązany z kosztorysem |
|---|---|---|
| `costEstimateId` w `POST` | `null` | GUID istniejącego kosztorysu |
| Struktura etapów | zarządzana przez UI | generowana automatycznie z grup kosztorysu |
| Pole `stages` w `POST` | wysyłane przez UI | ignorowane przez API |
| Pole `stages` w `PUT` | wysyłane przez UI (pełne drzewo) | ignorowane przez API (tylko `name` ma znaczenie) |
| `costEstimateGroupId` na etapie w odpowiedzi | zawsze `null` | GUID grupy kosztorysu |
| Zagnieżdżone etapy (`childStages`) | tak — dowolna głębokość, pole `children` w żądaniu | tak — odzwierciedla hierarchię grup kosztorysu |
| Wymagany poziom dostępu do kosztorysu | — | `Full` (403 jeśli niższy) |

> **Wskazówka dla UI:** Na podstawie `costEstimateId` w odpowiedzi możesz ustalić tryb harmonogramu i odpowiednio dostosować interfejs (np. ukryć edycję etapów w trybie powiązanym z kosztorysem).

---

## POST — Tworzenie harmonogramu

```
POST api/tenants/{tenantId}/project/{projectId}/work-schedule
```

Zwraca `201 Created` z pełnym modelem `WorkScheduleDetailsWeb`.

### Ciało żądania

```jsonc
{
  "name": "Harmonogram Q3 2025",       // wymagane, max 200 znaków
  "costEstimateId": null,               // null = tryb ręczny | GUID = tryb powiązany z kosztorysem
  "stages": [                           // ignorowane gdy costEstimateId != null
    {
      "id": null,                       // zawsze null przy tworzeniu
      "name": "Etap 1",                 // wymagane, max 200 znaków
      "order": 0,                       // wymagane, >= 0
      "works": [
        {
          "id": null,                   // zawsze null przy tworzeniu
          "name": "Praca fundamentowa",
          "order": 0,
          "colorRgb": "#FF5733",
          "isClosed": false,
          "periods": [
            {
              "id": null,
              "startDate": "2025-07-01T00:00:00Z",
              "endDate": "2025-07-15T00:00:00Z",
              "isClosed": false
            }
          ],
          "assignedUserIds": [
            "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          ],
          "comments": [
            {
              "id": null,
              "content": "Uwaga do pracy"
            }
          ]
        }
      ],
      "children": [                     // zagnieżdżone etapy — dowolna głębokość
        {
          "id": null,
          "name": "Etap 1.1",
          "order": 0,
          "works": [],
          "children": []
        }
      ]
    }
  ]
}
```

### Zagnieżdżone etapy (tryb ręczny)

Etapy mogą być zagnieżdżane na dowolną głębokość za pomocą pola `children`. Każdy etap może zawierać prace (`works`) oraz zagnieżdżone etapy potomne (`children`). Nie ma ograniczenia głębokości.

```jsonc
{
  "name": "Etap 1",
  "id": null,
  "order": 0,
  "works": [],
  "children": [
    {
      "name": "Etap 1.1",
      "id": null,
      "order": 0,
      "works": [],
      "children": [
        {
          "name": "Etap 1.1.1",
          "id": null,
          "order": 0,
          "works": [],
          "children": []   // dowolna głębokość
        }
      ]
    }
  ]
}
```

W odpowiedzi zagnieżdżone etapy są zwracane w polu `childStages` na każdym poziomie. Pole `parentStageId` wskazuje na bezpośredniego rodzica.

---

### Tryb powiązany z kosztorysem

W tym trybie pole `stages` jest całkowicie ignorowane. Struktura etapów jest generowana automatycznie z hierarchii grup kosztorysu:

```jsonc
{
  "name": "Harmonogram na podstawie kosztorysu",
  "costEstimateId": "a1b2c3d4-0000-0000-0000-000000000001",
  "stages": []   // ignorowane — można pominąć lub wysłać puste
}
```

> **Wymagany dostęp do kosztorysu:** Użytkownik musi posiadać poziom dostępu `Full` do wskazanego kosztorysu. Jeśli poziom dostępu jest niższy (`None`, `ReadOnly`, `Restricted`), API zwróci `403 Forbidden`.

---

## PUT — Aktualizacja harmonogramu

```
PUT api/tenants/{tenantId}/project/{projectId}/work-schedule/{workScheduleId}
```

Zwraca `200 OK` z pełnym modelem `WorkScheduleDetailsWeb`.

### Tryb ręczny — ciało żądania

Pole `stages` to **pełne, aktualne drzewo** harmonogramu. API porówna je z aktualnym stanem w bazie i:
- usunie etapy/prace **nieobecne** w przesłanym drzewie,
- zaktualizuje etapy/prace z pasującym `id`,
- doda nowe etapy/prace z `id: null`.

> **Ważne dla UI:** Zawsze wysyłaj kompletne drzewo etapów. Pominięcie etapu lub pracy jest równoznaczne z jej usunięciem.

```jsonc
{
  "name": "Harmonogram Q3 2025 — wersja 2",
  "stages": [
    {
      "id": "bbbbbbbb-0000-0000-0000-000000000001",  // istniejący etap — zostanie zaktualizowany
      "name": "Etap 1 (zaktualizowana nazwa)",
      "order": 0,
      "works": [
        {
          "id": "cccccccc-0000-0000-0000-000000000001", // istniejąca praca — zostanie zaktualizowana
          "name": "Praca fundamentowa",
          "order": 0,
          "colorRgb": "#FF5733",
          "isClosed": false,
          "periods": [
            {
              "id": "dddddddd-0000-0000-0000-000000000001",
              "startDate": "2025-07-01T00:00:00Z",
              "endDate": "2025-07-20T00:00:00Z",  // zmieniono datę końcową
              "isClosed": false
            }
          ],
          "assignedUserIds": [
            "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          ],
          "comments": []
        },
        {
          "id": null,                               // nowa praca — zostanie utworzona
          "name": "Nowa praca",
          "order": 1,
          "colorRgb": "#33AAFF",
          "isClosed": false,
          "periods": [],
          "assignedUserIds": [],
          "comments": []
        }
      ],
      "children": [
        {
          "id": null,                               // nowy zagnieżdżony etap
          "name": "Etap 1.1",
          "order": 0,
          "works": [],
          "children": []
        }
      ]
    }
  ]
}
```

### Tryb powiązany z kosztorysem — ciało żądania

Pole `stages` jest ignorowane. API automatycznie synchronizuje etapy z aktualnym stanem kosztorysu. Wysyłaj tylko `name`:

```jsonc
{
  "name": "Zaktualizowana nazwa harmonogramu",
  "stages": []   // ignorowane — można pominąć lub wysłać puste
}
```

> **Zachowanie synchronizacji:** Przy każdym `PUT` harmonogramu powiązanego z kosztorysem API odświeża strukturę etapów zgodnie z aktualną hierarchią grup kosztorysu. Nowe grupy generują nowe etapy. Usunięte grupy powodują ukrycie etapów (etapy są widoczne w odpowiedzi tylko gdy nie są ukryte).

> **Wymagany dostęp do kosztorysu:** Tak jak przy tworzeniu, użytkownik musi mieć poziom `Full` do powiązanego kosztorysu. Brak odpowiedniego dostępu zwraca `403 Forbidden`.

---

## GET — Lista harmonogramów

```
GET api/tenants/{tenantId}/project/{projectId}/work-schedule/{scope}
```

Parametr `scope` przyjmuje wartości:

| Wartość | Opis |
|---------|------|
| `All` | Wszystkie harmonogramy w projekcie (wymaga uprawnień admina) |
| `Mine` | Harmonogramy utworzone przez zalogowanego użytkownika |
| `Shared` | Harmonogramy, w których zalogowany użytkownik jest przypisany do co najmniej jednej pracy |

Zwraca `200 OK` z tablicą obiektów `WorkScheduleSummaryWeb`:

```jsonc
[
  {
    "id": "aaaaaaaa-0000-0000-0000-000000000001",
    "costEstimateId": null,                           // null = tryb ręczny
    "name": "Harmonogram Q3 2025",
    "createdAt": "2025-06-01T10:00:00Z",
    "createdByUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "createdByUserName": "Jan Kowalski"
  },
  {
    "id": "aaaaaaaa-0000-0000-0000-000000000002",
    "costEstimateId": "a1b2c3d4-0000-0000-0000-000000000001", // powiązany z kosztorysem
    "name": "Harmonogram na podstawie kosztorysu",
    "createdAt": "2025-06-10T08:30:00Z",
    "createdByUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "createdByUserName": "Jan Kowalski"
  }
]
```

---

## GET — Szczegóły harmonogramu

```
GET api/tenants/{tenantId}/project/{projectId}/work-schedule/details/{workScheduleId}
```

Zwraca `200 OK` z pełnym modelem `WorkScheduleDetailsWeb` zawierającym drzewo etapów.

```jsonc
{
  "id": "aaaaaaaa-0000-0000-0000-000000000001",
  "tenantId": "11111111-0000-0000-0000-000000000001",
  "projectId": "22222222-0000-0000-0000-000000000001",
  "costEstimateId": null,                             // null = tryb ręczny
  "name": "Harmonogram Q3 2025",
  "createdAt": "2025-06-01T10:00:00Z",
  "createdByUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "createdByUserName": "Jan Kowalski",
  "stages": [
    {
      "id": "bbbbbbbb-0000-0000-0000-000000000001",
      "name": "Etap 1",
      "order": 0,
      "parentStageId": null,                          // null = etap główny (korzeń)
      "costEstimateGroupId": null,                    // null w trybie ręcznym
      "works": [
        {
          "id": "cccccccc-0000-0000-0000-000000000001",
          "name": "Praca fundamentowa",
          "order": 0,
          "colorRgb": "#FF5733",
          "isClosed": false,
          "periods": [
            {
              "startDate": "2025-07-01T00:00:00Z",
              "endDate": "2025-07-15T00:00:00Z",
              "isClosed": false
            }
          ],
          "assignees": [
            {
              "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
              "userName": "Jan Kowalski"
            }
          ],
          "comments": [
            {
              "id": "eeeeeeee-0000-0000-0000-000000000001",
              "content": "Uwaga do pracy",
              "createdByUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
              "createdByUserName": "Jan Kowalski",
              "createdAt": "2025-06-02T09:15:00Z"
            }
          ]
        }
      ],
      "childStages": [                                // zagnieżdżone etapy
        {
          "id": "bbbbbbbb-0000-0000-0000-000000000002",
          "name": "Etap 1.1",
          "order": 0,
          "parentStageId": "bbbbbbbb-0000-0000-0000-000000000001",
          "costEstimateGroupId": null,
          "works": [],
          "childStages": []                           // może mieć dalsze zagnieżdżenia
        }
      ]
    }
  ]
}
```

---

## Modele odpowiedzi

### `WorkScheduleSummaryWeb` (lista)

| Pole | Typ | Opis |
|------|-----|------|
| `id` | `Guid` | Identyfikator harmonogramu |
| `costEstimateId` | `Guid?` | `null` = tryb ręczny; wartość = tryb powiązany z kosztorysem |
| `name` | `string` | Nazwa harmonogramu |
| `createdAt` | `DateTime` | Data utworzenia (UTC) |
| `createdByUserId` | `Guid` | ID użytkownika twórcy |
| `createdByUserName` | `string` | Nazwa użytkownika twórcy |

### `WorkScheduleDetailsWeb` (szczegóły, POST, PUT)

| Pole | Typ | Opis |
|------|-----|------|
| `id` | `Guid` | Identyfikator harmonogramu |
| `tenantId` | `Guid` | Identyfikator tenanta |
| `projectId` | `Guid` | Identyfikator projektu |
| `costEstimateId` | `Guid?` | `null` = tryb ręczny |
| `name` | `string` | Nazwa harmonogramu |
| `createdAt` | `DateTime` | Data utworzenia (UTC) |
| `createdByUserId` | `Guid` | ID użytkownika twórcy |
| `createdByUserName` | `string` | Nazwa użytkownika twórcy |
| `stages` | `WorkScheduleStageWeb[]` | Drzewo etapów (tylko korzenie — dzieci są w `childStages`) |

### `WorkScheduleStageWeb`

| Pole | Typ | Opis |
|------|-----|------|
| `id` | `Guid` | Identyfikator etapu |
| `name` | `string` | Nazwa etapu |
| `order` | `int` | Kolejność w obrębie poziomu |
| `parentStageId` | `Guid?` | `null` = etap główny (korzeń); wartość = etap zagnieżdżony |
| `costEstimateGroupId` | `Guid?` | Tylko w trybie powiązanym z kosztorysem |
| `works` | `WorkScheduleStageWorkWeb[]` | Prace przypisane do etapu |
| `childStages` | `WorkScheduleStageWeb[]` | Zagnieżdżone etapy (rekurencyjnie) |

### `WorkScheduleStageWorkWeb`

| Pole | Typ | Opis |
|------|-----|------|
| `id` | `Guid` | Identyfikator pracy |
| `name` | `string` | Nazwa pracy |
| `order` | `int` | Kolejność |
| `colorRgb` | `string` | Kolor w formacie hex, np. `#FF5733` |
| `isClosed` | `bool` | Czy praca jest zamknięta |
| `periods` | `WorkScheduleStageWorkPeriodWeb[]` | Okresy trwania pracy |
| `assignees` | `WorkScheduleStageWorkAssigneeWeb[]` | Przypisani użytkownicy (z nazwami) |
| `comments` | `WorkScheduleStageWorkCommentWeb[]` | Komentarze do pracy |

### `WorkScheduleStageWorkPeriodWeb`

| Pole | Typ | Opis |
|------|-----|------|
| `startDate` | `DateTime` | Data rozpoczęcia (UTC) |
| `endDate` | `DateTime` | Data zakończenia (UTC) |
| `isClosed` | `bool` | Czy okres jest zamknięty |

### `WorkScheduleStageWorkAssigneeWeb`

| Pole | Typ | Opis |
|------|-----|------|
| `userId` | `Guid` | ID przypisanego użytkownika |
| `userName` | `string` | Nazwa przypisanego użytkownika |

### `WorkScheduleStageWorkCommentWeb`

| Pole | Typ | Opis |
|------|-----|------|
| `id` | `Guid` | Identyfikator komentarza |
| `content` | `string` | Treść komentarza |
| `createdByUserId` | `Guid` | ID autora |
| `createdByUserName` | `string` | Nazwa autora |
| `createdAt` | `DateTime` | Data utworzenia (UTC) |

---

## Walidacja — zasady po stronie API

API zwróci `400 Bad Request` gdy:

| Pole | Reguła |
|------|--------|
| `name` | Wymagane, max 200 znaków |
| `stages[].name` | Wymagane, max 200 znaków (każdy poziom zagnieżdżenia) |
| `stages[].order` | Wymagane, wartość >= 0 |
| `stages[].works[].name` | Wymagane |
| `stages[].works[].colorRgb` | Wymagane |
| `stages[].works[].periods` | Okresy nie mogą się nakładać |
| `stages[].works[].assignedUserIds` | Wszyscy użytkownicy muszą być członkami projektu |
| `costEstimateId` (tylko `POST`) | Jeśli podany, musi należeć do tego projektu |
| `costEstimateId` (tylko `POST`) | Użytkownik musi mieć poziom dostępu `Full` do kosztorysu (sprawdzane po walidacji struktury) |
| `stages[].children` | Rekurencyjnie walidowane — te same reguły `name`, `order`, `works` obowiązują na każdym poziomie |

> Walidacja przypisanych użytkowników (`assignedUserIds`) działa na wszystkich poziomach zagnieżdżenia — API spłaszcza całe drzewo etapów przed weryfikacją.

---

## Uprawnienia dostępu

### Odczyt (`GET` szczegóły)

Dostęp do szczegółów harmonogramu mają:
- **SuperAdmin** — dostęp do wszystkich harmonogramów
- **Tenant Admin / Project Admin** — dostęp do wszystkich harmonogramów w swoim projekcie
- **Właściciel** (`createdByUserId` == zalogowany użytkownik) — dostęp do własnego harmonogramu

### Edycja (`PUT`)

Edycja harmonogramu jest dozwolona dla:
- **Tenant Admin / Project Admin**
- **Właściciela** harmonogramu (`createdByUserId` == zalogowany użytkownik)

Próba edycji cudzego harmonogramu przez nieuprawnionego użytkownika zwraca `403 Forbidden`.

### Dostęp do kosztorysu (tryb powiązany)

Przy tworzeniu (`POST`) i aktualizacji (`PUT`) harmonogramu powiązanego z kosztorysem API weryfikuje poziom dostępu użytkownika do kosztorysu:

| Poziom dostępu | Wartość | Tworzenie / aktualizacja harmonogramu |
|----------------|---------|---------------------------------------|
| `None` | 0 | ❌ 403 Forbidden |
| `ReadOnly` | 1 | ❌ 403 Forbidden |
| `Restricted` | 2 | ❌ 403 Forbidden |
| `Full` | 3 | ✅ Dozwolone |

Sprawdzenie dostępu następuje po weryfikacji przynależności kosztorysu do projektu (walidacja) i przed synchronizacją etapów.

---

### Powiadomienia (tylko tryb ręczny)

API automatycznie wysyła powiadomienia push do przypisanych użytkowników:

| Zdarzenie | Tytuł powiadomienia |
|-----------|---------------------|
| Tworzenie — przypisanie do pracy | „Przypisano do harmonogramu prac" |
| Aktualizacja — dodanie do pracy | „Przypisano do harmonogramu prac" |
| Aktualizacja — usunięcie z pracy | „Usunięto z harmonogramu prac" |

Użytkownicy, których przypisanie zostało jednocześnie usunięte i dodane ponownie (ta sama operacja `PUT`), nie otrzymują powiadomienia.
