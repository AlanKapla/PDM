# API Fix 04 — CreateDirectory: nowy endpoint (katalog bez plików)

## Cel
Stworzenie nowego endpointu `POST /file/directories` który tworzy katalog bez uploadu plików. Nowy command, handler, validator i endpoint w kontrolerze.

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Skill
Przeczytaj: `.github/skills/api/skill-api-cqrs.md`
Przeczytaj: `.github/skills/api/skill-api-validators.md`
Przeczytaj: `.github/skills/api/skill-api-controllers.md`

## Nowe pliki do stworzenia

### 1. `src/CQRS/Files/CreateDirectory/CreateDirectoryCommand.cs`

```csharp
using CQRS.Files._Shared;
using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.Files.CreateDirectory
{
    public sealed record CreateDirectoryCommand : ProjectScopedFilesRequestBase, IRequest<Unit>
    {
        public required string DirectoryName { get; init; }
        public Guid? ParentId { get; init; }
        public override string PermissionCode => PermissionCodes.ProjectFiles;
    }
}
```

Sprawdź jaki interfejs używają inne commands w tym projekcie — może `IRequestCommand<Unit>` zamiast `IRequest<Unit>`.

### 2. `src/CQRS/Files/CreateDirectory/CreateDirectoryCommandHandler.cs`

Handler:
- Pobiera `IRepository<ProjectFilePackage>`
- Tworzy `ProjectFilePackage` z `Name = request.DirectoryName`, `ParentId = request.ParentId`, `OwnerId = currentUser.Id`, `CreatedByUserId = currentUser.Id`, `TenantId`, `ProjectId`, `CreatedAt = DateTime.UtcNow`
- Zapisuje i zwraca `Unit.Value`

Nie robi nic z plikami ani blob storage.

### 3. `src/CQRS/Files/CreateDirectory/CreateDirectoryCommandValidator.cs`

Walidacje:
- `DirectoryName`: NotEmpty, MaxLength(200)
- `DirectoryName`: sprawdzenie unikalności per `(TenantId, ProjectId, OwnerId, ParentId)` — analogicznie do fix-03
- `ParentId` (jeśli podany): musi istnieć w tej samej `(TenantId, ProjectId)`

## Plik do modyfikacji

### 4. `src/WebApi/Controllers/FileController.cs`

Dodać nowy endpoint po istniejących package endpoints:

```csharp
/// <summary>
/// Creates a new empty directory
/// </summary>
[HttpPost("directories")]
public async Task<IActionResult> CreateDirectory([FromBody] CreateDirectoryCommand command)
{
    await Send(command with { TenantId = TenantId, ProjectId = ProjectId });
    return NoContent();
}
```

Sprawdź jak inne endpointy w `FileController.cs` przekazują `TenantId` i `ProjectId` (z route parametrów) — użyj tego samego wzorca.

## Weryfikacja
```
dotnet build src/CQRS/CQRS.csproj
dotnet build src/WebApi/WebApi.csproj
```
Oba buildy muszą przejść bez błędów.
