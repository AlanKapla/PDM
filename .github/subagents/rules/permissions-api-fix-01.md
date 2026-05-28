# permissions-api-fix-01 — Encja + migracja EF Core

## Zadanie

Uprość model uprawnień modułowych: usuń kolumnę `AccessLevel` z encji `ProjectMemberModulePermission` oraz stwórz migrację EF Core czyszczącą dane i usuwającą kolumnę.

## Krok 1 — Modyfikacja encji

Plik: `src/Entities/Models/Projects/ProjectMemberModulePermission.cs`

Obecna zawartość:
```csharp
using Entities.Enums;

namespace Entities.Models.Projects;

public class ProjectMemberModulePermission
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ProjectModule Module { get; set; }
    public ModuleAccessLevel AccessLevel { get; set; }

    public ProjectMember ProjectMember { get; set; } = default!;
}
```

Nowa zawartość (usuń `AccessLevel` i using `Entities.Enums` jeśli `ModuleAccessLevel` będzie usunięty — ale `ProjectModule` nadal jest w `Entities.Enums`, więc zostaw using):
```csharp
using Entities.Enums;

namespace Entities.Models.Projects;

public class ProjectMemberModulePermission
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ProjectModule Module { get; set; }

    public ProjectMember ProjectMember { get; set; } = default!;
}
```

## Krok 2 — Usuń enum ModuleAccessLevel

Usuń plik: `src/Entities/Enums/ModuleAccessLevel.cs`

Przed usunięciem upewnij się, że nie ma innych plików w `src/` które importują ten typ (poza tymi modyfikowanymi w kolejnych krokach).

## Krok 3 — Stwórz migrację EF Core

W katalogu projektu WebAPI (`02-ApplicationServices/ProductDataManagementWebAPI`) uruchom:

```powershell
cd src/Entities
dotnet ef migrations add migration-simplify-module-permissions --context AppDbContext --startup-project ../WebApi/
```

Następnie **ręcznie edytuj** wygenerowany plik migracji (w `src/Entities/Migrations/`) aby:

1. W metodzie `Up()` — PRZED usunięciem kolumny dodaj truncate/delete danych:
```csharp
migrationBuilder.Sql("DELETE FROM \"ProjectMemberModulePermissions\";");
migrationBuilder.DropColumn(
    name: "AccessLevel",
    table: "ProjectMemberModulePermissions");
```

2. W metodzie `Down()` — przywróć kolumnę:
```csharp
migrationBuilder.AddColumn<int>(
    name: "AccessLevel",
    table: "ProjectMemberModulePermissions",
    type: "integer",
    nullable: false,
    defaultValue: 0);
```

## Krok 4 — Weryfikacja

Uruchom:
```powershell
dotnet build src/Entities/Entities.csproj
```

Oczekiwany rezultat: Build succeeded (0 errors). Jeśli są błędy kompilacji związane z `ModuleAccessLevel` — to normalne, będą naprawiane w kolejnych krokach.

## Uwaga

Nie uruchamiaj `dotnet ef database update` — migracja zostanie zastosowana przy deploymencie.
