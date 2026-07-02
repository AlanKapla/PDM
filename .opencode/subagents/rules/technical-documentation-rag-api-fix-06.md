# API Fix 06 — Agenci AI + `TechnicalDocumentationOrchestratorService`

## Cel
Definicje subagentów (pliki `.md`) i serwis orkiestrujący pipeline Vision GPT-4o — wzorzec `CostEstimateAIGeneratorService`.

## Decyzje MVP
- **Brak RAG** / Azure AI Search
- **Brak SchemaVersion** w modelu JSON wyjściowym
- Równoległe wywołania z `SemaphoreSlim` (limit 5, jak kosztorys AI)

## Workspace
`C:\Users\kapla\source\repos\PDM\02-ApplicationServices\ProductDataManagementWebAPI`

## Skills
- `.cursor/skills/api-services/SKILL.md`

## Zależności
- **api-fix-02** — `ProjectTechnicalDocumentationDetails`
- **api-fix-05** — obrazy JPG z PDF

## Pliki referencyjne
- `src/Business/Implementation/Services/AI/CostEstimateAIGeneratorService.cs`
- `src/Business.AIAgent/Resources/Agents/sub_agents/cost_estimate_planner.md` — wzorzec pliku agenta
- `src/Business.AIAgent/Core/AgentRunner.cs` — `RunAsync(agentName, message, context, ct)`
- `src/Business/Interfaces/Services/IAICompletionService.cs` — `CompleteWithImageAsync` (alternatywa dla prostych agentów)

---

## 1. Definicje agentów

Katalog: `src/Business.AIAgent/Resources/Agents/sub_agents/`

| Plik | `name` w frontmatter | Rola |
|------|---------------------|------|
| `drawing_classification_agent.md` | `drawing-classification-agent` | Typ rysunku, skala, metadane strony |
| `architectural_extraction_agent.md` | `architectural-extraction-agent` | Pomieszczenia, ściany, otwory, wymiary |
| `installations_extraction_agent.md` | `installations-extraction-agent` | Instalacje branżowe |
| `aggregation_agent.md` | `aggregation-agent` | Scalenie częściowych wyników → `ProjectTechnicalDocumentationDetails` |

Każdy plik:
- YAML frontmatter z `name`, `description`
- Instrukcja: odpowiedź **wyłącznie JSON**
- Schema pól zgodna z klasami z **api-fix-02**
- Język promptów: polski (spójnie z innymi agentami)

**Uwaga:** Osobny `documentation_orchestrator.md` opcjonalny — logika orkiestracji w C# (`TechnicalDocumentationOrchestratorService`), nie w interaktywnym `AIHub`.

## 2. `ITechnicalDocumentationOrchestratorService`

Plik: `src/Business/Interfaces/Services/ITechnicalDocumentationOrchestratorService.cs`

```csharp
public sealed record TechnicalDocumentationImageInput(
    byte[] ImageBytes,
    string FileName,
    int PageNumber);

public sealed record TechnicalDocumentationPartialResult(
    string FileName,
    int PageNumber,
    string DrawingType,
    string? Scale,
    string ArchitecturalJson,
    string InstallationsJson);

public interface ITechnicalDocumentationOrchestratorService
{
    Task<ProjectTechnicalDocumentationDetails> ProcessImagesAsync(
        IReadOnlyList<TechnicalDocumentationImageInput> images,
        CancellationToken cancellationToken);
}
```

## 3. `TechnicalDocumentationOrchestratorService`

Plik: `src/Business/Implementation/Services/AI/TechnicalDocumentationOrchestratorService.cs`

Pipeline:
```
Dla każdego obrazu (równolegle, SemaphoreSlim=5):
  1. drawing-classification-agent → typ, skala
  2. architectural-extraction-agent → JSON pomieszczeń/ścian/otworów
  3. installations-extraction-agent → JSON instalacji

Po zebraniu wszystkich partial results:
  4. aggregation-agent → jeden ProjectTechnicalDocumentationDetails
```

Implementacja:
- `IAgentRunner` + `AgentContext` (wzorzec `CostEstimateAIGeneratorService`)
- Obrazy przekazuj jako base64 w wiadomości lub przez `CompleteWithImageAsync` gdy prostsze
- Parsowanie JSON: usuń markdown fences, `JsonSerializer` z `PropertyNameCaseInsensitive`
- Błąd agenta → log warning, kontynuuj z pustym partial result lub fail całości (preferuj fail przy 0 poprawnych obrazów)

## 4. Rejestracja DI

```csharp
services.AddScoped<ITechnicalDocumentationOrchestratorService, TechnicalDocumentationOrchestratorService>();
```

## Weryfikacja
```powershell
dotnet build src/Business/Business.csproj
```

## Następny krok
Orkiestrator wywoływany z **api-fix-07** (`TechnicalDocumentationProcessingService`).
