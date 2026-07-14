# UI Fix 04: Przebudowa TreeView + Header z sort/filter/search

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Przebudowa widoku drzewa: base fields z properties encji, dodatkowe fields z schema, sortowanie, filtrowanie, wyszukiwanie.

## Do zrobienia

### 1. Modyfikacja `CostEstimateTreeView.tsx`

**Propsy** — dostosuj do nowej struktury:
- `details.additionalFields` zamiast `details.schema`
- Usuń starą filtrację `fieldScope` (nie ma już)
- Base fields są teraz properties na item/grup

**Nowe stany**:
```typescript
const [searchQuery, setSearchQuery] = useState('');
const [sortConfig, setSortConfig] = useState<{ field: string; direction: 'asc' | 'desc' } | null>(null);
const [filterField, setFilterField] = useState<string | null>(null);
const [filterValue, setFilterValue] = useState<string>('');
```

**Nowa struktura kolumn — wspólna dla grup i pozycji**:

```typescript
// Definicje kolumn dla base fields
const baseColumns = [
  { id: 'name', label: 'Nazwa', fieldType: 'string', appliesTo: ['group', 'item'] },
  { id: 'quantity', label: 'Ilość', fieldType: 'numeric', appliesTo: ['item'] }, // Puste dla grup
  { id: 'unit', label: 'J.m.', fieldType: 'string', appliesTo: ['item'] },
  { id: 'unitPriceNet', label: 'Cena jedn. netto', fieldType: 'numeric', appliesTo: ['item'] },
  { id: 'vatRate', label: 'VAT', fieldType: 'numeric', appliesTo: ['item'] },
  { id: 'unitPriceGross', label: 'Cena jedn. brutto', fieldType: 'numeric', appliesTo: ['item'] },
  { id: 'netValue', label: 'Wartość netto', fieldType: 'numeric', appliesTo: ['group', 'item'] },
  { id: 'grossValue', label: 'Wartość brutto', fieldType: 'numeric', appliesTo: ['group', 'item'] },
  { id: 'vatValue', label: 'Wartość VAT', fieldType: 'numeric', appliesTo: ['item'] },
];

// Kolumny z pól dodatkowych
const additionalColumns = details.additionalFields.map(f => ({
  id: f.id,
  label: f.name,
  fieldType: ['string', 'decimal', 'boolean', 'datetime'][f.fieldType],
  isAdditional: true,
  appliesTo: ['group', 'item'], // Wspólne dla obu
}));
```

**Widoczność kolumn dla grup**:
- Grupy pokazują tylko: `name`, `netValue`, `grossValue` (z base fields) + additional fields
- Pozycje pokazują wszystkie base fields + additional fields

**Logika wyszukiwania**:
- Search input nad tabelą
- Przeszukuje: `item.name`, `group.name`, oraz wszystkie stringowe additional fields
- Filter: case-insensitive, częściowe dopasowanie

**Logika sortowania**:
- Kliknięcie na nagłówek kolumny → sortowanie
- Kliknięcie ponownie → zmiana kierunku
- Sortowanie dotyczy pozycji w obrębie grupy

**Renderowanie**:
- Grupa: wiersz z nazwą, NetValue, GrossValue + additional fields (Quantity, Unit itd. = puste/—)
- Pozycja: wiersz ze wszystkimi polami
- Opcje/Komponenty: child rows z odpowiednimi polami

### 2. Modyfikacja `TreeViewHeader.tsx`

Dostosuj do nowych kolumn:

```typescript
interface TreeViewHeaderProps {
  baseColumns: ColumnDef[];          // Kolumny base fields
  additionalColumns: ColumnDef[];    // Kolumny pól dodatkowych
  sortConfig: SortConfig | null;
  onSort: (field: string) => void;
  searchQuery: string;
  onSearchChange: (query: string) => void;
}
```

Renderuj nagłówki dla wszystkich kolumn. Dodaj ikonki sortowania (↑↓) przy kliknięciu.

### 3. Nowy komponent: Search input (w TreeViewHeader lub osobno)

```typescript
// SearchInput z debounce 300ms
// Pozycjonowany nad tabelą po lewej stronie
// Placeholder: "Szukaj w kosztorysie..."
```

### 4. Modyfikacja `TreeViewRow.tsx`

Dostosuj do nowych propsów:

```typescript
interface TreeViewRowProps {
  group: CostEstimateGroupWeb;
  level: number;
  isExpanded: boolean;
  isEditMode: boolean;
  baseColumns: ColumnDef[];
  additionalColumns: ColumnDef[];
  additionalFieldDefs: CostEstimateAdditionalFieldWeb[];
  searchQuery: string; // Do podświetlania wyników
  onToggle: () => void;
  // ... pozostałe callbacki
}
```

Renderuj dla grupy:
- Nazwa (z level indent)
- NetValue
- GrossValue
- Additional fields (wartości z group.additionalFieldValues)
- Puste komórki dla item-only fields (Quantity, Unit, itd.)

Renderuj dla pozycji (przez item row):
- Wszystkie base fields
- Additional fields
- Checkbox IsSelected
- Checkbox IsStageWork
- Przycisk plików

### 5. Obsługa pustych pól dla grup

Dla pól które nie dotyczą grup (Quantity, Unit, UnitPriceNet, VatRate, UnitPriceGross, VatValue):
- Wyświetl "—" (puste)
- Nie wyświetlaj inputa edycji

### Build

```powershell
npm run build
```
Jeśli build failed, przerwij i zgłoś błędy.
