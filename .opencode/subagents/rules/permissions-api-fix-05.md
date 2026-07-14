# permissions-api-fix-05 — Testy jednostkowe

## Zadanie

Zaktualizuj testy CQRS — napraw brakujący mock, zaktualizuj testy AddProjectMember i UpdateProjectMemberRole pod nowy model (lista modułów bez AccessLevel).

## Krok 1 — Zlokalizuj pliki testowe

```powershell
Get-ChildItem -Recurse tests/ -Filter "*ProjectMember*" | Select-Object FullName
Get-ChildItem -Recurse tests/ -Filter "*UpdateProjectMemberRole*" | Select-Object FullName
```

## Krok 2 — UpdateProjectMemberRoleCommandHandlerTests.cs

Plik: `tests/CQRS.Tests/Projects/UpdateProjectMemberRoleCommandHandlerTests.cs`

**Problem:** Brakuje `Mock<IRepository<ProjectMemberModulePermission>>` oraz `Mock<IPermissionsVersionService>` w setupie.

Przejrzyj plik i upewnij się że:

1. Konstruktor handlera otrzymuje wszystkie zależności w prawidłowej kolejności:
```csharp
private readonly Mock<IReadRepository<Project>> _projectRepoMock = new();
private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
private readonly Mock<IRepository<ProjectMemberModulePermission>> _modulePermissionRepoMock = new();
private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
private readonly Mock<IPermissionsVersionService> _permissionsVersionServiceMock = new();
private readonly Mock<INotificationSender> _notificationSenderMock = new();
private readonly Mock<ICurrentUser> _currentUserMock = new();
private readonly Mock<IUserService> _userServiceMock = new();
```

2. Konstruktor handlera w teście:
```csharp
var handler = new UpdateProjectMemberRoleCommandHandler(
    _projectRepoMock.Object,
    _projectMemberRepoMock.Object,
    _modulePermissionRepoMock.Object,
    _notificationRepoMock.Object,
    _permissionsVersionServiceMock.Object,
    _notificationSenderMock.Object,
    _currentUserMock.Object,
    _userServiceMock.Object);
```

## Krok 3 — Zaktualizuj testy pod nowy model

Znajdź w obu plikach testowych (AddProjectMember + UpdateProjectMemberRole) wszystkie miejsca gdzie:
- Tworzony jest `ModulePermissionInput` → zastąp na `ProjectModule` (enum value)
- Właściwość `ModulePermissions` w command → zastąp na `Modules`
- Assercja na `AccessLevel` → usuń (nie istnieje w nowym modelu)

### Przykład — stare:
```csharp
var command = new AddProjectMemberCommand
{
    TenantId = tenantId,
    ProjectId = projectId,
    UserId = userId,
    ModulePermissions = new List<ModulePermissionInput>
    {
        new() { Module = ProjectModule.Files, AccessLevel = ModuleAccessLevel.Write },
        new() { Module = ProjectModule.Estimates, AccessLevel = ModuleAccessLevel.Admin }
    }
};
```

### Przykład — nowe:
```csharp
var command = new AddProjectMemberCommand
{
    TenantId = tenantId,
    ProjectId = projectId,
    UserId = userId,
    Modules = new List<ProjectModule>
    {
        ProjectModule.Files,
        ProjectModule.Estimates
    }
};
```

### Przykład — stara assercja:
```csharp
_modulePermissionRepoMock.Verify(r => r.Insert(
    It.Is<ProjectMemberModulePermission>(p =>
        p.Module == ProjectModule.Files &&
        p.AccessLevel == ModuleAccessLevel.Write)),
    Times.Once);
```

### Nowa assercja:
```csharp
_modulePermissionRepoMock.Verify(r => r.Insert(
    It.Is<ProjectMemberModulePermission>(p =>
        p.Module == ProjectModule.Files)),
    Times.Once);
```

## Krok 4 — Usuń referencje do ModuleAccessLevel w testach

Usuń wszelkie `using Entities.Enums;` jeśli `ModuleAccessLevel` jest jedyną używaną rzeczą z tego namespace (ale `ProjectModule` też jest w `Entities.Enums` — więc using zostaje).

Usuń wszelkie `ModulePermissionInput` — w testach zastąpione przez `ProjectModule`.

## Krok 5 — Weryfikacja

```powershell
dotnet build tests/CQRS.Tests/CQRS.Tests.csproj 2>&1 | Select-Object -Last 8
dotnet test tests/CQRS.Tests/CQRS.Tests.csproj --filter "ProjectMember" 2>&1 | Select-Object -Last 20
```

Oczekiwany rezultat: Build succeeded, testy przechodzą.
