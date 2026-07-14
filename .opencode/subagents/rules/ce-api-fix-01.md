# ce-api-fix-01 — Słownik jednostek (ProjectUnit)

## Cel
Dodać słownik jednostek miar per projekt. Jednostki są przechowywane jako osobne wiersze powiązane z projektem. Projekt przy tworzeniu dostaje predefiniowany zestaw popularnych jednostek. Admin projektu może edytować listę. Kosztorys korzysta z tego słownika (endpoint do pobrania + caching).

## Wymagania z dokumentacji
Punkt 6: "pole jednostka powinień uzywac słownika jednostek, słownik ten ma byc zwracany przez dodatkowy endpoint i cachowany na ui, jednostki maja być zapisane w parametrach projektu, projekt podczas tworzena ma miec utworzony słownik z najbardziej popularnymi jednostkami, jednostki te admin projektu bedzie mogł edytowac, podczas wyboru jednostki w kosztorysie, user moze wybrac jednostke z drop down select lub wpisac własną z palca"

## Przeczytaj skill przed implementacją
`.github/skills/api-entities/SKILL.md`
`.github/skills/api-cqrs/SKILL.md`
`.github/skills/api-controllers/SKILL.md`

---

## 1. Encja ProjectUnit

Plik: `src/Entities/Models/Projects/ProjectUnit.cs`

```csharp
namespace Entities.Models.Projects
{
    public class ProjectUnit : BaseEntity
    {
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = default!;
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual Project Project { get; set; } = default!;
    }
}
```

NIE dziedziczy po `ProjectParams` — ProjectParams używa TPH z unique index `(ProjectId, ParamType)` co uniemożliwia wiele wierszy per projekt per typ. `ProjectUnit` to samodzielna encja z własną tabelą.

## 2. Konfiguracja EF

Plik: `src/Entities/Configurations/Projects/ProjectUnitConfiguration.cs`

```csharp
using Entities.Models.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.Projects
{
    public class ProjectUnitConfiguration : IEntityTypeConfiguration<ProjectUnit>
    {
        public void Configure(EntityTypeBuilder<ProjectUnit> builder)
        {
            builder.ToTable("ProjectUnits");

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(x => x.ProjectId);

            builder.HasOne(x => x.Project)
                .WithMany(p => p.Units)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
```

## 3. Nawigacja w Project

Dodaj do `src/Entities/Models/Projects/Project.cs`:
```csharp
public virtual ICollection<ProjectUnit> Units { get; set; } = new List<ProjectUnit>();
```

## 4. Rejestracja konfiguracji

Sprawdź czy `ApplicationDbContext` używa `ApplyConfigurationsFromAssembly` — jeśli tak, konfiguracja zostanie automatycznie wykryta. Jeśli nie, dodaj ją ręcznie.

## 5. Migracja EF Core

Po dodaniu encji wygeneruj migrację:
```powershell
cd src/Entities
dotnet ef migrations add AddProjectUnits --startup-project ../WebApi
```

## 6. Seed w CreateProjectCommandHandler

W `src/CQRS/Projects/CreateProject/CreateProjectCommandHandler.cs`, po zapisaniu projektu i waluty (za `currencyRepo.SaveChangesAsync`), dodaj seed jednostek:

Wstrzyknij `IRepository<ProjectUnit>` do konstruktora.

```csharp
private static readonly string[] DefaultUnits = new[]
{
    "szt", "m", "m²", "m³", "kg", "mb", "godz", "kpl", "t", "km", "l", "opak", "r-g", "kpl"
};
```

Po zapisaniu projektu utwórz jednostki:
```csharp
int unitOrder = 1;
foreach (string unitName in DefaultUnits.Distinct())
{
    await projectUnitRepo.Insert(new ProjectUnit
    {
        ProjectId = project.Id,
        Name = unitName,
        Order = unitOrder++,
        CreatedAt = DateTime.UtcNow
    });
}
await projectUnitRepo.SaveChangesAsync(cancellationToken);
```

## 7. Web modele

Plik: `src/Business/Interfaces/WebModels/Projects/ProjectUnitWeb.cs`
```csharp
namespace Business.Interfaces.WebModels.Projects
{
    public sealed record ProjectUnitWeb
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required int Order { get; init; }
    }
}
```

Plik: `src/Business/Interfaces/WebModels/Projects/UpsertProjectUnitWeb.cs`
```csharp
namespace Business.Interfaces.WebModels.Projects
{
    public sealed record UpsertProjectUnitWeb
    {
        public required string Name { get; init; }
        public int Order { get; init; }
    }
}
```

## 8. CQRS

### GetProjectUnits

Query: `src/CQRS/Projects/GetProjectUnits/GetProjectUnitsQuery.cs`
```csharp
public sealed record GetProjectUnitsQuery : IRequest<List<ProjectUnitWeb>>
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
}
```

Handler: pobierz jednostki przez `IReadRepository<ProjectUnit>`, filtruj po `ProjectId`, sprawdź że projekt należy do tenanta (przez Project.TenantId == TenantId). Zwróć posortowane wg `Order`, zmapuj do `ProjectUnitWeb`.

Walidator: TenantId i ProjectId nie mogą być empty.

### AddProjectUnit

Command: `src/CQRS/Projects/AddProjectUnit/`
- Wstrzykuje `IRepository<ProjectUnit>`
- Sprawdza że projekt istnieje i należy do tenanta (NotFoundApiException jeśli nie)
- Oblicza `Order = maxOrder + 1`
- Tworzy i zapisuje jednostkę
- Zwraca `Guid` (nowe Id)

### UpdateProjectUnit

Command: `src/CQRS/Projects/UpdateProjectUnit/`
- Wstrzykuje `IRepository<ProjectUnit>`
- Pobiera jednostkę po Id z ProjectId i TenantId check
- Aktualizuje `Name` i/lub `Order`
- Zwraca `NoContent`

### DeleteProjectUnit

Command: `src/CQRS/Projects/DeleteProjectUnit/`
- Wstrzykuje `IRepository<ProjectUnit>`
- Pobiera jednostkę, usuwa (hard delete — nie potrzeba soft delete dla jednostek)
- Zwraca `NoContent`

### ReorderProjectUnits

Command: `src/CQRS/Projects/ReorderProjectUnits/`
- Przyjmuje `List<Guid> UnitIds` (nowa kolejność)
- Aktualizuje `Order` na każdej jednostce wg indeksu listy
- Zwraca `NoContent`

## 9. Kontroler

Dodaj do `src/WebApi/Controllers/ProjectController.cs` sekcję dla units:

```csharp
// GET api/tenants/{tenantId}/projects/{projectId}/units
[HttpGet("{projectId}/units")]
[Authorize(Policy = PermissionCodes.ProjectView)]
[ProducesResponseType(typeof(List<ProjectUnitWeb>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetProjectUnits(
    [FromRoute] Guid tenantId,
    [FromRoute] Guid projectId)

// POST api/tenants/{tenantId}/projects/{projectId}/units
[HttpPost("{projectId}/units")]
[Authorize(Policy = PermissionCodes.ProjectSettings)]
[ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
public async Task<IActionResult> AddProjectUnit(
    [FromRoute] Guid tenantId,
    [FromRoute] Guid projectId,
    [FromBody] UpsertProjectUnitWeb body)

// PUT api/tenants/{tenantId}/projects/{projectId}/units/{unitId}
[HttpPut("{projectId}/units/{unitId:guid}")]
[Authorize(Policy = PermissionCodes.ProjectSettings)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public async Task<IActionResult> UpdateProjectUnit(...)

// DELETE api/tenants/{tenantId}/projects/{projectId}/units/{unitId}
[HttpDelete("{projectId}/units/{unitId:guid}")]
[Authorize(Policy = PermissionCodes.ProjectSettings)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public async Task<IActionResult> DeleteProjectUnit(...)

// POST api/tenants/{tenantId}/projects/{projectId}/units/reorder
[HttpPost("{projectId}/units/reorder")]
[Authorize(Policy = PermissionCodes.ProjectSettings)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public async Task<IActionResult> ReorderProjectUnits(...)
```

## 10. Weryfikacja

- `dotnet build` bez błędów
- `dotnet ef migrations script` generuje poprawny SQL z tabelą `ProjectUnits`
- Seed: nowy projekt ma 14 jednostek
