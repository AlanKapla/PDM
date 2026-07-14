# Analiza HTML vs Obecny UI — Kosztorysy

**Data:** 2026-06-11  
**Feature:** Dynamiczne schematy kosztorysów — UI redesign

---

## 1. Executive Summary

### HTML (koncepcja UX)
- **Prosty, czysty design** — minimalistyczny, Hanken Grotesk, Spline Sans Mono
- **Dwa widoki:** Tabela-drzewo (desktop) + Karty/outline (mobile)
- **Inline editing** — każde pole edytowalne, focus: `border: brand`, `box-shadow`
- **Zarządzanie kolumnami** — popover z listą (ukryj/odkryj/dodaj/usuń/zmień nazwę)
- **Radio button dla opcji** — wizualne zaznaczenie zamiast checkbox
- **Smooth animations** — expand/collapse z `transition: transform .15s`

### Obecny UI (CostEstimateTableView.tsx)
- **Chakra UI Table** — `<Table>`, `<Thead>`, `<Tbody>`, `<Tr>`, `<Td>`
- **3578 linii kodu** — bardzo złożony, wiele inline logiki
- **DnD Kit** — sortowanie drag & drop grup i pozycji
- **Formularze Chakra** — `<Input>`, `<Select>`, `<Checkbox>`
- **Responsive** — fallback do `CostEstimateMobileView` (osobny komponent)
- **Autosave** — `onFieldAutosave` po każdej zmianie

---

## 2. Struktura wizualna — porównanie

### 2.1 Layout główny

| Element | HTML | Obecny UI |
|---------|------|-----------|
| **Sidebar** | 230px, fixed, sticky | ✅ MainLayout (globalny) |
| **Topbar** | 62px, breadcrumbs + user | ✅ MainLayout (globalny) |
| **Canvas** | scroll area, max-width: 1160px | ✅ Podobne |
| **Doc header** | Tytuł + "Niezapisane" pill | ✅ Podobne (`hasChanges` badge) |
| **Toolbar** | Widoki, Zapisz, Przelicz, Excel, PDF, Udostępnij, Kolumny | ⚠️ Podobne, ale **brak "Kolumny"**, **brak PDF** |
| **Summary cards** | 3 karty (Netto, Brutto, VAT) | ✅ Podobne (`StatGroup`) |
| **Tree view** | Custom CSS grid/flex | ⚠️ Chakra `<Table>` (mniej kontroli nad stylem) |

---

### 2.2 Toolbar — szczegóły

#### HTML
```
[Tabela/Karty toggle] [Zapisz] [Przelicz] [Excel] [PDF] [Udostępnij] [Kolumny (12)] [+ Dodaj etap]
```

#### Obecny UI (CostEstimateToolbar.tsx)
```
[Zapisz] [Przelicz] [Excel] [Udostępnij] [Harmonogram] [+ Dodaj etap]
```

**Różnice:**
- ❌ **Brak przełącznika Tabela/Karty** (obecny UI ma automatyczny breakpoint)
- ❌ **Brak przycisku "Kolumny"** (obecnie kolumny zarządzane przez config, nie przez UI)
- ❌ **Brak przycisku "PDF export"**
- ✅ **Jest "Harmonogram"** (dodatkowa funkcjonalność)

---

### 2.3 Zarządzanie kolumnami

#### HTML — Popover "Kolumny"
```
┌─────────────────────────────────────────────────────────┐
│ Zarządzanie kolumnami                             [×]   │
├─────────────────────────────────────────────────────────┤
│ Kliknij ikonę oka aby ukryć/odkryć kolumnę            │
│                                                          │
│ WIDOCZNE (9)                                            │
│ [👁] Nazwa                    [SYSTEM]    [Pozycje]     │
│ [👁] Ilość                    [SYSTEM]    [Pozycje]     │
│ [👁] Jednostka                [SYSTEM]    [Pozycje]     │
│ [👁] Cena netto              [OBLICZ]    [Pozycje]     │
│ [👁] VAT                     [OBLICZ]    [Pozycje]     │
│ [👁] Cena brutto             [OBLICZ]    [Pozycje]     │
│ [👁] Wartość netto           [OBLICZ]    [Pozycje]     │
│ [👁] Wartość brutto          [OBLICZ]    [Pozycje]     │
│ [👁] Wartość VAT             [OBLICZ]    [Pozycje]     │
│                                                          │
│ UKRYTE (3)                                              │
│ [👁‍🗨] Kategoria               [SYSTEM]    [Pozycje]     │
│ [👁‍🗨] Pliki                   [SYSTEM]    [Pozycje]     │
│ [👁‍🗨] Status budowy           [WŁASNE]    [Pozycje]     │
│                                                          │
│ DODAJ NOWĄ KOLUMNĘ                                      │
│ Nazwa: [_________________]                              │
│ Typ:   [String] [Number] [Bool] [Date]                 │
│ Zakres: [Pozycje ▼]                                     │
│        [Anuluj]  [Dodaj kolumnę]                       │
└─────────────────────────────────────────────────────────┘
```

**Funkcjonalności:**
1. **Ukryj/odkryj** — toggle `IsVisible` na `FieldDefinition`
2. **Zmień nazwę** — inline edit `Label` (kliknięcie w nazwę)
3. **Dodaj kolumnę** — formularz na dole (Nazwa, Typ, Zakres)
4. **Usuń kolumnę** — (tylko dla `IsUserDefined == true`, ikona kosza)
5. **Zmień kolejność** — drag & drop (lub strzałki ↑↓)

#### Obecny UI
- ❌ **Brak UI do zarządzania kolumnami**
- ⚠️ Kolumny są **hardcoded** w `CostEstimateTableView.tsx` (linia ~1000-1500)
- ⚠️ Logika renderowania kolumn zależy od `templateStructure.systemFields`, `calculatedFields`, `genericFields`

---

### 2.4 Widok tabeli (desktop)

#### HTML — Custom CSS Grid/Flex
```html
<div class="tree-card">
  <div class="tree-scroll">
    <div class="thead">
      <div class="th-name">Nazwa</div>
      <div class="th-qty">Ilość</div>
      <div class="th-price">Cena netto</div>
      <!-- ... -->
    </div>
    <div class="trow lvl-etap">
      <div class="tname">
        <button class="chev">▼</button>
        <span class="tag etap">ETAP</span>
        <input class="name-in" value="Nazwa etapu" />
      </div>
      <div class="cell c-qty"><!-- ... --></div>
      <!-- ... -->
    </div>
    <!-- ... -->
  </div>
</div>
```

**Cechy:**
- **Flexbox grid** — każdy wiersz to `display: flex`, kolumny z `flex: 0 0 {width}`
- **Sticky header** — `position: sticky; top: 0; z-index: 4`
- **Inline editing** — każde pole to `<input>` z `border: transparent`, hover/focus zmienia border
- **Nested indent** — każdy poziom ma `padding-left` (etap 0px, podetap 20px, pozycja 40px)
- **Tags** — `<span class="tag etap">ETAP</span>` (kolory z CSS variables)
- **Radio button** — `<button class="radio">` dla opcji

#### Obecny UI — Chakra Table
```tsx
<Table size="sm" variant="simple">
  <Thead position="sticky" top={0} zIndex={1} bg="white">
    <Tr>
      <Th>Nazwa</Th>
      <Th isNumeric>Ilość</Th>
      <Th isNumeric>Cena netto</Th>
      <!-- ... -->
    </Tr>
  </Thead>
  <Tbody>
    <SortableGroupRow group={...} />
    <SortableItemRow item={...} />
    <!-- ... -->
  </Tbody>
</Table>
```

**Cechy:**
- **Chakra Table** — `<Table>`, `<Thead>`, `<Tbody>`, `<Tr>`, `<Td>`
- **DnD Kit** — drag & drop sortowanie (dodatkowa złożoność)
- **Komponenty SortableGroupRow, SortableItemRow** — osobne pliki (~500 linii każdy)
- **Chakra Input** — `<Input size="sm" variant="outline">`
- **Badge** dla tagów — `<Badge colorScheme="blue">ETAP</Badge>`
- **Checkbox** zamiast radio — `<Checkbox isChecked={...} />`

---

### 2.5 Widok kart (mobile)

#### HTML — Karty z chipami dla opcji
```html
<div class="ecard">
  <div class="ecard-head">
    <button class="ec-chev">▼</button>
    <span class="ec-no">1</span>
    <div class="ec-title">Nazwa etapu</div>
    <div class="ec-sums">
      <div class="ec-sum">
        <div class="k">Netto</div>
        <div class="v">120 000 zł</div>
      </div>
      <!-- ... -->
    </div>
  </div>
  <div class="ec-body">
    <div class="pod">
      <div class="pod-head"><!-- ... --></div>
      <div class="pod-body">
        <div class="poz">
          <div class="poz-head"><!-- ... --></div>
          <div class="komp-list">
            <div class="komp">◆ Robocizna</div>
            <div class="komp">◆ Materiał</div>
          </div>
          <div class="opts">
            <div class="opts-lbl">OPCJE (2) <span class="b">wybierz jedną</span></div>
            <div class="chips">
              <button class="chip on">
                <div class="ck">✓</div>
                <div class="cn">Wariant A</div>
                <div class="cprice">5 000 zł netto</div>
              </button>
              <button class="chip">
                <div class="ck"></div>
                <div class="cn">Wariant B</div>
                <div class="cprice">6 000 zł netto</div>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</div>
```

**Cechy:**
- **Karty zagnieżdżone** — `.ecard` (etap) → `.pod` (podetap) → `.poz` (pozycja)
- **Chipy dla opcji** — `.chip` z radio buttonem wewnątrz (`.ck`)
- **Field chips** — custom fields wyświetlane jako `<div class="fieldchips">` z `<div class="fieldchip">`
- **Kolory** — gradient dla etapu, `border-radius: 18px` dla kart

#### Obecny UI — CostEstimateMobileView.tsx
```tsx
<VStack spacing={4}>
  {flattenedGroups.map(group => (
    <Box key={group.id} bg="white" borderRadius="lg" boxShadow="sm">
      <HStack>
        <IconButton icon={<ChevronDown />} />
        <Badge>{group.level === 0 ? 'ETAP' : 'PODETAP'}</Badge>
        <Input value={group.name} />
      </HStack>
      {group.items.map(item => (
        <Box key={item.id}>
          <HStack>
            <Badge>POZYCJA</Badge>
            <Input value={item.name} />
          </HStack>
          {item.options && (
            <RadioGroup>
              {item.options.map(opt => (
                <Radio key={opt.id} value={opt.id}>{opt.name}</Radio>
              ))}
            </RadioGroup>
          )}
        </Box>
      ))}
    </Box>
  ))}
</VStack>
```

**Cechy:**
- **Chakra VStack/HStack** — layout z `spacing`
- **Radio dla opcji** — `<RadioGroup>` + `<Radio>` (Chakra)
- **Prostszy** niż HTML (mniej CSS, więcej Chakra props)

---

## 3. Kolumny — szczegóły

### 3.1 HTML — kolumny dynamiczne

**Standardowe (zawsze widoczne):**
1. **Nazwa** — `flex: 1; min-width: 270px` (elastyczna)
2. **Ilość** — `width: 108px` (z jednostką w osobnym `<span>`)
3. **Cena netto** — `width: 120px` (numeryczna)
4. **Wartość netto** — `width: 130px` (numeryczna)
5. **Wartość brutto** — `width: 130px` (numeryczna)
6. **Akcje** — `width: 96px` (ikony: +, ✎, 🗑)

**Custom (opcjonalne):**
- Dodawane po prawej stronie (przed kolumną "Akcje")
- Width dynamiczny (zależny od typu pola)
- Header: `<div class="th-custom">Label</div>`
- Kolumna: `<div class="c-custom"><input /></div>`

**Przycisk "Dodaj kolumnę":**
- Header: `<div class="th-addcol"><button class="addcol-btn">+</button></div>`
- Kolumna: `<div class="c-addcol-sp"></div>` (spacer)

### 3.2 Obecny UI — kolumny hardcoded

**Kod (linia ~1000-1500):**
```tsx
const visibleColumns = useMemo(() => {
  const cols: ExpandedColumn[] = [
    { id: 'name', label: 'Nazwa', width: 300, minWidth: 200, isResizable: true },
  ];
  
  // Dodaj kolumny z systemFields
  (templateStructure.systemFields || []).forEach(field => {
    if (field.fieldType === FieldType.ItemSystemName) return; // Nazwa już jest
    cols.push({
      id: field.id,
      label: field.label,
      width: 150,
      fieldDefinitionId: field.id,
      fieldType: field.fieldType,
    });
  });
  
  // Dodaj kolumny z calculatedFields
  (templateStructure.calculatedFields || []).forEach(field => {
    cols.push({
      id: field.id,
      label: field.label,
      width: 120,
      fieldDefinitionId: field.id,
      fieldType: field.fieldType,
    });
  });
  
  // Dodaj kolumny z genericFields
  (templateStructure.genericFields || []).forEach(field => {
    cols.push({
      id: field.id,
      label: field.label,
      width: 150,
      fieldDefinitionId: field.id,
      fieldType: field.fieldType,
    });
  });
  
  cols.push({ id: 'actions', label: '', width: 100 });
  
  return cols;
}, [templateStructure]);
```

**Problemy:**
- ❌ **Brak kontroli visibility** — wszystkie pola zawsze widoczne
- ❌ **Brak UI do dodawania pól** — tylko przez backend (dodanie do szablonu)
- ❌ **Kolejność fixed** — systemFields → calculatedFields → genericFields
- ❌ **Width hardcoded** — brak customization przez usera

---

## 4. Radio button dla opcji

### 4.1 HTML — wizualny radio button
```html
<button class="radio on">
  <span class="after"></span> <!-- biały dot wewnątrz -->
</button>
```

**CSS:**
```css
.radio {
  width: 18px; height: 18px;
  border-radius: 50%;
  border: 2px solid var(--line-strong);
  background: var(--surface);
}
.radio.on {
  border-color: var(--brand);
  background: var(--brand);
}
.radio.on::after {
  content: "";
  width: 7px; height: 7px;
  border-radius: 50%;
  background: #fff;
}
```

**Zachowanie:**
- Kliknięcie w radio → zaznaczenie opcji → `boolValue = true` dla `ItemSystemSelected`
- Radio behavior → odznaczenie innych opcji w tej samej grupie
- Kopiowanie wartości z wybranej opcji do pozycji nadrzędnej

### 4.2 Obecny UI — Chakra Checkbox
```tsx
<Checkbox
  isChecked={selectedValue?.boolValue === true}
  onChange={(e) => {
    handleOptionFieldChange(
      group.id,
      item.id,
      option.id,
      selectedFieldDef.id,
      e.target.checked ? 'true' : 'false',
      'system'
    );
  }}
/>
```

**Problemy:**
- ⚠️ **Checkbox zamiast radio** — wizualnie nie pasuje do "wybierz jedną opcję"
- ⚠️ **Logika radio behavior** — jest w `handleOptionFieldChange` (linia ~2600)
- ⚠️ **Brak wizualnego feedbacku** — zaznaczona opcja nie ma highlight tła (w HTML: `background: --brand-tint-2`)

---

## 5. Inline editing

### 5.1 HTML — edycja inline
```html
<input class="name-in" value="Nazwa pozycji" />
```

**CSS:**
```css
.name-in {
  border: 1px solid transparent;
  background: transparent;
  border-radius: 7px;
  padding: 5px 8px;
}
.name-in:hover {
  background: var(--surface);
  border-color: var(--line);
}
.name-in:focus {
  outline: none;
  background: var(--surface);
  border-color: var(--brand);
  box-shadow: 0 0 0 3px var(--brand-tint);
}
```

**Cechy:**
- **Transparent default** — pole wygląda jak tekst dopóki nie najedziesz myszką
- **Smooth focus** — `box-shadow` zamiast outline
- **Autosave** — onChange → debounce 500ms → autosave

### 5.2 Obecny UI — Chakra Input
```tsx
<Input
  value={fieldValue?.stringValue ?? ''}
  onChange={(e) => handleItemFieldChange(...)}
  size="sm"
  variant="outline"
  bg="white"
  borderColor="neutral.300"
  _hover={{ borderColor: 'primary.400' }}
  _focus={{ borderColor: 'primary.500', boxShadow: '0 0 0 1px var(--chakra-colors-primary-500)' }}
/>
```

**Cechy:**
- **Outline zawsze widoczny** — `borderColor: neutral.300` (lightgray)
- **Autosave** — `onFieldAutosave` (prop funkcji) → wywołanie API
- **LocalTextInput component** — hack do uniknięcia gubienia liter (linia ~116)

**Problemy:**
- ⚠️ **Widoczny border** — HTML ma transparent default (czystszy wygląd)
- ⚠️ **LocalTextInput hack** — potrzebny bo rodzic robi `setDetails()` po każdym keystroke
  - **Root cause:** React Query refetch lub state update w rodzicu
  - **HTML approach:** debounce onChange, update parent dopiero po 500ms

---

## 6. Expand/Collapse

### 6.1 HTML — smooth chevron animation
```html
<button class="chev collapsed">
  <svg><!-- chevron down icon --></svg>
</button>
```

**CSS:**
```css
.chev svg {
  transition: transform .15s;
}
.chev.collapsed svg {
  transform: rotate(-90deg); /* chevron right */
}
```

**Zachowanie:**
- Kliknięcie → toggle `collapsed` → animacja `rotate(-90deg)`
- Children (podgrupy/pozycje) → `display: none` jeśli parent collapsed

### 6.2 Obecny UI — Chakra IconButton
```tsx
<IconButton
  icon={isExpanded ? <ChevronDown /> : <ChevronRight />}
  onClick={() => toggleExpand(group.id)}
  size="sm"
  variant="ghost"
/>
```

**Cechy:**
- **Swap icon** — `ChevronDown` ↔ `ChevronRight` (bez animacji rotate)
- **Expanded state** — przechowywany w `expandedGroups` (Set<string>)

**Problemy:**
- ⚠️ **Brak smooth animation** — icon się zmienia instant (bez transition)

---

## 7. Responsive behavior

### 7.1 HTML — breakpoint + scaling
```css
@media (max-width: 760px) {
  .canvas { padding: 18px 14px 56px; }
  .summary .scard { flex: 1; min-width: 140px; }
  .chips { grid-template-columns: 1fr; } /* opcje w jednej kolumnie */
}
```

**Cechy:**
- **Auto-switch** — `< 760px` → widok kart (CSS only)
- **Scaling** — sidebar zwija się do 68px na `< 1080px`
- **Opcje full-width** — chipy układają się w jednej kolumnie

### 7.2 Obecny UI — conditional render
```tsx
const isMobile = useBreakpointValue({ base: true, lg: false });

if (isMobile) {
  return <CostEstimateMobileView details={details} />;
}

return <CostEstimateTableView details={details} />;
```

**Cechy:**
- **Conditional component** — dwa osobne komponenty (TableView vs MobileView)
- **Breakpoint: lg** — `992px` (Chakra default)

**Problemy:**
- ⚠️ **Duplikacja logiki** — wiele funkcji jest kopiowanych w obu komponentach
- ⚠️ **Różne API** — `CostEstimateMobileView` ma inne props niż `TableView`
- ⚠️ **Unmount/remount** — switch między widokami wymaga re-render całego drzewa

---

## 8. Kolory i style

### 8.1 HTML — CSS variables
```css
:root {
  --bg: #F3F5F8;
  --surface: #FFFFFF;
  --line: #E8EBF0;
  --text: #15212F;
  --brand: #2F6CEC;
  --brand-ink: #1E50C0;
  --brand-tint: #EAF1FE;
  --etap: #2F6CEC;
  --podetap: #6E59E6;
  --pozycja: #119D8C;
  --komponent: #C2792B;
}
```

**Cechy:**
- **Semantic variables** — `--surface`, `--line`, `--text` (nie `--gray-100`)
- **Tag colors** — dedykowane zmienne dla każdego typu (etap, podetap, pozycja, komponent)

### 8.2 Obecny UI — Chakra theme
```tsx
<Badge colorScheme="blue">ETAP</Badge>
<Badge colorScheme="purple">PODETAP</Badge>
<Badge colorScheme="green">POZYCJA</Badge>
<Badge colorScheme="orange">KOMPONENT</Badge>
```

**Cechy:**
- **Chakra colorScheme** — używa theme colors (blue.500, purple.500, etc.)
- **Tokens** — `appColors` z `theme/tokens/colors.ts`

**Problemy:**
- ⚠️ **Brak dedykowanych zmiennych** — używamy generycznych `blue`, `purple` (nie `etap`, `podetap`)
- ⚠️ **Trudniej customizować** — zmiana koloru etapu wymaga zmiany `colorScheme` w wielu miejscach

---

## 9. Kluczowe różnice — tabela

| Feature | HTML | Obecny UI | Priorytet zmiany |
|---------|------|-----------|------------------|
| **Zarządzanie kolumnami** | ✅ Popover (ukryj/odkryj/dodaj/usuń/zmień) | ❌ Brak UI | 🔴 MUST |
| **Radio dla opcji** | ✅ Wizualny radio button | ⚠️ Checkbox | 🟡 SHOULD |
| **Inline edit transparent** | ✅ Border transparent default | ⚠️ Border zawsze widoczny | 🟢 NICE |
| **Smooth expand animation** | ✅ Rotate chevron | ⚠️ Swap icon | 🟢 NICE |
| **Widok tabeli** | ✅ Custom CSS Grid/Flex | ⚠️ Chakra Table | 🟡 SHOULD |
| **Widok kart** | ✅ Nested cards + chipy | ⚠️ VStack/HStack | 🟡 SHOULD |
| **PDF export** | ✅ Przycisk w toolbar | ❌ Brak | 🔴 MUST |
| **Custom fields jako kolumny** | ✅ Dynamiczne kolumny | ⚠️ Hardcoded width | 🟡 SHOULD |
| **Tag colors** | ✅ Dedykowane CSS variables | ⚠️ Generic colorScheme | 🟢 NICE |
| **Responsive** | ✅ CSS only (auto-switch) | ⚠️ Conditional render (2 komponenty) | 🟡 SHOULD |

---

## 10. Plan refaktoru UI

### Faza 1: Zarządzanie kolumnami (MUST)
1. **Nowy komponent:** `CostEstimateSchemaManager.tsx`
   - Popover z listą pól (widoczne/ukryte)
   - Inline edit dla `Label`
   - Przycisk "Dodaj kolumnę" (modal)
   - Drag & drop do zmiany kolejności
2. **Hooks:**
   - `useSchemaFields` — fetch fields z `details.schema.fieldDefinitions`
   - `useUpdateFieldDefinition` — mutacja dla `Label`, `IsVisible`, `Order`
   - `useAddFieldDefinition` — mutacja dla nowych pól
   - `useDeleteFieldDefinition` — mutacja dla user-defined pól
3. **Integracja z `CostEstimateTableView`:**
   - Zmiana `visibleColumns` z hardcoded na `details.schema.fieldDefinitions.filter(f => f.isVisible)`

### Faza 2: PDF Export (MUST)
1. **Backend:** `ExportCostEstimateToPdfCommand` + handler (QuestPDF lub DinkToPdf)
2. **Frontend:** Przycisk w `CostEstimateToolbar.tsx`
3. **Endpoint:** `GET /api/tenants/{tenantId}/projects/{projectId}/cost-estimate/{estimateId}/export/pdf`
4. **Response:** Binary PDF (download)

### Faza 3: Radio dla opcji (SHOULD)
1. **Nowy komponent:** `OptionRadioButton.tsx`
   - Wizualny radio button (CSS jak w HTML)
   - `isChecked` prop → `border-color: brand`, `background: brand`, `::after` biały dot
2. **Zamiana w `CostEstimateTableView`:**
   - `<Checkbox />` → `<OptionRadioButton />`
3. **Highlight zaznaczonej opcji:**
   - `<Tr bg={isSelected ? 'primary.50' : 'white'}>`

### Faza 4: Transparent inline editing (NICE)
1. **Nowy komponent:** `TransparentInput.tsx`
   - `border: transparent` default
   - `hover` → `border: line`, `background: surface`
   - `focus` → `border: brand`, `box-shadow: 0 0 0 3px brand-tint`
2. **Zamiana w `CostEstimateTableView`:**
   - `<Input />` → `<TransparentInput />`
3. **Debounce onChange:**
   - `onChange` → `debounce(500ms)` → `onFieldAutosave`
   - Unikamy `LocalTextInput` hack

### Faza 5: Smooth expand animation (NICE)
1. **CSS transition:**
   ```css
   .chevron-icon {
     transition: transform 0.15s ease-in-out;
   }
   .chevron-icon.collapsed {
     transform: rotate(-90deg);
   }
   ```
2. **Zmiana w `SortableGroupRow`:**
   - Zamiast swap icon → rotate `ChevronDown`

### Faza 6: Widok tabeli (Custom Grid) (SHOULD)
1. **Nowy komponent:** `CostEstimateGridView.tsx`
   - Zamiast `<Table>` → `<Box display="flex" flexDirection="column">`
   - Header: `<Box display="flex" position="sticky" top={0}>`
   - Row: `<Box display="flex" borderBottom="1px">`
   - Column: `<Box flex="0 0 {width}px">`
2. **Zalety:**
   - Pełna kontrola nad layout (width, flex, align)
   - Łatwiejsze dodawanie/usuwanie kolumn dynamicznie
   - Brak limitów Chakra Table
3. **Migracja:**
   - Stopniowa — najpierw prototyp, potem zamiana `CostEstimateTableView`

### Faza 7: Widok kart (SHOULD)
1. **Refaktor `CostEstimateMobileView`:**
   - Nested cards z `borderRadius="xl"`, `boxShadow="md"`
   - Opcje jako chipy (`.chip` z HTML)
   - Field chips dla custom fields
2. **Komponenty:**
   - `EtapCard.tsx` — karta etapu
   - `PodetapCard.tsx` — karta podetapu
   - `PozycjaCard.tsx` — karta pozycji
   - `OptionChip.tsx` — chip opcji (selectable)

### Faza 8: Responsive (CSS only) (SHOULD)
1. **Unified component:**
   - Zamiast `if (isMobile) return <MobileView />` → jeden komponent z `@media`
2. **CSS:**
   ```css
   @media (max-width: 760px) {
     .grid-view { display: none; }
     .card-view { display: block; }
   }
   @media (min-width: 761px) {
     .grid-view { display: flex; }
     .card-view { display: none; }
   }
   ```
3. **Zalety:**
   - Brak unmount/remount przy resize
   - Prostsza logika (jeden komponent zamiast dwóch)

---

## 11. Priorytety

### Faza A: MUST (core functionality)
1. ✅ Zarządzanie kolumnami (popover)
2. ✅ PDF export

**Czas:** ~5 dni

### Faza B: SHOULD (UX improvements)
3. ✅ Radio dla opcji
4. ✅ Custom Grid view (zamiast Chakra Table)
5. ✅ Widok kart (refaktor mobile)
6. ✅ Responsive CSS only

**Czas:** ~8 dni

### Faza C: NICE (polish)
7. ✅ Transparent inline editing
8. ✅ Smooth expand animation
9. ✅ Tag colors (CSS variables)

**Czas:** ~2 dni

**Total:** ~15 dni

---

## 12. Rekomendacje

### Do zrobienia najpierw (przed backend)
1. ✅ **Prototyp Schema Manager** (Figma lub HTML mockup) — żeby wiedzieć jakie API potrzebujemy
2. ✅ **Definicja API endpointów** — `POST /schema/fields`, `PUT /schema/fields/{id}`, etc.
3. ✅ **Typy TypeScript** — `CostEstimateSchemaWeb`, `CostEstimateFieldDefinitionWeb`

### Do zrobienia równolegle z backend
1. ✅ **PDF export** — backend robi PDF generator, frontend dodaje przycisk
2. ✅ **Radio button** — frontend zmienia UI, backend już obsługuje logikę

### Do zrobienia po backend (wymaga nowego API)
1. ✅ **Zarządzanie kolumnami** — wymaga `schema` z API (zamiast `templateStructure`)
2. ✅ **Custom Grid view** — wymaga dynamicznych kolumn z `schema.fieldDefinitions`

---

## 13. Pytania do Ciebie

1. **Czy chcesz pełen redesign** (Custom Grid + Nested Cards) czy tylko **dodanie Schema Manager** do obecnego UI?
2. **Czy PDF export ma być prosta tabela** (jak Excel) czy **fancy design** (jak HTML preview)?
3. **Czy chcesz drag & drop do zmiany kolejności kolumn** w Schema Manager, czy wystarczą strzałki ↑↓?
4. **Czy mam zacząć od prototypu Schema Manager** (HTML mockup) czy od backendu (encje + API)?

---

**Koniec analizy HTML vs Obecny UI**
