# Audyt API — Feature: subscription-access-guard

**Data audytu:** 2026-05-22  
**Feature spec:** `.github/features/subscription-access-guard.md`

---

## BLOK 1 — Stan obecny

### Zaangażowane encje
- `TenantSubscription` — `src/Entities/Models/Subscriptions/TenantSubscription.cs`  
  Posiada `Status: SubscriptionStatus`, `TenantId`, `IsSubscriptionActive` (bool computed property).
- `SubscriptionStatus` enum — `src/Entities/Enums/SubscriptionStatus.cs`  
  Wartości: `Active=0, Trialing=1, PastDue=2, Canceled=3, GracePeriod=4`.

### Snapshoty kontekstu
- `TenantCtxSnapshot` — `src/Business/Interfaces/Model/ContextSnapshots.cs`  
  Zawiera: `TenantId, TenantRoleId, TenantPermissionCodes, IsTenantAdmin, IsActive`.  
  **Brak: `SubscriptionStatus` ani żadnej informacji o subskrypcji.**
- `ICurrentUser.GetTenantSnapshotAsync()` / `GetActiveTenantSnapshotAsync()` — `src/Business/Implementation/Model/CurrentUser.cs` linie 154–181.  
  Zwraca `TenantCtxSnapshot?`. Snapshot jest cache'owany przez `InMemoryUserContextCache` (TTL 3 min).

### Pipeline behaviors — kolejność rejestracji
Plik: `src/WebApi/Extensions/ServiceCollectionExtensions.cs` (metoda `AddCqrs`)

| Kolejność | Behavior | Marker interface |
|-----------|----------|-----------------|
| 1 | `LoggingBehavior` | — (wszystkie requesty) |
| 2 | `ValidationBehavior` | — (wszystkie walidatory FluentValidation) |
| 3 | `SuperAdminBehavior` | `ISuperAdminRequest` |
| 4 | `AuthorizationBehavior` | `IAuthorizableRequest` |
| 5 | `AssignedAuthorizationBehavior` | (własny marker) |
| 6 | `SubscriptionLimitsBehavior` | `IRequiresProjectSlot` / `IRequiresUserSlot` |
| 7 | `TransactionBehavior` | — (wszystkie) |

Nowy `SubscriptionEnforcementBehavior` musi być wstawiony **na pozycji 5** (po `AuthorizationBehavior`, przed `AssignedAuthorizationBehavior`).

### Wyjątki
Katalog: `src/Business/Interfaces/Exceptions/`

| Klasa | Reason | HTTP |
|-------|--------|------|
| `ForbiddenApiException` | `Forbidden` | 403 |
| `NotFoundApiException` | `NotFound` | 404 |
| `UnauthorizedApiException` | `Unauthorized` | 401 |
| `ConflictApiException` | `Conflict` | 409 |
| `ValidationApiException` | `ValidationError` | 400 |
| `NotImplementedApiException` | `InvalidOperation` | 501 |

Brakuje klasy dla HTTP 402. Middleware `ApiExceptionMiddleware` wywołuje `ex.GetStatusCode()` i serializuje odpowiedź jako `{ error, message, objectType, objectId }`.

### Istniejące Commands/Queries z obszaru subskrypcji

| Command/Query | Plik | IAuthorizableRequest | PermissionCode |
|--------------|------|---------------------|---------------|
| `GetSubscriptionStatusQuery` | `src/CQRS/Subscriptions/GetSubscriptionStatus/GetSubscriptionStatusQuery.cs` | **TAK** | `PermissionCodes.TenantView` |
| `ProcessMockPaymentCommand` | `src/CQRS/Subscriptions/ProcessMockPayment/ProcessMockPaymentCommand.cs` | **TAK** | `PermissionCodes.TenantEdit` |
| `GetTenantSubscriptionQuery` | `src/CQRS/Admin/Subscriptions/GetTenantSubscription/GetTenantSubscriptionQuery.cs` | NIE (`ISuperAdminRequest`) | — |

### ChangeActiveTenantCommand
Plik: `src/CQRS/Tenants/ChangeActiveTenant/ChangeActiveTenantCommand.cs`  
```csharp
public sealed record ChangeActiveTenantCommand : IRequestCommand<ActiveTenantWeb>
{
    public required Guid TenantId { get; init; }
}
```
**Brak `IAuthorizableRequest`** — nie przechodzi przez `AuthorizationBehavior`.  
Handler `ChangeActiveTenantCommandHandler.cs` (linie 22–40): bezpośrednio zapisuje `ActiveTenantId` w `TenantPreferencesProfile` bez żadnej weryfikacji subskrypcji.

### ActiveTenantWeb
Plik: `src/Business/Interfaces/WebModels/Tenants/ActiveTenantWeb.cs`
```csharp
public sealed record ActiveTenantWeb
{
    public Guid? ActiveTenantId { get; init; }
}
```
**Brak `IsSubscriptionBlocked: bool`** wymaganego przez feature spec (pkt 5).

---

## BLOK 2 — Luki i braki

| Brak / Luka | Warstwa | Priorytet | Opis |
|-------------|---------|-----------|------|
| Brak `SubscriptionSuspendedException` | Business.Interfaces.Exceptions | KRYTYCZNY | Potrzebna klasa z HTTP 402 i `ApiExceptionReason.SubscriptionSuspended` |
| Brak `ApiExceptionReason.SubscriptionSuspended` | Business.Interfaces.Exceptions | KRYTYCZNY | Nowa wartość enuma; `GetStatusCode()` musi obsłużyć → 402 |
| Brak `SubscriptionEnforcementBehavior` | CQRS.Behaviours | KRYTYCZNY | Nowy behavior sprawdzający status subskrypcji per request |
| Brak `IBypassSubscriptionCheck` | CQRS | KRYTYCZNY | Marker interface do oznaczania komend/zapytań wyłączonych z blokady |
| Brak `SubscriptionStatus` w `TenantCtxSnapshot` | Business.Interfaces.Model | WYSOKI | Behavior potrzebuje statusu subskrypcji; bez tego wymagane dodatkowe DB query |
| Brak logiki subskrypcji w `ChangeActiveTenantCommandHandler` | CQRS.Tenants | WYSOKI | Handler musi sprawdzić status i zwrócić `IsSubscriptionBlocked` |
| Brak `IsSubscriptionBlocked` w `ActiveTenantWeb` | Business.Interfaces.WebModels | WYSOKI | UI oczekuje tego pola w odpowiedzi ChangeActiveTenant |
| Brak `IBypassSubscriptionCheck` na `ProcessMockPaymentCommand` | CQRS.Subscriptions | WYSOKI | Komenda płatności musi być dostępna pomimo blokady |
| Brak `IBypassSubscriptionCheck` na `GetSubscriptionStatusQuery` | CQRS.Subscriptions | WYSOKI | Query statusu musi być dostępne pomimo blokady |
| Brak rejestracji `SubscriptionEnforcementBehavior` | WebApi.Extensions | WYSOKI | Należy dodać po `AuthorizationBehavior` |

---

## BLOK 3 — Zmiany w encjach/DB

| Encja | Zmiana | Typ | Wymaga migracji |
|-------|--------|-----|----------------|
| `TenantCtxSnapshot` (record, nie encja EF) | Dodanie `SubscriptionStatus SubscriptionStatus` | nowe pole w record | NIE (model in-memory) |
| `TenantSubscription` | Brak zmian w schemacie | — | NIE |
| `ActiveTenantWeb` (DTO, nie encja EF) | Dodanie `bool IsSubscriptionBlocked` | nowe pole w record | NIE |

> **Uwaga:** `TenantCtxSnapshot` to record in-memory (nie EF entity). Dodanie pola wymaga aktualizacji: konstruktora, `BuildTenantSnapshotAsync` w `CurrentUser.cs`, oraz wszystkich place gdzie snapshot jest tworzony w testach.

---

## BLOK 4 — Nowe Commands/Queries

| Command/Query | Typ | Opis | Handler |
|--------------|-----|------|---------|
| `GetSubscriptionStatusQuery` | modyfikacja — dodać `IBypassSubscriptionCheck` | Query musi przejść przez behavior mimo blokady | `GetSubscriptionStatusQueryHandler` — bez zmian w handlerze |
| `ProcessMockPaymentCommand` | modyfikacja — dodać `IBypassSubscriptionCheck` | Płatność musi być możliwa pomimo blokady | `ProcessMockPaymentCommandHandler` — bez zmian w handlerze |
| `ChangeActiveTenantCommand` | modyfikacja — dodać `IBypassSubscriptionCheck` + logika w handlerze | Zmiana tenanta ma własną logikę (patrz Blok 6) | `ChangeActiveTenantCommandHandler` — rozbudowa |

> `GetTenantSubscriptionQuery` implementuje `ISuperAdminRequest` (nie `IAuthorizableRequest`) — **nie** przechodzi przez `SubscriptionEnforcementBehavior` jeśli behavior filtruje po `IAuthorizableRequest`. Brak konieczności zmian.

---

## BLOK 5 — Zmiany w kontrolerach

| Endpoint | HTTP Method | Nowy/Modyfikacja | Opis |
|----------|-------------|-----------------|------|
| `PUT /api/tenants/active` | PUT | brak zmian w kontrolerze | Zmiana odpowiedzi następuje przez `ActiveTenantWeb.IsSubscriptionBlocked`; kontroler nie wymaga modyfikacji |

Żaden nowy endpoint nie jest wymagany po stronie API dla tej feature.

---

## BLOK 6 — Zmiany w serwisach

| Serwis | Interfejs | Nowy/Modyfikacja | Metody |
|--------|-----------|-----------------|--------|
| `ChangeActiveTenantCommandHandler` | — (handler) | MODYFIKACJA | Dodać: pobranie `TenantSubscription` dla `request.TenantId`, sprawdzenie `IsSubscriptionBlocked`, warunkowe zwrócenie `IsSubscriptionBlocked = true` dla TenantAdmin lub throw `SubscriptionSuspendedException` dla zwykłego membera |

Opcjonalnie (jeśli `SubscriptionStatus` nie wejdzie do `TenantCtxSnapshot`):

| Serwis | Interfejs | Nowy/Modyfikacja | Metody |
|--------|-----------|-----------------|--------|
| Nowy `ISubscriptionStatusService` | `ISubscriptionStatusService` | NOWY (opcjonalny) | `Task<bool> IsBlockedAsync(Guid tenantId, CancellationToken ct)` — enkapsuluje sprawdzenie statusu w `SubscriptionEnforcementBehavior` i handlerze |

---

## BLOK 7 — Problemy i ryzyka

| # | Problem | Warstwa | Ryzyko | Rekomendacja |
|---|---------|---------|--------|-------------|
| P1 | **`IsSubscriptionActive` w `TenantSubscription` zawiera `GracePeriod` jako aktywny**, ale feature spec mówi że `GracePeriod` → zablokowany | Entities | KRYTYCZNY — sprzeczność domenowa | Decyzja wymagana (patrz Pytania). W behavior użyć własnego warunku (`Active or Trialing`) zamiast `IsSubscriptionActive`. |
| P2 | **`ChangeActiveTenantCommand` nie implementuje `IAuthorizableRequest`** — nie przechodzi przez `AuthorizationBehavior` | CQRS | WYSOKI — handler musi ręcznie sprawdzić membership i subskrypcję | Dodać weryfikację w handlerze explicite; nie dodawać `IAuthorizableRequest` jeśli nie ma sensu semantycznie |
| P3 | **`TenantCtxSnapshot` nie zawiera `SubscriptionStatus`** — każde wywołanie behavior bez cache'u subskrypcji powoduje dodatkowe DB query | CQRS / Business | ŚREDNI — wydajność | Opcja A: dodać `SubscriptionStatus` do snapshotu (zmiana sygnatury constructora, update cache, update testów). Opcja B: behavior robi `IReadRepository<TenantSubscription>` query z własnym cache'em w scope requestu. |
| P4 | **`TenantCtxSnapshot` cache TTL = 3 min** — po opłaceniu subskrypcji user może być zablokowany jeszcze do 3 minut | Business | NISKI — UX | Po `ProcessMockPaymentCommand` wymusić inwalidację cache dla tenanta. Lub behavior zawsze czyta status bezpośrednio (z własnym krótkim cache'em). |
| P5 | **`ApiExceptionMiddleware` serializuje `Reason.ToString()`** jako pole `error` w response body — UI musi obsługiwać `"SubscriptionSuspended"` jako string | WebApi | NISKI | Dodać handling w interceptorze Axios po stronie UI (patrz feature spec UI) |
| P6 | **Brak testów jednostkowych** dla nowego behavior, `SubscriptionSuspendedException`, modyfikacji `ChangeActiveTenantCommandHandler` | CQRS.Tests / Business.Tests | ŚREDNI | Wzorzec istniejący: `SubscriptionLimitsBehavior` nie ma testów — jednak nowy behavior zawiera złożoną logikę warunkową (admin/member) i powinien być przetestowany |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe encje EF | 0 |
| Nowe DTO/modele | 1 (`SubscriptionSuspendedException`) |
| Nowe marker interfaces | 1 (`IBypassSubscriptionCheck`) |
| Nowe Behaviors | 1 (`SubscriptionEnforcementBehavior`) |
| Modyfikowane Commands | 3 (`ProcessMockPaymentCommand`, `GetSubscriptionStatusQuery`, `ChangeActiveTenantCommand`) |
| Modyfikowane Handlers | 1 (`ChangeActiveTenantCommandHandler`) |
| Modyfikowane modele | 2 (`ActiveTenantWeb`, `ApiExceptionReason`, opcjonalnie `TenantCtxSnapshot`) |
| Nowe endpointy | 0 |
| Wymaga migracji DB | **NIE** |
| Pytania domenowe | 3 |

---

## Pytania domenowe wymagające decyzji

1. **GracePeriod — aktywny czy blokujący?**  
   Feature spec mówi: `GracePeriod → zablokowany`. Istniejąca właściwość `TenantSubscription.IsSubscriptionActive` traktuje `GracePeriod` jako aktywny (`Active or Trialing or GracePeriod`). Czy `IsSubscriptionActive` ma zostać zmienione (zmiana semantyki używana przez `SubscriptionLimitsBehavior`), czy behavior ma użyć własnego predykatu ignorując tę właściwość?

2. **SubscriptionStatus w TenantCtxSnapshot — tak czy nie?**  
   Dodanie `SubscriptionStatus` do `TenantCtxSnapshot` eliminuje dodatkowe DB query w behavior i handlerze, ale wymaga aktualizacji konstruktora snapshotu, `BuildTenantSnapshotAsync`, `InMemoryUserContextCache` i **~10 miejsc w testach** gdzie snapshot jest tworzony ręcznie. Preferowane podejście?

3. **Inwalidacja cache po opłaceniu subskrypcji?**  
   Po `ProcessMockPaymentCommand` cache tenanta (TTL 3 min) może nadal zwracać stary status. Czy po udanej płatności należy wymusić inwalidację `TenantCtxSnapshot` dla danego tenanta, czy opóźnienie 3 min jest akceptowalne?

---

## Szczegółowe rekomendacje implementacyjne

### Krok 1 — `ApiExceptionReason` + `SubscriptionSuspendedException`

**Plik:** `src/Business/Interfaces/Exceptions/ApiExceptionReason.cs`  
Dodać wartość: `SubscriptionSuspended`

**Plik:** `src/Business/Interfaces/Exceptions/ApiException.cs` — zmiana w `GetStatusCode()`:
```csharp
ApiExceptionReason.SubscriptionSuspended => (HttpStatusCode)402,
```

**Nowy plik:** `src/Business/Interfaces/Exceptions/SubscriptionSuspendedException.cs`
```csharp
public class SubscriptionSuspendedException(string message)
    : ApiException(ApiExceptionReason.SubscriptionSuspended, message)
{
    public override HttpStatusCode GetStatusCode() => (HttpStatusCode)402;
}
```

### Krok 2 — `IBypassSubscriptionCheck`

**Nowy plik:** `src/CQRS/IBypassSubscriptionCheck.cs`
```csharp
/// <summary>
/// Marker interface — request zawsze przechodzi przez SubscriptionEnforcementBehavior
/// bez blokady, nawet gdy subskrypcja tenanta jest zawieszona.
/// </summary>
public interface IBypassSubscriptionCheck { }
```

### Krok 3 — Oznaczenie istniejących komend markerem

**`ProcessMockPaymentCommand`** — dodać `: IBypassSubscriptionCheck` do deklaracji  
**`GetSubscriptionStatusQuery`** — dodać `: IBypassSubscriptionCheck`  
**`ChangeActiveTenantCommand`** — dodać `: IBypassSubscriptionCheck` (własna logika w handlerze)

### Krok 4 — `SubscriptionEnforcementBehavior`

**Nowy plik:** `src/CQRS/Behaviours/SubscriptionEnforcementBehavior.cs`

Wzorzec: analogiczny do `SubscriptionLimitsBehavior` (wstrzykiwanie przez konstruktor).  
Logika:
1. Jeśli `request is IBypassSubscriptionCheck` → `return await next()`
2. Jeśli `request is not IAuthorizableRequest` → `return await next()` (brak TenantId)
3. Pobierz `TenantId` z `authorizableRequest.GetResource().TenantId`
4. Jeśli `TenantId == Guid.Empty` → `return await next()`
5. Pobierz `TenantSubscription` z `IReadRepository<TenantSubscription>` (gdzie `s.TenantId == tenantId`)
6. Sprawdź czy status blokujący: `status is PastDue or Canceled or GracePeriod` (nie używać `IsSubscriptionActive` — patrz P1)
7. Jeśli NIE blokujący → `return await next()`
8. Pobierz `TenantCtxSnapshot` przez `currentUser.GetTenantSnapshotAsync(tenantId, ct)`
9. Jeśli `snapshot.IsTenantAdmin` → `return await next()` (admin przechodzi)
10. Throw `new SubscriptionSuspendedException("Subskrypcja tenanta jest nieaktywna. Skontaktuj się z administratorem.")`

### Krok 5 — Rejestracja behavior

**Plik:** `src/WebApi/Extensions/ServiceCollectionExtensions.cs`  
Dodać **po** `AuthorizationBehavior`, **przed** `AssignedAuthorizationBehavior`:
```csharp
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SubscriptionEnforcementBehavior<,>));
```

### Krok 6 — `ActiveTenantWeb` + `ChangeActiveTenantCommandHandler`

**`ActiveTenantWeb`** — dodać pole:
```csharp
public bool IsSubscriptionBlocked { get; init; }
```

**`ChangeActiveTenantCommandHandler.Handle`** — przed zapisem do repo:
1. Pobierz `TenantSubscription` dla `request.TenantId`
2. Oblicz `isBlocked = subscription?.Status is PastDue or Canceled or GracePeriod`
3. Jeśli `isBlocked`:
   - Pobierz snapshot `currentUser.GetTenantSnapshotAsync(request.TenantId, ct)`
   - Jeśli NIE jest admin → `throw new SubscriptionSuspendedException(...)`
   - Jeśli jest admin → kontynuuj zapis, zwróć `IsSubscriptionBlocked = true`
4. Zwróć `new ActiveTenantWeb { ActiveTenantId = ..., IsSubscriptionBlocked = isBlocked }`

> Handler wymaga nowych zależności: `IReadRepository<TenantSubscription>` i `ICurrentUser`.
