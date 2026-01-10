# Business.AIAgent - AI Agent Framework

Framework do budowania agentów AI opartych na Azure OpenAI z możliwością wywoływania narzędzi (tools).

## Architektura

```
UI -> API -> CQRS Command Handler -> IOrchestrator -> IAgentRunner (pętla) -> IAzureOpenAIClient
                                                              ↓
                                                         ITool (wykonanie)
```

## Główne komponenty

### 1. **IOrchestrator**
- Punkt wejścia z CQRS Command Handlerów
- Wybiera odpowiednie narzędzia i promptsy na podstawie zadania
- Zarządza kontekstem wykonania

### 2. **IAgentRunner**
- Główna pętla agenta
- Wywołuje LLM, wykonuje narzędzia, powtarza do zakończenia
- Kontroluje max iteracji i timeout

### 3. **IAzureOpenAIClient**
- Abstrakcja komunikacji z Azure OpenAI
- Obsługuje konwersję między generycznymi modelami a SDK Azure
- Wspiera streaming (dla przyszłych rozszerzeń)

### 4. **ITool**
- Interfejs dla narzędzi które agent może wywoływać
- Automatyczne odkrywanie przez DI
- JSON Schema dla walidacji parametrów

## Generyczne modele komunikacji

Framework używa własnych, serializowalnych modeli zamiast typów SDK:

- **LLMMessage** - wiadomość w konwersacji (system/user/assistant/tool)
- **LLMRequest** - żądanie do LLM z historią i narzędziami
- **LLMResponse** - odpowiedź z LLM z metrykami
- **ToolCall** - wywołanie narzędzia przez LLM
- **ToolResult** - wynik wykonania narzędzia

### Dlaczego własne modele?

✅ **Serializacja** - można cache'ować i logować całe konwersacje  
✅ **Niezależność** - nie jesteśmy związani z konkretnym SDK  
✅ **Testowanie** - łatwiejsze mockowanie  
✅ **Rozszerzalność** - możemy dodawać własne pola (metadata)  

## Konfiguracja

### appsettings.json

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "",  // opcjonalne, jeśli UseManagedIdentity=false
    "DeploymentName": "gpt-4o",
    "MaxTokens": 1000,
    "Temperature": 0.7,
    "TopP": null,
    "MaxIterations": 10,
    "TimeoutSeconds": 60,
    "UseManagedIdentity": true  // rekomendowane w produkcji
  }
}
```

### Rejestracja w DI

```csharp
// W Program.cs lub ServiceCollectionExtensions.cs
services.AddAIAgent(configuration);

// Zarejestruj narzędzia
services.AddTool<GetCurrentDateTimeTool>();
services.AddTool<YourCustomTool>();

// Lub wiele na raz
services.AddTools(
    typeof(GetCurrentDateTimeTool),
    typeof(SearchDatabaseTool),
    typeof(SendEmailTool)
);
```

## Użycie w CQRS Command Handler

### Przykład: Prosty agent bez narzędzi

```csharp
public class AskAICommandHandler : IRequestHandler<AskAICommand, string>
{
    private readonly IOrchestrator orchestrator;

    public AskAICommandHandler(IOrchestrator orchestrator)
    {
        this.orchestrator = orchestrator;
    }

    public async Task<string> Handle(AskAICommand request, CancellationToken cancellationToken)
    {
        var systemPrompt = "You are a helpful assistant for project management tasks.";
        var userQuery = request.Question;

        var result = await orchestrator.ExecuteAsync(
            systemPrompt,
            userQuery,
            toolNames: null,  // bez narzędzi
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            throw new Exception($"AI Agent failed: {result.Error}");
        }

        return result.FinalMessage.Content ?? string.Empty;
    }
}
```

### Przykład: Agent z narzędziami

```csharp
public class AnalyzeProjectCommandHandler : IRequestHandler<AnalyzeProjectCommand, ProjectAnalysisWeb>
{
    private readonly IOrchestrator orchestrator;
    private readonly ICurrentUser currentUser;

    public async Task<ProjectAnalysisWeb> Handle(AnalyzeProjectCommand request, CancellationToken ct)
    {
        var systemPrompt = @"
You are a project analysis expert. Analyze the given project data and provide insights.
Use the available tools to gather additional information when needed.
Return your analysis as structured JSON.";

        var userQuery = $"Analyze project {request.ProjectId} and identify potential risks.";

        // Wybierz konkretne narzędzia
        var toolNames = new[] { "get_project_details", "get_project_costs", "get_work_schedule" };

        var additionalContext = new Dictionary<string, object>
        {
            { "UserId", currentUser.Id },
            { "TenantId", currentUser.ActiveTenantId },
            { "ProjectId", request.ProjectId }
        };

        var result = await orchestrator.ExecuteAsync(
            systemPrompt,
            userQuery,
            toolNames,
            additionalContext,
            ct);

        if (!result.Success)
        {
            throw new Exception($"Analysis failed: {result.Error}");
        }

        // Parse JSON response
        var analysis = JsonSerializer.Deserialize<ProjectAnalysisWeb>(
            result.FinalMessage.Content ?? "{}");

        return analysis ?? throw new Exception("Invalid response format");
    }
}
```

### Przykład: Kontynuacja konwersacji

```csharp
public class ContinueChatCommandHandler : IRequestHandler<ContinueChatCommand, ChatMessageWeb>
{
    private readonly IOrchestrator orchestrator;
    private readonly IChatHistoryRepository chatHistory;

    public async Task<ChatMessageWeb> Handle(ContinueChatCommand request, CancellationToken ct)
    {
        // Załaduj historię konwersacji
        var conversationHistory = await chatHistory.GetMessagesAsync(request.ChatId);

        var result = await orchestrator.ContinueConversationAsync(
            conversationHistory,
            request.NewMessage,
            toolNames: new[] { "search_knowledge_base", "get_user_info" },
            ct);

        // Zapisz nową wiadomość
        await chatHistory.AddMessageAsync(request.ChatId, result.FinalMessage);

        return MapToWeb(result.FinalMessage);
    }
}
```

## Tworzenie własnego narzędzia (ITool)

### Prosty przykład

```csharp
public class CalculatorTool : ToolBase
{
    public override string Name => "calculator";

    public override string Description => 
        "Performs basic mathematical operations: add, subtract, multiply, divide.";

    public override object GetParametersSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                operation = new
                {
                    type = "string",
                    description = "Mathematical operation",
                    @enum = new[] { "add", "subtract", "multiply", "divide" }
                },
                a = new { type = "number", description = "First number" },
                b = new { type = "number", description = "Second number" }
            },
            required = new[] { "operation", "a", "b" }
        };
    }

    public override async Task<ToolResult> ExecuteAsync(string arguments, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var args = JsonSerializer.Deserialize<CalculatorArgs>(arguments);
            
            double result = args.Operation switch
            {
                "add" => args.A + args.B,
                "subtract" => args.A - args.B,
                "multiply" => args.A * args.B,
                "divide" when args.B != 0 => args.A / args.B,
                "divide" => throw new DivideByZeroException(),
                _ => throw new ArgumentException($"Unknown operation: {args.Operation}")
            };

            return ToolResult.Success(
                string.Empty,
                Name,
                JsonSerializer.Serialize(new { result }),
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(string.Empty, Name, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    private class CalculatorArgs
    {
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty;

        [JsonPropertyName("a")]
        public double A { get; set; }

        [JsonPropertyName("b")]
        public double B { get; set; }
    }
}
```

### Zaawansowany przykład - Narzędzie z dostępem do bazy danych

```csharp
public class SearchProjectsTool : ToolBase
{
    private readonly IReadRepository<Project> projectRepo;
    private readonly ICurrentUser currentUser;
    private readonly ILogger<SearchProjectsTool> logger;

    public SearchProjectsTool(
        IReadRepository<Project> projectRepo,
        ICurrentUser currentUser,
        ILogger<SearchProjectsTool> logger)
    {
        this.projectRepo = projectRepo;
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public override string Name => "search_projects";

    public override string Description => 
        "Searches for projects by name or filters. Returns list of matching projects.";

    public override object GetParametersSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                query = new
                {
                    type = "string",
                    description = "Search query (project name or description)"
                },
                isActive = new
                {
                    type = "boolean",
                    description = "Filter by active status (optional)"
                },
                limit = new
                {
                    type = "number",
                    description = "Maximum number of results (default 10, max 50)"
                }
            },
            required = new[] { "query" }
        };
    }

    public override async Task<ToolResult> ExecuteAsync(string arguments, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var args = JsonSerializer.Deserialize<SearchProjectsArgs>(arguments);
            
            // Tenant isolation
            var tenantId = currentUser.ActiveTenantId;

            // Build query
            var query = projectRepo.GetAll()
                .Where(p => p.TenantId == tenantId && !p.IsDeleted)
                .Where(p => p.Name.Contains(args.Query) || p.Description.Contains(args.Query));

            if (args.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == args.IsActive.Value);
            }

            var limit = Math.Min(args.Limit ?? 10, 50);
            var projects = await query.Take(limit).ToListAsync(ct);

            var results = projects.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                description = p.Description,
                isActive = p.IsActive,
                createdAt = p.CreatedAt
            });

            stopwatch.Stop();

            logger.LogInformation("Found {Count} projects for query: {Query}", 
                projects.Count, args.Query);

            return ToolResult.Success(
                string.Empty,
                Name,
                JsonSerializer.Serialize(new { projects = results, count = projects.Count }),
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Error searching projects");
            return ToolResult.Failure(string.Empty, Name, ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    private class SearchProjectsArgs
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("limit")]
        public int? Limit { get; set; }
    }
}
```

## Flow wykonania

1. **CQRS Command Handler** wywołuje `IOrchestrator.ExecuteAsync()`
2. **Orchestrator** wybiera narzędzia i buduje konwersację
3. **AgentRunner** rozpoczyna pętlę:
   - Wywołuje LLM z historią konwersacji + dostępnymi toolami
   - LLM decyduje: odpowiedź końcowa lub wywołanie tooli
   - Jeśli tool calls → wykonuje równolegle wszystkie toole
   - Dodaje wyniki do konwersacji i wraca do LLM
   - Powtarza aż do natural completion lub max iterations
4. Zwraca **AgentRunResult** z kompletną historią i metrykami

## Metryki i diagnostyka

`AgentRunResult` zawiera:
- `FinalMessage` - końcowa odpowiedź agenta
- `ConversationHistory` - pełna historia (cache'owalna)
- `IterationCount` - ile razy wywołano LLM
- `TotalTokensUsed` - całkowite zużycie tokenów
- `TotalExecutionTimeMs` - całkowity czas wykonania
- `ToolResults` - wszystkie wykonane narzędzia z czasami
- `LLMResponses` - szczegółowe logi wszystkich wywołań LLM

## Best Practices

### 1. System Prompts
- Jasno definiuj role i zachowanie agenta
- Podaj przykłady oczekiwanych odpowiedzi
- Określ format output (JSON, markdown, etc.)

### 2. Tool Naming
- Używaj snake_case: `get_project_details`, `send_email`
- Nazwy powinny być opisowe i jednoznaczne
- Unikaj skrótów

### 3. Tool Descriptions
- Pisz jasne, konkretne opisy co tool robi
- Podaj przykłady użycia
- Określ kiedy NIE używać tego toola

### 4. Error Handling
- Zawsze catch wyjątki w narzędziach
- Zwracaj `ToolResult.Failure` z czytelnym komunikatem
- Loguj błędy dla diagnostyki

### 5. Tenant Isolation
- **ZAWSZE** filtruj dane po `TenantId` w toolach
- Używaj `ICurrentUser.ActiveTenantId`
- Nie ufaj parametrom z LLM - zawsze waliduj uprawnienia

### 6. Performance
- Narzędzia powinny być szybkie (< 1s)
- Używaj async/await konsekwentnie
- Rozważ cache dla często używanych danych
- Limit wyników (max 50-100 rekordów)

## Rozszerzenia (Roadmap)

- [ ] Streaming responses (real-time)
- [ ] Conversation memory/context windows
- [ ] Multi-agent orchestration
- [ ] Tool chain composition
- [ ] Rate limiting per user/tenant
- [ ] Cost tracking per tenant
- [ ] A/B testing różnych promptów
- [ ] Integration z Semantic Kernel

## Security

⚠️ **WAŻNE: Multi-tenancy**
- Wszystkie toole MUSZĄ respektować tenant isolation
- Nigdy nie ufaj parametrom z LLM - zawsze waliduj
- Używaj `ICurrentUser` do weryfikacji uprawnień

⚠️ **Zarządzanie tokenami Azure OpenAI**
- W produkcji używaj Managed Identity (`UseManagedIdentity: true`)
- Jeśli API Key - przechowuj w Azure Key Vault
- Regularnie rotuj klucze API

⚠️ **Content Filtering**
- Azure OpenAI ma wbudowane filtry bezpieczeństwa
- Obsługuj `FinishReason.ContentFilter`
- Nie przekazuj wrażliwych danych do LLM

## Przykładowe scenariusze użycia

### 1. Automatyczna analiza projektu
Tools: `get_project_details`, `get_work_schedule`, `get_project_costs`  
Output: Raport z insights i rekomendacjami

### 2. Asystent planowania prac
Tools: `search_projects`, `get_team_members`, `check_availability`, `create_work_schedule`  
Output: Propozycja harmonogramu z przypisaniami

### 3. Chatbot supportu
Tools: `search_knowledge_base`, `get_user_tickets`, `create_ticket`, `send_notification`  
Output: Interaktywna pomoc z możliwością eskalacji

### 4. Generowanie kosztorysów
Tools: `get_cost_template`, `search_materials`, `calculate_labor_cost`  
Output: Wygenerowany kosztorys na podstawie szablonu

## Debugging

### Włącz szczegółowe logi

```csharp
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Business.AIAgent": "Debug",
      "Business.AIAgent.Services.AgentRunner": "Trace"
    }
  }
}
```

### Sprawdź conversation history

```csharp
var result = await orchestrator.ExecuteAsync(...);

// Zapisz do pliku dla analizy
var historyJson = JsonSerializer.Serialize(result.ConversationHistory, new JsonSerializerOptions
{
    WriteIndented = true
});
await File.WriteAllTextAsync("conversation.json", historyJson);
```

### Monitoruj tokeny i koszty

```csharp
var totalCost = result.TotalTokensUsed * 0.00001; // przykładowa cena za token
logger.LogInformation("Agent used {Tokens} tokens, estimated cost: ${Cost:F4}", 
    result.TotalTokensUsed, totalCost);
```

## Support

W razie pytań lub problemów:
1. Sprawdź logi w Application Insights
2. Zweryfikuj konfigurację w appsettings
3. Testuj toole osobno przed integracją
4. Użyj debuggera do analizy conversation flow
