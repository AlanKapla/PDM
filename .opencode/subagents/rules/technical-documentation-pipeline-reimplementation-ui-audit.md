# UI Audit — Reimplementacja pipeline ekstrakcji dokumentacji technicznej

**Feature:** technical-documentation-pipeline-reimplementation  
**Data audytu:** 2026-06-26  
**Poprzedni audyt:** `technical-documentation-rag-ui-audit.md` (2026-06-22 — greenfield MVP; **UI jest w pełni zaimplementowane**)  
**Audyt API:** `technical-documentation-pipeline-reimplementation-api-audit.md`  
**Decyzje użytkownika:** `.opencode/features/technical-documentation-pipeline-reimplementation-plan.md` § Decyzje zatwierdzone

---

## PODSUMOWANIE METRYK

| Priorytet | Liczba |
|-----------|--------|
| **Krytyczne** | 4 |
| **Wysokie** | 7 |
| **Normalne** | 6 |

| Kategoria | Wartość |
|-----------|---------|
| Pliki UI do refaktoru | 8 |
| Pliki UI do zachowania (bez zmian lub minimalne) | 12 |
| Nowe komponenty UI (szacunek) | 2–3 |
| Pliki testów do adaptacji | 5 |
| Breaking change DetailsJson | **Tak** — wymaga ui-fix-02/03 |
| Excel export | **Poza scope** |

---

## BLOK 1 — Stan obecny UI (MVP zaimplementowany)

Odwrotnie niż raport RAG z 2026-06-22: moduł dokumentacji technicznej **istnieje w całości**.

| Warstwa UI | Stan | Kluczowe pliki |
|------------|------|----------------|
| Routing + breadcrumbs | ✅ | `AppRouter.tsx`, `Breadcrumbs.tsx` |
| Uprawnienia + kafelek | ✅ | `useProjectPermissions`, `ProjectDetails` |
| Strony | ✅ | `ProjectTechnicalDocumentationPage`, `ProjectTechnicalDocumentationDetailsPage` |
| API client | ✅ | `technicalDocumentationApi.ts` |
| React Query | ✅ | `useTechnicalDocumentation.ts` (list, detail, count, create, retry + polling 5s) |
| SignalR | ✅ | `technicalDocumentationHubService`, `useTechnicalDocumentationHub`, `TechnicalDocumentationToastBridge` |
| Upload | ✅ | `AddTechnicalDocumentationModal`, `MultiDocumentDropzone` |
| Widok szczegółów | ✅ | `TechnicalDocumentationDetailsView` + 4 sekcje podrzędne |
| Testy AXE | ✅ | StatusBadge, DetailsView, Modal, Page |
| Status enum | ⚠️ | 4 wartości — **brak** `CompletedWithWarnings` |
| Kontrakt Details | ⚠️ | Hybryda legacy summaries + opcjonalny `projectModel` |

### Architektura widoku szczegółów (obecna)

```
ProjectTechnicalDocumentationDetailsPage
  ├─ status === Pending|Processing → TechnicalDocumentationProcessingState
  ├─ status === Failed → Alert + retry
  ├─ status === Completed && details → TechnicalDocumentationDetailsView
  └─ TechnicalDocumentationFileList (zawsze)

TechnicalDocumentationDetailsView
  ├─ Informacje o budynku (legacy project.*)
  ├─ TechnicalDocumentationProjectModelSection (jeśli projectModel)
  ├─ TechnicalDocumentationMaterialScheduleSection
  ├─ TechnicalDocumentationValidatedDrawingsSection
  ├─ TechnicalDocumentationDrawingDependenciesSection
  ├─ ValidationSummariesSection (per-drawing CV)
  ├─ Korekty
  └─ Szczegóły projektu (legacy: rooms, roof, walls, … Accordion)
```

**Luka krytyczna:** `auditResult` jest w typach TS i mocku, ale **nigdzie nie jest renderowany**.

---

## BLOK 2 — Wpływ `CompletedWithWarnings`

### Obecny enum (`technicalDocumentation.types.ts`)

```typescript
Pending: 0, Processing: 1, Completed: 2, Failed: 3
// BRAK: CompletedWithWarnings: 4
```

### Pliki wymagające zmiany

| Plik | Problem | Priorytet | ui-fix |
|------|---------|-----------|--------|
| `technicalDocumentation.types.ts` | Brak wartości enum `4` | Krytyczny | 01 |
| `TechnicalDocumentationStatusBadge.tsx` | `STATUS_CONFIG` Record — brak wpisu; runtime `undefined` przy statusie 4 | Krytyczny | 01 |
| `ProjectTechnicalDocumentationDetailsPage.tsx` | `isCompleted` tylko `=== Completed` — **Details nie wyświetlą się** przy `CompletedWithWarnings` | Krytyczny | 01 |
| `useTechnicalDocumentationHub.ts` | Toast tylko `Completed` / `Failed` — brak info toast dla warnings | Wysoki | 01 |
| `useTechnicalDocumentation.ts` | Polling tylko Pending/Processing — OK (terminal status) | — | — |
| `ProjectTechnicalDocumentationPage.tsx` | Badge przez komponent — OK po ui-fix-01 | — | — |
| `technicalDocumentationHubService.ts` | Bez zmian (enum z API) | — | — |
| `TechnicalDocumentationToastBridge.tsx` | Bez zmian (deleguje do hooka) | — | — |
| `technicalDocumentationApi.ts` | Bez zmian | — | — |

### Rekomendacja UX `CompletedWithWarnings`

| Element | Propozycja |
|---------|------------|
| Badge | `orange.800` / `orange.100`, etykieta: „Ukończono z ostrzeżeniami” |
| Strona szczegółów | Alert `status="warning"` nad widokiem Details: „Przetwarzanie zakończone z ostrzeżeniami” |
| Toast SignalR | `showInfo` (nie error): „Dokumentacja gotowa z ostrzeżeniami” |
| Retry | **Brak** — sukces z ostrzeżeniami (zgodnie z API audit) |
| Spinner | Nie — status terminalny |

### Testy do adaptacji (ui-fix-01)

| Plik | Zmiana |
|------|--------|
| `TechnicalDocumentationStatusBadge.axe.test.tsx` | Nowy case `CompletedWithWarnings` |
| `ProjectTechnicalDocumentationPage.axe.test.tsx` | Opcjonalnie wiersz ze statusem 4 w mocku |

---

## BLOK 3 — Wpływ zmiany kontraktu `DetailsJson`

### Obecny kontrakt UI (`ProjectTechnicalDocumentationDetailsWeb`)

Root zawiera **mieszankę**:
- `projectModel?` — częściowy mirror wewnętrznego `ProjectModel` PDM
- Legacy summaries: `project`, `rooms[]`, `roof`, `walls`, `floors`, `foundations`, … (wymagane: `project`, `rooms`, `installations`)
- Artefakty per-drawing pipeline: `validatedDrawings`, `drawingDependencies`, `validationSummaries`
- `materialSchedule?`, `auditResult?` (audit **nie renderowany**)
- Dev-only: `validationReview?`, `corrections?`, `tokenUsage?`, `processedAt?`

### Docelowy kontrakt (decyzja §8.1 + api-fix-13)

```json
{
  "projectModel": {
    "project": {},
    "site": {},
    "floors": [],
    "foundations": {},
    "slab": {},
    "roof": {},
    "walls": {},
    "elevations": [],
    "warnings": [],
    "extractionMetadata": {},
    "columns": [],
    "beams": [],
    "lintels": []
  },
  "materialSchedule": {},
  "auditResult": {}
}
```

Legacy summaries (`rooms`, `roof`, `joinery`, …) **nie będą zapisywane** przez nowy group pipeline (`UseGroupPipeline=true`).

### Gap typów TS (`ProjectModelWeb` vs spec §8.1)

| Pole spec §8.1 | `ProjectModelWeb` (UI) | Akcja ui-fix-02 |
|----------------|------------------------|-----------------|
| `slab` | `ceilings[]` (inna semantyka) | Dodać `ProjectModelSlabWeb`; zachować `ceilings` jako deprecated/alias |
| `elevations` | brak | Dodać `ProjectModelElevationWeb[]` |
| `warnings[]` | `conflicts[]` + `missingData[]` | Dodać `ProjectModelWarningWeb[]`; mapować legacy przy backward compat |
| `extractionMetadata` | brak | Dodać `ProjectModelExtractionMetadataWeb` |
| `columns/beams/lintels` | istnieją | Zachować (rozszerzenie PDM) |

### Gap root DTO

| Zmiana | Opis |
|--------|------|
| Nowy root | `TechnicalDocumentationDetailsPayloadWeb` z polami: `projectModel`, `materialSchedule`, `auditResult` |
| Deprecate | Pola legacy w `ProjectTechnicalDocumentationDetailsWeb` → opcjonalne |
| Backward compat | Helper `isLegacyDetailsFormat(details)` — jeśli `project` + `rooms` bez `projectModel`, użyj starego widoku |
| `installations` required → optional | Obecny typ wymaga `installations` — złamie się przy nowym JSON |

### Ryzyko breaking change

**Krytyczne:** Rekordy w DB ze starym `DetailsJson` nadal będą otwierane w UI.

Strategia (do potwierdzenia przy implementacji):
1. **Dual-format renderer** w `TechnicalDocumentationDetailsView` — wykrywanie formatu
2. Nowe przetwarzania (`UseGroupPipeline=true`) → tylko nowy format
3. Stare rekordy → legacy sekcje Accordion (bez usuwania kodu do api-fix-11)

---

## BLOK 4 — Komponenty: refaktor vs zachować

### DO ZACHOWANIA (bez zmian lub minimalne)

| Komponent | Uwagi |
|-----------|-------|
| `ProjectTechnicalDocumentationPage` | Lista — badge auto po ui-fix-01 |
| `AddTechnicalDocumentationModal` | Upload flow bez zmian |
| `TechnicalDocumentationFileList` | Preview plików źródłowych |
| `TechnicalDocumentationProcessingState` | Pending/Processing |
| `technicalDocumentationApi.ts` | Endpointy bez zmian |
| `technicalDocumentationHubService.ts` | Hub bez zmian |
| `TechnicalDocumentationToastBridge.tsx` | Bridge bez zmian |
| `technicalDocumentationFormatters.ts` | Reuse |
| `MultiDocumentDropzone` | Poza scope pipeline |

### DO REFAKTORU

| Komponent | Zmiana | ui-fix |
|-----------|--------|--------|
| `TechnicalDocumentationStatusBadge` | `CompletedWithWarnings` | 01 |
| `ProjectTechnicalDocumentationDetailsPage` | Status terminalny + warning alert | 01, 03 |
| `useTechnicalDocumentationHub` | Toast warnings | 01 |
| `technicalDocumentation.types.ts` | Enum + nowy kontrakt §8.1 | 01, 02 |
| `TechnicalDocumentationDetailsView` | ProjectModel-first; audit; backward compat | 03 |
| `TechnicalDocumentationProjectModelSection` | slab, elevations, warnings, metadata | 04 |
| `TechnicalDocumentationMaterialScheduleSection` | Weryfikacja kształtu po api-fix-13 | 03 (minor) |
| `mockTechnicalDocumentationDetails.ts` | Nowy format mock + legacy fixture | 05 |

### DO UKRYCIA / WARUNKOWEGO RENDEROWANIA (group pipeline)

| Komponent | Powód |
|-----------|-------|
| `TechnicalDocumentationValidatedDrawingsSection` | Brak per-drawing validation w group pipeline |
| `TechnicalDocumentationDrawingDependenciesSection` | CrossRef wchłonięty do Consolidation |
| `ValidationSummariesSection` (inline w DetailsView) | CV per rysunek → zastąpione przez `projectModel.warnings` + `auditResult` |
| Legacy Accordion „Szczegóły projektu” | Zastąpione przez `projectModel` — pokazywać tylko dla legacy format |

### DO UTWORZENIA

| Komponent | Opis | ui-fix |
|-----------|------|--------|
| `TechnicalDocumentationAuditResultSection` | Warnings, missingMaterials, assumptions, unitErrors | 03 |
| `TechnicalDocumentationExtractionMetadataSection` | Pipeline version, grupy, tokeny (opcjonalnie collapsible) | 04 |

---

## BLOK 5 — Hooki i SignalR

### `useTechnicalDocumentation.ts`

| Aspekt | Stan | Akcja |
|--------|------|-------|
| Query keys | OK | — |
| Polling listy (5s gdy Pending/Processing) | OK | — |
| Polling detail (5s gdy Pending/Processing) | OK | — |
| `hasActiveProcessing` | Nie uwzględnia CompletedWithWarnings | OK (terminal) |

### `useTechnicalDocumentationHub.ts`

```typescript
// OBECNIE — brak gałęzi dla CompletedWithWarnings
if (event.status === TechnicalDocumentationStatus.Completed) { showSuccess(...) }
if (event.status === TechnicalDocumentationStatus.Failed) { showError(...) }
// DODAĆ:
if (event.status === TechnicalDocumentationStatus.CompletedWithWarnings) {
  showInfo('Przetwarzanie zakończone z ostrzeżeniami', ...);
}
```

Wzorzec: `useToastNotification().showInfo` (już używany w DetailsPage przy retry).

---

## BLOK 6 — Testy UI

| Plik testu | Stan | Adaptacja |
|------------|------|-----------|
| `TechnicalDocumentationStatusBadge.axe.test.tsx` | 2 case'y (Completed, Processing) | + CompletedWithWarnings |
| `TechnicalDocumentationDetailsView.axe.test.tsx` | 1 mock legacy | + mock nowy format §8.1 |
| `AddTechnicalDocumentationModal.axe.test.tsx` | OK | — |
| `ProjectTechnicalDocumentationPage.axe.test.tsx` | OK | Opcjonalnie status 4 |
| `technicalDocumentationFormatters.test.ts` | OK | — |
| `mockTechnicalDocumentationDetails.ts` | Legacy format | + `mockGroupPipelineDetails` |

Brak testów jednostkowych dla:
- `useTechnicalDocumentationHub` (opcjonalnie — nie wymagane w tej iteracji)
- `ProjectTechnicalDocumentationDetailsPage` (tylko axe na liście)

---

## BLOK 7 — Mapowanie ui-fix

### ui-fix-01 — `CompletedWithWarnings`

| Plik | Zmiana |
|------|--------|
| `technicalDocumentation.types.ts` | `CompletedWithWarnings: 4` |
| `TechnicalDocumentationStatusBadge.tsx` | Config orange + label |
| `ProjectTechnicalDocumentationDetailsPage.tsx` | `isTerminalSuccess` = Completed \|\| CompletedWithWarnings; Alert warning |
| `useTechnicalDocumentationHub.ts` | `showInfo` toast |
| `TechnicalDocumentationStatusBadge.axe.test.tsx` | Nowy test |

### ui-fix-02 — Typy nowego kontraktu Details

| Plik | Zmiana |
|------|--------|
| `technicalDocumentation.types.ts` | `ProjectModelSlabWeb`, `ProjectModelElevationWeb`, `ProjectModelWarningWeb`, `ProjectModelExtractionMetadataWeb`; root payload; legacy pola optional |
| Helper type guard | `isLegacyTechnicalDocumentationDetails(details)` |

### ui-fix-03 — Widok szczegółów (ProjectModel-first)

| Plik | Zmiana |
|------|--------|
| `TechnicalDocumentationDetailsView.tsx` | Branch legacy vs new; audit section; ukryj puste sekcje per-drawing |
| `TechnicalDocumentationAuditResultSection.tsx` | **NOWY** |
| `ProjectTechnicalDocumentationDetailsPage.tsx` | Przekaż status do DetailsView (opcjonalnie warning banner) |

### ui-fix-04 — Rozszerzenie ProjectModelSection

| Plik | Zmiana |
|------|--------|
| `TechnicalDocumentationProjectModelSection.tsx` | Panele: slab, elevations, warnings, extractionMetadata |
| `TechnicalDocumentationExtractionMetadataSection.tsx` | **NOWY** (lub inline) |

### ui-fix-05 — Testy i mocki

| Plik | Zmiana |
|------|--------|
| `mockTechnicalDocumentationDetails.ts` | Dwa fixture'y |
| `TechnicalDocumentationDetailsView.axe.test.tsx` | Oba formaty |
| `TechnicalDocumentationStatusBadge.axe.test.tsx` | CompletedWithWarnings |

---

## BLOK 8 — Pytania otwarte

1. **Backward compatibility** — czy dual-format renderer w UI jest zatwierdzony (rekomendacja: **tak**), czy wymuszamy re-process starych rekordów?
2. **Legacy sekcje** — kiedy usunąć kod Accordion legacy (po api-fix-11 gdy `UseGroupPipeline=true` w prod)?
3. **`validationReview`** (dev DetailsValidation) — czy pokazywać w UI tylko w Development build, czy ukryć całkowicie?
4. **Elewacje / stolarka** — w nowym modelu joinery może być w `elevations` lub `walls` — czy wystarczy tabela elevations bez dedykowanej sekcji joinery?

---

## PODSUMOWANIE — Priorytetyzacja

| Priorytet | Liczba | Kluczowe elementy |
|-----------|--------|-------------------|
| **Krytyczne** | 4 | Enum status, badge crash, Details niewidoczne przy warnings, breaking DetailsJson |
| **Wysokie** | 7 | Hub toast, auditResult UI, typy §8.1, legacy compat, deprecate per-drawing sections |
| **Normalne** | 6 | extractionMetadata panel, testy rozszerzone, dev validationReview |

### Wniosek

Reimplementacja UI to **adaptacja istniejącego MVP**, nie greenfield. Zakres:
- **Mały** dla `CompletedWithWarnings` (ui-fix-01, ~5 plików)
- **Średni/duży** dla nowego kontraktu Details (ui-fix-02–05, ~8 plików + 2 nowe komponenty)

Excel export — **poza scope** (brak zmian UI).

---

*Audyt przeprowadzony bez modyfikacji kodu produkcyjnego. Stan UI: feature MVP ~100% — wymaga adaptacji pod nowy status i kontrakt JSON.*
