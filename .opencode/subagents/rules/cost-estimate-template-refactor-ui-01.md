# UI-01: Typy TypeScript — podział UiConfigurationWeb, rozszerzenie ExpandedColumn

## Cel
Dostosowanie typów TypeScript do nowej struktury `UiConfigurationWeb` z osobnymi listami `groupColumns` i `itemColumns`. Dodanie `fieldScope` do `ExpandedColumn`.

## Pliki do zmiany

### 1. `src/types/costEstimate.types.ts`

**UiConfigurationWeb** — zmień na:
```typescript
export interface UiConfigurationWeb {
  groupColumns: ColumnConfigurationWeb[];
  itemColumns: ColumnConfigurationWeb[];
}
```

Zachowaj starą właściwość `columns?` jako deprecated (opcjonalnie) dla backward compatibility, lub po prostu usuń jeśli UI nie używa już starego API.

**ColumnConfigurationWeb** — zostaje bez zmian, już ma `fieldScope: number`.

**CostEstimateTemplateStructureWeb** — nie wymaga zmian (nadal ma `uiConfiguration?: UiConfigurationWeb`).

### 2. `src/types/costEstimate.types.new.ts`

Sprawdź czy jest `UiConfigurationWeb` — jeśli tak, zmień analogicznie.
Sprawdź czy jest `CostEstimateTemplateStructureWeb` w nowych typach — jeśli ma `uiConfiguration`, upewnij się że typ jest zgodny.

### 3. `src/components/CostEstimate/costEstimateTableTypes.ts`

**ExpandedColumn** — dodaj pole `fieldScope`:
```typescript
export interface ExpandedColumn {
  fieldId: string;
  label: string;
  width?: string;
  type: 'regular' | 'childField';
  fieldDef?: any;
  childField?: any;
  parentFieldDef?: any;
  originalColumn: any;
  isSortable: boolean;
  isFilterable: boolean;
  isBoolean: boolean;
  isNumeric: boolean;
  fieldScope: number;  // NOWE — wartość z ColumnConfigurationWeb.fieldScope (0=Group, 1-3=Item)
}
```

### 4. `src/components/CostEstimate/costEstimateTableTypes.ts`

**POSITION_COL_MIN_WIDTH** — po usunięciu kolumny "Pozycja" można ją zmniejszyć lub usunąć. Na razie zostaw, ale zmień wartość na `0` lub oznacz jako deprecated:
```typescript
/** @deprecated Kolumna Pozycja została usunięta — pozostawione dla kompatybilności */
export const POSITION_COL_MIN_WIDTH = 0;
```

## Zależności
- Wymaga API-01 (zmiana backend DTO) — ale typy można przygotować niezależnie
- Jest zależnością dla UI-02, UI-03, UI-04
