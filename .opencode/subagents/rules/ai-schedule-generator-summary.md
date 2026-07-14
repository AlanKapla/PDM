# Feature Summary — AI Schedule Generator

**Feature**: `ai-schedule-generator`
**Date**: 2026-06-09
**Status**: ✅ Wdrożony

## Co zostało zrobione

### API Layer (9 plików)

| # | Plik | Typ | Opis |
|---|------|-----|------|
| 1 | `Business/Interfaces/WebModels/WorkSchedules/AIScheduleResult.cs` | NOWY | DTO: AIScheduleResult, WorkPeriodResult, WorkDependencyResult |
| 2 | `Business/Interfaces/Services/IWorkScheduleAIGeneratorService.cs` | NOWY | Interfejs serwisu AI + StageInput, WorkInput |
| 3 | `Business/Implementation/Services/WorkScheduleAIGeneratorService.cs` | NOWY | Implementacja: budowa promptu → wywołanie AI → parsowanie JSON → algorytm Kahna (sortowanie topologiczne) → dystrybucja dat |
| 4 | `Business.AIAgent/Resources/Agents/sub_agents/schedule_generator_agent.md` | NOWY | Agent AI: gpt-4o, temp 0.3, 1 iteracja, reguły duration/dependency, JSON output |
| 5 | `CQRS/WorkSchedules/GenerateScheduleFromEstimateAI/GenerateScheduleFromEstimateAICommand.cs` | NOWY | Command z OverallStartDate, OverallEndDate |
| 6 | `CQRS/WorkSchedules/GenerateScheduleFromEstimateAI/GenerateScheduleFromEstimateAICommandHandler.cs` | NOWY | Handler: access check → sync z kosztorysem → load danych → AI → zapis okresów → zapis zależności → cache → builder |
| 7 | `CQRS/WorkSchedules/GenerateScheduleFromEstimateAI/GenerateScheduleFromEstimateAICommandValidator.cs` | NOWY | Walidator: RequiredId, daty, zakres czasowy |
| 8 | `WebApi/Controllers/WorkScheduleController.cs` | MODYFIKACJA | Nowy endpoint `POST {id}/generate-from-ai` + import |
| 9 | `WebApi/Extensions/ServiceCollectionExtensions.cs` | MODYFIKACJA | Rejestracja `IWorkScheduleAIGeneratorService` w DI |

### UI Layer (3 pliki)

| # | Plik | Typ | Opis |
|---|------|-----|------|
| 1 | `src/types/workSchedule.types.ts` | MODYFIKACJA | Nowy typ `GenerateScheduleFromEstimateAIRequest` |
| 2 | `src/api/projectApi.ts` | MODYFIKACJA | Nowa metoda `generateScheduleFromEstimateAI` + import typu |
| 3 | `src/components/WorkScheduleFormModal.tsx` | MODYFIKACJA | 10 zmian: stany AI, nowy przepływ create, handler AI, sekcja date picker + przycisk + loading + error + "Pomiń z ostrzeżeniem", blokada modala |

### Build verification
- **API**: `dotnet build --configuration Release` — ✅ Kompilacja powiodła się (0 błędów)
- **UI**: `npm run build` — ✅ built in 11.23s (0 błędów)

## Przepływ użytkownika

```
Kosztorys → "Utwórz harmonogram" → modal → wpisz nazwę → submit
  → Harmonogram utworzony (sync z kosztorysem automatycznie)
  → Krok 2: Data rozpoczęcia / zakończenia (domyślnie dzisiaj + 30 dni)
  → "Generuj harmonogram z AI"
    → AI analizuje nazwy etapów i zakresów
    → backend synchronizuje, wywołuje AI, zapisuje okresy i zależności
    → Sukces → nawigacja do widoku harmonogramu
  → "Pomiń" (2 kliknięcia z ostrzeżeniem)
    → Nawigacja do widoku harmonogramu (bez okresów i zależności)
```

## Blokery
Brak

## Uwagi
- AI generowanie jest synchroniczne (AJAX) — user czeka kilka sekund
- Backend wykonuje sync z kosztorysem przed AI (wewnątrz handlera)
- Kolejność zapisu: okresy → zależności (handler zależności dostosowuje okresy)
- Nazwy zakresów mapowane przez ID (nie przez nazwę) — unika problemów z duplikatami
- Przycisk "Pomiń" wymaga 2 kliknięć: pierwsze pokazuje ostrzeżenie, drugie potwierdza
