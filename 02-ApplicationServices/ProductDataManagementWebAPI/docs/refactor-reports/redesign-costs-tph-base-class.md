# Refactor Report — Redesign Costs: TPH Base Class

**Branch:** `AK/cost-estimate-refactor`  
**Data:** 2025-06-06  
**Zakres:** Wprowadzenie wspólnej klasy bazowej `BaseCost` z TPH (Table-Per-Hierarchy) dla encji `TrackedCost` i `ProjectCost`, unifikacja załączników (`BaseCostAttachment`), aktualizacja wszystkich powiązanych handlerów, konfiguracji EF Core i generacja migracji.

---

## Motywacja

`TrackedCost` i `ProjectCost` posiadały rozdzielne tabele i duplikowały pola (`Name`, `Description`, `Net`, `Gross`, `Date`, `TenantId`, `ProjectId`, `CreatedAt`, `UpdatedAt`). `ProjectCost` przechowywał załączniki jako inline-pola (`DocumentName`, `DocumentBlobName`, `ContentType`, `FileSize`), podczas gdy `TrackedCost` korzystał z oddzielnej tabeli `TrackedCostAttachments`. Celem refaktoru było:

- Eliminacja duplikacji pól przez wspólną klasę bazową
- Unifikacja mechanizmu załączników
- Uproszczenie zapytań poprzez możliwość operowania na `BaseCost`
- Przygotowanie pod rozszerzenie dashboardu finansowego o zaakceptowane `ProjectCost`

---

## Zmiany w encjach

### `src/Entities/Models/Costs/BaseCost.cs` — **NOWY PLIK**

Abstrakcyjna klasa bazowa TPH dziedzicząca po `DeletableEntity`.

```
Pola: TenantId, ProjectId, Name, Number, Description, Net (decimal), Gross (decimal),
      Contractor, Date (DateOnly), CreatedAt, UpdatedAt
Nawigacja: virtual ICollection<BaseCostAttachment> Attachments
Dziedziczenie: public abstract class BaseCost : DeletableEntity
```

### `src/Entities/Models/Costs/BaseCostAttachment.cs` — **NOWY PLIK**

Ujednolicona encja załącznika zastępująca `TrackedCostAttachment` oraz inline-pola w `ProjectCost`.

```
Pola: CostId (Guid, FK → BaseCost), TenantId, ProjectId,
      OriginalFileName, BlobName, ContentType, FileSize (long), CreatedAt
Nawigacja: virtual BaseCost Cost
```

### `src/Entities/Models/CostTrackers/TrackedCost.cs` — **ZMODYFIKOWANY**

- Usuniięto zduplikowane pola (`Name`, `Description`, `Net`, `Gross`, `Date`, `TenantId`, `ProjectId`, `CreatedAt`, `UpdatedAt`)
- Zmieniono dziedziczenie: `BaseEntity` → `BaseCost`
- Usunięto nawigację do `TrackedCostAttachment`, zastąpiono przez `BaseCost.Attachments`
- Pozostawiono pola specyficzne: `UserId`, `WorkItemId`, `WorkItemLinkId`

### `src/Entities/Models/ProjectCost.cs` — **ZMODYFIKOWANY**

- Usuniięto zduplikowane pola (`Name`, `Description`, `Net`, `Gross`, `Date`, `TenantId`, `ProjectId`, `CreatedAt`, `UpdatedAt`)
- Zmieniono dziedziczenie: `BaseEntity` → `BaseCost`
- Usunięto inline-pola dokumentu (`DocumentName`, `DocumentBlobName`, `ContentType`, `FileSize`)
- Dodano pola: `IsAccepted (bool)`, `AcceptedByUserId (Guid?)`, `AcceptedAt (DateTime?)`
- Nawigacja zmieniona z `User` na `ProjectMember` (złożony FK: `ProjectId + UserId`)

### `src/Entities/Models/CostTrackers/TrackedCostAttachment.cs` — **USUNIĘTY**

Zastąpiony przez `BaseCostAttachment`.

---

## Zmiany w konfiguracji EF Core

### `src/Entities/Configurations/Costs/BaseCostConfiguration.cs` — **NOWY PLIK**

```
- ToTable("Costs") — TPH tabela
- HasDiscriminator<string>("CostType") z wartościami "TrackedCost" / "ProjectCost"
- Precyzja decimal: Net (18,4), Gross (18,4)
- MaxLength: Name (200), Description (500), Contractor (200), Number (50)
```

### `src/Entities/Configurations/Costs/BaseCostAttachmentConfiguration.cs` — **NOWY PLIK**

```
- ToTable("CostAttachments")
- FK: CostId → BaseCost (Cascade delete)
- MaxLength: OriginalFileName, BlobName, ContentType
```

### `src/Entities/Configurations/CostTrackers/TrackedCostConfiguration.cs` — **ZMODYFIKOWANY**

- Usunięto konfigurację pól przeniesionych do `BaseCostConfiguration`
- Usunięto nawigację do `TrackedCostAttachment`
- Usunięto `ToTable("TrackedCosts")` — teraz zarządzane przez TPH w `BaseCostConfiguration`

### `src/Entities/Configurations/ProjectCostConfiguration.cs` — **ZMODYFIKOWANY**

- Usunięto konfigurację pól przeniesionych do `BaseCostConfiguration`
- Usunięto konfigurację inline-pól dokumentu
- Dodano konfigurację: `IsAccepted`, `AcceptedByUserId`, `AcceptedAt`
- Zmieniono FK nawigacji: `User (UserId)` → `ProjectMember (ProjectId, UserId)` — kompozytowy FK

### `src/Entities/Configurations/CostTrackers/TrackedCostAttachmentConfiguration.cs` — **USUNIĘTY**

---

## Zmiany w DbContext

### `src/Entities/Context/AppDbContext.cs` — **ZMODYFIKOWANY**

```diff
+ DbSet<BaseCostAttachment> CostAttachments
- DbSet<TrackedCostAttachment> TrackedCostAttachments  (usunięty)
  DbSet<TrackedCost> TrackedCosts                       (bez zmian)
  DbSet<ProjectCost> ProjectCosts                       (bez zmian)
```

---

## Zmiany w serwisach

### `src/Business/Interfaces/Services/ICostTrackerAttachmentService.cs` — **ZMODYFIKOWANY**

Zmiana typów z `TrackedCostAttachment` na `BaseCostAttachment` we wszystkich sygnaturach interfejsu.

### `src/Business/Implementation/Services/CostTrackerAttachmentService.cs` — **ZMODYFIKOWANY**

- Zmiana repozytorium z `IRepository<TrackedCostAttachment>` na `IRepository<BaseCostAttachment>`
- Zmiana nazwy kontenera Blob: `BlobContainerNames.ProjectCosts` → `BlobContainerNames.CostTrackers` (ujednolicony kontener)
- Aktualizacja pól przy tworzeniu załącznika: `TrackedCostId` → `CostId`

---

## Zmiany w handlerach CQRS

### `src/CQRS/CostTrackers/Shared/CostTrackerHandlerBase.cs` — **ZMODYFIKOWANY**

- Zmiana typów `ILookup<Guid, TrackedCostAttachment>` → `ILookup<Guid, BaseCostAttachment>` w metodzie `MapTrackedCostToWeb`
- Sygnatura: `MapTrackedCostToWeb(TrackedCost cost, IEnumerable<BaseCostAttachment> attachments)` — nadal przyjmuje `TrackedCost` (nie `BaseCost`)

### `src/CQRS/CostTrackers/Shared/TrackedCostMutationHandlerBase.cs` — **ZMODYFIKOWANY**

- Usunięto repozytorium `IRepository<TrackedCostAttachment>`
- Dodano repozytorium `IRepository<BaseCostAttachment>`
- Metody tworzenia/usuwania załączników zaktualizowane do `BaseCostAttachment`

### `src/CQRS/CostTrackers/CreateTrackedCost/CreateTrackedCostCommandHandler.cs` — **ZMODYFIKOWANY**

- Mapowanie `Net = request.NetAmount`, `Gross = request.GrossAmount ?? request.NetAmount` (wcześniej używano innych nazw pól)
- Tworzenie `BaseCostAttachment` zamiast `TrackedCostAttachment`

### `src/CQRS/CostTrackers/UpdateTrackedCost/UpdateTrackedCostCommandHandler.cs` — **ZMODYFIKOWANY**

- Analogiczne zmiany jak w `Create`
- Operacje na `BaseCostAttachment`

### `src/CQRS/CostTrackers/DeleteTrackedCost/DeleteTrackedCostCommandHandler.cs` — **ZMODYFIKOWANY**

- Usuwanie przez `IRepository<BaseCostAttachment>` zamiast `TrackedCostAttachment`

### `src/CQRS/ProjectCosts/Shared/ProjectCostHandlerBase.cs` — **PRZEPISANY**

Nowa klasa bazowa dla handlerów `ProjectCost`:

```
- UploadDocumentToCostAsync(ProjectCost, IFormFile, CancellationToken)
    → tworzy BaseCostAttachment, uploaduje do BlobContainerNames.CostTrackers
- RemoveAttachmentsAsync(Guid costId, CancellationToken)
    → soft-delete wszystkich BaseCostAttachment dla danego kosztu
Zależności: IBlobStorageService, IRepository<BaseCostAttachment>
```

### `src/CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandHandler.cs` — **PRZEPISANY**

- Mapowanie `Net = request.NetAmount`, `Gross = request.GrossAmount ?? request.NetAmount`
- Upload dokumentu przez `UploadDocumentToCostAsync` z klasy bazowej (zamiast inline-pól)
- Usunięto inline-pola dokumentu z inicjalizacji encji

### `src/CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandHandler.cs` — **PRZEPISANY**

- `ApplyFieldUpdates`: `projectCost.Net = request.NetAmount`, `projectCost.Gross = request.GrossAmount ?? request.NetAmount`
- `HandleDocumentOperationsAsync`: obsługa `RemoveDocument`, `UpdatedDocument`, `Document` przez metody klasy bazowej
- `HandleSharedUserUpdateAsync`: użytkownik z dostępem udostępnionym może aktualizować wyłącznie pole `IsAccepted`

### `src/CQRS/ProjectCosts/DeleteProjectCost/DeleteProjectCostCommandHandler.cs` — **PRZEPISANY**

- Wywołanie `RemoveAttachmentsAsync` przed usunięciem kosztu
- Użycie `SaveChangesAsync` zamiast `Update` + `SaveChanges`

### `src/CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs` — **PRZEPISANY**

- Pobieranie załączników przez `IReadRepository<BaseCostAttachment>` zamiast inline-pól
- Mapowanie `NetAmount = pc.Net`, `GrossAmount = pc.Gross`
- `HasDocument = attachments.Any()`, `DocumentFileName` z pierwszego załącznika
- Informacje o użytkowniku przez `ProjectMemberUserInfo`

### `src/CQRS/ProjectDashboard/GetProjectDashboard/GetProjectDashboardQueryHandler.cs` — **ZMODYFIKOWANY**

- Zmiana typów `TrackedCostAttachment` → `BaseCostAttachment` w metodach `LoadAttachmentsAsync` i `BuildAllCostsAsync`
- Repozytorium `IReadRepository<BaseCostAttachment>` zamiast `IReadRepository<TrackedCostAttachment>`

### `src/WebApi/Extensions/ServiceCollectionExtensions.cs` — **ZMODYFIKOWANY**

- Rejestracja `IRepository<BaseCostAttachment>` / `IReadRepository<BaseCostAttachment>` zamiast `TrackedCostAttachment`

---

## Migracja EF Core

### `src/Entities/Migrations/20260506093013_redesign-costs-tph-base-class.cs` — **WYGENEROWANA**

Zakres migracji:
1. Utworzenie tabeli `Costs` (TPH) z kolumną dyskryminatora `CostType`
2. Przeniesienie danych z `ProjectCosts` → `Costs` (discriminator = `'ProjectCost'`) przez SQL `INSERT INTO ... SELECT`
3. Przeniesienie danych z `TrackedCosts` → `Costs` (discriminator = `'TrackedCost'`) przez SQL `INSERT INTO ... SELECT`
4. Utworzenie tabeli `CostAttachments`
5. Przeniesienie danych z `TrackedCostAttachments` → `CostAttachments` (mapowanie `TrackedCostId` → `CostId`) przez SQL `INSERT INTO ... SELECT`
6. Usunięcie starych tabel: `TrackedCosts`, `ProjectCosts`, `TrackedCostAttachments`

---

## Znane problemy i uwagi

### ⚠️ Potencjalny błąd w `UpdateProjectCostCommandHandler`

W metodach `ApplyFieldUpdates` i `HandleSharedUserUpdateAsync` pole `IsClosed` encji nie jest ustawiane:

```csharp
// ApplyFieldUpdates — ustawia IsAccepted ale brakuje IsClosed
projectCost.IsAccepted = request.IsClosed;  // semantycznie niepoprawne
// projectCost.IsClosed = request.IsClosed;  // brakuje tej linii
```

Wymaga weryfikacji semantyki pól `IsClosed` vs `IsAccepted` w modelu domenowym `ProjectCost`.

### ℹ️ Kontener Blob ujednolicony

Wszystkie załączniki (dawne `ProjectCosts` + `TrackedCosts`) trafiają teraz do `BlobContainerNames.CostTrackers`. Pliki historyczne w kontenerze `project-costs` nie są migrowane automatycznie — wymagana ręczna migracja danych w Azure Blob Storage dla środowisk z danymi produkcyjnymi.

### ℹ️ `LoadTrackedCostsAsync` — NIEZAIMPLEMENTOWANE rozszerzenie

Metoda `LoadTrackedCostsAsync` w `GetProjectDashboardQueryHandler` powinna dodatkowo pobierać zaakceptowane `ProjectCost` (`IsAccepted == true`) i włączać je do podsumowania finansowego dashboardu. Zmiana ta nie została jeszcze zaimplementowana.

---

## Podsumowanie zmienionych plików

| Plik | Zmiana |
|------|--------|
| `Entities/Models/Costs/BaseCost.cs` | ✅ Nowy |
| `Entities/Models/Costs/BaseCostAttachment.cs` | ✅ Nowy |
| `Entities/Models/CostTrackers/TrackedCost.cs` | ✏️ Zmodyfikowany |
| `Entities/Models/ProjectCost.cs` | ✏️ Zmodyfikowany |
| `Entities/Models/CostTrackers/TrackedCostAttachment.cs` | ❌ Usunięty |
| `Entities/Configurations/Costs/BaseCostConfiguration.cs` | ✅ Nowy |
| `Entities/Configurations/Costs/BaseCostAttachmentConfiguration.cs` | ✅ Nowy |
| `Entities/Configurations/CostTrackers/TrackedCostConfiguration.cs` | ✏️ Zmodyfikowany |
| `Entities/Configurations/ProjectCostConfiguration.cs` | ✏️ Zmodyfikowany |
| `Entities/Configurations/CostTrackers/TrackedCostAttachmentConfiguration.cs` | ❌ Usunięty |
| `Entities/Context/AppDbContext.cs` | ✏️ Zmodyfikowany |
| `Entities/Migrations/20260506093013_redesign-costs-tph-base-class.cs` | ✅ Wygenerowany |
| `Business/Interfaces/Services/ICostTrackerAttachmentService.cs` | ✏️ Zmodyfikowany |
| `Business/Implementation/Services/CostTrackerAttachmentService.cs` | ✏️ Zmodyfikowany |
| `CQRS/CostTrackers/Shared/CostTrackerHandlerBase.cs` | ✏️ Zmodyfikowany |
| `CQRS/CostTrackers/Shared/TrackedCostMutationHandlerBase.cs` | ✏️ Zmodyfikowany |
| `CQRS/CostTrackers/CreateTrackedCost/CreateTrackedCostCommandHandler.cs` | ✏️ Zmodyfikowany |
| `CQRS/CostTrackers/UpdateTrackedCost/UpdateTrackedCostCommandHandler.cs` | ✏️ Zmodyfikowany |
| `CQRS/CostTrackers/DeleteTrackedCost/DeleteTrackedCostCommandHandler.cs` | ✏️ Zmodyfikowany |
| `CQRS/ProjectCosts/Shared/ProjectCostHandlerBase.cs` | 🔄 Przepisany |
| `CQRS/ProjectCosts/CreateProjectCost/CreateProjectCostCommandHandler.cs` | 🔄 Przepisany |
| `CQRS/ProjectCosts/UpdateProjectCost/UpdateProjectCostCommandHandler.cs` | 🔄 Przepisany |
| `CQRS/ProjectCosts/DeleteProjectCost/DeleteProjectCostCommandHandler.cs` | 🔄 Przepisany |
| `CQRS/ProjectCosts/GetProjectCosts/GetProjectCostsQueryHandler.cs` | 🔄 Przepisany |
| `CQRS/ProjectDashboard/GetProjectDashboard/GetProjectDashboardQueryHandler.cs` | ✏️ Zmodyfikowany |
| `WebApi/Extensions/ServiceCollectionExtensions.cs` | ✏️ Zmodyfikowany |
