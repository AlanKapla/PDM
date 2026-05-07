# Raport refaktoryzacji N10, N3, N7 — TenantId/ProjectId w wersjach plików i załącznikach

## N10 — Komentarz XML na CostEstimateFieldFile.CostEstimateId

### Status: WYKONANO

### Co zrobiono
- Zastąpiono istniejący, skrócony komentarz XML na właściwości `CostEstimateId` pełnym opisem denormalizacji zgodnym z wymaganiem

### Pliki zmodyfikowane
- `src/Entities/Models/CostEstimates/CostEstimateFieldFile.cs`

### Nieoczekiwane rzeczy
Brak.

### Wymaga decyzji
Nie.

## N3 — Dodanie TenantId i ProjectId do ProjectFileVersion

### Status: WYKONANO

### Audyt encji przed zmianą

Encja `ProjectFileVersion` zawierała następujące pola:
- `ProjectFileId` (Guid)
- `VersionNumber` (int)
- `CreatedByUserId` (Guid)
- `BlobFileName` (string)
- `BlobPath` (string)
- `ContentType` (string)
- `FileSizeBytes` (long)
- `CreatedAt` (DateTime)
- `IsDeleted` (bool, z DeletableEntity)

Nawigacje:
- `ProjectFile` (→ ProjectFile)
- `CreatedByUser` (→ User)
- `Comments` (ICollection<ProjectFileVersionComment>)

### Co zrobiono
- Dodano `public Guid TenantId { get; set; }` i `public Guid ProjectId { get; set; }` do encji
- W konfiguracji `ProjectFileVersionConfiguration` dodano `.IsRequired()` dla obu pól oraz indeks `{ TenantId, ProjectId }`
- W `UploadProjectFilesCommandHandler` uzupełniono inicjalizator `ProjectFileVersion` o `TenantId = request.TenantId, ProjectId = request.ProjectId`
- W `UploadProjectFileVersionCommandHandler` uzupełniono inicjalizator `ProjectFileVersion` o `TenantId = request.TenantId, ProjectId = request.ProjectId`

### Pliki zmodyfikowane
- `src/Entities/Models/ProjectFileVersion.cs`
- `src/Entities/Configurations/ProjectFileVersionConfiguration.cs`
- `src/CQRS/Files/UploadProjectFiles/UploadProjectFilesCommandHandler.cs`
- `src/CQRS/Files/UploadProjectFileVersion/UploadProjectFileVersionCommandHandler.cs`

### Nieoczekiwane rzeczy
Brak. Oba handlery miały pełny dostęp do `request.TenantId` i `request.ProjectId`.

### Wymaga decyzji
Nie.

## N7 — Dodanie TenantId i ProjectId do TrackedCostAttachment

### Status: WYKONANO

### Audyt encji przed zmianą

Encja `TrackedCostAttachment` zawierała następujące pola:
- `TrackedCostId` (Guid)
- `OriginalFileName` (string)
- `BlobName` (string)
- `ContentType` (string)
- `FileSize` (long)
- `CreatedAt` (DateTime)
- `IsDeleted` (bool, z DeletableEntity)
- `DeletedAt` (DateTime?, z DeletableEntity)

Nawigacje:
- `TrackedCost` (→ TrackedCost)

### Co zrobiono
- Dodano `public Guid TenantId { get; set; }` i `public Guid ProjectId { get; set; }` do encji
- W konfiguracji `TrackedCostAttachmentConfiguration` dodano `.IsRequired()` dla obu pól oraz indeks `{ TenantId, ProjectId }`
- W `CostTrackerAttachmentService.SyncAttachmentsAsync` uzupełniono inicjalizator `TrackedCostAttachment` o `TenantId = tenantId, ProjectId = projectId`
  — parametry `tenantId` i `projectId` były już przekazywane do metody (sygnatura: `SyncAttachmentsAsync(TrackedCost cost, ..., Guid tenantId, Guid projectId, ...)`)

### Pliki zmodyfikowane
- `src/Entities/Models/CostTrackers/TrackedCostAttachment.cs`
- `src/Entities/Configurations/CostTrackers/TrackedCostAttachmentConfiguration.cs`
- `src/Business/Implementation/Services/CostTrackerAttachmentService.cs`

### Nieoczekiwane rzeczy
Brak. `SyncAttachmentsAsync` przyjmuje `tenantId` i `projectId` jawnie — wartości były dostępne bezpośrednio w miejscu tworzenia encji.

### Wymaga decyzji
Nie.

## Migracja zbiorcza

### Nazwa pliku
`20260506082551_N3-N7-N10-tenantid-projectid-in-versions-and-attachments.cs`

### Zawartość Up()

```csharp
migrationBuilder.AddColumn<Guid>(
    name: "ProjectId",
    table: "TrackedCostAttachments",
    type: "uniqueidentifier",
    nullable: false,
    defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

migrationBuilder.AddColumn<Guid>(
    name: "TenantId",
    table: "TrackedCostAttachments",
    type: "uniqueidentifier",
    nullable: false,
    defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

migrationBuilder.AddColumn<Guid>(
    name: "ProjectId",
    table: "ProjectFileVersions",
    type: "uniqueidentifier",
    nullable: false,
    defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

migrationBuilder.AddColumn<Guid>(
    name: "TenantId",
    table: "ProjectFileVersions",
    type: "uniqueidentifier",
    nullable: false,
    defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

migrationBuilder.CreateIndex(
    name: "IX_TrackedCostAttachments_TenantId_ProjectId",
    table: "TrackedCostAttachments",
    columns: new[] { "TenantId", "ProjectId" });

migrationBuilder.CreateIndex(
    name: "IX_ProjectFileVersions_TenantId_ProjectId",
    table: "ProjectFileVersions",
    columns: new[] { "TenantId", "ProjectId" });

migrationBuilder.Sql(@"
    UPDATE pv
    SET pv.TenantId = pf.TenantId, pv.ProjectId = pf.ProjectId
    FROM ProjectFileVersions pv
    JOIN ProjectFiles pf ON pv.ProjectFileId = pf.Id
");

migrationBuilder.Sql(@"
    UPDATE ta
    SET ta.TenantId = tc.TenantId, ta.ProjectId = tc.ProjectId
    FROM TrackedCostAttachments ta
    JOIN TrackedCosts tc ON ta.TrackedCostId = tc.Id
");
```

### Zawartość Down()

```csharp
migrationBuilder.DropIndex(
    name: "IX_TrackedCostAttachments_TenantId_ProjectId",
    table: "TrackedCostAttachments");

migrationBuilder.DropIndex(
    name: "IX_ProjectFileVersions_TenantId_ProjectId",
    table: "ProjectFileVersions");

migrationBuilder.DropColumn(name: "ProjectId", table: "TrackedCostAttachments");
migrationBuilder.DropColumn(name: "TenantId",  table: "TrackedCostAttachments");
migrationBuilder.DropColumn(name: "ProjectId", table: "ProjectFileVersions");
migrationBuilder.DropColumn(name: "TenantId",  table: "ProjectFileVersions");
```

### Skrypty SQL backfill w Up()

**N3 — backfill ProjectFileVersions:**
```sql
UPDATE pv
SET pv.TenantId = pf.TenantId, pv.ProjectId = pf.ProjectId
FROM ProjectFileVersions pv
JOIN ProjectFiles pf ON pv.ProjectFileId = pf.Id
```

**N7 — backfill TrackedCostAttachments:**
```sql
UPDATE ta
SET ta.TenantId = tc.TenantId, ta.ProjectId = tc.ProjectId
FROM TrackedCostAttachments ta
JOIN TrackedCosts tc ON ta.TrackedCostId = tc.Id
```

## Podsumowanie końcowe

### Status kompilacji
**Sukces — 0 błędów, 0 ostrzeżeń.**

### Wygenerowane migracje

| Nazwa migracji | Tabele | Operacje |
|---|---|---|
| `20260506082551_N3-N7-N10-tenantid-projectid-in-versions-and-attachments` | `ProjectFileVersions`, `TrackedCostAttachments` | AddColumn ×4, CreateIndex ×2, Sql ×2 |

### Zmodyfikowane pliki

| Plik | Zmiana |
|---|---|
| `src/Entities/Models/CostEstimates/CostEstimateFieldFile.cs` | N10: nowy komentarz XML na CostEstimateId |
| `src/Entities/Models/ProjectFileVersion.cs` | N3: dodano TenantId, ProjectId |
| `src/Entities/Configurations/ProjectFileVersionConfiguration.cs` | N3: IsRequired + indeks TenantId/ProjectId |
| `src/CQRS/Files/UploadProjectFiles/UploadProjectFilesCommandHandler.cs` | N3: TenantId/ProjectId przy tworzeniu wersji |
| `src/CQRS/Files/UploadProjectFileVersion/UploadProjectFileVersionCommandHandler.cs` | N3: TenantId/ProjectId przy tworzeniu wersji |
| `src/Entities/Models/CostTrackers/TrackedCostAttachment.cs` | N7: dodano TenantId, ProjectId |
| `src/Entities/Configurations/CostTrackers/TrackedCostAttachmentConfiguration.cs` | N7: IsRequired + indeks TenantId/ProjectId |
| `src/Business/Implementation/Services/CostTrackerAttachmentService.cs` | N7: TenantId/ProjectId przy tworzeniu załącznika |
| `src/Entities/Migrations/20260506082551_N3-N7-N10-...cs` | Migracja zbiorcza + backfill SQL |

### Blokery przed deploymentem
Brak blokerów. Backfill SQL uzupełni historyczne rekordy automatycznie podczas `database update`.

### Następny krok
Uruchomić `dotnet ef database update` na środowisku docelowym. N5 — pominięte zgodnie z instrukcją.
