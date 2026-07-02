# API Fix 01 — Encje, EF Core, migracja, uprawnienia modułowe

## Cel
Fundament domeny dokumentacji technicznej: encje DB, enum statusu, rozszerzenie `ProjectModule` i pełna integracja z systemem uprawnień (`PROJECT.TECHNICAL_DOCUMENTATION`).

## Decyzje MVP (obowiązkowe)
- Jeden kod uprawnienia: `PROJECT.TECHNICAL_DOCUMENTATION`
- `ProjectModule.TechnicalDocumentation = 7`
- **Brak** `SchemaVersion` na encji
- **Brak** endpointu DELETE w MVP — encja **nie** dziedziczy po `DeletableEntity`

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Skills
Przeczytaj przed implementacją:
- `.cursor/skills/api-entities/SKILL.md`
- `.cursor/skills/api-repositories/SKILL.md`

## Pliki referencyjne
- `src/Entities/Models/Costs/BaseCostAttachment.cs` — wzorzec osobnej encji pliku + `BlobName`
- `src/Entities/Enums/ProjectModule.cs`
- `src/Business/Interfaces/Constants/PermissionCodes.cs`
- `src/Business/Interfaces/Constants/ModulePermissionTranslator.cs`
- `src/Business/Interfaces/Constants/PermissionScopes.cs`
- `src/Business/Interfaces/Constants/SuperAdminFallbackPermissions.cs`

---

## 1. Nowy enum `TechnicalDocumentationStatus`

Plik: `src/Entities/Enums/TechnicalDocumentationStatus.cs`

```csharp
namespace Entities.Enums;

public enum TechnicalDocumentationStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}
```

## 2. Encja `ProjectTechnicalDocumentation`

Plik: `src/Entities/Models/TechnicalDocumentation/ProjectTechnicalDocumentation.cs`

```csharp
namespace Entities.Models.TechnicalDocumentation;

public class ProjectTechnicalDocumentation : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public TechnicalDocumentationStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DetailsJson { get; set; }
    public int AutoRetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public virtual Project Project { get; set; } = default!;
    public virtual ICollection<ProjectTechnicalDocumentationFile> Files { get; set; } =
        new List<ProjectTechnicalDocumentationFile>();
}
```

## 3. Encja `ProjectTechnicalDocumentationFile`

Plik: `src/Entities/Models/TechnicalDocumentation/ProjectTechnicalDocumentationFile.cs`

```csharp
namespace Entities.Models.TechnicalDocumentation;

public class ProjectTechnicalDocumentationFile : BaseEntity
{
    public Guid TechnicalDocumentationId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public string OriginalFileName { get; set; } = default!;
    public string BlobName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual ProjectTechnicalDocumentation TechnicalDocumentation { get; set; } = default!;
}
```

## 4. Konfiguracje EF

### `ProjectTechnicalDocumentationConfiguration.cs`
Lokalizacja: `src/Entities/Configurations/TechnicalDocumentation/`

- `Name`: required, `HasMaxLength(200)`
- `Description`: optional, `HasMaxLength(2000)`
- `DetailsJson`: `HasColumnType("nvarchar(max)")`
- `Status`: required
- Indeks: `(TenantId, ProjectId)`
- FK `ProjectId` → `Projects`, `OnDelete(DeleteBehavior.Restrict)`
- FK `CreatedByUserId` → `Users`, `OnDelete(DeleteBehavior.Restrict)`

### `ProjectTechnicalDocumentationFileConfiguration.cs`

- FK `TechnicalDocumentationId` → `ProjectTechnicalDocumentation`, `OnDelete(DeleteBehavior.Cascade)`
- Indeks: `(TenantId, ProjectId, TechnicalDocumentationId)`

## 5. `DbSet` w `AppDbContext`

Dodaj:
```csharp
public DbSet<ProjectTechnicalDocumentation> ProjectTechnicalDocumentations { get; set; }
public DbSet<ProjectTechnicalDocumentationFile> ProjectTechnicalDocumentationFiles { get; set; }
```

## 6. Rozszerzenie `ProjectModule`

W `src/Entities/Enums/ProjectModule.cs` dodaj:
```csharp
TechnicalDocumentation = 7
```

## 7. Uprawnienia — 4 pliki

### `PermissionCodes.cs`
```csharp
public const string ProjectTechnicalDocumentation = "PROJECT.TECHNICAL_DOCUMENTATION";
```
Dodaj do tablicy `All`.

### `ModulePermissionTranslator.cs`
```csharp
ProjectModule.TechnicalDocumentation => new HashSet<string> { PermissionCodes.ProjectTechnicalDocumentation },
```

### `PermissionScopes.cs`
```csharp
[PermissionCodes.ProjectTechnicalDocumentation] = PermissionScope.Project,
```

### `SuperAdminFallbackPermissions.cs`
Dodaj `PermissionCodes.ProjectTechnicalDocumentation` do listy `ProjectReadOnly` (lub odpowiedniej listy fallback — sprawdź wzorzec innych modułów).

## 8. Rejestracja repozytoriów

W `src/WebApi/Extensions/ServiceCollectionExtensions.cs` dodaj:
```csharp
.AddReadRepository<ProjectTechnicalDocumentation>()
.AddWriteRepository<ProjectTechnicalDocumentation>()
.AddReadRepository<ProjectTechnicalDocumentationFile>()
.AddWriteRepository<ProjectTechnicalDocumentationFile>()
```

## 9. Migracja EF Core

```powershell
cd src/Entities
dotnet ef migrations add add-technical-documentation --startup-project ../WebApi
```

Sprawdź migrację: tabele `ProjectTechnicalDocumentations`, `ProjectTechnicalDocumentationFiles`, indeksy, FK.

## Weryfikacja
```powershell
dotnet build --configuration Release
```
Build solution musi przejść bez błędów.

## Zależności
- Brak — to pierwszy prompt API.
- Kolejne prompty (fix-02+) wymagają ukończenia tego kroku.
