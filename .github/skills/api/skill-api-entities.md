# Skill: API / Encje i EF Core

## Opis
Tworzenie encji EF Core, konfiguracji i migracji bazy danych.

## Kiedy używać
Użyj tego skilla gdy tworzysz lub modyfikujesz encję, konfigurację EF lub generujesz migrację.

---

## Lokalizacja

```
src/Entities/Models/{Namespace}/{NazwaEncji}.cs
src/Entities/Configurations/{NazwaEncji}Configuration.cs
src/Entities/Migrations/          ← generowane przez EF Core
```

## Encja bazowa

```csharp
// Wszystkie encje dziedziczą BaseEntity
public class Project : BaseEntity  // BaseEntity ma: Guid Id = Guid.NewGuid()
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    // Nawigacje EF Core
    public virtual Tenant Tenant { get; set; } = default!;
    public virtual ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
}

// Encja z soft-delete
public class ProjectCost : DeletableEntity  // DeletableEntity : BaseEntity + IsDeleted, DeletedAt
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = default!;
    public decimal? Net { get; set; }
}
```

## Konfiguracja EF Core

```csharp
// Entities/Configurations/ProjectConfiguration.cs
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Net)
            .HasPrecision(18, 2);

        // Relacje
        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Members)
            .WithOne(m => m.Project)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indeksy
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => new { p.TenantId, p.Name }).IsUnique();
    }
}
```

## Global Query Filters (soft-delete)

```csharp
// AppDbContext.cs — automatyczny filtr dla DeletableEntity
modelBuilder.Entity<ProjectCost>()
    .HasQueryFilter(e => !e.IsDeleted);
```

## Migracje

```bash
# Generuj migrację (NIE uruchamiaj database update na produkcji)
dotnet ef migrations add {opisowa-nazwa-migracji} \
    --project src/Entities \
    --startup-project src/WebApi

# Przykładowe nazwy:
# add-project-currency
# add-tracked-cost-contractor-field
# add-work-schedule-stage-color

# Sprawdź czy migracja jest pusta (tylko model snapshot):
# Jeśli Up() i Down() są puste → usuń migrację
dotnet ef migrations remove --project src/Entities --startup-project src/WebApi
```

## Zasady

- Każda encja w osobnym pliku, konfiguracja w osobnym pliku
- `HasPrecision(18, 2)` dla wszystkich pól `decimal`
- `default!` dla wymaganych właściwości nawigacyjnych
- `OnDelete(DeleteBehavior.Restrict)` zamiast Cascade dla relacji tenant/project
- `OnDelete(DeleteBehavior.Cascade)` dla dzieci encji (np. pozycje kosztorysu)
- Global Query Filter dla wszystkich encji z `IsDeleted`
- Nazwy migracji opisowe w kebab-case
- NIE uruchamiaj `database update` — tylko generuj migrację
