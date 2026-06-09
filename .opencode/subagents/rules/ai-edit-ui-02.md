# Prompt: ai-edit-ui-02 — Hook useAICostEstimateEdit + Modal AIEditCostEstimateModal

## Cel

Stworzyć hook `useAICostEstimateEdit` oraz modal `AIEditCostEstimateModal` do edycji kosztorysu przez AI.

## Pliki do utworzenia

### 1. `ProjectDataManagementUI/src/hooks/useAICostEstimateEdit.ts`

Wzoruj się na `useGenerateCostEstimateWithAI.ts`:

```typescript
import { useMutation } from '@tanstack/react-query';
import { costEstimateApi } from '../api/costEstimateApi';
import type {
  AICostEditRequestDto,
  AICostEditPreviewDto,
  ApplyAICostEditDto,
} from '../types/costEstimate.types.new';

export function useAICostEstimateEdit(
  tenantId: string,
  projectId: string,
  costEstimateId: string
) {
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

### 2. `ProjectDataManagementUI/src/components/AIEditCostEstimateModal.tsx`

Wzoruj się na `GenerateCostEstimateWithAIModal.tsx` (ten sam wzorzec wizard modala).

**Props:**
```typescript
interface AIEditCostEstimateModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  costEstimateId: string;
  onEditSuccess: () => void;
}
```

**Stany (enum):**
```typescript
type AIEditStep = 'input' | 'generating' | 'preview' | 'applying';
```

**Krok 1 — Input:**
- Textarea: "Opisz co chcesz zmienić w kosztorysie..."
- Placeholder: np. "Dodaj 3 pozycje do grupy Fundamenty..."
- Walidacja: niepuste, max 2000 znaków
- Przycisk: "Generuj propozycję" (ikona Bot/Zap)
- Chakra UI: FormControl, FormLabel, Textarea, FormErrorMessage
- Użyj `useRef` dla textarea z `autoFocus`

**Krok 2 — Generating:**
- Spinner + "AI analizuje kosztorys..."
- Progress bar (indeterminate)
- Modal nie jest closable (`closeOnOverlayClick={false}`, ukryty CloseButton)

**Krok 3 — Preview:**
- Alert z podsumowaniem: `summary` z preview
- Sekcja "Proponowane zmiany":
  - Jeśli `suggestedName` != null → "Nowa nazwa: {suggestedName}"
  - Lista grup w Accordion (jak Step4Preview w GenerateCostEstimateWithAIModal) — pełne drzewo
  - Użyj istniejących komponentów: `AIGroupPreviewDto` → Accordion, items jako listy
- Ostrzeżenia (jeśli są) → Alert warning
- Przyciski: "Wstecz" (krok 1) / "Zatwierdź zmiany"

**Krok 4 — Applying:**
- Spinner + "Zapisywanie zmian..."
- Po sukcesie → `onEditSuccess()` (co zamyka modal i przeładowuje dane)
- Po błędzie → Alert error + przycisk "Zamknij"

**Obsługa błędów:**
- `handleApiError` + toast notification
- Błędy network: Alert w modalu
- Błąd AI (brak odpowiedzi): warning + przycisk "Spróbuj ponownie"

**Wzór struktury JSX:**
```tsx
<Modal isOpen={isOpen} onClose={onClose} size="4xl" scrollBehavior="inside">
  <ModalOverlay />
  <ModalContent>
    <ModalHeader>
      <HStack>
        <Icon as={Bot} aria-hidden="true" />
        <Text>Edytuj kosztorys z AI</Text>
      </HStack>
      {step !== 'input' && step !== 'applying' && (
        <Progress size="sm" isIndeterminate={step === 'generating'} />
      )}
    </ModalHeader>
    <ModalCloseButton />
    <ModalBody>
      {/* Step content */}
    </ModalBody>
    <ModalFooter>
      {/* Step-specific footer */}
    </ModalFooter>
  </ModalContent>
</Modal>
```

**Footer w zależności od kroku:**
- `input`: Anuluj (left) + Generuj propozycję (right)
- `generating`: brak przycisków (lub disabled Anuluj)
- `preview`: Wstecz (left) + Zatwierdź zmiany (right, colorScheme="purple")
- `applying`: tylko Spinner

**Ważne zasady:**
- Zakaz inline styles (`var(--chakra-colors-*)`)
- Ikony dekoracyjne: `aria-hidden="true"`
- Spinner: `role="status"` + `aria-live="polite"`
- Komunikaty błędów: Alert z `role="alert"`
- Kontrast: nie używać `gray.400` dla tekstu

## Weryfikacja

1. Plik `useAICostEstimateEdit.ts` istnieje
2. Plik `AIEditCostEstimateModal.tsx` istnieje
3. TypeScript kompiluje bez błędów
4. Modal ma wszystkie 4 kroki
5. Obsługa błędów działa (toast + alert)
