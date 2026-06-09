# API Audit Report — AI Schedule Generator

**Feature**: `ai-schedule-generator`
**Date**: 2026-06-09
**Scope**: Pełny audyt warstwy API pod kątem implementacji generowania harmonogramu z kosztorysu wspieranego przez AI.

---

## BLOK 1 — Stan obecny

### 1.1 Architektura AI Agent (Business.AIAgent)

**Struktura narzędzia (Tool):**
- Każde narzędzie dziedziczy po `AgentToolBase : IAgentTool`
- Wymagane właściwości: `Name` (string), `Description` (string), `ParametersSchema` (JsonElement — JSON Schema)
- Metoda: `Task<ToolResult> ExecuteAsync(JsonElement arguments, AgentContext context, CancellationToken)`
- Helper metody w base: `GetString()`, `GetGuid()`, `GetInt()`, `BuildSchema(json)`
- `ToolResult` — `Success(string)` / `Failure(string)`
- `AgentContext` — SessionId, TenantId, UserId, ProjectId, Depth, BearerToken, OnEvent callback

**Rejestracja narzędzi:**
- W `AIAgentServiceExtensions.AddAIAgent()`:
  ```csharp
  services.AddScoped<IAgentTool, GetProjectInfoTool>();
  services.AddScoped<IAgentTool, GetCostEstimateItemsTool>();
  // etc.
  ```
- `ToolRegistry` (DI) zbiera wszystkie `IEnumerable<IAgentTool>` i udostępnia przez `GetAllowedTools(allowedNames)`

**AgentDefinition:**
- Ładowany z embedded .md resources: `Business.AIAgent.Resources.Agents.sub_agents.{name}.md`
- YAML frontmatter: `name`, `description`, `model` (default: gpt-4o), `temperature`, `max_tokens`, `max_iterations`, `tools` (lista), `sub_agents` (lista)
- Ciało pliku = SystemPrompt

**AgentRunner:**
- `IAgentRunner.RunAsync(agentName, userMessage, context)` → `AgentRunResult { IsSuccess, Response, ErrorMessage, Iterations }`
- Streaming przez `RunStreamingAsync()`
- Tool call execution przez `ToolCallExecutor`

### 1.2 Istniejący agent `work-schedule-agent`

- **Plik**: `Business.AIAgent.Resources.Agents.sub_agents.work_schedule_agent.md`
- **Model**: gpt-4o, temp: 0.3, max_tokens: 2048, max_iterations: 6
- **Tools**: `get_work_schedule`, `get_project_info`
- **Role**: Specialist in work schedules — tylko odczyt, brak narzędzi do zapisu

### 1.3 Istniejący `main_orchestrator`

- **Plik**: `Business.AIAgent.Resources.Agents.main_orchestrator.md`
- **Tools**: `call_sub_agent`, `get_project_info`, `http_fetch`
- **Sub-agents**: `cost-estimate-agent`, `work-schedule-agent`, `project-agent`
- **Model**: gpt-4o, temp: 0.7

### 1.4 Istniejące encje WorkSchedule

| Encja | Kluczowe pola | Uwagi |
|-------|--------------|-------|
| `WorkSchedule` | `TenantId`, `ProjectId`, `Name`, `CostEstimateId?`, `Stages`, `Dependencies` | `DeletableEntity` |
| `WorkScheduleStage` | `TenantId`, `ProjectId`, `WorkScheduleId`, `ParentStageId?`, `Name`, `Order`, `CostEstimateGroupId?` | `DeletableEntity`; ma `Works` i `ChildStages` |
| `WorkScheduleStageWork` | `TenantId`, `ProjectId`, `WorkScheduleStageId`, `CostEstimateItemId?`, `Name`, `Order`, `ColorRgb`, `PlannedStartDate?`, `PlannedEndDate?` | `DeletableEntity`; ma `Periods`, `Assignments`, `Comments`, `PredecessorDependencies`, `SuccessorDependencies` |
| `WorkScheduleStageWorkPeriod` | `TenantId`, `ProjectId`, `WorkScheduleStageWorkId`, `StartDate`, `EndDate`, `IsClosed` | `BaseEntity` |
| `WorkScheduleStageWorkDependency` | `TenantId`, `ProjectId`, `WorkScheduleId`, `PredecessorWorkId`, `SuccessorWorkId`, `DependencyType`, `LagDays` | `BaseEntity`; unikalny indeks na (WorkScheduleId, PredecessorWorkId, SuccessorWorkId) |

**`WorkDependencyType` enum:**
- `FinishToStart = 0` (domyślny)
- `StartToStart = 1`
- `FinishToFinish = 2`
- `StartToFinish = 3`

### 1.5 Istniejące CQRS

**`SyncWorkScheduleWithEstimateCommand` / Handler:**
- Bazuje na `WorkScheduleCommandBase : IRequestCommand<Unit>`
- PermissionCode: `PermissionCodes.ProjectSchedule`
- Weryfikuje `CostEstimateId.HasValue`, sprawdza dostęp przez `costEstimateAccessService`
- Wywołuje `WorkScheduleSyncService.SyncFromCostEstimateAsync()`
- Unieważnia cache przez `scheduleCache.InvalidateScheduleAsync()`

**`SetWorkScheduleStageWorkPeriodsCommand` / Handler:**
- Bazuje na `WorkScheduleStageWorkCommandBase : IRequestCommand<Unit>`
- `List<WorkPeriodDto> Periods` — DTO: `StartDate`, `EndDate`, `IsClosed`
- Kasuje wszystkie istniejące periody dla worka, wstawia nowe
- Aktualizuje `Work.PlannedStartDate` / `PlannedEndDate` (denormalizowane)
- Waliduje constraints zależności (nie można ustawić okresu wcześniej niż poprzednik)
- Unieważnia cache

**`SetWorkScheduleDependenciesCommand` / Handler:**
- Bazuje na `WorkScheduleCommandBase : IRequestCommand<WorkScheduleDetailsWeb>`
- `List<WorkDependencyDto> Dependencies` — DTO: `PredecessorWorkId`, `SuccessorWorkId`, `DependencyType`, `LagDays`
- Strategia diff: istniejące vs incoming → delete/add/update
- Automatycznie przesuwa okresy sukcesorów (algorytm Kahna — sortowanie topologiczne)
- Zwraca `WorkScheduleDetailsWeb` przez `WorkScheduleBuilder.BuildAsync()`
- Unieważnia cache

### 1.6 WorkScheduleSyncService

- `SyncFromCostEstimateAsync(WorkSchedule, CancellationToken)`:
  1. Ładuje wszystkie grupy z kosztorysu
  2. Soft-delete stages których grupy nie istnieją
  3. Buduje hierarchię grup (root + child)
  4. Tworzy/aktualizuje stage dla każdej grupy
  5. Ładuje itemy z kosztorysu, filtruje `IsWorkScopeItem`
  6. Tworzy/aktualizuje `WorkScheduleStageWork` dla każdego scope itemu
  7. Soft-delete obsolete worków
- Używa `IRepository<T>` bezpośrednio, nie używa CQRS wewnętrznie

### 1.7 WorkScheduleController — wzorzec endpointów

- **Route**: `api/tenants/{tenantId}/projects/{projectId}/work-schedule`
- **Auth**: `[Authorize(Policy = PermissionCodes.ProjectSchedule)]`
- **Wzorzec**: `POST/PUT/DELETE` → command z `with { TenantId, ProjectId, WorkScheduleId }`
- **Zwracane statusy**: `CreatedAtAction`, `NoContent`, `Ok(result)`
- **Endpointy istniejące**:
  - `POST /` — CreateWorkSchedule
  - `PUT /{workScheduleId}` — UpdateWorkSchedule
  - `GET /{scope}` — GetWorkSchedules
  - `GET /details/{workScheduleId}` — GetWorkSchedule
  - `POST /{workScheduleId}/sync-with-estimate` — SyncWorkScheduleWithEstimate
  - `DELETE /{workScheduleId}` — DeleteWorkSchedule
  - `PUT /{workScheduleId}/dependencies` — SetDependencies

### 1.8 IWorkScheduleCacheService

```csharp
Task<WorkScheduleDetailsWeb?> GetOrBuildScheduleAsync(Guid workScheduleId, Func<Task<WorkScheduleDetailsWeb>> factory, CancellationToken ct);
Task InvalidateScheduleAsync(Guid workScheduleId, CancellationToken ct);
Task InvalidateWorkAsync(Guid workScheduleId, Guid workId, CancellationToken ct);
```

### 1.9 IWorkScheduleAccessService

```csharp
Task RequireAdminOrOwnerAsync(Guid tenantId, Guid projectId, Guid workScheduleId, CancellationToken ct);
Task RequireAdminOwnerOrAssignedAsync(Guid tenantId, Guid projectId, Guid workScheduleId, Guid workScheduleStageWorkId, CancellationToken ct);
```

### 1.10 Web Model `WorkScheduleDetailsWeb`

```csharp
sealed record WorkScheduleDetailsWeb(
    Guid Id, Guid TenantId, Guid ProjectId, Guid? CostEstimateId,
    string Name, DateTime CreatedAt, Guid CreatedByUserId, string CreatedByUserName,
    List<WorkScheduleStageWeb> Stages,
    List<WorkScheduleWorkDependencyWeb> Dependencies
);

sealed record WorkScheduleStageWeb(
    Guid Id, string Name, int Order, Guid? ParentStageId, Guid? CostEstimateGroupId,
    List<WorkScheduleStageWorkWeb> Works,
    List<WorkScheduleStageWeb> ChildStages
);

sealed record WorkScheduleStageWorkWeb(
    Guid Id, Guid? CostEstimateItemId, string Name, int Order, string ColorRgb, bool IsClosed,
    DateTime? PlannedStartDate, DateTime? PlannedEndDate,
    List<WorkScheduleStageWorkPeriodWeb> Periods,
    List<WorkScheduleStageWorkAssigneeWeb> Assignees,
    List<WorkScheduleStageWorkCommentWeb> Comments
);

sealed record WorkScheduleStageWorkPeriodWeb(
    Guid Id, DateTime StartDate, DateTime EndDate, bool IsClosed
);

sealed record WorkScheduleWorkDependencyWeb(
    Guid Id, Guid PredecessorWorkId, Guid SuccessorWorkId,
    WorkDependencyType DependencyType, int LagDays
);
```

---

## BLOK 2 — Luki i braki

| Brak / Luka | Warstwa | Priorytet | Opis |
|-------------|---------|----------|------|
| Nowe narzędzie AI `analyze_schedule_structure` | AI Agent | HIGH | Narzędzie przyjmujące grupy+itemy+ramy czasowe, zwracające durations i dependencies |
| Nowy agent `schedule-generator-agent` | AI Agent | HIGH | Specjalistyczny agent do generowania harmonogramu, używa nowego narzędzia |
| Nowy serwis `IWorkScheduleAIGeneratorService` | Business | HIGH | Koordynuje wywołanie AI agenta i parsuje odpowiedź |
| Nowy CQRS `GenerateScheduleFromEstimateAICommand` | CQRS | HIGH | Command + Handler + Validator |
| Nowy endpoint `POST .../generate-from-ai` | WebApi | HIGH | Endpoint w WorkScheduleController |
| Rejestracja w DI nowego serwisu i narzędzia | DI | HIGH | AIAgentServiceExtensions + ServiceCollectionExtensions |

---

## BLOK 3 — Zmiany w encjach/DB

Brak — feature używa istniejących encji:
- `WorkScheduleStageWorkPeriod` — do przechowywania okresów (durations → StartDate/EndDate)
- `WorkScheduleStageWorkDependency` — do przechowywania zależności

---

## BLOK 4 — Nowe Commands/Queries

| Command/Query | Typ | Opis | Handler |
|--------------|-----|------|---------|
| `GenerateScheduleFromEstimateAICommand` | Nowy Command | `WorkScheduleCommandBase : IRequestCommand<WorkScheduleDetailsWeb>` | `GenerateScheduleFromEstimateAICommandHandler` |

**Szczegóły handlera:**
1. Weryfikacja dostępu przez `accessService.RequireAdminOrOwnerAsync()`
2. Załadowanie `WorkSchedule` z `CostEstimateId`
3. Załadowanie `CostEstimate` z grupami (`CostEstimateGroup`) i itemami (`CostEstimateItem` — tylko work scope items)
4. Przygotowanie kontekstu: lista etapów (grupy) z nazwami i kolejnością, lista zakresów (itemy) z nazwami, etapem i kolejnością, ramy czasowe (OverallStartDate, OverallEndDate)
5. Wywołanie `IWorkScheduleAIGeneratorService.GenerateScheduleAsync()` → zwraca listę okresów i zależności
6. Zapis okresów przez istniejący handler `SetWorkScheduleStageWorkPeriodsCommand` (lub bezpośrednio przez repo)
7. Zapis zależności przez istniejący handler `SetWorkScheduleDependenciesCommand`
8. Unieważnienie cache
9. Zwrócenie `WorkScheduleDetailsWeb` (można przez `WorkScheduleBuilder.BuildAsync()` lub jako wynik z `SetWorkScheduleDependenciesCommand`)

---

## BLOK 5 — Zmiany w kontrolerach

| Endpoint | HTTP Method | Nowy/Modyfikacja | Opis |
|----------|------------|-----------------|------|
| `api/tenants/{tenantId}/projects/{projectId}/work-schedule/{workScheduleId}/generate-from-ai` | POST | Nowy | Wywołuje `GenerateScheduleFromEstimateAICommand`, zwraca `WorkScheduleDetailsWeb` |

**Wzorzec do naśladowania** (endpoint `sync-with-estimate`):
```csharp
[HttpPost("{workScheduleId}/generate-from-ai")]
[Authorize(Policy = PermissionCodes.ProjectSchedule)]
public async Task<IActionResult> GenerateFromAI(
    [FromRoute] Guid tenantId,
    [FromRoute] Guid projectId,
    [FromRoute] Guid workScheduleId,
    [FromBody] GenerateScheduleFromEstimateAICommand command)
{
    command = command with { TenantId = tenantId, ProjectId = projectId, WorkScheduleId = workScheduleId };
    WorkScheduleDetailsWeb result = await Send(command);
    return Ok(result);
}
```

---

## BLOK 6 — Zmiany w serwisach

| Serwis | Interfejs | Nowy/Modyfikacja | Metody |
|--------|-----------|-----------------|--------|
| `WorkScheduleAIGeneratorService` | `IWorkScheduleAIGeneratorService` | Nowy | `Task<AIScheduleResult> GenerateScheduleAsync(...)` |

**`IWorkScheduleAIGeneratorService`:**
```csharp
public interface IWorkScheduleAIGeneratorService
{
    /// <summary>
    /// Wywołuje AI agent do wygenerowania durations i dependencies dla harmonogramu.
    /// </summary>
    Task<AIScheduleResult> GenerateScheduleAsync(
        Guid workScheduleId,
        Guid tenantId,
        Guid projectId,
        List<StageInput> stages,
        List<WorkInput> works,
        DateTime overallStartDate,
        DateTime overallEndDate,
        CancellationToken ct);
}
```

**`AIScheduleResult` (nowy DTO w Business.Interfaces.WebModels.AI lub WorkSchedules):**
```csharp
public sealed record AIScheduleResult
{
    public List<WorkPeriodResult> Periods { get; init; } = [];
    public List<WorkDependencyResult> Dependencies { get; init; } = [];
}

public sealed record WorkPeriodResult
{
    public Guid WorkScheduleStageWorkId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
}

public sealed record WorkDependencyResult
{
    public Guid PredecessorWorkId { get; init; }
    public Guid SuccessorWorkId { get; init; }
    public WorkDependencyType DependencyType { get; init; }
    public int LagDays { get; init; }
}
```

**Sposób wywołania AI:**
- Opcja A (rekomendowana): Handler ładuje dane z repo, serwis przygotowuje prompt i wywołuje `IAgentRunner.RunAsync("schedule-generator-agent", prompt, agentContext)`, parsuje JSON odpowiedzi.
- Opcja B: Nowe narzędzie `analyze_schedule_structure` które przyjmuje dane i zwraca JSON — ale to wymaga by agent umiał wywołać narzędzie z dużym payloadem.
- **Rekomendacja**: Opcja A — handler przygotowuje pełny kontekst (grupy, itemy, ramy czasowe) w prompcie, agent zwraca JSON w odpowiedzi tekstowej, serwis parsuje.

---

## BLOK 7 — Nowy Agent i Narzędzie AI

### 7.1 Nowy agent `schedule-generator-agent`

Plik: `Business.AIAgent.Resources.Agents.sub_agents.schedule_generator_agent.md`

```yaml
---
name: schedule-generator-agent
description: Generates work schedule durations and dependencies from cost estimate data
model: gpt-4o
temperature: 0.3
max_tokens: 4096
max_iterations: 1
tools: []
---
You are a work schedule generator for the PDM platform.
Analyze the provided cost estimate structure and generate durations and dependencies...

[Prompt details — patrz sekcja Schemat odpowiedzi AI]
```

**Uwaga**: `max_iterations: 1` — bo to agent bez tool calls, tylko generuje JSON z prompta.

### 7.2 Nowe narzędzie `analyze_schedule_structure` (alternatywnie)

Jeśli zamiast osobnego agenta wolelibyśmy tool:

```csharp
public sealed class AnalyzeScheduleStructureTool : AgentToolBase
{
    public override string Name => "analyze_schedule_structure";
    public override string Description => "Analyzes cost estimate structure and suggests durations and dependencies for a work schedule within given time frame.";
    
    public override JsonElement ParametersSchema => BuildSchema("""
    {
      "type": "object",
      "properties": {
        "stages": { "type": "array", "items": { "type": "object" }, "description": "List of stages..." },
        "works": { "type": "array", "items": { "type": "object" }, "description": "List of work items..." },
        "overall_start_date": { "type": "string", "description": "Project start date (ISO 8601)" },
        "overall_end_date": { "type": "string", "description": "Project end date (ISO 8601)" }
      },
      "required": ["stages", "works", "overall_start_date", "overall_end_date"]
    }
    """);
}
```

**Rekomendacja**: użyć osobnego agenta (opcja A) — prostsze, nie wymaga tool call z dużym JSONem, prompt jest statyczny.

---

## BLOK 8 — Schemat odpowiedzi AI

Agent powinien zwrócić czysty JSON (bez markdown, bez ```).

```json
{
  "durations": [
    {
      "work_name": "Nazwa zakresu robót",
      "duration_days": 14
    }
  ],
  "dependencies": [
    {
      "predecessor_work_name": "Nazwa zakresu poprzedzającego",
      "successor_work_name": "Nazwa zakresu następującego",
      "dependency_type": "FinishToStart",
      "lag_days": 0
    }
  ]
}
```

**Kluczowe założenia:**
- `work_name` — dokładna nazwa zakresu robót (WorkScheduleStageWork.Name), po niej handler mapuje na `WorkScheduleStageWorkId`
- `duration_days` — liczba dni roboczych trwania
- `dependency_type` — jeden z: `FinishToStart`, `StartToStart`, `FinishToFinish`, `StartToFinish`
- `lag_days` — przesunięcie w dniach (ujemne = lead, dodatnie = lag)
- Handler po otrzymaniu odpowiedzi:
  1. Dla każdego worka: oblicza `StartDate` i `EndDate` na podstawie `duration_days` i pozycji w topologii (używając overallStartDate jako początku pierwszych prac)
  2. Tworzy `WorkPeriodDto` dla każdego zakresu
  3. Tworzy `WorkDependencyDto` dla każdej zależności
  4. Zapisuje przez istniejące CQRS

**Alternatywny schemat (z datami):**
```json
{
  "works": [
    {
      "work_name": "Nazwa zakresu",
      "start_date": "2026-07-01T00:00:00",
      "end_date": "2026-07-14T00:00:00"
    }
  ],
  "dependencies": [
    {
      "predecessor_work_name": "...",
      "successor_work_name": "...",
      "dependency_type": "FinishToStart",
      "lag_days": 0
    }
  ]
}
```

**Rekomendacja**: pierwszy schemat (z duration_days) — AI łatwiej wylicza dni trwania niż konkretne daty. Handler rozkłada daty proporcjonalnie w ramach ram czasowych.

---

## BLOK 9 — Problemy i ryzyka

| # | Problem | Warstwa | Ryzyko | Rekomendacja |
|---|---------|---------|--------|-------------|
| 1 | Nazwy zakresów mogą się zmienić po synchronizacji z kosztorysem (SyncWorksFromItemsAsync) | Biznesowa | Średnie — AI otrzyma stare nazwy | Handler powinien najpierw zsynchronizować (SyncWithEstimate) przed wysłaniem do AI, żeby mieć aktualne nazwy |
| 2 | AI może zwrócić work_name który nie istnieje (literówka, różnica w formatowaniu) | AI | Wysokie — błąd mapowania | Handler powinien robić `fuzzy match` (case-insensitive, trim) i logować ostrzeżenia |
| 3 | Kosztorys może mieć setki pozycji → prompt będzie bardzo długi | AI | Średnie — przekroczenie tokenów | Limitować do N itemów/grup, paginacja lub podsumowanie |
| 4 | AI może zwrócić niepoprawny JSON (markdown, dodatkowe pole) | AI | Wysokie — parsing failure | Użyć `JsonDocument.Parse` z trybem leniwym, obsłużyć ```json bloki, zwrócić błąd walidacji |
| 5 | Zależności mogą tworzyć cykle | Domenowa | Średnie | Walidacja po otrzymaniu od AI — wykryć cykle przed zapisem |
| 6 | Istniejący handler `SetWorkScheduleDependenciesCommandHandler.AdjustSuccessorPeriodsAsync` automatycznie przesuwa okresy — może skonfliktować z tym co AI wygenerowało | Domenowa | Średnie | Najpierw zapisać okresy, potem zależności — handler sam dostosuje |
| 7 | `SetWorkScheduleStageWorkPeriodsCommandHandler` waliduje dependency constraints i może rzucić błędem jeśli okres sukcesora jest przed poprzednikiem | Domenowa | Średnie | Najpierw zapisać zależności (bez okresów), potem okresy — lub zapisać okresy w kolejności topologicznej |
| 8 | Brak obsługi wielowątkowości — użytkownik może kliknąć "Generuj" wielokrotnie | API | Niskie | Dodać guard na poziomie handlera (sprawdzić czy już istnieją okresy/zależności) |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe encje | 0 |
| Nowe Commands | 1 (`GenerateScheduleFromEstimateAICommand`) |
| Nowe Queries | 0 |
| Nowe endpointy | 1 (`POST .../generate-from-ai`) |
| Nowe serwisy | 1 (`IWorkScheduleAIGeneratorService` / `WorkScheduleAIGeneratorService`) |
| Nowe narzędzia AI | 0 (osobny agent z promptem) |
| Nowi agenci AI | 1 (`schedule-generator-agent`) |
| Wymaga migracji DB | Nie |
| Pytania domenowe | 3 |

---

## Pytania domenowe wymagające decyzji

1. **Kolejność zapisu: okresy czy zależności pierwsze?** Obecny `SetWorkScheduleDependenciesCommandHandler` automatycznie przesuwa okresy sukcesorów (AdjustSuccessorPeriodsAsync). Jeśli najpierw zapiszemy okresy (wyliczone z duration_days), a potem zależności, handler może je zmienić. Rozwiązania: (a) zapisać zależności bez okresów → potem okresy, (b) użyć bezpośredniego zapisu przez repo w handlerze, (c) wywołać `SetWorkScheduleDependenciesCommand` z dependency tylko, a okresy zapisać przed lub po. **Decyzja**: zapisać najpierw okresy (SetWorkScheduleStageWorkPeriodsCommand dla każdego worka), potem zależności przez SetWorkScheduleDependenciesCommand — handler automatycznie dostosuje okresy sukcesorów. AI powinno wygenerować okresy które są zgodne z zależnościami.

2. **Czy handler powinien automatycznie synchronizować z kosztorysem przed wywołaniem AI?** Feature spec mówi o "User tworzy nowy harmonogram... wybiera kosztorys" — w momencie tworzenia harmonogramu z kosztorysu, system najpierw synchronizuje (sync-with-estimate), potem dopiero AI generuje. Czy to ma być jeden endpoint (sync+generate) czy dwa osobne (najpierw sync, potem generate)? **Decyzja**: dwa osobne kroki — najpierw user synchronizuje (istniejący endpoint `sync-with-estimate`), potem wywołuje AI generate.

3. **Jak mapować `work_name` z odpowiedzi AI na `WorkScheduleStageWorkId`?** AI zna tylko nazwy zakresów. W przypadku dużych kosztorysów mogą być duplikaty nazw. **Decyzja**: do promptu przekazać listę z (id, name, order, stage_name). W odpowiedzi AI używać `work_name` do mapowania. Jeśli nie znajdzie — rzucić błędem z listą dostępnych nazw. W przyszłości można dodać wsparcie dla `work_id` w odpowiedzi AI.

---

## Lista plików wymagających modyfikacji

### AI Agent Layer (Business.AIAgent)
1. **NOWY** `Business.AIAgent/Resources/Agents/sub_agents/schedule_generator_agent.md` — definicja agenta
2. **MODYFIKACJA** `Business.AIAgent/Registration/AIAgentServiceExtensions.cs` — rejestracja nowego narzędzia (jeśli opcja tool)

### Business Layer
3. **NOWY** `Business/Interfaces/WebModels/WorkSchedules/AIScheduleResult.cs` — DTO dla wyniku AI (AIScheduleResult, WorkPeriodResult, WorkDependencyResult)
4. **NOWY** `Business/Interfaces/Services/IWorkScheduleAIGeneratorService.cs` — interfejs serwisu
5. **NOWY** `Business/Implementation/Services/WorkScheduleAIGeneratorService.cs` — implementacja serwisu

### CQRS Layer
6. **NOWY** `CQRS/WorkSchedules/GenerateScheduleFromEstimateAI/GenerateScheduleFromEstimateAICommand.cs` — command
7. **NOWY** `CQRS/WorkSchedules/GenerateScheduleFromEstimateAI/GenerateScheduleFromEstimateAICommandHandler.cs` — handler
8. **NOWY** `CQRS/WorkSchedules/GenerateScheduleFromEstimateAI/GenerateScheduleFromEstimateAICommandValidator.cs` — walidator

### WebApi Layer
9. **MODYFIKACJA** `WebApi/Controllers/WorkScheduleController.cs` — nowy endpoint
10. **MODYFIKACJA** `WebApi/Extensions/ServiceCollectionExtensions.cs` — rejestracja `IWorkScheduleAIGeneratorService`

### DI Registration
11. **MODYFIKACJA** `Business.AIAgent/Registration/AIAgentServiceExtensions.cs` — (jeśli opcja tool)

---

## Kluczowe wzorce do naśladowania

### Struktura handlera (wzorowana na SyncWorkScheduleWithEstimateCommandHandler)
```csharp
public sealed class GenerateScheduleFromEstimateAICommandHandler
    : IRequestHandler<GenerateScheduleFromEstimateAICommand, WorkScheduleDetailsWeb>
{
    private readonly IRepository<WorkSchedule> workScheduleRepo;
    private readonly IRepository<CostEstimate> costEstimateRepo; // do wczytania nazw grup/itemów
    private readonly IWorkScheduleAIGeneratorService aiGenerator;
    private readonly IWorkScheduleCacheService scheduleCache;
    private readonly IWorkScheduleAccessService accessService;
    private readonly IMediator mediator; // do wywołania SetWorkScheduleStageWorkPeriodsCommand itp.

    public async Task<WorkScheduleDetailsWeb> Handle(
        GenerateScheduleFromEstimateAICommand request,
        CancellationToken cancellationToken)
    {
        // 1. Access check
        await accessService.RequireAdminOrOwnerAsync(...);
        
        // 2. Load work schedule + cost estimate data
        // 3. Call AI generator
        // 4. Save periods using mediator.Send(SetWorkScheduleStageWorkPeriodsCommand)
        // 5. Save dependencies using mediator.Send(SetWorkScheduleDependenciesCommand)
        // 6. Invalidate cache
        // 7. Return WorkScheduleDetailsWeb (from builder or from SetWorkScheduleDependenciesCommand result)
    }
}
```

### Konwencje nazewnicze
- Command: `GenerateScheduleFromEstimateAICommand`
- Handler: `GenerateScheduleFromEstimateAICommandHandler`
- Validator: `GenerateScheduleFromEstimateAICommandValidator`
- Serwis: `IWorkScheduleAIGeneratorService` / `WorkScheduleAIGeneratorService`
- Endpoint: `POST .../generate-from-ai`

### Rejestracja DI
```csharp
// W ServiceCollectionExtensions.cs:
services.AddScoped<IWorkScheduleAIGeneratorService, WorkScheduleAIGeneratorService>();
```

### Wzorzec command base
- Command dziedziczy po `WorkScheduleCommandBase` (ma TenantId, ProjectId, WorkScheduleId, PermissionCode)
- Dodaje pola: `OverallStartDate`, `OverallEndDate`
- Zwraca `WorkScheduleDetailsWeb`
