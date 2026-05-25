# API Fix 04 — subscriptionStatus w GET /tenants/my-tenants

## Feature
subscription-access-guard

## Cel
Dodanie pola `subscriptionStatus` do odpowiedzi `GET /tenants/my-tenants`
aby UI mogło wyświetlić badge statusu subskrypcji na liście tenantów bez dodatkowych requestów.

---

## Krok 1 — Przeczytaj kontekst

Przed implementacją przeczytaj:
- `src/CQRS/Tenants/GetMyTenants/` — przeczytaj Query, Handler i WebModel
- `src/Business/Interfaces/WebModels/Tenants/` — znajdź model `UserTenantWeb` lub podobny
- `src/Entities/Enums/SubscriptionStatus.cs`
- `src/Entities/Models/Subscriptions/TenantSubscription.cs`

---

## Krok 2 — Dodaj `SubscriptionStatus?` do web modelu tenanta

Znajdź web model używany przez `GetMyTenantsQuery` (np. `UserTenantWeb`, `TenantListItemWeb` lub podobny).

Dodaj pole:
```csharp
/// <summary>
/// Status subskrypcji tenanta. NULL dla planu Free (brak ograniczeń).
/// Używany przez UI do wyświetlenia badge statusu i blokady przełączania.
/// </summary>
public SubscriptionStatus? SubscriptionStatus { get; init; }
```

---

## Krok 3 — Rozbuduj handler `GetMyTenantsQueryHandler`

**Plik:** `src/CQRS/Tenants/GetMyTenants/GetMyTenantsQueryHandler.cs`

Dodaj join lub osobne pobranie `TenantSubscription` dla każdego tenanta.

### Preferowane podejście — pobranie subskrypcji w jednym query

Po pobraniu listy tenantów (membershipów), pobierz wszystkie subskrypcje jednym zapytaniem:
```csharp
var tenantIds = memberships.Select(m => m.TenantId).ToList();
var subscriptions = await subscriptionReadRepo.GetBySearch(
    s => tenantIds.Contains(s.TenantId));
var subscriptionByTenantId = subscriptions.ToDictionary(s => s.TenantId);
```

Przy mapowaniu na web model:
```csharp
SubscriptionStatus = subscriptionByTenantId.TryGetValue(tenantId, out var sub) ? sub.Status : null
```

### Ważne
- Wstrzyknij `IReadRepository<TenantSubscription>` do handlera
- Nie filtruj — zwróć status niezależnie od wartości (Active, PastDue, Canceled, etc.)
- Free plan zazwyczaj nie ma rekordu `TenantSubscription` lub ma `Status = Active` — oba przypadki są obsługiwane przez nullable

---

## Weryfikacja
- `dotnet build src/WebApi/WebApi.csproj --no-incremental` — musi przejść bez błędów
