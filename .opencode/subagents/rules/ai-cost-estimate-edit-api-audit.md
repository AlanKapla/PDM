# Audyt API — AI Cost Estimate Edit

## Podsumowanie

Audyt zbadał wszystkie warstwy API pod kątem wdrożenia feature "AI Cost Estimate Edit".
System jest dobrze przygotowany do implementacji — istnieją sprawdzone wzorce do powielenia we wszystkich warstwach.

**Kluczowe wnioski:**
- Istniejący `CreateCostEstimateFromAIPreviewCommandHandler` jest **wzorcem** dla `ApplyCostEstimateAIEditCommand`
- `GetCostEstimateDetailsQueryHandler` pokazuje dokładnie jak załadować pełny kosztorys z cache
- `CostEstimateAIGeneratorService` jest wzorcem dla budowania wiadomości i parsowania odpowiedzi AI
- TransactionBehavior automatycznie wrapuje wszystkie `IRequestCommand` — apply będzie w transakcji automatycznie

---

## BLOK 1 — Stan obecny

### 1.1 Encje domenowe zaangażowane w feature

| Encja | Rola w feature |
|-------|---------------|
| `CostEstimate` | Główny kosztorys — metadata (name, description, status) |
| `CostEstimateGroup` | Grupy kosztorysu (hierarchia parent/child) |
| `CostEstimateItem` | Pozycje kosztorysowe (z RelationType: None/Component/Option) |
| `CostEstimateGroupFieldValue` | Wartości pól grupy |
| `CostEstimateItemFieldValue` | Wartości pól pozycji |
| `CostEstimateTemplate` | Szablon — definicje pól, ograniczenia |
| `CostEstimateTemplateFieldDefinitionBase` | Bazowa definicja pola (Group/System/Calculated/Generic) |

### 1.2 Istniejące endpointy (CostEstimateController)

| Endpoint | Metoda | Opis |
|----------|--------|------|
| `/{scope}` | GET | Lista kosztorysów |
| `/details/{id}` | GET | Szczegóły kosztorysu z hierarchią |
| `/` | POST | Tworzenie kosztorysu z szablonu |
| `/generate-ai-preview` | POST | Generowanie kosztorysu przez AI (creation preview) |
| `/create-from-ai-preview` | POST | Zapis kosztorysu z AI preview (creation) |
| `/{id}` | PUT | Update metadata (name, description) |
| `/{id}` | DELETE | Soft-delete kosztorysu |
| `/{id}/groups` | POST | Dodaj grupę |
| `/{id}/groups/{groupId}` | DELETE | Usuń grupę |
| `/{id}/groups/reorder` | PUT | Reorder grup |
| `/{id}/groups/{groupId}/fields` | PATCH | Upsert field grupy |
| `/{id}/items` | POST | Dodaj pozycję |
| `/{id}/items/{itemId}` | DELETE | Usuń pozycję |
| `/{id}/groups/{groupId}/items/reorder` | PUT | Reorder pozycji |
| `/{id}/items/{itemId}/move` | PATCH | Przenieś pozycję między grupami |
| `/{id}/items/{itemId}/fields` | PATCH | Upsert field pozycji |
| `/{id}/recalculate` | POST | Recalculacja kosztorysu |
| `/{id}/shares` | POST/PUT | Share operations |
| `/{id}/items/{itemId}/files` | POST | Upload plików |

**Routing:** `api/tenants/{tenantId}/projects/{projectId}/cost-estimate`
**Auth:** `[Authorize(Policy = PermissionCodes.ProjectEstimates)]` na wszystkich endpointach

### 1.3 Istniejące CQRS — analiza szczegółowa

#### Wzorzec Command/Query

```csharp
// Base records
public abstract record CostEstimateRequestBase : IAuthorizableRequest
{
    public Guid TenantId { get; init; }
    public Guid ProjectId { get; init; }
    public abstract string PermissionCode { get; }
    public virtual ResourceRef GetResource() =>
        new ResourceRef(TenantId: TenantId, ProjectId: ProjectId);
}

public abstract record CostEstimateCommandBase : CostEstimateRequestBase
{
    public Guid CostEstimateId { get; init; }
}

// Markers
public interface IRequestCommand<TResponse> : IRequest<TResponse> { }
public interface IRequestQuery<IResponse> : IRequest<IResponse> { }
```

#### TransactionBehavior

```csharp
// Automatycznie wrapuje każde IRequestCommand w transakcję:
public async Task<TResponse> Handle(TRequest request, ...)
{
    if (request is IRequestCommand<TResponse>)
    {
        IExecutionStrategy strategy = appDbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await appDbContext.Database.BeginTransactionAsync(ct);
            TResponse innerResponse = await next();
            await appDbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return innerResponse;
        });
    }
    return await next(ct);
}
```

**Konsekwencja:** `ApplyCostEstimateAIEditCommand` (IRequestCommand) będzie **automatycznie** wrapowany w transakcję. Nie trzeba implementować własnej transakcji.

#### AddCostEstimateGroupCommand

| Właściwość | Wartość |
|-----------|---------|
| Base | `CostEstimateCommandBase`, `IRequestCommand<Guid>` |
| PermissionCode | `PermissionCodes.ProjectEstimates` |
| Ciało | `ParentGroupId?`, `Order` |
| Cache | `GetCostEstimateAsync`, `GetTemplateAsync`, `GetGroupsDictionaryAsync` |
| Access | `EnsureCanModifyStructure()` (wymaga Full) |
| Invalidate | `InvalidateGroupsAsync` |
| Template check | `CanAddGroups`, `CanBranchGroups`, `MaxGroupLevel` |

**WZÓR:** Wszystkie commandy strukturalne (AddGroup, AddItem, DeleteGroup, DeleteItem, Reorder, Move) używają tego samego boilerplate: load CE z cache → check access → load template → validate → mutate → invalidate cache.

#### DeleteCostEstimateGroupCommand

| Właściwość | Wartość |
|-----------|---------|
| Base | `CostEstimateCommandBase`, `IRequestCommand<Unit>` |
| Access | Manualne sprawdzenie None/Restricted/ReadOnly (NIE używa `EnsureCanModifyStructure`) |
| Dodatkowe | Kasuje pliki, field values (hard delete), soft-delete items, soft-delete groups |
| Invalidate | `InvalidateCostEstimateAsync` (pełna) |
| Uwaga | **Unikalny** — własna implementacja access check zamiast extension method |

#### UpsertCostEstimateItemFieldCommand / UpsertCostEstimateGroupFieldCommand

| Właściwość | Wartość |
|-----------|---------|
| Base | `CostEstimateCommandBase`, `IRequestCommand<Guid>` |
| Access | None → Forbidden, ReadOnly → Forbidden, Restricted → dozwolone ale nie read-only fields |
| Walidacja | `CostEstimateFieldValueValidator` — type-mismatch, zakresy, max length |
| Feature | Auto-detect add vs update (sprawdza czy field value już istnieje dla item/grupy) |
| Feature | Auto-update Item.Name / Group.Name gdy zmieniane jest pole ItemSystemName / GroupName |
| Notification | Wysyła notyfikację do owner jeśli zmienia shared user |
| Cache | `InvalidateItemFieldValuesAsync` / `InvalidateGroupFieldValuesAsync` + `InvalidateItemsAsync` gdy zmienia się nazwa |

**UWAGA:** Ten handler jest **bardzo złożony** (~300 linii). To sugeruje, że lepiej delegować do niego niż próbować robić bulk field updates bezpośrednio.

#### CreateCostEstimateFromAIPreviewCommand — WZÓR DO NALADOWANIA

```csharp
public sealed record CreateCostEstimateFromAIPreviewCommand 
    : CostEstimateRequestBase, IRequestCommand<Guid>  // UWAGA: CostEstimateRequestBase, NIE CommandBase (bo brak CostEstimateId)
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public AICostEstimatePreviewWeb Preview { get; init; } = default!;
    public override string PermissionCode => PermissionCodes.ProjectEstimates;
}
```

Handler robi:
1. Load template z DB (z Include wszystkich field definitions) + build field def dictionary
2. Create CostEstimate entity
3. Iteruje po `Preview.Groups` (ordered by ParentTempId == null first)
4. Dla każdej grupy: insert group → map tempId → insert group field values → insert items
5. Dla każdego itemu: insert item → insert components (jeśli są) → insert item field values
6. Auto-uzupełnia: GroupName, ItemSystemName, ItemSystemSelected, ItemSystemIsWorkScope
7. Waliduje field values przez `CostEstimateFieldValueValidator`
8. Jedno `SaveChangesAsync` na końcu

**Kluczowe do naśladowania:**
- `BuildFieldDefDictionary(template)` — pattern łączenia Group+System+Calculated+Generic definitions
- `InsertGroupFieldValues` / `InsertItemFieldValues` — pattern walidacji i insertu z auto-uzupełnianiem
- `IsValidForInsert` — walidacja field values przez `CostEstimateFieldValueValidator`
- Sortowanie grup by ParentTempId

#### GenerateCostEstimateAIPreviewCommand

```csharp
public sealed record GenerateCostEstimateAIPreviewCommand 
    : CostEstimateRequestBase, IRequestCommand<AICostEstimatePreviewWeb>
{
    public AICostEstimateRequestWeb Request { get; init; } = default!;
    public override string PermissionCode => PermissionCodes.ProjectEstimates;
}
```

Handler:
1. Load template przez `ICostEstimateTemplateService.GetTemplateForAIGenerationAsync`
2. Call `aiGeneratorService.GeneratePreviewAsync(request, template, ct)`

### 1.4 CostEstimateAIGeneratorService — analiza

**DI:**
- `IAgentRunner` — do wywoływania agentów
- `ILogger`

**Przepływ:**
1. `BuildPlannerMessage(request)` — buduje tekst z opisem inwestycji
2. `_agentRunner.RunAsync("cost-estimate-planner", message, context, ct)` — planuje strukturę grup
3. `ParseGroupPlan(response)` — parsuje JSON → `(suggestedName, suggestedDescription, List<GroupStub>)`
4. `BuildTemplateSchema(template)` — buduje tekstową reprezentację szablonu (field definitions, jednostki)
5. Równolegle (SemaphoreSlim 5): dla każdej grupy wywołuje `cost-estimate-group-generator`
6. `ParseSingleGroup(response)` — parsuje JSON → `AIGroupPreviewWeb?`
7. `RemoveInvalidFieldValues(preview, template)` — odfiltrowuje nieprawidłowe field values

**Jak parsuje odpowiedź AI:**
- `ExtractJson(raw)` — wyciąga pierwszy `{...}` z odpowiedzi
- `JsonSerializer.Deserialize<T>(json, _jsonOptions)` gdzie `_jsonOptions` = case-insensitive

**Wzorzec budowania wiadomości:**
- StringBuilder, proste klucz:wartość
- Instrukcje dla AI w formacie: `"""Zwróć JSON: {"key":"value",...}"""`

### 1.5 Agent definitions

#### cost-estimate-planner.md
- `model: gpt-4o`, `temperature: 0.2`, `max_tokens: 1000`, `max_iterations: 1`
- Brak tools
- Zwraca JSON z `suggestedName`, `suggestedDescription`, `groups[{tempId, name, order}]`

#### cost-estimate-group-generator.md
- `model: gpt-4o`, `temperature: 0.3`, `max_tokens: 3000`, `max_iterations: 1`
- Brak tools (zadeklarowane `tools: []`)
- Zwraca JSON z `tempId, name, fieldValues[...], items[...]`
- Szczegółowe instrukcje o formatach pól, typach elementów, zasadach

#### cost-estimate-agent.md
- `model: gpt-4o`, `temperature: 0.3`, `max_tokens: 2048`, `max_iterations: 6`
- Tools: `get_cost_estimate`, `get_cost_estimate_items`, `get_project_info`
- Agent analityczny (read-only)

### 1.6 Agent Framework

**AgentRunner:**
- `RunAsync(agentName, userMessage, context, cancellationToken)` → `AgentRunResult`
- `RunStreamingAsync(...)` → `IAsyncEnumerable<AgentStreamEvent>` (tokiem)
- Max sub-agent depth: konfigurowalne w `AzureAIAgentOptions.MaxSubAgentDepth`
- Timeout: konfigurowalny w `AzureAIAgentOptions.AgentTimeoutSeconds`
- Tool calling: accumulator pattern dla streamingowych tool callów
- Iteracje: max `definition.MaxIterations`

**ToolRegistry:**
- `GetAllowedTools(IEnumerable<string> allowedNames)` → filtruje po nazwach
- Rejestracja: wszystkie `IAgentTool` jako Scoped w DI

**AgentToolBase:**
- Helpery: `GetString`, `GetGuid`, `GetInt`, `BuildSchema`
- Wymaga: `Name`, `Description`, `ParametersSchema`, `ExecuteAsync`

### 1.7 Istniejące narzędzia AI Agent

| Tool | Metoda | Opis |
|------|--------|------|
| `GetCostEstimateTool` | `get_cost_estimate` | Lista kosztorysów projektu (summary) |
| `GetCostEstimateItemsTool` | `get_cost_estimate_items` | Items + groups kosztorysu |
| `GetProjectInfoTool` | `get_project_info` | Info o projekcie |
| `GetWorkScheduleTool` | `get_work_schedule` | Harmonogram |
| `HttpFetchTool` | `http_fetch` | Fetch URL |
| `CallSubAgentTool` | `call_sub_agent` | Wywołanie sub-agenta |

**Brak narzędzia do pobierania pełnego kosztorysu z wszystkimi polami** — to jest luka.

### 1.8 CostEstimateCacheService

```csharp
public interface ICostEstimateCacheService
{
    Task<CostEstimate?> GetCostEstimateAsync(...);
    Task<CostEstimateTemplate?> GetTemplateAsync(...);
    Task<Dictionary<Guid, CostEstimateGroup>> GetGroupsDictionaryAsync(...);
    Task<Dictionary<Guid, CostEstimateItem>> GetItemsDictionaryAsync(...);
    Task<Dictionary<Guid, CostEstimateGroupFieldValue>> GetGroupFieldValuesDictionaryAsync(...);
    Task<Dictionary<Guid, CostEstimateItemFieldValue>> GetItemFieldValuesDictionaryAsync(...);
    
    // Invalidation
    Task InvalidateCostEstimateAsync(...);      // ALL cache
    Task InvalidateGroupsAsync(...);            // tylko groups
    Task InvalidateItemsAsync(...);             // tylko items
    Task InvalidateGroupFieldValuesAsync(...);  // tylko group field values
    Task InvalidateItemFieldValuesAsync(...);   // tylko item field values
    Task InvalidateTemplateAsync(...);          // template
}
```

**Wzorzec użycia w handlerach:**
- Load z cache: oddzielne wywołania dla każdej kolekcji (Redis — niezależne)
- Po mutacji: invalidacja specyficznego cache (nie wszystko naraz)
- Tylko przy delete grup/item: `InvalidateCostEstimateAsync` (pełna)

### 1.9 CostEstimateAccessService

```csharp
public enum CostEstimateAccessLevel { None = 0, ReadOnly = 1, Restricted = 2, Full = 3 }
```

- **Full** (3): owner lub admin — wszystko dozwolone
- **Restricted** (2): shared user — może edytować pola (poza read-only), NIE może zmieniać struktury
- **ReadOnly** (1): SuperAdmin fallback — tylko odczyt
- **None** (0): brak dostępu

Cache: `ce:access:{tenantId}:{projectId}:level:{userId}:{costEstimateId}`, TTL 15 min

**Extension method:** `EnsureCanModifyStructure()` — rzuca Forbidden dla None/Restricted/ReadOnly

### 1.10 Istniejące testy

- **Tylko testy walidatorów** (CommandValidator) — 19 plików testowych
- **Testy handlerów** (CommandHandler) — 10 plików: AddGroup, AddItem, DeleteGroup, DeleteItem, UpdateCostEstimate, DeleteCostEstimate, CreateCostEstimate, GetCostEstimates, GetCostEstimateDetails, RecalculateCostEstimate
- **Wzorzec testów handlerów:**
  ```csharp
  public sealed class AddCostEstimateGroupCommandHandlerTests
  {
      private readonly Mock<IRepository<CostEstimateGroup>> _groupRepoMock = new();
      private readonly Mock<ICostEstimateCacheService> _cacheServiceMock = new();
      private readonly Mock<ICostEstimateAccessService> _ceAccessServiceMock = new();
      private readonly Mock<ICurrentUser> _currentUserMock = new();
      private readonly AddCostEstimateGroupCommandHandler _handler;
      
      // Arrange: setup mocks
      // Act: await _handler.Handle(command, CancellationToken.None)
      // Assert: FluentAssertions + Moq Verify
  }
  ```
- **Business.Tests:** Jeden test dla `CostEstimateAccessServiceTests`
- **Brak testów dla AI generator service** i handlerów AI

---

## BLOK 2 — Luki i braki

| # | Brak / Luka | Warstwa | Priorytet | Opis |
|---|-------------|---------|-----------|------|
| 1 | Brak narzędzia `get_full_cost_estimate` dla agenta | AI Agent (Tools) | HIGH | Potrzebne do wczytania pełnego stanu kosztorysu przez agenta edytora |
| 2 | Brak agenta `cost-estimate-editor` | AI Agent (Definitions) | HIGH | Nowy agent do edycji istniejącego kosztorysu |
| 3 | Brak serwisu `ICostEstimateAIEditService` | Business | HIGH | Logika budowania kontekstu, wywoływania agenta, parsowania odpowiedzi |
| 4 | Brak `GenerateCostEstimateAIEditCommand` | CQRS | HIGH | Command do generowania podglądu edycji (preview) |
| 5 | Brak `ApplyCostEstimateAIEditCommand` | CQRS | HIGH | Command do aplikowania zatwierdzonych zmian |
| 6 | Brak `AICostEditPreviewWeb` | Business (Web Models) | HIGH | DTO dla podglądu edycji |
| 7 | Brak endpointów `POST /{id}/ai/edit-preview` i `POST /{id}/ai/apply-edit` | WebApi | HIGH | Nowe endpointy w CostEstimateController |
| 8 | Brak mechanizmu diff dla istniejących vs nowych grup/items | CQRS | MEDIUM | Apply musi odróżnić: add group, delete group, update group, zachować istniejące ID |
| 9 | Brak idempotentności dla apply | CQRS | MEDIUM | Feature spec wymaga idempotentności — przy drugim wywołaniu nic nie zmieniać |
| 10 | Brak testów dla istniejących AI handlerów | Tests | MEDIUM | `GenerateCostEstimateAIPreviewCommandHandler` i `CreateCostEstimateFromAIPreviewCommandHandler` nie mają testów |
| 11 | `DeleteCostEstimateGroupCommandHandler` nie używa `EnsureCanModifyStructure()` | CQRS | LOW | Inconsistency — używa manualnych ifów zamiast extension method |
| 12 | Brak rate-limiting dla AI edit (inaczej niż creation) | Business | MEDIUM | Przy creation SemaphoreSlim(5) dla grup. Przy edit nie ma równoległości, ale warto limitować |

---

## BLOK 3 — Zmiany w encjach/DB

| Encja | Zmiana | Typ | Wymaga migracji |
|-------|--------|-----|----------------|
| — | Brak zmian w encjach | — | NIE |

Feature operuje **wyłącznie na istniejących encjach** przez istniejące CQRS commandy. Nie wymaga zmian w modelu danych.

---

## BLOK 4 — Nowe Commands/Queries

| Command/Query | Typ | Opis | Handler |
|--------------|-----|------|---------|
| `GenerateCostEstimateAIEditCommand` | NOWY | Generuje podgląd edycji przez AI. Przyjmuje `CostEstimateId`, `UserRequest`. Ładuje pełny kosztorys z cache, wywołuje agenta, zwraca `AICostEditPreviewWeb`. Nie zapisuje do DB. | `GenerateCostEstimateAIEditCommandHandler` |
| `ApplyCostEstimateAIEditCommand` | NOWY | Aplikuje zatwierdzone zmiany do DB. Przyjmuje `CostEstimateId`, `AICostEditPreviewWeb`. Deleguje do istniejących CQRS commandów. W transakcji (TransactionBehavior). Kończy się recalculate. | `ApplyCostEstimateAIEditCommandHandler` |

### Szczegółowa specyfikacja

#### GenerateCostEstimateAIEditCommand

```csharp
public sealed record GenerateCostEstimateAIEditCommand 
    : CostEstimateCommandBase, IRequestCommand<AICostEditPreviewWeb>
{
    public string UserRequest { get; init; } = string.Empty;
    public override string PermissionCode => PermissionCodes.ProjectEstimates;
}
```

**Handler — wzorzec:**
1. Load cost estimate z cache (`ceCacheService.GetCostEstimateAsync`)
2. Check access (`ceAccessService.GetAccessLevelAsync` + ensure not None)
3. Load template z cache (`ceCacheService.GetTemplateAsync`)
4. Load wszystkie kolekcje z cache (groups, groupFieldValues, items, itemFieldValues)
5. Build pełny JSON stanu kosztorysu (wzorując się na `GetCostEstimateDetailsQueryHandler.BuildGroupHierarchy`)
6. Buduj message dla agenta: stan kosztorysu + request użytkownika + template schema
7. Wywołaj agenta `cost-estimate-editor` przez `_agentRunner.RunAsync`
8. Parsuj odpowiedź na `AICostEditPreviewWeb`
9. Waliduj field values względem template (jak `RemoveInvalidFieldValues` / `IsValidForInsert`)
10. Zwróć preview

#### ApplyCostEstimateAIEditCommand

```csharp
public sealed record ApplyCostEstimateAIEditCommand 
    : CostEstimateCommandBase, IRequestCommand<Unit>
{
    public AICostEditPreviewWeb Preview { get; init; } = default!;
    public override string PermissionCode => PermissionCodes.ProjectEstimates;
}
```

**Handler — wzorzec:**
1. Load cost estimate + check access (EnsureCanModifyStructure)
2. Load template + build field definitions dictionary
3. **Apply changes przez istniejące CQRS commandy** (przez `IMediator.Send`):
   - Update metadata → `UpdateCostEstimateCommand`
   - Usuń grupy (których brak w preview) → `DeleteCostEstimateGroupCommand`
   - Dodaj nowe grupy → `AddCostEstimateGroupCommand`
   - Usuń itemy (których brak w grupach) → `DeleteCostEstimateItemCommand`
   - Dodaj nowe itemy → `AddCostEstimateItemCommand`
   - Upsert field values → `UpsertCostEstimateItemFieldCommand` / `UpsertCostEstimateGroupFieldCommand`
   - Reorder → `ReorderCostEstimateItemsCommand` / `ReorderCostEstimateGroupsCommand`
4. **Recalculate** → `RecalculateCostEstimateCommand`
5. InvalidateALL cache → `ceCacheService.InvalidateCostEstimateAsync`

**UWAGA:** Wywoływanie istniejących commandów z handlera przez `IMediator.Send` to standardowy pattern CQRS. TransactionBehavior zadba o transakcję.

**Alternatywa (bardziej wydajna):** Bezpośrednia manipulacja encjami + SaveChangesAsync (jak w CreateCostEstimateFromAIPreview). Jednak delegowanie do istniejących commandów jest bezpieczniejsze — każdy handler ma własną walidację, access check, cache invalidation.

---

## BLOK 5 — Zmiany w kontrolerach

| Endpoint | HTTP | Nowy | Opis |
|----------|------|------|------|
| `/{id}/ai/edit-preview` | POST | NOWY | Generuje propozycję edycji przez AI. Body: `{ userRequest: string }`. Zwraca `AICostEditPreviewWeb` |
| `/{id}/ai/apply-edit` | POST | NOWY | Aplikuje zatwierdzone zmiany. Body: `AICostEditPreviewWeb`. Zwraca 204 No Content |

**Routing:** `api/tenants/{tenantId}/projects/{projectId}/cost-estimate/{id}/ai/edit-preview`

**Auth:** `[Authorize(Policy = PermissionCodes.ProjectEstimates)]`

**ProducesResponseType:**
- edit-preview: `200 OK` + `AICostEditPreviewWeb`, `400 BadRequest`, `403 Forbidden`, `404 NotFound`
- apply-edit: `204 No Content`, `400 BadRequest`, `403 Forbidden`, `404 NotFound`, `409 Conflict`

---

## BLOK 6 — Zmiany w serwisach

| Serwis | Interfejs | Nowy/Modyfikacja | Metody |
|--------|-----------|-----------------|--------|
| `CostEstimateAIEditService` | `ICostEstimateAIEditService` | NOWY | `GenerateEditPreviewAsync(costEstimate, template, userRequest, cancellationToken)` |
| `GetFullCostEstimateTool` | `IAgentTool` | NOWY | Narzędzie dla agenta `cost-estimate-editor` — zwraca pełny kosztorys z hierarchią |
| `CostEstimateAIGeneratorService` | `ICostEstimateAIGeneratorService` | BEZ ZMIAN | Nie modyfikować istniejącego serwisu |

### ICostEstimateAIEditService

```csharp
public interface ICostEstimateAIEditService
{
    Task<AICostEditPreviewWeb> GenerateEditPreviewAsync(
        CostEstimate costEstimate,
        CostEstimateTemplate template,
        Dictionary<Guid, CostEstimateGroup> groupsDict,
        Dictionary<Guid, CostEstimateGroupFieldValue> groupFieldValuesDict,
        Dictionary<Guid, CostEstimateItem> itemsDict,
        Dictionary<Guid, CostEstimateItemFieldValue> itemFieldValuesDict,
        string userRequest,
        CancellationToken cancellationToken);
}
```

LUB uproszczona wersja (jeśli handler ładuje dane):

```csharp
public interface ICostEstimateAIEditService
{
    Task<AICostEditPreviewWeb> GenerateEditPreviewAsync(
        Guid costEstimateId,
        Guid templateId,
        string userRequest,
        CancellationToken cancellationToken);
}
```

### GetFullCostEstimateTool

Nowe narzędzie dla agenta. Wzorzec: `AgentToolBase`.

```csharp
public sealed class GetFullCostEstimateTool : AgentToolBase
{
    public override string Name => "get_full_cost_estimate";
    public override string Description => "Returns the complete cost estimate with all groups, items, field values, and template schema.";
    
    // Parameters: cost_estimate_id (required)
    // Uses ICostEstimateCacheService to load everything
    // Returns comprehensive JSON with full hierarchy
}
```

### Rejestracja DI

W `AIAgentServiceExtensions.cs`:
```csharp
services.AddScoped<IAgentTool, GetFullCostEstimateTool>();
```

---

## BLOK 7 — Problemy i ryzyka

| # | Problem | Warstwa | Ryzyko | Rekomendacja |
|---|---------|---------|--------|-------------|
| 1 | Apply przez istniejące CQRS commandy = N round-tripów do DB + N cache invalidacji | CQRS | MEDIUM | Każdy command to osobna transakcja (TransactionBehavior). Apply będzie wrapowany w zewnętrzną transakcję, ale wewnętrzne commandy też próbują tworzyć własne transakcje. **Rozwiązanie:** Użyć `TransactionBehavior.Suppress` lub zrobić apply bezpośrednio przez repozytoria + jeden SaveChanges (jak CreateCostEstimateFromAIPreview) |
| 2 | Idempotentność — apply wielokrotne | CQRS | MEDIUM | Jeśli pierwsze apply się udało, drugie nie powinno nic zmienić. **Rozwiązanie:** Sprawdzić przed apply czy kosztorys już ma oczekiwany stan (np. porównać hash). Lub polegać na tym, że istniejące commandy są idempotentne (DeleteGroup już usuniętej → NotFound, UpsertField już istniejącego → update) |
| 3 | Duży JSON w preview — limit tokenów agenta | AI Agent | MEDIUM | Kosztorys z 100+ pozycjami może przekroczyć max_tokens (2048-3000). **Rozwiązanie:** Użyć `max_tokens: 4096`+ dla edytora, podzielić response na części |
| 4 | Concurrent edits — dwóch userów edytuje ten sam kosztorys przez AI | CQRS | LOW | Brak乐观 locking. **Rozwiązanie:** Dodać `RowVersion`/`ConcurrencyToken` do CostEstimate lub zaakceptować last-write-wins |
| 5 | Apply może być time-intensive (wiele operacji + recalculate) | CQRS | LOW | Dla kosztorysów z 50+ grupami i 500+ pozycjami apply może trwać >30s. **Rozwiązanie:** Monitorować, dodać progress callback jeśli potrzeba |
| 6 | `DeleteCostEstimateGroupCommandHandler` ma własną logikę access control (nie używa `EnsureCanModifyStructure`) | CQRS | LOW | Inconsistency — ale działa. Przy rozwijaniu feature warto ujednolicić |
| 7 | Brak mechanizmu "dry run" dla apply — nie ma możliwości sprawdzenia czy apply się uda bez zapisywania | CQRS | NISKIE | Feature zakłada preview → approve, ale preview to tylko propozycja AI. Walidacja field values odbywa się dopiero przy apply. Można dodać walidację w preview |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe encje | 0 |
| Nowe Commands | 2 (`GenerateCostEstimateAIEditCommand`, `ApplyCostEstimateAIEditCommand`) |
| Nowe Queries | 0 |
| Nowe endpointy | 2 |
| Nowe serwisy | 1 (`ICostEstimateAIEditService`) |
| Nowe narzędzia agenta | 1 (`GetFullCostEstimateTool`) |
| Nowi agenci | 1 (`cost-estimate-editor.md`) |
| Nowe Web modele | 1 (`AICostEditPreviewWeb`) |
| Wymaga migracji DB | NIE |
| Pliki do modyfikacji | ~15 |

---

## Wzorce do powielenia

### Wzorzec 1: Load full cost estimate (z GetCostEstimateDetailsQueryHandler)

```csharp
// Kolejność: costEstimate → template → groups → groupFieldValues → items → itemFieldValues
CostEstimate costEstimate = await ceCacheService.GetCostEstimateAsync(...);
CostEstimateTemplate template = await ceCacheService.GetTemplateAsync(costEstimate.TemplateId, ct);
Dictionary<Guid, CostEstimateGroup> groupsDict = await ceCacheService.GetGroupsDictionaryAsync(...);
Dictionary<Guid, CostEstimateGroupFieldValue> groupFieldValuesDict = await ceCacheService.GetGroupFieldValuesDictionaryAsync(...);
Dictionary<Guid, CostEstimateItem> itemsDict = await ceCacheService.GetItemsDictionaryAsync(...);
Dictionary<Guid, CostEstimateItemFieldValue> itemFieldValuesDict = await ceCacheService.GetItemFieldValuesDictionaryAsync(...);
```

### Wzorzec 2: Build field definitions dictionary (z CreateCostEstimateFromAIPreviewCommandHandler)

```csharp
private static Dictionary<Guid, CostEstimateTemplateFieldDefinitionBase> BuildFieldDefDictionary(
    CostEstimateTemplate template)
{
    return template.GroupFieldDefinitions
        .Cast<CostEstimateTemplateFieldDefinitionBase>()
        .Concat(template.SystemFieldDefinitions)
        .Concat(template.CalculatedFieldDefinitions)
        .Concat(template.GenericFieldDefinitions)
        .ToDictionary(f => f.Id);
}
```

### Wzorzec 3: Validate field value against template (z CreateCostEstimateFromAIPreviewCommandHandler)

```csharp
private bool IsValidForInsert(AIFieldValueWeb fv, CostEstimateTemplateFieldDefinitionBase fieldDef)
{
    CostEstimateFieldValueContext ctx = CostEstimateFieldValueContext.From(
        fieldDef, fv.StringValue, fv.DecimalValue, fv.BoolValue, fv.DateTimeValue);
    
    if (ctx.FieldTypeConfig.IsCollection || ctx.FieldTypeConfig.IsFile)
        return false;
    
    ValidationResult result = fieldValueValidator.Validate(ctx);
    return result.IsValid;
}
```

### Wzorzec 4: Build AI agent message (z CostEstimateAIGeneratorService)

```csharp
StringBuilder sb = new();
sb.AppendLine("KONTEKST:");
sb.AppendLine($"CostEstimate: {ce.Name}");
// ... dodaj stan kosztorysu
sb.AppendLine("REQUEST:");
sb.AppendLine(userRequest);
sb.AppendLine("""Zwróć JSON: {...}""");
```

### Wzorzec 5: Parse AI response (z CostEstimateAIGeneratorService)

```csharp
private static string ExtractJson(string raw)
{
    int firstBrace = raw.IndexOf('{');
    int lastBrace = raw.LastIndexOf('}');
    if (firstBrace >= 0 && lastBrace >= firstBrace)
        return raw[firstBrace..(lastBrace + 1)];
    return raw;
}

// Potem: JsonSerializer.Deserialize<T>(json, _jsonOptions)
```

### Wzorzec 6: Handler test pattern (z AddCostEstimateGroupCommandHandlerTests)

```csharp
[Fact]
public async Task Handle_WhenValidRequest_InsertsGroupAndReturnsGuid()
{
    // Arrange
    _cacheServiceMock.Setup(s => s.GetCostEstimateAsync(...)).ReturnsAsync(costEstimate);
    _ceAccessServiceMock.Setup(s => s.GetAccessLevelAsync(...)).ReturnsAsync(CostEstimateAccessLevel.Full);
    _cacheServiceMock.Setup(s => s.GetTemplateAsync(...)).ReturnsAsync(template);
    
    // Act
    Guid result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.Should().NotBeEmpty();
    _groupRepoMock.Verify(r => r.Insert(It.IsAny<CostEstimateGroup>()), Times.Once);
    _cacheServiceMock.Verify(s => s.InvalidateGroupsAsync(..., It.IsAny<CancellationToken>()), Times.Once);
}
```

---

## Pliki do modyfikacji/tworzenia

### Nowe pliki

| Plik | Lokalizacja |
|------|-------------|
| `AICostEditPreviewWeb.cs` | `src/Business/Interfaces/WebModels/AI/` |
| `ICostEstimateAIEditService.cs` | `src/Business/Interfaces/Services/` |
| `CostEstimateAIEditService.cs` | `src/Business/Implementation/Services/AI/` |
| `GenerateCostEstimateAIEditCommand.cs` | `src/CQRS/CostEstimates/GenerateCostEstimateAIEdit/` |
| `GenerateCostEstimateAIEditCommandHandler.cs` | `src/CQRS/CostEstimates/GenerateCostEstimateAIEdit/` |
| `GenerateCostEstimateAIEditCommandValidator.cs` | `src/CQRS/CostEstimates/GenerateCostEstimateAIEdit/` |
| `ApplyCostEstimateAIEditCommand.cs` | `src/CQRS/CostEstimates/ApplyCostEstimateAIEdit/` |
| `ApplyCostEstimateAIEditCommandHandler.cs` | `src/CQRS/CostEstimates/ApplyCostEstimateAIEdit/` |
| `ApplyCostEstimateAIEditCommandValidator.cs` | `src/CQRS/CostEstimates/ApplyCostEstimateAIEdit/` |
| `GetFullCostEstimateTool.cs` | `src/Business.AIAgent/Tools/CostEstimate/` |
| `cost-estimate-editor.md` | `src/Business.AIAgent/Resources/Agents/sub_agents/` |

### Modyfikowane pliki

| Plik | Zmiana |
|------|--------|
| `CostEstimateController.cs` | Dodanie 2 nowych endpointów |
| `AIAgentServiceExtensions.cs` | Rejestracja `GetFullCostEstimateTool` (IAgentTool) |
| `ServiceCollectionExtensions.cs` (WebApi) | Ewentualna rejestracja `ICostEstimateAIEditService` (jeśli nie w AIAgent) |
| `CreateCostEstimateFromAIPreviewCommandHandler.cs` | Wzorzec — bez zmian, ale warto skopiować pattern Insert*FieldValues |
| (opcjonalnie) `DeleteCostEstimateGroupCommandHandler.cs` | Ujednolicenie access check — użyć `EnsureCanModifyStructure()` |

---

## Pytania domenowe wymagające decyzji

1. **Apply — przez istniejące CQRS commandy czy bezpośrednio przez repozytoria?**
   - Przez CQRS: bezpieczniejsze, ale N transakcji + N cache invalidacji
   - Bezpośrednio: wydajniejsze, ale więcej boilerplate i ryzyko pominięcia walidacji
   - **Sugerowany wybór:** Bezpośrednio przez repozytoria (jak CreateCostEstimateFromAIPreview) w jednej transakcji + jedna cache invalidacja na końcu. To eliminuje ryzyko Problem #1.

2. **Czy `GenerateCostEstimateAIEditCommand` wymaga sprawdzenia `CostEstimateAccessLevel`?**
   - Preview jest read-only — czy Restricted (shared) może zobaczyć pełny stan?
   - Jeśli Restricted ma ograniczone kolumny (IsVisible), to czy preview AI też powinien uwzględniać te ograniczenia?
   - **Sugerowany wybór:** Restricted może generować preview (ale z widocznymi kolumnami), apply wymaga Full (EnsureCanModifyStructure)

3. **Jak obsłużyć idempotentność apply?**
   - Opcja A: Porównać hash obecnego stanu z oczekiwanym
   - Opcja B: Polegać na naturalnej idempotentności (Delete nieistniejącego → NotFound, ale to błąd)
   - Opcja C: Dodać pole `LastEditHash` do CostEstimate
   - **Sugerowany wybór:** Opcja B na start (naturalna idempotentność commandów), rozważyć C w przyszłości

4. **Czy `AICostEditPreviewWeb` powinien zawierać pełny stan po edycji, czy tylko diff?**
   - Feature spec mówi o "pełnym stanie" (groups z rzeczywistymi ID dla istniejących, tempId dla nowych)
   - To ułatwia apply (jeden JSON = final state), ale wymaga rozróżnienia add vs update na podstawie ID (Guid vs tempId)
   - **Sugerowany wybór:** Pełny stan — pattern jak w CreateCostEstimateFromAIPreview (tempId → Guid mapping)

5. **Czy potrzebujemy oddzielnego `ICostEstimateAIEditService` czy rozszerzyć istniejący `ICostEstimateAIGeneratorService`?**
   - Rozszerzenie istniejącego interfejsu: mniej plików, ale miesza creation z edit
   - Nowy serwis: czystszy, SRP
   - **Sugerowany wybór:** Nowy interfejs `ICostEstimateAIEditService` — edycja istniejącego kosztorysu to inna domena niż creation
