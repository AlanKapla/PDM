# Summary — AI Schedule Periods & Dependencies

**Domena**: `ai-schedule-periods-dependencies`
**Data**: 2026-07-30
**Branch**: `cursor/ai-schedule-periods-deps-audit-76f5`
**Status**: Audyt + refaktor (fix-01..03) zakończone, build Release 0 errors

## Audyt

Raport: `.opencode/subagents/rules/ai-schedule-periods-dependencies-audit.md`

| Metryka | Wartość |
|---------|---------|
| Krytyczne | 5 |
| Wysokie | 12 |
| Normalne | 10 |

**Runtime (przed refaktorem):** per-stage `schedule-duration-agent` + `schedule-dependency-agent` (2N równolegle) → merge → Kahn → N× SetPeriods → SetDependencies. Monolityczny `schedule-generator-agent` był dead code.

## Wykonane refaktory

| Prompt | Zakres | Status |
|--------|--------|--------|
| fix-01 | TenantId/ProjectId w reload, ValidationApiException, zawsze clear deps, cykl→exception, sealed controller + orphan braces | ✅ |
| fix-02 | Intra-stage FS wg Order, FS=`predEnd+lag`, ScaleScheduleToOverallEndDate, StageInput.Order w prompt | ✅ |
| fix-03 | SemaphoreSlim(4), WhenAll bez `.Result`, usunięty dead agent, slim handler, docs | ✅ |

## Zmodyfikowane pliki kodu

- `GenerateScheduleFromEstimateAICommandHandler.cs`
- `WorkScheduleAIGeneratorService.cs`
- `WorkScheduleController.cs`
- `schedule_generator_agent.md` (usunięty)
- `.opencode/features/ai-schedule-generator.md`
- `.opencode/subagents/rules/ai-schedule-generator-summary.md`

## Świadomie odłożone (z audytu)

- Wydzielenie God-class na osobne klasy (PromptBuilder / TopologyCalculator) — duży refaktor strukturalny
- Batch `SetWorkSchedulePeriodsBulkCommand` zamiast N× mediator
- Unit testy `CalculateSchedule` / handler / merge
- Predykaty TenantId w SetPeriods ExecuteDelete / SetDependencies Adjust (poza ścieżką Generate AI)
- IReadRepository dla reload

## Build

`dotnet build --configuration Release` — 0 errors
