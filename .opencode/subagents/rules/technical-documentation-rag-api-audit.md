# API Audit — Technical Documentation (RAG / ekstrakcja PDF/JPG)

**Feature:** technical-documentation-rag  
**Data audytu:** 2026-06-22  
**Audytowane obszary:** Encje/DB, uprawnienia modułowe, CQRS, Azure Storage Queue + worker, Business.AIAgent, blob storage, SignalR, kontroler/endpointy  
**Decyzje MVP (zatwierdzone):** jeden kod `PROJECT.TECHNICAL_DOCUMENTATION`, Azure Storage Queue, retry auto max 3 + ręczny, brak RAG, dedykowany `TechnicalDocumentationHub`, osobna encja/blob (bez `ProjectFile`), konwersja PDF przez Docnet.Core, osobny endpoint count

---

## BLOK 1 — Stan obecny

### Feature — brak implementacji

Przeszukanie `02-ApplicationServices/ProductDataManagementWebAPI` nie wykazało żadnych plików ani referencji do:
- `ProjectTechnicalDocumentation`, `TechnicalDocumentation`, `TECHNICAL_DOCUMENTATION`
- `TechnicalDocumentationHub`, `TechnicalDocumentationController`
- agentów dokumentacji technicznej

**Cały feature wymaga implementacji od zera.**

---

### Infrastruktura gotowa do wykorzystania

#### Azure Storage Queue + BackgroundService

Wzorzec w pełni działający:

| Element | Lokalizacja | Uwagi |
|---------|-------------|-------|
| `IQueueStorageService` | `Business/Interfaces/Services/IQueueStorageService.cs` | `EnqueueAsync`, `DequeueAsync`, `DeleteMessageAsync`, `DequeueCount` |
| `QueueStorageService` | `Business/Implementation/Services/QueueStorageService.cs` | Ten sam account co Blob (`BlobStorageSettings.QueueUrl`), `DefaultAzureCredential` |
| `QueueNames` | `Business/Interfaces/Constants/QueueNames.cs` | 4 kolejki: notification, email, message — **brak kolejki dokumentacji technicznej** |
| Workery | `NotificationWorker`, `MessageWorker`, `EmailWorker`, `NotificationMarkAsReadWorker` | `BackgroundService`, pętla poll, poison message po `MaxDequeueCount` (**obecnie 5**, MVP wymaga **3**) |
| Rejestracja DI | `ServiceCollectionExtensions.AddAppServices()` | `AddSingleton<IQueueStorageService>`, `AddHostedService<*Worker>` |

#### Blob storage

| Element | Lokalizacja | Uwagi |
|---------|-------------|-------|
| `IBlobStorageService` | `Business/Interfaces/Services/IBlobStorageService.cs` | `UploadAsync`, `DownloadAsync`, `DeleteAsync`, `GenerateSasUri` |
| `BlobContainerNames` | `Business/Interfaces/Configurations/BlobContainerNames.cs` | `Documentation`, `CostEstimates`, `CostTrackers`, `ProjectCosts` — **brak kontenera dla dokumentacji technicznej** |
| Wzorzec uploadu | `UploadProjectFilesCommandHandler` | Kontener `documentation` (pliki projektowe), ścieżka `{tenantId}/{projectId}/...` |
| Wzorzec SAS preview | `GetProjectCostsQueryHandler` | `GenerateSasUri` z `contentDisposition: inline/attachment` |
| Limit uploadu | `ServiceCollectionExtensions.AddApiBasics()` | `MultipartBodyLengthLimit = 52_428_800` (50 MB) — zgodne z wymaganiami feature |

> **Uwaga:** `BlobContainerNames.Documentation` jest używany przez moduł **Pliki projektowe** (`ProjectFile`). Zgodnie z decyzją MVP dokumentacja techniczna wymaga **osobnego kontenera** (np. `TechnicalDocumentation`), nie współdzielenia z `ProjectFile`.

#### SignalR

| Hub | Ścieżka | Wzorzec |
|-----|---------|---------|
| `NotificationHub` | `/api/hubs/notifications` | `IHubContext<NotificationHub, INotificationClient>` |
| `MessageHub` | `/api/hubs/messages` | Dispatcher z workera kolejki |
| `AIHub` | `/api/hubs/ai` | Streaming agentów (interaktywny chat) |

Wzorzec push z workera → klient:
1. Worker przetwarza wiadomość z kolejki
2. `INotificationDispatcher` (`SignalRNotificationDispatcher`) wywołuje `hubContext.Clients.User(azureAdB2CObjectId).ReceiveNotification(payload)`
3. Identyfikacja użytkownika przez `IUserIdProvider` (`AzureAdB2CUserIdProvider`)

**Brak dedykowanego `TechnicalDocumentationHub`** — wymagany nowy hub zgodnie z MVP.

#### Business.AIAgent

Infrastruktura agentowa gotowa:

| Element | Lokalizacja |
|---------|-------------|
| `AgentRunner` | `Business.AIAgent/Core/AgentRunner.cs` — iteracyjna pętla tool-calling, timeout, streaming |
| `IAICompletionService` | `CompleteAsync` (tekst/JSON), `CompleteWithImageAsync` (Vision GPT-4o) |
| `CallSubAgentTool` | Delegacja do subagentów przez `agent_name` + `task` |
| `CostEstimateAIGeneratorService` | **Wzorzec orkiestratora**: planner → równoległe subagenty → agregacja wyniku |
| Agent definitions | `Business.AIAgent/Resources/Agents/sub_agents/*.md` |
| Rejestracja DI | `AIAgentServiceExtensions.AddAIAgent()` |

**Brak agentów dokumentacji technicznej** (orkiestrator, klasyfikacja, ekstrakcja, agregacja).

#### AI Cost Document Import (wzorzec parsowania obrazów)

Zaimplementowany synchroniczny parsing (bez kolejki):

| Element | Stan |
|---------|------|
| `ParseCostDocumentQuery` + handler | `CQRS/AI/ParseCostDocument/` |
| `IDocumentParserService` / `DocumentParserService` | Vision przez `IAICompletionService.CompleteWithImageAsync` |
| `AICostController` | `POST .../ai/cost/parse/project-cost`, `parse/tracked-cost` |
| PDF | **Nieobsługiwany** — kontroler akceptuje tylko `.jpg/.jpeg/.png` |
| Docnet.Core | **Brak w `.csproj`** — pakiet nie jest zainstalowany |

#### Uprawnienia modułowe

System uproszczony (boolean per moduł, bez `ModuleAccessLevel`):

| Element | Stan |
|---------|------|
| `ProjectModule` enum | `Settings, Files, Estimates, Costs, Schedule, DashboardTracker` — **brak `TechnicalDocumentation`** |
| `ProjectMemberModulePermission` | PK `(TenantId, ProjectId, UserId, Module)` — obecność modułu = dostęp |
| `PermissionCodes` | Jeden kod per moduł: `PROJECT.FILES`, `PROJECT.COSTS` itd. — **brak `PROJECT.TECHNICAL_DOCUMENTATION`** |
| `ModulePermissionTranslator` | `Translate(module) → HashSet<string>` z jednym kodem |
| `PermissionScopes` | Mapowanie kod → scope (Project) |
| `SuperAdminFallbackPermissions.ProjectReadOnly` | Lista kodów read-only — wymaga rozszerzenia |
| Zaproszenia / role | `InviteProjectMemberCommand.Modules`, `UpdateProjectMemberRoleCommand.Modules` — lista `ProjectModule` |

Zgodnie z decyzją MVP: **jeden kod** `PROJECT.TECHNICAL_DOCUMENTATION` — spójne z obecnym modelem uproszczonym (nie wymaga osobnych kodów READ/WRITE).

#### CQRS / kontrolery

- **Brak** jakichkolwiek Commands/Queries/Validators dla dokumentacji technicznej
- **Brak** kontrolera `TechnicalDocumentationController`
- **Brak** wzorca `202 Accepted` w całym API — uploady zwracają `200 OK` lub `201 Created`
- Wzorzec count: `GetUnreadCounterQuery` (`NotificationController`) — prosty `int`, osobny endpoint

---

## BLOK 2 — Luki i braki

| Brak / Luka | Warstwa | Priorytet | Opis |
|-------------|---------|-----------|------|
| Encje `ProjectTechnicalDocumentation` + `ProjectTechnicalDocumentationFile` | Entities | **krytyczne** | Główna encja ze statusem, JSON `Details`, kolekcja plików źródłowych |
| Enum `TechnicalDocumentationStatus` | Entities | **krytyczne** | `Pending`, `Processing`, `Completed`, `Failed` |
| Model `ProjectTechnicalDocumentationDetails` (+ typy zagnieżdżone) | Business | **krytyczne** | Serializacja JSON do kolumny `Details` |
| `PermissionCodes.ProjectTechnicalDocumentation` | Business | **krytyczne** | `"PROJECT.TECHNICAL_DOCUMENTATION"` |
| `ProjectModule.TechnicalDocumentation` | Entities | **krytyczne** | Nowa wartość enum + migracja |
| Rozszerzenie `ModulePermissionTranslator`, `PermissionScopes`, `SuperAdminFallbackPermissions`, `PermissionCodes.All` | Business | **krytyczne** | Pełna integracja z systemem uprawnień |
| `CreateTechnicalDocumentationCommand` (upload → 202) | CQRS | **krytyczne** | Tworzy rekord `Pending`, upload blobów, enqueue |
| `TechnicalDocumentationWorker` | Business | **krytyczne** | BackgroundService konsumujący kolejkę |
| `QueueNames.TechnicalDocumentationProcess` | Business | **krytyczne** | Nowa kolejka Azure Storage Queue |
| `ITechnicalDocumentationProcessingService` | Business | **krytyczne** | Pipeline: PDF→JPG, agenci AI, zapis JSON, SignalR |
| `TechnicalDocumentationHub` + dispatcher | WebApi | **krytyczne** | Powiadomienia o zakończeniu przetwarzania |
| `TechnicalDocumentationController` | WebApi | **krytyczne** | Endpointy REST (lista, szczegóły, upload, retry, count) |
| Pakiet `Docnet.Core` | Business | **krytyczne** | Konwersja PDF→JPG (decyzja MVP); **blocker** — brak w repo |
| `BlobContainerNames.TechnicalDocumentation` | Business | **wysokie** | Osobny kontener blob (nie `Documentation` / `ProjectFile`) |
| `GetTechnicalDocumentationListQuery` | CQRS | **wysokie** | Lista dokumentacji projektu |
| `GetTechnicalDocumentationDetailsQuery` | CQRS | **wysokie** | Szczegóły + SAS URL plików źródłowych |
| `GetTechnicalDocumentationCountQuery` | CQRS | **wysokie** | Osobny endpoint count dla kafelka ProjectDetails |
| `RetryTechnicalDocumentationCommand` | CQRS | **wysokie** | Ręczny retry — reset statusu, re-enqueue |
| 5 definicji agentów `.md` | Business.AIAgent | **wysokie** | Orkiestrator, klasyfikacja, ekstrakcja arch., instalacje, agregacja |
| `TechnicalDocumentationOrchestratorService` | Business | **wysokie** | Wzorzec `CostEstimateAIGeneratorService` — koordynacja subagentów |
| `IPdfToImageConverterService` (Docnet.Core) | Business | **wysokie** | Renderowanie stron PDF do JPG |
| Web modele (`TechnicalDocumentationWeb`, `TechnicalDocumentationListItemWeb`, itd.) | Business | **wysokie** | DTO dla API |
| Validatory FluentValidation | CQRS | **wysokie** | Upload (pliki, rozmiar, MIME), retry (status `Failed`) |
| `MaxDequeueCount = 3` w workerze dokumentacji | Business | **wysokie** | MVP: auto retry max 3 (obecne workery używają 5) |
| Pole `RetryCount` / `AutoRetryCount` na encji | Entities | **normalne** | Śledzenie auto-retry vs ręczny retry |
| `DeleteTechnicalDocumentationCommand` | CQRS | **normalne** | Nie w spec MVP, ale prawdopodobnie potrzebne operacyjnie |
| Testy jednostkowe (handler, validator, worker, orchestrator) | tests/ | **normalne** | xUnit + Moq wg wzorca repo |
| Azure AI Search / RAG | — | — | **Poza MVP** — świadomie wyłączone |

---

## BLOK 3 — Zmiany w encjach/DB

| Encja | Zmiana | Typ | Wymaga migracji |
|-------|--------|-----|----------------|
| `ProjectTechnicalDocumentation` | Nowa encja | nowa encja | **Tak** |
| `ProjectTechnicalDocumentationFile` | Nowa encja (pliki źródłowe) | nowa encja | **Tak** |
| `TechnicalDocumentationStatus` | Nowy enum | nowy enum | **Tak** (kolumna int) |
| `ProjectModule` | `TechnicalDocumentation = 7` | nowa wartość enum | **Nie** (tylko kod) |
| `Permission` (seed) | Nowy kod `PROJECT.TECHNICAL_DOCUMENTATION` | seed data | **Tak** (jeśli tabela Permissions istnieje w seederze) |

### Proponowany model `ProjectTechnicalDocumentation`

```csharp
public class ProjectTechnicalDocumentation : BaseEntity  // lub DeletableEntity jeśli soft-delete
{
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public TechnicalDocumentationStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DetailsJson { get; set; }          // nvarchar(max), serializowany ProjectTechnicalDocumentationDetails
    public int AutoRetryCount { get; set; }           // licznik auto-retry z kolejki (max 3)
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public virtual Project Project { get; set; } = default!;
    public virtual ICollection<ProjectTechnicalDocumentationFile> Files { get; set; } = new List<ProjectTechnicalDocumentationFile>();
}
```

### Proponowany model `ProjectTechnicalDocumentationFile`

```csharp
public class ProjectTechnicalDocumentationFile : BaseEntity
{
    public Guid TechnicalDocumentationId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public string OriginalFileName { get; set; } = default!;
    public string BlobName { get; set; } = default!;
    public string ContentType { get; set; } = default!;   // application/pdf | image/jpeg
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual ProjectTechnicalDocumentation TechnicalDocumentation { get; set; } = default!;
}
```

**Wzorzec:** analogiczny do `BaseCostAttachment` (osobna encja + `BlobName`, bez FK do `ProjectFile`).

### Konfiguracja EF

- Indeks: `(TenantId, ProjectId)` na `ProjectTechnicalDocumentation`
- FK: `ProjectId` → `Projects` z `OnDelete(DeleteBehavior.Restrict)`
- FK: `TechnicalDocumentationId` → `ProjectTechnicalDocumentation` z `OnDelete(DeleteBehavior.Cascade)`
- `DetailsJson`: `HasColumnType("nvarchar(max)")` + opcjonalnie `HasConversion` z `System.Text.Json`
- Predykaty w handlerach: zawsze `TenantId` + `ProjectId`

### Migracja

```powershell
cd src/Entities
dotnet ef migrations add add-technical-documentation --startup-project ../WebApi
```

---

## BLOK 4 — Nowe Commands/Queries

| Command/Query | Typ | Opis | Handler |
|---------------|-----|------|---------|
| `CreateTechnicalDocumentationCommand` | **Nowy** | Multipart: `Name`, `Description?`, `Files[]` (PDF/JPG, max 50 MB). Tworzy encję `Pending`, upload blobów, enqueue do kolejki. Zwraca `TechnicalDocumentationCreatedWeb` (Id, Status). Permission: `PROJECT.TECHNICAL_DOCUMENTATION` | `CreateTechnicalDocumentationCommandHandler` |
| `RetryTechnicalDocumentationCommand` | **Nowy** | Dla statusu `Failed`: reset do `Pending`, wyczyść `ErrorMessage`, enqueue ponownie. Permission: `PROJECT.TECHNICAL_DOCUMENTATION` | `RetryTechnicalDocumentationCommandHandler` |
| `GetTechnicalDocumentationListQuery` | **Nowy** | Lista dokumentacji projektu (Id, Name, Description, Status, CreatedAt, FileCount, CompletedAt). Permission: `PROJECT.TECHNICAL_DOCUMENTATION` | `GetTechnicalDocumentationListQueryHandler` |
| `GetTechnicalDocumentationDetailsQuery` | **Nowy** | Pełne szczegóły + `Details` (deserializowany JSON) + pliki z SAS URL. Permission: `PROJECT.TECHNICAL_DOCUMENTATION` | `GetTechnicalDocumentationDetailsQueryHandler` |
| `GetTechnicalDocumentationCountQuery` | **Nowy** | `int` — liczba dokumentacji w projekcie (dla kafelka). Permission: `PROJECT.TECHNICAL_DOCUMENTATION` | `GetTechnicalDocumentationCountQueryHandler` |
| `DeleteTechnicalDocumentationCommand` | **Nowy (opcjonalny)** | Soft-delete + cleanup blobów. Permission: `PROJECT.TECHNICAL_DOCUMENTATION` | `DeleteTechnicalDocumentationCommandHandler` |

### Walidatory

| Validator | Reguły kluczowe |
|-----------|----------------|
| `CreateTechnicalDocumentationCommandValidator` | `Name` required; `Files` not empty; każdy plik ≤ 50 MB; MIME `application/pdf` lub `image/jpeg`; rozszerzenie `.pdf`/`.jpg`/`.jpeg` |
| `RetryTechnicalDocumentationCommandValidator` | Dokumentacja istnieje; `Status == Failed`; opcjonalnie: nie w trakcie `Processing` |

### Wzorzec `CreateTechnicalDocumentationCommandHandler`

```csharp
public sealed class CreateTechnicalDocumentationCommandHandler
    : IRequestHandler<CreateTechnicalDocumentationCommand, TechnicalDocumentationCreatedWeb>
{
    // IRepository<ProjectTechnicalDocumentation>, IRepository<ProjectTechnicalDocumentationFile>
    // IBlobStorageService, IQueueStorageService, ICurrentUser

    public async Task<TechnicalDocumentationCreatedWeb> Handle(...)
    {
        // 1. Walidacja projektu (TenantId + ProjectId)
        // 2. Insert ProjectTechnicalDocumentation (Status = Pending)
        // 3. Upload każdego pliku do BlobContainerNames.TechnicalDocumentation
        //    ścieżka: {tenantId}/{projectId}/{documentationId}/{fileId}/{originalFileName}
        // 4. Insert ProjectTechnicalDocumentationFile records
        // 5. SaveChanges
        // 6. Enqueue JSON: { documentationId, tenantId, projectId, userId, azureAdB2CObjectId }
        // 7. Return new TechnicalDocumentationCreatedWeb(Id, Status = Pending)
    }
}
```

### Payload kolejki

```json
{
  "documentationId": "guid",
  "tenantId": "guid",
  "projectId": "guid",
  "userId": "guid",
  "azureAdB2CObjectId": "string",
  "isManualRetry": false
}
```

---

## BLOK 5 — Zmiany w kontrolerach

| Endpoint | HTTP Method | Nowy/Modyfikacja | Opis |
|----------|-------------|-----------------|------|
| `api/tenants/{tenantId}/projects/{projectId}/technical-documentation` | `GET` | **Nowy** | Lista dokumentacji |
| `api/tenants/{tenantId}/projects/{projectId}/technical-documentation/count` | `GET` | **Nowy** | Count dla kafelka ProjectDetails |
| `api/tenants/{tenantId}/projects/{projectId}/technical-documentation/{id}` | `GET` | **Nowy** | Szczegóły dokumentacji |
| `api/tenants/{tenantId}/projects/{projectId}/technical-documentation` | `POST` | **Nowy** | Upload + create → **202 Accepted** |
| `api/tenants/{tenantId}/projects/{projectId}/technical-documentation/{id}/retry` | `POST` | **Nowy** | Ręczny retry → **202 Accepted** |
| `api/tenants/{tenantId}/projects/{projectId}/technical-documentation/{id}` | `DELETE` | **Nowy (opcjonalny)** | Usunięcie → 204 |

### Nowy kontroler `TechnicalDocumentationController`

**Uzasadnienie:** osobna domena (asynchroniczne przetwarzanie AI), nie pasuje do `FileController` (synchroniczny CRUD plików) ani `AICostController` (synchroniczny one-shot parse).

```csharp
[ApiController]
[Route("api/tenants/{tenantId:guid}/projects/{projectId:guid}/technical-documentation")]
public sealed class TechnicalDocumentationController(IMediator mediator)
    : BaseApiController(mediator)
{
    [HttpGet("count")]
    [Authorize(Policy = PermissionCodes.ProjectTechnicalDocumentation)]
    public async Task<IActionResult> GetCount(...) => Ok(await Send(new GetTechnicalDocumentationCountQuery(...)));

    [HttpPost]
    [Authorize(Policy = PermissionCodes.ProjectTechnicalDocumentation)]
    [RequestSizeLimit(52_428_800)]
    [ProducesResponseType(typeof(TechnicalDocumentationCreatedWeb), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Create(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid projectId,
        [FromForm] string name,
        [FromForm] string? description,
        [FromForm] List<IFormFile> files)
    {
        TechnicalDocumentationCreatedWeb result = await Send(new CreateTechnicalDocumentationCommand { ... });
        return AcceptedAtAction(nameof(GetDetails), new { tenantId, projectId, id = result.Id }, result);
    }

    [HttpPost("{id:guid}/retry")]
    [Authorize(Policy = PermissionCodes.ProjectTechnicalDocumentation)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Retry(...) { ... return Accepted(); }
}
```

### Mapowanie SignalR

W `Program.cs`:
```csharp
app.MapHub<TechnicalDocumentationHub>("/api/hubs/technical-documentation")
    .RequireAuthorization();
```

### Interfejs klienta SignalR

```csharp
public interface ITechnicalDocumentationClient
{
    Task ProcessingCompleted(TechnicalDocumentationProcessingResultDto result);
}

public sealed record TechnicalDocumentationProcessingResultDto(
    Guid DocumentationId,
    string Name,
    TechnicalDocumentationStatus Status,
    string? ErrorMessage);
```

---

## BLOK 6 — Zmiany w serwisach

| Serwis | Interfejs | Nowy/Modyfikacja | Metody |
|--------|-----------|-----------------|--------|
| `TechnicalDocumentationWorker` | — (`BackgroundService`) | **Nowy** | Pętla dequeue z `QueueNames.TechnicalDocumentationProcess`, `MaxDequeueCount = 3`, wywołanie `ITechnicalDocumentationProcessingService` |
| `TechnicalDocumentationProcessingService` | `ITechnicalDocumentationProcessingService` | **Nowy** | `ProcessAsync(documentationId, ct)` — pełny pipeline |
| `TechnicalDocumentationOrchestratorService` | `ITechnicalDocumentationOrchestratorService` | **Nowy** | Koordynacja agentów AI (wzorzec `CostEstimateAIGeneratorService`) |
| `PdfToImageConverterService` | `IPdfToImageConverterService` | **Nowy** | `ConvertAllPagesAsync(byte[] pdfBytes, ct) → List<byte[]>` via Docnet.Core |
| `TechnicalDocumentationDispatcher` | `ITechnicalDocumentationDispatcher` | **Nowy** | `DispatchCompletedAsync(payload, ct)` — push przez `IHubContext<TechnicalDocumentationHub>` |
| `QueuedTechnicalDocumentationSender` | `IQueuedTechnicalDocumentationSender` | **Nowy** | `EnqueueAsync(documentationId, ...)` — helper do enqueue z handlerów |

### Pipeline `TechnicalDocumentationProcessingService`

```
1. Load documentation + files (TenantId + ProjectId predicate)
2. Status → Processing
3. Dla każdego pliku:
   - PDF → IPdfToImageConverterService (Docnet.Core) → lista JPG per strona
   - JPG → bez konwersji
4. ITechnicalDocumentationOrchestratorService.ProcessImagesAsync(images):
   a. DrawingClassificationAgent (per obraz)
   b. ArchitecturalExtractionAgent (per obraz, równolegle, SemaphoreSlim)
   c. InstallationsExtractionAgent (per obraz, równolegle)
   d. AggregationAgent (raz, scala wyniki → ProjectTechnicalDocumentationDetails)
5. Serializuj Details → DetailsJson
6. Status → Completed, CompletedAt = UtcNow
7. ITechnicalDocumentationDispatcher → SignalR do użytkownika
--- on error ---
   Status → Failed, ErrorMessage = ex.Message
   ITechnicalDocumentationDispatcher → SignalR (status Failed)
```

### Agenci AI (definicje `.md`)

| Agent | Plik | Rola |
|-------|------|------|
| `documentation-orchestrator` | `sub_agents/documentation_orchestrator.md` | Koordynacja (opcjonalnie, jeśli używamy AgentRunner z tools) |
| `drawing-classification-agent` | `sub_agents/drawing_classification_agent.md` | Typ rysunku, skala |
| `architectural-extraction-agent` | `sub_agents/architectural_extraction_agent.md` | Pomieszczenia, ściany, otwory, dach |
| `installations-extraction-agent` | `sub_agents/installations_extraction_agent.md` | Instalacje branżowe |
| `aggregation-agent` | `sub_agents/aggregation_agent.md` | Scalenie → `ProjectTechnicalDocumentationDetails` JSON |

**Rekomendacja architektoniczna:** dla pipeline'u backgroundowego użyć wzorca `CostEstimateAIGeneratorService` (bezpośrednie wywołania `IAgentRunner.RunAsync` / `IAICompletionService.CompleteWithImageAsync`), a nie interaktywnego `AIHub`. `AIHub` służy do chatu ze streamingiem — dokumentacja techniczna to fire-and-forget z dedykowanym hubem powiadomień.

### Rejestracja DI (rozszerzenie `AddAppServices`)

```csharp
services.AddHostedService<TechnicalDocumentationWorker>();
services.AddScoped<ITechnicalDocumentationProcessingService, TechnicalDocumentationProcessingService>();
services.AddScoped<ITechnicalDocumentationOrchestratorService, TechnicalDocumentationOrchestratorService>();
services.AddScoped<IPdfToImageConverterService, PdfToImageConverterService>();
services.AddScoped<ITechnicalDocumentationDispatcher, SignalRTechnicalDocumentationDispatcher>();
services.AddScoped<IQueuedTechnicalDocumentationSender, QueuedTechnicalDocumentationSender>();
```

### Zmiany w `.csproj`

```xml
<!-- Business.csproj -->
<PackageReference Include="Docnet.Core" Version="2.6.0" />
```

---

## BLOK 7 — Problemy i ryzyka

| # | Problem | Warstwa | Ryzyko | Rekomendacja |
|---|---------|---------|--------|-------------|
| 1 | **Brak Docnet.Core** | Business | **krytyczne** | Dodać pakiet NuGet. Docnet wymaga natywnych bibliotek PDFium — zweryfikować w Docker (linux-x64). Testować render w CI/CD. |
| 2 | **Kolizja nazwy kontenera `Documentation`** | Blob | **wysokie** | Nie używać `BlobContainerNames.Documentation` (zajęty przez ProjectFile). Nowy enum `TechnicalDocumentation`. |
| 3 | **Brak wzorca 202 Accepted** | WebApi | **normalne** | Pierwszy endpoint z `202` w API — użyć `AcceptedAtAction` z `Location` header do GET details. |
| 4 | **MaxDequeueCount = 5 vs MVP = 3** | Worker | **normalne** | Nowy worker z `MaxDequeueCount = 3`. Po przekroczeniu: status `Failed`, `ErrorMessage = "Auto-retry limit exceeded"`, SignalR notify. Nie zmieniać istniejących workerów. |
| 5 | **Długi czas przetwarzania AI** | Business.AIAgent | **wysokie** | Wiele stron PDF × wiele subagentów = długi pipeline. Ustawić timeout per agent (`AzureAIAgentOptions.AgentTimeoutSeconds`). Rozważyć `SemaphoreSlim` (wzorzec z `CostEstimateAIGeneratorService`, limit 5). Visibility timeout kolejki ≥ czas przetwarzania (np. 30 min). |
| 6 | **Koszt Azure OpenAI** | Business | **wysokie** | Każda strona PDF = osobne wywołanie Vision. Duże dokumenty (50 MB, wiele stron) mogą generować wysokie koszty. Rozważyć limit stron w MVP. |
| 7 | **SignalR — identyfikacja użytkownika** | WebApi | **normalne** | Worker musi znać `azureAdB2CObjectId` twórcy (zapisany w payload kolejki). Wzorzec z `NotificationWorker` + `SignalRNotificationDispatcher.Clients.User()`. |
| 8 | **JSON `Details` bez wersjonowania** | Entities | **normalne** | Feature otwiera kwestię wersjonowania modelu JSON. MVP: jeden schemat, `DetailsJson` bez `schemaVersion`. Dodać pole w przyszłości. |
| 9 | **Feature spec vs MVP — granularne READ/WRITE** | Uprawnienia | **normalne** | Spec feature wymienia odczyt/zapis osobno; MVP i obecny system = jeden kod. Implementacja: jeden kod dla wszystkich operacji. UI rozróżnia przycisk „Dodaj" przez obecność uprawnienia modułu (tenant admin / `IsAdmin` / przypisany moduł). |
| 10 | **Brak RAG / Azure AI Search** | — | — | Świadomie poza MVP. Nie dodawać zależności AI Search. |
| 11 | **Transakcyjność upload + enqueue** | CQRS | **wysokie** | Upload blobów przed `SaveChanges` — przy błędzie DB wymagany cleanup blobów (wzorzec `try/catch` + `DeleteAsync` z `UploadProjectFilesCommandHandler`). Enqueue po successful commit. |
| 12 | **Worker scope DI** | Architektura | **normalne** | `BackgroundService` wymaga `IServiceScopeFactory` do tworzenia scope per message (uniknięcie singleton-scoped leak). Istniejące workery wstrzykują Scoped serwisy bezpośrednio — rozważyć poprawkę lub nowy worker ze scope per message. |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe encje | 2 (`ProjectTechnicalDocumentation`, `ProjectTechnicalDocumentationFile`) |
| Nowe enumy | 2 (`TechnicalDocumentationStatus`, wartość `ProjectModule.TechnicalDocumentation`) |
| Nowe web modele (DTO) | ~5 (`TechnicalDocumentationCreatedWeb`, `TechnicalDocumentationListItemWeb`, `TechnicalDocumentationDetailsWeb`, `TechnicalDocumentationFileWeb`, `TechnicalDocumentationProcessingResultDto`) |
| Nowe Commands | 2–3 (`Create`, `Retry`, opcjonalnie `Delete`) |
| Nowe Queries | 3 (`List`, `Details`, `Count`) |
| Nowe kontrolery | 1 (`TechnicalDocumentationController`) |
| Nowe endpointy | 5–6 |
| Nowe serwisy (interfejsy) | 5 (`IProcessing`, `IOrchestrator`, `IPdfToImageConverter`, `IDispatcher`, `IQueuedSender`) |
| Nowe serwisy (implementacje) | 6 (+ `TechnicalDocumentationWorker`) |
| Nowe agenci AI (definicje `.md`) | 5 |
| Nowy SignalR Hub | 1 (`TechnicalDocumentationHub`) |
| Nowa kolejka Azure | 1 (`technical-documentation-process`) |
| Nowy kontener blob | 1 (`technicaldocumentation`) |
| Modyfikacje uprawnień | 5 plików (`PermissionCodes`, `ProjectModule`, `ModulePermissionTranslator`, `PermissionScopes`, `SuperAdminFallbackPermissions`) |
| Wymaga migracji DB | **Tak** |
| Wymaga zmiany `.csproj` | **Tak** (`Docnet.Core` w `Business.csproj`) |
| Blocker | **Tak** — brak Docnet.Core + brak całej implementacji feature |
| Pytania domenowe | 4 |

---

## Pytania domenowe wymagające decyzji

1. **Limit stron PDF w MVP** — czy przetwarzać wszystkie strony PDF, czy ustawić twardy limit (np. 20 stron) ze względu na koszt i czas Azure OpenAI Vision?

2. **Usuwanie dokumentacji** — czy MVP wymaga endpointu `DELETE` (soft-delete + cleanup blobów), czy wystarczy lista + szczegóły + retry?

3. **Odbiorca powiadomienia SignalR** — czy powiadomienie o zakończeniu przetwarzania trafia tylko do użytkownika, który utworzył dokumentację, czy do wszystkich członków projektu z uprawnieniem `PROJECT.TECHNICAL_DOCUMENTATION`?

4. **Wersjonowanie modelu JSON `Details`** — czy w MVP dodać pole `SchemaVersion` na encji (przyszła kompatybilność wsteczna), czy odkładać do kolejnej iteracji?

---

## Załącznik — mapowanie wzorców z powiązanych feature'ów

| Obszar | Wzorzec źródłowy | Plik referencyjny |
|--------|-----------------|-------------------|
| Upload plików + blob | file-directories | `UploadProjectFilesCommandHandler.cs` |
| Parsowanie obrazów AI | ai-cost-document-import | `DocumentParserService.cs`, `AICostController.cs` |
| Orkiestracja agentów | ai-cost-estimate | `CostEstimateAIGeneratorService.cs` |
| Uprawnienia modułowe | project-module-permissions | `ModulePermissionTranslator.cs`, `ProjectMemberModulePermission.cs` |
| Kolejka + worker | messages/notifications | `MessageWorker.cs`, `NotificationWorker.cs` |
| SignalR push | notifications | `SignalRNotificationDispatcher.cs` |
| Count endpoint | notifications | `GetUnreadCounterQuery` |
| Osobna encja pliku | costs attachments | `BaseCostAttachment.cs` |
