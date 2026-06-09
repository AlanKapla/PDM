# ProjectCost — Fix 01: Krytyczne (bezpieczeństwo danych i autoryzacja)

Cel: usunąć błędy mogące prowadzić do utraty danych, ujawniania soft-deleted, osierocenia rekordów i niespójności semantyki autoryzacji.

## Zakres zmian

### 1. K1 — Brak `SaveChangesAsync` po `InsertRange` w ShareProjectCosts

Plik: `src/CQRS/ProjectCosts/ShareProjectCosts/ShareProjectCostsCommandHandler.cs`

Po `await sharedProjectCostRepo.InsertRange(newShares, ct)` (lub równoważnym), **przed** wysłaniem powiadomień, dodać:
```csharp
await sharedProjectCostRepo.SaveChangesAsync(cancellationToken);
```
Powiadomienia mają być wysyłane dopiero po commit udostępnień.

### 2. K2 — Dodać brakujące walidatory

#### 2a. `DeleteProjectCostCommandValidator`
Plik: `src/CQRS/ProjectCosts/DeleteProjectCost/DeleteProjectCostCommandValidator.cs` (nowy)

```csharp
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.ProjectCosts.DeleteProjectCost;

public sealed class DeleteProjectCostCommandValidator : AbstractValidator<DeleteProjectCostCommand>
{
    public DeleteProjectCostCommandValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();
        RuleFor(x => x.CostId).RequiredId();
    }
}
```

#### 2b. `GetProjectCostsQueryValidator`
Plik: `src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryValidator.cs` (nowy)

```csharp
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.ProjectCosts.GetProjectCosts;

public sealed class GetProjectCostsQueryValidator : AbstractValidator<GetProjectCostsQuery>
{
    public GetProjectCostsQueryValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ProjectId).RequiredId();
        RuleFor(x => x.Scope).IsInEnum();
    }
}
```

### 3. K3 — Filtr `IsDeleted == false` w GetProjectCostsQueryHandler

Plik: `src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs`

W każdym predykacie `LoadCostsAsync` (All / Mine / Shared) dopisać warunek `&& !pc.IsDeleted` (dla scope Shared — analogicznie `&& !spc.ProjectCost.IsDeleted`).

### 4. K4 — Kolejność: upload PRZED Insert w CreateProjectCostCommandHandler

Plik: `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandHandler.cs`

Zmienić sekwencję: najpierw upload pliku (jeśli `request.Document is not null`), uzyskane metadane (np. nazwa blob, content-type) przypisać do `ProjectCost`, dopiero potem `Insert`. Dzięki temu fail uploadu rzuca wyjątek przed jakimkolwiek zapisem do DB i nie wymaga compensating delete. Komunikat błędu ma pozostać `ValidationApiException` z dotychczasową treścią lub bardziej precyzyjną.

### 5. W6 (decyzja domenowa) — NotFound → Forbidden przy braku uprawnień

We wszystkich handlerach domeny (`Update`, `Delete`, `UpdateCostShare`, `ShareProjectCosts`, ewentualnie `Get` jeżeli rzuca przy braku dostępu), w miejscach gdzie obecnie po sprawdzeniu uprawnień (admin/owner/share) rzucany jest `NotFoundApiException` z powodu BRAKU UPRAWNIEŃ (nie z powodu nieistniejącego rekordu) — zamienić na:
```csharp
throw new ForbiddenApiException();
```
Pozostawić `NotFoundApiException` tylko tam, gdzie rekord faktycznie nie istnieje w DB.

## Wymagania techniczne

- Zakaz `var` — explicit types we wszystkich nowych/zmodyfikowanych liniach.
- Walidatory: `public sealed class`.
- Po zmianach uruchomić build: `dotnet build src\WebApi\WebApi.csproj` w `02-ApplicationServices/ProductDataManagementWebAPI`.
- Zwrócić raport: status buildu (0/N błędów), lista zmodyfikowanych plików, blokery.
