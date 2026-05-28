# permissions-api-fix-04 — CQRS Commands/Handlers + WebModele + CurrentUser

## Zadanie

Zaktualizuj Commands, Handlers, WebModele i CurrentUser — usuń `ModuleAccessLevel` z logiki, uprość payload do listy modułów.

## Krok 1 — AddProjectMemberCommand.cs

Plik: `src/CQRS/Projects/AddProjectMember/AddProjectMemberCommand.cs`

Nowa zawartość:
```csharp
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Enums;
using MediatR;

namespace CQRS.Projects.AddProjectMember
{
    public sealed record AddProjectMemberCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid UserId { get; init; }
        public IReadOnlyList<ProjectModule> Modules { get; init; } = Array.Empty<ProjectModule>();

        public string PermissionCode => PermissionCodes.ProjectMembers;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
```

## Krok 2 — AddProjectMemberCommandHandler.cs

Plik: `src/CQRS/Projects/AddProjectMember/AddProjectMemberCommandHandler.cs`

Znajdź i zastąp fragment tworzący uprawnienia modułowe:

**Stare:**
```csharp
// Save granular module permissions (only non-None entries)
foreach (ModulePermissionInput mp in request.ModulePermissions.Where(p => p.AccessLevel != Entities.Enums.ModuleAccessLevel.None))
{
    await modulePermissionRepo.Insert(new ProjectMemberModulePermission
    {
        TenantId = request.TenantId,
        ProjectId = request.ProjectId,
        UserId = request.UserId,
        Module = mp.Module,
        AccessLevel = mp.AccessLevel
    });
}
```

**Nowe:**
```csharp
foreach (ProjectModule module in request.Modules)
{
    await modulePermissionRepo.Insert(new ProjectMemberModulePermission
    {
        TenantId = request.TenantId,
        ProjectId = request.ProjectId,
        UserId = request.UserId,
        Module = module
    });
}
```

Usuń niepotrzebne usingi: `using CQRS.Projects.AddProjectMember;` (jeśli `ModulePermissionInput` był importowany), usuń using `Entities.Enums.ModuleAccessLevel` jeśli nie jest już używany.

Dodaj wywołanie `BumpVersionAsync` po `InvalidateProjectMembersCacheAsync` (dla spójności z UpdateHandler):

**Znajdź:**
```csharp
await userService.InvalidateProjectMembersCacheAsync(request.TenantId, request.ProjectId, cancellationToken);
```

**Zastąp** (dodaj BumpVersion dla nowego użytkownika):
```csharp
await userService.InvalidateProjectMembersCacheAsync(request.TenantId, request.ProjectId, cancellationToken);
await permissionsVersionService.BumpVersionAsync(request.UserId, cancellationToken);
```

**Uwaga:** Dodaj `IPermissionsVersionService permissionsVersionService` do konstruktora i pola klasy (analogicznie jak w `UpdateProjectMemberRoleCommandHandler`).

## Krok 3 — UpdateProjectMemberRoleCommand.cs

Plik: `src/CQRS/Projects/UpdateProjectMemberRole/UpdateProjectMemberRoleCommand.cs`

Nowa zawartość:
```csharp
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Enums;
using MediatR;

namespace CQRS.Projects.UpdateProjectMemberRole
{
    public sealed record UpdateProjectMemberRoleCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid UserId { get; init; }
        public required bool IsAdmin { get; init; }
        public IReadOnlyList<ProjectModule> Modules { get; init; } = Array.Empty<ProjectModule>();

        public string PermissionCode => PermissionCodes.ProjectMembers;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
```

## Krok 4 — UpdateProjectMemberRoleCommandHandler.cs

Plik: `src/CQRS/Projects/UpdateProjectMemberRole/UpdateProjectMemberRoleCommandHandler.cs`

Znajdź i zastąp fragment usuwający i dodający uprawnienia modułowe:

**Stare:**
```csharp
foreach (ModulePermissionInput mp in request.ModulePermissions.Where(p => p.AccessLevel != ModuleAccessLevel.None))
{
    await modulePermissionRepo.Insert(new ProjectMemberModulePermission
    {
        TenantId = request.TenantId,
        ProjectId = request.ProjectId,
        UserId = request.UserId,
        Module = mp.Module,
        AccessLevel = mp.AccessLevel
    });
}
```

**Nowe:**
```csharp
foreach (ProjectModule module in request.Modules)
{
    await modulePermissionRepo.Insert(new ProjectMemberModulePermission
    {
        TenantId = request.TenantId,
        ProjectId = request.ProjectId,
        UserId = request.UserId,
        Module = module
    });
}
```

Usuń niepotrzebne usingi: `ModuleAccessLevel`, `ModulePermissionInput`.

## Krok 5 — WebModel ProjectMemberWeb.cs

Plik: `src/Business/Interfaces/WebModels/Projects/ProjectMemberWeb.cs`

Nowa zawartość:
```csharp
namespace Business.Interfaces.WebModels.Projects
{
    public sealed record ProjectMemberWeb
    {
        public required Guid UserId { get; init; }
        public required string Email { get; init; }
        public required string FirstName { get; init; }
        public required string LastName { get; init; }
        public required DateTime JoinedAt { get; init; }
        public required bool IsAdmin { get; init; }
        public IReadOnlyList<int> Modules { get; init; } = Array.Empty<int>();
    }
}
```

(Usuń cały record `ModulePermissionWeb` — nie jest już potrzebny.)

## Krok 6 — GetProjectMembersQueryHandler.cs

Plik: `src/CQRS/Projects/GetProjectMembers/GetProjectMembersQueryHandler.cs`

Zaktualizuj mapowanie w metodzie `Handle` — zastąp stare mapowanie `ModulePermissions`:

**Stare:**
```csharp
ModulePermissions = entity?.ModulePermissions
    .Select(mp => new ModulePermissionWeb
    {
        Module = (int)mp.Module,
        AccessLevel = (int)mp.AccessLevel
    })
    .ToArray() ?? Array.Empty<ModulePermissionWeb>()
```

**Nowe:**
```csharp
Modules = entity?.ModulePermissions
    .Select(mp => (int)mp.Module)
    .ToArray() ?? Array.Empty<int>()
```

Usuń using `Business.Interfaces.WebModels.Projects` jeśli `ModulePermissionWeb` nie jest już potrzebny (ale `ProjectMemberWeb` nadal jest).

## Krok 7 — Weryfikacja

```powershell
dotnet build src/CQRS/CQRS.csproj 2>&1 | Select-Object -Last 8
dotnet build src/WebApi/WebApi.csproj 2>&1 | Select-Object -Last 8
```

Oczekiwany rezultat: Build succeeded (0 errors).
