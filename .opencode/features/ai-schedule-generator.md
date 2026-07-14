# AI Schedule Generator — Harmonogram z kosztorysu wspierany przez AI

## Opis

Użytkownik podczas tworzenia harmonogramu na podstawie kosztorysu podaje ramy czasowe
(okno: data rozpoczęcia i zakończenia całego harmonogramu), a agent AI na podstawie
nazw etapów (grup kosztorysowych) i pozycji (zakresów robót) dobiera czasy ich trwania
oraz automatycznie tworzy zależności między zakresami robót.

## Przepływ

1. User tworzy nowy harmonogram w trybie "Na podstawie kosztorysu"
2. Wybiera kosztorys
3. **Nowy krok**: Podaje datę rozpoczęcia i zakończenia całego projektu (ramy czasowe)
4. System wysyła do API żądanie wygenerowania harmonogramu z AI
5. AI analizuje nazwy grup kosztorysowych (etapy) i pozycji (zakresy robót)
6. AI zwraca:
   - Sugerowane czasy trwania (dni) dla każdego zakresu robót
   - Sugerowane zależności (predecessor/successor/dependencyType/lagDays) między zakresami
7. System zapisuje harmonogram z etapami, zakresami, okresami i zależnościami
8. User widzi gotowy harmonogram z wypełnionymi datami i zależnościami

## Wymagane zmiany

### API — Nowy endpoint/CQRS

**Command**: `GenerateScheduleFromEstimateAICommand`
- `TenantId`, `ProjectId`, `WorkScheduleId`
- `OverallStartDate` (DateTime) — ramy czasowe: start
- `OverallEndDate` (DateTime) — ramy czasowe: koniec

**Handler**: `GenerateScheduleFromEstimateAICommandHandler`
- Ładuje harmonogram z linked cost estimate
- Pobiera wszystkie grupy i pozycje z kosztorysu
- Wywołuje AI agent z kontekstem (nazwy etapów, nazwy pozycji, ramy czasowe)
- AI zwraca JSON z durations i dependencies
- Zapisuje okresy (WorkScheduleStageWorkPeriod) do każdego zakresu
- Zapisuje zależności (WorkScheduleStageWorkDependency)
- Unieważnia cache

**Validator**: FluentValidation dla dat

### AI Agent — Nowe narzędzie / agent

**Nowy agent**: `schedule-generator-agent`
- Używa modelu gpt-4o
- Tools: `get_cost_estimate_items`, `get_project_info`
- Prompt: analizuje nazwy grup/items i sugeruje durations + dependencies

**Nowe narzędzie**: `analyze_schedule_structure`
- Input: lista etapów (nazwa, kolejność), lista zakresów (nazwa, etap, kolejność), ramy czasowe
- Output: JSON z durations (workId → liczba dni) i dependencies (predecessorWorkName → successorWorkName → typ → lagDays)

### UI — WorkScheduleFormModal

**Nowy krok w modalu**:
- Po wybraniu kosztorysu w trybie 'linked' i przed utworzeniem
- Pojawia się sekcja "Ramy czasowe" z dwoma DatePickerami: data rozpoczęcia i zakończenia
- Przycisk "Generuj harmonogram z AI"
- Stan ładowania podczas generowania
- Po sukcesie: przekierowanie do widoku harmonogramu z gotowymi danymi

**Nowy API call**: `generateScheduleFromEstimateAI(tenantId, projectId, workScheduleId, overallStartDate, overallEndDate)`

### Nowa warstwa serwisowa

**Interface**: `IWorkScheduleAIGeneratorService`
**Implementacja**: `WorkScheduleAIGeneratorService`
- Odpowiada za komunikację z AI agentem
- Parsuje odpowiedź AI do listy okresów i zależności
- Zapisuje okresy przez istniejący `SetWorkScheduleStageWorkPeriodsCommand`
- Zapisuje zależności przez istniejący `SetWorkScheduleDependenciesCommand`

### Nowa encja / zmiany w DB

Brak — używamy istniejących:
- `WorkScheduleStageWorkPeriod` — do przechowywania okresów
- `WorkScheduleStageWorkDependency` — do przechowywania zależności

## Plan kroków

1. Audyt API — analiza istniejących CQRS, AI agent tools, serwisów
2. Audyt UI — analiza WorkScheduleFormModal, projectApi
3. Implementacja API:
   - a. Nowe narzędzie AI (`analyze_schedule_structure` tool)
   - b. Nowy agent `schedule-generator-agent`
   - c. Nowy serwis `IWorkScheduleAIGeneratorService` / `WorkScheduleAIGeneratorService`
   - d. Nowy CQRS: `GenerateScheduleFromEstimateAI`
   - e. Nowy endpoint w `WorkScheduleController`
   - f. Rejestracja w DI
4. Implementacja UI:
   - a. Nowy API call w `projectApi.ts`
   - b. Nowy typ TypeScript dla odpowiedzi
   - c. Modyfikacja `WorkScheduleFormModal` — dodanie pól ram czasowych i przycisku AI
