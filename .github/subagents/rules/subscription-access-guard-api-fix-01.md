# API Fix 01 — Wyjątek HTTP 402 + Marker Interface

## Feature
subscription-access-guard

## Cel
Dodanie nowej klasy wyjątku `SubscriptionSuspendedException` (HTTP 402),
nowej wartości `ApiExceptionReason.SubscriptionSuspended` oraz marker interface `IBypassSubscriptionCheck`.

---

## Krok 1 — Dodaj `ApiExceptionReason.SubscriptionSuspended`

**Plik:** `src/Business/Interfaces/Exceptions/ApiExceptionReason.cs`

Przeczytaj plik i dodaj nową wartość `SubscriptionSuspended` do enuma.
Upewnij się że `GetStatusCode()` w `ApiException.cs` obsługuje tę wartość zwracając `402`.

Przeczytaj `ApiException.cs` i sprawdź jak `GetStatusCode()` jest zaimplementowany.
Jeśli używa switch expression — dodaj gałąź dla `SubscriptionSuspended → 402`.

---

## Krok 2 — Utwórz `SubscriptionSuspendedException`

**Plik:** `src/Business/Interfaces/Exceptions/SubscriptionSuspendedException.cs`

Wzoruj się na istniejących wyjątkach (np. `ForbiddenApiException.cs` lub `ConflictApiException.cs`).
Klasa musi dziedziczyć po `ApiException`, ustawiać `Reason = ApiExceptionReason.SubscriptionSuspended`.

Konstruktor powinien przyjmować `Guid tenantId` i ustawiać sensowny komunikat np.:
`"Tenant subscription is suspended. Only an admin can access this tenant to renew the subscription."`

---

## Krok 3 — Utwórz `IBypassSubscriptionCheck`

**Plik:** `src/CQRS/Behaviours/IBypassSubscriptionCheck.cs`

Prosty marker interface:
```csharp
namespace CQRS.Behaviours;

/// <summary>
/// Oznacza komendy/zapytania które są przepuszczane przez SubscriptionEnforcementBehavior
/// niezależnie od statusu subskrypcji tenanta (np. płatność, status subskrypcji).
/// </summary>
public interface IBypassSubscriptionCheck { }
```

---

## Weryfikacja
- `dotnet build src/WebApi/WebApi.csproj --no-incremental` — musi przejść bez błędów
- Sprawdź że `ApiExceptionReason.SubscriptionSuspended` kompiluje się i `GetStatusCode()` zwraca 402
