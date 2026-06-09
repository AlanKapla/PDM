# Prompt UI-02: Hook useGenerateCostEstimateWithAI

## Cel
Utwórz hook React Query do obsługi generowania i zapisu kosztorysu z AI.

---

## Lokalizacja

```
src/hooks/useGenerateCostEstimateWithAI.ts
```

---

## Implementacja

```typescript
import { useMutation } from '@tanstack/react-query';
import { costEstimateApi } from '../api/costEstimateApi';
import type {
  AICostEstimateRequestDto,
  AICostEstimatePreviewDto,
  CreateCostEstimateFromAIPreviewDto,
} from '../types/costEstimate.types.new';

/**
 * Hook do generowania podglądu kosztorysu przez AI i jego zapisu po zatwierdzeniu.
 *
 * Wzorzec użycia w komponencie:
 *  const { generatePreview, createFromPreview } = useGenerateCostEstimateWithAI(tenantId, projectId);
 *
 *  // Krok 1 — wygeneruj podgląd (nie zapisuje do DB)
 *  const preview = await generatePreview.mutateAsync(request);
 *
 *  // Krok 2 — po zatwierdzeniu przez użytkownika — zapisz
 *  const id = await createFromPreview.mutateAsync({ name, description, preview });
 */
export function useGenerateCostEstimateWithAI(tenantId: string, projectId: string) {
  const generatePreview = useMutation<AICostEstimatePreviewDto, Error, AICostEstimateRequestDto>({
    mutationFn: (request: AICostEstimateRequestDto) =>
      costEstimateApi.generateAIPreview(tenantId, projectId, request),
  });

  const createFromPreview = useMutation<string, Error, CreateCostEstimateFromAIPreviewDto>({
    mutationFn: (body: CreateCostEstimateFromAIPreviewDto) =>
      costEstimateApi.createFromAIPreview(tenantId, projectId, body),
  });

  return {
    /** Mutacja generowania podglądu AI (nie zapisuje do DB) */
    generatePreview,
    /** Mutacja zapisu zatwierdzonego podglądu */
    createFromPreview,
  };
}
```

---

## Konwencje
- TanStack React Query 5 — `useMutation` z jawnym typowaniem generycznym
- Brak `onSuccess`/`onError` w hooku — obsługa błędów w komponencie
- Eksport nazwy (nie default)

## Weryfikacja
```
npx tsc --noEmit 2>&1 | Select-String "useGenerateCostEstimateWithAI|error TS"
```
Oczekiwany wynik: brak błędów.
