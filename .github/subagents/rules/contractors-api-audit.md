# Audyt API — Feature: Kontrahenci (Contractors)

Data: 2026-05-18  
Agent: api-audit-agent

---

## BLOK 1 — Stan obecny

### Encje zaangażowane

Aktualnie nie istnieje encja `Contractor`. Informacja o kontrahencie jest przechowywana jako **wolny tekst** (`string?`) bezpośrednio w encji `BaseCost`.

| Encja | Ścieżka | Pole Contractor |
|-------|---------|----------------|
| `BaseCost` | `src/Entities/Models/Costs/BaseCost.cs` | `public string? Contractor { get; set; }` — linia 18 |
| `TrackedCost` | dziedziczy po `BaseCost` | dziedziczy pole |
| `ProjectCost` | dziedziczy po `BaseCost` | dziedziczy pole |

**BaseCost** jest abstract, zapisywana w tabeli `Costs` (TPH discriminator `CostType`).  
Kolumna w DB: `Contractor nvarchar(500) NULL` — brak indeksu na tej kolumnie.

### Endpointy już istniejące (związane z Costs)

Brak dedykowanego `ContractorController`. Contractor string przechodzi przez:
- `POST/PUT /api/projects/{projectId}/costs` (ProjectCost)
- `POST/PUT /api/projects/{projectId}/cost-tracker` (TrackedCost)

### Serwisy / uprawnienia

| Uprawnienie | Kod | Scope | Przypisane do |
|-------------|-----|-------|---------------|
| Odczyt tenanta | `TENANT.VIEW` | Tenant | TenantAdmin, TenantMember |
| Edycja tenanta | `TENANT.EDIT` | Tenant | TenantAdmin only |

Oba kody **istnieją** w `PermissionCodes.cs` i są w `PermissionScopes.cs` ze scope `PermissionScope.Tenant`.  
Nie trzeba dodawać nowych PermissionCodes dla podstawowych operacji CRUD na Contractors.

---

## BLOK 2 — Luki i braki

| Brak / Luka | Warstwa | Priorytet | Opis |
|-------------|---------|-----------|------|
| Brak encji `Contractor` | Entities | KRYTYCZNY | Nowa tabela `Contractors` scoped do TenantId |
| Brak konfiguracji EF `ContractorConfiguration` | Entities | KRYTYCZNY | Mapping + indeks na TenantId |
| Brak `ContractorId Guid?` w `BaseCost` | Entities | KRYTYCZNY | Zastąpienie `string? Contractor` FK-em |
| Brak migracji DB | Entities | KRYTYCZNY | DropColumn + CreateTable + AddForeignKey |
| Brak `ContractorWeb` | Business/WebModels | WYSOKI | DTO odpowiedzi dla listy/operacji |
| Brak CQRS GetContractors | CQRS/Contractors | WYSOKI | Query lista kontrahentów tenanta |
| Brak CQRS CreateContractor | CQRS/Contractors | WYSOKI | Command tworzenie |
| Brak CQRS UpdateContractor | CQRS/Contractors | WYSOKI | Command aktualizacja |
| Brak CQRS DeleteContractor | CQRS/Contractors | WYSOKI | Command soft-delete lub hard-delete |
| Brak `ContractorController` | WebApi | WYSOKI | Endpointy CRUD |
| `TrackedCostCommandBase` — pole `string? Contractor` | CQRS/CostTrackers | WYSOKI | Zamiana na `Guid? ContractorId` |
| `CreateProjectCostCommand` — pole `string? Contractor` | CQRS/ProjectCosts | WYSOKI | Zamiana na `Guid? ContractorId` |
| `UpdateProjectCostCommand` — pole `string? Contractor` | CQRS/ProjectCosts | WYSOKI | Zamiana na `Guid? ContractorId` |
| Walidator `TrackedCostCommandBaseValidator` | CQRS/CostTrackers | ŚREDNI | Usunąć MaxLength string, opcjonalnie walidować GUID |
| Walidator `CreateProjectCostCommandValidator` | CQRS/ProjectCosts | ŚREDNI | j.w. |
| Walidator `UpdateProjectCostCommandValidator` | CQRS/ProjectCosts | ŚREDNI | j.w. |
| `TrackedCostWeb` — pole `string? Contractor` | Business/WebModels | ŚREDNI | Zamiana na `Guid? ContractorId` + opcjonalnie `string? ContractorName` |
| `ProjectCostListItemWeb` — pole `string? Contractor` | Business/WebModels | ŚREDNI | j.w. |
| Handlery TrackedCost (Create, Update, HandlerBase ×2) | CQRS/CostTrackers | ŚREDNI | Zamiana mapowania Contractor string → ContractorId |
| Handlery ProjectCost (Create, Update, GetList) | CQRS/ProjectCosts | ŚREDNI | j.w. |
| Testy walidatorów ProjectCost (Create + Update) | tests/WebApi.Tests | NISKI | Aktualizacja testów po usunięciu pola Contractor string |

---

## BLOK 3 — Zmiany w encjach / DB

| Encja | Zmiana | Typ | Wymaga migracji |
|-------|--------|-----|----------------|
| `BaseCost` | Usunąć `string? Contractor`, dodać `Guid? ContractorId` + nav `Contractor? Contractor` | Modyfikacja istniejącej encji | TAK — DropColumn + AddColumn + FK |
| `Contractor` | Nowa encja tenant-scoped | Nowa encja | TAK — CreateTable |

### Propozycja pól encji `Contractor`

```csharp
// src/Entities/Models/Tenants/Contractor.cs
public class Contractor : DeletableEntity          // soft-delete jak inne encje
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;   // max 500
    public string? TaxId { get; set; }              // NIP / VAT — max 50 (pytanie domenowe)
    public string? Email { get; set; }              // max 200 (pytanie domenowe)
    public string? PhoneNumber { get; set; }        // max 20 (pytanie domenowe)
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual Tenant Tenant { get; set; } = default!;
    public virtual ICollection<BaseCost> Costs { get; set; } = new List<BaseCost>();
}
```

### Zmiana w BaseCost

```csharp
// Usunąć:
public string? Contractor { get; set; }

// Dodać:
public Guid? ContractorId { get; set; }
public virtual Contractor? Contractor { get; set; }
```

### Zmiana w BaseCostConfiguration

```csharp
// Usunąć:
builder.Property(x => x.Contractor).HasMaxLength(500);

// Dodać:
builder.HasOne(x => x.Contractor)
    .WithMany(c => c.Costs)
    .HasForeignKey(x => x.ContractorId)
    .OnDelete(DeleteBehavior.SetNull);   // SetNull żeby nie tracić kosztu gdy contractor usunięty

builder.Property(x => x.ContractorId).IsRequired(false);
```

### Migracja (format — ostatnia migracja: `20260518122030_migration-2.cs`)

```csharp
// Up:
migrationBuilder.CreateTable(
    name: "Contractors",
    columns: table => new
    {
        Id = table.Column<Guid>(nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
        TenantId = table.Column<Guid>(nullable: false),
        Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
        TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
        Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
        PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
        CreatedAt = table.Column<DateTime>(nullable: false),
        UpdatedAt = table.Column<DateTime>(nullable: true),
        IsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
        DeletedAt = table.Column<DateTime>(nullable: true),
    }, ...);

migrationBuilder.DropColumn(name: "Contractor", table: "Costs");

migrationBuilder.AddColumn<Guid>(
    name: "ContractorId", table: "Costs",
    type: "uniqueidentifier", nullable: true);

migrationBuilder.AddForeignKey(...);
```

> ⚠️ **RYZYKO DANYCH**: Istniejące wartości tekstowe w kolumnie `Contractor` zostaną trwale utracone przy DropColumn. Jeśli środowisko Prod/Staging ma dane, wymagana jest wcześniejsza strategia migracji danych (np. seed Contractor entities z unikalnych wartości).

---

## BLOK 4 — Nowe Commands / Queries

| Command/Query | Typ | Opis | Handler |
|--------------|-----|------|---------|
| `GetContractorsQuery` | Nowe Query | Lista kontrahentów tenanta (TENANT.VIEW) | `GetContractorsQueryHandler` |
| `GetContractorQuery` | Nowe Query | Jeden kontrahent po ID (TENANT.VIEW) | `GetContractorQueryHandler` |
| `CreateContractorCommand` | Nowy Command | Tworzenie kontrahenta (TENANT.EDIT) | `CreateContractorCommandHandler` |
| `UpdateContractorCommand` | Nowy Command | Aktualizacja kontrahenta (TENANT.EDIT) | `UpdateContractorCommandHandler` |
| `DeleteContractorCommand` | Nowy Command | Usunięcie kontrahenta (TENANT.EDIT) — soft/hard TBD | `DeleteContractorCommandHandler` |

### Wzorzec nowego Command (tenant-scoped)

Wzorzec z `UpdateTenantCommand` (`src/CQRS/Tenants/UpdateTenant/UpdateTenantCommand.cs`):
```csharp
public sealed record CreateContractorCommand : IRequestCommand<ContractorWeb>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required string Name { get; init; }
    public string? TaxId { get; init; }
    // ...

    public string PermissionCode => PermissionCodes.TenantEdit;
    public ResourceRef GetResource() => new(TenantId: TenantId);  // tylko TenantId — brak ProjectId
}
```

### Zmodyfikowane Commands

| Command | Zmiana |
|---------|--------|
| `TrackedCostCommandBase` | `string? Contractor` → `Guid? ContractorId` |
| `CreateProjectCostCommand` | `string? Contractor` → `Guid? ContractorId` |
| `UpdateProjectCostCommand` | `string? Contractor` → `Guid? ContractorId` |

---

## BLOK 5 — Zmiany w kontrolerach

| Endpoint | HTTP Method | Nowy/Modyfikacja | Opis |
|----------|------------|-----------------|------|
| `GET /api/tenants/{tenantId}/contractors` | GET | Nowy | Lista kontrahentów, wymaga TENANT.VIEW |
| `GET /api/tenants/{tenantId}/contractors/{contractorId}` | GET | Nowy | Jeden kontrahent, wymaga TENANT.VIEW |
| `POST /api/tenants/{tenantId}/contractors` | POST | Nowy | Tworzenie, wymaga TENANT.EDIT |
| `PUT /api/tenants/{tenantId}/contractors/{contractorId}` | PUT | Nowy | Aktualizacja, wymaga TENANT.EDIT |
| `DELETE /api/tenants/{tenantId}/contractors/{contractorId}` | DELETE | Nowy | Usunięcie, wymaga TENANT.EDIT |

Nowy plik: `src/WebApi/Controllers/ContractorController.cs`  
Wzorzec: `src/WebApi/Controllers/TenantController.cs`

---

## BLOK 6 — Zmiany w serwisach

Brak potrzeby tworzenia dedykowanego serwisu domenowego — operacje CRUD na Contractor są proste i mogą być obsłużone bezpośrednio przez handlery MediatR z użyciem `IRepository<Contractor>` (wzorzec jak `UpdateTenantCommandHandler`).

| Serwis | Interfejs | Nowy/Modyfikacja | Metody |
|--------|-----------|-----------------|--------|
| Brak nowego serwisu domenowego | — | — | Handlery używają repo bezpośrednio |

---

## BLOK 7 — Problemy i ryzyka

| # | Problem | Warstwa | Ryzyko | Rekomendacja |
|---|---------|---------|--------|--------------|
| 1 | **Utrata danych** — kolumna `Contractor nvarchar(500)` zawiera wolny tekst. DropColumn spowoduje trwałą utratę danych. | Entities/Migracja | WYSOKIE | Przed migracją: wyciągnąć unikalne wartości `Contractor`, stworzyć encje `Contractor`, zaktualizować `ContractorId` w istniejących rekordach Costs — wymaga migration SQL skryptu z danymi. |
| 2 | **Niespójność maxLength** — DB ma `nvarchar(500)`, walidatory Commands mają `MaximumLength(300)`. | Entities/CQRS | NISKI | Ujednolicić przy refaktorze (proponuję 500 w DB, 500 w walidatorach). |
| 3 | **WebModel TrackedCostWeb i ProjectCostListItemWeb** — po zmianie na `Guid? ContractorId` UI traci dostęp do nazwy. Dwa warianty: (a) zwracać tylko ID i UI pobiera listę z cache, (b) dołączyć `ContractorName string?` w web modelu (wymaga JOIN w query). | CQRS/WebModels | ŚREDNIE | Decyzja domenowa — rekomendacja: zwracać `ContractorId` + `ContractorName` embedded (JOIN przy GetProjectCosts i CostTrackerHandlerBase). |
| 4 | **DeleteContractor — integritas** — jeśli kontrahent jest użyty w kosztach, delete powinien być soft-delete lub zablokowany. Przy `OnDelete(SetNull)` soft-delete ContractorId w kosztach stanie się null. | Entities | ŚREDNIE | Użyć `OnDelete(SetNull)` w FK + soft-delete dla Contractor (pole `IsDeleted`). |
| 5 | **CostTrackerHandlerBase — 2 miejsca mapowania** — linie ~107 i ~540 mapują `Contractor = cost.Contractor`. Po zmianie wymagają albo eager-load nav property, albo mapowania `ContractorId`. | CQRS | NISKI | Przy Include() lub projekcji z JOIN dodać ContractorName. |
| 6 | **Testy walidatorów** — `CreateProjectCostCommandValidatorTests` i `UpdateProjectCostCommandValidatorTests` mają testy dla `Contractor string`. | tests | NISKI | Zaktualizować / usunąć testy dotyczące MaxLength string Contractor, dodać testy dla ContractorId. |
| 7 | **Brak GetContractorById query** — jeśli formularze kosztu mają pokazywać wybranego kontrahenta, potrzebny endpoint GET single. | CQRS/WebApi | NISKI | Dodać `GetContractorQuery` + endpoint. |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe encje | 1 (`Contractor`) |
| Nowe Commands | 3 (Create, Update, Delete Contractor) |
| Nowe Queries | 2 (GetContractors, GetContractor) |
| Zmodyfikowane Commands | 3 (TrackedCostCommandBase, CreateProjectCostCommand, UpdateProjectCostCommand) |
| Zmodyfikowane Queries/Handlery | 4 (CostTrackerHandlerBase ×2, GetProjectCostsQueryHandler, Create/Update ProjectCost handlers) |
| Nowe Web modele | 1 (`ContractorWeb`) |
| Zmodyfikowane Web modele | 2 (`TrackedCostWeb`, `ProjectCostListItemWeb`) |
| Nowe endpointy | 5 |
| Nowe serwisy | 0 |
| Wymaga migracji DB | **TAK** |
| Wymaga migracji danych | **TAK** (jeśli środowisko ma dane) |
| Pliki testów do aktualizacji | 2 |
| Pytania domenowe | 5 |

---

## Pytania domenowe wymagające decyzji

1. **Pola encji Contractor** — Jakie pola powinna mieć encja `Contractor`? Minimum: `Name`. Czy potrzebne: `TaxId (NIP)`, `Email`, `PhoneNumber`, `Address`, `Notes`?

2. **DeleteContractor — strategia** — Soft-delete (jak inne encje: `IsDeleted = true`) czy hard-delete? Co zrobić z kosztami powiązanymi z usuniętym kontrahentem?

3. **WebModel kosztów po migracji** — Czy `TrackedCostWeb` i `ProjectCostListItemWeb` mają zwracać tylko `Guid? ContractorId` (i UI rozwiązuje nazwę) czy embedded `ContractorName string?` (join w query)?

4. **Migracja danych** — Czy środowisko Staging/Prod posiada dane w kolumnie `Contractor string?`, które trzeba zachować? Jeśli tak — wymagany jest skrypt SQL do stworzenia encji Contractor z unikalnych wartości i zaktualizowania FK przed DropColumn.

5. **Uprawnienia dla GetContractors** — Czy lista kontrahentów jest dostępna dla wszystkich członków tenanta (`TENANT.VIEW`) czy tylko dla admina (`TENANT.EDIT`)? (Feature brief mówi TENANT.VIEW dla odczytu — potwierdzenie.)

---

## Pełna lista plików do modyfikacji

### Nowe pliki

| Plik | Opis |
|------|------|
| `src/Entities/Models/Tenants/Contractor.cs` | Nowa encja |
| `src/Entities/Configurations/ContractorConfiguration.cs` | EF Core config |
| `src/Entities/Migrations/{timestamp}_add-contractors.cs` | Migracja DB |
| `src/Business/Interfaces/WebModels/Contractors/ContractorWeb.cs` | DTO odpowiedzi |
| `src/CQRS/Contractors/GetContractors/GetContractorsQuery.cs` | |
| `src/CQRS/Contractors/GetContractors/GetContractorsQueryHandler.cs` | |
| `src/CQRS/Contractors/GetContractor/GetContractorQuery.cs` | |
| `src/CQRS/Contractors/GetContractor/GetContractorQueryHandler.cs` | |
| `src/CQRS/Contractors/CreateContractor/CreateContractorCommand.cs` | |
| `src/CQRS/Contractors/CreateContractor/CreateContractorCommandHandler.cs` | |
| `src/CQRS/Contractors/CreateContractor/CreateContractorCommandValidator.cs` | |
| `src/CQRS/Contractors/UpdateContractor/UpdateContractorCommand.cs` | |
| `src/CQRS/Contractors/UpdateContractor/UpdateContractorCommandHandler.cs` | |
| `src/CQRS/Contractors/UpdateContractor/UpdateContractorCommandValidator.cs` | |
| `src/CQRS/Contractors/DeleteContractor/DeleteContractorCommand.cs` | |
| `src/CQRS/Contractors/DeleteContractor/DeleteContractorCommandHandler.cs` | |
| `src/WebApi/Controllers/ContractorController.cs` | 5 endpoints |

### Modyfikowane pliki

| Plik | Zmiana |
|------|--------|
| `src/Entities/Models/Costs/BaseCost.cs` | Usunąć `string? Contractor`, dodać `Guid? ContractorId` + nav property |
| `src/Entities/Configurations/Costs/BaseCostConfiguration.cs` | Usunąć `HasMaxLength(500)` dla Contractor, dodać FK config + opcjonalnie HasIndex na ContractorId |
| `src/CQRS/CostTrackers/Shared/TrackedCostCommandBase.cs` | `string? Contractor` → `Guid? ContractorId` |
| `src/CQRS/CostTrackers/Shared/TrackedCostCommandBaseValidator.cs` | Usunąć MaxLength rule dla Contractor |
| `src/CQRS/CostTrackers/CreateTrackedCost/CreateTrackedCostCommandHandler.cs` | `Contractor = request.Contractor` → `ContractorId = request.ContractorId` (linia ~65) |
| `src/CQRS/CostTrackers/UpdateTrackedCost/UpdateTrackedCostCommandHandler.cs` | `cost.Contractor = request.Contractor` → `cost.ContractorId = request.ContractorId` (linia ~57) |
| `src/CQRS/CostTrackers/Shared/CostTrackerHandlerBase.cs` | Aktualizacja 2 mappingów (linie ~107, ~540): `Contractor = cost.Contractor` → `ContractorId` / `ContractorName` |
| `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommand.cs` | `string? Contractor` → `Guid? ContractorId` (linia 16) |
| `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandValidator.cs` | Usunąć MaxLength rule dla Contractor (linia 19) |
| `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandHandler.cs` | `Contractor = request.Contractor` → `ContractorId = request.ContractorId` (linie 68, 88) |
| `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommand.cs` | `string? Contractor` → `Guid? ContractorId` (linia 18) |
| `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandValidator.cs` | Usunąć MaxLength rule dla Contractor (linia 21) |
| `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandHandler.cs` | `projectCost.Contractor = request.Contractor` → `projectCost.ContractorId = request.ContractorId` (linia 116); `Contractor = projectCost.Contractor` → ContractorId/Name (linia 138) |
| `src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs` | `Contractor = pc.Contractor` → `ContractorId = pc.ContractorId` / ContractorName (linia ~161) |
| `src/Business/Interfaces/WebModels/CostTrackers/TrackedCostWeb.cs` | `string? Contractor` → `Guid? ContractorId` + opcjonalnie `string? ContractorName` |
| `src/Business/Interfaces/WebModels/ProjectCosts/ProjectCostListItemWeb.cs` | `string? Contractor` → `Guid? ContractorId` + opcjonalnie `string? ContractorName` |
| `tests/WebApi.Tests/Validators/CreateProjectCostCommandValidatorTests.cs` | Usunąć/zastąpić testy Contractor string (linie 141–165, 224) |
| `tests/WebApi.Tests/Validators/UpdateProjectCostCommandValidatorTests.cs` | Usunąć/zastąpić testy Contractor string (linie 118–126, 185) |
