# API Fix 02 — SubscriptionEnforcementBehavior

## Feature
subscription-access-guard

## Cel
Nowy MediatR pipeline behavior który sprawdza status subskrypcji tenanta
dla każdego `IAuthorizableRequest` i blokuje dostęp gdy subskrypcja nie jest aktywna.

## Wymagania wstępne
Musi być wykonany po `api-fix-01` (wymaga `SubscriptionSuspendedException` i `IBypassSubscriptionCheck`).

---

## Krok 1 — Przeczytaj kontekst

Przed implementacją przeczytaj:
- `src/CQRS/Behaviours/SubscriptionLimitsBehavior.cs` — wzorzec behavior
- `src/CQRS/Behaviours/AuthorizationBehavior.cs` — kolejność w pipeline
- `src/Business/Interfaces/Model/ICurrentUser.cs` — dostępne metody (GetTenantSnapshotAsync)
- `src/Business/Interfaces/Model/ContextSnapshots.cs` — co zawiera `TenantCtxSnapshot` (w szczególności `IsTenantAdmin`)
- `src/Entities/Models/Subscriptions/TenantSubscription.cs` — właściwości encji
- `src/Entities/Enums/SubscriptionStatus.cs` — wartości enuma
- `src/WebApi/Extensions/ServiceCollectionExtensions.cs` — kolejność rejestracji behaviors

---

## Krok 2 — Utwórz `SubscriptionEnforcementBehavior`

**Plik:** `src/CQRS/Behaviours/SubscriptionEnforcementBehavior.cs`

### Logika

```
1. Jeśli request implementuje IBypassSubscriptionCheck → przepuść (next())
2. Jeśli request NIE implementuje IAuthorizableRequest → przepuść (next())
3. Pobierz TenantId z request.GetResource().TenantId
4. Jeśli TenantId == Guid.Empty → przepuść (Global scope permissions)
5. Pobierz TenantSubscription z IReadRepository<TenantSubscription>
   - predykat: s => s.TenantId == tenantId
   - Jeśli subscription is null → przepuść (brak subskrypcji = Free plan, zawsze aktywny)
6. Sprawdź IsSubscriptionBlocked(subscription.Status):
   - PastDue → true
   - Canceled → true
   - GracePeriod → true
   - Active, Trialing → false
7. Jeśli NIE zablokowany → przepuść (next())
8. Pobierz TenantCtxSnapshot przez currentUser.GetTenantSnapshotAsync(tenantId, ct)
9. Jeśli snapshot?.IsTenantAdmin == true → przepuść (admin może dostać się do tenanta)
10. Jeśli user.IsSuperAdmin → przepuść
11. Throw SubscriptionSuspendedException(tenantId)
```

### Prywatna metoda pomocnicza
```csharp
private static bool IsSubscriptionBlocked(SubscriptionStatus status)
    => status is SubscriptionStatus.PastDue or SubscriptionStatus.Canceled or SubscriptionStatus.GracePeriod;
```

### Ważne
- Behavior ma być `public sealed class SubscriptionEnforcementBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull`
- Wstrzyknij: `ICurrentUser`, `IReadRepository<TenantSubscription>`, `ILogger<SubscriptionEnforcementBehavior<TRequest, TResponse>>`
- Używaj `IAuthorizableRequest` (ten sam namespace co w `AuthorizationBehavior`)
- **NIE modyfikuj** `TenantCtxSnapshot` — snapshot pobierasz tylko do sprawdzenia `IsTenantAdmin`

---

## Krok 3 — Zarejestruj behavior w DI

**Plik:** `src/WebApi/Extensions/ServiceCollectionExtensions.cs`

Przeczytaj plik. Znajdź metodę `AddCqrs()` (lub podobną) gdzie rejestrowane są behaviors.
Dodaj `SubscriptionEnforcementBehavior` **po** `AuthorizationBehavior` i **przed** `AssignedAuthorizationBehavior`.

Wzorzec rejestracji:
```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SubscriptionEnforcementBehavior<,>));
```

---

## Weryfikacja
- `dotnet build src/WebApi/WebApi.csproj --no-incremental` — musi przejść bez błędów
