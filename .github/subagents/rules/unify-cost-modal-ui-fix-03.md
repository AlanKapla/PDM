# UI Fix-03 — Wspólny CostModal z trybem

## Cel

Stworzyć jeden modal `CostModal` z trybem `type: "tracked" | "project"` zastępujący:
- `src/features/dashboard/components/TrackedCostModal.tsx`
- `src/components/ExpenseFormModal.tsx`

## Kontekst

Raport UI audytu: `.github/subagents/rules/unify-cost-modal-ui-audit.md`
Decyzja architektoniczna: jeden modal z trybem, NIE dwa osobne komponenty.

## Pola formularza

### Wspólne (oba tryby)

| Pole | Label | Typ | Walidacja |
|------|-------|-----|-----------|
| `name` | "Nazwa" | text input | required |
| `description` | "Opis" | textarea | opcjonalne, max 2000 |
| `net` | "Kwota netto" | number input (step=0.01) | opcjonalne, >= 0 |
| `gross` | "Kwota brutto" | number input (step=0.01) | opcjonalne, >= 0 |
| `contractor` | "Wykonawca" | text input | opcjonalne, max 300 |
| `date` | "Data" | date input | opcjonalne |
| `number` | "Numer faktury" | text input | opcjonalne |

### Tylko `type === "tracked"` (TrackedCost)

| Pole | Label | Typ | Uwagi |
|------|-------|-----|-------|
| `newFiles` | "Załączniki" | multi-file input | nowe pliki |
| `existingAttachmentIds` | checkbox lista | existing attachments w trybie edit | tylko gdy `mode === "edit"` |

### Tylko `type === "project"` (ProjectCost)

| Pole | Label | Typ | Uwagi |
|------|-------|-----|-------|
| `isAccepted` | "Zaakceptowane" | Checkbox | widoczny w obu trybach create/edit |
| `document` | "Dokument" | single file input | nowy dokument |
| `removeDocument` | (wewnętrzny) | boolean | `true` gdy użytkownik usuwa istniejący dokument |

## Props komponentu

```typescript
// Typy bazowe (importy z odpowiednich plików typów)
type CostModalMode = 'create' | 'edit';

interface CostModalTrackedProps {
  type: 'tracked';
  workItemType?: WorkItemType | null;
  costEstimateItemId?: string | null;
  workScheduleStageWorkId?: string | null;
  cost?: TrackedCostWeb;
  onSuccess: (cost: TrackedCostWeb) => void;
}

interface CostModalProjectProps {
  type: 'project';
  cost?: ProjectCostWeb;
  onSuccess: (cost: ProjectCostWeb) => void;
}

type CostModalTypeProps = CostModalTrackedProps | CostModalProjectProps;

interface CostModalBaseProps {
  tenantId: string;
  projectId: string;
  mode: CostModalMode;
  onClose: () => void;
}

type CostModalProps = CostModalBaseProps & CostModalTypeProps;
```

## Implementacja

### Lokalizacja

`src/features/dashboard/components/CostModal.tsx`

Uzasadnienie: TrackedCostModal już tam jest, nowy wspólny modal pasuje do tej lokalizacji. ProjectSimpleCosts może importować z `features/dashboard/components/CostModal`.

### Wrapper

Używaj `AppModal` z `src/components/ui/AppModal.tsx` — NIE raw Chakra `Modal`.

### State

Jeden obiekt `form`:
```typescript
interface CostFormState {
  name: string;
  description: string;
  net: string;         // string dla kontrolowanego inputu, parsowany na submit
  gross: string;
  contractor: string;
  date: string;
  number: string;
  // tracked-only
  newFiles: File[];
  existingAttachmentIds: string[];
  // project-only
  isAccepted: boolean;
  document: File | null;
  removeDocument: boolean;
}
```

### Inicjalizacja state (tryb edit)

- Wspólne pola: z `cost` prop niezależnie od trybu
- TrackedCost edit: `existingAttachmentIds` z `cost.attachments?.map(a => a.id)` lub podobnie
- ProjectCost edit: `isAccepted` z `cost.isAccepted`, `document` = null (istniejący dokument pokazany osobno)

### Logika submit

```
if (type === 'tracked') {
  if (mode === 'create') → useTrackedCostMutations.createCost(...)
  if (mode === 'edit')   → useTrackedCostMutations.updateCost(...)
}
if (type === 'project') {
  if (mode === 'create') → useProjectCostMutations.createCost(...)
  if (mode === 'edit')   → useProjectCostMutations.updateCost(...)
}
```

Hooki:
- `useTrackedCostMutations` z `src/features/dashboard/hooks/useTrackedCostMutations.ts`
- `useProjectCostMutations` z `src/hooks/useProjectCostMutations.ts` (stworzony w UI fix-02)

Wywołuj oba hooki zawsze (hooks rules) — używaj tylko odpowiedniego na submit.

### Renderowanie pól specyficznych

```tsx
{/* Pola tylko dla TrackedCost */}
{props.type === 'tracked' && (
  <>
    {/* multi-file attachments */}
    {mode === 'edit' && (/* existing attachments checkboxes */)}
  </>
)}

{/* Pola tylko dla ProjectCost */}
{props.type === 'project' && (
  <>
    {/* single document */}
    {/* isAccepted checkbox */}
  </>
)}
```

### Tytuł modala

```typescript
const title = mode === 'create'
  ? type === 'tracked' ? 'Dodaj koszt' : 'Dodaj wydatek'
  : type === 'tracked' ? 'Edytuj koszt' : 'Edytuj wydatek';
```

### Action button

```typescript
const actionLabel = mode === 'create' ? 'Dodaj' : 'Zapisz zmiany';
```

## Aktualizacja miejsc użycia

### 1. `AdditionalCostsTab.tsx`

Zastąp `<TrackedCostModal` na `<CostModal type="tracked"`.

### 2. `AllCostsTab.tsx`

Zastąp `<TrackedCostModal` na `<CostModal type="tracked"`.

### 3. `WorkItemAccordion.tsx`

Zastąp `<TrackedCostModal` na `<CostModal type="tracked" workItemType={...} costEstimateItemId={...} workScheduleStageWorkId={...}`.

### 4. `ProjectSimpleCosts.tsx`

Zastąp `<ExpenseFormModal` na `<CostModal type="project"`.
Uprość logikę: nie przekazuj `onSave` callbacka — modal sam wywołuje hook mutacji i woła `onSuccess`.
Dostosuj `onSuccess` callback do nowego sygnatury: `(cost: ProjectCostWeb) => void`.

## Po zakończeniu

1. Usuń stary plik `src/features/dashboard/components/TrackedCostModal.tsx`
2. Usuń stary plik `src/components/ExpenseFormModal.tsx`
3. Sprawdź że nie ma importów do usuniętych plików

## Kryteria sukcesu

- `npx tsc --noEmit` → 0 błędów
- `npm run build` → sukces
- Brak pliku `TrackedCostModal.tsx`
- Brak pliku `ExpenseFormModal.tsx`
- `CostModal` używa `AppModal` (nie raw `Modal`)
- Oba tryby (tracked i project) renderują prawidłowo
- Pola specyficzne renderowane tylko w odpowiednim trybie
