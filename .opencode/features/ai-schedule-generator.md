# AI Schedule Generator — Harmonogram z kosztorysu wspierany przez AI

## Opis

Użytkownik podczas tworzenia harmonogramu na podstawie kosztorysu podaje ramy czasowe
(okno: data rozpoczęcia i zakończenia całego harmonogramu), a agenci AI na podstawie
nazw etapów (grup kosztorysowych) i pozycji (zakresów robót) dobierają czasy ich trwania
oraz automatycznie tworzą zależności między zakresami robót.

## Przepływ

1. User tworzy nowy harmonogram w trybie "Na podstawie kosztorysu"
2. Wybiera kosztorys
3. **Nowy krok**: Podaje datę rozpoczęcia i zakończenia całego projektu (ramy czasowe)
4. System wysyła do API żądanie wygenerowania harmonogramu z AI
5. AI analizuje nazwy grup kosztorysowych (etapy) i pozycji (zakresy robót) — **per stage**
6. AI zwraca (merge wyników):
   - Sugerowane czasy trwania (dni) dla każdego zakresu robót
   - Sugerowane zależności (predecessor/successor/dependencyType/lagDays) między zakresami
7. System zapisuje harmonogram z etapami, zakresami, okresami i zależnościami
8. User widzi gotowy harmonogram z wypełnionymi datami i zależnościami

## Runtime (aktualny)

### Per-stage agenci (równolegle, limit concurrency = 4)

Dla każdego etapu uruchamiane są dwa agenci (`IAgentRunner.RunAsync`), współdzieląc `SemaphoreSlim(4)`:

| Agent | Rola |
|-------|------|
| `schedule-duration-agent` | Estymacja `duration_days` tylko dla prac danego etapu |
| `schedule-dependency-agent` | Max 2 zależności cross-stage dla focus stage (predecessor / successor) |

Wyniki zbierane przez `await Task.WhenAll(...)` (bez `.Result`), potem merge + dedupe.

### Deterministyczne post-processing (kod, nie AI)

1. **Intra-stage FS** — sekwencyjne `FinishToStart` (lag=0) między kolejnymi pracami w etapie wg `Order`
2. Walidacja durations/dependencies
3. Sortowanie topologiczne (Kahn) → daty start/end
4. **OverallEndDate skaluje łańcuch** — gdy `max(EndDate) > overallEndDate`, proporcjonalna kompresja offsetów i duration (min 1 dzień), potem enforce FS

### Obsolete (nieaktualne)

- ~~Monolityczny `schedule-generator-agent`~~ — usunięty (dead resource)
- ~~Tool `analyze_schedule_structure`~~ — nieużywany; prompty budowane w `WorkScheduleAIGeneratorService`

## Wymagane zmiany

### API — Endpoint/CQRS

**Command**: `GenerateScheduleFromEstimateAICommand`
- `TenantId`, `ProjectId`, `WorkScheduleId`
- `OverallStartDate` (DateTime) — ramy czasowe: start
- `OverallEndDate` (DateTime) — ramy czasowe: koniec

**Handler**: `GenerateScheduleFromEstimateAICommandHandler`
- Access check → sync z kosztorysem → load stages/works
- Wywołuje `IWorkScheduleAIGeneratorService.GenerateScheduleAsync`
- Zapisuje okresy (`SetWorkScheduleStageWorkPeriodsCommand`) i zależności (`SetWorkScheduleDependenciesCommand`)
- Unieważnia cache → zwraca `WorkScheduleDetailsWeb`

**Validator**: FluentValidation dla dat

### AI Agent — Per-stage

**Agenci** (EmbeddedResource w `Business.AIAgent/Resources/Agents/sub_agents/`):
- `schedule_duration_agent.md` → `schedule-duration-agent`
- `schedule_dependency_agent.md` → `schedule-dependency-agent`

**Serwis**: `WorkScheduleAIGeneratorService`
- Buduje prompty per stage
- Uruchamia agentów z limitem concurrency
- Merge, intra-stage deps, walidacja, kalkulacja dat + scale do OverallEndDate

### UI — WorkScheduleFormModal

**Krok w modalu**:
- Po wybraniu kosztorysu w trybie 'linked' i przed utworzeniem
- Sekcja "Ramy czasowe" z dwoma DatePickerami
- Przycisk "Generuj harmonogram z AI"
- Stan ładowania / error / "Pomiń z ostrzeżeniem"

**API call**: `generateScheduleFromEstimateAI(tenantId, projectId, workScheduleId, overallStartDate, overallEndDate)`

### DB

Brak nowych encji — używamy:
- `WorkScheduleStageWorkPeriod`
- `WorkScheduleStageWorkDependency`

## Powiązane refaktory

Zobacz: `.opencode/subagents/rules/ai-schedule-periods-dependencies-*` (audit + refactor prompts).
Starsze podsumowanie monolitycznego agenta: `ai-schedule-generator-summary.md` (superseded).
