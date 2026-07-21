# pdf-cost-upload-ui-fix-04 — CostForm accept + domknięcie testów

## Kontekst

Skills: ui-components, ui-unit-tests

## Cel

TrackedCost formularz: jawny `accept` dla załączników; domknięcie testów UI feature.

## Zadania

1. `CostForm.tsx` — na `<input type="file">`:
   `accept=".jpg,.jpeg,.png,.pdf"`
   Opcjonalnie filtr po stronie `handleFileChange` z toastem (soft-fail) — spójnie z dropzone.

2. Przejrzyj inne miejsca uploadu kosztów (nie cost-estimate item files) — ujednolić jeśli brakuje PDF w accept tylko w kontekście kosztów.

3. Uruchom / popraw:
   - `MultiDocumentDropzone.test.tsx`
   - ewentualne testy modalu
   - `npm run test:run` dla zmienionych plików (lub pełny suite jeśli szybki)

## Kryteria done

- [ ] CostForm accept ustawione
- [ ] Testy UI zielone dla zmienionych plików
