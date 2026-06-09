# Notifications — Podsumowanie audytu i refaktoru

Domena **Notifications** w `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/Notifications/`.

## Inwentaryzacja

| Pozycja | Liczba |
|---------|--------|
| Commands | 2 |
| Queries | 3 |
| Handlers | 5 |
| Validators | 3 |
| Web models | 1 |
| Endpointy kontrolera | 5 |

## Zakres audytu

Pełny raport: [.github/subagents/rules/notifications-audit.md](notifications-audit.md)

Zidentyfikowano **2 problemy krytyczne / 8 wysokich / 10 normalnych**.

## Decyzje domenowe

| Pytanie | Decyzja |
|---------|---------|
| Notyfikacje per-tenant czy user-wide? | **User-wide** — routing `/api/notification` zostaje, predykaty filtrują tylko po `UserId` |
| Bulk update przez `ExecuteUpdateAsync`? | Tak |
| Wydzielić `NotificationWebMapper`? | Tak |
| Dodać `PageSize()` / `NonNegativeOffset()` do `CommonValidationExtensions`? | Tak |

## Wykonane prompty refaktoru

| # | Plik | Zakres | Build |
|---|------|--------|-------|
| 01 | [notifications-fix-01.md](notifications-fix-01.md) | Krytyczne walidatory (K1, K2): usunięcie zapytania DB z `MarkNotificationAsReadCommandValidator`, usunięcie walidacji autentykacji z `GetUnreadNotificationsQueryValidator`, sealed validators, sprzątanie usingów | ✅ 0 błędów |
| 02 | [notifications-fix-02.md](notifications-fix-02.md) | Dodanie `PageSize()` i `NonNegativeOffset()` do `CommonValidationExtensions`, użycie ich + `RequiredId()` w 3 validatorach | ✅ 0 błędów |
| 03 | [notifications-fix-03.md](notifications-fix-03.md) | Commands/Queries/`NotificationWeb` → `sealed record` z `required { get; init; }`; `Metadata` → `IReadOnlyDictionary`; kontroler bez `var` z object initializerami | ✅ 0 błędów |
| 04 | [notifications-fix-04.md](notifications-fix-04.md) | Handlery: `sealed`, eliminacja `var`, wydzielenie `NotificationWebMapper`, bulk-update przez `ExecuteUpdateAsync`, sprzątanie usingów/komentarzy/emoji, `cancellationToken` | ✅ 0 błędów |

## Stan końcowy

| Metryka | Przed | Po |
|---------|-------|-----|
| Commands/Queries `sealed` | 0/5 | 5/5 |
| Commands/Queries z `required { get; init; }` lub parameterless | 0/5 (pozycyjne lub bez sealed) | 5/5 |
| Validators `sealed` | 1/3 | 3/3 |
| Validators wykonujące zapytanie DB | 1 | 0 |
| Validators z walidacją autentykacji | 1 | 0 |
| Handlers `sealed` | 0/5 | 5/5 |
| Handlers z `var` | 1/5 | 0/5 |
| Web modele `sealed record` z explicit properties | 0/1 | 1/1 |
| Duplikacja mapowania (`MapType`, `DeserializeMetadata`) | TAK (2×) | NIE (`NotificationWebMapper`) |
| `MarkAll` używa `ExecuteUpdateAsync` (bulk SQL) | NIE | TAK |
| Krytyczne problemy | 2 | 0 |

## Pozostałe (świadomie odłożone)

- **W4 — TenantId w predykatach**: świadomie pominięte zgodnie z decyzją domenową (notyfikacje user-wide). Jeśli w przyszłości powstanie potrzeba per-tenant inboxów, należy:
  1. Dodać `TenantId` do Commands/Queries.
  2. Przebudować routing na `/api/tenants/{tenantId}/notifications`.
  3. Dodać `n.TenantId == request.TenantId` do wszystkich predykatów.
- **N3 — `NotificationController` routing**: pozostaje `/api/notification` zgodnie z decyzją user-wide.
- **N10 — `Notification.CreatedAt` (`DateTime`) vs `NotificationWeb.CreatedAt` (`DateTimeOffset`)**: zachowano obecny kontrakt — konwersja domyślna pozostaje. Do rozważenia migracja schematu w osobnym PR jeśli pojawią się problemy ze strefami.
- **Drobny `cancellationToken` w `MarkNotificationAsReadCommandHandler.GetFirstBySearch`**: brak overloadu w `IRepository<T>` (jest tylko w `IReadRepository<T>`). Wymagałoby zmiany interfejsu repo — poza zakresem tej domeny.

## Wynik

Wszystkie 2 krytyczne problemy rozwiązane. Wszystkie 8 problemów wysokich z wyjątkiem W4 (świadomie odłożone) rozwiązane. Większość problemów normalnych również rozwiązana w toku fix-04. Build solution: ✅ 0 błędów.
