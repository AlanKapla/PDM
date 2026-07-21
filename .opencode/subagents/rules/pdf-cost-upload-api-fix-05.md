# pdf-cost-upload-api-fix-05 — Limit 50 MB ProjectCost + TrackedCost NewFiles + testy

## Kontekst

Feature: `.opencode/features/pdf-cost-upload.md`  
Skills: api-validators, api-unit-tests

## Cel

Wyrównać limit dokumentu ProjectCost do 50 MB; dodać walidację typów/rozmiaru dla TrackedCost `NewFiles`; domknąć testy regresji JPG/PNG/PDF.

## Zadania

1. `DocumentValidationHelper`:
   - `MaxDocumentSize = 50L * 1024 * 1024`
   - Zaktualizuj komentarze w `ProjectCostValidationExtensions.ApplyDocumentRules` (było „max 10MB”)

2. `TrackedCostCommandBaseValidator` (lub Create/Update validators):
   - Dla każdego `NewFiles`: extension+MIME jak helper (jpg/jpeg/png/pdf), max 50 MB per file
   - Komunikaty po angielsku (konwencja FluentValidation)

3. Testy:
   - Validator ProjectCost: plik 11 MB PDF teraz OK; plik >50 MB fail
   - TrackedCost: niedozwolony typ fail
   - Regresja: istniejące testy AICost (JPG) przechodzą
   - Testy soft-fail batch / magic bytes jeśli nie pokryte w 03

4. Upewnij się, że `dotnet build` i `dotnet test` (CQRS.Tests, Business.Tests, WebApi.Tests) przechodzą dla zmienionych obszarów.

## Poza zakresem

- UI (osobne prompty)
- Zmiana limitu single-parse 20 MB

## Kryteria done

- [ ] 50 MB w DocumentValidationHelper
- [ ] TrackedCost NewFiles walidowane
- [ ] Testy zielone
