# tenant-simplify-api-fix-01 — Encja TenantMember: IsAdmin zamiast RoleId

## Cel
Zastąp `RoleId` (FK do tabeli Roles) polem `IsAdmin: bool` w encji `TenantMember`.
Usuń nawigację `MemberRole` oraz zależność od encji `Role` w kontekście tenanta.

## Skill
Przeczytaj `.opencode/skills/api/skill-api-entities.md` przed implementacją.

## Pliki do modyfikacji

### 1. `src/Entities/Models/Tenants/TenantMember.cs`

**Obecna zawartość:**
```csharp
using Entities.Enums;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Users;

namespace Entities.Models.Tenants
{
    public class TenantMember
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public Guid? RoleId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Tenant Tenant { get; set; } = default!;
        public User User { get; set; } = default!;
        public Role? MemberRole { get; set; }
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    }
}
```

**Nowa zawartość:**
```csharp
using Entities.Models.Projects;
using Entities.Models.Users;

namespace Entities.Models.Tenants
{
    public class TenantMember
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Tenant Tenant { get; set; } = default!;
        public User User { get; set; } = default!;
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    }
}
```

### 2. Konfiguracja EF Core

Sprawdź `src/Entities/Context/AppDbContext.cs`.
Jeśli istnieje konfiguracja encji `TenantMember` inline lub w osobnym pliku konfiguracyjnym (szukaj `HasOne(m => m.MemberRole)` lub `HasForeignKey(m => m.RoleId)`), **usuń** tę konfigurację FK.

### 3. Migracja EF Core

Uruchom w katalogu `src/Entities`:
```
dotnet ef migrations add TenantMember_ReplaceRoleIdWithIsAdmin --project ../Entities -- --environment Migration
```

**Jeśli migracja się nie uruchamia**, dodaj ją ręcznie jako nowy plik w `src/Entities/Migrations/` z operacjami:
- `DropForeignKey("TenantMembers", "FK_TenantMembers_Roles_RoleId")`
- `DropIndex` na kolumnie `RoleId` (jeśli istnieje)
- `DropColumn("RoleId", "TenantMembers")`
- `AddColumn<bool>("IsAdmin", "TenantMembers", nullable: false, defaultValue: false)`

## Build check
```
dotnet build src/Entities/Entities.csproj
```

**WAŻNE:** Po tej zmianie inne projekty (CQRS, Business) będą mieć błędy kompilacji bo odwołują się do `RoleId` i `MemberRole` na `TenantMember`. To jest oczekiwane — zostaną naprawione w kolejnych promptach (fix-02 do fix-06). Sprawdź tylko że projekt `Entities` buduje się poprawnie.
