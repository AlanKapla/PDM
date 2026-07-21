# pdf-cost-upload-api-fix-02 — Multi-image Vision + DocumentParserService

## Kontekst

Feature: `.opencode/features/pdf-cost-upload.md`  
Zależność: **api-fix-01** (IPdfToImageConverter) musi być zaimplementowany.  
Skills: api-services

## Cel

Vision API przyjmuje wiele obrazów (strony PDF). Parser rozpoznaje PDF vs JPG/PNG.

## Zadania

1. Rozszerz `IAICompletionService`:
```csharp
Task<string> CompleteWithImagesAsync(
    string systemPrompt,
    IReadOnlyList<(byte[] ImageBytes, string MediaType)> images,
    CancellationToken cancellationToken);
```

2. Zaimplementuj w `AzureAICompletionService`:
   - Jeden `UserChatMessage` z wieloma `ChatMessageContentPart.CreateImagePart(...)` w kolejności stron
   - MediaType dla skonwertowanych stron: `image/jpeg`
   - Zachowaj istniejące `CompleteWithImageAsync` (może delegować do multi z 1 elementem)

3. Zaktualizuj `DocumentParserService.ParseAsync`:
   - Jeśli `mediaType` to `application/pdf` LUB magic bytes wskazują PDF:
     - Wywołaj `IPdfToImageConverter.ConvertAllPagesToJpegAsync`
     - Wywołaj `CompleteWithImagesAsync` z listą JPG
   - W przeciwnym razie: istniejąca ścieżka single-image (JPG/PNG)
   - Wyjątki konwersji PDF: nie łapać jako confidence=0 — propaguj do callera (handler/worker mapuje komunikat)

4. Zaktualizuj komentarz w `IDocumentParserService` — zgodny z rzeczywistością.

5. Testy: mock `IAICompletionService` + mock `IPdfToImageConverter` w Business.Tests / CQRS.Tests dla parsera.

## Poza zakresem

- Walidacja HTTP / soft-fail batch (03)
- Worker wiring szczegółowy (04) — parser ma być gotowy

## Kryteria done

- [ ] Multi-image API działa
- [ ] PDF path używa converter + multi Vision
- [ ] JPG/PNG path bez regresji
- [ ] Build OK
