# API Fix 09: Uproszczenie uploadu plików

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Pliki są teraz bezpośrednio na `CostEstimateItem` (przez `CostEstimateItemFile`), a nie przez `CostEstimateItemFieldValue`.
Upraszczamy endpoint uploadu — nie potrzebuje już `fieldDefinitionId`.

## Do zrobienia

### 1. Nowy Command: `UploadItemFilesCommand`

```csharp
public sealed record UploadItemFilesCommand : IRequest<List<Guid>>
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CostEstimateId { get; init; }
    public Guid ItemId { get; init; }
    public List<IFormFile> Files { get; init; } = default!;
}
```

### 2. Handler: `UploadItemFilesCommandHandler`

1. **Walidacja**:
   - Sprawdź czy item istnieje (NotFoundApiException)
   - Sprawdź czy item należy do kosztorysu
   - Sprawdź limity: max 10 plików, max 50 MB na plik

2. **Upload do Blob Storage** (jeśli skonfigurowany):
   - Dla każdego pliku:
     - Generuj unikalny BlobName
     - Upload do Azure Blob (lub lokalnie jeśli brak konfiguracji)
     - Zapisz metadane

3. **Zapisz w bazie**:
   - Utwórz `CostEstimateItemFile` dla każdego pliku
   - Powiąż z `ItemId`
   - Ustaw Order (kolejność)

4. **Zwróć listę ID utworzonych plików**

### 3. Nowy Command: `DeleteItemFileCommand`

```csharp
public sealed record DeleteItemFileCommand : IRequest
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CostEstimateId { get; init; }
    public Guid ItemId { get; init; }
    public Guid FileId { get; init; }
}
```

Handler:
- Soft-delete plik (IsDeleted = true)
- Usuń blob z Azure Storage (jeśli skonfigurowany)

### 4. Nowy Command: `ReplaceItemFilesCommand` (Replace All)

```csharp
public sealed record ReplaceItemFilesCommand : IRequest<List<Guid>>
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid CostEstimateId { get; init; }
    public Guid ItemId { get; init; }
    public List<IFormFile> Files { get; init; } = default!;
}
```

Handler:
- Soft-delete wszystkie istniejące pliki itemu + usuń blob
- Dodaj nowe pliki

### 5. Kontroler — nowe endpointy

Dodaj do `CostEstimateController.cs`:

```csharp
/// <summary>
/// Dodaj pliki do pozycji (append). Bez fieldDefinitionId.
/// </summary>
[HttpPost("{id:guid}/items/{itemId:guid}/files")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
[RequestSizeLimit(524_288_000)] // 500 MB total
public async Task<IActionResult> UploadItemFiles(
    [FromRoute] Guid tenantId,
    [FromRoute] Guid projectId,
    [FromRoute] Guid id,
    [FromRoute] Guid itemId,
    [FromForm] List<IFormFile> files)
{
    var command = new UploadItemFilesCommand
    {
        TenantId = tenantId,
        ProjectId = projectId,
        CostEstimateId = id,
        ItemId = itemId,
        Files = files
    };
    var result = await Send(command);
    return Ok(result);
}

/// <summary>
/// Zastąp wszystkie pliki pozycji (replace all).
/// </summary>
[HttpPut("{id:guid}/items/{itemId:guid}/files")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
[RequestSizeLimit(524_288_000)]
public async Task<IActionResult> ReplaceItemFiles(...)
{
    // analogicznie
}

/// <summary>
/// Usuń plik z pozycji (soft delete).
/// </summary>
[HttpDelete("{id:guid}/items/{itemId:guid}/files/{fileId:guid}")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
public async Task<IActionResult> DeleteItemFile(...)
{
    // analogicznie
}
```

### 6. Usuń stary endpoint upload

**NIE usuwaj jeszcze** starego `UploadCostEstimateFieldFiles` — zostanie usunięty w Fix-10.
Po prostu dodaj nowy obok starego.

### Build

```powershell
dotnet build --configuration Release
```
Jeśli build failed, przerwij i zgłoś błędy.
