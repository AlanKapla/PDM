# ui-fix-05 — Testy i mocki (dual-format)

## Cel i zakres

Zaktualizować mocki i testy pod dual-format DetailsJson (legacy MVP + nowy group pipeline §8.1).

## Pliki do modyfikacji

| Plik | Akcja |
|------|-------|
| `mockTechnicalDocumentationDetails.ts` | Zachować legacy; dodać `mockGroupPipelineDetails` |
| `TechnicalDocumentationDetailsView.axe.test.tsx` | 2 case'y: legacy + new |
| `TechnicalDocumentationStatusBadge.axe.test.tsx` | CompletedWithWarnings (jeśli nie w ui-fix-01) |
| `technicalDocumentationFormatters.test.ts` | Rozszerzyć jeśli nowe formaty |

## Wymagania techniczne

- Skills: `ui-unit-tests`
- `mockGroupPipelineDetails`: pełny `projectModel` z slab, elevations, warnings, extractionMetadata + materialSchedule + auditResult
- `renderWithChakra` + `toHaveNoViolations`
- Bez testów trywialnych — pokrycie realnych ścieżek renderowania

## Kryteria akceptacji

- [ ] `npm run test:run` — wszystkie testy technicalDocumentation green
- [ ] `npm run test:axe` — DetailsView + StatusBadge green
- [ ] Mock new format zgodny z ui-fix-02 typami

## Zależności

- Po: **ui-fix-02**, **ui-fix-03**, **ui-fix-04**
- Ostatni krok UI w tej iteracji
