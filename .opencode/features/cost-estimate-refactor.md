# Feature: Usprawnienia modułu kosztorysów (CostEstimate)

## Opis

Kompleksowe usprawnienia modułu kosztorysów — poprawa UX, uzupełnienie braków,
refaktor kodu i uspójnienie funkcjonalności między widokami Tree i Card,
oraz między warstwami API i UI.

## Zakres zmian

### 1. Exkluzywna selekcja opcji (Exclusive Selection)

**Stan obecny:**
W backendzie istnieje `CheckExclusiveSelectionAsync` ale jest zablokowane przez TODO
(zwraca `null` zamiast sprawdzać, czy inna opcja już jest zaznaczona).

**Stan docelowy:**
Po zaznaczeniu opcji (radio button), backend sprawdza czy inna opcja w tej samej pozycji
była zaznaczona — jeśli tak, odznacza ją (ustawia `isSelected = false`).
Tylko jedna opcja na pozycję może być zaznaczona.

**Warstwy:**
- API: `CheckExclusiveSelectionAsync` w `CostEstimateFieldValueService`
- API: handler `UpsertFieldValueCommandHandler` — wywołanie metody
- UI: brak zmian (radio button już działa)

### 2. Pole Jednostka — kombobox z predefiniowanymi jednostkami + custom

**Stan obecny:**
Pole jednostka (`ItemSystemUnit`, fieldType=102) to zwykły text input.

**Stan docelowy:**
Dropdown/combobox z predefiniowanymi jednostkami:
- szt, m, m², m³, kg, mb, godz, kpl, t, km, l, opak, r-g, mb
- Możliwość wpisania własnej jednostki (free-text)
- W API: walidacja czy jednostka istnieje na liście (lub jest custom)
- W UI: komponent `UnitComboboxField` lub modyfikacja istniejącego pola

**Warstwy:**
- API: opcjonalna lista predefiniowanych jednostek (enum lub stała)
- API: walidator akceptujący predefiniowane + custom
- UI: nowy komponent `UnitField` z combobox patternem
- UI: autosave działa jak dotychczas

### 3. Uzupełnienie widoku CardView

**Stan obecny:**
`CostEstimateCardView` ma mniej funkcji niż `CostEstimateTreeView`:
- Brak widocznych opcji (radio)
- Brak widocznych komponentów (checkbox)
- Brak edycji pól per-item (tylko podgląd)
- Mniej responsywny układ

**Stan docelowy:**
CardView pokazuje tyle samo co TreeView — opcje, komponenty, wszystkie pola.
Może być alternatywnym układem wizualnym (karty zamiast wierszy), ale funkcjonalnie
ma być ekwiwalentny.

**Warstwy:**
- UI: rozszerzenie `CostEstimateCardView.tsx`
- UI: dodanie `OptionsSection` i `ComponentsSection` w karcie pozycji
- UI: obsługa edycji pól inline w karcie

### 4. Refaktor CostEstimateEditPage (1900+ linii)

**Stan obecny:**
`CostEstimateEditPage.tsx` ma ~1900 linii — miesza logikę stanu, fetchowania,
renderowanie, modale, obsługę błędów, routing.

**Stan docelowy:**
Podział na mniejsze, dedykowane komponenty/hooki:
- `useCostEstimateEditPage` (hook ze stanem strony)
- `CostEstimateEditToolbar` (przyciski akcji, przełącznik widoku)
- `CostEstimateEditModals` (kontrola modali)
- `CostEstimateEditContent` (główna zawartość — TreeView/CardView)

**Warstwy:**
- UI: refaktor pliku — wyciągnięcie logiki
- UI: brak zmian w API

### 5. Schema Editor — dodawanie/usuwanie pól schematu

**Stan obecny:**
`SchemaManagerModal` pozwala zarządzać schematem ale ma ograniczenia:
- Dodaje tylko wybrane typy pól
- Brak walidacji duplikatów nazw
- Pola systemowe nie są oznaczone jako readonly

**Stan docelowy:**
Pełny edytor schematu:
- Dodawanie pól: string, decimal, bool, dateTime
- Usuwanie pól niesystemowych (z potwierdzeniem)
- Oznaczenie pól systemowych jako readonly
- Walidacja duplikatów nazw (client-side + API)
- Podgląd jakie dane zostaną utracone przy usuwaniu pola

**Warstwy:**
- API: walidacja duplikatów nazw w handlerze
- API: endpint do usuwania pola z schematu + kaskadowe usuwanie fieldValues
- UI: `SchemaManagerModal` — usprawnienia
- UI: potwierdzenie usunięcia z informacją o stracie danych

### 6. Silnik obliczeń — testy i spójność

**Stan obecny:**
Silnik obliczeń istnieje w dwóch warstwach (`CostEstimateCalculationService.cs`
i `recalculateCostEstimateDetails.ts`) — logika musi być identyczna.

**Stan docelowy:**
- Testy jednostkowe API: pokrycie `CostEstimateCalculationService`
- Testy jednostkowe UI: pokrycie `recalculateCostEstimateDetails`
- Dokumentacja formuł obliczeniowych w obu warstwach

**Warstwy:**
- API: testy handlerów/serwisów
- UI: testy Vitest

### 7. Obsługa błędów autosave

**Stan obecny:**
`useFieldAutosave` przy błędzie zapisu pokazuje toast ale nie przywraca
poprzedniej wartości w UI (desync).

**Stan docelowy:**
- Przy błędzie zapisu → przywrócenie poprzedniej wartości (rollback optimistic update)
- Wyświetlenie błędu z możliwością ponowienia (retry)
- Timeout requestów (np. 30s)

**Warstwy:**
- UI: `useFieldAutosave` — rollback + retry
- UI: brak zmian w API

## Pliki do zmiany (wstępna lista)

### API
- `src/Business/Implementation/Services/CostEstimateFieldValueService.cs` — exclusive selection
- `src/CQRS/CostEstimates/Commands/UpsertFieldValue/` — handler + validator
- `src/CQRS/CostEstimates/Commands/DeleteFieldDefinition/` — nowy handler
- `src/CQRS/CostEstimates/Validators/` — walidacja jednostek
- `src/Business/Interfaces/WebModels/CostEstimates/` — ewentualnie enum jednostek
- `tests/CQRS.Tests/CostEstimates/` — testy
- `tests/Business.Tests/Services/` — testy serwisu

### UI
- `src/pages/CostEstimateEditPage.tsx` — refaktor
- `src/components/CostEstimate/CostEstimateCardView.tsx` — rozszerzenie
- `src/components/CostEstimate/CostEstimateTreeView.tsx` — ewentualne poprawki
- `src/components/CostEstimate/SchemaManagerModal.tsx` — usprawnienia
- `src/components/CostEstimate/fields/UnitField.tsx` — nowy komponent
- `src/hooks/useFieldAutosave.ts` — rollback + retry
- `src/utils/recalculateCostEstimateDetails.ts` — testy
- `src/types/costEstimate.types.new.ts` — ewentualne typy

## Kolejność wdrożenia (proponowana)

1. **Exclusive selection** — najmniejsza zmiana, can't break anything
2. **Pole jednostka** — odizolowana zmiana w jednym komponencie
3. **Refaktor CostEstimateEditPage** — czysto strukturalny, bez zmiany funkcjonalności
4. **Silnik obliczeń + testy** — po refaktorze strony
5. **CardView** — po refaktorze strony
6. **Schema Editor** — po testach
7. **Obsługa błędów autosave** — na końcu, wymaga testów

## Pytania do zatwierdzenia

1. Czy priorytetyzacja jest poprawna?
2. Czy któryś obszar pominąć na razie?
3. Czy dodać coś czego nie ma na liście?
