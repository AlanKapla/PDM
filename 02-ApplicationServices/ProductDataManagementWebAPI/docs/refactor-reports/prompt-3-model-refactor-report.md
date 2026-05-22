# Raport — Prompt 3: Refaktor modelu danych

**Branch:** `AK/cost-estimate-refactor`  
**Data:** 2025  
**Zakres:** Wyłącznie model encji, konfiguracje EF Core, AppDbContext  

---

## Status ogólny

| Krok | Nazwa | Status |
|------|-------|--------|
| 1 | DeletableEntity — klasa bazowa | ✅ WYKONANO |
| 2 | DeletableEntity na WorkScheduleStageWork | ✅ WYKONANO |
| 3 | GlobalQueryFilter na brakujących encjach | ✅ WYKONANO |
| 4 | Bezpośrednie FK na encjach harmonogramu | ✅ WYKONANO |
| 5 | Usunięcie WorkItemLinkId z TrackedCost | ✅ WYKONANO |
| 6 | Usunięcie encji łącznikowych | 🔴 BLOKER |
| 7 | IWorkItemLinkService — inwentaryzacja | ✅ WYKONANO (tylko inwentaryzacja) |
| 8 | SaveChangesAsync w DbContext | ✅ WYKONANO |

---

## KROK 1 — DeletableEntity

### Status: ✅ WYKONANO

### Co zrobiono
- Utworzono `DeletableEntity.cs` jako `abstract class DeletableEntity : BaseEntity` z polami `IsDeleted` i `DeletedAt`
- Usunięto pusty plik `IDeletable.cs` (artefakt przerwanej sesji Prompt 2)
- Zmieniono dziedziczenie z `BaseEntity` na `DeletableEntity` dla 7 encji
- Usunięto jawne pola `IsDeleted` / `DeletedAt` z ciał wszystkich 7 encji (dziedziczone)

### Encje zmigrowane

| Encja | Plik | IsDeleted przed | IsDeleted po |
|-------|------|-----------------|--------------|
| `CostEstimate` | `CostEstimates/CostEstimate.cs` | jawne pole | dziedziczone |
| `CostEstimateGroup` | `CostEstimates/CostEstimateGroup.cs` | jawne pole | dziedziczone |
| `CostEstimateItem` | `CostEstimates/CostEstimateItem.cs` | jawne pole | dziedziczone |
| `WorkSchedule` | `WorkSchedule.cs` | jawne pole | dziedziczone |
| `WorkScheduleStage` | `WorkScheduleStage.cs` | jawne pole | dziedziczone |
| `TrackedCost` | `CostTrackers/TrackedCost.cs` | jawne pole | dziedziczone |
| `TrackedCostAttachment` | `CostTrackers/TrackedCostAttachment.cs` | jawne pole | dziedziczone |

### Pliki zmodyfikowane
- `src\Entities\Models\Base\DeletableEntity.cs` *(NOWY)*
- `src\Entities\Models\CostEstimates\CostEstimate.cs`
- `src\Entities\Models\CostEstimates\CostEstimateGroup.cs`
- `src\Entities\Models\CostEstimates\CostEstimateItem.cs`
- `src\Entities\Models\WorkSchedule.cs`
- `src\Entities\Models\WorkScheduleStage.cs`
- `src\Entities\Models\CostTrackers\TrackedCost.cs`
- `src\Entities\Models\CostTrackers\TrackedCostAttachment.cs`

### Pliki usunięte
- `src\Entities\Models\Base\IDeletable.cs`

---

## KROK 2 — DeletableEntity na WorkScheduleStageWork

### Status: ✅ WYKONANO

### Co zrobiono
- Zmieniono dziedziczenie `WorkScheduleStageWork : BaseEntity` → `WorkScheduleStageWork : DeletableEntity`
- Usunięto nawigację `ICollection<CostEstimateItemWorkScheduleStageWorkLink> WorkItemLinks` z encji
- Usunięto `using Entities.Models.WorkItemLinks` z pliku encji

W `WorkScheduleStageWorkConfiguration` dodano:
```csharp
builder.Property(w => w.IsDeleted).IsRequired().HasDefaultValue(false);
builder.Property(w => w.DeletedAt);
builder.HasQueryFilter(w => !w.IsDeleted);
builder.HasIndex(w => new { w.WorkScheduleStageId, w.IsDeleted });
builder.HasIndex(w => new { w.TenantId, w.ProjectId, w.IsDeleted });
```

### Pliki zmodyfikowane
- `src\Entities\Models\WorkScheduleStageWork.cs`
- `src\Entities\Configurations\WorkScheduleConfiguration.cs`

---

## KROK 3 — GlobalQueryFilter na brakujących encjach

### Status: ✅ WYKONANO

| Encja | Konfiguracja | Stan przed | Stan po |
|-------|-------------|------------|---------|
| `WorkSchedule` | `WorkScheduleConfiguration` | brak QueryFilter | `HasQueryFilter(w => !w.IsDeleted)` ✅ |
| `WorkScheduleStage` | `WorkScheduleStageConfiguration` | brak QueryFilter | `HasQueryFilter(s => !s.IsDeleted)` ✅ |
| `TrackedCostAttachment` | `TrackedCostAttachmentConfiguration` | już istniał | bez zmian ✅ |

### Pliki zmodyfikowane
- `src\Entities\Configurations\WorkScheduleConfiguration.cs`

---

## KROK 4 — Bezpośrednie FK na encjach harmonogramu

### Status: ✅ WYKONANO

### 4.1 — WorkSchedule

Dodano:
```csharp
public Guid? CostEstimateId { get; set; }
public virtual CostEstimate? CostEstimate { get; set; }
```
Usunięto:
```csharp
public virtual ICollection<CostEstimateWorkScheduleLink> CostEstimateLinks { get; set; }
```

### 4.2 — WorkScheduleStage

Dodano:
```csharp
public Guid? CostEstimateGroupId { get; set; }
public virtual CostEstimateGroup? CostEstimateGroup { get; set; }
```
Usunięto:
```csharp
public virtual ICollection<CostEstimateGroupWorkScheduleStageLink> CostEstimateGroupLinks { get; set; }
```

### 4.3 — CostEstimate

Dodano:
```csharp
public virtual ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();
```
Usunięto:
```csharp
public virtual ICollection<CostEstimateWorkScheduleLink> WorkScheduleLinks { get; set; }
```

### 4.4 — CostEstimateGroup

Dodano:
```csharp
public virtual ICollection<WorkScheduleStage> WorkScheduleStages { get; set; } = new List<WorkScheduleStage>();
```
Usunięto:
```csharp
public virtual ICollection<CostEstimateGroupWorkScheduleStageLink> WorkScheduleStageLinks { get; set; }
```

### 4.5 — CostEstimateItem

Usunięto:
```csharp
public virtual ICollection<CostEstimateItemWorkScheduleStageWorkLink> WorkItemLinks { get; set; }
```

### 4.6 — WorkScheduleConfiguration — nowe relacje

```csharp
// WorkSchedule ↔ CostEstimate
builder.HasOne(w => w.CostEstimate)
       .WithMany(c => c.WorkSchedules)
       .HasForeignKey(w => w.CostEstimateId)
       .OnDelete(DeleteBehavior.SetNull)
       .IsRequired(false);
builder.HasIndex(w => w.CostEstimateId);

// WorkScheduleStage ↔ CostEstimateGroup
builder.HasOne(s => s.CostEstimateGroup)
       .WithMany(g => g.WorkScheduleStages)
       .HasForeignKey(s => s.CostEstimateGroupId)
       .OnDelete(DeleteBehavior.SetNull)
       .IsRequired(false);
builder.HasIndex(s => s.CostEstimateGroupId);
```

### 4.7 — WorkScheduleStageWork ↔ CostEstimateItem

Istniejąca relacja już miała poprawną konfigurację:
```csharp
.OnDelete(DeleteBehavior.SetNull)
.IsRequired(false)
```
→ **OK, bez zmian.**

### Naprawione uszkodzone konfiguracje łącznikowe

Po usunięciu nawigacji z encji, 3 pliki konfiguracyjne łączników miały błędy kompilacji (`.WithMany(nav)` na nieistniejące właściwości). Naprawiono przez zastąpienie `.WithMany()` bez wyrażenia lambda:

| Plik | Naprawione wywołanie |
|------|---------------------|
| `CostEstimateWorkScheduleLinkConfiguration.cs` | `.WithMany(c => c.WorkScheduleLinks)` → `.WithMany()` |
| `CostEstimateWorkScheduleLinkConfiguration.cs` | `.WithMany(w => w.CostEstimateLinks)` → `.WithMany()` |
| `CostEstimateGroupWorkScheduleStageLinkConfiguration.cs` | `.WithMany(g => g.WorkScheduleStageLinks)` → `.WithMany()` |
| `CostEstimateGroupWorkScheduleStageLinkConfiguration.cs` | `.WithMany(s => s.CostEstimateGroupLinks)` → `.WithMany()` |
| `CostEstimateItemWorkScheduleStageWorkLinkConfiguration.cs` | `.WithMany(i => i.WorkItemLinks)` → `.WithMany()` |
| `CostEstimateItemWorkScheduleStageWorkLinkConfiguration.cs` | `.WithMany(w => w.WorkItemLinks)` → `.WithMany()` |
| `CostEstimateItemWorkScheduleStageWorkLinkConfiguration.cs` | usunięto `HasMany(TrackedCosts)` (FK `WorkItemLinkId` usunięty) |
| `CostEstimateItemWorkScheduleStageWorkLinkConfiguration.cs` | usunięto `builder.Ignore(ActualNet/ActualGross/Variance)` (computed props usunięte) |

### Pliki zmodyfikowane
- `src\Entities\Models\WorkSchedule.cs`
- `src\Entities\Models\WorkScheduleStage.cs`
- `src\Entities\Models\CostEstimates\CostEstimate.cs`
- `src\Entities\Models\CostEstimates\CostEstimateGroup.cs`
- `src\Entities\Models\CostEstimates\CostEstimateItem.cs`
- `src\Entities\Models\WorkItemLinks\CostEstimateItemWorkScheduleStageWorkLink.cs`
- `src\Entities\Configurations\WorkScheduleConfiguration.cs`
- `src\Entities\Configurations\WorkItemLinks\CostEstimateWorkScheduleLinkConfiguration.cs`
- `src\Entities\Configurations\WorkItemLinks\CostEstimateGroupWorkScheduleStageLinkConfiguration.cs`
- `src\Entities\Configurations\WorkItemLinks\CostEstimateItemWorkScheduleStageWorkLinkConfiguration.cs`

---

## KROK 5 — Usunięcie WorkItemLinkId z TrackedCost

### Status: ✅ WYKONANO

### Encja TrackedCost — zmiany

Usunięto:
```csharp
public Guid? WorkItemLinkId { get; set; }
public virtual CostEstimateItemWorkScheduleStageWorkLink? CostEstimateItemWorkScheduleStageWorkLink { get; set; }
public void ValidateLinkExclusivity() { ... }
```

Pozostawiono bez zmian:
```csharp
public Guid? CostEstimateItemId { get; set; }   // koszt przy pozycji kosztorysu
public Guid? WorkScheduleStageWorkId { get; set; } // koszt przy zakresie pracy
public virtual CostEstimateItem? CostEstimateItem { get; set; }
public virtual WorkScheduleStageWork? WorkScheduleStageWork { get; set; }
```

**Nowa semantyka TrackedCost:**

| `CostEstimateItemId` | `WorkScheduleStageWorkId` | Znaczenie |
|:--------------------:|:-------------------------:|-----------|
| `null` | `null` | Koszt dodatkowy projektu |
| wypełnione | `null` | Koszt przy pozycji kosztorysu |
| `null` | wypełnione | Koszt przy zakresie pracy |
| wypełnione | wypełnione | Koszt wspólny (powiązany z oboma) |

### Konfiguracja TrackedCostConfiguration — zmiany

Usunięto:
```csharp
builder.Property(tc => tc.WorkItemLinkId);
builder.HasIndex(tc => tc.WorkItemLinkId);
```

Pozostawiono (poprawne, bez zmian):
```csharp
builder.HasOne(tc => tc.CostEstimateItem)
    .WithMany()
    .HasForeignKey(tc => tc.CostEstimateItemId)
    .OnDelete(DeleteBehavior.SetNull);

builder.HasOne(tc => tc.WorkScheduleStageWork)
    .WithMany()
    .HasForeignKey(tc => tc.WorkScheduleStageWorkId)
    .OnDelete(DeleteBehavior.SetNull);

builder.HasIndex(tc => tc.CostEstimateItemId);
builder.HasIndex(tc => tc.WorkScheduleStageWorkId);
builder.HasQueryFilter(tc => !tc.IsDeleted);
```

### Pliki zmodyfikowane
- `src\Entities\Models\CostTrackers\TrackedCost.cs`
- `src\Entities\Configurations\CostTrackers\TrackedCostConfiguration.cs`

---

## KROK 6 — Usunięcie encji łącznikowych

### Status: 🔴 BLOKER

Pliki encji i konfiguracji **NIE zostały usunięte**. Poniżej pełna inwentaryzacja użyć wymaganych do przepisania w Prompt 4.

### Użycia CostEstimateWorkScheduleLink

| Plik | Linia | Kontekst |
|------|-------|---------|
| `WorkScheduleSyncService.cs` | 47 | `GetWorkScheduleLinkAsync` — pobiera link po `workScheduleId` |
| `SyncWorkScheduleWithEstimateCommandHandler.cs` | 44 | `IRepository<CostEstimateWorkScheduleLink>` bezpośrednio przez repo |
| `GetWorkSchedulesQueryHandler.cs` | 20 | DI: `IReadRepository<CostEstimateWorkScheduleLink>` |
| `GetProjectDashboardQueryHandler.cs` | 333 | ładuje `wsLinks` przez repo do budowania dashboardu |
| `WorkScheduleBuilder.cs` | 19, 64 | DI repo + `GetFirstBySearch` |

### Użycia CostEstimateGroupWorkScheduleStageLink

| Plik | Linia | Kontekst |
|------|-------|---------|
| `WorkScheduleSyncService.cs` | 68, 97, 102 | `GetGroupStageLinksForWorkScheduleLinkAsync` + sygnatury metod prywatnych |
| `WorkScheduleBuilder.cs` | 20, 32 | DI `IReadRepository<...>` + użycie |

### Użycia CostEstimateItemWorkScheduleStageWorkLink

| Plik | Linia | Kontekst |
|------|-------|---------|
| `CostTrackerHandlerBase.cs` | 109, 329, 363, 380, 475, 480, 501 | Wielokrotne użycia w metodach mapujących web modele |
| `TrackedCostMutationHandlerBase.cs` | 13, 19, 34, 38 | DI `IReadRepository<...>` + walidacja istnienia |
| `GetProjectDashboardQueryHandler.cs` | 162, 197, 304–509 | Centralna logika dashboardu kosztowego |
| `UpdateTrackedCostCommandHandler.cs` | 22 | DI repo |

### Pliki do usunięcia w Prompt 4 (po przepisaniu handlerów)

```
src\Entities\Models\WorkItemLinks\CostEstimateWorkScheduleLink.cs
src\Entities\Models\WorkItemLinks\CostEstimateGroupWorkScheduleStageLink.cs
src\Entities\Models\WorkItemLinks\CostEstimateItemWorkScheduleStageWorkLink.cs
src\Entities\Configurations\WorkItemLinks\CostEstimateWorkScheduleLinkConfiguration.cs
src\Entities\Configurations\WorkItemLinks\CostEstimateGroupWorkScheduleStageLinkConfiguration.cs
src\Entities\Configurations\WorkItemLinks\CostEstimateItemWorkScheduleStageWorkLinkConfiguration.cs
```

DbSety do usunięcia z `AppDbContext`:
```csharp
public DbSet<CostEstimateWorkScheduleLink> CostEstimateWorkScheduleLinks
public DbSet<CostEstimateGroupWorkScheduleStageLink> CostEstimateGroupWorkScheduleStageLinks
public DbSet<CostEstimateItemWorkScheduleStageWorkLink> CostEstimateItemWorkScheduleStageWorkLinks
```

---

## KROK 7 — IWorkItemLinkService — inwentaryzacja

### Status: ✅ WYKONANO (tylko inwentaryzacja — serwis nie został usunięty)

### Interfejs IWorkItemLinkService — metody

```csharp
// Odczyt
Task<CostEstimateWorkScheduleLink?> GetWorkScheduleLinkAsync(Guid workScheduleId, CancellationToken ct)
Task<IReadOnlyList<CostEstimateGroupWorkScheduleStageLink>> GetGroupStageLinksForWorkScheduleLinkAsync(Guid workScheduleLinkId, CancellationToken ct)

// Tworzenie
Task<CostEstimateWorkScheduleLink> CreateWorkScheduleLinkAsync(Guid workScheduleId, Guid? costEstimateId, CancellationToken ct)
Task CreateGroupStageLinkForScheduleStageAsync(Guid workScheduleId, Guid stageId, Guid? costEstimateGroupId, CancellationToken ct)

// Usuwanie item links
Task DeleteWorkItemLinkForWorkAsync(Guid workScheduleStageWorkId, CancellationToken ct)
Task DeleteWorkItemLinksForWorksAsync(IReadOnlyCollection<Guid> workIds, CancellationToken ct)
Task DeleteWorkItemLinksForItemsAsync(IReadOnlyCollection<Guid> costEstimateItemIds, CancellationToken ct)

// Usuwanie group-stage links
Task DeleteGroupStageLinksForStagesAsync(IReadOnlyCollection<Guid> stageIds, CancellationToken ct)
Task DeleteGroupStageLinksForGroupsAsync(IReadOnlyCollection<Guid> costEstimateGroupIds, CancellationToken ct)

// Usuwanie wszystkich łączników
Task DeleteAllLinksForScheduleAsync(Guid workScheduleId, CancellationToken ct)
Task DeleteAllLinksForEstimateAsync(Guid costEstimateId, CancellationToken ct)

// Synchronizacja
Task SyncWorkItemLinkAsync(Guid? workItemLinkId, Guid? costEstimateItemId, Guid? workScheduleStageWorkId, CancellationToken ct)
Task UpsertWorkItemLinkAsync(Guid projectId, Guid groupStageLinkId, Guid costEstimateItemId, Guid workScheduleStageWorkId, string displayName, decimal? budgetNet, decimal? budgetGross, int order, CancellationToken ct)
Task SyncPlannedDatesForStageWorkAsync(Guid workScheduleStageWorkId, DateTime? plannedStart, ...)
```

### Użycia IWorkItemLinkService w handlerach/serwisach

| Plik | Metoda serwisu | Kontekst |
|------|---------------|---------|
| `WorkScheduleSyncService.cs` | `GetWorkScheduleLinkAsync` | Sprawdza czy WS jest powiązany z estimate |
| `WorkScheduleSyncService.cs` | `GetGroupStageLinksForWorkScheduleLinkAsync` | 2× — przed i po sync etapów |
| `WorkScheduleSyncService.cs` | `DeleteGroupStageLinksForStagesAsync` | Soft-delete etapu → kaskadowe usunięcie linków |
| `WorkScheduleSyncService.cs` | `CreateGroupStageLinkForScheduleStageAsync` | Tworzy link group↔stage |
| `WorkScheduleSyncService.cs` | `UpsertWorkItemLinkAsync` | Tworzy/aktualizuje link item↔work |
| `WorkScheduleSyncService.cs` | `DeleteWorkItemLinksForWorksAsync` | Bulk-delete item links |
| `CreateWorkScheduleCommandHandler.cs` | `CreateWorkScheduleLinkAsync` | Tworzy link WS↔estimate przy tworzeniu WS |
| `DeleteWorkScheduleCommandHandler.cs` | `DeleteAllLinksForScheduleAsync` | Usuwa wszystkie linki przy usuwaniu WS |
| `DeleteWorkScheduleStageCommandHandler.cs` | `DeleteWorkItemLinksForWorksAsync` + `DeleteGroupStageLinksForStagesAsync` | Kaskadowe usunięcie przy usuwaniu etapu |
| `DeleteWorkScheduleStageWorkCommandHandler.cs` | `DeleteWorkItemLinkForWorkAsync` | Usuwa link przy usuwaniu zakresu pracy |
| `SetWorkScheduleStageWorkIsClosedCommandHandler.cs` | `SyncPlannedDatesForStageWorkAsync` | Sync dat planowanych |
| `SetWorkScheduleStageWorkPeriodIsClosedCommandHandler.cs` | `SyncPlannedDatesForStageWorkAsync` | Sync dat planowanych |
| `SetWorkScheduleStageWorkPeriodsCommandHandler.cs` | `SyncPlannedDatesForStageWorkAsync` | Sync dat planowanych |

**Uwaga:** `SyncPlannedDatesForStageWorkAsync` synchronizuje daty planowane do link entity — w nowej architekturze nie ma już link entity; docelowo synchronizacja idzie bezpośrednio na `WorkScheduleStageWork`. Metoda zostaje jako **kandydat do refaktoru** w Prompt 4, ale dotyczy dat — nie łącznika jako takiego.

---

## KROK 8 — SaveChangesAsync w AppDbContext

### Status: ✅ WYKONANO

Dodano override `SaveChangesAsync` do `AppDbContext`:

```csharp
public override async Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
{
    DateTime now = DateTime.UtcNow;

    foreach (EntityEntry<DeletableEntity> entry in ChangeTracker.Entries<DeletableEntity>())
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.IsDeleted = false;
            entry.Entity.DeletedAt = null;
        }
    }

    foreach (EntityEntry entry in ChangeTracker.Entries())
    {
        if (entry.State == EntityState.Added
            && entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
        {
            entry.Property("CreatedAt").CurrentValue = now;
        }

        if (entry.State == EntityState.Modified
            && entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
        {
            entry.Property("UpdatedAt").CurrentValue = now;
        }
    }

    return await base.SaveChangesAsync(cancellationToken);
}
```

Dodano `using Microsoft.EntityFrameworkCore.ChangeTracking` dla `EntityEntry<T>`.

### Pliki zmodyfikowane
- `src\Entities\Context\AppDbContext.cs`

---

## Pełna lista zmodyfikowanych plików

### Nowe pliki
```
src\Entities\Models\Base\DeletableEntity.cs
```

### Zmodyfikowane pliki

**Encje:**
```
src\Entities\Models\CostEstimates\CostEstimate.cs
src\Entities\Models\CostEstimates\CostEstimateGroup.cs
src\Entities\Models\CostEstimates\CostEstimateItem.cs
src\Entities\Models\WorkSchedule.cs
src\Entities\Models\WorkScheduleStage.cs
src\Entities\Models\WorkScheduleStageWork.cs
src\Entities\Models\CostTrackers\TrackedCost.cs
src\Entities\Models\CostTrackers\TrackedCostAttachment.cs
src\Entities\Models\WorkItemLinks\CostEstimateItemWorkScheduleStageWorkLink.cs
```

**Konfiguracje EF:**
```
src\Entities\Configurations\WorkScheduleConfiguration.cs
src\Entities\Configurations\CostTrackers\TrackedCostConfiguration.cs
src\Entities\Configurations\WorkItemLinks\CostEstimateWorkScheduleLinkConfiguration.cs
src\Entities\Configurations\WorkItemLinks\CostEstimateGroupWorkScheduleStageLinkConfiguration.cs
src\Entities\Configurations\WorkItemLinks\CostEstimateItemWorkScheduleStageWorkLinkConfiguration.cs
```

**DbContext:**
```
src\Entities\Context\AppDbContext.cs
```

### Usunięte pliki
```
src\Entities\Models\Base\IDeletable.cs
```

---

## Blokery dla Prompt 4

### 🔴 BLOKER A — Handlery używające WorkItemLinkId (KROK 5)

Wynikające z usunięcia `TrackedCost.WorkItemLinkId`:

| Plik | Liczba błędów | Opis |
|------|:---:|------|
| `CostTrackerHandlerBase.cs` | 5 | `WorkItemLinkId`, `CostEstimateItemWorkScheduleStageWorkLink` nav |
| `TrackedCostMutationHandlerBase.cs` | 4 | `WorkItemLinkId`, `CostEstimateItemWorkScheduleStageWorkLink` nav |
| `CreateTrackedCostCommandHandler.cs` | 2 | `WorkItemLinkId`, `ValidateLinkExclusivity()` |
| `GetProjectDashboardQueryHandler.cs` | 9 | `WorkItemLinkId` w wielu miejscach |
| `WorkItemLinkService.cs` | 2 | `WorkItemLinkId` w predykacie LINQ |

### 🔴 BLOKER B — Handlery/serwisy używające 3 typów encji łącznikowych (KROK 6)

Wymagają pełnego przepisania przed usunięciem encji łącznikowych:

- **`WorkScheduleSyncService.cs`** — centralny serwis synchronizacji; wymaga przepisania z użyciem nowych FK (`WorkSchedule.CostEstimateId`, `WorkScheduleStage.CostEstimateGroupId`)
- **`GetProjectDashboardQueryHandler.cs`** — centralna logika dashboardu kosztowego; wymaga nowego sposobu łączenia kosztów z pozycjami
- **`CostTrackerHandlerBase.cs`** + **`TrackedCostMutationHandlerBase.cs`** — nowe mapowanie web modeli TrackedCost
- **`SyncWorkScheduleWithEstimateCommandHandler.cs`** — wymaga nowej logiki sync przez FK zamiast link entity
- **`GetWorkSchedulesQueryHandler.cs`** — pobiera informację o powiązanym estimate przez link entity
- **`WorkScheduleBuilder.cs`** — buduje DTO harmonogramu z uwzględnieniem linków

### 🟡 BLOKER C — Walidator (nie handler)

- **`CreateWorkScheduleCommandValidator.cs` linia 36:** używa `ws.CostEstimateLinks.Any(l => l.CostEstimateId == id)` — po zmianie modelu walidacja powinna sprawdzać `ws.CostEstimateId == id` bezpośrednio

---

## Stan kompilacji

### ❌ Nie kompiluje się

**24 błędy CS1061/CS0117** — wyłącznie w warstwie CQRS/Business, **nie** w modelu/konfiguracji:

| Typ błędu | Liczba | Przyczyna |
|-----------|:------:|-----------|
| `TrackedCost` brak `WorkItemLinkId` | 13 | KROK 5 — oczekiwane |
| `TrackedCost` brak `CostEstimateItemWorkScheduleStageWorkLink` | 5 | KROK 5 — oczekiwane |
| `TrackedCost` brak `ValidateLinkExclusivity` | 1 | KROK 5 — oczekiwane |
| `WorkSchedule` brak `CostEstimateLinks` | 1 | KROK 4 — oczekiwane |
| `CS0165` unassigned variable `link` | 1 | konsekwencja usunięcia `WorkItemLinkId` |

**✅ Model, konfiguracje EF i AppDbContext — kompilują się poprawnie.**  
Wszystkie błędy kompilacji są w handlerach/serwisach — zakres Prompt 4.

---

## Diagram hierarchii dziedziczenia po zmianach

```
BaseEntity
│   └── Id: Guid
│
└── DeletableEntity  (NOWA klasa bazowa)
        ├── IsDeleted: bool
        ├── DeletedAt: DateTime?
        │
        ├── CostEstimate
        ├── CostEstimateGroup
        ├── CostEstimateItem
        ├── WorkSchedule         ← + CostEstimateId? (nowe FK)
        ├── WorkScheduleStage    ← + CostEstimateGroupId? (nowe FK)
        ├── WorkScheduleStageWork ← NOWE (było: BaseEntity)
        ├── TrackedCost          ← - WorkItemLinkId (usunięte)
        └── TrackedCostAttachment
```

---

## Docelowa architektura powiązań (po Prompt 4)

```
CostEstimate ──────────────────────────────┐ 1:N
                                           ▼
WorkSchedule ← CostEstimateId?     WorkSchedule
                                           │ 1:N
CostEstimateGroup ─────────────────────────┐
                                           ▼
WorkScheduleStage ← CostEstimateGroupId?  WorkScheduleStage
                                           │ 1:N
CostEstimateItem ──────────────────────────┐
                                           ▼
WorkScheduleStageWork ← CostEstimateItemId? WorkScheduleStageWork
                                           │ 1:N
TrackedCost ← WorkScheduleStageWorkId?    TrackedCost
TrackedCost ← CostEstimateItemId?
```

**Encje łącznikowe do usunięcia w Prompt 4:**GetProjectDashboardQueryHandlerGetProjectDashboardQueryHandler
- ~~`CostEstimateWorkScheduleLink`~~
- ~~`CostEstimateGroupWorkScheduleStageLink`~~
- ~~`CostEstimateItemWorkScheduleStageWorkLink`~~
