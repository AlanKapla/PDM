# Refactor 02 — Okresy i zależności: skala do OverallEndDate, formuła FS, intra-stage

## Decyzje domenowe (zaakceptowane)
1. **`OverallEndDate` wymusza dopasowanie**: po topologii, jeśli `max(EndDate) > overallEndDate`, skaluj/kompresuj durations proporcjonalnie (min 1 dzień na pracę) tak aby łańcuch mieścił się w oknie `[overallStartDate, overallEndDate]`. Jeśli nawet przy min 1 dniu się nie mieści — `ValidationApiException`.
2. **Intra-stage deps**: po merge AI, **w kodzie** dodaj sekwencyjne FinishToStart (lag=0) między pracami tego samego stage wg `Order` (work[i] → work[i+1]). Nie polegaj na AI dla intra-stage. Cross-stage nadal z dependency agent (max 2/stage).
3. **Formuła FinishToStart** w `CalculateSchedule` musi być zgodna z `SetWorkScheduleDependenciesCommandHandler.AdjustSuccessorPeriodsAsync`:
   - Adjust: successor start >= `predMaxEnd.AddDays(lagDays)` (bez `+1`)
   - Zmień AI: `successorStart = currentEnd.AddDays(lag)` zamiast `currentEnd.AddDays(1 + lag)` dla FinishToStart default.

## Plik główny
`src/Business/Implementation/Services/WorkScheduleAIGeneratorService.cs`

## Zmiany szczegółowe

### A. Intra-stage dependencies (po merge, przed Validate / Calculate)

Po zbudowaniu `mergedDependencies` (i przed `ValidateAIScheduleResult`):
- Dla każdej grupy works by StageId, posortuj po `Order`, dodaj FS edges między kolejnymi.
- Deduplikuj jak istniejące (HashSet pred|succ|type).
- Używaj prawdziwych GUID stringów (`work.Id.ToString()`).

### B. Align FinishToStart w CalculateSchedule

W switch default / FinishToStart:
```csharp
successorStart = currentEnd.AddDays(lag);
```
(nie `1 + lag`).

Dla default bez edgeDep też: `currentEnd.AddDays(0)` lub `currentEnd` — spójnie z lag=0.

### C. Scale do overallEndDate

Po wyliczeniu start/end dla wszystkich works:
1. Znajdź `maxEnd = endDateByWorkId.Values.Max()`.
2. Jeśli `maxEnd <= overallEndDate` — OK, bez zmian.
3. Jeśli `maxEnd > overallEndDate`:
   - `availableDays = (overallEndDate - overallStartDate).TotalDays + 1` (włącznie)
   - `usedDays = (maxEnd - overallStartDate).TotalDays + 1`
   - `scale = availableDays / usedDays` (jeśli usedDays <= 0, skip)
   - Przeskaluj każdą `duration` (zaokrąglij w dół, min 1), **ponownie** przelicz daty topologią z nowymi durations **albo** liniowo przeskaluj offsety od overallStartDate i ustaw End = Start + scaledDuration - 1, potem clamp do overallEndDate.
4. Po skalowaniu jeśli nadal `max(End) > overallEndDate` → `ValidationApiException` z komunikatem że okno jest zbyt krótkie na liczbę prac.

Rekomendowana prosta strategia (łatwa do testowania):
- Po pierwszej topologii, jeśli overflow: ustaw `scale = availableSpan / currentSpan`, dla każdego worka: `offset = (start - overallStart).TotalDays * scale`, `dur = max(1, round(duration * scale))`, `newStart = overallStart + offset days`, `newEnd = newStart + dur - 1`, clamp `newEnd` do overallEndDate.
- Zachowaj kolejność topologiczną (nie cofaj successor przed predecessor — po skalowaniu opcjonalnie jeden pass Adjust lokalny: jeśli succ.Start < pred.End + lag, przesuń succ).

### D. Użyj StageInput.Order w dependency prompt (opcjonalne, jeśli mało pracy)

Jeśli `stages` jest przekazywane do serwisu — dołącz order do overview w `BuildStageDependencyPrompt`. Nie usuwaj parametru `stages` w tym prompcie (cleanup API w prompt-03).

## Zakaz
- Zakaz `var`
- Nie ruszaj UI
- Nie zmieniaj AdjustSuccessorPeriods w SetDependencies (źródło prawdy) — tylko AI CalculateSchedule

## Weryfikacja
```
dotnet build 02-ApplicationServices/ProductDataManagementWebAPI --configuration Release
```

## Raport
Status build, pliki, blokery.
