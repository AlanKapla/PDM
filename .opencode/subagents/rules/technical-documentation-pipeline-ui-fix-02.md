# ui-fix-02 — Typy TypeScript nowego kontraktu Details §8.1

## Cel i zakres

Zaktualizować `technicalDocumentation.types.ts` pod nowy root `DetailsJson`: `projectModel` (§8.1) + `materialSchedule` + `auditResult`. Dodać brakujące pola modelu. Legacy pola opcjonalne + type guard backward compat.

## Pliki do modyfikacji

| Plik | Akcja |
|------|-------|
| `src/types/technicalDocumentation.types.ts` | Rozszerzenie typów |
| `src/components/technicalDocumentation/technicalDocumentationDetailsGuards.ts` | **NOWY** — `isLegacyTechnicalDocumentationDetails()` |

## Wymagania techniczne

- Skills: `ui-types`
- Nowe interfejsy:
  - `ProjectModelSlabWeb`
  - `ProjectModelElevationWeb`
  - `ProjectModelWarningWeb` (`code?`, `message`, `severity?`, `sourceGroup?`)
  - `ProjectModelExtractionMetadataWeb` (`pipelineVersion`, `thematicGroups?`, `tokenUsage?`, `processedAt?`)
- Rozszerzyć `ProjectModelWeb`: `slab?`, `elevations?`, `warnings?`, `extractionMetadata?`
- `ProjectTechnicalDocumentationDetailsWeb`:
  - `project`, `rooms`, `installations` → **optional** (legacy)
  - `projectModel`, `materialSchedule`, `auditResult` — primary dla nowego formatu
- Type guard:
```typescript
export function isLegacyTechnicalDocumentationDetails(
  details: ProjectTechnicalDocumentationDetailsWeb
): boolean {
  return details.projectModel === undefined
    && (details.project !== undefined || details.rooms.length > 0);
}
```
- Bez `any`; sufiks `*Web`

## Kryteria akceptacji

- [ ] Typy kompilują się z istniejącym kodem (legacy mock nadal valid)
- [ ] Nowy mock shape (ui-fix-05) kompiluje się
- [ ] Type guard rozróżnia legacy vs new
- [ ] `npm run build` OK

## Zależności

- Po: **api-fix-13** (kontrakt API ustalony)
- Przed: **ui-fix-03**, **ui-fix-04**, **ui-fix-05**
