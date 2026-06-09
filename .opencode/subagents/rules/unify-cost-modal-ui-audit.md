# UI Audit — feature: unify-cost-modal

Data audytu: 2026-05-15  
Audytor: ui-audit-agent

---

## Znalezione pliki — przegląd

| Plik | Rola | Powiązany z |
|------|------|-------------|
| `src/features/dashboard/components/TrackedCostModal.tsx` | **Modal TrackedCost** — add/edit | TrackedCost |
| `src/components/ExpenseFormModal.tsx` | **Modal ProjectCost** — add/edit | ProjectCost |
| `src/components/ExpenseCard.tsx` | Karta mobilna listy ProjectCost | ProjectCost |
| `src/pages/ProjectSimpleCosts.tsx` | Strona zarządzania ProjectCost | ProjectCost |
| `src/features/dashboard/components/tabs/AdditionalCostsTab.tsx` | Zakładka kosztów dodatkowych dashboard | TrackedCost |
| `src/features/dashboard/components/tabs/AllCostsTab.tsx` | Zakładka wszystkich kosztów dashboard | TrackedCost |
| `src/features/dashboard/components/WorkItemAccordion.tsx` | Akordeon pozycji powiązanych | TrackedCost |
| `src/features/dashboard/hooks/useTrackedCostMutations.ts` | Hook mutacji TrackedCost | TrackedCost |
| `src/hooks/useProjectCostTracker.ts` | Hook (deprecated) wrapper | TrackedCost |
| `src/features/dashboard/services/dashboardApi.ts` | Serwis API dla TrackedCost (aktualny) | TrackedCost |
| `src/api/costTrackerApi.ts` | Serwis API dla TrackedCost (starszy) | TrackedCost |
| `src/api/projectApi.ts` | API dla ProjectCost (inline, bez dedyk. serwisu) | ProjectCost |
| `src/features/dashboard/types/projectDashboard.types.ts` | Typy TrackedCost (aktualne, bogatsze) | TrackedCost |
| `src/types/costTracker.types.ts` | Typy TrackedCost (starsze) + CreateCostRequest | TrackedCost |
| `src/types/project.types.ts` | Typy ProjectCost | ProjectCost |

---

## BLOK 1 — Stan obecny UI

### TrackedCostModal (`src/features/dashboard/components/TrackedCostModal.tsx`)

**Props:**
```typescript
{
  tenantId: string;
  projectId: string;
  mode: 'create' | 'edit';
  workItemType?: WorkItemType | null;       // specyficzne dla TrackedCost
  costEstimateItemId?: string | null;      // specyfyczne
  workScheduleStageWorkId?: string | null; // specyficzne
  cost?: TrackedCostWeb;
  onSuccess: (cost: TrackedCostWeb) => void;
  onClose: () => void;
}
```

**State (useState per field):**
- `name`, `description`, `net`, `gross`, `contractor`, `date`, `number`
- `newFiles: File[]` — wieloplikowe
- `existingAttachmentIds: string[]` — zarządzanie istniejącymi załącznikami

**Formularz:**
1. Nazwa (required)
2. Opis (Textarea)
3. Kwota netto + brutto (SimpleGrid 2 col)
4. Numer faktury
5. Wykonawca
6. Data
7. Załączniki (multi-file input)
8. Istniejące załączniki (checkboxes do usunięcia — tylko edit)
9. Error alert

**Infrastruktura:**
- Wrapper: `AppModal` ✓ (zgodne ze standardami)
- Hook: `useTrackedCostMutations` → `createTrackedCost` / `updateTrackedCost` z `dashboardApi.ts`
- Endpoint POST: `/tenants/{t}/projects/{p}/cost-trackers/costs` (multipart/form-data)
- Endpoint PUT: `/tenants/{t}/projects/{p}/cost-trackers/costs/{id}` (multipart/form-data)

**Miejsca użycia (3):**
- `AdditionalCostsTab.tsx` — create + edit, bez `workItemType`
- `AllCostsTab.tsx` — tylko edit, bez `workItemType`
- `WorkItemAccordion.tsx` — z `workItemType` (LinkedWorkItem, ScheduleWorkItem, EstimateItem)

---

### ExpenseFormModal (`src/components/ExpenseFormModal.tsx`)

**Props:**
```typescript
{
  isOpen: boolean;
  onClose: () => void;
  editingCost?: ProjectCostListItemWeb | null;
  documentFile: File | null;
  onDocumentFileChange: (file: File | null) => void;
  onSave: (data: ExpenseFormData) => void;
  isSaving: boolean;
}
```

**State (jeden obiekt `form`):**
```typescript
{
  name, place, date, description, netAmount, grossAmount, isAccepted, removeDocument
}
```

**Formularz:**
1. Nazwa (required, z FormErrorMessage)
2. Miejsce
3. Data (required, z FormErrorMessage)
4. Opis (Textarea)
5. Kwota netto + brutto (HStack, z walidacją "przynajmniej jedno wymagane")
6. Dokument (single file z chip-em do usunięcia)
7. Zaakceptowane (Checkbox)

**Infrastruktura:**
- Wrapper: **RAW Chakra `Modal`** ❌ (NIEZGODNE ze standardami — powinno być `AppModal`)
- Brak własnego hooka mutacji — wywołanie API w rodzicu (`ProjectSimpleCosts`)
- Endpoint POST: `/tenants/{t}/projects/{p}/cost` (multipart/form-data, przez `projectApi.createProjectCost`)
- Endpoint PUT: `/tenants/{t}/projects/{p}/cost/{id}` (multipart/form-data, przez `projectApi.updateProjectCost`)
- Posiada wbudowany `AlertDialog` do usuwania dokumentu

---

## BLOK 2 — Tabela: pole → TrackedCost modal → ProjectCost modal

| Pole logiczne | TrackedCostModal | ExpenseFormModal | Nazwa w API (TrackedCost) | Nazwa w API (ProjectCost) |
|---|---|---|---|---|
| Nazwa | `name` (string) ✓ required | `name` (string) ✓ required | `name` | `name` |
| Opis | `description` (Textarea) | `description` (Textarea) | `description` | `description` |
| Kwota netto | `net` (number input) | `netAmount` (number input) | `net` | `netAmount` ⚠️ |
| Kwota brutto | `gross` (number input) | `grossAmount` (number input) | `gross` | `grossAmount` ⚠️ |
| Wykonawca | `contractor` (Input) | **BRAK** (zostanie dodane) | `contractor` | `contractor` (brak dziś) |
| Data | `date` (date input) | `date` (date input, required) | `date` | `date` |
| Dokument/Plik | multi-file (`newFiles[]`) + existing checkboxes | single file + remove chip | `newFiles[]` + `existingAttachmentIds[]` | `document` / `updatedDocument` + `removeDocument` |
| Numer faktury | `number` (Input) | **BRAK** | `number` | — |
| Miejsce | **BRAK** | `place` (Input) | — | `place` (do usunięcia z API) |
| Zaakceptowane | **BRAK** | `isAccepted` (Checkbox) | — | `isAccepted` |
| CostEstimateItemId | prop (hidden, do request) | **BRAK** | `costEstimateItemId` | — |
| WorkScheduleStageWorkId | prop (hidden, do request) | **BRAK** | `workScheduleStageWorkId` | — |

---

## BLOK 3 — Duplikacje między modalami

| Duplikacja | Lokalizacja w TrackedCostModal | Lokalizacja w ExpenseFormModal |
|---|---|---|
| `name` FormControl | linia ~110 | linia ~130 |
| `description` Textarea | linia ~120 | linia ~160 |
| Netto input (number, step=0.01) | linia ~130 | linia ~200 |
| Brutto input (number, step=0.01) | linia ~138 | linia ~215 |
| Data date input | linia ~155 | linia ~148 |
| `sx={{ 'input, textarea, select': { fontSize: '16px' }}}` | VStack sx prop | ModalContent sx prop |
| Tytuł "Dodaj koszt" / "Edytuj koszt" | `mode === 'create'` ternary | `isEdit` ternary |
| Przycisk "Dodaj" / "Zapisz zmiany" | `mode === 'create'` ternary | hardcode "Zapisz" |
| Walidacja required name | `isActionDisabled={!name.trim()}` | `submitted && !form.name.trim()` |
| multipart/form-data upload pattern | `dashboardApi.ts` createTrackedCost | `projectApi.ts` createProjectCost |

**Liczba zduplikowanych FormControl: 5 z 7 pól wspólnych.**

---

## BLOK 4 — Pola specyficzne (tylko w jednym modalu)

### Tylko TrackedCost:
| Pole | Powód istnienia |
|------|----------------|
| `number` (numer faktury) | Faktury/dokumenty księgowe |
| `newFiles[]` (wiele załączników) | TrackedCost obsługuje wiele plików |
| `existingAttachmentIds[]` (zarządzanie istniejącymi) | Wieloplikowy model danych |
| `costEstimateItemId` (prop) | Powiązanie z pozycją kosztorysu |
| `workScheduleStageWorkId` (prop) | Powiązanie z zadaniem harmonogramu |
| `workItemType` (prop) | Typ powiązania (LinkedWorkItem / ScheduleWorkItem / EstimateItem) |

### Tylko ProjectCost (do decyzji):
| Pole | Powód istnienia | Status |
|------|----------------|--------|
| `place` | Miejsce poniesieniakosztu | ⚠️ Do USUNIĘCIA z API wg kontekstu feature |
| `isAccepted` | Workflow zatwierdzania | Pozostaje w ProjectCost |
| `document` (single file) | Jeden dokument źródłowy | Różny model niż TrackedCost |

---

## BLOK 5 — Aktualny stan typów TypeScript

### Problem: Duplikacja `TrackedCostWeb`

`TrackedCostWeb` jest zdefiniowany w **dwóch miejscach** z różnymi kształtami:

**A) `src/types/costTracker.types.ts`** (starszy):
```typescript
interface TrackedCostWeb {
  id, trackerId, costEstimateId, costEstimateItemId, isAdditional,
  name, number, description, net, gross, vatAmount, vatRate,
  contractor, date, createdAt, updatedAt,
  attachments: TrackedCostAttachmentWeb[]
  // Brak: workScheduleStageWorkId, sourceType, scheduleName, etc.
}
```

**B) `src/features/dashboard/types/projectDashboard.types.ts`** (nowszy, bogatszy):
```typescript
interface TrackedCostWeb {
  id, costEstimateItemId, workScheduleStageWorkId, isAdditional,
  name, description, net, gross, vatRate, contractor, date, number,
  attachments: TrackedCostAttachmentWeb[],
  createdAt, updatedAt,
  sourceType: 'ProjectAdditional' | 'ScheduleWorkItem' | 'EstimateItem' | 'LinkedWorkItem',
  scheduleName, stageName, workItemName,
  estimateName, estimateGroupName, estimateItemName
  // Brak: trackerId (usunięte), vatAmount
}
```

**`TrackedCostModal` używa wersji B** (z `projectDashboard.types.ts`).
**`costTrackerApi.ts` + `useCostTrackerCosts` hook używają wersji A** (z `costTracker.types.ts`).

### Problem: Duplikacja `CreateTrackedCostRequest`

Zdefiniowane w **trzech miejscach**:

| Plik | Nazwa | Uwagi |
|------|-------|-------|
| `src/types/costTracker.types.ts` | `CreateCostRequest` | camelCase, brak `gross`, brak `workScheduleStageWorkId` |
| `src/types/costTracker.types.ts` | `CreateTrackedCostRequest` | **PascalCase** ⚠️ stary format |
| `src/features/dashboard/types/projectDashboard.types.ts` | `CreateTrackedCostRequest` | camelCase, ma `net` + `gross`, ma `workScheduleStageWorkId` ✓ |

**`TrackedCostModal` używa wersji z `projectDashboard.types.ts`** ✓ (poprawna).

### Stan typów ProjectCost

Zdefiniowane w jednym miejscu `src/types/project.types.ts` — spójne, ale używają starych nazw:
```typescript
interface ProjectCostListItemWeb {
  netAmount?: number;  // ⚠️ będzie przemianowane na net
  grossAmount: number; // ⚠️ będzie przemianowane na gross
  place?: string;      // ⚠️ do usunięcia
  isAccepted: boolean; // pozostaje
  // ...
}
```

---

## BLOK 6 — Serwisy API

| Funkcja | Plik | Endpoint | Uwagi |
|---------|------|---------|-------|
| `createTrackedCost` | `features/dashboard/services/dashboardApi.ts` | POST `/cost-trackers/costs` | **Aktualny** dla TrackedCostModal |
| `updateTrackedCost` | `features/dashboard/services/dashboardApi.ts` | PUT `/cost-trackers/costs/{id}` | **Aktualny** |
| `deleteTrackedCost` | `features/dashboard/services/dashboardApi.ts` | DELETE `/cost-trackers/costs/{id}` | **Aktualny** |
| `costTrackerApi.createCost` | `api/costTrackerApi.ts` | POST `/cost-trackers/costs` | Starszy, brak `gross` w FormData |
| `costTrackerApi.updateCost` | `api/costTrackerApi.ts` | PUT `/cost-trackers/costs/{id}` | Starszy, brak `gross` |
| `projectApi.createProjectCost` | `api/projectApi.ts` | POST `/cost` | Inline, multipart/form-data |
| `projectApi.updateProjectCost` | `api/projectApi.ts` | PUT `/cost/{id}` | Inline, multipart/form-data |
| `projectApi.deleteProjectCost` | `api/projectApi.ts` | DELETE `/cost/{id}` | Inline |

**Brak dedykowanego pliku serwisu dla ProjectCost** — wszystko w `projectApi.ts` (duży plik).

---

## BLOK 7 — Hooki

| Hook | Plik | Typ | Używa |
|------|------|-----|-------|
| `useTrackedCostMutations` | `features/dashboard/hooks/useTrackedCostMutations.ts` | Mutacje (create/update/delete) | `dashboardApi.ts` — aktualny |
| `useCostTrackerByProject` | `hooks/queries/useCostTracker.ts` | React Query (query) | `costTrackerApi.ts` — starszy |
| `useCostTrackerCosts` | `hooks/queries/useCostTracker.ts` | React Query (query) | `costTrackerApi.ts` — starszy |
| `useProjectCostTracker` | `hooks/useProjectCostTracker.ts` | Deprecated wrapper | `useCostTrackerByProject` |
| **Brak** | — | Mutacje dla ProjectCost | — |

**ProjectCost nie ma własnego hooka mutacji** — API wołane inline w `ProjectSimpleCosts.tsx`.

---

## BLOK 8 — Hooki mutacji: `useTrackedCostMutations` szczegółowo

```typescript
export function useTrackedCostMutations({ tenantId, projectId, onSuccess })
// Zwraca: { createCost, updateCost, deleteCost, updateBudget, isLoading, error }
```

- Używany **wyłącznie** przez `TrackedCostModal`
- Zarządza `isLoading` i `error` stanem (nie React Query mutation — plain useState/useCallback)
- Po każdej operacji wywołuje `onSuccess?.()` (refetch dashboardu)

---

## BLOK 9 — Spójność UI

| Wzorzec | Istniejąca implementacja | TrackedCostModal | ExpenseFormModal | Do dostosowania |
|---------|--------------------------|-----------------|-----------------|----------------|
| Modal wrapper | `AppModal` (src/components/ui/AppModal.tsx) | ✅ AppModal | ❌ Raw Chakra Modal | ExpenseFormModal |
| Walidacja wymaganego pola | `isActionDisabled` w AppModal | ✅ | ❌ własna logika `submitted` | ujednolicić |
| State formularza | per-field `useState` (TrackedCostModal) | ✅ | obiekt `form` (różnica) | ujednolicić lub wybrać jeden wzorzec |
| Error display | `<Alert status="error">` w ciele modala | ✅ | przez parent toast | ujednolicić |
| fontSize fix mobile | `sx={{ 'input, textarea': { fontSize: '16px' }}}` | ✅ | ✅ | ok |
| Nazwy pól | net/gross | TrackedCost: net/gross | ProjectCost: netAmount/grossAmount | ⚠️ po API rename |
| Plik upload pattern | multipart/form-data | wieloplik (newFiles[]) | jeden plik (document) | do decyzji |
| Formatowanie kwot | PLN formatters (formatters.ts) | ✅ (w liście) | ✅ (w liście) | ok |

---

## BLOK 10 — Problemy i ryzyka

| # | Problem | Komponent/Plik | Ryzyko | Rekomendacja |
|---|---------|---------------|--------|-------------|
| 1 | `ExpenseFormModal` używa raw Chakra `Modal` zamiast `AppModal` | `ExpenseFormModal.tsx` | Niespójność UX, inny wygląd stopki | Wymienić na `AppModal` |
| 2 | `TrackedCostWeb` zduplikowany w 2 plikach z różnymi polami | `costTracker.types.ts` vs `projectDashboard.types.ts` | Dezorientacja, błędy TS przy refactorze | Wybrać i usunąć starszy |
| 3 | `CreateTrackedCostRequest` zduplikowany w 3 miejscach | `costTracker.types.ts` (×2), `projectDashboard.types.ts` | Niezgodność pól (brak `gross` w starszym) | Wyczyścić, zostawić tylko `projectDashboard.types.ts` |
| 4 | Brak hooka mutacji dla ProjectCost | `ProjectSimpleCosts.tsx` | API wołane inline 200+ linii od początku pliku | Wyodrębnić `useProjectCostMutations` |
| 5 | `costTrackerApi.ts` pomija pole `gross` w `buildCostFormData` | `costTrackerApi.ts` linia ~24 | `gross` nie wysyłane przez starszy kod | Naprawić lub oznaczyć jako deprecated |
| 6 | Po rename API `netAmount→net`, `grossAmount→gross` w ProjectCost | `project.types.ts`, `ExpenseFormModal.tsx`, `ProjectSimpleCosts.tsx` | TypeScript błędy po API zmianie | Zmienić typy i mapowanie we wszystkich miejscach |
| 7 | Usunięcie `place` z ProjectCost | `ProjectCostListItemWeb`, `ExpenseFormModal.tsx`, `ProjectSimpleCosts.tsx` | `place` renderowany w tabeli i formularzu (3+ miejsca) | Usunąć pole + FormControl z formularza |
| 8 | Logika dokumentu w ProjectCost jest niekompatybilna z TrackedCost | `ExpenseFormModal.tsx` vs `TrackedCostModal.tsx` | Model danych single-file vs multi-file | Wymagana decyzja (patrz pytania) |
| 9 | Używanie deprecated `useProjectCostTracker` hook | `ProjectBudgetDashboard.tsx` | Stary hook wrappuje stary API | Migrować na `useCostTrackerByProject` |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Modalne komponenty do ujednolicenia | 2 (TrackedCostModal, ExpenseFormModal) |
| Zmodyfikowane komponenty | min. 5 (oba modale, ProjectSimpleCosts, project.types.ts, costTracker.types.ts) |
| Nowe hooki | 1 (useProjectCostMutations) |
| Zduplikowane typy do usunięcia | 3 (TrackedCostWeb stary, CreateTrackedCostRequest ×2 stare) |
| Nowe wywołania API | 0 (po stronie API) / 1 nowy serwis plik (projectCostApi.ts) |
| Pytania domenowe | 3 |

---

## Rekomendacja architektury — wspólny modal z `type: "tracked" | "project"`

Zgodnie z decyzją architektoniczną: **jeden modal** z trybem.

### Proponowana struktura

```
src/features/costs/                        # nowy moduł domenowy
├── components/
│   └── UnifiedCostModal.tsx               # jeden modal z type prop
├── types/
│   └── costModal.types.ts                 # wspólne typy formularza
├── hooks/
│   ├── useTrackedCostMutations.ts         # przeniesiony/zrefaktorowany
│   └── useProjectCostMutations.ts         # nowy
└── services/
    ├── trackedCostApi.ts                  # wydzielony z dashboardApi.ts
    └── projectCostApi.ts                  # wydzielony z projectApi.ts
```

### Sygnatura `UnifiedCostModal`

```typescript
// Pola wspólne (BaseCost)
interface BaseCostFormData {
  name: string;
  description?: string | null;
  net?: number | null;
  gross?: number | null;
  contractor?: string | null;
  date?: string | null;
}

// Pola specyficzne dla tracked
interface TrackedCostExtension {
  type: 'tracked';
  workItemType?: WorkItemType | null;
  costEstimateItemId?: string | null;
  workScheduleStageWorkId?: string | null;
  // Wieloplikowe załączniki
  editingCost?: TrackedCostWeb;
}

// Pola specyficzne dla project
interface ProjectCostExtension {
  type: 'project';
  editingCost?: ProjectCostListItemWeb;
  // Single-document handling (wewnętrznie)
}

export type UnifiedCostModalProps = {
  tenantId: string;
  projectId: string;
  mode: 'create' | 'edit';
  onSuccess: () => void;
  onClose: () => void;
} & (TrackedCostExtension | ProjectCostExtension);
```

### Pola wspólne vs specyficzne w widoku formularza

**Renderowane zawsze (shared section):**
- Nazwa (required)
- Opis
- Kwota netto + Kwota brutto (SimpleGrid 2 col)
- Wykonawca ← **tu pojawia się dla obu po ujednoliceniu API**
- Data

**Renderowane tylko dla `type="tracked"`:**
- Numer faktury (`number`)
- Multi-file załączniki (`newFiles[]` + existing checkboxes)

**Renderowane tylko dla `type="project"`:**
- Single-document upload + chip usunięcia
- Zaakceptowane (`isAccepted`) checkbox

**Nie renderowane (usunięte):**
- Miejsce (`place`) — zostanie usunięte z API

### Obsługa submit

```typescript
const handleAction = async () => {
  if (props.type === 'tracked') {
    // wywołaj useTrackedCostMutations
  } else {
    // wywołaj useProjectCostMutations
  }
};
```

Logika submit przez discriminated union, bez `if/else` w środku sekcji pól.

---

## Pytania domenowe wymagające decyzji

1. **Model pliku dla ProjectCost:** Czy po ujednoliceniu ProjectCost powinien obsługiwać **wiele plików** (jak TrackedCost) czy pozostać przy **jednym dokumencie**? Wpływa na API.

2. **Pole `isAccepted` w nowym modalu:** Czy pole "Zaakceptowane" powinno być widoczne w modalu dla `type="project"` (workflow), czy przenieść do osobnej akcji (jak toggle w tabeli)?

3. **Numer faktury (`number`) dla ProjectCost:** Czy pole `number` (numer faktury) z TrackedCost powinno być dodane **też** do ProjectCost jako część ujednolicenia BaseCost?
