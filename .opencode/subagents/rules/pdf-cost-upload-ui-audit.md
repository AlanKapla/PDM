# Audyt UI — pdf-cost-upload

Data: 2026-07-21  
Źródło: feature-planner + feature `.opencode/features/pdf-cost-upload.md`

## Podsumowanie

| Poziom | Opis |
|--------|------|
| Krytyczne | MultiDocumentDropzone filtruje PDF; test asertywnie odrzuca PDF |
| Wysokie | Copy modalu JPG/PNG; AICostReviewItem bez podglądu PDF |
| Normalne | CostForm bez `accept`; DocumentDropzone (single) nadal JPG/PNG |

## Co już istnieje (reuse)

1. **`AICostImportModal`** + `MultiDocumentDropzone` — single sync / batch async.
2. **`useAICostDocumentParser` / `useAICostImportBatch` / `aiCostApi`** — multipart; typy w `ai.types.ts`.
3. **`AICostReviewItem`** — SAS `previewUrl`; dziś tylko `<Image>` dla image/*.
4. **`CostModal` (ProjectCost)** — już `accept=".pdf,.jpg,.jpeg,.png"`.
5. **`useToastNotification`** — showError / showInfo dla soft-fail.

## Luki

| Komponent | Problem |
|-----------|---------|
| `MultiDocumentDropzone.tsx` | `ACCEPTED_EXTENSIONS` = jpg/jpeg/png; `isAcceptedImageFile`; copy „JPG, PNG”; filtruje PDF |
| `MultiDocumentDropzone.test.tsx` | `plikNieobrazkowy_jestFiltrowany` — oczekuje `onChange([])` dla PDF → do odwrócenia |
| `DocumentDropzone.tsx` | accept/copy JPG/PNG (używany rzadziej; ujednolicić) |
| `AICostImportModal.tsx` | Tekst „JPG lub PNG” |
| `AICostReviewItem.tsx` | `isImagePreview` bez PDF → „Podgląd niedostępny” mimo SAS |
| `CostForm.tsx` | `<input type="file" multiple>` bez `accept` |
| `ai.types.ts` / `aiCostApi.ts` | Brak `rejectedFiles` w typie response batch — do synchronizacji z API |

## Soft-fail UX

- Dropzone: niepoprawne pliki → toast / inline alert, poprawne zostają na liście.
- Po submit batch: jeśli API zwróci `rejectedFiles[]` — toast z listą nazw + powodów.
- Limit łączny 50 MB — bez zmian (`onSizeExceeded`).

## Podgląd PDF w review

- Jeśli `contentType === 'application/pdf'` lub `.pdf`: pokaż przycisk „Otwórz PDF” / `<iframe src={previewUrl}>` (preferencja: iframe + link „pełny rozmiar”).
- Nie renderować `<Image>` dla PDF.

## Pliki do zmiany

```
src/components/ui/MultiDocumentDropzone.tsx
src/components/ui/MultiDocumentDropzone.test.tsx
src/components/ui/DocumentDropzone.tsx
src/components/CostTracker/AICostImportModal.tsx
src/components/AICostReview/AICostReviewItem.tsx
src/components/CostTracker/CostForm.tsx
src/types/ai.types.ts
src/api/aiCostApi.ts (+ test jeśli dotyczy)
```

## Skills

- ui-components, ui-hooks, ui-types, ui-unit-tests, ui-accessibility
