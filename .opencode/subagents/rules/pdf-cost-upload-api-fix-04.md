# pdf-cost-upload-api-fix-04 — Integracja handler + worker (PDF errors)

## Kontekst

Zależności: api-fix-01, 02, 03.  
Skills: api-cqrs, api-services

## Cel

Pełna ścieżka AI: blob = oryginał; konwersja tylko przed AI; password/corrupt → ErrorNeedsReview z komunikatem PL bez psucia reszty batcha.

## Zadania

1. `ParseCostDocumentQueryHandler` — upewnij się, że bajty + ContentType trafiają do parsera; mapuj `PdfConversionException` na 400 BadRequest z komunikatem PL (Forbidden/Conflict nie — to błąd pliku). Preferuj wyjątek API już używany w projekcie dla walidacji pliku albo zwróć ProblemDetails przez middleware — sprawdź konwencję (np. rzucenie wyjątku łapanego jako 400).

2. `AICostImportWorker.ProcessItemAsync`:
   - Pobierz oryginał z blob (bez zmian)
   - `ParseAsync(fileBytes, item.ContentType, ct)` — parser sam konwertuje PDF
   - Catch `PdfConversionException` (lub równoważny): ustaw `LastError` = komunikat PL, `Status = ErrorNeedsReview`, inkrementuj ErrorCount batcha, **bez** retry dla password/corrupt/too many pages (retry nie pomoże)
   - Inne błędy: istniejący retry flow
   - **Nigdy** nie zapisuj skonwertowanych JPG do blob

3. Accept / MoveToCostAttachment — bez zmian (przenosi oryginał).

4. Testy worker: mock converter/parser rzucający PasswordProtected → ErrorNeedsReview, brak re-queue.

## Kryteria done

- [ ] Sync parse zwraca czytelny błąd dla złego PDF
- [ ] Worker: per-item fail, batch kontynuuje
- [ ] Blob tylko oryginał
- [ ] Build + testy OK
