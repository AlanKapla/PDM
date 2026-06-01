# API Audit — AI Cost Document Import

**Feature:** ai-cost-document-import  
**Data audytu:** 2026-06-01  
**Audytowane obszary:** Business.AIAgent, CQRS (CostTrackers/ProjectCosts), Controllers, Contractor search, AzureAIAgentOptions, DI registration, Vision/PDF support

---

## BLOK 1 — Stan obecny

### Business.AIAgent

Pełna infrastruktura agentowa gotowa do użycia:

- `AzureAIAgentOptions` — opcje konfiguracyjne: `Endpoint`, `ApiKey?` (nullable — fallback na DefaultAzureCredential), `DefaultDeployment` (default `"gpt-4o"`), `MaxSubAgentDepth`
- `AgentRunner` — buduje `AzureOpenAIClient` → `ChatClient` przez `BuildChatClient(modelName)`. Obsługuje iteracyjną pętlę tool-calling. **Przyjmuje tylko string jako `userMessage` (`UserChatMessage(string)`) — brak obsługi ImageContentPart w wiadomościach wejściowych.**
- `IAgentTool / AgentToolBase` — dobrze zdefiniowany wzorzec; tools dziedziczą z `AgentToolBase`, implementują `ExecuteAsync(JsonElement, AgentContext, CancellationToken)`
- `ToolRegistry` — odkrywa wszystkie `IAgentTool` zarejestrowane w DI
- `AIAgentServiceExtensions.AddAIAgent()` — rejestruje cały stack: loader, registry, executor, runner + konkretne tools jako `IAgentTool`
- `Azure.AI.OpenAI` **2.2.0-beta.4** — wersja beta; obsługuje `ChatMessageContentPart.CreateImagePart(BinaryData, mediaType)` dla JPG/PNG

### Istniejące CQRS — CostTrackers / ProjectCosts

Wzorzec komend:
```
TrackedCostCommandBase  ← CostTrackerCommandBase : IAuthorizableRequest
  └── CreateTrackedCostCommand
  └── UpdateTrackedCostCommand
```

Wzorzec zapytań:
```
IRequestQuery<T> + IAuthorizableRequest
  └── GetCostLinkOptionsQuery
  └── GetProjectCostsQuery
```

`CreateTrackedCostCommandHandler` wstrzykuje: `IReadRepository<T>`, `ICostTrackerFinancialService`, `ICostTrackerAttachmentService`, `IContractorService`, `ICurrentUser`.

**Brak jakiegokolwiek `ParseCostDocumentQuery` ani handlerów AI w CQRS.**

### Controllers

- `CostTrackerController` — route: `api/tenants/{tenantId:guid}/projects/{projectId:guid}/cost-trackers`, plik przez `[FromForm]`, uprawnienia: `PermissionCodes.ProjectDashboardTracker`
- `ProjectCostController` — route: `api/tenants/{tenantId}/projects/{projectId}/cost`, uprawnienia: `PermissionCodes.ProjectCosts`
- **Brak `AICostController`.**

### Contractor

- Encja `Contractor`: `Name`, `TaxId` (= NIP), `Street`, `City`, `PostalCode`, `Country`, `Email`, `PhoneNumber`, `TenantId`
- `IContractorService` ma **tylko** `GetNamesByIdsAsync(ids, tenantId)` — brak wyszukiwania po name/NIP/adresie
- `GetContractorsQueryHandler` implementuje już wyszukiwanie po Name + TaxId + City, ale jest to zapytanie CQRS wymagające uprawnień `TenantSettingsView` — nie nadaje się do wołania z wnętrza handlera bez kontekstu HTTP

### Inne

- `Business.AIAgent.csproj` NIE referuje projektu `Business` (referuje tylko `Entities` + `Repositories`)
- `CQRS.csproj` NIE referuje `Business.AIAgent`
- Brak jakiejkolwiek biblioteki PDF w projekcie (brak `PdfPig`, `iTextSharp`, `PdfSharpCore` itp.)
- Brak użycia Vision/ImageContentPart w całej bazie kodu

---

## BLOK 2 — Luki i braki

| Brak / Luka | Warstwa | Priorytet | Opis |
|-------------|---------|-----------|------|
| `ParsedCostDto` (web model) | `Business/Interfaces/WebModels/AI/` | **Wysoki** | Brak DTO zwracanego przez query. Musi odzwierciedlać spec z feature.md |
| `IDocumentParserService` (interfejs) | `Business/Interfaces/Services/` | **Wysoki** | Pośrednia warstwa między CQRS a Business.AIAgent; pozwala handlerowi nie referencować AIAgent |
| `DocumentParserService` (implementacja) | `Business.AIAgent/Services/` | **Wysoki** | Implementacja `IDocumentParserService` z użyciem `AzureAIAgentOptions` + `ChatClient`, wywołanie Vision API |
| `ParseCostDocumentQuery` | `CQRS/AI/ParseCostDocument/` | **Wysoki** | Nowa query: `IRequestQuery<ParsedCostDto>` + `IAuthorizableRequest`, przyjmuje `IFormFile` + `CostType` |
| `ParseCostDocumentQueryHandler` | `CQRS/AI/ParseCostDocument/` | **Wysoki** | Handler wstrzykujący `IDocumentParserService` + `IContractorService` (rozszerzone) |
| `AICostController` | `WebApi/Controllers/` | **Wysoki** | Nowy kontroler z endpointem `POST /ai/cost/parse`, `[FromForm]`, multipart |
| Rozszerzenie `IContractorService` | `Business/Interfaces/Services/` | **Wysoki** | Dodanie metody `SearchByProfileAsync(name, taxId, address, tenantId, ct)` → `Contractor?` |
| `ParseCostDocumentTool` | `Business.AIAgent/Tools/CostDocument/` | **Średni** | Tool dla agenta chat — przyjmuje base64 image jako parametr JSON, zwraca JSON z ParsedCostDto |
| `cost_document_parser.md` | `Business.AIAgent/Resources/Agents/sub_agents/` | **Średni** | Agent definition dla parsera dokumentów, model `gpt-4o`, bez tools (one-shot Vision) |
| Referencja `Business` ← `Business.AIAgent` | `Business.AIAgent.csproj` | **Wysoki** | Niezbędna by AIAgent mógł implementować `IDocumentParserService` |
| Biblioteka PDF | `Business.AIAgent.csproj` lub `Business.csproj` | **Wysoki (blocker dla PDF)** | Brak obsługi PDF; GPT-4o Vision przyjmuje obrazy (nie PDF bezpośrednio przez SDK) |
| Rejestracja `ParseCostDocumentTool` | `AIAgentServiceExtensions.cs` | **Niski** | Dodać po stworzeniu toola |

---

## BLOK 3 — Zmiany w encjach/DB

| Encja | Zmiana | Typ | Wymaga migracji |
|-------|--------|-----|----------------|
| Brak | — | — | **Nie** |

Feature jest read-only na poziomie AI parsowania. `ParsedCostDto` to DTO, nie encja. Zapis odbywa się przez istniejące `CreateTrackedCostCommand` / `CreateProjectCostCommand` — bez zmian.

---

## BLOK 4 — Nowe Commands/Queries

| Command/Query | Typ | Opis | Handler |
|---------------|-----|------|---------|
| `ParseCostDocumentQuery` | **Nowy** | Query parsujące dokument przez AI. Pola: `TenantId`, `ProjectId`, `File: IFormFile`, `CostType: CostDocumentType` (enum: TrackedCost / ProjectCost). Implementuje `IRequestQuery<ParsedCostDto>`, `IAuthorizableRequest` | `ParseCostDocumentQueryHandler` |
| `ParseCostDocumentQueryHandler` | **Nowy** | Wstrzykuje `IDocumentParserService`, `IContractorService`. Konwertuje IFormFile → byte[], woła serwis AI, następnie szuka kontrahenta, zwraca `ParsedCostDto` | — |

### Wzorzec do zastosowania

```csharp
// CQRS/AI/ParseCostDocument/ParseCostDocumentQuery.cs
public sealed record ParseCostDocumentQuery : IRequestQuery<ParsedCostDto>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public required Guid ProjectId { get; init; }
    public required IFormFile File { get; init; }
    public CostDocumentType CostType { get; init; } = CostDocumentType.TrackedCost;

    public string PermissionCode => PermissionCodes.ProjectDashboardTracker;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}

// CQRS/AI/ParseCostDocument/ParseCostDocumentQueryHandler.cs
public sealed class ParseCostDocumentQueryHandler
    : IRequestHandler<ParseCostDocumentQuery, ParsedCostDto>
{
    private readonly IDocumentParserService _parserService;
    private readonly IContractorService _contractorService;

    public ParseCostDocumentQueryHandler(
        IDocumentParserService parserService,
        IContractorService contractorService) { ... }

    public async Task<ParsedCostDto> Handle(
        ParseCostDocumentQuery request,
        CancellationToken cancellationToken)
    {
        using MemoryStream ms = new();
        await request.File.CopyToAsync(ms, cancellationToken);
        byte[] fileBytes = ms.ToArray();
        string mediaType = request.File.ContentType;

        ParsedCostDto result = await _parserService.ParseAsync(
            fileBytes, mediaType, request.TenantId, cancellationToken);

        // Wyszukaj kontrahenta po danych z dokumentu
        if (!string.IsNullOrWhiteSpace(result.ContractorName) || !string.IsNullOrWhiteSpace(result.ContractorNip))
        {
            Contractor? found = await _contractorService.SearchByProfileAsync(
                result.ContractorName, result.ContractorNip, result.ContractorAddress,
                request.TenantId, cancellationToken);

            if (found is not null)
            {
                result = result with { ContractorId = found.Id, ContractorFound = true };
            }
        }

        return result;
    }
}
```

---

## BLOK 5 — Zmiany w kontrolerach

| Endpoint | HTTP Method | Nowy/Modyfikacja | Opis |
|----------|-------------|-----------------|------|
| `api/tenants/{tenantId}/projects/{projectId}/ai/cost/parse` | `POST` | **Nowy kontroler** | `AICostController`, `[FromForm]`, przyjmuje `ParseCostDocumentQuery` z pliku + CostType, zwraca `ParsedCostDto` |

### Czy nowy kontroler czy istniejący?

**Nowy `AICostController`** — uzasadnienie:
- `CostTrackerController` odpowiada za CRUD TrackedCost, nie AI
- `ProjectCostController` odpowiada za CRUD ProjectCost, nie AI
- Route `ai/cost/parse` semantycznie należy do osobnej domeny AI
- W przyszłości controller będzie rozbudowywany o kolejne AI endpointy

```csharp
[ApiController]
[Route("api/tenants/{tenantId:guid}/projects/{projectId:guid}/ai/cost")]
public class AICostController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpPost("parse")]
    [Authorize(Policy = PermissionCodes.ProjectDashboardTracker)]
    [RequestSizeLimit(20971520)]      // 20 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 20971520)]
    [ProducesResponseType(typeof(ParsedCostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ParseDocument(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid projectId,
        [FromForm] IFormFile file,
        [FromForm] CostDocumentType costType = CostDocumentType.TrackedCost)
    {
        ParseCostDocumentQuery query = new()
        {
            TenantId = tenantId,
            ProjectId = projectId,
            File = file,
            CostType = costType
        };
        ParsedCostDto result = await Send(query);
        return Ok(result);
    }
}
```

---

## BLOK 6 — Zmiany w serwisach

| Serwis | Interfejs | Nowy/Modyfikacja | Metody |
|--------|-----------|-----------------|--------|
| `IDocumentParserService` | `Business/Interfaces/Services/IDocumentParserService.cs` | **Nowy** | `Task<ParsedCostDto> ParseAsync(byte[] fileBytes, string mediaType, Guid tenantId, CancellationToken ct)` |
| `DocumentParserService` | Implementacja w `Business.AIAgent/Services/DocumentParserService.cs` | **Nowy** | Wstrzykuje `IOptions<AzureAIAgentOptions>`. Buduje `AzureOpenAIClient` → `ChatClient("gpt-4o")`. Tworzy `UserChatMessage` z `CreateImagePart(BinaryData, mediaType)`. Deserializuje odpowiedź JSON → `ParsedCostDto`. |
| `IContractorService` | `Business/Interfaces/Services/IContractorService.cs` | **Modyfikacja** | Dodać metodę: `Task<Contractor?> SearchByProfileAsync(string? name, string? taxId, string? address, Guid tenantId, CancellationToken ct)` |
| `ContractorService` | `Business/Implementation/Services/ContractorService.cs` | **Modyfikacja** | Implementacja `SearchByProfileAsync` — szuka po Name LIKE, TaxId ==, Street LIKE (wszystko OR). Zwraca `null` gdy brak dopasowania. |

### Jak wstrzyknąć OpenAI client w DocumentParserService

```csharp
// Business.AIAgent/Services/DocumentParserService.cs
public sealed class DocumentParserService : IDocumentParserService
{
    private readonly AzureAIAgentOptions _options;
    private readonly ILogger<DocumentParserService> _logger;

    public DocumentParserService(
        IOptions<AzureAIAgentOptions> options,
        ILogger<DocumentParserService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ParsedCostDto> ParseAsync(
        byte[] fileBytes, string mediaType, Guid tenantId, CancellationToken cancellationToken)
    {
        // Buduj klienta (ten sam wzorzec co AgentRunner.BuildChatClient)
        AzureOpenAIClient azureClient = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? new AzureOpenAIClient(new Uri(_options.Endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(_options.Endpoint), new ApiKeyCredential(_options.ApiKey));

        ChatClient client = azureClient.GetChatClient(_options.DefaultDeployment);

        // Buduj wiadomość z obrazem
        ChatMessageContentPart imagePart = ChatMessageContentPart.CreateImagePart(
            BinaryData.FromBytes(fileBytes), mediaType);

        ChatMessageContentPart textPart = ChatMessageContentPart.CreateTextPart(
            "Wyciągnij dane kosztowe z dokumentu. Odpowiedz TYLKO w JSON...");

        List<ChatMessage> messages =
        [
            new SystemChatMessage("Jesteś ekspertem od odczytywania faktur i rachunków..."),
            new UserChatMessage(imagePart, textPart)
        ];

        ChatCompletion response = await client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        string json = response.Content[0].Text;

        return DeserializeParsedCostDto(json);
    }
}
```

**Uwaga**: `DocumentParserService` jest rejestrowany w `AIAgentServiceExtensions.AddAIAgent()`:
```csharp
services.AddScoped<IDocumentParserService, DocumentParserService>();
```

### Jak wyszukiwać kontrahentów — rozszerzony IContractorService

```csharp
// Nowa metoda w IContractorService:
Task<Contractor?> SearchByProfileAsync(
    string? name, string? taxId, string? address,
    Guid tenantId, CancellationToken cancellationToken);

// Implementacja w ContractorService:
public async Task<Contractor?> SearchByProfileAsync(
    string? name, string? taxId, string? address,
    Guid tenantId, CancellationToken cancellationToken)
{
    IEnumerable<Contractor> all = await contractorRepo.GetBySearch(
        c => c.TenantId == tenantId && !c.IsDeleted, cancellationToken);

    // Szukaj po TaxId (najdokładniejsze)
    if (!string.IsNullOrWhiteSpace(taxId))
    {
        Contractor? byTaxId = all.FirstOrDefault(c =>
            c.TaxId != null && c.TaxId.Replace("-", "").Replace(" ", "") ==
            taxId.Replace("-", "").Replace(" ", ""));
        if (byTaxId is not null) return byTaxId;
    }

    // Szukaj po nazwie (fuzzy)
    if (!string.IsNullOrWhiteSpace(name))
    {
        Contractor? byName = all.FirstOrDefault(c =>
            c.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        if (byName is not null) return byName;
    }

    return null;
}
```

### Wzorzec ParseCostDocumentTool (dla agent loop)

```csharp
// Business.AIAgent/Tools/CostDocument/ParseCostDocumentTool.cs
public sealed class ParseCostDocumentTool : AgentToolBase
{
    private readonly IDocumentParserService _parserService;
    // TenantId pochodzi z AgentContext

    public override string Name => "parse_cost_document";
    public override string Description =>
        "Parses a cost document (invoice/receipt) encoded as base64 and extracts cost data.";

    public override JsonElement ParametersSchema => BuildSchema("""
        {
          "type": "object",
          "properties": {
            "image_base64": { "type": "string", "description": "Base64 encoded JPG/PNG image" },
            "media_type":   { "type": "string", "description": "image/jpeg or image/png" }
          },
          "required": ["image_base64", "media_type"]
        }
        """);

    public override async Task<ToolResult> ExecuteAsync(
        JsonElement arguments, AgentContext context, CancellationToken cancellationToken)
    {
        string? base64 = GetString(arguments, "image_base64");
        string? mediaType = GetString(arguments, "media_type");

        if (string.IsNullOrWhiteSpace(base64) || string.IsNullOrWhiteSpace(mediaType))
            return ToolResult.Failure("image_base64 and media_type are required");

        byte[] bytes = Convert.FromBase64String(base64);
        ParsedCostDto result = await _parserService.ParseAsync(bytes, mediaType, context.TenantId, cancellationToken);
        return ToolResult.Success(JsonSerializer.Serialize(result));
    }
}
```

---

## BLOK 7 — Problemy i ryzyka

| # | Problem | Warstwa | Ryzyko | Rekomendacja |
|---|---------|---------|--------|-------------|
| 1 | **CQRS nie referuje Business.AIAgent** | Architektura | Wysoki | Wprowadzić `IDocumentParserService` w `Business/Interfaces/Services/` jako warstwę pośrednią. CQRS używa interfejsu, Business.AIAgent go implementuje. Dodać `<ProjectReference Include="..\Business\Business.csproj" />` do `Business.AIAgent.csproj`. |
| 2 | **Brak biblioteki PDF** | Business.AIAgent | **Blocker dla PDF** | Azure OpenAI SDK 2.2.0-beta.4 NIE obsługuje PDF bezpośrednio jako content part. Opcje: (a) dodać `UglyToad.PdfPig` do ekstrakcji tekstu (bez Vision, tylko OCR-like), (b) dodać `PDFiumSharp` / `Docnet.Core` do renderowania pierwszej strony jako bitmap, (c) użyć Azure Document Intelligence. Decyzja wymagana przed implementacją. |
| 3 | **Azure.AI.OpenAI 2.2.0-beta.4** | Business.AIAgent | Średni | Wersja beta. `ChatMessageContentPart.CreateImagePart(BinaryData, string)` jest dostępna w tej wersji — sprawdzone. Jednak API może ulec zmianie przy GA. |
| 4 | **AgentRunner nie obsługuje Vision** | Business.AIAgent | Niski | `AgentRunner.RunStreamingAsync` przyjmuje tylko `string userMessage`. Dla `DocumentParserService` NIE używamy `AgentRunner` — tworzymy `ChatClient` bezpośrednio. To jest intencjonalne (one-shot, bez tool-calling loop). |
| 5 | **`IFormFile` w Query** | CQRS | Niski | `IFormFile` w Query jest akceptowalne (tak samo jak w Command — `TrackedCostCommandBase.NewFiles`). Handler odpowiada za natychmiastowe odczytanie stream do `byte[]`. |
| 6 | **Wybór PermissionCode** | CQRS/WebApi | Niski | Feature integruje się zarówno z TrackedCost jak i ProjectCost. Rekomendacja: używać `PermissionCodes.ProjectDashboardTracker` (niższy próg dostępu). Ewentualnie nowy `PermissionCodes.AIFeatures` — do decyzji. |
| 7 | **JSON prompt injection w odpowiedzi AI** | Business.AIAgent | Średni | GPT-4o może zwrócić nieoczekiwany JSON lub inny format. `DocumentParserService.DeserializeParsedCostDto()` musi być odporna na błędy (try/catch, JsonException), zwracać `ParsedCostDto` z partial data zamiast rzucać wyjątek. |
| 8 | **Timeout dla Vision API** | Business.AIAgent | Niski | Duże obrazy mogą wydłużyć czas odpowiedzi. Ustawić jawny `CancellationToken` z rozsądnym timeout (np. 30s) w kontrolerze lub handlerze. |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe encje | 0 |
| Nowe web modele (DTO) | 1 (`ParsedCostDto`) |
| Nowe Queries | 1 (`ParseCostDocumentQuery`) |
| Nowe Commands | 0 |
| Nowe kontrolery | 1 (`AICostController`) |
| Nowe serwisy (interfejsy) | 1 (`IDocumentParserService`) |
| Nowe serwisy (implementacje) | 1 (`DocumentParserService`) |
| Modyfikacje istniejących serwisów | 1 (`IContractorService` + `ContractorService`) |
| Nowe AI Tools | 1 (`ParseCostDocumentTool`) |
| Nowe Agent Definitions | 1 (`cost_document_parser.md`) |
| Wymaga migracji DB | **Nie** |
| Wymaga zmiany `.csproj` | **Tak** (Business.AIAgent → dodać ref do Business) |
| Blocker | **Tak** — brak biblioteki PDF |
| Pytania domenowe | 3 |

---

## Pytania domenowe wymagające decyzji

1. **PDF — strategia obsługi**: Czy PDF jest konwertowany do obrazu (Vision) czy tekstu (text extraction)?  
   - Opcja A: `UglyToad.PdfPig` — ekstrakcja tekstu, bez Vision (taniej, szybciej, gorsza jakość dla dokumentów graficznych)  
   - Opcja B: `PDFiumSharp` / `Docnet.Core` — renderowanie strony PDF do bitmap → Vision (pełna jakość, wymaga natywnych bibliotek)  
   - Opcja C: Azure Document Intelligence — dedykowany serwis do ekstrakcji danych z faktur (Form Recognizer), ale wymaga dodatkowej konfiguracji Azure + kosztów

2. **PermissionCode dla endpointu AI**: Czy używać `PermissionCodes.ProjectDashboardTracker` (oba moduły), czy stworzyć nowy `PermissionCodes.AIFeatures` / połączyć `ProjectDashboardTracker || ProjectCosts`?

3. **Wyszukiwanie kontrahenta — próg dopasowania**: Czy wystarczy dopasowanie po NIP lub nazwie (fuzzy), czy potrzebne bardziej zaawansowane matching (np. Levenshtein distance)? Czy dopasowanie adresu (Street) ma być brane pod uwagę przy braku NIP i nazwy?
