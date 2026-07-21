# pdf-cost-upload-api-fix-03 — Walidacja MIME/magic + soft-fail batch

## Kontekst

Feature: `.opencode/features/pdf-cost-upload.md`  
Skills: api-validators, api-controllers, api-cqrs

## Cel

Akceptacja `.jpg/.jpeg/.png/.pdf` z walidacją rozszerzenia + MIME + magic bytes. Soft-fail w batch: `rejectedFiles[]`.

## Zadania

1. Helper (np. `FileContentValidator` w Business/Interfaces/Helpers):
   - Dozwolone: jpg/jpeg/png/pdf
   - MIME: image/jpeg, image/jpg, image/png, application/pdf
   - Magic: JPEG `FF D8 FF`, PNG `\x89PNG`, PDF `%PDF`
   - Metoda `Validate(IFormFile)` → Success | Failure(reason)

2. `AICostController.ParseDocumentInternal` — dozwól `.pdf` (+ istniejące); komunikat „Dozwolone formaty: JPG, PNG, PDF.”

3. `ParseCostDocumentQueryValidator` — AllowedExtensions + ContentType; opcjonalnie magic (jeśli da się czytać stream w validatorze — ostrożnie z pozycją streamu; magic może być w handlerze przed parse).

4. `SubmitAICostImportBatchCommandValidator`:
   - Nie używaj twardego `RuleForEach` odrzucającego całą paczkę dla złego typu
   - Zostaw: min 2 pliki, total size ≤ MaxBatchTotalBytes, TenantId/ProjectId
   - Walidacja typu per-file w **handlerze**

5. `SubmitAICostImportBatchCommandHandler`:
   - Dla każdego pliku: Validate → jeśli fail, dodaj do `rejectedFiles` (fileName + reason PL), **nie** twórz item/blob
   - Poprawne pliki: jak dotychczas (item + blob + queue)
   - Jeśli zero poprawnych: BadRequest / ConflictApiException z komunikatem
   - Response: rozszerz `AICostImportSubmitResultWeb` o:
```csharp
public IReadOnlyList<AICostImportRejectedFileWeb> RejectedFiles { get; init; }
// fileName, reason
```
   - `TotalFiles` = liczba zaakceptowanych (lub osobne AcceptedCount — spójnie udokumentuj)

6. Testy kontrolera / validatora / handlera.

## Poza zakresem

- Worker password handling (04)
- Limit ProjectCost 50 MB (05)

## Kryteria done

- [ ] PDF akceptowany w parse i batch
- [ ] Mixed batch: złe pliki w rejectedFiles, dobre przetwarzane
- [ ] Magic bytes sprawdzane
- [ ] Build + testy OK
