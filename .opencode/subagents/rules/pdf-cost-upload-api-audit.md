# Audyt API — pdf-cost-upload

Data: 2026-07-21  
Źródło: feature-planner + feature `.opencode/features/pdf-cost-upload.md`

## Podsumowanie

| Poziom | Liczba / opis |
|--------|----------------|
| Krytyczne | 6 — brak PDF w AI, brak konwersji, Vision 1 obraz, brak NuGet, brak magic bytes, batch all-or-nothing |
| Wysokie | 3 — limity 10/20/50 MB, komentarz vs kod w IDocumentParserService, TrackedCost bez walidacji plików |
| Normalne | 2 — PNG do zachowania, preview PDF w review (UI) |

## Co już istnieje (reuse)

1. **`AICostController`** — parse sync + batch; limity RequestSizeLimit (20 MB parse, 50 MB batch).
2. **`DocumentParserService` + `IAICompletionService.CompleteWithImageAsync`** — Vision na jednej binarke.
3. **`AICostImportWorker`** — per-item download z blob → ParseAsync → enrich → duplicate; błędy → retry / ErrorNeedsReview.
4. **`AICostImportBlobService`** — upload pending oryginału; MoveToCostAttachment przy accept; SAS preview.
5. **`DocumentValidationHelper`** — już dopuszcza PDF dla ProjectCost (extension + MIME), ale MaxDocumentSize = **10 MB**.
6. **`SubmitAICostImportBatchCommandHandler`** — zapis oryginału do blob (gotowe pod PDF bez konwersji przy upload).

## Czego NIE reuse'ować / luki

| Plik | Problem |
|------|---------|
| `AICostController.ParseDocumentInternal` | Tylko `.jpg/.jpeg/.png` |
| `ParseCostDocumentQueryValidator` | Tylko JPG/PNG |
| `SubmitAICostImportBatchCommandValidator` | Tylko JPG/PNG; RuleForEach = fail całej paczki |
| `DocumentParserService` | Brak gałęzi PDF; przekazuje `application/pdf` do Vision |
| `AzureAICompletionService` | Brak multi-image API |
| `Business.csproj` / AIAgent | Brak Docnet, ImageSharp |
| Brak magic-byte validation | Tylko rozszerzenie / MIME z klienta |
| `DocumentValidationHelper` | 10 MB — do wyrównania do 50 MB |
| `TrackedCostCommandBaseValidator` | Brak reguł na `NewFiles` |

## Co dodać

### Serwisy (Business)
- `IPdfToImageConverter` / `PdfToImageConverter` — Docnet 175 DPI, max 20 stron, ImageSharp → JPG bytes[]
- Wyjątki domenowe lub typed results: PasswordProtected, Corrupt, TooManyPages
- Rejestracja DI w `ServiceCollectionExtensions`

### AI
- `CompleteWithImagesAsync(systemPrompt, IReadOnlyList<(byte[] bytes, string mediaType)>, ct)`
- `DocumentParserService.ParseAsync`: jeśli PDF → convert → multi-image; else istniejąca ścieżka

### Walidacja
- Shared helper: extension + MIME + magic (`FF D8` JPEG, `%PDF` PDF, PNG signature)
- Soft-fail batch: nieblokujący RuleForEach; handler filtruje → `rejectedFiles[]` w `AICostImportSubmitResultWeb`
- Worker: przy PDF password/corrupt → LastError PL, status ErrorNeedsReview

### Limity
- `DocumentValidationHelper.MaxDocumentSize` → 50L * 1024 * 1024
- Opcjonalnie TrackedCost NewFiles: te same typy + 50 MB per file

## Pliki do zmiany (lista)

```
src/Business/Business.csproj
src/Business/Interfaces/Services/IPdfToImageConverter.cs          (new)
src/Business/Implementation/Services/AI/PdfToImageConverter.cs    (new)
src/Business/Interfaces/Helpers/DocumentValidationHelper.cs
src/Business/Interfaces/Helpers/FileMagicBytesValidator.cs        (new, opcjonalnie)
src/Business/Interfaces/WebModels/AI/AICostImportWebModels.cs     (rejectedFiles)
src/Business/Implementation/Services/AI/DocumentParserService.cs
src/Business/Interfaces/Services/IDocumentParserService.cs
src/Business.AIAgent/Services/IAICompletionService.cs
src/Business.AIAgent/Services/AzureAICompletionService.cs
src/CQRS/AI/ParseCostDocument/ParseCostDocumentQueryValidator.cs
src/CQRS/AI/ParseCostDocument/ParseCostDocumentQueryHandler.cs   (opcjonalnie — logika w parserze)
src/CQRS/AI/SubmitAICostImportBatch/SubmitAICostImportBatchCommandValidator.cs
src/CQRS/AI/SubmitAICostImportBatch/SubmitAICostImportBatchCommandHandler.cs
src/WebApi/Controllers/AICostController.cs
src/Business/Implementation/Services/AI/AICostImportWorker.cs
src/WebApi/Extensions/ServiceCollectionExtensions.cs
tests/Business.Tests/...  tests/CQRS.Tests/...  tests/WebApi.Tests/...
```

## Pytania przed refaktorem — ZAMKNIĘTE

Wszystkie decyzje zatwierdzone w feature spec (DPI 175, ImageSharp, soft-fail, 50 MB, max 20 stron).

## Ryzyka

- Docnet native PDFium na Linux Docker — zweryfikować runtime w Dockerfile
- Multi-image Vision: limity tokenów / payload przy 20 stronach @ 175 DPI
- Sync Docnet → owijać w Task.Run z CancellationToken
