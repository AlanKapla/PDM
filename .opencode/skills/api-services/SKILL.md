---
name: api-services
description: "Tworzenie serwisów domenowych z interfejsami i rejestracją DI. Użyj gdy tworzysz lub modyfikujesz serwis domenowy w Business/Implementation/Services/."
---

# Skill: API / Serwisy domenowe

## Opis
Tworzenie serwisów domenowych z interfejsami i rejestracją DI.

## Kiedy używać
Użyj tego skilla gdy tworzysz lub modyfikujesz serwis domenowy w Business/Implementation/Services/.

---

## Hierarchia referencji projektów

```
Entities
  └── Repositories
        └── Business          ← serwisy domenowe, interfejsy, web modele, AI serwisy
              └── CQRS        ← handlery, validatory, queries/commands
```

```
Business.AIAgent              ← agent AI (AgentRunner, Tools, rejestracja DI)
  └── Business                ← tylko przez Business, NIGDY bezpośrednio CQRS
```

### Zasady referencji

- `Business.AIAgent` → `Business` ✅ — serwisy AI (DocumentParserService, CostEstimateAIGeneratorService) żyją w `Business`
- `Business.AIAgent` → `CQRS` ❌ — zakaz bezpośredniej referencji do CQRS z Business.AIAgent
- `CQRS` → `Business` ✅ — handlery używają serwisów i interfejsów z Business
- `Business` → `CQRS` ❌ — zakaz (cykl)

### Gdzie żyją serwisy AI

Serwisy używające Azure OpenAI (`AzureAIAgentOptions`, `ChatClient`) należą do `Business`, nie do `Business.AIAgent`:

```
src/Business/Interfaces/Configuration/AzureAIAgentOptions.cs   ← opcje konfiguracji
src/Business/Interfaces/Services/IDocumentParserService.cs      ← interfejs
src/Business/Interfaces/Services/ICostEstimateAIGeneratorService.cs
src/Business/Implementation/Services/DocumentParserService.cs  ← implementacja
src/Business/Implementation/Services/CostEstimateAIGeneratorService.cs
```

`Business.AIAgent` zawiera wyłącznie: AgentRunner, Tools (IAgentTool), AgentDefinitionLoader, rejestrację DI (`AddAIAgent()`).

---

## Lokalizacja

```
src/Business/Interfaces/Services/I{NazwaSerwisu}.cs
src/Business/Implementation/Services/{NazwaSerwisu}.cs
```

## Wzorzec

```csharp
// Interfejs
public interface ICostEstimateCalculationService
{
    CostEstimate RecalculateCostEstimate(CostEstimate costEstimate);
}

// Implementacja
public sealed class CostEstimateCalculationService : ICostEstimateCalculationService
{
    public CostEstimate RecalculateCostEstimate(CostEstimate costEstimate)
    {
        // logika obliczeniowa
        return costEstimate;
    }
}
```

## Serwis z zależnościami

```csharp
public sealed class WorkScheduleSyncService : IWorkScheduleSyncService
{
    private readonly IRepository<WorkScheduleStage> stageRepo;
    private readonly IRepository<WorkScheduleStageWork> workRepo;

    public WorkScheduleSyncService(
        IRepository<WorkScheduleStage> stageRepo,
        IRepository<WorkScheduleStageWork> workRepo)
    {
        this.stageRepo = stageRepo;
        this.workRepo = workRepo;
    }

    public async Task SyncFromCostEstimateAsync(
        WorkSchedule schedule,
        CostEstimate estimate,
        CancellationToken ct)
    {
        // logika synchronizacji
    }
}
```

## Rejestracja DI

```csharp
// ServiceCollectionExtensions.cs
services.AddScoped<ICostEstimateCalculationService, CostEstimateCalculationService>();
services.AddScoped<IWorkScheduleSyncService, WorkScheduleSyncService>();

// Singleton dla serwisów bezstanowych (bez zależności Scoped)
services.AddSingleton<ICostTrackerFinancialService, CostTrackerFinancialService>();
```

## Zasady

- Serwis zawsze `sealed`
- Zawsze dedykowany interfejs `I{NazwaSerwisu}`
- Rejestracja przez interfejs w DI — nigdy przez typ konkretny
- `Scoped` dla serwisów z zależnościami (repo, cache)
- `Singleton` dla serwisów czystych obliczeniowych (bez zależności Scoped)
- Serwis nie zawiera logiki orkiestracji — to rola handlera
- Serwis zawiera logikę domenową wielokrotnie używaną przez wiele handlerów
- Zakaz `var` — zawsze explicit type
- Metody async zawsze z `CancellationToken`
