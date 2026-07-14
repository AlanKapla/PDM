# UI Audit: Cost Estimate Template Refactor

**Feature:** `.opencode/features/cost-estimate-template-refactor.md`
**Date:** 2026-06-10
**Scope:** CostEstimateTableView, SortableGroupRow, SortableItemRow, SortableOptionRow, SortableComponentRow, CostEstimateExcelView, CostEstimateMobileView, types, hooks

---

## BLOK 1 — Stan obecny UI

### Komponenty / Strony powiązane z feature

| Komponent/Strona | Lokalizacja | Opis | Powiązanie z feature |
|---|---|---|---|
| `CostEstimateTableView.tsx` | `src/components/CostEstimate/` | Główny widok tabeli kosztorysu. Buduje `expandedColumns` z `templateStructure.uiConfiguration.columns`, renderuje pogrupowane wiersze grup/pozycji/opcji/komponentów. | ⭐ Core — źródło kolumn, buduje ExpandedColumn[], rendruje tabelę z sticky kolumną "Pozycja". |
| `SortableGroupRow.tsx` | `src/components/CostEstimate/rows/` | Wiersz grupy (etapu). Iteruje po WSZYSTKICH `expandedColumns`, rozpoznaje pola nagłówkowe przez `groupHeaderFields.find()`. | ⭐ Core — otrzymuje wszystkie kolumny, ale wyświetla tylko group fields. Renderuje ETAP badge. |
| `SortableItemRow.tsx` | `src/components/CostEstimate/rows/` | Wiersz pozycji. Iteruje po WSZYSTKICH `expandedColumns`, pomija pola nagłówkowe grup (wyświetla `—`). Renderuje obszar komponentów i opcji. | ⭐ Core — otrzymuje wszystkie kolumny, ale wyświetla tylko item fields. Renderuje POZYCJA badge. |
| `SortableOptionRow.tsx` | `src/components/CostEstimate/rows/` | Wiersz opcji pozycji. Otrzymuje `expandedColumns`. | 🔗 Pochodny — passthrough kolumn. |
| `SortableComponentRow.tsx` | `src/components/CostEstimate/rows/` | Wiersz komponentu pozycji. Otrzymuje `expandedColumns`. | 🔗 Pochodny — passthrough kolumn. |
| `costEstimateTableTypes.ts` | `src/components/CostEstimate/` | Definiuje `ExpandedColumn`, `FlatRow`, typy callbacków. | ⭐ Core — brak `fieldScope` w `ExpandedColumn`. |
| `CostEstimateExcelView.tsx` | `src/components/` | Starszy widok Excel. Używa `CostEstimateDataModel` i `columnLayout` z `uiConfiguration.columns`. | 🔗 Wtórny — do migracji na nowe typy. |
| `CostEstimateMobileView.tsx` | `src/components/CostEstimate/mobile/` | Widok mobilny na modalach. Nie używa `expandedColumns` bezpośrednio. | 🔗 Wtórny — `MobileFieldInput.getOrderedFields()` czyta `uiConfiguration.columns`. |
| `MobileFieldInput.tsx` | `src/components/CostEstimate/mobile/` | Renderer pól w modalach mobilnych. Zawiera `getOrderedFields()`. | 🔗 Wtórny — czyta `columns` z szablonu. |
| `CostEstimateEditPage.tsx` | `src/pages/` | Strona edycji kosztorysu. ORKIESTRUJE dane. Nie buduje kolumn. | 🔗 Pośredni — potrzebuje dostosowania do nowych typów. |
| `costEstimate.types.ts` | `src/types/` | Stare typy (`CostEstimateDataModel`, `UiConfigurationWeb`, `ColumnConfigurationWeb`). | ⭐ Core — definiuje `ColumnConfigurationWeb.fieldScope`. |
| `costEstimate.types.new.ts` | `src/types/` | Nowe typy (`CostEstimateDetailsWeb`, `FieldDefinitionWeb`, `CostEstimateFieldTypeConfigWeb`). | ⭐ Core — docelowa struktura typów. |
| `FieldRenderer.tsx` | `src/components/` | Renderer pól w komórkach tabeli. | 🔗 Pośredni — bezpośrednio niezmieniany. |
| `resolveFieldDefinition.ts` | `src/utils/` | Utility do wyszukiwania definicji pól po ID/nazwie. | 🔗 Pomocniczy — może być użyty przy refactorze. |

### Przepływ danych w obecnym UI

```
CostEstimateDetailsWeb.templateStructure
  └── uiConfiguration.columns[] (ColumnConfigurationWeb[] — flat, mieszane pola grup i pozycji)
       └── CostEstimateTableView.expandedColumns (ExpandedColumn[])
            ├── SortableGroupRow → iteruje, szuka groupHeaderFields, resztę pomija
            ├── SortableItemRow → iteruje, pomija groupHeaderFields, wyświetla resztę
            ├── SortableOptionRow → passthrough
            └── SortableComponentRow → passthrough
```

**Kluczowy problem:** Wszystkie komponenty wierszy otrzymują tę samą pełną listę kolumn. Każdy z nich sam decyduje, które kolumny wyświetlić. Prowadzi to do:
- Zduplikowanej logiki wyszukiwania `groupHeaderFields` w każdym wierszu
- Renderowania ukrytych kolumn (padding/background dla "—")
- Konieczności przekazywania pełnego `templateStructure` do każdego wiersza

---

## BLOK 2 — Luki i braki w UI

| Brak / Luka | Typ | Priorytet | Opis |
|---|---|---|---|
| Brak podziału expandedColumns na groupColumns i itemColumns | komponent | 🔴 HIGH | `CostEstimateTableView` nie dzieli kolumn. Wszystkie komponenty wierszy otrzymują pełną listę i same filtrują. |
| Brak `fieldScope` w `ExpandedColumn` | typ | 🔴 HIGH | Interfejs nie przechowuje informacji o zakresie pola (Group/ItemSystem/ItemCalculated/ItemGeneric). |
| Kolumna "Pozycja" (sticky z ETAP/POZYCJA) istnieje | komponent | 🔴 HIGH | Feature wymaga jej usunięcia — jest zbędna i myląca. |
| filterAndSortGroups i filterAndSortItems używają expandedColumns + ręcznego wykrywania group/item | hook/logic | 🟡 MEDIUM | Obie funkcje muszą działać na odpowiednio podzielonych kolumnach. |
| `getOrderedFields()` w MobileFieldInput.tsx używa starego wzorca (wyszukiwanie po fieldName) | hook/logic | 🟡 MEDIUM | Może nie działać poprawnie z nową strukturą `fieldId`-based. |
| CostEstimateExcelView używa starych typów `CostEstimateDataModel` i `columnLayout` | komponent | 🟡 MEDIUM | Wymaga migracji na nowe typy lub oznaczenia do deprecacji. |
| Brak mechanizmu zwijania/rozwijania sekcji pól (collapsible field sections) | komponent | 🟢 LOW | Feature spec wspomina o opcjonalnym collapsible UI dla group/item fields. |
| Brak testów AXE dla komponentów wierszy | test | 🟢 LOW | SortableGroupRow, SortableItemRow, SortableOptionRow, SortableComponentRow nie mają testów dostępności. |

---

## BLOK 3 — Typy TypeScript

| Typ | Plik | Nowy/Modyfikacja | Opis zmian |
|---|---|---|---|
| `ExpandedColumn` | `costEstimateTableTypes.ts` | 🔧 Modyfikacja | Dodać pole `fieldScope: number` (FieldScope enum) pochodzące z `ColumnConfigurationWeb.fieldScope`. |
| `GroupExpandedColumn` | `costEstimateTableTypes.ts` | 🆕 Nowy (albo rozszerzenie) | Opcjonalnie: typ pochodny po ExpandedColumn z zakresem Group. |
| `ItemExpandedColumn` | `costEstimateTableTypes.ts` | 🆕 Nowy (albo rozszerzenie) | Opcjonalnie: typ pochodny po ExpandedColumn z zakresem Item. |
| `CostEstimateTableViewProps` | `CostEstimateTableView.tsx` | 🔧 Modyfikacja | Nie zmienia się — nadal przyjmuje `details: CostEstimateDetailsWeb`. |
| `SortableGroupRowProps` | `SortableGroupRow.tsx` | 🔧 Modyfikacja | Zmienić `expandedColumns` na `groupColumns: ExpandedColumn[]`. Usunąć konieczność `templateStructure` do wykrywania group fields. |
| `SortableItemRowProps` | `SortableItemRow.tsx` | 🔧 Modyfikacja | Zmienić `expandedColumns` na `itemColumns: ExpandedColumn[]`. Usunąć konieczność `templateStructure` do pomijania group fields. |
| `SortableOptionRowProps` | `SortableOptionRow.tsx` | 🔧 Modyfikacja | Dopasować do nowej nazwy prop (itemColumns). |
| `SortableComponentRowProps` | `SortableComponentRow.tsx` | 🔧 Modyfikacja | Dopasować do nowej nazwy prop (itemColumns). |
| `ColumnConfigurationWeb` | `costEstimate.types.ts` | ✅ Istnieje | Już ma `fieldScope: number`. Nie wymaga zmian. |
| `UiConfigurationWeb` | `costEstimate.types.ts` | ✅ Istnieje | Już ma `columns: ColumnConfigurationWeb[]`. |

---

## BLOK 4 — Serwisy API (src/api/)

| Funkcja API | Plik | Nowa/Modyfikacja | Endpoint | Opis |
|---|---|---|---|---|
| Brak zmian w API | — | — | — | Nowa struktura `CostEstimateDetailsWeb` jest już zwracana przez API. Klient API w `costEstimateApi.ts` już mapuje odpowiedź. |

**Uwaga:** API nie wymaga zmian dla tego feature — backend już zwraca `CostEstimateDetailsWeb` z `templateStructure.uiConfiguration.columns` gdzie każda kolumna ma `fieldScope`. Zmiany dotyczą wyłącznie warstwy UI (jak konsumujemy te dane).

---

## BLOK 5 — Hooki React Query

| Hook | Plik | Nowy/Modyfikacja | Query/Mutation | Opis |
|---|---|---|---|---|
| `useCostEstimate.ts` | `src/hooks/` | ✅ Istnieje | Query | Już zwraca `CostEstimateDetailsWeb`. Nie wymaga zmian. |
| `useCostEstimate.ts` | `src/hooks/queries/` | ✅ Istnieje | Query | Alternatywna lokalizacja. Do sprawdzenia. |

**Uwaga:** Hooki nie wymagają modyfikacji. Zmiany dotyczą wyłącznie logiki przetwarzania danych w komponentach.

---

## BLOK 6 — Nowe komponenty

| Komponent | Lokalizacja | Opis | Zależy od |
|---|---|---|---|
| Brak nowych komponentów | — | Feature nie wymaga tworzenia nowych komponentów, tylko modyfikacji istniejących | — |

**Decyzja domenowa:** Czy potrzebujemy mechanizmu zwijania/rozwijania sekcji pól (collapsible field sections)? Jeśli tak, może być potrzebny nowy komponent `CollapsibleFieldSection` lub podobny.

---

## BLOK 7 — Modyfikacje istniejących komponentów

### 7.1 CostEstimateTableView.tsx — Główne zmiany

| Co zmienić | Typ zmiany | Opis |
|---|---|---|
| Budowanie `expandedColumns` | 🔧 Logika | W `useMemo` dla `expandedColumns` dodać pole `fieldScope` (już dostępne w `ColumnConfigurationWeb.fieldScope`). |
| Stworzenie `groupColumns` | 🆕 Nowa zmienna | `const groupColumns = useMemo(() => expandedColumns.filter(col => col.fieldScope === FieldScope.Group), [expandedColumns])` |
| Stworzenie `itemColumns` | 🆕 Nowa zmienna | `const itemColumns = useMemo(() => expandedColumns.filter(col => col.fieldScope !== FieldScope.Group), [expandedColumns])` |
| Renderowanie `SortableGroupRow` | 🔧 Props | Zmienić `expandedColumns={expandedColumns}` → `columns={groupColumns}` |
| Renderowanie `SortableItemRow` | 🔧 Props | Zmienić `expandedColumns={expandedColumns}` → `columns={itemColumns}` |
| Renderowanie `SortableOptionRow` | 🔧 Props | Zmienić `expandedColumns={expandedColumns}` → `columns={itemColumns}` (lub passthrough) |
| Renderowanie `SortableComponentRow` | 🔧 Props | Zmienić `expandedColumns={expandedColumns}` → `columns={itemColumns}` (lub passthrough) |
| Renderowanie nagłówka tabeli | 🔧 Logika | Zachować `expandedColumns` dla nagłówka (nadal pokazujemy wszystkie kolumny), lub podzielić na sekcje. |
| Kolumna "Pozycja" | 🔧 Usunięcie | Usunąć cały sticky `<Th>` z "Pozycja" w `renderTableHeader()`. Usunąć sticky `<Td>` z badge ETAP/POZYCJA z `SortableGroupRow` i `SortableItemRow`. |
| `filterAndSortGroups` | 🔧 Logika | Użyć `groupColumns` zamiast `expandedColumns.find()` i ręcznego `isGroupCol()`. |
| `filterAndSortItems` | 🔧 Logika | Użyć `itemColumns` zamiast `expandedColumns.find()` i ręcznego `isItemCol()`. |
| `filterOptions` | 🔧 Logika | Użyć `itemColumns` zamiast `expandedColumns`. |
| Stopka (podsumowanie) | 🔧 Logika | Użyć odpowiednich kolumn dla wyświetlania sum. |
| `getColumnWidth` | 🔧 Logika | Użyć odpowiednich kolumn dla wyliczania całkowitej szerokości tabeli. |
| Szerokość tabeli | 🔧 Logika | `minWidth` na tabeli: usunąć `POSITION_COL_MIN_WIDTH` z sumy. |

### 7.2 SortableGroupRow.tsx

| Co zmienić | Typ zmiany | Opis |
|---|---|---|
| Props: `expandedColumns` → `columns` | 🔧 Props | Zmienić nazwę i semantykę — teraz otrzymuje tylko group columns. |
| Iteracja po kolumnach | 🔧 Logika | Usunąć sprawdzanie `if (col.type === 'childField')` — grupy nie mają child fields. |
| Iteracja po kolumnach | 🔧 Logika | Usunąć `templateStructure.groupHeaderFields?.find(...)` — wszystkie kolumny są grupowe. |
| Sticky "Pozycja" z ETAP badge | 🔧 Usunięcie | Usunąć sticky lewy `<Td>` z numerem ETAP. |
| `templateStructure` w props | 🔧 Props | Możliwe do usunięcia z props (jeśli nie potrzebne do innych rzeczy). |

### 7.3 SortableItemRow.tsx

| Co zmienić | Typ zmiany | Opis |
|---|---|---|
| Props: `expandedColumns` → `columns` | 🔧 Props | Zmienić nazwę i semantykę — teraz otrzymuje tylko item columns. |
| Iteracja po kolumnach | 🔧 Logika | Usunąć blok `if (groupHeaderField)` — wszystkie kolumny są itemowe. |
| Iteracja po kolumnach | 🔧 Logika | Uprościć wyszukiwanie `fieldDef` — `col.fieldDef` jest wystarczające. |
| Sticky "Pozycja" z POZYCJA badge | 🔧 Usunięcie | Usunąć sticky lewy `<Td>` z numerem POZYCJA. |
| Props do SortableOptionRow/SortableComponentRow | 🔧 Props | Przekazać `columns={columns}` (itemColumns). |

### 7.4 SortableOptionRow.tsx i SortableComponentRow.tsx

| Co zmienić | Typ zmiany | Opis |
|---|---|---|
| Props: `expandedColumns` → `columns` | 🔧 Props | Dopasowanie nazwy prop. |
| Iteracja po kolumnach | 🔧 Minor | Ewentualne uproszczenie — nie trzeba już pomijać group fields. |

### 7.5 CostEstimateTableView.tsx — renderTableHeader()

| Co zmienić | Typ zmiany | Opis |
|---|---|---|
| Sticky kolumna "Akcje" | 🔧 Zachować | Jeśli `canStructuralEdit` — pozostaje. |
| Sticky kolumna "Pozycja" | 🔧 Usunąć | Cały `<Th>` z "Pozycja" do usunięcia. |
| Resize uchwyty | 🔧 Zachować | Pozostają na każdej kolumnie. |

### 7.6 CostEstimateExcelView.tsx

| Co zmienić | Typ zmiany | Opis |
|---|---|---|
| `columnLayout` z `uiConfiguration.columns` | 🔧 Minor | Obecnie używa starego interfejsu. Sprawdzić czy nadal działa z `CostEstimateDetailsWeb`. |
| Typy `CostEstimateDataModel` | 🔧 Refactor | Docelowo: migracja na nowe typy lub oznaczenie jako deprecated. |

### 7.7 CostEstimateMobileView.tsx & MobileFieldInput.tsx

| Co zmienić | Typ zmiany | Opis |
|---|---|---|
| `getOrderedFields()` | 🔧 Minor | Obecnie filtruje groupHeaderFields z columns. W nowej strukturze nie będzie to potrzebne — kolumny są już oznaczone fieldScope. |
| Import nowych typów | 🔧 Minor | Upewnić się że używa `CostEstimateDetailsWeb` z nowych typów. |

---

## BLOK 8 — Spójność UI

| Wzorzec | Istniejąca implementacja | Czy feature musi się dostosować |
|---|---|---|
| Nazewnictwo prop `expandedColumns` | Używane we wszystkich komponentach wierszy | ✅ Tak — zmienić na `columns` (lub `groupColumns`/`itemColumns`) |
| Obsługa błędów (puste wartości) | `getItemFieldValueForColumn` zwraca `undefined` → `formatDisplayValue` pokazuje `—` | ✅ Nie zmienia się |
| Obsługa loadowania | Brak — dane są już załadowane przez React Query | ✅ Nie zmienia się |
| Formatowanie walut | `toLocaleString('pl-PL', { minimumFractionDigits: 2 })` | ✅ Nie zmienia się |
| Drag & drop (DnD) | Używa `@dnd-kit` z `SortableContext` na `flatRows` | ✅ Po usunięciu kolumny "Pozycja" — DnD pozostaje. Numeracja wierszy może być przesunięta. |
| Clickable row pattern | `cursor="pointer"` na `SortableGroupRow` | ⚠️ Sprawdzić czy zachowanie expand/collapse pozostanie niezmienione. |
| Kolorystyka | Chakra UI tokens + `appColors` | ✅ Nie zmienia się |
| Obsługa `isReadOnly` Restricted users | `accessLevel === CostEstimateAccessLevel.Restricted` → `canEditFields` | ✅ Nie zmienia się |
| Użycie `POSITION_COL_MIN_WIDTH` | Stała w `costEstimateTableTypes.ts` | ⚠️ Po usunięciu kolumny "Pozycja" — stała może być niepotrzebna |

---

## BLOK 9 — Dostępność (WCAG AA / AXE)

### 9.1 Kontrast kolorów

| Element | Kolor tekstu | Kolor tła | Kontrast (szac.) | Status |
|---|---|---|---|---|
| Nagłówki tabeli | `neutral.500` (#A0AEC0) | white | ~4.5:1 | ⚠️ Graniczny dla małego tekstu (fontSize="xs" / "sm") |
| Tekst "Pozycja" w badge ETAP/POZYCJA | `neutral.600` (#718096) | `primary.100` / `level1.100` | ~6:1+ | ✓ |
| Placeholdery filtrów | `neutral.400` (#CBD5E0) | `neutral.50` (#FAFAFA) | ~2.5:1 | ✗ Zbyt niski dla WCAG AA (potrzeba ≥3:1 dla dużego, ≥4.5:1 dla małego) |
| Tekst "—" w pustych komórkach | `neutral.400` | white | ~2.5:1 | ✗ Zbyt niski |
| Filtry placeholder | `neutral.400` (#CBD5E0) | `neutral.50` (#FAFAFA) | ~2.5:1 | ✗ Zbyt niski |

### 9.2 Atrybuty ARIA

| Komponent | Problem | Rekomendacja |
|---|---|---|
| `<IconButton>` sortowania | `aria-label` istnieje (np. "Sortuj") | ✓ OK |
| `<IconButton>` wyczyść filtr | `aria-label="Wyczyść filtr"` | ✓ OK |
| `<IconButton>` expand/collapse | `aria-label` istnieje? | ⚠️ Sprawdzić w SortableGroupRow |
| `<Tr>` z `cursor="pointer"` + `onClick` | Brak `role="button"` i `tabIndex` | ⚠️ Wiersze grup z expand/collapse powinny mieć `role="button" tabIndex={0}` |
| `<Badge>` "ETAP" / "POZYCJA" | Czysto dekoracyjne? | ⚠️ Dodać `aria-hidden="true"` jeśli nie niesie treści (lub usunąć — feature wymaga usunięcia) |
| `<IconButton>` drag handle | Brak analizy | ⚠️ Sprawdzić czy ma `aria-label` |

### 9.3 Zarządzanie fokusem

| Element | Status | Uwagi |
|---|---|---|
| Expand/Collapse grupy | ⚠️ Sprawdzić | Kliknięcie w wiersz grupy expanduje/collapsuje. Fokus powinien pozostać na aktywowanym elemencie. |
| Filtry w nagłówkach | ✓ OK | Inputy filtrów są osiągalne tabem. |
| Sortowanie | ✓ OK | Przycisk sortowania jest osiągalny. |
| Edycja inline | ⚠️ Sprawdzić | Pola edytowalne inline — czy są osiągalne klawiaturą (Tab order)? |
| Drag & drop | ⚠️ Sprawdzić | `@dnd-kit` obsługuje KeyboardSensor, ale rękojeść drag powinna być osiągalna. |

### 9.4 Testy AXE

| Komponent | Status | Uwagi |
|---|---|---|
| `CostEstimateTableView` | ❌ Brak testu | Dodać test `*.axe.test.tsx` |
| `SortableGroupRow` | ❌ Brak testu | Dodać test |
| `SortableItemRow` | ❌ Brak testu | Dodać test |
| `SortableOptionRow` | ❌ Brak testu | Dodać test |
| `SortableComponentRow` | ❌ Brak testu | Dodać test |

### Podsumowanie dostępności

| Kategoria | Status | Uwagi |
|---|---|---|
| Kontrast kolorów | ⚠️ | Placeholdery filtrów (`neutral.400` na `neutral.50`) i puste komórki poniżej progu WCAG AA. |
| Atrybuty ARIA | ⚠️ | Wiersze grup z expand/collapse potrzebują `role="button"` i `tabIndex`. |
| Klawiatura / fokus | ⚠️ | Edycja inline i drag & drop wymagają weryfikacji. |
| Testy AXE | ✗ | Żaden z komponentów wierszy nie ma testów AXE. |

---

## BLOK 10 — Problemy i ryzyka

| # | Problem | Komponent/Plik | Ryzyko | Rekomendacja |
|---|---|---|---|---|
| 1 | `filterAndSortGroups` i `filterAndSortItems` używają `expandedColumns` do wyszukiwania kolumn po `fieldId` | `CostEstimateTableView.tsx` (linie 689-842) | 🟡 Średnie — Po podziale na group/item columns, logika filtrowania/sortowania musi działać na odpowiednim podzbiorze kolumn. | Przed refactorem upewnić się, że każdy filtr/sort ma jednoznacznie określony zakres (group lub item). |
| 2 | `SortableGroupRow` zawiera logikę sumowania (`shouldSum`, `summaryValues`) która używa `expandedColumns` | `SortableGroupRow.tsx` (linie 291-375) | 🟡 Średnie — Sumy grupowe pozostają, ale iteracja po kolumnach będzie tylko po group columns. | Upewnić się, że pola sumowane (calculated z `sumInGroup`) mają `fieldScope === Group`. Jeśli nie — dodać mapowanie. |
| 3 | `SortableItemRow` zawiera logikę wykrywania fieldDef z `templateStructure` jako fallback | `SortableItemRow.tsx` (linie 323-349) | 🟡 Średnie — Po refactorze `col.fieldDef` powinien być zawsze dostępny dla item columns. | Upewnić się że `expandedColumns` building poprawnie przypisuje `fieldDef` dla wszystkich kolumn. |
| 4 | `MobileFieldInput.getOrderedFields()` wyszukuje pola po `fieldName` | `MobileFieldInput.tsx` (linie 303-328) | 🟢 Niskie — Może nie znaleźć pola jeśli `fieldName` się zmieni. | Użyć `fieldId` zamiast `fieldName` lub użyć nowego `fieldScope` do filtrowania. |
| 5 | Usunięcie kolumny "Pozycja" zmieni szerokość tabeli i przesunięcie sticky kolumn | `CostEstimateTableView.tsx` | 🟢 Niskie — `POSITION_COL_MIN_WIDTH` (ok. 100px) znika z całkowitej szerokości. `left` pozycjonowanie sticky kolumny Akcje musi być zaktualizowane (z `left: 0` zamiast `left: 120px`). | Po usunięciu: sticky Actions column `left={0}` (bez przesunięcia o Pozycja column). |
| 6 | CostEstimateExcelView może być niekompatybilny z nową strukturą | `CostEstimateExcelView.tsx` | 🟡 Średnie — Używa starych typów `CostEstimateDataModel`. Jeśli API przestanie zwracać stare typy, widok przestanie działać. | Oznaczyć jako deprecated lub zmigrować na nowe typy. |
| 7 | Brak testów — ryzyko regresji przy refactorze | Wszystkie komponenty | 🔴 Wysokie — Zmiany w logice budowania kolumn i renderowania wierszy mogą wprowadzić regresje w displayu danych, sumowaniu, drag & drop. | Dodać testy jednostkowe i/lub testy wizualne przed refactorem. |
| 8 | `flatRows` budowa używa `expandedColumns` w filtrowaniu/sortowaniu | `CostEstimateTableView.tsx` (linie 847-892) | 🟡 Średnie — `flatRows` to płaska lista wszystkich wierszy (grup + pozycji). Filtrowanie/sortowanie musi być zgodne z nowym podziałem. | Upewnić się że grupy są filtrowane/sortowane przez `groupColumns`, a pozycje przez `itemColumns`. |
| 9 | Kolumna z checkboxem "Widoczność kolumny" (`isVisible`) nie jest obsługiwana | `ColumnConfigurationWeb` ma `isVisible?: boolean` | 🟢 Niskie — Obecnie nie używane. Jeśli frontend ma ukrywać kolumny, to osobny feature. | Nie blokuje refactora. |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---|---|
| Nowe komponenty | 0 |
| Zmodyfikowane komponenty | 5 (CostEstimateTableView, SortableGroupRow, SortableItemRow, SortableOptionRow, SortableComponentRow) |
| Zmodyfikowane strony | 1 (CostEstimateEditPage — minor) |
| Zmodyfikowane widoki | 1 (CostEstimateExcelView — migracja typów) |
| Zmodyfikowane widoki mobilne | 1 (CostEstimateMobileView/MobileFieldInput — minor) |
| Nowe hooki | 0 |
| Nowe typy TypeScript | 0 (tylko modyfikacja `ExpandedColumn`) |
| Zmodyfikowane typy | 1 (`ExpandedColumn` — dodać `fieldScope`) |
| Nowe wywołania API | 0 |
| Naruszenia WCAG AA | 2 (placeholdery filtrów, puste komórki) |
| Brak testów AXE | 5 komponentów |
| Pytania domenowe | 3 |

---

## Pytania domenowe wymagające decyzji

1. **Collapsible field sections** — Czy interfejs użytkownika powinien umożliwiać zwijanie/rozwijanie sekcji pól grup i pozycji w widoku tabeli (feature spec pkt 4)? Jeśli tak, jaki ma być mechanizm (przycisk expand/collapse na górze tabeli, czy per sekcja)?

2. **`isVisible` na kolumnach** — `ColumnConfigurationWeb` ma pole `isVisible?: boolean`. Czy frontend powinien ukrywać nievisible kolumny? Jeśli tak, expandColumns building powinien filtrować po `isVisible !== false`.

3. **Nazewnictwo prop w row components** — Czy zmieniamy `expandedColumns` na `columns` (prościej) czy na `groupColumns`/`itemColumns` (bardziej opisowo)? Rekomendacja: `columns` dla spójności — każdy row component wie jaki zakres otrzymuje.
