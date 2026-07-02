# API Fix 02 — Web modele + model JSON `ProjectTechnicalDocumentationDetails`

## Cel
DTO dla API i model domenowy JSON zapisywany w `DetailsJson`. Bez `SchemaVersion` w MVP.

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Skills
- `.cursor/skills/api-cqrs/SKILL.md`

## Zależności
- Wymaga ukończenia **api-fix-01** (encje + enum `TechnicalDocumentationStatus`)

## Pliki referencyjne
- `.opencode/features/technical-documentation-rag.md` — pełna hierarchia klas `ProjectTechnicalDocumentationDetails`
- `src/Business/Interfaces/WebModels/` — wzorzec `sealed record`

---

## 1. Model domenowy JSON

Katalog: `src/Business/Interfaces/WebModels/TechnicalDocumentation/`

Utwórz klasy zgodnie ze specyfikacją feature (bez pola `SchemaVersion`):

| Plik | Klasa |
|------|-------|
| `ProjectTechnicalDocumentationDetails.cs` | główny model |
| `ProjectInfo.cs` | metadane projektu |
| `Drawing.cs`, `DrawingSource.cs` | rysunki |
| `Room.cs`, `Dimensions.cs`, `Wall.cs`, `Opening.cs` | architektura |
| `InsulationInfo.cs`, `Finishing.cs` | izolacja/wykończenie |
| `RoofDetails.cs` | dach |
| `InstallationInfo.cs` | instalacje |
| `StockItem.cs` | stolarka |
| `MaterialSummary.cs` | zestawienie materiałów |

Wszystkie klasy `public sealed class` z domyślnymi wartościami (`= new()`, `= string.Empty`).

## 2. Web modele API

Katalog: `src/Business/Interfaces/WebModels/TechnicalDocumentation/`

### `TechnicalDocumentationStatusWeb.cs`
Enum mirror C# (lub użyj bezpośrednio `TechnicalDocumentationStatus` z Entities — sprawdź konwencję innych modułów).

### `TechnicalDocumentationListItemWeb.cs`
```csharp
public sealed record TechnicalDocumentationListItemWeb
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required TechnicalDocumentationStatus Status { get; init; }
    public required int FileCount { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
}
```

### `TechnicalDocumentationFileWeb.cs`
```csharp
public sealed record TechnicalDocumentationFileWeb
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSize { get; init; }
    public string? SasUriPreview { get; init; }
    public string? SasUriDownload { get; init; }
}
```

### `TechnicalDocumentationDetailsWeb.cs`
```csharp
public sealed record TechnicalDocumentationDetailsWeb
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required TechnicalDocumentationStatus Status { get; init; }
    public required int FileCount { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public ProjectTechnicalDocumentationDetails? Details { get; init; }
    public required List<TechnicalDocumentationFileWeb> Files { get; init; }
}
```

**Uwaga MVP:** Nie eksponuj `AutoRetryCount` / `retryCount` w web modelach — UI nie wyświetla licznika retry.

### `TechnicalDocumentationCreatedWeb.cs`
```csharp
public sealed record TechnicalDocumentationCreatedWeb
{
    public required Guid Id { get; init; }
    public required TechnicalDocumentationStatus Status { get; init; }
}
```

### `TechnicalDocumentationProcessingResultDto.cs` (SignalR)
```csharp
public sealed record TechnicalDocumentationProcessingResultDto
{
    public required Guid DocumentationId { get; init; }
    public required Guid ProjectId { get; init; }
    public required Guid TenantId { get; init; }
    public required string Name { get; init; }
    public required TechnicalDocumentationStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
}
```

### `TechnicalDocumentationQueueMessageDto.cs` (kolejka)
```csharp
public sealed record TechnicalDocumentationQueueMessageDto
{
    public required Guid DocumentationId { get; init; }
    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }
    public required Guid UserId { get; init; }
    public required bool IsManualRetry { get; init; }
}
```

## 3. Helper serializacji (opcjonalny)

Plik: `src/Business/Implementation/Helpers/TechnicalDocumentationDetailsSerializer.cs`

Metody statyczne:
- `string Serialize(ProjectTechnicalDocumentationDetails details)`
- `ProjectTechnicalDocumentationDetails? Deserialize(string? json)`

Użyj `System.Text.Json` z `PropertyNameCaseInsensitive = true`.

## Weryfikacja
```powershell
dotnet build src/Business/Business.csproj
```

## Następny krok
Web modele są używane w **api-fix-03** (Queries) i **api-fix-04** (Commands).
