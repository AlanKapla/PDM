# UI Audit — AI Cost Document Import

**Feature:** ai-cost-document-import  
**Data audytu:** 2026-06-01  
**Audytowane obszary:** CostTracker komponenty, ProjectCost formularz, API klienci, hooki React Query, AppModal, typy TypeScript, i18n, dostępność WCAG AA

---

## BLOK 1 — Stan obecny UI

| Komponent/Strona | Lokalizacja | Opis | Powiązane z feature |
|-----------------|------------|------|---------------------|
| `CostFormModal` | `src/components/CostTracker/CostFormModal.tsx` | 4-krokowy modal/drawer (Kosztorys→Etap→Pozycja→Dane kosztu) do tworzenia TrackedCost. Krok 3 renderuje `<CostForm>`. **Nie używa AppModal** — własna implementacja Modal+Drawer. | Punkt integracji #1: przycisk "Importuj z dokumentu" w kroku 3 |
| `CostFormDrawer` | `src/components/CostTracker/CostFormDrawer.tsx` | Drawer do create/edit TrackedCost (z CostLinkSection). Używa Chakra `Drawer`. **Nie używa AppModal**. | Punkt integracji #2: przycisk "Importuj z dokumentu" tylko przy `!isEdit` |
| `CostForm` | `src/components/CostTracker/CostForm.tsx` | Czysta forma TrackedCost: nazwa, kwota netto, numer faktury, kontrahent (ContractorPicker), data, opis, załączniki. Inline `<input type="file" ref={...}>` bez dropzone. | Target pre-fill po AI parsowaniu |
| `CostModal` | `src/features/dashboard/components/CostModal.tsx` | Unified modal dla TrackedCost i ProjectCost (typ prop `type: 'tracked'|'project'`). **Używa AppModal** jako wrappera. Obsługuje oba typy kosztów i tryby create/edit. | Punkt integracji #3: przycisk w `mode === 'create'` |
| `AppModal` | `src/components/ui/AppModal.tsx` | Standardowy wrapper modala dla całej aplikacji. Mobile: full-screen, desktop: isCentered, scrollBehavior="inside". Stopka przyklejona na mobile. | Wzorzec dla `AICostImportModal` |
| `CostFormValues` | `src/types/costTracker.types.ts` | Typ formularza TrackedCost: `name, description?, net?, number?, contractorId?, date?, newFiles?, existingAttachmentIds?`. Brak `gross`. | ParsedCostDto mapuje tu bezpośrednio (5/6 pól) |
| `ProjectCostListItemWeb` | `src/types/project.types.ts` | Web model ProjectCost. Pola: `net, gross, name, number, contractorId, date, description, approvalStatus`. | Target zapisu ProjectCost |
| `CreateProjectCostRequest` | `src/types/project.types.ts` | Request do tworzenia ProjectCost: `name, number?, contractorId?, date: string, description?, net?, gross?, document?`. | Mapowanie z ParsedCostDto dla ProjectCost |
| `costTrackerApi` | `src/api/costTrackerApi.ts` | Klient API dla TrackedCost. Pattern: `const costTrackerApi = { method: async(...) => axiosClient.get/post }`. FormData dla multipart. | Wzorzec dla `aiCostApi.ts` |
| `projectApi` | `src/api/projectApi.ts` | Klient API dla ProjectCost (`createProjectCost`, `updateProjectCost`). Używa FormData z `TenantId, ProjectId` jako PascalCase fields. | Drugi punkt API dla ProjectCost |
| `useCostTracker` | `src/hooks/queries/useCostTracker.ts` | Query hooki dla CostTracker. Eksportuje `costTrackerKeys` i `useCostTrackerByProject`, etc. Pattern: `useQuery({ queryKey, queryFn, enabled })`. | Wzorzec dla query hooków |
| `useProjectCostMutations` | `src/hooks/useProjectCostMutations.ts` | Custom hook ze stanem `useState/useCallback`. **Nie używa useMutation** — ręczne zarządzanie `isCreating/isUpdating`. | Wzorzec dla mutacji (alternatywny) |
| `ContractorPicker` | `src/components/ContractorPicker.tsx` | Picker kontrahenta z `canQuickAdd` prop dla quick-add. Używany w CostForm i CostModal. | Pre-fill `contractorId` z ParsedCostDto |
| `UploadFilesModal` | `src/components/UploadFilesModal.tsx` | Modal uploadu plików do projektu. Zbyt złożony (packageName, mode new/existing). Nie nadaje się do reuse w AI import. | Wzorzec dla drag-drop (fragmenty), ale nie reusable |
| `FileFieldRenderer` | `src/components/CostEstimate/FileFieldRenderer.tsx` | Komponent do zarządzania plikami pól kosztorysu. Akceptuje `.pdf, .jpg, .jpeg`. Zbyt domenowy. | Wzorzec dla dozwolonych typów plików |

---

## BLOK 2 — Luki i braki w UI

| Brak / Luka | Typ | Priorytet | Opis |
|-------------|-----|-----------|------|
| `ParsedCostDto` (typ TS) | TypeScript type | **Wysoki** | Brak definicji w `src/types/`. Musi odzwierciedlać spec z feature.md |
| `aiCostApi.ts` | API Client | **Wysoki** | Brak klienta do endpointu `POST /ai/cost/parse`. Nowy plik `src/api/aiCostApi.ts` |
| `useAICostDocumentParser` | Hook (useMutation) | **Wysoki** | Hook zarządzający stanem parsowania: idle→loading→success/error. `useMutation` z React Query |
| `AICostImportModal` | Komponent | **Wysoki** | Modal 2-krokowy: upload pliku → edycja + zatwierdzenie. Oparty o `AppModal` |
| Przycisk "Importuj z dokumentu" w `CostFormModal` | Modyfikacja komponentu | **Wysoki** | Dodać w `renderStep()` case 3, przed `<CostForm>` |
| Przycisk "Importuj z dokumentu" w `CostFormDrawer` | Modyfikacja komponentu | **Wysoki** | Dodać w DrawerBody VStack, przed `<CostForm>`, tylko gdy `!isEdit` |
| Przycisk "Importuj z dokumentu" w `CostModal` (dashboard) | Modyfikacja komponentu | **Wysoki** | Dodać w AppModal body, przed pierwszym FormControl, tylko w `mode === 'create'` |
| `DocumentDropzone` | Komponent UI | **Średni** | Brak generycznego komponentu drag-and-drop do wyboru pojedynczego pliku. Należy stworzyć w `src/components/ui/DocumentDropzone.tsx` |
| Baner "Kontrahent nie znaleziony" | Komponent (inline w AICostImportModal) | **Średni** | Alert z info + przycisk "Dodaj kontrahenta" gdy `contractorFound: false` |
| `CostFormValues` — brak pola `gross` | Modyfikacja TypeScript type | **Niski** | ProjectCost ma gross, TrackedCost nie. AICostImportModal musi obsłużyć obie ścieżki |

---

## BLOK 3 — Typy TypeScript

| Typ | Plik | Nowy/Modyfikacja | Opis zmian |
|-----|------|-----------------|------------|
| `ParsedCostDto` | `src/types/ai.types.ts` (nowy plik) | **Nowy** | Pełna definicja wg spec: `name, description?, number?, net?, gross?, date?, contractorId?, contractorName?, contractorNip?, contractorAddress?, contractorFound, suggestedContractor?, confidence, rawText?` |
| `CostDocumentType` | `src/types/ai.types.ts` | **Nowy** | Enum: `'TrackedCost' \| 'ProjectCost'` — odpowiada `CostDocumentType` z API |
| `ParseCostDocumentRequest` | `src/types/ai.types.ts` | **Nowy** | Typ żądania: `{ file: File; costType: CostDocumentType }` — używany przez `aiCostApi.ts` |
| `CostFormValues` | `src/types/costTracker.types.ts` | **Bez zmian** | Nie modyfikować. TrackedCost nie ma pola `gross`. AICostImportModal obsługuje to wewnętrznie przez osobny prop `gross` przekazywany do CostModal (ProjectCost) |

### Definicja `ParsedCostDto`

```typescript
// src/types/ai.types.ts

export type CostDocumentType = 'TrackedCost' | 'ProjectCost';

export interface SuggestedContractor {
  name: string;
  nip?: string;
  address?: string;
}

export interface ParsedCostDto {
  name: string;
  description?: string;
  number?: string;
  net?: number;
  gross?: number;
  date?: string;                      // ISO date string
  contractorId?: string;              // GUID — tylko gdy contractorFound = true
  contractorName?: string;
  contractorNip?: string;
  contractorAddress?: string;
  contractorFound: boolean;
  suggestedContractor?: SuggestedContractor;
  confidence: number;                 // 0–1
  rawText?: string;
}

export interface ParseCostDocumentRequest {
  file: File;
  costType: CostDocumentType;
}
```

---

## BLOK 4 — Serwisy API (src/api/)

| Funkcja API | Plik | Nowa/Modyfikacja | Endpoint | Opis |
|-------------|------|-----------------|---------|------|
| `aiCostApi.parseCostDocument` | `src/api/aiCostApi.ts` | **Nowa** | `POST /tenants/{tenantId}/projects/{projectId}/ai/cost/parse` | Multipart FormData: `file` + `costType`. Zwraca `ParsedCostDto` |

### Implementacja `aiCostApi.ts`

```typescript
// src/api/aiCostApi.ts
import { axiosClient } from './axiosClient';
import type { ParsedCostDto, ParseCostDocumentRequest } from '../types/ai.types';

export const aiCostApi = {
  /**
   * POST /api/tenants/{tenantId}/projects/{projectId}/ai/cost/parse
   * Parsuje dokument (JPG/PNG/PDF) przez AI i zwraca sugestię kosztu.
   */
  parseCostDocument: async (
    tenantId: string,
    projectId: string,
    data: ParseCostDocumentRequest
  ): Promise<ParsedCostDto> => {
    const form = new FormData();
    form.append('file', data.file);
    form.append('costType', data.costType);

    const res = await axiosClient.post<ParsedCostDto>(
      `/tenants/${tenantId}/projects/${projectId}/ai/cost/parse`,
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    );
    return res.data;
  },
};
```

**Uwaga:** `axiosClient` ma ustawioną baseURL `api/` — route w kontrolerze `api/tenants/{tenantId}/projects/{projectId}/ai/cost/parse` pasuje bezpośrednio. Sprawdź `axiosClient.ts` czy baseURL zawiera `/api/` — jeśli tak, nie dodawaj prefiksu.

---

## BLOK 5 — Hooki React Query

| Hook | Plik | Nowy/Modyfikacja | Query/Mutation | Opis |
|------|------|-----------------|---------------|------|
| `useAICostDocumentParser` | `src/hooks/useAICostDocumentParser.ts` | **Nowy** | `useMutation` | Mutacja parsowania dokumentu. Stan: idle→loading→success/error. `mutateAsync` do wywołania z komponentu |

### Implementacja `useAICostDocumentParser`

```typescript
// src/hooks/useAICostDocumentParser.ts
import { useMutation } from '@tanstack/react-query';
import { aiCostApi } from '../api/aiCostApi';
import type { ParsedCostDto, ParseCostDocumentRequest } from '../types/ai.types';

interface UseAICostDocumentParserParams {
  tenantId: string;
  projectId: string;
}

export function useAICostDocumentParser({ tenantId, projectId }: UseAICostDocumentParserParams) {
  return useMutation<ParsedCostDto, Error, ParseCostDocumentRequest>({
    mutationFn: (data: ParseCostDocumentRequest) =>
      aiCostApi.parseCostDocument(tenantId, projectId, data),
  });
}

// Użycie:
// const { mutateAsync: parseDocument, isPending, isError, error } = useAICostDocumentParser({ tenantId, projectId });
// await parseDocument({ file, costType: 'TrackedCost' });
```

**Wzorzec React Query 5:** `isPending` zamiast `isLoading` (zmiany w RQ5 API). Nie invaliduje żadnego cache — parsowanie jest read-only.

---

## BLOK 6 — Nowe komponenty

| Komponent | Lokalizacja | Opis | Zależy od |
|-----------|------------|------|-----------|
| `AICostImportModal` | `src/components/CostTracker/AICostImportModal.tsx` | Modal 2-krokowy: krok 1 = upload pliku, krok 2 = edytowalne pola wypełnione przez AI + zatwierdzenie. Po zatwierdzeniu wywołuje `onConfirm(values)`. | `AppModal`, `useAICostDocumentParser`, `DocumentDropzone`, `CostForm`, `ContractorPicker`, `ParsedCostDto` |
| `DocumentDropzone` | `src/components/ui/DocumentDropzone.tsx` | Prosty komponent drag-and-drop lub click-to-select dla jednego pliku. Akceptuje `.jpg, .jpeg, .png, .pdf`. Wyświetla nazwę pliku lub placeholder. | Chakra UI `Box`, `Text`, `Icon` |

### Props `AICostImportModal`

```typescript
interface AICostImportModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  /** Typ kosztu determinuje które pola pokazać i którym endpointem zapisać */
  costType: CostDocumentType;
  /** Kontekst dla TrackedCost — przekazywane do createCost jeśli costType='TrackedCost' */
  costEstimateId?: string | null;
  costEstimateItemId?: string | null;
  /** Wywoływane po pomyślnym zatwierdzeniu i zapisaniu kosztu */
  onSuccess: () => void;
}
```

### Logika kroków `AICostImportModal`

```
Krok 1: Upload
  - DocumentDropzone (.jpg, .jpeg, .png, .pdf, max 20MB)
  - Przycisk "Analizuj dokument" (disabled gdy brak pliku)
  - Po kliknięciu → wywołuje useAICostDocumentParser.mutateAsync
  - Podczas parsowania: Spinner + "AI analizuje dokument..."
  
Krok 2: Edycja + zatwierdzenie  
  - Pasek confidence (jeśli < 0.7 → Alert warning "Niska pewność AI")
  - Baner kontrahenta gdy contractorFound = false + suggestedContractor:
      Alert status="info": "Nie znaleziono kontrahenta '{name}'. Dodaj ręcznie."
  - Formularz (pola edytowalne, wypełnione z ParsedCostDto):
    - Nazwa (required)
    - Numer faktury
    - Kwota netto (zawsze)
    - Kwota brutto (tylko gdy costType='ProjectCost')
    - Wykonawca (ContractorPicker, pre-fill contractorId gdy found)
    - Data
    - Opis
  - Przyciski w stopce AppModal:
    - Anuluj (wraca do kroku 1)
    - "Potwierdź i dodaj koszt" (colorScheme="green")
```

### Mapowanie `ParsedCostDto` → formularz

| ParsedCostDto | CostFormValues (TrackedCost) | CostFormState (ProjectCost) |
|---------------|-----------------------------|-----------------------------|
| `name` | `name` | `name` |
| `description` | `description` | `description` |
| `net` | `net` (number→string via String()) | `net` (String()) |
| `gross` | — (brak pola) | `gross` (String()) |
| `number` | `number` | `number` |
| `date` | `date` (ISO→`date.substring(0, 10)`) | `date` (ISO→`split('T')[0]`) |
| `contractorId` gdy `contractorFound=true` | `contractorId` | `contractorId` |
| `contractorId` gdy `contractorFound=false` | `contractorId: null` | `contractorId: null` |

### Zapis po potwierdzeniu

```typescript
// costType = 'TrackedCost' → costTrackerApi.createCost(...)
// costType = 'ProjectCost' → projectApi.createProjectCost(...) z date: new Date(form.date)
```

---

## BLOK 7 — Modyfikacje istniejących komponentów

| Komponent | Plik | Typ zmiany | Opis |
|-----------|------|-----------|------|
| `CostFormModal` | `src/components/CostTracker/CostFormModal.tsx` | Modyfikacja `renderStep()` case 3 | Zawinąć `<CostForm>` w `<VStack>`, dodać `<Button leftIcon={<Wand2 />} variant="outline" size="sm">Importuj z dokumentu</Button>` przed formularzem. Button otwiera `AICostImportModal` z `costType='TrackedCost'`. Po `onSuccess` wywołuje `handleClose()` + `onSuccess()`. |
| `CostFormDrawer` | `src/components/CostTracker/CostFormDrawer.tsx` | Modyfikacja DrawerBody | W `DrawerBody > VStack`, jako pierwszy element (gdy `!isEdit`), dodać Button "Importuj z dokumentu". Otwiera `AICostImportModal`. |
| `CostModal` (dashboard) | `src/features/dashboard/components/CostModal.tsx` | Modyfikacja body AppModal | W `<AppModal>` body, w głównym `<VStack>`, jako **pierwszy element** (przed `{error && ...}`), gdy `mode === 'create'`: dodać `<Button>Importuj z dokumentu</Button>`. |

### Szczegółowe lokalizacje wstawienia — linie

#### `CostFormModal.tsx` — renderStep case 3 (ok. linia 304)
```tsx
// PRZED:
case 3:
  return (
    <CostForm
      values={formValues}
      ...
    />
  );

// PO:
case 3:
  return (
    <VStack spacing={4} align="stretch">
      <Button
        leftIcon={<Wand2 size={14} />}
        variant="outline"
        size="sm"
        colorScheme="primary"
        alignSelf="flex-start"
        onClick={onOpenAIImport}
        isDisabled={isSubmitting}
      >
        Importuj z dokumentu
      </Button>
      <CostForm
        values={formValues}
        ...
      />
    </VStack>
  );
```

#### `CostFormDrawer.tsx` — DrawerBody VStack (ok. linia 145)
```tsx
// PRZED:
<DrawerBody>
  <VStack spacing={4} align="stretch">
    <CostForm ... />

// PO:
<DrawerBody>
  <VStack spacing={4} align="stretch">
    {!isEdit && (
      <Button
        leftIcon={<Wand2 size={14} />}
        variant="outline"
        size="sm"
        colorScheme="primary"
        alignSelf="flex-start"
        onClick={onOpenAIImport}
        isDisabled={isSubmitting}
      >
        Importuj z dokumentu
      </Button>
    )}
    <CostForm ... />
```

#### `CostModal.tsx` — AppModal body, w VStack (ok. linia 325)
```tsx
// PRZED:
<VStack spacing={4} align="stretch" sx={...}>
  {error && <Alert ...>}
  <FormControl isRequired>

// PO:
<VStack spacing={4} align="stretch" sx={...}>
  {mode === 'create' && (
    <Button
      leftIcon={<Wand2 size={14} />}
      variant="outline"
      size="sm"
      colorScheme="primary"
      alignSelf="flex-start"
      onClick={onOpenAIImport}
    >
      Importuj z dokumentu
    </Button>
  )}
  {error && <Alert ...>}
  <FormControl isRequired>
```

---

## BLOK 8 — Spójność UI

| Wzorzec | Istniejąca implementacja | Czy feature musi się dostosować |
|---------|------------------------|--------------------------------|
| Modal base — `AppModal` | `CostModal.tsx` (dashboard) używa `AppModal`. `CostFormModal.tsx` ma własny Modal/Drawer. | `AICostImportModal` → **używać `AppModal`** (jak CostModal dashboard) |
| Multi-step w modalu | `CostFormModal.tsx` używa Chakra `Stepper`. | AICostImportModal ma tylko 2 kroki — wystarczy `useState<'upload'\|'preview'>` bez Stepper |
| Nazwa przycisku akcji | Istniejące modale: "Dodaj", "Zapisz zmiany", "Potwierdź" | Button: **"Importuj z dokumentu"** (wejściowy), **"Potwierdź i dodaj koszt"** (w kroku 2) |
| Ikona AI | Brak precedensu — istniejące przyciski używają `Plus`, `Trash2`, `Copy`. | Rekomendacja: `Wand2` z lucide-react (symbol AI/magic). Import sprawdzić. |
| Obsługa błędów API | `handleApiError(err)` + `showError(title, description)`. Wzorzec z `CostFormModal.tsx`, `CostFormDrawer.tsx`. | `AICostImportModal` musi importować `handleApiError` i `useToastNotification` |
| Loading state | `isLoading`/`isSubmitting` + `isLoading={true}` na Button. | `useAICostDocumentParser.isPending` → Button "Analizuj" w stanie `isLoading` |
| FormData dla multipart | `costTrackerApi.ts` buduje FormData z `buildCostFormData()`. `projectApi.ts` buduje FormData z PascalCase. | `aiCostApi.ts` → camelCase (serwer .NET odbiera z `[FromForm]` case-insensitive) |
| ContractorPicker | Używany w `CostForm.tsx` i `CostModal.tsx`. Props: `tenantId, value, onChange, canQuickAdd, isDisabled, isInvalid`. | AICostImportModal krok 2 musi przekazać `canQuickAdd` z `useProjectPermissions` |
| Waluty/kwoty | `formatCurrency` lokalna w `ProjectCosts.tsx`. Brak globalnego formattera. Pola kwoty jako `NumberInput` (CostForm) lub `<Input type="number">` (CostModal). | W AICostImportModal kroku 2: użyć `NumberInput` (spójność z CostForm) |

---

## BLOK 9 — Dostępność (WCAG AA / AXE) — OBOWIĄZKOWY

### Kontrast kolorów

| Element | Kolor tekstu | Kolor tła | Kontrast (szac.) | Status |
|---------|-------------|-----------|-----------------|--------|
| Przycisk "Importuj z dokumentu" (outline) | `primary.600` (#2B6CB0) | white | ~7.6:1 | ✓ |
| Label w formularzu kroku 2 | `neutral.700` lub domyślny Chakra | white | ~11:1 | ✓ |
| Alert baner kontrahenta (info) | `blue.800` | `blue.50` | ~8:1 | ✓ |
| Alert confidence (warning) | `orange.800` | `orange.50` | ~7:1 | ✓ |
| Placeholder w `DocumentDropzone` | `neutral.500`/`gray.500` | tło `gray.50` | ~4.5:1 | ⚠ sprawdź przy finalnym projekcie |

### Atrybuty ARIA

| Komponent | Problem | Rekomendacja |
|-----------|---------|-------------|
| `DocumentDropzone` jako `<Box onClick>` | Jeśli zaimplementowana jako `div`, brak role i obsługi klawiatury | Użyć `role="button"`, `tabIndex={0}`, `onKeyDown` (Enter/Space = click), `aria-label="Wybierz dokument"` |
| Ukryty `<input type="file">` | Sam w sobie nie jest problemem, ale potrzebuje `aria-label` lub powiązanego `<label>` | Dodać `aria-label="Wybierz plik do importu"` |
| Spinner "AI analizuje dokument..." | Tekst musi być dostępny dla screen readerów | Dodać `role="status"` lub `aria-live="polite"` na kontener z Spinner+Text |
| Ikona `<Wand2>` w Button | Ikona obok tekstu przycisku | Dodać `aria-hidden="true"` na ikonę (tekst Button wystarczy) |
| Pasek confidence (Progress) | Brak etykiety | Dodać `aria-label={`Pewność AI: ${Math.round(confidence * 100)}%`}` |
| `AICostImportModal` — krok 1/2 | Zmiana kroku bez powiadomienia | Dodać `aria-live="polite"` na kontener kroku lub `role="region"` z `aria-label` |

### Zarządzanie fokusem

- `AICostImportModal` opiera się na Chakra `Modal` (przez `AppModal`) → automatyczny focus trap ✓
- Przy przejściu krok 1 → krok 2: focus powinien przenieść się na pierwsze pole formularza (pole Nazwa). Realizacja przez `useEffect` + `ref.current?.focus()` po zmianie kroku.
- `DocumentDropzone`: musi być osiągalna klawiaturą (Tab, Enter/Space).

### Testy AXE

- `AICostImportModal` — brak testów (nowy komponent). Należy dodać test AXE w `src/components/CostTracker/__tests__/AICostImportModal.test.tsx`
- `DocumentDropzone` — brak testów. Dodać test AXE w `src/components/ui/__tests__/DocumentDropzone.test.tsx`

### Podsumowanie dostępności

| Kategoria | Status | Uwagi |
|----------|--------|-------|
| Kontrast kolorów | ✓ | Użyć `primary.600` dla outline button (sprawdzony) |
| Atrybuty ARIA | ⚠ | DocumentDropzone wymaga `role="button"` + keyboard handler; Spinner wymaga `aria-live` |
| Klawiatura / fokus | ⚠ | Focus na pierwsze pole po przejściu krok 1→2; DocumentDropzone keyboard support |
| Testy AXE | ✗ | Nowe komponenty wymagają testów AXE |

---

## BLOK 10 — Problemy i ryzyka

| # | Problem | Komponent/Plik | Ryzyko | Rekomendacja |
|---|---------|---------------|--------|-------------|
| 1 | `CostFormModal` nie używa `AppModal` — ma własną implementację Modal+Drawer | `CostFormModal.tsx` | Średnie — `AICostImportModal` musi być zagnieżdżony wewnątrz istniejącego Modalu/Drawera | Chakra obsługuje zagnieżdżone modale przez `useDisclosure`. Przetestować czy overlay działa poprawnie na mobile. |
| 2 | Brak pola `gross` w `CostFormValues` | `costTracker.types.ts` | Niskie — AICostImportModal dla ProjectCost musi używać innego stanu formularza (`CostFormState` z CostModal) | W `AICostImportModal` wewnętrznie zarządzać stanem w zależności od `costType`. Nie modyfikować `CostFormValues`. |
| 3 | `projectApi.createProjectCost` przyjmuje `date: Date` (obiekt), nie string | `projectApi.ts` | Niskie — łatwe do obsłużenia | W `AICostImportModal` przy zapisie ProjectCost: `date: form.date ? new Date(form.date) : new Date()` |
| 4 | API endpoint timeout — GPT-4o Vision może trwać 10-30 sekund | `AICostImportModal` | Wysokie (UX) | Axios timeout domyślny może być za niski. Sprawdzić `axiosClient.ts` — jeśli < 30s, przekazać `timeout: 45000` dla parsowania albo ustawić w `aiCostApi.ts` |
| 5 | Brak obsługi PDF w API (patrz API audit — blocker) | — | Wysokie | W `DocumentDropzone` i walidacji pliku na poziomie UI poinformować usera że PDF może być niedostępny (tymczasowo akceptować tylko JPG/PNG) dopóki backend nie obsłuży PDF |
| 6 | Brak i18n — wszelkie stringi hardcoded po polsku | Cały UI | Niskie | Projekt nie używa i18next. Wszystkie nowe stringi pisać po polsku inline, jak istniejący kod. |
| 7 | `useProjectCostMutations` — starszy pattern (useState zamiast useMutation) | `useProjectCostMutations.ts` | Niskie | Nie modyfikować istniejącego hooka. `AICostImportModal` dla ProjectCost może importować `projectApi` bezpośrednio lub używać nowego `useMutation` |
| 8 | Podwójne zagnieżdżenie modali na mobile (full-screen) | `CostFormModal` + `AICostImportModal` | Średnie | Na mobile `CostFormModal` renderuje jako Drawer (size="full"). Otwieranie kolejnego modala full-screen może dać złe UX. Rozważyć: na mobile step 3 w `CostFormModal` nie pokazuje przycisku importu (lub otwiera bottom-sheet) |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe komponenty | 2 (`AICostImportModal`, `DocumentDropzone`) |
| Zmodyfikowane komponenty | 3 (`CostFormModal`, `CostFormDrawer`, `CostModal`) |
| Nowe hooki | 1 (`useAICostDocumentParser`) |
| Nowe typy TypeScript | 4 (`ParsedCostDto`, `CostDocumentType`, `ParseCostDocumentRequest`, `SuggestedContractor`) w nowym pliku `src/types/ai.types.ts` |
| Nowe wywołania API | 1 (`aiCostApi.parseCostDocument`) w nowym pliku `src/api/aiCostApi.ts` |
| Naruszenia WCAG AA | 3 (DocumentDropzone brak role/keyboard, Spinner brak aria-live, brak testów AXE) |
| Pytania domenowe | 3 |

### Pytania domenowe wymagające decyzji

1. **Obsługa PDF przed gotowością API:** Czy w kroku 1 (upload) blokować PDF do czasu aż backend zaimplementuje konwersję, czy akceptować i zwracać błąd z serwera? Rekomendacja: na UI akceptować wszystkie typy, wyświetlić error z API gdy PDF niedostępny.

2. **Zapis kosztu wewnątrz AICostImportModal czy przez pre-fill:** Czy `AICostImportModal` po potwierdzeniu **sam zapisuje** koszt (end-to-end w modalu) i woła `onSuccess()`, czy wywołuje `onPrefill(values: ParsedCostDto)` który zamknka AI modal i pre-wypełnia CostFormModal/Drawer? Feature.md sugeruje zapis end-to-end w modalu, co daje lepsze UX, ale wymaga duplikacji logiki zapisu.

3. **Integracja kontrahenta suggestedContractor:** Gdy `contractorFound: false`, pokazujemy baner z nazwą kontrahenta z dokumentu. Czy przycisk "Dodaj kontrahenta" ma otwierać `ContractorPicker` z `canQuickAdd` (quick-add inline), czy nawigować do `/contractors`? Rekomendacja: quick-add przez istniejący ContractorPicker (prop `canQuickAdd`).
