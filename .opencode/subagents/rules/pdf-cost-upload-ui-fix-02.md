# pdf-cost-upload-ui-fix-02 — AICostImportModal + typy rejectedFiles

## Kontekst

Zależność: UI fix-01; API fix-03 (`RejectedFiles` w response).  
Skills: ui-components, ui-hooks, ui-types, ui-api-client

## Cel

Copy modalu; obsługa `rejectedFiles` z batch API; toasty soft-fail.

## Zadania

1. `types/ai.types.ts` — rozszerz response batch:
```typescript
export interface AICostImportRejectedFile {
  fileName: string;
  reason: string;
}
// w AICostImportBatchWeb / submit result:
rejectedFiles?: AICostImportRejectedFile[];
```
   Zsynchronizuj nazwy pól z API (camelCase z JSON).

2. `aiCostApi.ts` — typy odpowiedzi zgodne (bez regresji multipart).

3. `AICostImportModal.tsx`:
   - Tekst: „JPG, PNG lub PDF. Jeden plik — natychmiastowa analiza. Wiele plików — analiza w tle (łącznie do 50 MB).”
   - Podłącz `onFilesRejected` z dropzone → `showError` z listą
   - Po `submitBatch`: jeśli `rejectedFiles?.length` → `showError` / `showInfo` z powodami; jeśli są accepted — nadal toast „Analiza w tle”

4. Zaktualizuj `aiCostApi.test.ts` jeśli mockuje response.

## Kryteria done

- [ ] Copy zaktualizowane
- [ ] rejectedFiles obsługiwane
- [ ] Brak `any`
