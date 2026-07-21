# Summary — pdf-cost-upload

Data: 2026-07-21  
Feature: `.opencode/features/pdf-cost-upload.md`  
Status: **wdrożony** (API + UI)

## Co zostało zrobione

### Planowanie
- Feature spec, audyty API/UI, 5 promptów API + 4 UI

### API
1. **Docnet.Core 2.6.0 + ImageSharp 3.1.12** — `IPdfToImageConverter` (175 DPI, max 20 stron, JPEG q=85)
2. **`PdfConversionException`** — PasswordProtected / Corrupt / TooManyPages (+ `UserMessage` PL)
3. **Multi-image Vision** — `CompleteWithImagesAsync`; `DocumentParserService` PDF→obrazy→AI
4. **Walidacja** — `FileContentValidator` (extension + MIME + magic bytes)
5. **Soft-fail batch** — `rejectedFiles[]` w `AICostImportSubmitResultWeb`; `totalFiles` = zaakceptowane
6. **Worker** — `PdfConversionException` → `ErrorNeedsReview` bez retry; blob = oryginał
7. **Limity** — ProjectCost document 50 MB; TrackedCost `NewFiles` typ + 50 MB

### UI
1. **MultiDocumentDropzone / DocumentDropzone** — JPG/PNG/PDF + soft-fail `onFilesRejected`
2. **AICostImportModal** — copy + toasty rejected (dropzone + API)
3. **AICostReviewItem** — iframe / otwórz PDF przez SAS
4. **CostForm / CostModal** — `accept` + filtr soft-fail

## Nowe pliki (kluczowe)

### API
- `Business/Interfaces/Services/IPdfToImageConverter.cs`
- `Business/Implementation/Services/AI/PdfToImageConverter.cs`
- `Business/Interfaces/Exceptions/PdfConversionException.cs` (+ Reason enum)
- `Business/Interfaces/Helpers/FileContentValidator.cs`
- Testy: `PdfToImageConverterTests`, `DocumentParserServiceTests`, `FileContentValidatorTests`, `ParseCostDocumentQueryHandlerTests`, `AICostImportWorkerTests`, `ParseCostDocumentQueryValidatorTests`, `CreateTrackedCostCommandValidatorTests` (NewFiles)

### UI
- `AICostReviewItem.axe.test.tsx`

## Zmodyfikowane obszary
- `AzureAICompletionService`, `DocumentParserService`, `AICostController`, Parse/SubmitBatch validators+handlers, `AICostImportWorker`, `DocumentValidationHelper`, `TrackedCostCommandBaseValidator`
- `MultiDocumentDropzone`, `DocumentDropzone`, `AICostImportModal`, `AICostReviewItem`, `CostForm`, `CostModal`, `ai.types.ts`, `aiCostApi.ts`, `useAICostImportBatch`

## Status testów

| Warstwa | Wynik (wg agentów) |
|---------|-------------------|
| API Business.Tests | ✅ (m.in. 270 przy fix-03; AI/PDF suite zielony) |
| API CQRS.Tests | ✅ (988 przy fix-03; AI filtry zielone) |
| API WebApi.Tests | ✅ (207 przy fix-03; validators 46/46 przy fix-05) |
| UI Vitest feature | ✅ 17/17 (MultiDocumentDropzone, aiCostApi, AXE review) |
| UI pełny suite (fix-04) | ✅ 93/93 |

Build API Release: **0 błędów** po każdym prompcie.

## Blokery
Brak.

## Pozostałe TODO / follow-up

1. **Smoke-test Docker Linux** — konwersja 1-stronicowego PDF w kontenerze (`pdfium.so` RID linux-x64).
2. **Jakość AI** — weryfikacja 175 DPI na próbce wielostronicowych faktur; ewentualnie tuning DPI / JPEG quality.
3. **Fixture PDF z hasłem / >20 stron** — unit testy convertera (mapowanie ErrorCode jest w kodzie; brak pełnych fixture’ów).
4. **Limit single-parse 20 MB** — celowo bez zmian (zgodnie z multi-file spec); ewentualna późniejsza ujednolicenie.
5. **ImageSharp 3.1.12** — nie 4.x (v4 wymaga licencji przy buildzie).

## Prompty / rules

```
.opencode/features/pdf-cost-upload.md
.opencode/subagents/rules/pdf-cost-upload-api-audit.md
.opencode/subagents/rules/pdf-cost-upload-ui-audit.md
.opencode/subagents/rules/pdf-cost-upload-api-fix-01.md … 05.md
.opencode/subagents/rules/pdf-cost-upload-ui-fix-01.md … 04.md
.opencode/subagents/rules/pdf-cost-upload-summary.md  (ten plik)
```
