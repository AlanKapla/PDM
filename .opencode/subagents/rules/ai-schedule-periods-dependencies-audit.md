# Audyt CQRS/domeny — AI Schedule Periods & Dependencies

**Domena**: `ai-schedule-periods-dependencies`
**Feature**: `ai-schedule-generator` (stan PO wdrożeniu)
**Data**: 2026-07-30
**Zakres**: Jak AI tworzy `WorkScheduleStageWorkPeriod` i `WorkScheduleStageWorkDependency`; CQRS, serwis, agenci, endpoint, DI, testy.
**Ograniczenie**: Raport tylko — bez zmian w kodzie produkcyjnym.

## BLOK 1 — INWENTARYZACJA

| Plik | Typ | Ścieżka |
|------|-----|---------|
| GenerateScheduleFromEstimateAICommand.cs | Command | `src/CQRS/WorkSchedules/GenerateScheduleFromEstimateAI/` |
| GenerateScheduleFromEstimateAICommandHandler.cs | Handler | j.w. |
| GenerateScheduleFromEstimateAICommandValidator.cs | Validator | j.w. |
| SetWorkScheduleStageWorkPeriodsCommand.cs | Command | `src/CQRS/WorkSchedules/SetWorkScheduleStageWorkPeriods/` |
| SetWorkScheduleStageWorkPeriodsCommandHandler.cs | Handler | j.w. |
| SetWorkScheduleStageWorkPeriodsCommandValidator.cs | Validator | j.w. |
| SetWorkScheduleDependenciesCommand.cs | Command | `src/CQRS/WorkSchedules/SetWorkScheduleDependencies/` |
| SetWorkScheduleDependenciesCommandHandler.cs | Handler | j.w. |
| SetWorkScheduleDependenciesCommandValidator.cs | Validator | j.w. |
| WorkScheduleRequestBase.cs / CommandBase / StageWorkCommandBase | Shared base | `src/CQRS/WorkSchedules/Shared/` |
| WorkScheduleDtos.cs (`WorkPeriodDto`, `WorkDependencyDto`) | DTO | j.w. |
| WorkScheduleBuilder.cs | Shared builder | j.w. |
| IWorkScheduleAIGeneratorService.cs + StageInput/WorkInput | Interface + input models | `src/Business/Interfaces/Services/` |
| WorkScheduleAIGeneratorService.cs | Serwis AI (główna logika) | `src/Business/Implementation/Services/` |
| AIScheduleResult.cs (+ WorkPeriodResult, WorkDependencyResult) | WebModel / wynik AI | `src/Business/Interfaces/WebModels/WorkSchedules/` |
| schedule_generator_agent.md | Agent prompt (DEAD — nieużywany) | `src/Business.AIAgent/Resources/Agents/sub_agents/` |
| schedule_duration_agent.md | Agent prompt (duration) | j.w. |
| schedule_dependency_agent.md | Agent prompt (dependency) | j.w. |
| WorkScheduleController.GenerateFromAI | Endpoint | `src/WebApi/Controllers/WorkScheduleController.cs` |
| ServiceCollectionExtensions (DI) | Rejestracja | `src/WebApi/Extensions/ServiceCollectionExtensions.cs` L403 |
| projectApi.generateScheduleFromEstimateAI | UI API client | `01-Applications/.../src/api/projectApi.ts` |
| GenerateScheduleFromEstimateAIRequest | UI typ | `src/types/workSchedule.types.ts` |
| WorkScheduleFormModal.handleGenerateFromAI | UI flow | `src/components/WorkScheduleFormModal.tsx` |
| mockHandlers generate-from-ai | UI mock | `src/api/mock/mockHandlers.ts` |
| WorkScheduleSyncService | Sync kosztorys | `src/Business/Implementation/Services/WorkScheduleSyncService.cs` |

**Testy**: brak plików testowych dla `GenerateScheduleFromEstimateAI` / `WorkScheduleAIGeneratorService` (0 hits w `tests/`).

## BLOK 2 — COMMANDS I QUERIES — STRUKTURA

### 2.1 Positional parameters vs explicit properties

| Command/Query | Używa positional params | Przykład |
|--------------|------------------------|---------|
| GenerateScheduleFromEstimateAICommand | Nie (explicit + baza) | `: WorkScheduleCommandBase` + `OverallStartDate` / `OverallEndDate` |
| SetWorkScheduleStageWorkPeriodsCommand | Nie | `: WorkScheduleStageWorkCommandBase` + `Periods` |
| SetWorkScheduleDependenciesCommand | Nie | `: WorkScheduleCommandBase` + `Dependencies` |
| WorkPeriodDto / WorkDependencyDto | Tak (positional record) | `WorkPeriodDto(StartDate, EndDate, IsClosed)` — akceptowalne dla DTO |

Docelowy wzorzec (sealed + required init) spełniony częściowo: klasy bazowe używają `Guid TenantId { get; init; }` bez `required` — spójne z resztą domeny WorkSchedule.

### 2.2 Sealed

| Command/Query | Jest sealed | Uwagi |
|--------------|------------|-------|
| GenerateScheduleFromEstimateAICommand | Tak | OK |
| SetWorkScheduleStageWorkPeriodsCommand | Tak | OK |
| SetWorkScheduleDependenciesCommand | Tak | OK |
| WorkScheduleCommandBase / RequestBase | abstract record (nie sealed) | OK jako baza |

### 2.3 Interfejsy i autoryzacja

| Command/Query | Interfejs | IAuthorizableRequest | PermissionCode poprawny |
|--------------|-----------|---------------------|------------------------|
| GenerateScheduleFromEstimateAICommand | `IRequestCommand<WorkScheduleDetailsWeb>` via baza | Tak (przez `WorkScheduleRequestBase`) | `PermissionCodes.ProjectSchedule` — OK |
| SetWorkScheduleStageWorkPeriodsCommand | `IRequestCommand<Unit>` | Tak | `ProjectSchedule` — OK |
| SetWorkScheduleDependenciesCommand | `IRequestCommand<WorkScheduleDetailsWeb>` | Tak | `ProjectSchedule` — OK |

Handler AI dodatkowo: `accessService.RequireAdminOrOwnerAsync` — warstwa owner/admin ponad permission policy.

### 2.4 Wspólne pola — kandydaci do klasy bazowej

| Pole wspólne | Występuje w | Kandydat do wydzielenia |
|-------------|------------|------------------------|
| TenantId, ProjectId, WorkScheduleId | Wszystkie 3 commands | Już w `WorkScheduleCommandBase` |
| OverallStartDate / OverallEndDate | Tylko Generate AI | Nie wydzielać |

## BLOK 3 — WALIDATORY

### 3.1 Pokrycie walidatorami

| Command/Query | Walidator | Brakujące reguły |
|--------------|----------|-----------------|
| GenerateScheduleFromEstimateAICommand | Tak, sealed | Brak max długości okna (np. 10 lat); brak limitu „rozsądnej” przyszłości |
| SetWorkScheduleStageWorkPeriodsCommand | Tak, sealed | OK (overlap, End≥Start) |
| SetWorkScheduleDependenciesCommand | Tak, sealed | Brak walidacji enum `DependencyType`; brak detekcji cykli na poziomie walidatora |

### 3.2 Reguły szczegółowe

| Walidator | Pole | Obecna reguła | Brakująca reguła | Uzasadnienie |
|-----------|------|--------------|-----------------|-------------|
| Generate…Validator | TenantId/ProjectId/WorkScheduleId | `RequiredId()` | — | OK |
| Generate…Validator | OverallStart/End | NotEmpty + End>Start + ≥1 dzień | Opcjonalnie max window | Feature nie wymaga |
| SetPeriods Validator | Periods | NotNull, End≥Start, no overlap | — | OK |
| SetDependencies Validator | Dependencies | RequiredId, no self, no duplicate pairs | UniqueIds N/A; cykl | Cykl wykrywany dopiero w serwisie AI (cicho) / AdjustSuccessorPeriods |

### 3.3 Spójność

- Generate / SetDependencies: komunikaty EN.
- SetPeriods `ValidateDependencyConstraintsAsync`: komunikaty **PL** (`"Zależność z …"`) — niespójność EN/PL w ścieżce AI gdy zależności już istnieją przy ponownym ustawianiu okresów.
- Walidatory sealed — OK.
- Nieużywane usingi: nie stwierdzono krytycznych.

### 3.4 Wspólne reguły walidacji

| Reguła wspólna | Walidatory | Kandydat do extension |
|---------------|-----------|----------------------|
| TenantId/ProjectId/WorkScheduleId RequiredId | 3× | Już w CommonValidationExtensions |
| Date range End > Start | Generate + Periods | Opcjonalnie `MustBeAfter(other)` |

## BLOK 4 — HANDLERY (+ serwis AI)

### 4.1 Struktura

| Handler / Serwis | Sealed | Explicit types (brak var) | Uwagi |
|------------------|--------|--------------------------|-------|
| GenerateScheduleFromEstimateAICommandHandler | Tak | Tak | Handle ~130 linii — narusza max ~20 |
| SetWorkScheduleStageWorkPeriodsCommandHandler | Tak | Tak | Handle + prywatne metody walidacji |
| SetWorkScheduleDependenciesCommandHandler | Tak | Tak | AdjustSuccessorPeriods ~80 linii |
| WorkScheduleAIGeneratorService | Tak | Tak | ~794 linie — God-class |
| WorkScheduleController | **Nie** sealed | — | + podejrzane extra `}` na końcu pliku (L533–534) |

### 4.2 Logika biznesowa

| Handler | Linie ~ | Za dużo logiki | Co wydzielić |
|---------|---------|---------------|-------------|
| Generate…Handler | ~130 Handle | Tak | Reload stages/works; batch save periods; orkiestracja zapisu |
| SetPeriods Handler | ~80 Handle + validate | Umiarkowanie | Dependency constraint → serwis współdzielony z SetDependencies |
| SetDependencies Handler | ~100 + Adjust | Tak | AdjustSuccessorPeriods → serwis domenowy (już zduplikowana matematyka z AI CalculateSchedule) |
| WorkScheduleAIGeneratorService | ~794 | Tak | Prompt builders; Parse/Validate; CalculateSchedule (topologia) → osobne klasy |

### 4.3 SOLID i DRY

| Handler | Podobny do | Wspólna logika | Kandydat |
|---------|-----------|---------------|----------|
| AI CalculateSchedule Kahn + dates | SetDependencies.AdjustSuccessorPeriods | Topologia + FinishToStart/SS/FF/SF | Wspólny `WorkScheduleDateCalculator` |
| SetPeriods.ComputeViolationDays | Adjust.ComputeRequiredShift | Identyczna matematyka zależności | Ten sam serwis |
| Generate Handler N× mediator.Send(SetPeriods) | SetPeriods pojedynczo | Replace-all periods | Batch command lub bezpośredni zapis w transakcji |

### 4.4 Obsługa błędów

| Handler / Serwis | Problem | Ryzyko |
|------------------|---------|--------|
| WorkScheduleAIGeneratorService | `InvalidOperationException` przy fail/empty duration agent (L131–146, L170–172) | 500 zamiast ValidationApiException / API domain error |
| WorkScheduleSyncService | `InvalidOperationException` gdy brak CostEstimateId | Handler AI wcześniej sprawdza ValidationApiException — ścieżka sync rzadko, ale niespójna |
| Generate Handler | `?? throw NotFoundApiException` — OK; `is null` na targetWork — OK | — |
| ValidateAIScheduleResult | ValidationApiException — OK | — |
| ParseJson | ValidationApiException na JsonException — OK; null JSON → potem IOE przy empty durations | Niespójny typ wyjątku |

### 4.5 Zapytania do DB

| Handler | Problem | Ryzyko |
|---------|---------|--------|
| Generate…Handler L77–78 | `stageRepo.GetBySearch(s => WorkScheduleId && !IsDeleted)` — **brak TenantId/ProjectId** | Cross-tenant leakage jeśli ID zgadnięte; naruszenie konwencji |
| Generate…Handler L81–84 | `workRepo.GetBySearch(w => StageId != Empty && !IsDeleted)` — **ładuje prawie wszystkie worki w DB**, filtr stageIds w pamięci; brak TenantId/ProjectId | Wydajność + bezpieczeństwo (KRYTYCZNE) |
| SetPeriods ExecuteDeleteAsync | Predykat tylko po `WorkScheduleStageWorkId` (bez TenantId/ProjectId) | Niższe (work wcześniej zwalidowany), nadal naruszenie wzorca |
| SetPeriods otherWorks GetBySearch | `otherWorkIds.Contains(w.Id)` bez TenantId/ProjectId | Naruszenie wzorca |
| SetDependencies existing deps | `WorkScheduleId + TenantId` — **brak ProjectId** | Naruszenie wzorca |
| SetDependencies AdjustSuccessorPeriods | `involvedIds.Contains(w.Id)` bez TenantId/ProjectId | Naruszenie wzorca |
| Generate Handler | Używa `IRepository<>` do read-only reload — powinien `IReadRepository<>` | Konwencja |
| Generate Handler | N wywołań SetPeriods → N× InvalidateCache + N× auth | Wydajność / długi request |
| Nested mediator.Send w TransactionBehavior | Outer transaction reused — OK atomowość | Przy partial failure mid-loop rollback outer — OK; ale koszt pipeline |

## BLOK 5 — WEB MODELE

### 5.1 Sealed record z explicit properties

| WebModel | Sealed record | Explicit properties | Uwagi |
|----------|--------------|--------------------|-------|
| AIScheduleResult | Tak | Tak (`init` lists) | OK |
| WorkPeriodResult | Tak | Tak | OK |
| WorkDependencyResult | Tak | Tak | OK |
| StageInput / WorkInput | Tak | Tak (bez `required`) | W interfejsie serwisu — nietypowa lokalizacja vs WebModels |
| WorkPeriodDto / WorkDependencyDto | Tak | Positional | OK dla shared DTO |
| WorkScheduleDetailsWeb | (istniejący) | — | Zwracany z endpointu |

### 5.2 Duplikacje

| Duplikowane pola | W modelach | Kandydat |
|-----------------|-----------|----------|
| Start/End dates | WorkPeriodResult, WorkPeriodDto, entity Period | Mapowanie w handlerze — akceptowalne |
| Pred/Succ/Type/Lag | WorkDependencyResult, WorkDependencyDto, AIDependency | DTO AI (snake_case) osobno — OK |

Internal AI DTOs (`AIDuration`, `AIDependency`, snake_case) żyją wewnątrz serwisu — OK dla deserializacji JSON z LLM.

## BLOK 6 — PROBLEMY I REKOMENDACJE

#### Krytyczne (błędy logiki lub bezpieczeństwa)

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|-------------|
| K1 | Reload works bez TenantId/ProjectId i z predykatem `StageId != Empty` — skan szeroki | Generate…Handler L81–88 | Cross-tenant data w pamięci; obciążenie DB | `w.TenantId == tenantId && w.ProjectId == projectId && stageIds.Contains(w.WorkScheduleStageId) && !w.IsDeleted` |
| K2 | Reload stages bez TenantId/ProjectId | Generate…Handler L77–78 | Naruszenie izolacji tenanta | Dodać `TenantId` + `ProjectId` do predykatu |
| K3 | `overallEndDate` **nie skaluje** dat — CalculateSchedule ignoruje górną ramę (tylko prompt + fallback EndDate) | WorkScheduleAIGeneratorService.CalculateSchedule | Harmonogram wychodzi poza zadeklarowane okno użytkownika; feature „ramy czasowe” niespełnione | Po topologii: scale/compress durations lub przesuń łańcuch aby `max(End) ≤ overallEndDate`; waliduj overflow |
| K4 | Podwójne planowanie dat: AI CalculateSchedule, potem SetDependencies.AdjustSuccessorPeriods z **inną** formułą FinishToStart (`end+1+lag` vs `end+lag`) | AI service + SetDependencies Handler | Niespójne daty przy innych typach deps / re-run; dryf względem okna | Jedna funkcja domenowa; przy zapisie z AI albo skip Adjust, albo zapis deps przed okresami z jednym kalkulatorem |
| K5 | WorkScheduleController L533–534 — dodatkowe zamykające `}` | WorkScheduleController.cs | Ryzyko błędu kompilacji / uszkodzony plik | Usunąć orphan braces; dodać `sealed` + ProducesResponseType |

#### Wysokie (naruszenia wzorców, duplikacje, brakujące walidacje)

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|-------------|
| W1 | `InvalidOperationException` zamiast `ValidationApiException` przy fail AI duration/dependency | WorkScheduleAIGeneratorService L131–172 | 500 + nies leak | Mapować na ValidationApiException / ConflictApiException |
| W2 | God-class serwis (~794 LOC): prompty, parse, validate, Kahn, merge | WorkScheduleAIGeneratorService | Niemaintainable; brak testów jednostkowych kalkulatora | Wydzielić `ScheduleDurationPromptBuilder`, `ScheduleDependencyMerger`, `WorkScheduleTopologyCalculator` |
| W3 | Handler Generate Handle() ≫ 20 linii; N× `mediator.Send(SetPeriods)` | Generate…Handler | Timeout, N auth checks, N cache invalidate | Batch `SetWorkSchedulePeriodsBulkCommand` lub bezpośredni insert w transakcji; jeden Invalidate na końcu |
| W4 | Max 2 deps / stage + zakaz intra-stage deps | Prompty + dependency agent | Słaba sekwencja wewnątrz etapu; równoległe prace w stage bez FS | Decyzja produktowa: dodać intra-stage FS wg Order **lub** osobny agent ordering |
| W5 | Dead code: `schedule-generator-agent` nigdy nie wywoływany | schedule_generator_agent.md | Drift dokumentacji vs runtime | Usunąć lub oznaczyć obsolete; feature doc aktualizować |
| W6 | `.Result` po `Task.WhenAll` | WorkScheduleAIGeneratorService L124, L163 | Anti-pattern (choć po WhenAll zwykle bezpieczne) | `durationTasks[i].GetAwaiter().GetResult()` unikać → użyć już completed `await` / lokalnych wyników z WhenAll array |
| W7 | Brak detekcji cykli — prace poza topologią cicho ustawiane na `start + Order*2` | CalculateSchedule L705–716 | Ukryty cykl / złe daty bez błędu dla usera | ValidationApiException przy remaining inDegree > 0 |
| W8 | Gdy `aiResult.Dependencies.Count == 0` — **pomijane** SetDependencies → stare zależności mogą zostać | Generate…Handler L152–167 | Stare deps po re-generacji AI | Zawsze wywołać SetDependencies (nawet z pustą listą = clear) |
| W9 | Parametry `stages` i `workScheduleId` nieużywane w serwisie | IWorkScheduleAIGeneratorService / impl | Dead API surface | Usunąć lub użyć stages.Order w promptach dependency |
| W10 | Brak testów jednostkowych AI generator / CalculateSchedule / handler | tests/ | Regresje przy refaktorze okresów/deps | Testy: topologia FS, cykl, scale do end date, merge dedupe, empty deps clear |
| W11 | Predykaty bez ProjectId w SetDependencies / bez TenantId w Adjust loads | SetDependencies Handler | Konwencja bezpieczeństwa | Uzupełnić TenantId+ProjectId wszędzie |
| W12 | 2×N równoległych wywołań GPT-4o (duration+dependency per stage) | WorkScheduleAIGeneratorService | Koszt $ i latency; rate limits Azure OpenAI | Batch stages / semafor concurrency; cache; model lżejszy |

#### Normalne (styl, konwencje, drobne usprawnienia)

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|-------------|
| N1 | Nieużywana zmienna lokalna `key` przy dedupe deps | AI service L188–189 | Warning CS0219 | Usunąć |
| N2 | AgentContext bez UserId / BearerToken | AI service L50–54 | Ograniczone (tools: []) | Uzupełnić z ICurrentUser dla audytu |
| N3 | Endpoint bez `[ProducesResponseType]` | Controller | Słabszy Swagger | Dodać 200/400/404 |
| N4 | Controller nie `sealed` | Controller | Konwencja | sealed |
| N5 | Komunikaty PL w SetPeriods dependency validation | SetPeriods Handler | Niespójność i18n | EN jak reszta API |
| N6 | Duration agent nie dostaje Order etapów z StageInput.Order | Prompty | Słabszy kontekst | Dołączyć order/parent z `stages` |
| N7 | Sync service `InvalidOperationException` | WorkScheduleSyncService | Niespójność wyjątków | ValidationApiException |
| N8 | UI mock generate-from-ai zwraca stały schedule bez okresów AI | mockHandlers.ts | Dev UX | Opcjonalnie mock z periods |
| N9 | IRepository zamiast IReadRepository przy reload | Generate Handler | Konwencja | IReadRepository |
| N10 | Feature doc nadal opisuje monolithic schedule-generator-agent + tool analyze_schedule_structure | ai-schedule-generator.md | Dokumentacja ≠ kod | Zaktualizować do per-stage duration/dependency agents |

## BLOK SPECJALNY — PRZEPŁYW TWORZENIA OKRESÓW I ZALEŻNOŚCI PRZEZ AI

### 1. Wejście (UI → endpoint → command)

1. User w `WorkScheduleFormModal` po utworzeniu harmonogramu linked do kosztorysu podaje `overallStartDate` / `overallEndDate`.
2. `handleGenerateFromAI` → `projectApi.generateScheduleFromEstimateAI(...)` POST body z datami ISO.
3. `POST .../work-schedule/{id}/generate-from-ai` → `GenerateFromAI` ustawia TenantId/ProjectId/WorkScheduleId z route.
4. Pipeline: ValidationBehavior → AuthorizationBehavior (`ProjectSchedule`) → TransactionBehavior → Handler.
5. Handler: `RequireAdminOrOwnerAsync`.

**Problemy**: brak ProducesResponseType; controller braces; UI waliduje daty lokalnie redundantnie z FluentValidation.

### 2. Sync z kosztorysem

1. Load WorkSchedule z Stages/Works (`TenantId+ProjectId` — OK).
2. Wymóg `CostEstimateId` → inaczej ValidationApiException.
3. `workScheduleSyncService.SyncFromCostEstimateAsync` — tworzy/aktualizuje stages i works ze scope items; soft-delete obsolete; usuwa deps dla usuniętych works.

**Problemy**: Sync rzuca InvalidOperationException gdy brak CE (handler już pilnuje); sync queries bez TenantId na groups/stages (wzorzec szerszy niż AI).

### 3. Przygotowanie WorkInput / StageInput

1. Reload allStages po sync (predykat **bez** TenantId/ProjectId) — K2.
2. Reload allWorks (`StageId != Empty`) — K1.
3. Mapowanie StageInput + WorkInput (StageName z dictionary).
4. Empty works → ValidationApiException.

**Problemy**: `stageInputs` przekazywane do AI, ale serwis **ignoruje** `stages` — dependency agent widzi tylko StageName z works, nie ParentStageId/Order z StageInput.

### 4. Per-stage duration agents vs dependency agents (równoległość)

1. Group works by StageId.
2. Dla każdego stage: queue `schedule-duration-agent` (tylko works stage).
3. Dla każdego stage: queue `schedule-dependency-agent` (focus stage + ALL works overview; max 2 cross-stage deps; FinishToStart lag=0).
4. `await Task.WhenAll` obu list — pełna równoległość 2N agentów.
5. `schedule-generator-agent` **nie jest używany** (dead).

**Problemy**: W12 koszt; W4 brak intra-stage; W5 dead agent; `.Result` (W6); AgentContext bez UserId (N2).

### 5. Merge + walidacja odpowiedzi AI

1. Merge durations ze wszystkich stage; fail jeśli agent fail lub empty list → **InvalidOperationException** (W1).
2. Merge dependencies z dedupe po (pred, succ, type); null deps = skip stage.
3. `ValidateAIScheduleResult`: GUID-y, wszystkie works mają duration ≥1, deps referencje, no self, type enum string.
4. **Brak** walidacji: limitu 2 deps/stage egzekwowanego w kodzie; cykli; dopasowania sumy duration do okna czasowego.

### 6. CalculateSchedule (topologia Kahna, daty Start/End)

1. Graph z deps; Kahn; roots start przy `overallStartDate` z małym offsetem `duration*0.1`.
2. FinishToStart: `successorStart = predEnd + 1 + lag`.
3. SS/FF/SF obsługiwane w switch.
4. Clamp `successorStart >= overallStartDate`.
5. Works poza sortowaniem (cykl/izolacja): `overallStart + Order*2` — cicho (W7).
6. **overallEndDate nieużywany do skalowania** (K3) — tylko fallback EndDate w mapowaniu wyniku.
7. Wynik: `AIScheduleResult` z Periods (1 period/work) + Dependencies.

### 7. Zapis okresów (SetWorkScheduleStageWorkPeriods) — kolejność, N wywołań

1. `foreach` po `aiResult.Periods` → osobny `mediator.Send(SetWorkScheduleStageWorkPeriodsCommand)` z jedną `WorkPeriodDto(start, end, IsClosed=false)`.
2. SetPeriods: validate work TenantId/ProjectId; access check; opcjonalnie dependency constraints (przy pierwszym generowaniu deps zwykle jeszcze brak); **delete all periods** worka; insert new; update PlannedStart/End; InvalidateCache.
3. Kolejność: kolejność listy z AI (= kolejność `works` input), nie topologiczna.

**Problemy**: W3 N round-trips; ExecuteDelete bez TenantId; przy istniejąych deps Validate może rzucić PL ValidationApiException jeśli daty kolidują — w happy path AI deps jeszcze nie zapisane.

### 8. Zapis zależności (SetWorkScheduleDependencies) — interakcja z AdjustSuccessorPeriods

1. Jeśli `Dependencies.Count > 0` → jeden `SetWorkScheduleDependenciesCommand` z pełną listą (replace-all diff).
2. Validate IDs należą do schedule.
3. **AdjustSuccessorPeriodsAsync** (Kahn): przesuwa okresy successorów jeśli naruszają constraint — formuła **bez +1 dnia** względem AI (K4).
4. Diff delete/add/update deps; SaveChanges; Invalidate; BuildAsync (wynik zagnieżdżony ignorowany przez outer handler).
5. Jeśli 0 deps z AI → **brak wywołania** → stare deps zostają (W8).

### 9. Cache invalidate + BuildAsync

1. Outer handler: `scheduleCache.InvalidateScheduleAsync` (ponownie po N invalidacjach z SetPeriods + SetDeps).
2. `workScheduleBuilder.BuildAsync` → `WorkScheduleDetailsWeb` → 200 OK.
3. UI: toast sukcesu, navigate do widoku harmonogramu.

**Problemy**: redundantne invalidate; BuildAsync po SetDeps już budował raz (gdy deps>0) — podwójny build.

### Diagram przepływu (skrót)

```
UI dates → POST generate-from-ai → Command
  → access + load schedule
  → SyncFromCostEstimate
  → reload stages/works (⚠ tenant predicates)
  → AI: N duration + N dependency agents ∥
  → merge + validate
  → CalculateSchedule (Kahn; ⚠ no end-date scale)
  → N× SetPeriods (replace periods)
  → 0..1× SetDependencies (+ AdjustSuccessorPeriods)
  → Invalidate + BuildAsync → WorkScheduleDetailsWeb
```

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Liczba Commands (w zakresie) | 3 (Generate AI + SetPeriods + SetDependencies) |
| Liczba Queries | 0 |
| Liczba Walidatorów | 3 |
| Liczba Handlerów | 3 (+ 1 serwis AI sealed) |
| Commands/Queries z positional params | 0 (DTO positional: WorkPeriodDto, WorkDependencyDto) |
| Commands/Queries bez sealed | 0 |
| Queries bez walidatora | 0 (N/A) |
| Handlery z var | 0 |
| Handlery bez sealed | 0 (Controller nie sealed) |
| Agenci AI zdefiniowani | 3 (1 dead: schedule-generator-agent) |
| Agenci używani w runtime | 2 (duration + dependency, per stage) |
| Testy jednostkowe AI schedule | 0 |
| Problemy krytyczne | 5 |
| Problemy wysokie | 12 |
| Problemy normalne | 10 |

## Pytania domenowe wymagające decyzji człowieka

1. **Ramy czasowe (`OverallEndDate`)**: czy system ma **wymuszać** dopasowanie całego łańcucha do okna (scale/compress durations), czy data końca to tylko wskazówka dla LLM w prompcie (obecne zachowanie)?
2. **Zależności wewnątrz etapu**: czy prace w tym samym stage mają iść sekwencyjnie (FS wg `Order`), czy świadomie pozostają równoległe (obecny prompt: zakaz intra-stage)?
3. **Limit „max 2 deps per stage”**: czy to akceptowalny model produktu (rzadki graf cross-stage), czy potrzeba bogatszego łańcucha (więcej poprzedników/następców)?
4. **Re-generacja AI przy istniejących okresach/zależnościach**: czy zawsze **czyszcić** wszystkie zależności i periody przed zapisem wyniku AI (nawet gdy AI zwróci pustą listę deps)?
5. **Źródło prawdy dat**: czy daty z `CalculateSchedule` są finalne (AdjustSuccessorPeriods skip przy generate-from-ai), czy Adjust ma zawsze korygować — wtedy formuła FinishToStart musi być jedna?
6. **Usunięcie `schedule-generator-agent.md`**: usunąć dead resource, czy wrócić do monolitycznego agenta jako fallback gdy stages=1?
7. **Budżet wywołań AI**: czy 2N równoległych gpt-4o jest akceptowalne kosztowo, czy wprowadzić limit concurrency / jeden batch call?
8. **Kontroler `WorkScheduleController` L533–534**: potwierdzić czy extra braces to lokalna korupcja pliku wymagająca natychmiastowej naprawy kompilacji.
