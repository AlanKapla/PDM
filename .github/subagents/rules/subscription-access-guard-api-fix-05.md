# API Fix 05 — IBypassSubscriptionCheck na komendach/zapytaniach subskrypcji

## Feature
subscription-access-guard

## Cel
Oznaczenie `IBypassSubscriptionCheck` na komendach/zapytaniach które muszą być
dostępne nawet gdy subskrypcja tenanta jest zablokowana (płatność, podgląd statusu).

## Wymagania wstępne
Musi być wykonany po `api-fix-01` (wymaga `IBypassSubscriptionCheck`).

---

## Krok 1 — Przeczytaj kontekst

Przed implementacją przeczytaj:
- `src/CQRS/Subscriptions/ProcessMockPayment/ProcessMockPaymentCommand.cs`
- `src/CQRS/Subscriptions/GetSubscriptionStatus/GetSubscriptionStatusQuery.cs`
- `src/CQRS/Behaviours/IBypassSubscriptionCheck.cs` (utworzony w api-fix-01)

---

## Krok 2 — Dodaj `IBypassSubscriptionCheck` do `ProcessMockPaymentCommand`

**Plik:** `src/CQRS/Subscriptions/ProcessMockPayment/ProcessMockPaymentCommand.cs`

Dodaj implementację interfejsu:
```csharp
public sealed record ProcessMockPaymentCommand : IRequestCommand<...>, IAuthorizableRequest, IBypassSubscriptionCheck
```

---

## Krok 3 — Dodaj `IBypassSubscriptionCheck` do `GetSubscriptionStatusQuery`

**Plik:** `src/CQRS/Subscriptions/GetSubscriptionStatus/GetSubscriptionStatusQuery.cs`

Dodaj implementację interfejsu analogicznie jak w kroku 2.

---

## Krok 4 — Dodaj `IBypassSubscriptionCheck` do `ChangeActiveTenantCommand`

**Plik:** `src/CQRS/Tenants/ChangeActiveTenant/ChangeActiveTenantCommand.cs`

`ChangeActiveTenantCommand` nie implementuje `IAuthorizableRequest` więc behavior i tak go pomija,
ale dla pewności i dokumentacji dodaj `IBypassSubscriptionCheck`.

---

## Weryfikacja
- `dotnet build src/WebApi/WebApi.csproj --no-incremental` — musi przejść bez błędów
- Sprawdź że `SubscriptionEnforcementBehavior` ma test działania (opcjonalnie)
