# Skill: API / Serwisy domenowe

## Opis
Tworzenie serwisów domenowych z interfejsami i rejestracją DI.

## Kiedy używać
Użyj tego skilla gdy tworzysz lub modyfikujesz serwis domenowy w Business/Implementation/Services/.

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
