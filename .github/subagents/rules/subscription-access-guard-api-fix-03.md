# API Fix 03 — ActiveTenantWeb + ChangeActiveTenantCommandHandler

## Feature
subscription-access-guard

## Cel
Modyfikacja `ActiveTenantWeb` (dodanie `IsSubscriptionBlocked`) oraz
`ChangeActiveTenantCommandHandler` (sprawdzenie statusu subskrypcji przed przełączeniem tenanta).

## Wymagania wstępne
Musi być wykonany po `api-fix-01` (wymaga `SubscriptionSuspendedException`).

---

## Krok 1 — Przeczytaj kontekst

Przed implementacją przeczytaj:
- `src/Business/Interfaces/WebModels/Tenants/ActiveTenantWeb.cs`
- `src/CQRS/Tenants/ChangeActiveTenant/ChangeActiveTenantCommand.cs`
- `src/CQRS/Tenants/ChangeActiveTenant/ChangeActiveTenantCommandHandler.cs`
- `src/Business/Interfaces/Model/ICurrentUser.cs` — metody: `GetTenantSnapshotAsync`, `IsSuperAdmin`
- `src/Entities/Models/Subscriptions/TenantSubscription.cs` — właściwości
- `src/Entities/Enums/SubscriptionStatus.cs`

---

## Krok 2 — Dodaj `IsSubscriptionBlocked` do `ActiveTenantWeb`

**Plik:** `src/Business/Interfaces/WebModels/Tenants/ActiveTenantWeb.cs`

Dodaj pole:
```csharp
public bool IsSubscriptionBlocked { get; init; }
```

---

## Krok 3 — Rozbuduj `ChangeActiveTenantCommandHandler`

**Plik:** `src/CQRS/Tenants/ChangeActiveTenant/ChangeActiveTenantCommandHandler.cs`

### Nowe zależności do wstrzyknięcia
- `IReadRepository<TenantSubscription>` — do sprawdzenia statusu subskrypcji
- `ICurrentUser` — już jest wstrzyknięty

### Logika po sprawdzeniu/zapisaniu profilu preferencji

**Przed** zwróceniem `ActiveTenantWeb` dodaj sprawdzenie:

```
1. Pobierz TenantSubscription: subscription = await subscriptionReadRepo.GetFirstBySearch(s => s.TenantId == request.TenantId)
2. Jeśli subscription is null → zwróć IsSubscriptionBlocked = false (Free plan, zawsze aktywny)
3. Sprawdź bool isBlocked = status is PastDue or Canceled or GracePeriod
4. Jeśli NIE isBlocked → zwróć IsSubscriptionBlocked = false
5. Jeśli isBlocked:
   a. Pobierz snapshot: var snapshot = await currentUser.GetTenantSnapshotAsync(request.TenantId, cancellationToken)
   b. bool isTenantAdmin = snapshot?.IsTenantAdmin == true || currentUser.IsSuperAdmin
   c. Jeśli isTenantAdmin → zwróć IsSubscriptionBlocked = true (admin może się przełączyć)
   d. Jeśli NIE isTenantAdmin → throw new SubscriptionSuspendedException(request.TenantId)
```

### Ważne
- Zmiana ActiveTenantId w profilu powinna nastąpić **przed** sprawdzeniem subskrypcji
  (admin potrzebuje aktywnego tenanta ustawionego, żeby trafić na właściwą stronę)
- Prywatna metoda pomocnicza:
  ```csharp
  private static bool IsSubscriptionBlocked(SubscriptionStatus status)
      => status is SubscriptionStatus.PastDue or SubscriptionStatus.Canceled or SubscriptionStatus.GracePeriod;
  ```

---

## Weryfikacja
- `dotnet build src/WebApi/WebApi.csproj --no-incremental` — musi przejść bez błędów
