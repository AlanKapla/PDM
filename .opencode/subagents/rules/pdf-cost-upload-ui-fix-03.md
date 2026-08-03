# pdf-cost-upload-ui-fix-03 — AICostReviewItem podgląd PDF

## Kontekst

Audyt UI; skills: ui-components, ui-accessibility

## Cel

Podgląd oryginalnego PDF przez SAS URL (iframe lub otwarcie w nowej karcie), nie „Podgląd niedostępny”.

## Zadania

1. `AICostReviewItem.tsx`:
   - `isPdfPreview` = contentType `application/pdf` lub rozszerzenie `.pdf`
   - Dla PDF: `<iframe title=... src={previewUrl} />` (maxH, width 100%) **lub** wyraźny przycisk „Otwórz PDF” + opcjonalnie iframe
   - Przycisk pełnego rozmiaru także dla PDF (`window.open(previewUrl)`)
   - `aria-label` na kontrolkach
   - JPG/PNG: bez zmian (`<Image>`)

2. Nie pobieraj / nie konwertuj PDF po stronie klienta.

## Kryteria done

- [ ] PDF ma użyteczny podgląd / link
- [ ] Obrazy bez regresji
- [ ] AXE / a11y podstawowe OK
