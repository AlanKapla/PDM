# Raport — H3–H6: DbSet, Decimal, DeletableEntity, DateTime

## H3 — WorkScheduleStageWorkPeriod DbSet

### Status
✅ WYKONANO

### Co zrobiono
- Dodano `public DbSet<WorkScheduleStageWorkPeriod> WorkScheduleStageWorkPeriods => Set<WorkScheduleStageWorkPeriod>();`
  obok `WorkScheduleStageWorks` i `WorkScheduleStageWorkAssignments` w `AppDbContext.cs`

### Stan konfiguracji EF (`WorkScheduleStageWorkPeriodConfiguration` w `WorkScheduleConfiguration.cs`)

| Aspekt | Status |
|--------|--------|
| GlobalQueryFilter | ❌ nie — `BaseEntity`, brak soft-delete |
| Indeks `(WorkScheduleStageWorkId, StartDate)` | ✅ |
| Indeks `(TenantId, ProjectId)` | ✅ |
| HasPrecision | ❌ nie dotyczy — brak pól `decimal` |
| HasMaxLength | ❌ nie dotyczy — brak pól `string` |

Konfiguracja kompletna, bez zmian.

> **Uwaga:** EF przy poprzedniej migracji (`add-tenant-invitation-configuration`) zapisał tabelę
> jako `WorkScheduleStageWorkPeriod` (liczba pojedyncza). Dodanie DbSet z konwencją liczby
> mnogiej spowodowało rename tabeli — migracja `fix-trackedcost-decimal-precision` zawiera
> `RenameTable` z `WorkScheduleStageWorkPeriod` na `WorkScheduleStageWorkPeriods` oraz rename
> indeksów. Jest to zamierzone i oczekiwane.

---

## H4 — TrackedCost decimal precision

### Status
✅ WYKONANO

### Konfiguracja przed zmianą

```csharp
builder.Property(tc => tc.Net)
    .HasColumnType("decimal(15,2)");

builder.Property(tc => tc.Gross)
    .HasColumnType("decimal(15,2)");
```

### Konfiguracja po zmianie

```csharp
builder.Property(tc => tc.Net)
    .HasPrecision(18, 2);

builder.Property(tc => tc.Gross)
    .HasPrecision(18, 2);
```

Pozostałe pola `decimal` w `TrackedCost` — brak. Jedynymi polami liczbowymi były `Net` i `Gross`.

### Zawartość migracji (Up / Down — fragment)

```csharp
// Up
migrationBuilder.AlterColumn<decimal>(
    name: "Net",
    table: "TrackedCosts",
    type: "decimal(18,2)",
    precision: 18,
    scale: 2,
    nullable: true,
    oldClrType: typeof(decimal),
    oldType: "decimal(15,2)",
    oldNullable: true);

migrationBuilder.AlterColumn<decimal>(
    name: "Gross",
    table: "TrackedCosts",
    type: "decimal(18,2)",
    precision: 18,
    scale: 2,
    nullable: true,
    oldClrType: typeof(decimal),
    oldType: "decimal(15,2)",
    oldNullable: true);

// Down
migrationBuilder.AlterColumn<decimal>(
    name: "Net",
    table: "TrackedCosts",
    type: "decimal(15,2)",
    nullable: true,
    oldClrType: typeof(decimal),
    oldType: "decimal(18,2)",
    oldPrecision: 18,
    oldScale: 2,
    oldNullable: true);

migrationBuilder.AlterColumn<decimal>(
    name: "Gross",
    table: "TrackedCosts",
    type: "decimal(15,2)",
    nullable: true,
    oldClrType: typeof(decimal),
    oldType: "decimal(18,2)",
    oldPrecision: 18,
    oldScale: 2,
    oldNullable: true);
```

### Ocena migracji

| Zmiana | Oczekiwana | Status |
|--------|-----------|--------|
| Net precision 15→18 | tak | ✅ |
| Gross precision 15→18 | tak | ✅ |
| Nieoczekiwane zmiany | nie | ⚠️ patrz uwaga H3 (rename tabeli) |

> **Uwaga:** EF zgrupował wszystkie pending changes H3–H6 do pierwszej migracji
> `fix-trackedcost-decimal-precision`. Kolejne dwie migracje
> (`add-message-history-isdeleted`, `fix-notification-createdat-type`) są puste.

---

## H5 — MessageHistory DeletableEntity

### Status
✅ WYKONANO

### Analiza przed zmianą

| Aspekt | Wartość |
|--------|---------|
| Obecne dziedziczenie | `BaseEntity` |
| Computed `IsDeleted` | ✅ tak — `public bool IsDeleted => DeletedAt.HasValue;` |
| Jawne `DeletedAt` | ✅ tak — `public DateTime? DeletedAt { get; set; }` |
| GlobalQueryFilter | ❌ nie |
| `builder.Ignore(m => m.IsDeleted)` | ✅ tak — było w konfiguracji |
| Handler delete | `DeleteMessageCommandHandler` — ustawiał tylko `message.DeletedAt = DateTime.UtcNow` |

### Co zrobiono
- `MessageHistory.cs`: zmieniono dziedziczenie `BaseEntity` → `DeletableEntity`
- Usunięto computed property `public bool IsDeleted => DeletedAt.HasValue`
- Usunięto jawne pole `public DateTime? DeletedAt` (dziedziczone z `DeletableEntity`)
- `MessageHistoryConfiguration.cs`: usunięto `builder.Property(m => m.DeletedAt)`
  i `builder.Ignore(m => m.IsDeleted)`, dodano `builder.HasQueryFilter(m => !m.IsDeleted)`
- `DeleteMessageCommandHandler.cs`: dodano `message.IsDeleted = true` przed `message.DeletedAt = DateTime.UtcNow`

### Handlery zaktualizowane

| Handler | Zmiana |
|---------|--------|
| `DeleteMessageCommandHandler.cs` | Dodano `message.IsDeleted = true;` |

### Zawartość migracji (Up — fragment)

```csharp
// Up
migrationBuilder.AddColumn<bool>(
    name: "IsDeleted",
    table: "MessageHistories",
    type: "bit",
    nullable: false,
    defaultValue: false);

migrationBuilder.Sql(@"
    UPDATE MessageHistories
    SET IsDeleted = 1
    WHERE DeletedAt IS NOT NULL");

// Down
migrationBuilder.DropColumn(
    name: "IsDeleted",
    table: "MessageHistories");
```

### Skrypt synchronizacji danych

```sql
UPDATE MessageHistories
SET IsDeleted = 1
WHERE DeletedAt IS NOT NULL
```

Dodano do `Up()` migracji bezpośrednio po `AddColumn<bool>`.

---

## H6 — Notification.CreatedAt DateTime

### Status
✅ WYKONANO

### Analiza przed zmianą

| Aspekt | Wartość |
|--------|---------|
| Obecny typ | `DateTimeOffset` |
| Inicjalizator w encji | `= DateTimeOffset.UtcNow` |
| Konfiguracja EF | brak jawnego mapowania — EF automatycznie jako `datetimeoffset` |
| Handlery ustawiające `CreatedAt` | 17 miejsc (patrz tabela niżej) |

### Co zrobiono
- `Notification.cs`: `DateTimeOffset CreatedAt = DateTimeOffset.UtcNow` → `DateTime CreatedAt = DateTime.UtcNow`,
  usunięto zbędny `using System;`
- `NotificationDto.cs`: `DateTimeOffset CreatedAt` → `DateTime CreatedAt`
- Wszystkie wystąpienia `CreatedAt = DateTimeOffset.UtcNow` w handlerach i serwisach
  zastąpione na `CreatedAt = DateTime.UtcNow` (masowe zastąpienie przez PowerShell)

### Handlery zaktualizowane (17 plików)

| Handler / Serwis | Liczba zmian |
|---------|--------|
| `QueuedNotificationSender.cs` | ×1 |
| `UpdateCostShareCommandHandler.cs` | ×2 |
| `WorkScheduleNotificationService.cs` | ×1 |
| `UpdateFileShareCommandHandler.cs` | ×2 |
| `ShareProjectCostsCommandHandler.cs` | ×1 |
| `ShareCostEstimateCommandHandler.cs` | ×1 |
| `UpdateCostEstimateSharesCommandHandler.cs` | ×1 |
| `CostEstimateFieldUpdateNotificationHelper.cs` | ×1 |
| `AddProjectMemberCommandHandler.cs` | ×1 |
| `RemoveProjectMemberCommandHandler.cs` | ×1 |
| `UpdateProjectMemberRoleCommandHandler.cs` | ×1 |
| `ToggleProjectStatusCommandHandler.cs` | ×1 |
| `InviteTenantMemberCommandHandler.cs` | ×1 |
| `RemoveTenantMemberCommandHandler.cs` | ×1 |
| `UpdateTenantMemberRoleCommandHandler.cs` | ×1 |
| `ToggleTenantStatusCommandHandler.cs` | ×1 |

### Zawartość migracji (Up / Down — fragment)

```csharp
// Up
migrationBuilder.AlterColumn<DateTime>(
    name: "CreatedAt",
    table: "Notifications",
    type: "datetime2",
    nullable: false,
    oldClrType: typeof(DateTimeOffset),
    oldType: "datetimeoffset");

// Down
migrationBuilder.AlterColumn<DateTimeOffset>(
    name: "CreatedAt",
    table: "Notifications",
    type: "datetimeoffset",
    nullable: false,
    oldClrType: typeof(DateTime),
    oldType: "datetime2");
```

---

## Podsumowanie końcowe

### Kompilacja po wszystkich zmianach

| Status | Liczba błędów |
|--------|--------------|
| ✅ OK | 0 |

### Wygenerowane migracje

| Migracja | Dotyczy | Zawartość |
|----------|---------|-----------|
| `20260505121002_fix-trackedcost-decimal-precision` | H3 + H4 + H5 + H6 | Wszystkie zmiany w jednej migracji (EF zgrupował pending changes) |
| `20260505121025_add-message-history-isdeleted` | — | Pusta |
| `20260505121046_fix-notification-createdat-type` | — | Pusta |

> Dwie puste migracje można usunąć (`ef migrations remove` ×2) lub zostawić jako ślad intencji.

### Zmodyfikowane pliki

| Plik | Zmiana |
|------|--------|
| `src\Entities\Context\AppDbContext.cs` | Dodano `DbSet<WorkScheduleStageWorkPeriod>` (H3) |
| `src\Entities\Configurations\CostTrackers\TrackedCostConfiguration.cs` | `HasColumnType("decimal(15,2)")` → `HasPrecision(18, 2)` dla Net i Gross (H4) |
| `src\Entities\Models\MessageHistory.cs` | `BaseEntity` → `DeletableEntity`, usunięto `IsDeleted` computed, `DeletedAt` (H5) |
| `src\Entities\Configurations\MessageHistoryConfiguration.cs` | Usunięto `Ignore(IsDeleted)`, dodano `HasQueryFilter(m => !m.IsDeleted)` (H5) |
| `src\Chat\CQRS\Messages\DeleteMessage\DeleteMessageCommandHandler.cs` | Dodano `message.IsDeleted = true` (H5) |
| `src\Entities\Models\Notification.cs` | `DateTimeOffset` → `DateTime` dla `CreatedAt` (H6) |
| `src\Business\Interfaces\DTO\NotificationDto.cs` | `DateTimeOffset` → `DateTime` dla `CreatedAt` (H6) |
| `src\Business\Implementation\Services\QueuedNotificationSender.cs` | `DateTimeOffset.UtcNow` → `DateTime.UtcNow` (H6) |
| 13× handlery/serwisy ustawiające `NotificationDto.CreatedAt` | `DateTimeOffset.UtcNow` → `DateTime.UtcNow` (H6) |
| `src\Entities\Migrations\20260505121002_fix-trackedcost-decimal-precision.cs` | Nowa migracja (H3+H4+H5+H6) ze skryptem sync `IsDeleted` |
| `src\Entities\Migrations\20260505121025_add-message-history-isdeleted.cs` | Nowa pusta migracja |
| `src\Entities\Migrations\20260505121046_fix-notification-createdat-type.cs` | Nowa pusta migracja |

### Blokery przed deploymentem

| # | Opis | Akcja |
|---|------|-------|
| 1 | `WorkScheduleStageWorkPeriods` — rename tabeli (PK i FK) | Automatyczne przez migrację ✅ |
| 2 | `MessageHistory.IsDeleted` — sync dla rekordów z `DeletedAt != null` | Skrypt SQL dodany do `Up()` migracji ✅ |
| 3 | `Notification.CreatedAt` — zmiana `datetimeoffset` → `datetime2` | Automatyczna konwersja przez SQL Server (UTC zachowane) ✅ |

### Następny krok
- Opcjonalnie: usunięcie dwóch pustych migracji (`ef migrations remove` ×2)
- H1, H2, H7 lub redesign ProjectCost / TrackedCost
