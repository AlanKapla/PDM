# API Fix 01 — Enum CostApprovalStatus + Encja ProjectCost + Web model + EF Config

## Cel
Zastąpić bool `IsAccepted` nowym enumem `CostApprovalStatus` w encji `ProjectCost`.
Zachować `AcceptedByUserId` i `AcceptedAt` (zmiana nazwy na `ApprovedByUserId` / `ApprovedAt`).
Usunąć nawigację `SharedWith` z `ProjectCost`.
Zaktualizować web model `ProjectCostListItemWeb`.
Zaktualizować `ProjectCostConfiguration`.

Przeczytaj skill `.opencode/skills/api/skill-api-entities.md` przed implementacją.

---

## Krok 1 — Nowy enum `CostApprovalStatus`

Utwórz plik:
`02-ApplicationServices/ProductDataManagementWebAPI/src/Entities/Models/Costs/CostApprovalStatus.cs`

```csharp
namespace Entities.Models.Costs
{
    public enum CostApprovalStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Approved = 2
    }
}
```

---

## Krok 2 — Encja `ProjectCost`

Plik: `src/Entities/Models/Costs/ProjectCost.cs`

Zastąp:
```csharp
/// <summary>
/// Czy koszt został zaakceptowany (włączony do trackera)
/// </summary>
public bool IsAccepted { get; set; } = false;

/// <summary>
/// ID użytkownika, który zaakceptował koszt
/// </summary>
public Guid? AcceptedByUserId { get; set; }

/// <summary>
/// Data akceptacji kosztu
/// </summary>
public DateTime? AcceptedAt { get; set; }

// Navigation
public virtual ProjectMember ProjectMember { get; set; } = default!;
public virtual ICollection<SharedProjectCost> SharedWith { get; set; } = new List<SharedProjectCost>();
```

Na:
```csharp
/// <summary>
/// Status akceptacji kosztu
/// </summary>
public CostApprovalStatus ApprovalStatus { get; set; } = CostApprovalStatus.Draft;

/// <summary>
/// ID użytkownika, który zaakceptował koszt
/// </summary>
public Guid? ApprovedByUserId { get; set; }

/// <summary>
/// Data akceptacji kosztu
/// </summary>
public DateTime? ApprovedAt { get; set; }

// Navigation
public virtual ProjectMember ProjectMember { get; set; } = default!;
```

---

## Krok 3 — Encja `SharedProjectCost` — USUŃ PLIK

Usuń plik: `src/Entities/Models/Costs/SharedProjectCost.cs`

---

## Krok 4 — EF Core konfiguracja `ProjectCostConfiguration`

Plik: `src/Entities/Configurations/ProjectCostConfiguration.cs`

Zastąp całą zawartość:

```csharp
using Entities.Models.Costs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class ProjectCostConfiguration : IEntityTypeConfiguration<ProjectCost>
    {
        public void Configure(EntityTypeBuilder<ProjectCost> builder)
        {
            builder.Property(pc => pc.UserId).IsRequired();

            builder.Property(pc => pc.ApprovalStatus)
                .IsRequired()
                .HasDefaultValue(CostApprovalStatus.Draft)
                .HasConversion<string>();

            builder.Property(pc => pc.ApprovedByUserId);
            builder.Property(pc => pc.ApprovedAt);

            builder.HasOne(pc => pc.ProjectMember)
                .WithMany()
                .HasForeignKey(pc => new { pc.TenantId, pc.ProjectId, pc.UserId })
                .HasPrincipalKey(pm => new { pm.TenantId, pm.ProjectId, pm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(pc => new { pc.TenantId, pc.ProjectId, pc.ApprovalStatus });
            builder.HasIndex(pc => pc.Date);
        }
    }
}
```

---

## Krok 5 — Usuń konfigurację `SharedProjectCostConfiguration`

Usuń plik: `src/Entities/Configurations/SharedProjectCostConfiguration.cs`

---

## Krok 6 — `AppDbContext` — usuń `DbSet<SharedProjectCost>`

Plik: `src/Entities/Context/AppDbContext.cs`

Znajdź i usuń linię:
```csharp
public DbSet<SharedProjectCost> SharedProjectCosts { get; set; }
```

Usuń też ewentualny `using` dla `SharedProjectCost` jeśli jest zbędny.

---

## Krok 7 — Web model `ProjectCostListItemWeb`

Plik: `src/Business/Interfaces/WebModels/ProjectCosts/ProjectCostListItemWeb.cs`

Zastąp całą zawartość:

```csharp
using Entities.Models.Costs;

namespace Business.Interfaces.WebModels.ProjectCosts
{
    /// <summary>
    /// Model kosztu projektu (lista oraz odpowiedź Create/Update)
    /// </summary>
    public sealed record ProjectCostListItemWeb
    {
        public required Guid Id { get; init; }
        public required Guid UserId { get; init; }
        public required string UserName { get; init; }
        public required string Name { get; init; }
        public Guid? ContractorId { get; init; }
        public string? ContractorName { get; init; }
        public string? Number { get; init; }
        public DateTime? Date { get; init; }
        public string? Description { get; init; }
        public decimal? Net { get; init; }
        public decimal? Gross { get; init; }
        public required CostApprovalStatus ApprovalStatus { get; init; }
        public Guid? ApprovedByUserId { get; init; }
        public DateTime? ApprovedAt { get; init; }
        public bool HasDocument { get; init; }
        public string? DocumentFileName { get; init; }
        public string? PreviewSasUrl { get; init; }
        public string? DownloadSasUrl { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}
```

---

## Weryfikacja
Po zmianach uruchom:
```
cd 02-ApplicationServices/ProductDataManagementWebAPI
dotnet build src/Entities/Entities.csproj
dotnet build src/Business/Business.csproj
```

Oczekiwany wynik: błędy kompilacji tylko w projektach CQRS i WebApi (jeszcze niezmienione) — to normalne, naprawiane w kolejnych krokach.
