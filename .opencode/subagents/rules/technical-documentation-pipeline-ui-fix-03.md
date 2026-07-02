# ui-fix-03 — Widok szczegółów ProjectModel-first + AuditResult

## Cel i zakres

Refaktor `TechnicalDocumentationDetailsView` — priorytet nowego formatu §8.1, renderowanie `auditResult`, ukrycie sekcji per-drawing gdy puste, backward compat dla legacy Accordion.

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `TechnicalDocumentationDetailsView.tsx` | Refaktor branch legacy/new |
| `TechnicalDocumentationAuditResultSection.tsx` | **NOWY** |
| `ProjectTechnicalDocumentationDetailsPage.tsx` | Przekazanie `status` (opcjonalnie) |

## Wymagania techniczne

- Skills: `ui-components`, `ui-types`
- Nowy format (`!isLegacy`):
  1. `TechnicalDocumentationProjectModelSection` (główna treść)
  2. `TechnicalDocumentationMaterialScheduleSection`
  3. `TechnicalDocumentationAuditResultSection` — warnings, missingMaterials, assumptions, unitErrors
  4. Ukryj: `validatedDrawings`, `drawingDependencies`, `validationSummaries` gdy brak danych
- Legacy format: zachować obecny Accordion „Szczegóły projektu”
- Informacje o budynku: preferuj `projectModel.project` w nowym formacie; fallback `project.*` w legacy
- `auditResult` — **obowiązkowy render** gdy present (obecnie brak w UI — luka audytu)
- Jeden plik = jeden komponent; Chakra tokens; bez inline styles

## Kryteria akceptacji

- [ ] Nowy mock (ui-fix-05) renderuje ProjectModel + Audit bez legacy Accordion
- [ ] Legacy mock renderuje bez regresji
- [ ] `auditResult.warnings` widoczne jako lista Alert lub lista punktów
- [ ] `npm run test:axe` — DetailsView axe green dla obu fixture'ów
- [ ] `npm run build` OK

## Zależności

- Po: **ui-fix-01**, **ui-fix-02**
- Przed: **ui-fix-04** (rozszerzenia ProjectModelSection mogą być równolegle)
- Po: **api-fix-13** (realne dane z API)
