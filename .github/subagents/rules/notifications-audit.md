# Audyt CQRS — Domena Notifications

## BLOK 1 — INWENTARYZACJA

| Plik | Typ | Ścieżka |
|------|-----|---------|
| `GetAllNotificationsQuery.cs` | Query | `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/Notifications/GetAllNotifications/GetAllNotificationsQuery.cs` |
| `GetAllNotificationsQueryHandler.cs` | Handler | `…/CQRS/Notifications/GetAllNotifications/GetAllNotificationsQueryHandler.cs` |
| `GetAllNotificationsQueryValidator.cs` | Validator | `…/CQRS/Notifications/GetAllNotifications/GetAllNotificationsQueryValidator.cs` |
| `GetUnreadNotificationsQuery.cs` | Query | `…/CQRS/Notifications/GetUnreadNotifications/GetUnreadNotificationsQuery.cs` |
| `GetUnreadNotificationsQueryHandler.cs` | Handler | `…/CQRS/Notifications/GetUnreadNotifications/GetUnreadNotificationsQueryHandler.cs` |
| `GetUnreadNotificationsQueryValidator.cs` | Validator | `…/CQRS/Notifications/GetUnreadNotifications/GetUnreadNotificationsQueryValidator.cs` |
| `GetUnreadCounterQuery.cs` | Query | `…/CQRS/Notifications/GetUnreadCounter/GetUnreadCounterQuery.cs` |
| `GetUnreadCounterQueryHandler.cs` | Handler | `…/CQRS/Notifications/GetUnreadCounter/GetUnreadCounterQueryHandler.cs` |
| `MarkNotificationAsReadCommand.cs` | Command | `…/CQRS/Notifications/MarkNotificationAsRead/MarkNotificationAsReadCommand.cs` |
| `MarkNotificationAsReadCommandHandler.cs` | Handler | `…/CQRS/Notifications/MarkNotificationAsRead/MarkNotificationAsReadCommandHandler.cs` |
| `MarkNotificationAsReadCommandValidator.cs` | Validator | `…/CQRS/Notifications/MarkNotificationAsRead/MarkNotificationAsReadCommandValidator.cs` |
| `MarkAllNotificationsAsReadCommand.cs` | Command | `…/CQRS/Notifications/MarkAllNotificationsAsRead/MarkAllNotificationsAsReadCommand.cs` |
| `MarkAllNotificationsAsReadCommandHandler.cs` | Handler | `…/CQRS/Notifications/MarkAllNotificationsAsRead/MarkAllNotificationsAsReadCommandHandler.cs` |
| `NotificationWeb.cs` | Web Model | `…/Business/Interfaces/WebModels/Notifications/NotificationWeb.cs` |
| `NotificationController.cs` | Controller | `…/WebApi/Controllers/NotificationController.cs` |
| `Notification.cs` | Entity | `…/Entities/Models/Notifications/Notification.cs` |

**Endpointy kontrolera:**

| Metoda | Trasa | Request | Authorize |
|--------|-------|---------|-----------|
| GET | `/api/notification` | `GetAllNotificationsQuery` | `[Authorize]` (klasa) — brak policy |
| GET | `/api/notification/unread` | `GetUnreadNotificationsQuery` | `[Authorize]` (klasa) — brak policy |
| GET | `/api/notification/unread-counter` | `GetUnreadCounterQuery` | `[Authorize]` (klasa) — brak policy |
| PUT | `/api/notification/{notificationId}/mark-as-read` | `MarkNotificationAsReadCommand` | `[Authorize]` (klasa) — brak policy |
| PUT | `/api/notification/mark-all-as-read` | `MarkAllNotificationsAsReadCommand` | `[Authorize]` (klasa) — brak policy |

Uwaga: trasa nie zachowuje wzorca `/api/tenants/{tenantId}/...` z konwencji projektu. Notyfikacje są user-scoped (filtrowane po `currentUser.Id`), więc świadomie odbiegają od konwencji tenant-scoped — wymaga decyzji domenowej (patrz BLOK 6 / W4).

## BLOK 2 — COMMANDS I QUERIES — STRUKTURA

### 2.1 Positional parameters vs explicit properties

| Command/Query | Używa positional params | Przykład |
|--------------|------------------------|---------|
| `GetAllNotificationsQuery` | TAK | `record GetAllNotificationsQuery(int Take = 50, int Skip = 0)` |
| `GetUnreadNotificationsQuery` | TAK | `record GetUnreadNotificationsQuery(int Take = 50, int Skip = 0)` |
| `GetUnreadCounterQuery` | NIE (parameterless) | `record GetUnreadCounterQuery()` |
| `MarkNotificationAsReadCommand` | TAK | `record MarkNotificationAsReadCommand(Guid NotificationId)` |
| `MarkAllNotificationsAsReadCommand` | NIE (parameterless) | `record MarkAllNotificationsAsReadCommand` |

Docelowo każda Command/Query powinna używać `public required ... { get; init; }`. Aktualnie 3/5 narusza wzorzec.

### 2.2 Sealed

| Command/Query | Jest sealed | Uwagi |
|--------------|------------|-------|
| `GetAllNotificationsQuery` | NIE | `public record` |
| `GetUnreadNotificationsQuery` | NIE | `public record` |
| `GetUnreadCounterQuery` | NIE | `public record` |
| `MarkNotificationAsReadCommand` | NIE | `public record` |
| `MarkAllNotificationsAsReadCommand` | NIE | `public record` |

5/5 narusza wzorzec — żadna deklaracja nie jest `sealed`.

### 2.3 Interfejsy i autoryzacja

| Command/Query | Interfejs | IAuthorizableRequest | PermissionCode poprawny |
|--------------|-----------|---------------------|------------------------|
| `GetAllNotificationsQuery` | `IRequestQuery<IEnumerable<NotificationWeb>>` | NIE | brak |
| `GetUnreadNotificationsQuery` | `IRequestQuery<IEnumerable<NotificationWeb>>` | NIE | brak |
| `GetUnreadCounterQuery` | `IRequestQuery<int>` | NIE | brak |
| `MarkNotificationAsReadCommand` | `IRequestCommand<Unit>` | NIE | brak |
| `MarkAllNotificationsAsReadCommand` | `IRequestCommand<int>` | NIE | brak |

Brak `IAuthorizableRequest` w całej domenie. Autoryzacja opiera się wyłącznie na `[Authorize]` na klasie kontrolera (uwierzytelnienie) oraz filtrowaniu po `currentUser.Id` w handlerach. Domena jest user-scoped, więc nie potrzebuje `PermissionCode` per-resource — `ResourceRef` byłby tu sztuczny. Konwencja projektu nie wymaga `IAuthorizableRequest` dla zasobów per-user.

### 2.4 Wspólne pola — kandydaci do klasy bazowej

| Pole wspólne | Występuje w | Kandydat do wydzielenia |
|-------------|------------|------------------------|
| `int Take`, `int Skip` (paginacja) | `GetAllNotificationsQuery`, `GetUnreadNotificationsQuery` | TAK — wspólny rekord `PaginationParams { int Take; int Skip }` lub klasa bazowa `PagedQueryBase` |

## BLOK 3 — WALIDATORY

### 3.1 Pokrycie walidatorami

| Command/Query | Walidator | Brakujące reguły |
|--------------|----------|-----------------|
| `GetAllNotificationsQuery` | `GetAllNotificationsQueryValidator` (sealed) | brak |
| `GetUnreadNotificationsQuery` | `GetUnreadNotificationsQueryValidator` (NIE sealed) | brak |
| `GetUnreadCounterQuery` | **BRAK** | nie wymagany — brak parametrów |
| `MarkNotificationAsReadCommand` | `MarkNotificationAsReadCommandValidator` (NIE sealed) | brak |
| `MarkAllNotificationsAsReadCommand` | **BRAK** | nie wymagany — brak parametrów |

### 3.2 Reguły szczegółowe

| Walidator | Pole | Obecna reguła | Brakująca/Nieoptymalna reguła | Uzasadnienie |
|-----------|------|--------------|-----------------|-------------|
| `MarkNotificationAsReadCommandValidator` | `NotificationId` | `NotEmpty()` z własnym message | `RequiredId()` z `CommonValidationExtensions` | Spójność komunikatów w solution |
| `MarkNotificationAsReadCommandValidator` | `NotificationId` | `MustAsync(NotificationMustExistAndBelongToUser)` | — | **Antipattern**: walidator wykonuje query DB sprawdzające istnienie + właściciela. To powiela logikę handlera (handler i tak rzuca `NotFoundApiException`). Walidator powinien sprawdzać tylko strukturę żądania, nie istnienie zasobu w DB. Powoduje 2× zapytanie do DB i zwraca HTTP 400 zamiast 404. |
| `GetAllNotificationsQueryValidator` | `Take`/`Skip` | `GreaterThan(0)` / `GreaterThanOrEqualTo(0)` z własnymi messages | brak ekstensji `PageSize()` / `NonNegativeOffset()` | duplikacja z `GetUnreadNotificationsQueryValidator` |
| `GetUnreadNotificationsQueryValidator` | `x` (root) | `Must(_ => currentUser.IsAuthenticated && currentUser.Id != Guid.Empty)` z `WithErrorCode("401")` | — | **Antipattern**: walidacja autentykacji w validatorze. To zadanie middleware/pipeline auth, nie FluentValidation. `WithErrorCode("401")` nie zwróci 401 — `ValidationApiException` mapuje się na 400. |
| `GetUnreadNotificationsQueryValidator` | `Take`/`Skip` | identyczne z `GetAllNotificationsQueryValidator` | — | duplikacja kodu |

### 3.3 Spójność — nieużywane usingi, komunikaty EN/PL, sealed

- `GetUnreadNotificationsQueryValidator` — niesealed; using `Business.Interfaces.Exceptions` nieużywany.
- `MarkNotificationAsReadCommandValidator` — niesealed; nadmiarowe usingi `Entities.Models.*` (Chats, Costs, Files, Projects, Roles, Tenants, Users, WorkSchedules) — nie używa żadnej z tych encji.
- Komunikaty: wszystkie w języku angielskim (spójne).
- `GetAllNotificationsQueryValidator` — sealed (jedyny zgodny ze wzorcem).
- `MarkNotificationAsReadCommandValidator`: `notification != null` zamiast `notification is not null`.

### 3.4 Wspólne reguły walidacji

| Reguła wspólna | Walidatory | Kandydat do extension |
|---------------|-----------|----------------------|
| `Take > 0 && Take <= 100` | `GetAllNotificationsQueryValidator`, `GetUnreadNotificationsQueryValidator` | TAK — np. `PageSize(int max = 100)` w `CommonValidationExtensions` |
| `Skip >= 0` | jw. | `NonNegativeOrder()` istnieje, ale dotyczy semantyki Order — nazwa myląca dla offsetu paginacji. Można dodać `NonNegativeOffset()` lub uogólnić. |

## BLOK 4 — HANDLERY

### 4.1 Struktura

| Handler | Sealed | Explicit types (brak var) | Uwagi |
|---------|--------|--------------------------|-------|
| `GetAllNotificationsQueryHandler` | NIE | TAK | OK pod tym kątem |
| `GetUnreadNotificationsQueryHandler` | NIE | TAK | OK pod tym kątem |
| `GetUnreadCounterQueryHandler` | NIE | TAK | OK pod tym kątem |
| `MarkNotificationAsReadCommandHandler` | NIE | TAK | OK pod tym kątem |
| `MarkAllNotificationsAsReadCommandHandler` | NIE | **NIE** — `var unreadNotifications = await ...`, `foreach (var notification in ...)` | naruszenie zakazu `var` |

5/5 handlerów nie jest `sealed`. 1/5 używa `var`.

### 4.2 Logika biznesowa

| Handler | Linie ~ | Za dużo logiki | Co wydzielić |
|---------|---------|---------------|-------------|
| `GetAllNotificationsQueryHandler` | ~75 | UMIARKOWANIE | Mapping (`MapType`, `DeserializeMetadata`, projection do `NotificationWeb`) zduplikowany 1:1 z `GetUnreadNotificationsQueryHandler`. Wydzielić do `NotificationWebMapper`. |
| `GetUnreadNotificationsQueryHandler` | ~75 | UMIARKOWANIE | jw. — duplikacja mapowania |
| `GetUnreadCounterQueryHandler` | ~30 | NIE | OK |
| `MarkNotificationAsReadCommandHandler` | ~50 | NIE | OK, `Handle()` ~25 linii — w granicach |
| `MarkAllNotificationsAsReadCommandHandler` | ~50 | NIE | OK |

### 4.3 SOLID i DRY

| Handler | Podobny do | Wspólna logika | Kandydat do klasy bazowej / serwisu |
|---------|-----------|---------------|-------------------------------------|
| `GetAllNotificationsQueryHandler` | `GetUnreadNotificationsQueryHandler` | `MapType`, `DeserializeMetadata`, projekcja na `NotificationWeb`, `GetPagedBySearchAsync` z tym samym order/include | `NotificationWebMapper` (static) i ewentualnie wspólny `NotificationQueryHandlerBase` |
| `GetUnreadNotificationsQueryHandler` | jw. | jw. | jw. |
| `MarkAllNotificationsAsReadCommandHandler` | — | bulk update — można użyć `ExecuteUpdateAsync` zamiast load+foreach+UpdateRange | optymalizacja zalecana |

### 4.4 Obsługa błędów

| Handler | Problem | Ryzyko |
|---------|---------|--------|
| `MarkNotificationAsReadCommandHandler` | Walidator wcześniej wykonuje `MustAsync` sprawdzający istnienie+ownership; jeśli zwróci `false`, pipeline rzuca `ValidationApiException` (HTTP 400) zamiast `NotFoundApiException` (HTTP 404) z handlera. Kod handlera `if (notification is null) throw NotFoundApiException` jest dead-code (walidator już odfiltrował). | Niespójna semantyka HTTP (400 zamiast 404 dla nieistniejącego zasobu); podwójne zapytanie do DB. |
| `MarkAllNotificationsAsReadCommandHandler` | brak — operacja idempotentna, brak wyjątków | brak |
| `GetUnreadNotificationsQueryHandler` | walidator zwraca błąd walidacji dla nieautentykowanego usera; faktycznie powinno zwrócić 401. `[Authorize]` na kontrolerze już to robi. Reguła w validatorze jest martwa. | mylący kod; jeśli kiedyś `[Authorize]` zniknie, zwróci 400 zamiast 401 |
| Wszystkie handlery | `MarkNotificationAsReadCommandHandler` używa `is null` poprawnie; pozostałe nie potrzebują null-check | OK |

### 4.5 Zapytania do DB

| Handler | Problem | Ryzyko |
|---------|---------|--------|
| `GetAllNotificationsQueryHandler` | `IReadRepository<Notification>` — OK; `Include(Tenant).Include(Project)` — potrzebne dla `Tenant.Name` i `Project?.Name`; predykat zawiera `UserId == currentUser.Id`, **nie zawiera** `TenantId`. Filtrowanie cross-tenant odbywa się tylko przez `UserId`. Jeśli user ma członkostwo w wielu tenantach — zwróci notyfikacje ze wszystkich. | Cross-tenant data exposure: brak filtra `TenantId` w predykacie. Należy potwierdzić, czy zamierzone (user-wide inbox) lub dodać `TenantId == currentUser.TenantId`. |
| `GetUnreadNotificationsQueryHandler` | jak wyżej — bez `TenantId` w predykacie | jw. |
| `GetUnreadCounterQueryHandler` | `IReadRepository<Notification>`, `CountAsync` — OK; brak `TenantId` w predykacie | jw. |
| `MarkNotificationAsReadCommandHandler` | `IRepository<Notification>` (zapis) — OK; predykat `Id == NotificationId && UserId == currentUser.Id` — bez `TenantId`. | Id jest GUID, kolizja w praktyce niemożliwa, ale konwencja narusza |
| `MarkAllNotificationsAsReadCommandHandler` | `IRepository<Notification>` — OK; **bulk update przez load do pamięci**: ładuje wszystkie nieprzeczytane do pamięci, mutuje, `UpdateRange`. Powinno użyć `ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true))` — single SQL UPDATE bez load do pamięci. | wydajność i RAM dla użytkowników z wieloma nieprzeczytanymi notyfikacjami |
| `MarkAllNotificationsAsReadCommandHandler` | `unreadNotifications.Count()` wywoływane 2× po materializacji | drobny zapach kodu |
| Wszystkie handlery | `MarkNotificationAsRead*` (handler+validator) i `MarkAllNotificationsAsRead*` nie przekazują `cancellationToken` do operacji repo (`GetFirstBySearch`, `Update`, `GetBySearch`, `UpdateRange`). | brak anulowania długich operacji |

**Nadmiarowe usingi we wszystkich handlerach** — wszystkie 5 handlerów importuje `Entities.Models.Chats`, `Costs`, `Files`, `Projects`, `Roles`, `Tenants`, `Users`, `WorkSchedules` mimo że używają tylko `Notifications` (i pochodnie `Tenant`/`Project` tylko w 2 z 5).

## BLOK 5 — WEB MODELE

### 5.1 Sealed record z explicit properties

| WebModel | Sealed record | Explicit properties | Uwagi |
|----------|--------------|--------------------|----|
| `NotificationWeb` | NIE | NIE — positional record (12 parametrów konstruktora) | narusza wzorzec `sealed record` z `{ get; init; }` |

`NotificationWeb` ma 12 pól positional — dla takiej liczby pól wymóg explicit `{ get; init; }` jest szczególnie ważny dla czytelności i ewolucji modelu (named arguments).

`Dictionary<string, object?>?` jako pole — mutowalna kolekcja w "immutable" modelu. Rozważyć `IReadOnlyDictionary<string, object?>?`.

`Notification.CreatedAt` (encja) jest `DateTime`, `NotificationWeb.CreatedAt` jest `DateTimeOffset` — konwersja domyślna może zgubić kontekst strefy/Kind.

### 5.2 Duplikacje

| Duplikowane pola | W modelach | Kandydat do wydzielenia |
|-----------------|-----------|------------------------|
| brak (jeden web model w domenie) | — | nie dotyczy |

## BLOK 6 — PROBLEMY I REKOMENDACJE

### Krytyczne (błędy logiki lub bezpieczeństwa)

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|-------------|
| K1 | Walidator wykonuje query DB sprawdzające istnienie+ownership notyfikacji; pipeline rzuca `ValidationApiException` (HTTP 400) zamiast `NotFoundApiException` (HTTP 404) z handlera. | `MarkNotificationAsReadCommandValidator.cs` | Niespójna semantyka HTTP; podwójne zapytanie do DB; klient nie odróżni „nie ma" od „walidacja". | Usunąć regułę `MustAsync(NotificationMustExistAndBelongToUser)`. Pozostawić tylko `RuleFor(x => x.NotificationId).RequiredId()`. Sprawdzanie istnienia i ownership zostawić w handlerze (już istnieje i poprawnie rzuca `NotFoundApiException`). |
| K2 | Walidacja autentykacji w FluentValidation z `WithErrorCode("401")`. Pipeline mapuje validation na 400, więc kod 401 jest fikcyjny. Logika martwa, bo `[Authorize]` na kontrolerze zwraca 401 wcześniej. | `GetUnreadNotificationsQueryValidator.cs` | Mylący/martwy kod; jeśli `[Authorize]` zostanie usunięty, otrzymamy 400 zamiast 401. | Usunąć regułę `RuleFor(x => x).Must(_ => currentUser.IsAuthenticated...)`. Autentykacja należy do warstwy ASP.NET Auth, nie do walidatorów. |

### Wysokie (naruszenia wzorców, duplikacje, brakujące walidacje)

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|-------------|
| W1 | Wszystkie 5 Commands/Queries nie są `sealed` i 3/5 używa positional params zamiast `required { get; init; }`. | wszystkie pliki Command/Query | naruszenie konwencji projektu | Refaktor do `public sealed record … { public required ... { get; init; } }`. Dla parameterless: `public sealed record GetUnreadCounterQuery : IRequestQuery<int>;`. |
| W2 | Wszystkie 5 Handlerów nie jest `sealed`. | wszystkie `*Handler.cs` | naruszenie konwencji | Dodać `sealed`. |
| W3 | `MarkAllNotificationsAsReadCommandHandler` używa `var` — naruszenie zakazu `var`. | `MarkAllNotificationsAsReadCommandHandler.cs` | naruszenie konwencji | Zamienić na `IEnumerable<Notification> unreadNotifications = ...` i `foreach (Notification notification in ...)`. |
| W4 | Predykaty zapytań nie zawierają `TenantId`. Notyfikacje są filtrowane tylko po `UserId`. Jeśli user należy do wielu tenantów, dostaje wszystkie notyfikacje cross-tenant. | wszystkie 5 handlerów | potencjalne wycieki danych między tenantami; niejasna semantyka „inbox" vs „per-tenant" | Zdecydować z biznesem: czy notyfikacje są user-wide czy per-tenant. Jeśli per-tenant — dodać `n.TenantId == currentUser.TenantId` do wszystkich predykatów (wymaga `ICurrentUser.TenantId` lub przekazania w request) i przebudować routing na `/api/tenants/{tenantId}/notifications`. |
| W5 | `MarkAllNotificationsAsReadCommandHandler`: load wszystkich + foreach + `UpdateRange` zamiast `ExecuteUpdateAsync`. | `MarkAllNotificationsAsReadCommandHandler.cs` | wydajność/RAM przy dużych skrzynkach | Użyć bulk-update: `notificationRepo.ExecuteUpdateAsync(n => n.UserId == currentUser.Id && !n.IsRead, s => s.SetProperty(n => n.IsRead, true), ct)` (lub odpowiednika repo). |
| W6 | Duplikacja mapowania (`MapType`, `DeserializeMetadata`, projekcja na `NotificationWeb`) między `GetAllNotificationsQueryHandler` i `GetUnreadNotificationsQueryHandler`. | oba handlery | DRY; ryzyko rozjazdu przy zmianie modelu | Wydzielić `static class NotificationWebMapper` w `CQRS/Notifications/` z metodami `ToWeb(Notification)`, `MapType`, `DeserializeMetadata`. |
| W7 | `NotificationWeb` jest positional `record` (12 pól), nie `sealed`, nie używa `{ get; init; }`. | `NotificationWeb.cs` | naruszenie konwencji web modeli; trudność czytania przy 12 pozycyjnych argumentach | Zamienić na `public sealed record NotificationWeb { public required Guid Id { get; init; } ... }`. |
| W8 | Duplikacja walidacji paginacji (`Take`/`Skip`) między dwoma walidatorami; nie korzysta z `CommonValidationExtensions`. | `GetAllNotificationsQueryValidator.cs`, `GetUnreadNotificationsQueryValidator.cs` | DRY | Dodać extension `PageSize(int max=100)` i `NonNegativeOffset()` w `CommonValidationExtensions` i użyć w obu validatorach. Rozważyć wspólną klasę bazową `PagedQueryBase` lub `record PaginationParams`. |

### Normalne (styl, konwencje, drobne usprawnienia)

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|-------------|
| N1 | Wszystkie 5 handlerów ma masę nieużywanych usingów (`Entities.Models.Chats`, `Costs`, `Files`, `Projects`, `Roles`, `Tenants`, `Users`, `WorkSchedules`). | wszystkie `*Handler.cs` + `MarkNotificationAsReadCommandValidator.cs`, `GetUnreadNotificationsQueryValidator.cs` | szum w kodzie | Posprzątać usingi; rozważyć `GlobalUsings.cs` jeśli faktycznie wymagane. |
| N2 | `MarkNotificationAsReadCommandValidator`: `notification != null` zamiast `notification is not null`. | `MarkNotificationAsReadCommandValidator.cs` | spójność stylu | Zmienić na `is not null` (jeśli reguła w ogóle pozostanie — patrz K1). |
| N3 | `NotificationController` używa `var` w każdej akcji oraz brak explicit return types. Trasy bez `tenantId`, niespójne z konwencją `/api/tenants/{tenantId}/...`. | `NotificationController.cs` | konwencja | Rozważyć `/api/tenants/{tenantId}/notifications` jeśli zostanie podjęta decyzja o per-tenant. Zamienić `var` na typy konkretne. |
| N4 | `MarkAllNotificationsAsReadCommandHandler.unreadNotifications.Count()` wywoływane wielokrotnie (logger + return). | `MarkAllNotificationsAsReadCommandHandler.cs` | drobne | Zmaterializować raz: `int count = unreadNotifications.Count();`. |
| N5 | `NotificationWeb.Metadata` jest `Dictionary<string, object?>?` (mutable). | `NotificationWeb.cs` | mutowalność „immutable" modelu | Rozważyć `IReadOnlyDictionary<string, object?>?`. |
| N6 | Komentarze w kodzie po polsku (`// Jeśli już przeczytana, nie rób nic`, `// Walidacja: notyfikacja musi istnieć...`, komentarze w kontrolerze), reszta kodu i komunikatów po angielsku. | `MarkNotificationAsReadCommandHandler.cs`, `MarkNotificationAsReadCommandValidator.cs`, `NotificationController.cs` | spójność językowa | Zunifikować na angielski. |
| N7 | `GetAllNotificationsQueryHandler` używa emoji w logach (`📥`, `✅`). | `GetAllNotificationsQueryHandler.cs` | konwencja log-ów | Usunąć emoji z poziomu produkcyjnego logowania. |
| N8 | Brak konwencji `cancellationToken` przekazywanego do wszystkich operacji repo (`GetFirstBySearch`, `Update`, `GetBySearch`, `UpdateRange`). | `MarkNotificationAsReadCommandHandler.cs`, `MarkAllNotificationsAsReadCommandHandler.cs`, `MarkNotificationAsReadCommandValidator.cs` | brak anulowania długich operacji | Dodać `cancellationToken` do wszystkich wywołań repozytoriów. |
| N9 | `GetUnreadNotificationsQueryValidator` ma nadmiarowe `using Business.Interfaces.Exceptions;`. | `GetUnreadNotificationsQueryValidator.cs` | drobne | Usunąć. |
| N10 | `Notification.CreatedAt` typu `DateTime`, `NotificationWeb.CreatedAt` typu `DateTimeOffset`. Konwersja domyślna może spowodować zgubienie strefy/Kind. | `Notification.cs` vs `NotificationWeb.cs` | dane czasowe | Ujednolicić typy (preferować `DateTimeOffset` w encji) lub explicit konwersję z określonym `DateTimeKind`. |

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Liczba Commands | 2 |
| Liczba Queries | 3 |
| Liczba Walidatorów | 3 |
| Liczba Handlerów | 5 |
| Liczba Web Modeli | 1 |
| Commands/Queries z positional params | 3 (z 5; 2 są parameterless) |
| Commands/Queries bez `sealed` | 5/5 |
| Queries/Commands bez walidatora | 2 (`GetUnreadCounterQuery`, `MarkAllNotificationsAsReadCommand`) — uzasadnione (parameterless) |
| Handlery z `var` | 1 (`MarkAllNotificationsAsReadCommandHandler`) |
| Handlery bez `sealed` | 5/5 |
| Walidatory bez `sealed` | 2 (`GetUnreadNotificationsQueryValidator`, `MarkNotificationAsReadCommandValidator`) |
| Web modele niezgodne ze wzorcem | 1/1 |
| Problemy krytyczne | 2 |
| Problemy wysokie | 8 |
| Problemy normalne | 10 |
