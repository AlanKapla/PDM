# pdf-cost-upload-ui-fix-01 — MultiDocumentDropzone + DocumentDropzone

## Kontekst

Feature: `.opencode/features/pdf-cost-upload.md`  
Audyt: `pdf-cost-upload-ui-audit.md`  
Skills: ui-components, ui-unit-tests, ui-accessibility

## Cel

Akceptacja `.jpg,.jpeg,.png,.pdf` z walidacją rozszerzenia + MIME; soft-fail (złe pliki nie blokują dobrych).

## Zadania

1. `MultiDocumentDropzone.tsx`:
   - `ACCEPTED_EXTENSIONS` += `.pdf`
   - Default `accept=".jpg,.jpeg,.png,.pdf"`
   - Walidacja: extension **oraz** MIME (`image/jpeg`, `image/png`, `application/pdf`; puste MIME — polegaj na extension + ostrzeżenie opcjonalne)
   - Soft-fail: callback `onFilesRejected?: (rejections: { fileName: string; reason: string }[]) => void` LUB toast przez prop; poprawne pliki i tak trafiają do `onChange`
   - Copy: „JPG, PNG, PDF · łącznie maks. {n} MB”

2. `DocumentDropzone.tsx` — te same rozszerzenia/copy (maxSizeMB bez zmian domyślnych).

3. Testy `MultiDocumentDropzone.test.tsx`:
   - **Odwróć** test `plikNieobrazkowy_jestFiltrowany` → PDF jest akceptowany
   - Dodaj: mieszanka JPG+PDF OK
   - Dodaj: plik `.exe` / złe MIME odrzucony, JPG zostaje

## Kryteria done

- [ ] PDF w dropzone
- [ ] Soft-fail działa
- [ ] Testy zaktualizowane i zielone
