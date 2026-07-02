# Plan wdrożenia: Reimplementacja pipeline ekstrakcji dokumentacji technicznej

**Data:** 2026-06-26  
**Typ:** Reimplementacja istniejącego feature (nie greenfield)  
**Status:** Zatwierdzony — decyzje poniżej; audyt API w toku

---

## Decyzje zatwierdzone (2026-06-26)

| # | Temat | Decyzja |
|---|---|---|
| 1 | Plan ogólny | **TAK** — reimplementacja 9-fazowego pipeline grupowego |
| 2 | Excel export | **PÓŹNIEJ** — poza scope tej iteracji (brak api-fix-13 Excel, brak ui-fix-01) |
| 3 | Model API | Zewnętrzny **ProjectModel** (spec użytkownika §8.1) jako główny kontrakt zapisu w `DetailsJson`; `MaterialSchedule` (faza 7) i `AuditResult` (faza 8) jako część outputu. Rozszerzyć/zastąpić obecny `ProjectTechnicalDocumentationDetails` |
| 4 | Strategia migracji | Feature flag **`UseGroupPipeline`** w `TechnicalDocumentationOptions` — domyślnie `false` (prod), `true` (dev); po stabilizacji usunąć stary pipeline |
| 5 | Agent C | **ZAWSZE** przy critical diff — bez osobnej flagi `EnableAgentC` |
| 6 | K-06 dual grouping | **Dwa osobne call'e per grupa**: obraz K-06 w `reinforcement` (schemat `k06`) i w `foundations` (schemat `k06_foundations`); osobne prompty/schematy, bez jednego calla z dual focus |
| 7 | DetailsValidation | **TAK** — opcjonalna faza dev (`EnableTestValidation`) |
| 8 | Status | Nowy enum **`CompletedWithWarnings`** + migracja EF |
| 9 | Multi-image limit | Max **6 obrazów per grupa** w jednym call; przy przekroczeniu: sub-batch'e w grupie + merge JSON w C# przed Verification. Preprocessor: próg kompresji **3MB** |
| 10 | CrossReference / SharedState | **Wchłonięte do Consolidation** (LLM text-only). Usunąć deterministyczne fazy CrossReference / Rooms / Openings |

---

## Typ zmiany

**Full-stack (głównie API/Business)** — reimplementacja pipeline agentów i modelu wyjściowego; UI minimalne (głównie nowy status `CompletedWithWarnings`). Excel export poza scope.

---

## Opis

Użytkownik prosi o reimplementację pipeline ekstrakcji danych z rysunków budowlanych (JPG/PDF) zgodnie z docelową architekturą 9 faz: Ingestion → Classification → Grouping → Extraction A/B (per grupa, wszystkie obrazy) → Verification → Consolidation → Calculation → Audit → Output.

Obecna implementacja PDM ma działający MVP (CQRS, worker, SignalR, UI) z pipeline 6 faz opartym o **ekstrakcję per rysunek** (Classification → Router → A+B+Comparator lub Universal w `ImageExtractionPipelineAgent`), a następnie agregację C# (CrossReference, Rooms, Openings, Materials, Report).

Reimplementacja polega na **przebudowie warstwy agentowej** na model **grup tematycznych** (7+1) z cross-validation A/B na poziomie grupy, deterministyczną weryfikacją (DiffEngine) + opcjonalnym Agentem C, oraz fazami downstream bez obrazów (Consolidation, Calculation, Audit). Integracja z PDM (encje, CQRS, worker, `IAICompletionService`) pozostaje.

---

## Mapowanie: spec użytkownika → architektura PDM

| Faza docelowa (użytkownik) | Odpowiednik PDM (obecny / proponowany) | Uwagi |
|---|---|---|
| 1. Ingestion (walidacja, kompresja >3MB, base64) | `TechnicalDocumentationProcessingService.BuildImageInputsAsync` + `TechnicalDocumentationImagePreprocessor` | PDF→JPG już w ProcessingService (Docnet). Preprocessor: SkiaSharp, próg **1MB / 2048px** — wymaga dostosowania do **3MB** |
| 2. Classification (1× GPT-4o per rysunek) | `DrawingClassificationAgentService` + `ObviousDrawingTypeDetector` w `ImageExtractionPipelineAgent` | Logika istnieje; wydzielić do osobnej fazy pipeline |
| 3. Grouping (7 grup + other, K-06 → 2 grupy) | `MaterialDrawingGroupResolver` (4 grupy: Foundations/Walls/Ceilings/Roof) | **Nowy** `DrawingThematicGroupResolver` — inna taksonomia i reguły multi-group |
| 4. Extraction A+B (1 call per grupa, wszystkie obrazy, równolegle) | `ImageExtractionPipelineAgent` — **per rysunek**, max 3 równolegle | **Reimplementacja** — wymaga multi-image w `IAICompletionService` |
| 5. Verification (DiffEngine A vs B, Agent C dla krytycznych) | `ComparatorAgentService` — deterministyczny merge **per rysunek**, bez vision LLM | Rozszerzyć do `ExtractionDiffEngine` + `ExtractionVerificationAgent` (Agent C) |
| 6. Consolidation (merge grup → ProjectModel, text-only GPT) | `RoomsPipelineAgent` + `CrossReferencePipelineAgent` + `OpeningsPipelineAgent` (do usunięcia) | **Nowa faza** LLM text-only; wchłania CrossReference/SharedState; `ProjectModelFallbackBuilder` jako safety net |
| 7. Calculation (MaterialSchedule + narzuty) | `MaterialsCalculationPipelineAgent` + `MaterialCalculationAgentService` | Refaktor wejścia: z `ProjectModel` po Consolidation, nie z per-drawing `FloorPlanDrawing[]` |
| 8. Audit (spójność, text-only) | `AuditAgentService` + `TechnicalDocumentationDeterministicAuditor` w `ReportPipelineAgent` | Wydzielić do osobnej fazy |
| 9. Output (JSON, REST) | `ProjectTechnicalDocumentation.DetailsJson` + `TechnicalDocumentationController` | JSON ✅ (ProjectModel + MaterialSchedule + Audit); Excel **poza scope**; REST ✅ |
| Background worker + kolejka | `TechnicalDocumentationWorker` + Azure Queue | Bez zmian |
| SignalR | `TechnicalDocumentationHub` | Bez zmian |
| UI | `ProjectTechnicalDocumentationPage`, `TechnicalDocumentationDetailsView` | Bez zmian strukturalnych (jeśli model API stabilny) |

### Mapowanie grup docelowych → drawingType PDM

| Grupa docelowa | drawingType (z `drawing_classification_agent.md`) | Uwagi |
|---|---|---|
| `reinforcement` | `zbrojenie_stropu_dolne`, `zbrojenie_stropu_gorne` | K-02, K-03 |
| `roof_structure` | `rzut_dachu`, `rzut_wiezby_dachowej`, `aksonometria_wiezby` | K-04, A-04 |
| `floor_plans` | `rzut_parteru`, `rzut_piętra`, `rzut_poddasza`, `rzut_piwnicy` | A-02, A-03 |
| `sections` | `przekroj` | A-05 |
| `elevations` | `elewacja` | A-07…A-10 |
| `foundations` | `rzut_fundamentow` | K-01 |
| `site` | `zagospodarowanie_terenu` | A-01 |
| `other` | `detale_konstrukcyjne`, `opis_techniczny`, `nieznany` | K-06 → **foundations + reinforcement** (dual membership) |

Konfiguracja mapowania: rozszerzyć `TechnicalDocumentationOptions` (obecnie tylko `EnableTestValidation`).

---

## Co zachowujemy bez zmian

- **Encje i DB:** `ProjectTechnicalDocumentation`, `ProjectTechnicalDocumentationFile` (rozszerzenie enum o `CompletedWithWarnings` — migracja EF)
- **CQRS:** create (202), list, details, count, retry — handlery i walidatory
- **Infrastruktura async:** Azure Storage Queue, `TechnicalDocumentationWorker`, `TechnicalDocumentationProcessingService` (szkielet: load → images → orchestrator → save → SignalR)
- **Blob storage:** kontener `technicaldocumentation`, upload/download plików źródłowych
- **SignalR:** hub + dispatcher + toast w UI
- **PDF→JPG:** `IPdfToImageConverterService` (Docnet.Core) w `BuildImageInputsAsync`
- **Uprawnienia:** `PROJECT.TECHNICAL_DOCUMENTATION`, moduł w `ProjectModule`
- **Kontroler:** `TechnicalDocumentationController` (nie Minimal API)
- **Integracja AI:** `IAICompletionService`, `TransientAiCompletionRetry`, `TechnicalDocumentationAgentInvoker`
- **Helpery JSON:** `AiGeneratedJsonSanitizer`, `TechnicalDocumentationJsonHelper`, `TechnicalDocumentationDetailsSerializer`
- **Infrastruktura UI:** kafelek, lista, upload, szczegóły (Accordion) — adaptacja pod nowy kontrakt JSON i status
- **Ground truth:** `details_schema_reference.json` — aktualizacja po migracji modelu (gate regresji)
- **Ground truth:** `details_schema_reference.json` + testy porównawcze (K-02 mass 1170.30 kg)
- **Testy jednostkowe:** 37+ plików w `Business.Tests/Services/TechnicalDocumentation/` — część do adaptacji, nie usuwania

---

## Co reimplementujemy

| Obszar | Obecne | Docelowe |
|---|---|---|
| **Struktura pipeline** | 6 faz, ekstrakcja per rysunek w fazie 1 | 9 faz, ekstrakcja per grupa tematyczna |
| **Grouping** | 4 grupy materiałowe (`MaterialDrawingGroupKind`) | 7+1 grup tematycznych, multi-group (K-06) |
| **Extraction** | A+B+Comparator lub Universal per rysunek | A+B równolegle per grupa, wszystkie obrazy grupy w jednym call |
| **Verification** | `FloorPlanDrawingMerger` per rysunek | DiffEngine na wynikach grup + Agent C (vision) dla krytycznych rozbieżności |
| **Consolidation** | Rozproszona agregacja C# (CrossRef, Rooms, Openings) | Centralna faza text-only GPT → `ProjectModel` |
| **CrossReference / SharedState** | Osobna faza pipeline (`CrossReferencePipelineAgent`) | **Usunięte** — wchłonięte do Consolidation (LLM text-only) |
| **Rooms / Openings** | Osobne fazy C# | **Usunięte** — scalone w Consolidation |
| **Prompty** | `universal_extraction_agent*.md`, focus per drawingType | Nowe prompty per grupa tematyczna (multi-image) |
| **IAICompletionService** | Single-image only | Rozszerzenie o `CompleteWithImagesAsync` (multi-image vision) |
| **ImagePreprocessor** | Próg 1MB, max 2048px | Dostosowanie do spec (>3MB kompresja) |
| **DetailsValidation** | Osobna faza testowa (ground truth diff) | Zachować jako opcjonalny krok dev (`EnableTestValidation`) |
| **Excel export** | Brak | **Poza scope** tej iteracji |
| **Feature flag** | Brak | `UseGroupPipeline` — równoległy rollout nowego pipeline |
| **Status CompletedWithWarnings** | Tylko `Completed` / `Failed` | Nowy enum + logika w `ProcessingService` |
| **Stare agenty per-drawing** | `ImageExtractionPipelineAgent`, `ExtractionFocusRouter`, `UniversalExtractionAgent` | Deprecate po migracji |

---

## Warstwy do zmiany

- [ ] **Business (serwisy AI)** — główna reimplementacja pipeline, grouping, verification, consolidation
- [ ] **Business.AIAgent** — nowe/rozszerzone prompty `.md`, ewentualnie rozszerzenie `IAICompletionService`
- [x] **Business (web modele)** — migracja do zewnętrznego **ProjectModel** (§8.1) jako główny kontrakt `DetailsJson` + `MaterialSchedule` + `AuditResult`
- [ ] **Business (konfiguracja)** — `TechnicalDocumentationOptions`: `UseGroupPipeline`, mapowanie grup, próg 3MB, `MaxImagesPerGroup=6`
- [ ] **CQRS** — obsługa `CompletedWithWarnings` w `GetTechnicalDocumentationDetails` (deserializacja Details)
- [ ] **Encje / migracje DB** — **`CompletedWithWarnings`** w `TechnicalDocumentationStatus` + migracja EF
- [ ] **UI** — minimalne: badge/status `CompletedWithWarnings`, typy TS, hub toast; adaptacja sekcji Details pod nowy ProjectModel
- [ ] **Testy** — adaptacja istniejących + nowe testy integracyjne ground truth

---

## Proponowany nowy pipeline (dostosowany do PDM)

```
TechnicalDocumentationProcessingService
    │  PDF→JPG, blob download
    ▼
TechnicalDocumentationOrchestratorService
    │  tokenUsageRecorder.Reset()
    │  branch: options.UseGroupPipeline ? GroupPipelineRunner : LegacyPipelineRunner
    ▼
TechnicalDocumentationPipelineRunner (NOWA struktura 9 faz — gdy UseGroupPipeline=true)
    │
    ├─► 1. IngestionPipelineAgent (C#)
    │       TechnicalDocumentationImagePreprocessor (SkiaSharp, próg 3MB)
    │       OUT: prepared TechnicalDocumentationImageInput[]
    │
    ├─► 2. ClassificationPipelineAgent (AI vision, równolegle per rysunek)
    │       DrawingClassificationAgentService + ObviousDrawingTypeDetector
    │       OUT: DrawingClassification[] per image
    │
    ├─► 3. GroupingPipelineAgent (C#)
    │       DrawingThematicGroupResolver (config z TechnicalDocumentationOptions)
    │       OUT: ThematicDrawingGroup[] (multi-membership dla K-06)
    │
    ├─► 4. GroupExtractionPipelineAgent (AI vision, równolegle per grupa)
    │       Per grupa: ExtractionAgentA + ExtractionAgentB (parallel)
    │       1 LLM call per agent per grupa, WSZYSTKIE obrazy grupy
    │       OUT: GroupExtractionResult[] (raw JSON per grupa, A i B)
    │
    ├─► 5. VerificationPipelineAgent (C# + AI vision)
    │       ExtractionDiffEngine.Compare(A, B) per grupa
    │       Agent C (vision) **zawsze** dla critical discrepancies
    │       Sub-batch merge JSON w C# przed diff (gdy >6 obrazów)
    │       OUT: VerifiedGroupExtraction[] + Conflicts[]
    │
    ├─► 6. ConsolidationPipelineAgent (AI text-only)
    │       Merge 7 grup → ProjectModel (bez obrazów)
    │       Wchłania dawny CrossReference/SharedState/Rooms/Openings
    │       Fallback C#: ProjectModelFallbackBuilder
    │       OUT: ProjectModel (spec §8.1)
    │
    ├─► 7. CalculationPipelineAgent (C# + opcjonalnie AI text)
    │       MaterialScheduleBuilder + narzuty (+5%/+10%/+15%)
    │       OUT: MaterialSchedule
    │
    ├─► 8. AuditPipelineAgent (C# + opcjonalnie AI text)
    │       TechnicalDocumentationDeterministicAuditor + AuditAgentService
    │       OUT: AuditResult
    │
    └─► 9. OutputPipelineAgent (C#)
            Serializacja DetailsJson: ProjectModel + MaterialSchedule + AuditResult + extractionMetadata
            Status: Completed | CompletedWithWarnings (gdy warnings w pipeline)
            OUT: zapis do DB + SignalR

    [opcjonalnie, dev only — EnableTestValidation]
    └─► DetailsValidationPipelineAgent
```

### Klasy / serwisy do utworzenia lub zastąpienia

| Nowy / zmieniony | Zastępuje / bazuje na |
|---|---|
| `IngestionPipelineAgent` | część `ImageExtractionPipelineAgent.PrepareImagesForVisionAsync` |
| `ClassificationPipelineAgent` | część `ImageExtractionPipelineAgent.ClassifyAllImagesAsync` |
| `DrawingThematicGroupResolver` | `MaterialDrawingGroupResolver` (inna taksonomia) |
| `GroupExtractionPipelineAgent` | `ImageExtractionPipelineAgent` (per-drawing) |
| `GroupExtractionAgentService` (A/B) | `ArchitecturalExtractionAgentService`, `ExtractionAgentBService`, `UniversalExtractionAgentService` |
| `ExtractionDiffEngine` | `ComparatorAgentService` + `FloorPlanDrawingMerger` |
| `ExtractionVerificationAgentService` (Agent C) | nowy |
| `ConsolidationPipelineAgent` | `CrossReferencePipelineAgent` + `RoomsPipelineAgent` + część agregacji |
| `CalculationPipelineAgent` | `MaterialsCalculationPipelineAgent` (refaktor wejścia) |
| `AuditPipelineAgent` | część `ReportPipelineAgent` |
| `OutputPipelineAgent` | część `ReportPipelineAgent` + `ProjectTechnicalDocumentationDetailsBuilder` |
| `IAICompletionService.CompleteWithImagesAsync` | nowa metoda multi-image |

---

## Wpływ na model danych

### Decyzja: zewnętrzny **ProjectModel** (spec §8.1) jako główny kontrakt `DetailsJson`

Docelowy schemat zapisu w `DetailsJson`:

```json
{
  "projectModel": { /* project, site, floors[], foundations, slab, roof, walls, elevations, warnings[], extractionMetadata{} */ },
  "materialSchedule": { /* faza 7 */ },
  "auditResult": { /* faza 8 */ },
  "tokenUsage": 0,
  "processedAt": "..."
}
```

### Gap: obecny PDM `ProjectModel` vs spec §8.1

| Pole spec §8.1 | Obecny PDM `ProjectModel` | Akcja |
|---|---|---|
| `project` | `Project` (ProjectModelMetadata) | Mapowanie / rename JSON |
| `site` | `Site` | OK |
| `floors[]` | `Floors[]` | OK |
| `foundations` | `Foundations` | OK |
| `slab` | `Ceilings[]` (częściowo) | **Nowe** `Slab` lub rename/mapowanie |
| `roof` | `Roof` | OK |
| `walls` | `Walls` | OK |
| `elevations` | **brak** | **Nowe** `Elevations[]` |
| `warnings[]` | `Conflicts` + `MissingData` | **Nowe** `Warnings[]` (ujednolicenie) |
| `extractionMetadata{}` | **brak** | **Nowe** — pipeline version, grupy, tokeny per faza |
| — | `Columns`, `Beams`, `Lintels` | Zachować jako rozszerzenie PDM lub mapować do grup |

### Legacy `ProjectTechnicalDocumentationDetails`

Obecny model z polami `Rooms`, `RoofSummary`, `ValidatedDrawings` itd. zostanie **zastąpiony** lub ograniczony do warstwy kompatybilności UI (builder opcjonalny tylko podczas przejścia). Po stabilizacji UI czyta bezpośrednio `projectModel` z DetailsJson.

### Zmiany modelowe

| Zmiana | Priorytet | Uwaga |
|---|---|---|
| Nowy `ProjectModel` wg spec §8.1 | **krytyczne** | api-fix-13 |
| Typy pośrednie: `ThematicDrawingGroup`, `GroupExtractionResult`, `VerifiedGroupExtraction` | wewnętrzne | Nie serializować do DB |
| `Details.VerificationConflicts` | normalne | Opcjonalnie w extractionMetadata |
| Ground truth `details_schema_reference.json` | **krytyczne** | Aktualizacja po migracji modelu |

### K-02 mass 1170.30 kg

Kryterium akceptacji: po reimplementacji testy oparte o `details_schema_reference.json` muszą przechodzić (lub świadomie zaktualizować ground truth z uzasadnieniem).

---

## Wpływ na UI

| Obszar | Wpływ | Działanie |
|---|---|---|
| Lista, upload, SignalR | **Niski** | Nowy status `CompletedWithWarnings` w badge + hub toast |
| Szczegóły — sekcje legacy | **Średni** | Adaptacja pod nowy `projectModel` w DetailsJson (sekcje mogą wymagać refaktoru) |
| Eksport Excel | **Brak** | Poza scope |
| Typy TypeScript | **Średni** | Sync `ProjectModelWeb`, `TechnicalDocumentationStatus`, Details shape |

**Rekomendacja:** UI w pierwszej iteracji — **minimalne zmiany** (status + typy); pełna adaptacja sekcji szczegółów po api-fix-13.

---

## Plan kroków (wysokopoziomowy)

1. **Audyt API** — szczegółowa inwentaryzacja zależności starego pipeline (pliki, testy, prompty) i mapowanie na nowe fazy
2. **Rozszerzenie infrastruktury AI** — `IAICompletionService.CompleteWithImagesAsync`, `TechnicalDocumentationOptions` (grupy, progi)
3. **Fazy 1–3** — Ingestion, Classification (wydzielenie), Grouping (nowy resolver + testy K-06 dual)
4. **Faza 4–5** — Group Extraction A/B + Verification (DiffEngine + Agent C) + nowe prompty
5. **Faza 6** — Consolidation (text-only GPT → ProjectModel) + fallback C#
6. **Faza 7–9** — Refaktor Calculation, Audit, Output; integracja z `DetailsBuilder`
7. **Pipeline runner** — nowa kolejność faz, usunięcie/deprecacja starych agentów
8. **Testy regresji** — ground truth, istniejące testy jednostkowe, testy integracyjne pipeline
9. **Testy regresji** — ground truth, adaptacja 37+ testów jednostkowych
10. **Audyt UI** — status `CompletedWithWarnings` + nowy kontrakt Details

*(Excel export — osobny feature, poza tą iteracją)*

---

## Proponowane prompty implementacyjne

### API

| ID | Zakres |
|---|---|
| **api-fix-01** | `TechnicalDocumentationOptions`: `UseGroupPipeline`, mapowanie `drawingType` → grupy tematyczne, próg kompresji 3MB, `MaxImagesPerGroup=6` |
| **api-fix-02** | `IAICompletionService.CompleteWithImagesAsync` + implementacja w `AzureAICompletionService` + `TechnicalDocumentationAgentInvoker` |
| **api-fix-03** | `IngestionPipelineAgent` + `TechnicalDocumentationImagePreprocessor` (próg 3MB, SkiaSharp) |
| **api-fix-04** | `ClassificationPipelineAgent` — wydzielenie z `ImageExtractionPipelineAgent` |
| **api-fix-05** | `DrawingThematicGroupResolver` + testy (K-06 → `foundations` + `reinforcement`, dual membership) |
| **api-fix-06** | `GroupExtractionAgentService` (A/B) + prompty/schematy per grupa (`k06`, `k06_foundations` dla K-06); sub-batch przy >6 obrazów |
| **api-fix-07** | `ExtractionDiffEngine` + `ExtractionVerificationAgentService` (Agent C **zawsze** przy critical diff) + merge JSON sub-batch |
| **api-fix-08** | `ConsolidationPipelineAgent` + prompt `consolidation_agent.md` (text-only); zastępuje CrossRef/Rooms/Openings |
| **api-fix-09** | Refaktor `CalculationPipelineAgent` i `AuditPipelineAgent` — wejście z `ProjectModel` po Consolidation |
| **api-fix-10** | `OutputPipelineAgent` + `TechnicalDocumentationPipelineRunner` (9 faz) + `UseGroupPipeline` branching + DI |
| **api-fix-11** | Deprecacja: `ImageExtractionPipelineAgent`, `CrossReferencePipelineAgent`, `RoomsPipelineAgent`, `OpeningsPipelineAgent`, starych extraction services |
| **api-fix-12** | Adaptacja testów + test integracyjny ground truth (K-02 1170.30 kg) |
| **api-fix-13** | Migracja modelu: zewnętrzny **ProjectModel** (§8.1) jako główny kontrakt `DetailsJson` + `TechnicalDocumentationDetailsSerializer` |
| **api-fix-14** | `CompletedWithWarnings`: enum, migracja EF, `ProcessingService`, CQRS details handler, SignalR DTO |

### UI

| ID | Zakres |
|---|---|
| **ui-fix-01** | Status `CompletedWithWarnings` — badge, typy TS, hub toast, details page (traktuj jak Completed dla wyświetlania Details) |
| **ui-fix-02** | *(opcjonalnie, po api-fix-13)* Adaptacja sekcji szczegółów pod nowy `ProjectModel` |

### Kolejność wykonania

```
api-fix-01 → api-fix-02 → api-fix-13 (model — wcześnie dla kontraktu)
    → api-fix-03..05 → api-fix-06 → api-fix-07 → api-fix-08 → api-fix-09
    → api-fix-10 → api-fix-14 → api-fix-11 → api-fix-12
    → ui-fix-01 → [ui-fix-02]
```

api-fix-06..08 są największym ryzykiem. api-fix-10 integruje dopiero gdy fazy 1–9 działają izolowanie. Stary pipeline działa pod `UseGroupPipeline=false` do czasu merge.

---

## Ryzyka i trade-offy

| Ryzyko | Wpływ | Mitygacja |
|---|---|---|
| **Koszt tokenów** — multi-image per grupa vs per rysunek | Wysoki | Grupowanie redukuje liczbę calli, ale zwiększa rozmiar promptu; benchmark na zestawie A-01…K-06 |
| **Limit kontekstu vision** — wiele dużych obrazów w jednym call | Wysoki | Preprocessor 3MB, batchowanie grup, priorytetyzacja obrazów |
| **Regresja ground truth** | Wysoki | Testy na `details_schema_reference.json` jako gate przed merge |
| **37+ testów jednostkowych** do adaptacji | Średni | Stopniowa migracja; zachować testy C# helpers (merger, normalizer, classifier) |
| **Brak multi-image w `IAICompletionService`** | Bloker | api-fix-02 jako pierwszy krok techniczny |
| **Breaking UI** | Średni | Nowy kontrakt DetailsJson — ui-fix-01/02 po api-fix-13/14 |
| **Dwa pipeline równolegle** | Średni | `UseGroupPipeline` — utrzymanie obu ścieżek do stabilizacji |
| **Złożoność Consolidation** | Średni | `ProjectModelFallbackBuilder` jako safety net gdy LLM zawiedzie |
| **Czas implementacji** | Wysoki | Podział na 12+ promptów api-fix; fazy 4–8 jako core |

### Trade-off: per-drawing vs per-group extraction

| Aspekt | Per-drawing (obecne) | Per-group (docelowe) |
|---|---|---|
| Kontekst między arkuszami | Via CrossReference + SharedState | Natywnie w jednym call (np. A-02 + A-05 w `floor_plans`/`sections`) |
| Cross-validation | A+B per rysunek | A+B per grupa |
| Koszt API | N × calli | G × calli (G << N typowo) |
| Debugging | Łatwiejszy per arkusz | Trudniejszy — wymaga logowania per grupa |
| Zgodność z zasadą „obraz przy pytaniach ekstrakcyjnych” | ✅ | ✅ |

---

## Następne kroki

1. ✅ Plan zatwierdzony
2. ✅ Audyt API — raport: `.opencode/subagents/rules/technical-documentation-pipeline-reimplementation-api-audit.md`
3. Audyt UI (po zatwierdzeniu użytkownika)
4. Generowanie plików `technical-documentation-pipeline-api-fix-*.md`
5. Implementacja przez `@api-refactor-agent`
