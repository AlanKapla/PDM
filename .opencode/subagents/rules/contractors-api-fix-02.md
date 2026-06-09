# contractors-api-fix-02 — BaseCost: Contractor string? → ContractorId Guid? + relacja

## Cel
Zastąpienie wolnotekstowego pola `Contractor string?` w `BaseCost` relacją FK `ContractorId Guid?` do nowej encji `Contractor`.
Aktualizacja wszystkich Commands, Validatorów, Handlerów i WebModels.

## Skill
Przeczytaj `.github/skills/api/skill-api-entities.md` i `.github/skills/api/skill-api-cqrs.md` przed implementacją.

## Kontekst
- Raport audytu: `.github/subagents/rules/contractors-api-audit.md`
- Encja `Contractor` istnieje już po wykonaniu `contractors-api-fix-01`
- **Stare dane są porzucane** (clean break) — migracja usuwa kolumnę `Contractor nvarchar(500)` i dodaje `ContractorId uniqueidentifier`
- `OnDelete(SetNull)` — gdy kontrahent usunięty, koszt nie ginie (ContractorId → NULL)

## Zmiany do wykonania

### 1. Modyfikacja BaseCost
Plik: `src/Entities/Models/Costs/BaseCost.cs`

Usunąć:
```csharp
public string? Contractor { get; set; }
```
Dodać:
```csharp
public Guid? ContractorId { get; set; }
public virtual Contractor? Contractor { get; set; }
```

### 2. Modyfikacja BaseCostConfiguration
Plik: `src/Entities/Configurations/Costs/BaseCostConfiguration.cs`

Usunąć linię: `builder.Property(x => x.Contractor).HasMaxLength(500);` (lub podobną)

Dodać:
```csharp
builder.HasOne(x => x.Contractor)
    .WithMany(c => c.Costs)
    .HasForeignKey(x => x.ContractorId)
    .OnDelete(DeleteBehavior.SetNull);
builder.Property(x => x.ContractorId).IsRequired(false);
```

### 3. Migracja DB dla BaseCost
Wygeneruj migrację EF Core:
```
dotnet ef migrations add update-basecost-contractor-fk --project src/Entities --startup-project src/WebApi
```

Migracja powinna:
- `migrationBuilder.DropColumn(name: "Contractor", table: "Costs")`
- `migrationBuilder.AddColumn<Guid>(name: "ContractorId", table: "Costs", nullable: true)`
- `migrationBuilder.AddForeignKey(...)` do tabeli `Contractors`, `onDelete: ReferentialAction.SetNull`

### 4. Modyfikacja TrackedCostCommandBase
Plik: `src/CQRS/CostTrackers/Shared/TrackedCostCommandBase.cs`

Zmiana:
```csharp
// Usunąć:
public string? Contractor { get; init; }
// Dodać:
public Guid? ContractorId { get; init; }
```

### 5. Modyfikacja TrackedCostCommandBaseValidator
Plik: `src/CQRS/CostTrackers/Shared/TrackedCostCommandBaseValidator.cs`

Usunąć regułę dla `Contractor` string (MaximumLength).
Dodać (opcjonalnie):
```csharp
RuleFor(x => x.ContractorId)
    .NotEqual(Guid.Empty)
    .When(x => x.ContractorId.HasValue);
```

### 6. Modyfikacja CreateTrackedCostCommandHandler
Plik: `src/CQRS/CostTrackers/CreateTrackedCost/CreateTrackedCostCommandHandler.cs`

Zmiana: `Contractor = request.Contractor` → `ContractorId = request.ContractorId`

### 7. Modyfikacja UpdateTrackedCostCommandHandler
Plik: `src/CQRS/CostTrackers/UpdateTrackedCost/UpdateTrackedCostCommandHandler.cs`

Zmiana: `cost.Contractor = request.Contractor` → `cost.ContractorId = request.ContractorId`

### 8. Modyfikacja CostTrackerHandlerBase — mapowanie
Plik: `src/CQRS/CostTrackers/Shared/CostTrackerHandlerBase.cs`

Znajdź wszystkie miejsca gdzie mapowane jest `Contractor = cost.Contractor` (2 miejsca).
Zmienić na:
```csharp
ContractorId = cost.ContractorId,
ContractorName = cost.Contractor?.Name,
```

Przy pobieraniu kosztów Include kontrahenta:
```csharp
q => q.Include(x => x.Contractor)
```

### 9. Modyfikacja CreateProjectCostCommand
Plik: `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommand.cs`

Zmiana: `public string? Contractor { get; init; }` → `public Guid? ContractorId { get; init; }`

### 10. Modyfikacja CreateProjectCostCommandValidator
Plik: `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandValidator.cs`

Usunąć MaxLength rule dla Contractor, dodać `NotEqual(Guid.Empty).When(...)` dla ContractorId.

### 11. Modyfikacja CreateProjectCostCommandHandler
Plik: `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandHandler.cs`

Zmiana wszystkich: `Contractor = request.Contractor` → `ContractorId = request.ContractorId`

### 12. Modyfikacja UpdateProjectCostCommand
Plik: `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommand.cs`

Zmiana: `public string? Contractor { get; init; }` → `public Guid? ContractorId { get; init; }`

### 13. Modyfikacja UpdateProjectCostCommandValidator
Plik: `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandValidator.cs`

Jak wyżej — usunąć MaxLength, dodać NotEqual Guid.Empty.

### 14. Modyfikacja UpdateProjectCostCommandHandler
Plik: `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandHandler.cs`

Zmiana:
- `projectCost.Contractor = request.Contractor` → `projectCost.ContractorId = request.ContractorId`
- `Contractor = projectCost.Contractor` (w zwracanym web modelu) → `ContractorId = projectCost.ContractorId, ContractorName = projectCost.Contractor?.Name`

Przy odczycie projectCost Include kontrahenta:
```csharp
q => q.Include(x => x.Contractor)
```

### 15. Modyfikacja GetProjectCostsQueryHandler
Plik: `src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs`

Zmiana w projekcji/mapowaniu: `Contractor = pc.Contractor` → `ContractorId = pc.ContractorId, ContractorName = pc.Contractor != null ? pc.Contractor.Name : null`

Upewnić się, że query Include-uje Contractor (eager loading lub projekcja w Select).

### 16. Modyfikacja TrackedCostWeb
Plik: `src/Business/Interfaces/WebModels/CostTrackers/TrackedCostWeb.cs`

Zmiana:
```csharp
// Usunąć:
public string? Contractor { get; set; }
// Dodać:
public Guid? ContractorId { get; set; }
public string? ContractorName { get; set; }
```

### 17. Modyfikacja ProjectCostListItemWeb
Plik: `src/Business/Interfaces/WebModels/ProjectCosts/ProjectCostListItemWeb.cs` (lub podobna ścieżka)

Identyczna zmiana jak w TrackedCostWeb: `string? Contractor` → `Guid? ContractorId` + `string? ContractorName`

### 18. Aktualizacja testów walidatorów
Pliki:
- `tests/WebApi.Tests/Validators/CreateProjectCostCommandValidatorTests.cs`
- `tests/WebApi.Tests/Validators/UpdateProjectCostCommandValidatorTests.cs`

Usunąć/zastąpić testy dotyczące pola `Contractor string` (MaxLength).
Dodać test: `ContractorId = Guid.Empty` powinno dać błąd walidacji.

## Weryfikacja
```
dotnet build ProductDataManagementWebAPI.sln --nologo 2>&1 | Select-Object -Last 10
dotnet test tests\WebApi.Tests\WebApi.Tests.csproj --nologo --verbosity minimal 2>&1 | Select-Object -Last 15
```
Build i testy muszą przejść.
