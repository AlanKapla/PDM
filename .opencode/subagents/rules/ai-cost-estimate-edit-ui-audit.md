# UI Audit Report: AI Cost Estimate Edit

**Audit date:** 2026-06-09
**Auditor:** UI Audit Agent
**Feature spec:** `.opencode/features/ai-cost-estimate-edit.md`

---

## Blok 1 — Stan obecny UI

### 1.1 CostEstimateEditPage (`src/pages/CostEstimateEditPage.tsx`)

| Właściwość | Opis |
|---|---|
| Lokalizacja | `src/pages/CostEstimateEditPage.tsx` (1704 linii) |
| Routing | `/projects/:projectId/cost-estimates/:estimateId` |
| Stan danych | `useState<CostEstimateDetailsWeb | null>(null)` — **ręczne ładowanie** (nie React Query) |
| Ładowanie | `loadCostEstimate()` → `costEstimateApi.getCostEstimateDetails()` |
| Tryb edycji | `isEditMode` (boolean), `hasChanges` (boolean) |
| Zarządzanie zmianami | `handleDataChange` → `setDetails(recalculated)`, `setHasChanges(true)` |
| Narzędzia | `CostEstimateToolbar` jako komponent w sticky box |
| Modale już istniejące | `WorkScheduleFormModal`, `ShareCostEstimateModal`, `ConfirmDialog` (delete), `Modal` (edit meta), `AlertDialog` (unsaved) |
| AI integration | **Brak** — żaden modal AI nie jest zintegrowany w tej stronie |

### 1.2 CostEstimateToolbar (`src/components/CostEstimateToolbar.tsx`)

| Właściwość | Opis |
|---|---|
| Lokalizacja | `src/components/CostEstimateToolbar.tsx` (457 linii) |
| Export | `default` (zgodnie z konwencją dla `components/ui/`) |
| Props | `isEditMode`, `hasChanges`, `canEdit`, `canShare`, `canSchedule`, `hasSchedule`, `isSyncing`, `isRecalculating` + 9 callbacków |
| Responsywność | 3 breakpointy: `full` (≥1100px), `compact` (600-1099px), `mobile` (<600px) przez `ResizeObserver` |
| Wzorzec akcji | `ActionDef[]` — tablica obiektów z `{ id, icon, label, tooltip, onClick, isVisible, colorScheme, variant, isLoading }` |
| Grupy akcji | `modeActions` (Edycja/Podgląd), `otherActions` (Odśwież, Udostępnij), `expandActions` (Rozwiń/Zwiń) |
| Harmonogram | Osobny dropdown z `Menu` (conditionally rendered) |
| Przyciski AI | **Brak** — nie ma przycisku "Edytuj z AI" |

### 1.3 GenerateCostEstimateWithAIModal (`src/components/GenerateCostEstimateWithAIModal.tsx`)

| Właściwość | Opis |
|---|---|
| Lokalizacja | `src/components/GenerateCostEstimateWithAIModal.tsx` (828 linii) |
| Export | `default` |
| Przeznaczenie | **Tworzenie NOWEGO** kosztorysu przez AI (nie edycja istniejącego) |
| Wzorzec | 5-step wizard: `AIModalStep = 1 | 2 | 3 | 4 | 5` |
| Footer | Własna implementacja (nie AppModal) — przycisk Wstecz/Anuluj po lewej, akcja po prawej |
| Header | Tytuł + `Progress` bar + krok z 5 |
| Stan | `step`, `form`, `formErrors`, `templates`, `selectedTemplateId`, `preview`, `finalName`, `finalDescription` |
| Preview | Accordion z `GroupPreviewItem` (rekurencyjny) |
| Użycie | Tylko w `ProjectCosts.tsx` — `onCostEstimateCreated` → `navigate` |

### 1.4 useGenerateCostEstimateWithAI (`src/hooks/useGenerateCostEstimateWithAI.ts`)

| Właściwość | Opis |
|---|---|
| Lokalizacja | `src/hooks/useGenerateCostEstimateWithAI.ts` (38 linii) |
| Zwraca | `{ generatePreview: useMutation, createFromPreview: useMutation }` |
| `generatePreview` | `useMutation<AICostEstimatePreviewDto, Error, AICostEstimateRequestDto>` |
| `createFromPreview` | `useMutation<string, Error, CreateCostEstimateFromAIPreviewDto>` |
| Wzorzec | Dwie mutacje — preview (GET-like POST) i save (POST) |

### 1.5 costEstimateApi.ts (`src/api/costEstimateApi.ts`)

| Właściwość | Opis |
|---|---|
| Lokalizacja | `src/api/costEstimateApi.ts` (456 linii) |
| Wzorzec | `export const costEstimateApi = { ... }` — object literal |
| Istniejące metody AI | `generateAIPreview(tenantId, projectId, request)`, `createFromAIPreview(tenantId, projectId, body)` |
| Endpointy AI | `POST /tenants/{t}/projects/{p}/cost-estimate/generate-ai-preview`, `POST /tenants/{t}/projects/{p}/cost-estimate/create-from-ai-preview` |
| Wzorzec URL | `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/...` |

### 1.6 Types (`src/types/costEstimate.types.new.ts`)

| Właściwość | Opis |
|---|---|
| Istniejące AI typy | `AICostEstimateRequestDto`, `AIFieldValueDto`, `AIItemPreviewDto`, `AIGroupPreviewDto`, `AICostEstimatePreviewDto`, `CreateCostEstimateFromAIPreviewDto` |
| Konwencja nazewnictwa | `*Web` (odpowiedź), `*Dto` (request/body), sufixy `*Preview`, `*Request` |
| Brakujące typy dla feature | `AICostEditRequestDto`, `AICostEditPreviewDto`, `AICostEditActionDto`, `AICostEditDiffDto` |

---

## Blok 2 — Luki i braki w UI

| Brak / Luka | Typ | Priorytet | Opis |
|---|---|---|---|
| Przycisk "Edytuj z AI" w toolbarze | Komponent | **HIGH** | Nowy przycisk w `CostEstimateToolbar`, widoczny gdy `canEdit && isEditMode` |
| AIEditCostEstimateModal | Komponent | **HIGH** | Nowy modal w stylu wizard (3-4 kroki), wzorowany na `GenerateCostEstimateWithAIModal` |
| Integracja modala w CostEstimateEditPage | Modyfikacja | **HIGH** | Dodanie stanu `useDisclosure`, wywołanie modala, callback po sukcesie (reload) |
| Hook useAICostEstimateEdit | Hook | **HIGH** | Nowy hook z `generateEditPreview` i `applyEdit` mutation |
| Metody API generateAIEditPreview / applyAIEdit | API Client | **HIGH** | Nowe metody w `costEstimateApi.ts` |
| Typy AICostEdit* | Type | **HIGH** | `AICostEditRequestDto`, `AICostEditPreviewDto`, `AICostEditActionDto` |
| Diff view dla zmian | Komponent | **MEDIUM** | Komponent podglądu co się zmieni (summary, listy add/delete/update) |
| Testy AXE dla nowego modala | Test | **MEDIUM** | Należy dodać testy dostępności dla `AIEditCostEstimateModal` |
| Testy AXE dla toolbara | Test | **LOW** | Istniejący toolbar nie ma testów AXE — brak pliku `*.axe.test.*` |

---

## Blok 3 — Typy TypeScript

| Typ | Plik | Nowy/Modyfikacja | Opis zmian |
|---|---|---|---|
| `AICostEditRequestDto` | `costEstimate.types.new.ts` | **NOWY** | `{ costEstimateId: string; userRequest: string }` |
| `AICostEditActionDto` | `costEstimate.types.new.ts` | **NOWY** | Pojedyncza operacja: `{ actionType: 'add' \| 'update' \| 'delete', entityType: 'group' \| 'item' \| 'field', ... }` |
| `AICostEditDiffDto` | `costEstimate.types.new.ts` | **NOWY** | Podsumowanie zmian: `{ summary, groupsToAdd, groupsToDelete, itemsToAdd, itemsToDelete, fieldsToUpdate }` |
| `AICostEditPreviewDto` | `costEstimate.types.new.ts` | **NOWY** | Główny DTO podglądu: `{ summary, suggestedName, suggestedDescription, groups: AIGroupPreviewDto[], warnings, diff: AICostEditDiffDto }` |
| `ApplyAICostEditDto` | `costEstimate.types.new.ts` | **NOWY** | Body do apply: `{ preview: AICostEditPreviewDto }` |

### Konwencja nazewnictwa (zgodność z istniejącym):

```
AICostEditRequestDto      ← jak AICostEstimateRequestDto
AICostEditPreviewDto      ← jak AICostEstimatePreviewDto
AICostEditActionDto       ← nowy, specyficzny dla edycji
AICostEditDiffDto         ← nowy, podsumowanie
ApplyAICostEditDto        ← jak CreateCostEstimateFromAIPreviewDto
```

---

## Blok 4 — Serwisy API (src/api/)

| Funkcja API | Plik | Nowa/Modyfikacja | Endpoint | Opis |
|---|---|---|---|---|
| `generateAIEditPreview` | `costEstimateApi.ts` | **NOWA** | `POST /tenants/{t}/projects/{p}/cost-estimate/{id}/ai/edit-preview` | Generuje propozycję edycji (nie zapisuje) |
| `applyAIEdit` | `costEstimateApi.ts` | **NOWA** | `POST /tenants/{t}/projects/{p}/cost-estimate/{id}/ai/apply-edit` | Aplikuje zatwierdzone zmiany |

### Wzorzec (zgodny z istniejącym):

```typescript
// Sekcja AI EDIT w costEstimateApi.ts (po istniejącej sekcji AI GENERATION)

generateAIEditPreview: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    request: AICostEditRequestDto
): Promise<AICostEditPreviewDto> => {
    const response = await axiosClient.post<AICostEditPreviewDto>(
        `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/ai/edit-preview`,
        request
    );
    return response.data;
},

applyAIEdit: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    body: ApplyAICostEditDto
): Promise<void> => {
    await axiosClient.post(
        `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/ai/apply-edit`,
        body
    );
},
```

---

## Blok 5 — Hooki React Query

| Hook | Plik | Nowy/Modyfikacja | Query/Mutation | Opis |
|---|---|---|---|---|
| `useAICostEstimateEdit` | `src/hooks/useAICostEstimateEdit.ts` | **NOWY** | 2× `useMutation` | `generateEditPreview` + `applyEdit` |

### Wzorzec (wzorowany na `useGenerateCostEstimateWithAI`):

```typescript
// src/hooks/useAICostEstimateEdit.ts
import { useMutation } from '@tanstack/react-query';
import { costEstimateApi } from '../api/costEstimateApi';
import type {
    AICostEditRequestDto,
    AICostEditPreviewDto,
    ApplyAICostEditDto,
} from '../types/costEstimate.types.new';

export function useAICostEstimateEdit(tenantId: string, projectId: string, costEstimateId: string) {
    const generateEditPreview = useMutation<AICostEditPreviewDto, Error, AICostEditRequestDto>({
        mutationFn: (request: AICostEditRequestDto) =>
            costEstimateApi.generateAIEditPreview(tenantId, projectId, costEstimateId, request),
    });

    const applyEdit = useMutation<void, Error, ApplyAICostEditDto>({
        mutationFn: (body: ApplyAICostEditDto) =>
            costEstimateApi.applyAIEdit(tenantId, projectId, costEstimateId, body),
    });

    return {
        generateEditPreview,
        applyEdit,
    };
}
```

### Zarządzanie stanem w komponencie:

Hook zwraca surowe mutacje — stan `isGenerating`, `isApplying`, `preview`, `error` zarządzany jest przez stan mutacji (React Query). Komponent może używać:

```typescript
const { generateEditPreview, applyEdit } = useAICostEstimateEdit(tenantId, projectId, estimateId);
// generateEditPreview.isPending, generateEditPreview.data, generateEditPreview.error
// applyEdit.isPending
```

---

## Blok 6 — Nowe komponenty

| Komponent | Lokalizacja | Opis | Zależy od |
|---|---|---|---|
| `AIEditCostEstimateModal` | `src/components/AIEditCostEstimateModal.tsx` | Modal wizard: input → loading → preview → confirm | `useAICostEstimateEdit`, `AICostEditPreviewDto` |
| `AIEditPreviewDiff` | `src/components/AIEditPreviewDiff.tsx` (lub w jednym pliku) | Podgląd proponowanych zmian (diff) | `AICostEditPreviewDto` |

### AIEditCostEstimateModal — proponowana struktura (wzorowana na GenerateCostEstimateWithAIModal):

```
Krok 1 (AIEditStep.Input):
  - Textarea: "Co chcesz zmienić?" (placeholder)
  - Walidacja: niepuste, max 2000 znaków
  - Przycisk: "Generuj propozycję"

Krok 2 (AIEditStep.Generating):
  - Spinner: "AI analizuje kosztorys..."
  - Progress bar (indeterminate)

Krok 3 (AIEditStep.Preview):
  - Alert z podsumowaniem zmian (summary)
  - Sekcja diff: "Co się zmieni?"
    - Grupy dodane: N
    - Grupy usunięte: N  
    - Pozycje dodane: N
    - Pozycje usunięte: N
    - Pola zmienione: N
  - Accordion z pełną strukturą (jak Step4Preview)
  - Ostrzeżenia (jeśli są)
  - Przyciski: "Wstecz" / "Zatwierdź zmiany"

Krok 4 (AIEditStep.Applying):
  - Spinner: "Zapisywanie zmian..."
  - Po sukcesie: automatyczne zamknięcie + przeładowanie danych
```

**Uwaga:** Różnica względem `GenerateCostEstimateWithAIModal`:
- Nie ma wyboru szablonu (template już istnieje)
- Nie ma formularza opisu inwestycji (tylko jedno pole tekstowe)
- Zmiany są aplikowane do istniejącego kosztorysu (nie tworzony nowy)
- Po sukcesie → reload danych (nie navigate do nowego ID)

---

## Blok 7 — Modyfikacje istniejących komponentów

| Komponent | Plik | Typ zmiany | Opis |
|---|---|---|---|
| `CostEstimateToolbar` | `src/components/CostEstimateToolbar.tsx` | **Modyfikacja** | Dodać przycisk "Edytuj z AI" w `otherActions` lub nowej grupie `aiActions` |
| `CostEstimateToolbarProps` | `src/components/CostEstimateToolbar.tsx` | **Modyfikacja** | Dodać props: `canUseAI?: boolean` i `onAIEdit: () => void` |
| `CostEstimateEditPage` | `src/pages/CostEstimateEditPage.tsx` | **Modyfikacja** | Dodać `useDisclosure` dla AI modala, podpiąć przycisk w toolbarze, dodać modal w JSX, callback `onAIEditSuccess` → `loadCostEstimate()` |

### Schemat integracji przycisku w toolbarze:

```tsx
// W CostEstimateToolbarProps dodać:
canUseAI?: boolean;
onAIEdit: () => void;

// W ActionDef[] (nowa grupa lub w otherActions):
{
    id: "ai-edit",
    icon: <Zap size={14} />,
    label: "Edytuj z AI",
    tooltip: "Edytuj kosztorys przy pomocy AI",
    onClick: onAIEdit,
    colorScheme: "purple",
    variant: "outline",
    isVisible: canUseAI && isEditMode,
}
```

**Warunek widoczności:** `canUseAI && isEditMode` — przycisk dostępny tylko w trybie edycji i gdy user ma uprawnienia AI.

### Schemat integracji w CostEstimateEditPage:

```tsx
// 1. Stan modala
const { isOpen: isAIEditOpen, onOpen: onAIEditOpen, onClose: onAIEditClose } = useDisclosure();

// 2. W toolbar — podmiana propów
<CostEstimateToolbar
    ...
    canUseAI={canFullEdit}
    onAIEdit={onAIEditOpen}
/>

// 3. Callback po sukcesie AI edycji
const handleAIEditSuccess = useCallback(() => {
    onAIEditClose();
    loadCostEstimate(); // przeładowanie danych
}, [onAIEditClose, loadCostEstimate]);

// 4. Modal w JSX (przed zamykającym </MainLayout>)
{canFullEdit && (
    <AIEditCostEstimateModal
        isOpen={isAIEditOpen}
        onClose={onAIEditClose}
        tenantId={user.activeTenantId}
        projectId={projectId}
        costEstimateId={estimateId}
        onEditSuccess={handleAIEditSuccess}
    />
)}
```

---

## Blok 8 — Spójność UI

| Wzorzec | Istniejąca implementacja | Czy feature musi się dostosować |
|---|---|---|
| **Wizard modal** | `GenerateCostEstimateWithAIModal` — 5-step, własny footer (nie AppModal) | **TAK** — nowy modal powinien użyć tego samego wzorca (custom step modal, nie AppModal, bo AppModal ma fixed footer który nie pasuje do wizarda) |
| **Toolbar ActionDef** | `CostEstimateToolbar` — array `ActionDef[]` z `isVisible` | **TAK** — nowy przycisk dodać jako wpis w tablicy |
| **API client** | `costEstimateApi` — object literal, `async` metody | **TAK** — nowe metody w tym samym stylu |
| **Hook mutacji** | `useGenerateCostEstimateWithAI` — dwie mutacje | **TAK** — nowy hook z dwoma mutacjami |
| **Obsługa błędów** | `handleApiError` + `showError` toast | **TAK** — użyć tego samego wzorca |
| **Nawigacja po sukcesie** | `onCostEstimateCreated` → `navigate()` lub `refreshData()` | **TAK** — `onEditSuccess` → `loadCostEstimate()` (przeładowanie danych, nie nawigacja) |
| **Nazewnictwo typów** | `AICostEstimate*` → `AICostEdit*` | **TAK** — zachować konwencję `AICostEdit*Dto`, `AICostEdit*Web` |
| **Empty state** | Template empty → Alert z linkiem | **N/D** — nie dotyczy (template już istnieje) |
| **Loading state** | Spinner + tekst w `Step3Generating` | **TAK** — taki sam wzorzec dla generowania i aplikowania |
| **Sticky toolbar** | `position="sticky" top={0}` | **TAK** — toolbar już jest sticky, nie trzeba zmieniać |

### Kluczowa decyzja: AppModal vs własny modal

**Problem:** Skill `ui-forms-modals` mówi "Zawsze używaj `AppModal` — zakaz tworzenia własnych modali", ale istniejący `GenerateCostEstimateWithAIModal` (autorytatywny przykład w kodzie) **nie używa AppModal**. Używa własnej implementacji z Chakra `Modal`, bo:

1. Wizard potrzebuje dynamicznego footera (przycisk Wstecz po lewej, różne przyciski akcji po prawej w zależności od kroku)
2. `AppModal` ma fixed footer z jednym przyciskiem akcji
3. `AppModal` nie wspiera progress baru w headerze

**Rekomendacja:** Nowy `AIEditCostEstimateModal` powinien wzorować się na `GenerateCostEstimateWithAIModal` (własna implementacja z Chakra `Modal`), a **nie** na `AppModal`. Jest to uzasadnione wyjątkiem dla wizard pattern.

---

## Blok 9 — Dostępność (WCAG AA / AXE)

### 9.1 Istniejący GenerateCostEstimateWithAIModal — audyt

#### Kontrast kolorów

| Element | Kolor | Kontrast (szac.) | Status |
|---|---|---|---|
| Text hint "liczba znaków" w Step1 | `color="gray.400"` | ~3.5:1 (za niski dla 12px tekstu) | **✗ FAIL** |
| Text "Pusta grupa" w Step4 | `color="gray.400"` | ~3.5:1 | **✗ FAIL** |
| Tekst główny w Step4 | `color="gray.600"` | ~7.0:1 | ✓ OK |
| Tekst Sugerowana nazwa | `color="purple.600"` | ~7.1:1 | ✓ OK |

#### Atrybuty ARIA

| Komponent | Problem | Rekomendacja |
|---|---|---|
| `<Icon as={Bot}>` w headerze | Brak `aria-hidden` | **✗ FAIL** — dodać `aria-hidden="true"` |
| `<Icon as={Folder}>` w Step4 | `aria-hidden="true"` | ✓ OK |
| `<Icon as={FileText}>` w Step4 | `aria-hidden="true"` | ✓ OK |
| `<Alert status="warning">` w Step4 | Brak `role="alert"` explicit (Chakra dodaje automatycznie) | ✓ OK |
| Modal na step 3 | `closeOnOverlayClick={false}` ale brak komunikatu dlaczego | **⚠ UWAGA** |
| `<Spinner>` w Step3 | Brak `aria-label` lub `aria-live` region | **⚠ UWAGA** — dodać `aria-live="polite"` lub `role="status"` |

#### Zarządzanie fokusem

| Element | Status |
|---|---|
| Chakra Modal — focus trap | ✓ OK (automatyczny) |
| Chakra Modal — powrót fokusa | ✓ OK (automatyczny) |
| Step 1 — autoFocus na textarea | ✓ OK |
| Step 4 — Accordion nawigacja klawiaturą | ✓ OK (Chakra Accordion) |

#### Inline styles (naruszenie zasad projektu)

| Linia | Problem |
|---|---|
| `color="var(--chakra-colors-purple-500)"` w Step4 | **✗ FAIL** — użyj tokena Chakra `color="purple.500"` |
| `color="var(--chakra-colors-gray-400)"` w Step4 | **✗ FAIL** — użyj tokena Chakra `color="gray.400"` |

### 9.2 Rekomendacje dla nowego AIEditCostEstimateModal

| Kategoria | Rekomendacja |
|---|---|
| Kontrast | Nie używać `gray.400` dla tekstu treści. Używać `gray.600` lub ciemniejszego. |
| ARIA | Każda ikona dekoracyjna: `aria-hidden="true"`. Spinner: `role="status"` + `aria-live="polite"`. |
| Klawiatura | Modal Chakra = focus trap automatyczny. Zapewnić logiczną kolejność Tab. |
| Inline styles | Zakaz `var(--chakra-colors-*)` — używać tokenów Chakra. |
| IconButton | Każdy `IconButton` musi mieć `aria-label`. |
| Komunikaty błędów | `Alert status="error"` z `role="alert"`. |
| AXE tests | Dodać `*.axe.test.tsx` dla `AIEditCostEstimateModal`. |

### 9.3 Podsumowanie dostępności

| Kategoria | Status | Uwagi |
|---|---|---|
| Kontrast kolorów | ⚠ | `gray.400` użyte w 2 miejscach — za niskie dla tekstu |
| Atrybuty ARIA | ⚠ | Brak `aria-hidden` na `Icon as={Bot}` w headerze |
| Klawiatura / fokus | ✓ | Chakra Modal zapewnia focus trap |
| Inline styles | ✗ | `color="var(--chakra-colors-*)"` w Step4 — złamanie zasad |
| Testy AXE | ✗ | Brak testów AXE dla istniejących komponentów AI |

---

## Blok 10 — Problemy i ryzyka

| # | Problem | Komponent/Plik | Ryzyko | Rekomendacja |
|---|---|---|---|---|
| 1 | **GenerateCostEstimateWithAIModal nie używa AppModal** | `GenerateCostEstimateWithAIModal.tsx` | Niska — skill mówi "zawsze AppModal" ale kod pokazuje inaczej. Nowy modal też nie będzie używał AppModal. | Świadomie odejść od AppModal dla wizard pattern. Dodać adnotację w kodzie dlaczego. |
| 2 | **Brak testów AXE dla AI modali i toolbara** | Wszystkie komponenty AI | Średnie — WCAG AA może być naruszone bez wiedzy | Dodać `*.axe.test.tsx` dla nowego modala i toolbara |
| 3 | **Inline styles w istniejącym komponencie** | `GenerateCostEstimateWithAIModal.tsx` (kolor SVG) | Niskie — kosmetyka, ale złamanie reguł projektu | Nowy modal nie powinien używać inline styles |
| 4 | **CostEstimateEditPage używa useState zamiast React Query** | `CostEstimateEditPage.tsx` | Średnie — brak cache, refetch, deduplication | Nie zmieniać — to istniejący wzorzec strony. Feature używa go przez `loadCostEstimate()` callback. |
| 5 | **hasChanges + AI edit = konflikt** | `CostEstimateEditPage.tsx` | **WYSOKIE** — Jeśli user ma niezapisane zmiany manualne i kliknie "Edytuj z AI", zmiany mogą być nadpisane. | Przed otwarciem AI modala: flush pending changes (użyć `flushPendingChanges`). Po sukcesie AI: `loadCostEstimate()` przeładowuje wszystko. |
| 6 | **hasChanges guard nawigacji** | `CostEstimateEditPage.tsx` | Średnie — Po AI apply, `setHasChanges(false)` musi być wywołane. | W `handleAIEditSuccess` upewnić się, że `loadCostEstimate()` wywołane, a w nim `setHasChanges(false)`. |
| 7 | **Permission sprawdzanie** | `CostEstimateEditPage.tsx` | Średnie — Feature wymaga `PermissionCodes.ProjectEstimates` | Warunek `canUseAI` = `canFullEdit` (owner/admin). Weryfikacja z feature spec. |
| 8 | **Recalculate po AI apply** | Backend | Średnie — Feature spec mówi o recalculate po apply | Upewnić się że backend wywołuje `RecalculateCostEstimateCommand` po apply. UI nie musi robić recalculate osobno. |

---

## Podsumowanie

| Metryka | Wartość |
|---|---|
| Nowe komponenty | 1 (`AIEditCostEstimateModal`) |
| Nowe sub-komponenty wewnątrz modala | 3 (`AIEditStepInput`, `AIEditStepPreview`, `AIEditDiffSummary`) |
| Zmodyfikowane komponenty | 2 (`CostEstimateToolbar`, `CostEstimateEditPage`) |
| Nowe hooki | 1 (`useAICostEstimateEdit`) |
| Nowe typy TypeScript | 4 (`AICostEditRequestDto`, `AICostEditPreviewDto`, `AICostEditActionDto`, `AICostEditDiffDto`, `ApplyAICostEditDto`) |
| Nowe wywołania API | 2 (`generateAIEditPreview`, `applyAIEdit`) |
| Naruszenia WCAG AA (istniejące) | 4 |
| Pytania domenowe | 4 |

---

## Pytania domenowe wymagające decyzji

1. **Konflikt z niezapisanymi zmianami:** Czy przed otwarciem AI modala powinniśmy flushować zmiany (autosave)? Czy blokować jeśli `hasChanges === true`? Rekomendacja: flush + auto-recalculate przed otwarciem.

2. **Zachowanie obecnego stanu kosztorysu:** Po AI apply, `loadCostEstimate()` przeładowuje całość — czy to resetuje `isEditMode`? Rekomendacja: zachować `isEditMode=true` po AI edycji (user kontynuuje edycję).

3. **Widoczność przycisku "Edytuj z AI":** Tylko dla `canFullEdit` czy też dla `canRestrictedEdit`? Feature spec mówi `CostEstimateAccessLevel.Full`. Rekomendacja: `canFullEdit`.

4. **Krok 3 — struktura diff view:** Feature spec mówi o `AICostEditPreviewWeb` z `Groups` (pełny stan po edycji). Czy w UI pokazujemy tylko diff (podsumowanie zmian) czy pełne drzewo (jak w GenerateCostEstimateWithAIModal)? Rekomendacja: pokazać podsumowanie + pełne drzewo w accordion (wzorowane na Step4Preview), ale dodać sekcję "Co się zmieni?" z listą zmian.

---

## Pliki do modyfikacji/tworzenia

### Pliki do utworzenia:
1. `src/hooks/useAICostEstimateEdit.ts`
2. `src/components/AIEditCostEstimateModal.tsx`

### Pliki do modyfikacji:
1. `src/types/costEstimate.types.new.ts` — dodać typy `AICostEdit*`
2. `src/api/costEstimateApi.ts` — dodać metody `generateAIEditPreview`, `applyAIEdit`
3. `src/components/CostEstimateToolbar.tsx` — dodać przycisk i props
4. `src/pages/CostEstimateEditPage.tsx` — dodać modal i integrację
5. `src/components/AIEditCostEstimateModal.axe.test.tsx` — testy AXE (nowy plik)
