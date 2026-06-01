# API Fix 01 — Encja + EF Core + Migracja

## Cel
Dodanie pola `ParentId` (nullable Guid) do encji `ProjectFilePackage` z self-referencing FK, zmiana unique constraint i wygenerowanie migracji EF Core.

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Skill
Przeczytaj: `.github/skills/api/skill-api-entities.md`

## Pliki do zmiany

### 1. `src/Entities/Models/Files/ProjectFilePackage.cs`

Dodać trzy rzeczy:
- `public Guid? ParentId { get; set; }`
- `public ProjectFilePackage? Parent { get; set; }`
- `public ICollection<ProjectFilePackage> Children { get; set; } = new List<ProjectFilePackage>();`

Stan obecny encji:
```csharp
public class ProjectFilePackage : DeletableEntity
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }

    // Navigation
    public Project Project { get; set; } = default!;
    public User Owner { get; set; } = default!;
    public User CreatedByUser { get; set; } = default!;
    public TenantMember OwnerTenantMember { get; set; } = default!;
    public TenantMember CreatedByTenantMember { get; set; } = default!;

    public ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();
}
```

### 2. `src/Entities/Configurations/ProjectFilePackageConfiguration.cs`

Stan obecny konfiguracji:
```csharp
public class ProjectFilePackageConfiguration : IEntityTypeConfiguration<ProjectFilePackage>
{
    public void Configure(EntityTypeBuilder<ProjectFilePackage> builder)
    {
        builder.HasKey(pfp => pfp.Id);

        builder.Property(pfp => pfp.Name).IsRequired().HasMaxLength(200);
        builder.Property(pfp => pfp.CreatedAt).IsRequired();
        builder.Property(pfp => pfp.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasQueryFilter(pfp => !pfp.IsDeleted);

        builder.HasOne(pfp => pfp.Project).WithMany()
            .HasForeignKey(pfp => pfp.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(pfp => pfp.Owner).WithMany()
            .HasForeignKey(pfp => pfp.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(pfp => pfp.CreatedByUser).WithMany()
            .HasForeignKey(pfp => pfp.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(pfp => pfp.OwnerTenantMember).WithMany()
            .HasForeignKey(pfp => new { pfp.TenantId, pfp.OwnerId })
            .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(pfp => pfp.CreatedByTenantMember).WithMany()
            .HasForeignKey(pfp => new { pfp.TenantId, pfp.CreatedByUserId })
            .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
            .OnDelete(DeleteBehavior.Restrict);

        // OBECNY unique constraint — do zmiany
        builder.HasIndex(pfp => new { pfp.TenantId, pfp.ProjectId, pfp.OwnerId, pfp.Name })
            .IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasIndex(pfp => new { pfp.ProjectId, pfp.TenantId });
        builder.HasIndex(pfp => new { pfp.OwnerId, pfp.ProjectId });
        builder.HasIndex(pfp => new { pfp.ProjectId, pfp.IsDeleted });
    }
}
```

Zmiany w konfiguracji:
1. Dodać self-referencing FK (po bloku `builder.HasOne(pfp => pfp.CreatedByTenantMember)...`):
   ```csharp
   builder.HasOne(pfp => pfp.Parent)
       .WithMany(pfp => pfp.Children)
       .HasForeignKey(pfp => pfp.ParentId)
       .OnDelete(DeleteBehavior.Restrict);
   ```

2. Usunąć stary unique index:
   ```csharp
   builder.HasIndex(pfp => new { pfp.TenantId, pfp.ProjectId, pfp.OwnerId, pfp.Name })
       .IsUnique().HasFilter("[IsDeleted] = 0");
   ```

3. Zamiast niego dodać DWA filtrowane indeksy (SQL Server traktuje dwa NULL jako równe w unique index na nullable column — dwa filtrowane indeksy rozwiązują problem):
   ```csharp
   // Unikalność dla podkatalogów (ParentId IS NOT NULL)
   builder.HasIndex(pfp => new { pfp.TenantId, pfp.ProjectId, pfp.OwnerId, pfp.ParentId, pfp.Name })
       .IsUnique()
       .HasFilter("[IsDeleted] = 0 AND [ParentId] IS NOT NULL");

   // Unikalność dla katalogów głównych (ParentId IS NULL)
   builder.HasIndex(pfp => new { pfp.TenantId, pfp.ProjectId, pfp.OwnerId, pfp.Name })
       .IsUnique()
       .HasFilter("[IsDeleted] = 0 AND [ParentId] IS NULL");
   ```

4. Dodać index na `ParentId` dla lookupów dzieci:
   ```csharp
   builder.HasIndex(pfp => pfp.ParentId);
   ```

## Migracja EF Core

Po zmianach w encji i konfiguracji, wygeneruj migrację:
```
cd src/Entities
dotnet ef migrations add AddDirectoryHierarchyToProjectFilePackage --project ../Entities --startup-project ../WebApi
```

Sprawdź wygenerowaną migrację — powinna zawierać:
- Dodanie kolumny `ParentId` (nullable Guid)
- Dodanie FK do tabeli `ProjectFilePackages`
- Usunięcie starego unique index
- Dodanie dwóch nowych filtrowanych unique indexów
- Dodanie zwykłego index na `ParentId`

## Weryfikacja
```
dotnet build src/Entities/Entities.csproj
```
Build musi przejść bez błędów.
