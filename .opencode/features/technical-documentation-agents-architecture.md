# Architektura agentów — odczyt rysunków budowlanych

Sekwencyjny pipeline 6 agentów. **ImageExtractionAgent** wewnętrznie używa cross-validation (Classification → Router → A+B+Comparator lub Universal). Dane propagowane przez `SharedState` między arkuszami.

## Legenda

| Symbol | Znaczenie |
|--------|-----------|
| 🔵 | agent AI (vision) — wewnątrz ImageExtraction |
| 🟢 (teal) | cross-validation (vision) — wewnątrz ImageExtraction |
| 🟣 | agent AI (text only) |
| 🟠 | router / kod C# |
| 🟡 | SharedState (propagowane wartości) |
| 🟢 | wynik końcowy |

---

## WEJŚCIE — rysunki projektu (JPG/PDF→JPG)

- 📄 A-01 Zagospodarowanie
- 📄 A-02 Rzut parteru
- 📄 A-03 Rzut poddasza
- 📄 A-04 Rzut dachu
- 📄 A-05 Przekrój A-A
- 📄 K-01 Fundamenty
- 📄 K-02 Zbrojenie dolne
- 📄 K-03 Zbrojenie górne
- 📄 K-04 Więźba dachowa
- 📄 K-06 Detale konstr.
- 📄 A-07…A-10 Elewacje

---

## PIPELINE — sekwencyjny (`TechnicalDocumentationPipelineRunner`)

```
ORCHESTRATOR (PipelineRunner)
    │
    ├─► 1. ImageExtractionAgent     — odczyt każdego rysunku
    ├─► 2. CrossReferenceAgent      — wiązanie zależności + SharedState
    ├─► 3. RoomsAgent               — pokoje + m² z rzutów
    ├─► 4. OpeningsAgent            — okna i drzwi
    ├─► 5. MaterialsCalculationAgent — zapotrzebowanie materiałów
    └─► 6. ReportAgent              — raport końcowy + audit
```

Kontekst: `TechnicalDocumentationAgentContext` (Drawings, Dependencies, Details, SharedState, PartialResults, ProjectModel).

---

### 1. ImageExtractionAgent

**Typ:** kod C# orkiestrujący sub-agentów AI · równolegle max 3 rysunki

Wewnętrzny przepływ per rysunek (bez zmian względem poprzedniej architektury):

#### ClassificationAgent (sub-prompt)

Zbiera **WSZYSTKIE** informacje tekstowe z rysunku — 6 źródeł: tabliczka, tabele, bloki opisowe, etykiety, legenda, uwagi.

**Wynik:** `DrawingClassification` → `classificationContext`

#### ExtractionFocusRouter (kod C#)

Wybiera focus A/B, decyduje o CV, wstrzykuje classificationContext.

#### Cross-validation (rysunki krytyczne)

| Agent | Opis |
|-------|------|
| **ExtractionAgent A** | vision + focusA + context |
| **ExtractionAgent B** | vision + focusB + context (odwrócona kolejność) |
| **ComparatorAgent** | porównanie A vs B na obrazie |

#### UniversalExtractionAgent (pozostałe rysunki)

Jeden przebieg z focusA + context.

| I/O | Opis |
|-----|------|
| IN | `TechnicalDocumentationImageInput[]` |
| OUT | `FloorPlanDrawing[]`, `PartialResults[]` |

---

### 2. CrossReferenceAgent

**Typ:** kod C# — `TechnicalDocumentationCrossReferenceLinker` + `SharedStatePropagator`

Łączy odesłania między arkuszami (A-02↔A-05, A-02↔K-01, K-02/K-03↔A-05, A-04↔K-04, K-01↔K-06).

Propaguje do `SharedState` (odczyt z rysunków, nie hardcodowane):

| Klucz | Źródło |
|-------|--------|
| `ceiling.thicknessCm` | Slabs / przekroje |
| `reinforcement.totalMassKg` | K-02, K-03 |
| `timber.totalVolumeM3` | K-04 |
| `roof.pitchDegrees` | A-04, A-05 |

| I/O | Opis |
|-----|------|
| IN | `FloorPlanDrawing[]` |
| OUT | `DrawingDependencyLink[]`, `SharedState` |

---

### 3. RoomsAgent

**Typ:** kod C# — `IAggregationAgent` + `ProjectModelFallbackBuilder`

Scala pomieszczenia z rzutów (A-02, A-03) w `ProjectModel.Floors[]`. Mapuje do `Details.Rooms` i `TotalAreaM2`.

| I/O | Opis |
|-----|------|
| IN | `FloorPlanDrawing[]`, `SharedState` |
| OUT | `ProjectModel`, `Details.Rooms` |

---

### 4. OpeningsAgent

**Typ:** kod C# — `TechnicalDocumentationDetailsAggregator`

Agreguje okna/drzwi z rzutów i elewacji do `Details.Joinery`.

| I/O | Opis |
|-----|------|
| IN | `FloorPlanDrawing[]` |
| OUT | `JoinerySummary` |

---

### 5. MaterialsCalculationAgent

**Typ:** kod C# — `IMaterialCalculationAgent` + `IMaterialOrchestrationService`

Oblicza zapotrzebowanie z `ProjectModel` + rysunki + dependencies + `buildingType`.

**Grupowanie rysunków** (`MaterialDrawingGroupResolver` + `MaterialDrawingGroupClassifier`) — uniwersalne, bez hardkodowanych numerów arkuszy:

| Grupa | Kryteria |
|-------|----------|
| Fundamenty | `drawingType` + dane ekstrakcji + zależności semantyczne |
| Stropy | zbrojenie stropu + przekroje kontekstowe |
| Dach | rzut dachu / więźby + dane `roof.*` |
| Ściany | rzuty kondygnacji + elewacje + przekroje |

Deterministyczna konsolidacja (`DrawingMaterialConsolidator` + `MaterialScheduleDrawingEnricher`) stanowi bazę;
wyniki LLM per grupa są nakładane (`MaterialScheduleMerger.Overlay`).

| sourceType | Opis |
|------------|------|
| **calculated** | beton, ściany, połać dachu |
| **read** | masa stali, objętość drewna z tabel |
| **estimated** | brak wymiaru → norma |

Narzuty: beton +5%, stal +10%, drewno +10%, pokrycia +15%, izolacja +10%

| I/O | Opis |
|-----|------|
| IN | `ProjectModel`, drawings, dependencies, SharedState |
| OUT | `MaterialSchedule` |

---

### 6. ReportAgent

**Typ:** kod C# + `IAuditAgent`

Finalizacja: legacy summaries (dach, izolacja, instalacje), audit, validation summaries, `ProcessedAt`, `TokenUsage`.

| I/O | Opis |
|-----|------|
| IN | pełny kontekst pipeline |
| OUT | `ProjectTechnicalDocumentationDetails` |

---

## WYJŚCIE — zapis do bazy

### ✅ `ProjectTechnicalDocumentation.Details` (JSON)

| Pole | Agent źródłowy |
|------|----------------|
| `validatedDrawings` | ImageExtraction |
| `drawingDependencies` | CrossReference |
| `projectModel`, `rooms` | Rooms |
| `joinery` | Openings |
| `materialSchedule` | MaterialsCalculation |
| `auditResult`, `validationSummaries`, `processedAt`, `tokenUsage` | Report |
| `roof`, `thermalInsulation`, `installations` | Report (legacy aggregator) |

↓

**SignalR → UI → status:** `Completed` / `CompletedWithWarnings` / `Failed`

---

## PODSUMOWANIE AGENTÓW PIPELINE

| # | Agent | Widzi obrazy | Wywołania | Odpowiada za |
|---|-------|:------------:|-----------|--------------|
| 1 | **ImageExtraction** | ✓ (sub-agenty) | 1× per rysunek | ekstrakcja + CV |
| 2 | **CrossReference** | ✗ kod C# | 1× per dokumentacja | zależności, SharedState |
| 3 | **Rooms** | ✗ kod C# | 1× per dokumentacja | pomieszczenia, ProjectModel |
| 4 | **Openings** | ✗ kod C# | 1× per dokumentacja | okna, drzwi |
| 5 | **MaterialsCalculation** | ✗ kod C# | 1× per dokumentacja | ilości materiałów |
| 6 | **Report** | ✗ kod C# + audit | 1× per dokumentacja | raport końcowy |

## SUB-AGENTY (prompty .md używane w runtime)

| Plik | Użycie |
|------|--------|
| `drawing_classification_agent.md` | `DrawingClassificationAgentService` — klasyfikacja + tabliczka |
| `universal_extraction_agent.md` | `UniversalExtractionAgentService`, `ArchitecturalExtractionAgentService` — ekstrakcja vision |
| `universal_extraction_agent_b.md` | `ExtractionAgentBService` — drugi przebieg CV |
| `extraction_focus_prompts.md` | `ExtractionFocusPromptLoader` — focus per `drawingType` |
| `material_calculation_agent.md` | `MaterialCalculationAgentService` — kalkulacja per grupa rysunków |
| `material_orchestration_agent.md` | `MaterialOrchestrationService` — audyt harmonogramu |

Agenty pipeline 2–6 (CrossReference, Rooms, Openings, Materials, Report) oraz `ComparatorAgent` / `AggregationAgent` / `AuditAgent` działają w **kodzie C#** — bez osobnych promptów `.md`.

## Pliki implementacji

```
Business/Implementation/Services/AI/
├── TechnicalDocumentationOrchestratorService.cs      ← deleguje do PipelineRunner
└── TechnicalDocumentation/Pipeline/
    ├── TechnicalDocumentationPipelineRunner.cs
    ├── ImageExtractionPipelineAgent.cs
    ├── CrossReferencePipelineAgent.cs
    ├── RoomsPipelineAgent.cs
    ├── OpeningsPipelineAgent.cs
    ├── MaterialsCalculationPipelineAgent.cs
    ├── ReportPipelineAgent.cs
    ├── TechnicalDocumentationSharedStatePropagator.cs
    └── TechnicalDocumentationPipelineHelpers.cs
```

Prompty agentów: `Business.AIAgent/Resources/Agents/sub_agents/technical_documentation/`
