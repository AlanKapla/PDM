# API Fix 02 — Usunięcie mechanizmu sharingu (CQRS + Serwisy)

## Cel
Usunąć wszystkie pliki związane z mechanizmem udostępniania kosztów:
- CQRS: `ShareProjectCosts/`, `UpdateCostShare/`
- Serwisy: `IProjectCostShareNotificationService`, `ProjectCostShareNotificationService`
- Uproszczenie `IProjectCostAccessService` — usunięcie `HasShareAccessAsync`

Przeczytaj skill `.github/skills/api/skill-api-cqrs.md` i `.github/skills/api/skill-api-services.md`.

---

## Krok 1 — Usuń foldery CQRS sharingu

Usuń kompletne foldery (wszystkie pliki wewnątrz):

1. `src/CQRS/ProjectCosts/ShareProjectCosts/`
   - `ShareProjectCostsCommand.cs`
   - `ShareProjectCostsCommandHandler.cs`
   - `ShareProjectCostsCommandValidator.cs`

2. `src/CQRS/ProjectCosts/UpdateCostShare/`
   - `UpdateCostShareCommand.cs`
   - `UpdateCostShareCommandHandler.cs`
   - `UpdateCostShareCommandValidator.cs`

---

## Krok 2 — Usuń serwisy notyfikacji sharingu

Usuń pliki:
1. `src/Business/Interfaces/Services/IProjectCostShareNotificationService.cs`
2. `src/Business/Implementation/Services/ProjectCostShareNotificationService.cs`

---

## Krok 3 — Uproszcz `IProjectCostAccessService`

Plik: `src/Business/Interfaces/Services/IProjectCostAccessService.cs`

Zastąp całą zawartość:

```csharp
using Entities.Models.Costs;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Resolves write access checks for <see cref="ProjectCost"/> resources.
    /// </summary>
    public interface IProjectCostAccessService
    {
        /// <summary>
        /// True when the current user is tenant/project admin or owner of the cost
        /// (full edit and delete).
        /// </summary>
        Task<bool> HasWriteAccessAsync(
            ProjectCost cost,
            Guid currentUserId,
            CancellationToken cancellationToken);
    }
}
```

---

## Krok 4 — Uproszcz `ProjectCostAccessService`

Plik: `src/Business/Implementation/Services/ProjectCostAccessService.cs`

Zastąp całą zawartość:

```csharp
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Costs;

namespace Business.Implementation.Services
{
    public sealed class ProjectCostAccessService : IProjectCostAccessService
    {
        private readonly ICurrentUser currentUser;

        public ProjectCostAccessService(ICurrentUser currentUser)
        {
            this.currentUser = currentUser;
        }

        public async Task<bool> HasWriteAccessAsync(
            ProjectCost cost,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            if (cost.UserId == currentUserId)
            {
                return true;
            }

            return await currentUser.IsTenantOrProjectAdminAsync(
                cost.TenantId, cost.ProjectId, cancellationToken);
        }
    }
}
```

---

## Krok 5 — Sprawdź rejestrację DI

Plik: `src/WebApi/DependencyInjection/` lub `src/WebApi/Program.cs` lub `src/Business/BusinessDependencyInjection.cs`

Znajdź rejestrację `IProjectCostShareNotificationService` i `ProjectCostShareNotificationService` — usuń je.

---

## Weryfikacja
```
dotnet build src/Business/Business.csproj
dotnet build src/CQRS/CQRS.csproj
```

Oczekiwany wynik: błędy kompilacji tylko w CQRS (UpdateProjectCostCommandHandler nadal używa SharedWith) — naprawiane w fix-03.
