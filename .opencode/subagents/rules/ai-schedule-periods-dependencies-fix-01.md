# Refactor 01 — Krytyczne bezpieczeństwo + wyjątki + clear deps + controller

## Decyzje domenowe (zaakceptowane)
- Predykaty reload **zawsze** z `TenantId` + `ProjectId`.
- Błędy AI → `ValidationApiException` (nie `InvalidOperationException`).
- Re-generacja AI **zawsze** wywołuje `SetWorkScheduleDependencies` (także przy pustej liście = clear starych deps).
- Cykl w grafie → `ValidationApiException` (nie ciche umieszczanie prac).
- Usunąć orphan braces w `WorkScheduleController` (L533–534); dodać `sealed` jeśli brak.

## Zakaz
- Zakaz `var`
- `is null` / `is not null`
- Nie zmieniać UI (tsx) w tym prompcie

## Zmiany

### 1. `GenerateScheduleFromEstimateAICommandHandler.cs`

**Reload stages** — zamień na:
```csharp
List<WorkScheduleStage> allStages = (await stageRepo.GetBySearch(
    s => s.WorkScheduleId == workScheduleId
         && s.TenantId == tenantId
         && s.ProjectId == projectId
         && !s.IsDeleted))
    .ToList();
```

**Reload works** — najpierw `stageIds`, potem:
```csharp
List<WorkScheduleStageWork> allWorks = (await workRepo.GetBySearch(
    w => w.TenantId == tenantId
         && w.ProjectId == projectId
         && stageIds.Contains(w.WorkScheduleStageId)
         && !w.IsDeleted))
    .ToList();
```
Usuń filtr w pamięci po szerokim skanie.

**Zapis zależności** — ZAWSZE wywołuj SetDependencies (usuń `if (aiResult.Dependencies.Count > 0)`):
```csharp
SetWorkScheduleDependenciesCommand depsCommand = new SetWorkScheduleDependenciesCommand
{
    TenantId = tenantId,
    ProjectId = projectId,
    WorkScheduleId = workScheduleId,
    Dependencies = aiResult.Dependencies.Select(d => new WorkDependencyDto(
        d.PredecessorWorkId,
        d.SuccessorWorkId,
        d.DependencyType,
        d.LagDays)).ToList()
};
await mediator.Send(depsCommand, cancellationToken);
```

Opcjonalnie: użyj `IReadRepository<>` dla stage/work reload jeśli DI to wspiera i nie psuje konstruktora — jeśli wymaga dużych zmian DI, zostaw IRepository ale z poprawnymi predykatami.

### 2. `WorkScheduleAIGeneratorService.cs`

Zamień wszystkie `throw new InvalidOperationException(...)` na `throw new ValidationApiException(...)` (fail duration/dependency agent, empty durations).

W `CalculateSchedule`: jeśli po Kahnie zostały węzły z `inDegree > 0` (cykl) **lub** prace bez `startDateByWorkId` z powodu cyklu — rzuć `ValidationApiException` z komunikatem o cyklu zamiast cichego `overallStart + Order*2` dla residual cykli. Izolowane prace bez deps (inDegree 0, nie w queue — nie powinno się zdarzyć) mogą startować przy overallStartDate.

### 3. `WorkScheduleController.cs`

- Usuń orphan `}` na końcu pliku po `GenerateFromAI` (linie ~533–534).
- Upewnij się, że klasa kontrolera jest `sealed`.
- Dodaj `[ProducesResponseType(typeof(WorkScheduleDetailsWeb), StatusCodes.Status200OK)]` na `GenerateFromAI` jeśli brakuje.

## Weryfikacja
```
dotnet build 02-ApplicationServices/ProductDataManagementWebAPI --configuration Release
```
0 błędów kompilacji.

## Raport
Zwróć: status build, lista plików, blokery.
