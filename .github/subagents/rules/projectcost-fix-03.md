# ProjectCost — Fix 03: Walidatory — CommonValidationExtensions, DRY

Cel: ujednolicić walidatory domeny do wzorca z `CommonValidationExtensions`, wydzielić wspólne reguły, dodać `sealed`.

Wymaga wcześniejszego ukończenia fix-01 (nowe walidatory dla Delete/Get) oraz fix-02 (sealed records).

## Zakres zmian

### 1. W3 — Użycie `CommonValidationExtensions` we wszystkich walidatorach domeny

Pliki:
- `CreateProjectCostCommandValidator.cs`
- `UpdateProjectCostCommandValidator.cs`
- `ShareProjectCostsCommandValidator.cs`
- `UpdateCostShareCommandValidator.cs`
- (świeżo dodane) `DeleteProjectCostCommandValidator.cs`, `GetProjectCostsQueryValidator.cs`

Zasady:
- `RuleFor(x => x.TenantId).RequiredId();`
- `RuleFor(x => x.ProjectId).RequiredId();`
- `RuleFor(x => x.CostId).RequiredId();` — gdzie pole istnieje
- `RuleFor(x => x.SharedWithUserIds).UniqueIds();` — w Share i UpdateCostShare zamiast ręcznego `Distinct().Count() == Count`
- `RuleFor(x => x.ProjectCostIds).UniqueIds();` — w Share
- `RuleFor(x => x.SharedWithUserIds).NotCurrentUser(currentUser);` — w Share i UpdateCostShare zamiast inline `Contains(currentUser.Id)` (jeżeli rozszerzenie istnieje pod tą sygnaturą; jeśli wymaga wstrzyknięcia `ICurrentUser` — wstrzyknąć przez konstruktor walidatora)

### 2. W4 — Wspólny helper "users are project members"

W obu walidatorach (`Share`, `UpdateCostShare`) powtarza się sprawdzanie czy każdy `SharedWithUserId` jest członkiem projektu (DB hit przez `IRepository<ProjectMember>.GetBySearch`).

Wydzielić do osobnego pliku w `src/CQRS/ProjectCosts/Shared/ProjectCostValidationExtensions.cs`:

```csharp
internal static class ProjectCostValidationExtensions
{
    public static IRuleBuilderOptions<T, IEnumerable<Guid>> AllAreProjectMembers<T>(
        this IRuleBuilder<T, IEnumerable<Guid>> rule,
        IRepository<ProjectMember> projectMemberRepository,
        Func<T, Guid> tenantIdSelector,
        Func<T, Guid> projectIdSelector)
    {
        // MustAsync — sprawdza że wszystkie userIds są w ProjectMember dla (TenantId, ProjectId)
    }
}
```

Użyć w obu walidatorach. Walidator wstrzykuje `IRepository<ProjectMember>` przez konstruktor.

### 3. W12 — Wspólne reguły Net/Gross/Document/Date/Name dla Create + Update

Wydzielić do tego samego `ProjectCostValidationExtensions` metody:
- `ApplyCostNameRules<T>(this AbstractValidator<T> v, Expression<Func<T, string>> selector)` — NotEmpty + MaxLength(200)
- `ApplyCostFinancialRules<T>(...)` — Net/Gross > 0 + (Net or Gross required)
- `ApplyCostDateRules<T>(...)` — Date <= today + 1
- `ApplyDocumentRules<T>(...)` — typ + rozmiar (zachować istniejący `OverridePropertyName("Amount")` dla finansów)

W Create/Update validator wywołać te metody zamiast inline.

### 4. N2 — `sealed` dla wszystkich walidatorów

```csharp
public sealed class CreateProjectCostCommandValidator : AbstractValidator<CreateProjectCostCommand>
```

### 5. Cleanup usingów (N4)

Usunąć nieużywane `using` z walidatorów (Chats, Files, Notifications, Roles, Tenants, Users, WorkSchedules itp.).

## Wymagania techniczne

- Zakaz `var`.
- Build: `dotnet build src\WebApi\WebApi.csproj` w `02-ApplicationServices/ProductDataManagementWebAPI`.
- Jeśli sygnatura `NotCurrentUser` w `CommonValidationExtensions` wymaga innej formy — dostosować i odnotować w raporcie.
- Zwrócić raport: status buildu, lista plików, blokery.
