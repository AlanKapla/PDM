# Audyt UI — CostEstimate (TreeView + CardView + SchemaManager)

**Data audytu:** 2026-06-11
**Audytor:** UI Audit Agent
**Zakres:** Wszystkie komponenty w `src/components/CostEstimate/` + typy `costEstimate.types.ts` / `costEstimate.types.new.ts`

---

## BLOK 1 — Stan obecny UI

| Komponent/Strona | Lokalizacja | Opis | Powiązane z feature |
|---|---|---|---|
| `CostEstimateModernView` | `CostEstimateModernView.tsx` | Wrapper z toggle Tree/Card View | Główny kontener widoku |
| `CostEstimateTreeView` | `TreeView/CostEstimateTreeView.tsx` | Widok tabelaryczno-drzewiasty (Tablica + hierarchia) | Główny widok |
| `TreeViewHeader` | `TreeView/TreeViewHeader.tsx` | Nagłówki kolumn + zarządzanie kolumnami | Renderowanie kolumn |
| `TreeViewRow` | `TreeView/TreeViewRow.tsx` | Wiersz grupy/pozycji z pełną hierarchią | Renderowanie danych |
| `useTreeViewState` | `TreeView/useTreeViewState.ts` | Hook stanu expanded/collapsed/sort | Zarządzanie stanem |
| `CostEstimateCardView` | `CardView/CostEstimateCardView.tsx` | Widok akordeonowych kart | Alternatywny widok |
| `StageCard` | `CardView/StageCard.tsx` | Karta etapu (grupy głównej) | Renderowanie grup |
| `SubStageSection` | `CardView/SubStageSection.tsx` | Sekcja podetapu z pozycjami | Renderowanie podgrup |
| `PositionCard` | `CardView/PositionCard.tsx` | Karta pojedynczej pozycji | Renderowanie pozycji |
| `PrototypeInputs` | `PrototypeInputs.tsx` | Inline inputy (tekst, numer, tag, dot) | Komponenty UI |
| `PrototypeActionButtons` | `PrototypeActionButtons.tsx` | Ghost buttony, drag handle, chevron | Komponenty UI |
| `SchemaManagerModal` | `SchemaManager/SchemaManagerModal.tsx` | Modal zarządzania schematem | Zarządzanie polami |
| `SchemaPopover` | `SchemaManager/SchemaPopover.tsx` | Popover szybkiej widoczności | Zarządzanie polami |
| `FieldDefinitionRow` | `SchemaManager/FieldDefinitionRow.tsx` | Wiersz definicji pola (drag, rename, delete) | Zarządzanie polami |
| `FieldDefinitionList` | `SchemaManager/FieldDefinitionList.tsx` | Lista wierszy definicji pól | Zarządzanie polami |
| `AddFieldModal` | `SchemaManager/AddFieldModal.tsx` | Modal dodawania nowego pola | Zarządzanie polami |

---

## BLOK 2 — Luki i braki w UI

| # | Brak / Luka | Typ | Priorytet | Opis |
|---|-------------|-----|-----------|------|
| 1 | **Brak obsługi `dateTimeValue` w `getFieldValue`** | hook/komponent | **CRITICAL** | `getFieldValue` zwraca `fv?.stringValue ?? fv?.decimalValue ?? ''` — pomija `dateTimeValue`. Pola typu Data (fieldType=304/305) zawsze pokazują pustą wartość. |
| 2 | **CardView nie renderuje pól dynamicznych** | komponent | **CRITICAL** | `PositionCard`, `SubStagePositionRow` wyświetlają tylko zestaw HARDCODED pól (nazwa, ilość, jednostka, netto, brutto). Pola kalkulowane, generyczne i grupowe ze schematu NIE SĄ wyświetlane w CardView. |
| 3 | **Duplikacja kolumny "Nazwa" w TreeView dla grup** | komponent | **HIGH** | Jeśli GroupName jest w `groupFields` (fieldScope=0), wartość pojawia się 2×: w hardcoded name area (`groupNameValue` l.109-111) i w `renderGroupCustomFields()` (l.136-155 TreeViewRow). |
| 4 | **Duplikacja pola ItemSystemName w TreeView dla pozycji** | komponent | **HIGH** | Jeśli ItemSystemName (fieldType=100) jest w `systemFields` (fieldScope=1), wartość pojawia się 2×: w hardcoded name area (l.567-573) i w `renderSystemField` domyślnym (l.533-543 TreeViewRow). |
| 5 | **Brak kolumn Netto/Brutto w nagłówku TreeView** | komponent | **HIGH** | TreeViewHeader nie zawiera kolumn dla sum Netto i Brutto, ale TreeViewRow (group row) je renderuje (l.203-223). Kolumny danych nie pasują do nagłówków. |
| 6 | **Kolejność pól w nagłówku nie zgadza się z wierszami** | komponent | **HIGH** | Header: System → Group → Calculated → Generic. Group rows: Group fields → Totals. Item rows: System → Calculated → Generic. Group fields są między System a Calculated w headerze, ale w group row są zaraz po name, a item rows nie mają ich wcale. |
| 7 | **Sub-grupy w TreeView nie mogą być zwijane** | komponent | **HIGH** | `TreeViewRow` dla childGroups przekazuje `onToggle={() => {}}` (l.270). Użytkownik nie może zwinąć podetapu w widoku drzewa. |
| 8 | **`AddFieldModal` ma błędne mapowanie wartości fieldType** | komponent | **HIGH** | `300=Tekst` (powinno być Integer/Liczba całkowita), `301=Liczba` (Decimal), `302=Data` (String), `303=Tak/Nie` (Boolean). Tworzy pole złego typu. |
| 9 | **`getTypeLabel` w TreeViewHeader ma błędne mapowanie** | komponent | **HIGH** | `300|301 → Tekst` (powinno: 300=Integer/Liczba, 301=Decimal/Liczba), `302 → Boolean` (String), `303|304 → Data` (303=Boolean, 304=Date → OK). |
| 10 | **Brak obsługi pól typu Collection w renderowaniu** | komponent | **MEDIUM** | `CostEstimateFieldDefinitionWeb` ma `childFields: ... | null` ale żaden komponent nie renderuje zagnieżdżonych pól kolekcji. |
| 11 | **CardView nie używa schematu w PositionCard** | komponent | **MEDIUM** | `PositionCard` przyjmuje `schema` jako prop (l.26) ale go nie używa — zadeklarowany, ale nieużywany. |
| 12 | **PrototypeNumberInput używa `type="text"`** | komponent | **MEDIUM** | `type="text"` (l.62 PrototypeInputs) zamiast `type="number"`. Na urządzeniach mobilnych brak klawiatury numerycznej, a wartość nie jest walidowana jako liczba. |
| 13 | **Niespójne szerokości kolumn akcji** | komponent | **LOW** | Group row akcje: 96px (l.225 TreeViewRow), Item row akcje: 120px (l.618 TreeViewRow). W nagłówku: 120px (l.156 TreeViewHeader). |
| 14 | **`renderGroupCustomFields` nadmiarowe sprawdzanie wartości** | komponent | **LOW** | `fv?.decimalValue !== null && fv?.decimalValue !== undefined` — typ `decimalValue` to `number \| undefined`, więc sprawdzanie `!== null` jest zbędne. |
| 15 | **Hardcodowane GUID-y pól systemowych** | stałe | **LOW** | `FIELD_GROUP_NAME`, `FIELD_ITEM_NAME`, etc. są zakodowane jako stałe GUID-y w TreeViewRow (l.20-27). Jeśli backend zmieni GUID-y, UI przestanie wyświetlać wartości tych pól. |

---

## BLOK 3 — Typy TypeScript

| Typ | Plik | Nowy/Modyfikacja | Opis zmian |
|-----|------|-------------------|------------|
| `CostEstimateFieldValueWeb` | `costEstimate.types.new.ts` | Istniejący | Ma wszystkie potrzebne pola (`stringValue`, `decimalValue`, `boolValue`, `dateTimeValue`). Jest OK. |
| `CostEstimateFieldDefinitionWeb` | `costEstimate.types.new.ts` | Istniejący | Zawiera `id`, `fieldName`, `fieldScope`, `fieldType`, `label`, `isVisible` — OK. |
| `CostEstimateSchemaWeb` | `costEstimate.types.new.ts` | Istniejący | `fieldDefinitions: CostEstimateFieldDefinitionWeb[]` — OK. |
| `CostEstimateDetailsWeb` | `costEstimate.types.new.ts` | Istniejący | Zawiera `schema: CostEstimateSchemaWeb` — OK, zgodne z backendem. |

**Wniosek:** Typy są zgodne z API (CostEstimateDetailsWeb → schema → fieldDefinitions). Problemy leżą po stronie implementacji komponentów, nie typów.

---

## BLOK 4 — Serwisy API (src/api/)

Nie analizowano w tym audycie — skupiono się na warstwie UI. API calls są używane w `SchemaManagerModal.tsx` (l.24: `reorderFieldDefinitions`, `updateFieldDefinition`, `deleteFieldDefinition` z `../../../api/costEstimateApi`).

---

## BLOK 5 — Hooki React Query

Nie analizowano w tym audycie — hooki są w osobnych plikach poza katalogiem `CostEstimate`.

---

## BLOK 6 — Nowe komponenty (nie wymagane)

Aktualny zestaw komponentów jest wystarczający. Wymagane są modyfikacje istniejących, nie nowe komponenty.

---

## BLOK 7 — Modyfikacje istniejących komponentów

| Komponent | Plik | Typ zmiany | Opis |
|-----------|------|-----------|------|
| `TreeViewRow` — `getFieldValue` | `TreeView/TreeViewRow.tsx` | Poprawka | Dodać obsługę `dateTimeValue` w `getFieldValue` (l.94-97) i analogicznie w ItemRow (l.372-375) |
| `TreeViewRow` — `renderGroupCustomFields` | `TreeView/TreeViewRow.tsx` | Poprawka | Filtrować groupFields aby pominąć pole GroupName (fieldId === FIELD_GROUP_NAME), bo jest już wyświetlane w name area |
| `TreeViewRow` — `renderSystemField` | `TreeView/TreeViewRow.tsx` | Poprawka | Dodać filtrowanie systemFields aby pominąć pole ItemSystemName (fieldType=100), bo jest w name area |
| `TreeViewRow` — `onToggle` dla childGroups | `TreeView/TreeViewRow.tsx` | Poprawka | Przekazać prawdziwy toggle zamiast `() => {}` (l.270) — potrzebny handler z collapse możliwością |
| `TreeViewHeader` — kolumny Netto/Brutto | `TreeView/TreeViewHeader.tsx` | Dodanie | Dodać kolumny Netto/Brutto w nagłówku z szerokościami 130px (zgodnie z TreeViewRow l.203-223) |
| `TreeViewHeader` — kolejność pól | `TreeView/TreeViewHeader.tsx` | Poprawka | Uzgodnić kolejność kolumn w headerze z renderowaniem w wierszach. Group row: name → groupFields → Netto → Brutto → Akcje. Item row: name → systemFields → calculatedFields → genericFields → Netto → Brutto → Akcje. |
| `TreeViewHeader` — `getTypeLabel` | `TreeView/TreeViewHeader.tsx` | Poprawka | Naprawić mapowanie typów: 300=Integer, 301=Decimal, 302=String, 303=Boolean, 304=Date, 305=DateTime |
| `CardView` — PositionCard + SubStagePositionRow | `CardView/PositionCard.tsx`, `CardView/SubStageSection.tsx` | Rozszerzenie | Dodać renderowanie pól kalkulowanych i generycznych ze schematu (analogicznie do TreeView), opcjonalnie ukrytych za "Pokaż więcej" |
| `CardView` — StageCard | `CardView/StageCard.tsx` | Rozszerzenie | Dodać renderowanie pól grupowych (groupFields) ze schematu dla nagłówka grupy |
| `AddFieldModal` — mapowanie fieldType | `SchemaManager/AddFieldModal.tsx` | Poprawka | Poprawić wartości opcji: 300=Integer/Liczba całkowita, 301=Decimal/Liczba, 302=String/Tekst, 303=Boolean/Tak-Nie, 304=Date/Data |
| `PrototypeInputs` — `PrototypeNumberInput` | `PrototypeInputs.tsx` | Poprawka | Zmienić `type="text"` na `type="number"` (l.62) |
| `PositionCard` — usunięcie nieużywanego propa | `CardView/PositionCard.tsx` | Czyszczenie | Usunąć nieużywany prop `schema` z interfejsu i komponentu (l.26, 44) |

---

## BLOK 8 — Spójność UI

| Wzorzec | Istniejąca implementacja | Czy feature musi się dostosować |
|---------|------------------------|--------------------------------|
| Renderowanie pól ze schematu | TreeView: dynamiczne renderowanie przez fieldScope | CardView: **TAK** — musi być dostosowane |
| Obsługa wartości dla Date | Brak `getDateValue` / brak obsługi `dateTimeValue` w `getFieldValue` | **TAK** — dodać obsługę |
| Filtrowanie pól systemowych przed duplikacją | Obecnie brak | **TAK** — filtrować GroupName z groupFields, ItemSystemName z systemFields |
| Kolory/typy pól | Niespójne mapowanie (TreeViewHeader vs AddFieldModal) | **TAK** — ujednolicić mapowanie typów |
| Szerokość kolumn akcji | 96px (group), 120px (item), 120px (header) | **TAK** — ujednolicić na 120px |

---

## BLOK 9 — Dostępność (WCAG AA / AXE)

### Kontrast kolorów
| Element | Kolor tekstu | Kolor tła | Kontrast (szac.) | Status |
|---------|-------------|-----------|-----------------|--------|
| ColumnHeader (TreeViewHeader l.74-87) | `neutral.500` | `neutral.50` | ~6.5:1 | ✓ |
| Akcje label "Akcje" (TreeViewHeader l.328) | `neutral.400` | `neutral.50` | ~4.0:1 | ⚠ graniczny dla małego tekstu |
| Hidden field labels (SchemaPopover l.229) | `neutral.400` | white | ~4.5:1 | ⚠ graniczny |

### Atrybuty ARIA
| Komponent | Problem | Rekomendacja |
|-----------|---------|-------------|
| `ChevronButton` w `PrototypeActionButtons.tsx` (l.139-190) | Używa natywnego `<button>` z `aria-label` | ✓ OK |
| `DragHandle` (PrototypeActionButtons l.107-134) | Brak `aria-label` i `role` — używa `<div>` z `cursor: grab` | ⚠ Dodać `aria-label="Przeciągnij aby zmienić kolejność"` i `role="button"`, obsługę klawiatury |
| `PrototypeTag` (PrototypeInputs.tsx l.95-133) | Używa `<span>` — semantycznie OK, brak interakcji | ✓ OK (dekoracyjny) |
| `PrototypeDot` (PrototypeInputs.tsx l.139-164) | Używa `<span>` — dekoracyjny | ✓ OK (dekoracyjny) |
| GhostActionButton (PrototypeActionButtons) | Ma `aria-label` przez prop `label` | ✓ OK |
| Radio button dla opcji (TreeViewRow l.463-481) | Używa `<Box as="button">` z `aria-label` | ✓ OK |

### Zarządzanie fokusem
- `SchemaManagerModal` używa Chakra `Modal` — automatyczny focus trap ✓
- `SchemaPopover` używa Chakra `Popover` — focus management wbudowany ✓
- Custom dropdowny/dropdowny — brak, wszystkie przez Chakra ✓

### Testy AXE
- Brak dedykowanych testów AXE dla komponentów CostEstimate
- **Rekomendacja:** Dodać testy AXE dla `CostEstimateTreeView`, `CostEstimateCardView`, `SchemaManagerModal`

### Podsumowanie dostępności
| Kategoria | Status | Uwagi |
|----------|--------|-------|
| Kontrast kolorów | ✓ | Kolory neutralne spełniają AA, ale `neutral.400` na małym tekście jest graniczne |
| Atrybuty ARIA | ⚠ | `DragHandle` brak `aria-label`/`role`/obsługi klawiatury |
| Klawiatura / fokus | ✓ | Wszystkie modale/popover przez Chakra, focus trap OK |
| Testy AXE | ✗ | Brak testów AXE dla CostEstimate |

---

## BLOK 10 — Problemy i ryzyka

| # | Problem | Komponent/Plik | Ryzyko | Rekomendacja |
|---|---------|---------------|--------|-------------|
| 1 | **Duplikacja kolumny Nazwa** — GroupName i ItemSystemName renderowane 2× | `TreeViewRow.tsx` l.136-155, l.533-543 | Wysokie — użytkownik widzi 2 identyczne kolumny z nazwą, wpisywanie w jedną nie zmienia drugiej | Filtrować GroupName z `groupFields` w `renderGroupCustomFields`; filtrować ItemSystemName z `systemFields` w `renderSystemField` |
| 2 | **Brak obsługi `dateTimeValue`** | `TreeViewRow.tsx` l.94-97, l.372-375 | Wysokie — wartości dat są wyświetlane jako puste, edycja dat nie działa | Dodać `|| fv?.dateTimeValue` do `getFieldValue` |
| 3 | **Błędne mapowanie typów w AddFieldModal** | `AddFieldModal.tsx` l.31-36 | Wysokie — tworzy pole złego typu (np. "Tekst" → backend dostaje fieldType=300=Integer) | Poprawić wartości: 300→Integer/Liczba, 301→Decimal, 302→String/Tekst, 303→Boolean/Tak-Nie, 304→Date/Data |
| 4 | **Błędne etykiety typów w TreeViewHeader** | `TreeViewHeader.tsx` l.61-67 | Średnie — użytkownik widzi "Tekst" dla typu Integer, "Boolean" dla String, itd. | Naprawić mapowanie |
| 5 | **CardView nie obsługuje pól dynamicznych** | `PositionCard.tsx`, `SubStageSection.tsx` | Wysokie — użytkownik dodaje pole przez SchemaManager, ale w CardView go nie widzi | Dodać renderowanie pól ze schematu w CardView |
| 6 | **Brak kolumn Netto/Brutto w nagłówku** | `TreeViewHeader.tsx` | Średnie — kolumny danych nie pasują do nagłówków, dezorientacja użytkownika | Dodać kolumny Netto (130px) i Brutto (130px) z wyrównaniem do prawej |
| 7 | **Sub-grupy w TreeView nie mogą być zwijane** | `TreeViewRow.tsx` l.270 | Średnie — użytkownik nie może zwinąć podetapu | Przekazać prawidłowy `onToggle` z `useTreeViewState` |
| 8 | **`PrototypeNumberInput` używa `type="text"`** | `PrototypeInputs.tsx` l.62 | Niskie — brak klawiatury numerycznej na mobile, brak walidacji liczb | Zmienić na `type="number"` |
| 9 | **Niespójne szerokości akcji** | `TreeViewRow.tsx` l.225 vs l.618 | Niskie — wizualna niespójność | Ujednolicić na 120px |
| 10 | **Hardcodowane GUID-y pól systemowych** | `TreeViewRow.tsx` l.20-27 | Średnie — backend zmienia GUID-y → UI nie wyświetla wartości | Użyć `fieldName` lub mapowania przez typ zamiast GUID-ów |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe komponenty | 0 |
| Zmodyfikowane komponenty | 10 (`TreeViewRow`, `TreeViewHeader`, `PositionCard`, `SubStageSection`, `StageCard`, `AddFieldModal`, `PrototypeInputs`, `SchemaManagerModal`, `SchemaPopover`) |
| Nowe hooki | 0 |
| Nowe typy TypeScript | 0 |
| Nowe wywołania API | 0 |
| Naruszenia WCAG AA | 1 (DragHandle brak aria-label/role) |
| Naruszenia krytyczne (CRITICAL) | 3 (obsługa dateTimeValue, duplikacja kolumn, CardView bez pól dynamicznych) |
| Pytania domenowe | 4 |

---

## Pytania domenowe wymagające decyzji

1. **Czy GroupName i ItemSystemName powinny być wykluczone z `groupFields`/`systemFields`, czy powinny być usunięte z hardcoded name area?** — Obecnie są renderowane 2×. Decyzja: usunąć z renderowania dynamicznego (filtrować), zachować w hardcoded name area.

2. **Czy CardView ma obsługiwać pola dynamiczne (kalkulowane, generyczne, grupowe) tak samo jak TreeView, czy ma pozostać uproszczonym widokiem tylko z podstawowymi polami?** — Jeśli CardView ma być pełnoprawnym widokiem, wymaga dodania renderowania wszystkich pól ze schematu. Jeśli ma być "szybkim podglądem" — należy to udokumentować i dodać informację w UI.

3. **Jakie jest docelowe zachowanie dla pól typu Collection?** — `CostEstimateFieldDefinitionWeb` ma `childFields` dla kolekcji, ale UI ich nie obsługuje. Czy kolekcje będą renderowane w przyszłości?

4. **Czy GUID-y pól systemowych (`00000000-0000-0000-0000-000000000001`, `00000000-0000-0000-0000-000000000100`, etc.) są stabilne w API, czy mogą się zmienić?** — Jeśli są zmienne, UI powinno używać mapowania po `fieldType` / `fieldName` zamiast hardcodowanych GUID-ów.

---

## Overall Assessment: ⚠️ **Wymaga znaczących poprawek przed wdrożeniem**

Komponent CostEstimate jest rozbudowany i ma dobrze zaprojektowaną architekturę (separacja TreeView/CardView, zarządzanie schematem, prototypowe inputy), ale zawiera **3 krytyczne błędy** i **6 poważnych niespójności**, które uniemożliwiają poprawne działanie:

### Najpoważniejsze problemy:

1. **Duplikacja kolumny Nazwa** — najważniejszy błąd UX: użytkownik widzi 2× tę samą nazwę, wprowadzenie wartości w jeden input nie aktualizuje drugiego
2. **Brak obsługi `dateTimeValue`** — wszystkie pola typu Data są niewidoczne i nieedytowalne, mimo że API je zwraca
3. **CardView nie obsługuje pól dynamicznych** — dodanie nowej kolumny przez SchemaManager działa tylko w TreeView, w CardView jest niewidoczna
4. **Błędne mapowanie typów w AddFieldModal** — tworzy pole złego typu (Integer zamiast String, itp.)
5. **Sub-grupy w TreeView nie mogą być zwijane** — blokada podstawowej funkcjonalności UI

### Zalecana kolejność napraw:

1. 🔴 **Natychmiast:** Obsługa `dateTimeValue` w `getFieldValue` (wpływa na wszystkie dane typu Data)
2. 🔴 **Natychmiast:** Filtrowanie GroupName z `groupFields` i ItemSystemName z `systemFields` (eliminacja duplikacji)
3. 🔴 **Natychmiast:** Poprawa mapowania typów w `AddFieldModal` (błędne tworzenie pól)
4. 🟡 **W tej iteracji:** Rozszerzenie CardView o pola dynamiczne
5. 🟡 **W tej iteracji:** Dodanie kolumn Netto/Brutto do TreeViewHeader
6. 🟡 **W tej iteracji:** Naprawa toggla dla sub-grup w TreeView
7. 🟢 **Kosmetyka:** Ujednolicenie szerokości akcji, zmiana `type="text"` na `type="number"`, usunięcie nieużywanego propa `schema` z PositionCard
