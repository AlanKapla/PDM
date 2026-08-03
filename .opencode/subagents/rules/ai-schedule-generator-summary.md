# Feature Summary — AI Schedule Generator

> **Superseded** by `ai-schedule-periods-dependencies-*` (audit + refactor-01/02/03).
> Runtime: per-stage `schedule-duration-agent` + `schedule-dependency-agent` (concurrency limit 4),
> intra-stage FS in code, OverallEndDate scale. Monolithic `schedule-generator-agent` removed.

**Feature**: `ai-schedule-generator`
**Date**: 2026-06-09 (updated 2026-07-30)
**Status**: ✅ Wdrożony (per-stage agents)

## Co zostało zrobione (oryginał + ewolucja)

### API Layer

| # | Plik | Typ | Opis |
|---|------|-----|------|
| 1 | `Business/Interfaces/WebModels/WorkSchedules/AIScheduleResult.cs` | NOWY | DTO: AIScheduleResult, WorkPeriodResult, WorkDependencyResult |
| 2 | `Business/Interfaces/Services/IWorkScheduleAIGeneratorService.cs` | NOWY | Interfejs serwisu AI + StageInput, WorkInput |
| 3 | `Business/Implementation/Services/WorkScheduleAIGeneratorService.cs` | NOWY | Per-stage duration/dependency agents → merge → Kahn → scale |
| 4 | `Business.AIAgent/.../schedule_duration_agent.md` | NOWY | Agent duration (per stage) |
| 5 | `Business.AIAgent/.../schedule_dependency_agent.md` | NOWY | Agent dependency (per stage, cross-stage only) |
| — | ~~`schedule_generator_agent.md`~~ | USUNIĘTY | Dead monolithic agent (refactor-03) |
| 6 | `CQRS/.../GenerateScheduleFromEstimateAICommand.cs` | NOWY | Command z OverallStartDate, OverallEndDate |
| 7 | `CQRS/.../GenerateScheduleFromEstimateAICommandHandler.cs` | NOWY | Handler: sync → AI → periods → deps → cache |
| 8 | `CQRS/.../GenerateScheduleFromEstimateAICommandValidator.cs` | NOWY | Walidator: RequiredId, daty |
| 9 | `WebApi/Controllers/WorkScheduleController.cs` | MODYFIKACJA | `POST {id}/generate-from-ai` |
| 10 | `WebApi/Extensions/ServiceCollectionExtensions.cs` | MODYFIKACJA | DI `IWorkScheduleAIGeneratorService` |

### UI Layer (3 pliki)

| # | Plik | Typ | Opis |
|---|------|-----|------|
| 1 | `src/types/workSchedule.types.ts` | MODYFIKACJA | `GenerateScheduleFromEstimateAIRequest` |
| 2 | `src/api/projectApi.ts` | MODYFIKACJA | `generateScheduleFromEstimateAI` |
| 3 | `src/components/WorkScheduleFormModal.tsx` | MODYFIKACJA | Ramy czasowe + generowanie AI |

## Przepływ użytkownika

```
Kosztorys → "Utwórz harmonogram" → modal → wpisz nazwę → submit
  → Harmonogram utworzony (sync z kosztorysem automatycznie)
  → Krok 2: Data rozpoczęcia / zakończenia
  → "Generuj harmonogram z AI"
    → per-stage duration + dependency agents (max 4 concurrent)
    → intra-stage FS + scale do OverallEndDate
    → zapis okresów i zależności → widok harmonogramu
  → "Pomiń" (2 kliknięcia z ostrzeżeniem)
```

## Blokery
Brak

## Uwagi
- Limit concurrency: `SemaphoreSlim(4)` na duration+dependency w jednym `GenerateScheduleAsync`
- Wyniki z `Task.WhenAll` (bez `.Result`)
- Intra-stage ordering: deterministyczny FS wg Order (nie AI)
- Cross-stage deps: AI (`schedule-dependency-agent`)
- Feature doc: `.opencode/features/ai-schedule-generator.md`
