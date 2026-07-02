# API Fix 04 — CQRS Commands (Create, Retry) + kolejka + blob upload

## Cel
Upload dokumentacji (multipart → 202), ręczny retry przy `Failed`, enqueue do Azure Storage Queue.

## Decyzje MVP
- **Brak DELETE** — nie implementuj `DeleteTechnicalDocumentationCommand`
- Ręczny retry tylko gdy `Status == Failed`
- Enqueue **po** successful `SaveChanges`
- Cleanup blobów przy błędzie DB (wzorzec `UploadProjectFilesCommandHandler`)

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Skills
- `.cursor/skills/api-cqrs/SKILL.md`
- `.cursor/skills/api-validators/SKILL.md`
- `.cursor/skills/api-repositories/SKILL.md`

## Zależności
- **api-fix-01**, **api-fix-02**
- `BlobContainerNames.TechnicalDocumentation` i `QueueNames.TechnicalDocumentationProcess` — dodane w **api-fix-05** (zdefiniuj je tymczasowo lub wykonaj fix-05 przed testem end-to-end)

## Pliki referencyjne
- `src/CQRS/Files/UploadProjectFiles/UploadProjectFilesCommandHandler.cs` — blob upload + cleanup
- `src/Business/Interfaces/Constants/QueueNames.cs`
- `src/Business/Interfaces/Services/IQueueStorageService.cs`

---

## 1. Stałe infrastrukturalne (minimalnie w tym kroku)

Jeśli **api-fix-05** jeszcze nie wykonany, dodaj teraz:

### `QueueNames.cs`
```csharp
public const string TechnicalDocumentationProcess = "technical-documentation-process";
```

### `BlobContainerNames.cs`
```csharp
TechnicalDocumentation
```

## 2. `IQueuedTechnicalDocumentationSender`

Plik: `src/Business/Interfaces/Services/IQueuedTechnicalDocumentationSender.cs`

```csharp
Task EnqueueAsync(
    Guid documentationId,
    Guid tenantId,
    Guid projectId,
    Guid userId,
    bool isManualRetry,
    CancellationToken cancellationToken);
```

Implementacja: `src/Business/Implementation/Services/QueuedTechnicalDocumentationSender.cs`
- Serializuj `TechnicalDocumentationQueueMessageDto` do JSON
- `IQueueStorageService.EnqueueAsync(QueueNames.TechnicalDocumentationProcess, json, ct)`
- `EnsureQueueAsync` przy pierwszym enqueue (opcjonalnie)

## 3. `CreateTechnicalDocumentationCommand`

Katalog: `src/CQRS/TechnicalDocumentation/CreateTechnicalDocumentation/`

```csharp
public sealed record CreateTechnicalDocumentationCommand : IRequestCommand<TechnicalDocumentationCreatedWeb>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required List<IFormFile> Files { get; init; }
    public string PermissionCode => PermissionCodes.ProjectTechnicalDocumentation;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

### `CreateTechnicalDocumentationCommandValidator`
- `Name`: NotEmpty, MaxLength(200)
- `Description`: MaxLength(2000) when not null
- `Files`: NotEmpty
- Każdy plik:
  - `Length <= 52_428_800` (50 MB)
  - Content-Type: `application/pdf` lub `image/jpeg`
  - Rozszerzenie: `.pdf`, `.jpg`, `.jpeg`

### `CreateTechnicalDocumentationCommandHandler`
1. Waliduj istnienie projektu (`TenantId` + `ProjectId`)
2. Insert `ProjectTechnicalDocumentation` — `Status = Pending`, `CreatedByUserId = currentUser.UserId`, `CreatedAt = UtcNow`
3. Dla każdego pliku:
   - `fileId = Guid.NewGuid()`
   - Blob path: `{tenantId}/{projectId}/{documentationId}/{fileId}/{originalFileName}`
   - Kontener: `BlobContainerNames.TechnicalDocumentation`
   - `IBlobStorageService.UploadAsync`
   - Insert `ProjectTechnicalDocumentationFile`
4. `SaveChanges`
5. `IQueuedTechnicalDocumentationSender.EnqueueAsync(..., isManualRetry: false)`
6. Return `TechnicalDocumentationCreatedWeb(Id, Status = Pending)`

**Rollback:** przy wyjątku po uploadzie — `DeleteAsync` dla każdego bloba z listy `uploadedBlobPaths`.

## 4. `RetryTechnicalDocumentationCommand`

Katalog: `src/CQRS/TechnicalDocumentation/RetryTechnicalDocumentation/`

```csharp
public sealed record RetryTechnicalDocumentationCommand(
    Guid TenantId, Guid ProjectId, Guid DocumentationId)
    : IRequestCommand<Unit>, IAuthorizableRequest
{
    public string PermissionCode => PermissionCodes.ProjectTechnicalDocumentation;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

### `RetryTechnicalDocumentationCommandValidator`
- Dokumentacja istnieje (custom async rule lub w handlerze)
- `Status == Failed` — inaczej `ValidationException` / business rule error

### `RetryTechnicalDocumentationCommandHandler`
1. Load documentation (`TenantId` + `ProjectId` + `Id`)
2. Jeśli `Status != Failed` → `ConflictApiException`
3. Reset: `Status = Pending`, `ErrorMessage = null`, `CompletedAt = null`, `DetailsJson = null`
4. **Nie** resetuj `AutoRetryCount` (licznik auto-retry z worker)
5. `SaveChanges`
6. Enqueue z `isManualRetry: true`

## 5. Rejestracja DI

W `ServiceCollectionExtensions.AddAppServices()`:
```csharp
services.AddScoped<IQueuedTechnicalDocumentationSender, QueuedTechnicalDocumentationSender>();
```

## Weryfikacja
```powershell
dotnet build src/CQRS/CQRS.csproj
```

## Następny krok
Worker i processing w **api-fix-07** konsumują kolejkę.
