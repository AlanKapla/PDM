# contractors-api-fix-01 — Encja Contractor + EF konfiguracja + DbSet + migracja

## Cel
Dodanie nowej encji `Contractor` (tenant-scoped) do warstwy Entities oraz wygenerowanie migracji DB.

## Skill
Przeczytaj `.github/skills/api/skill-api-entities.md` przed implementacją.

## Kontekst
- Raport audytu: `.github/subagents/rules/contractors-api-audit.md`
- Encja należy do tenanta (tylko `TenantId`, bez `ProjectId`)
- Używa soft-delete (dziedziczy po `DeletableEntity`)
- Wzorzec: `src/Entities/Models/Tenants/Tenant.cs` i jego konfiguracja

## Pola encji

```csharp
public class Contractor : DeletableEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;       // wymagane, max 500
    public string? TaxId { get; set; }                  // NIP/VAT, max 50
    public string? Email { get; set; }                  // max 200
    public string? PhoneNumber { get; set; }            // max 20
    public string? Street { get; set; }                 // max 300
    public string? City { get; set; }                   // max 100
    public string? PostalCode { get; set; }             // max 20
    public string? Country { get; set; }                // max 100
    public string? Notes { get; set; }                  // max 2000
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual Tenant Tenant { get; set; } = default!;
    public virtual ICollection<BaseCost> Costs { get; set; } = new List<BaseCost>();
}
```

## Zmiany do wykonania

### 1. Nowa encja
Plik: `src/Entities/Models/Tenants/Contractor.cs`

### 2. Nowa konfiguracja EF Core
Plik: `src/Entities/Configurations/ContractorConfiguration.cs`

Wymagania konfiguracji:
- Tabela: `Contractors`
- PK: `Id` (NEWSEQUENTIALID)
- `Name`: required, max 500
- `TaxId`: max 50, nullable
- `Email`: max 200, nullable
- `PhoneNumber`: max 20, nullable
- `Street`: max 300, nullable
- `City`: max 100, nullable
- `PostalCode`: max 20, nullable
- `Country`: max 100, nullable
- `Notes`: max 2000, nullable
- Relacja FK do `Tenant` (HasOne Tenant, WithMany — Tenant nie musi mieć kolekcji)
- Indeks na `TenantId`
- Indeks filtrowany na `(TenantId, Name)` gdzie `IsDeleted = 0`

### 3. Dodanie DbSet do AppDbContext
Plik: `src/Entities/Context/AppDbContext.cs`

Dodać:
```csharp
public DbSet<Contractor> Contractors => Set<Contractor>();
```

### 4. Migracja DB
Wygeneruj migrację EF Core:
```
dotnet ef migrations add add-contractors --project src/Entities --startup-project src/WebApi
```

Migracja powinna:
- Utworzyć tabelę `Contractors` z wszystkimi kolumnami
- Dodać indeks na `TenantId`
- Dodać indeks filtrowany na `(TenantId, Name)` WHERE `IsDeleted = 0`
- Dodać FK do tabeli `Tenants`

## Weryfikacja
Po implementacji uruchom:
```
dotnet build ProductDataManagementWebAPI.sln --nologo 2>&1 | Select-Object -Last 10
```
Build musi zakończyć się `0 Error(s)`.
