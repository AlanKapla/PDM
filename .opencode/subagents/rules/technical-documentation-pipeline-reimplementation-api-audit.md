# API Audit — Reimplementacja pipeline ekstrakcji dokumentacji technicznej

**Feature:** technical-documentation-pipeline-reimplementation  
**Data audytu:** 2026-06-26  
**Branch:** AK/cost-estimate-refactor  
**Poprzedni audyt:** `technical-documentation-rag-api-audit.md` (2026-06-22 — greenfield MVP; **cały feature jest już zaimplementowany**)  
**Decyzje użytkownika:** patrz `.opencode/features/technical-documentation-pipeline-reimplementation-plan.md` § Decyzje zatwierdzone

---

## PODSUMOWANIE METRYK

| Priorytet | Liczba |
|-----------|--------|
| **Krytyczne** | 6 |
| **Wysokie** | 9 |
| **Normalne** | 8 |

| Kategoria | Wartość |
|-----------|---------|
| Pliki pipeline do refaktoru | ~45 |
| Pliki do usunięcia (po migracji) | ~15 |
| Pliki do zachowania bez zmian | ~25 |
| Nowe pliki (szacunek) | ~20 |
| Testy do adaptacji | 37 plików w `Business.Tests/Services/TechnicalDocumentation/` |
| Blokery techniczne | 2 (multi-image `IAICompletionService`, model JSON breaking change) |
| Wymaga migracji EF | **Tak** (`CompletedWithWarnings`) |

---

## BLOK 1 — Stan obecny (MVP zaimplementowany)

Feature dokumentacji technicznej jest **w pełni zaimplementowany** (odwrotnie niż raport RAG z 2026-06-22):

| Warstwa | Stan | Kluczowe pliki |
|---------|------|----------------|
| Encje + DB | ✅ | `ProjectTechnicalDocumentation`, `ProjectTechnicalDocumentationFile`, `TechnicalDocumentationStatus` |
| CQRS | ✅ | Create (202), List, Details, Count, Retry — 12 plików w `CQRS/TechnicalDocumentation/` |
| Worker + Queue | ✅ | `TechnicalDocumentationWorker`, `QueuedTechnicalDocumentationSender` |
| Blob + PDF | ✅ | kontener `technicaldocumentation`, `PdfToImageConverterService` (Docnet) |
| SignalR | ✅ | `TechnicalDocumentationHub`, `SignalRTechnicalDocumentationDispatcher` |
| Kontroler | ✅ | `TechnicalDocumentationController` |
| Pipeline AI | ✅ | 6 faz, per-drawing extraction |
| Testy | ✅ | 37+ plików jednostkowych |

### Obecna struktura pipeline (6 faz)

```
TechnicalDocumentationPipelineRunner
  Faza 1: ImageExtraction          (per rysunek, max 3 równolegle)
  Faza 2: CrossReference + Rooms + Openings  (równolegle, C#)
  Faza 3: MaterialsCalculation
  Faza 4: Report                     (audit + DetailsBuilder)
  Faza 5: DetailsValidation        (opcjonalnie, EnableTestValidation)
```

Rejestracja DI: `ServiceCollectionExtensions.cs` linie ~414–439.

### Konfiguracja (`TechnicalDocumentationOptions`)

Obecnie tylko:
- `EnableTestValidation` (bool)
- `EnableTestValidationAiReview` (bool)

**Brak:** `UseGroupPipeline`, mapowanie grup tematycznych, `MaxImagesPerGroup`, próg 3MB.

### Preprocessor obrazów

`TechnicalDocumentationImagePreprocessor`:
- Próg kompresji: **1 MB** (`RecompressThresholdBytes = 1_048_576`)
- Max wymiar: **2048 px**
- **Wymaga:** próg **3 MB** (decyzja zatwierdzona)

### `IAICompletionService` — single-image only

```csharp
// Business.AIAgent/Services/IAICompletionService.cs
CompleteWithImageAsync(systemPrompt, imageBytes, mediaType, ...)
CompleteWithImageAndTextAsync(systemPrompt, userText, imageBytes, mediaType, ...)
```

`AzureAICompletionService` buduje `UserChatMessage` z **jednym** `CreateImagePart`.  
**Brak** `CompleteWithImagesAsync` — **bloker** dla fazy 4 (ekstrakcja per grupa).

---

## BLOK 2 — Gap analysis: obecny vs docelowy 9-fazowy pipeline

| Faza docelowa | Obecny stan | Gap | Priorytet |
|---------------|-------------|-----|-----------|
| 1. Ingestion | Wbudowane w `ImageExtractionPipelineAgent.PrepareImagesForVisionAsync` | Wydzielić agent; próg 1MB→3MB | Wysoki |
| 2. Classification | W `ImageExtractionPipelineAgent.ClassifyAllImagesAsync` | Wydzielić agent; logika OK | Normalny |
| 3. Grouping | `MaterialDrawingGroupResolver` (4 grupy materiałowe) | **Nowy** `DrawingThematicGroupResolver` (7+1 grup, K-06 dual) | Krytyczny |
| 4. Extraction A/B per grupa | Per-drawing w `ImageExtractionPipelineAgent` | **Pełna reimplementacja** + multi-image | Krytyczny |
| 5. Verification | `ComparatorAgentService` per rysunek (C# merge) | `ExtractionDiffEngine` per grupa + Agent C zawsze | Krytyczny |
| 6. Consolidation | `RoomsPipelineAgent` (C#) + `CrossReference` + `Openings` | **Nowa faza LLM** text-only; usuń fazy 2 | Krytyczny |
| 7. Calculation | `MaterialsCalculationPipelineAgent` | Wejście z `ProjectModel` po Consolidation (nie z `Drawings[]`) | Wysoki |
| 8. Audit | Część `ReportPipelineAgent` | Wydzielić `AuditPipelineAgent` | Normalny |
| 9. Output | `ReportPipelineAgent` + `DetailsBuilder` | Nowy kontrakt JSON (ProjectModel §8.1) | Krytyczny |
| Feature flag | Brak | `UseGroupPipeline` + legacy runner | Wysoki |
| Status warnings | Tylko `Completed` | `CompletedWithWarnings` + EF migracja | Wysoki |

### Różnice architektoniczne kluczowe

| Aspekt | Obecne | Docelowe |
|--------|--------|----------|
| Jednostka ekstrakcji | 1 rysunek = 1–3 LLM calli | 1 grupa = 1–2 calli A/B (wszystkie obrazy grupy) |
| K-06 | Jeden rysunek, jeden focus | Ten sam obraz w 2 grupach: `reinforcement` (k06) + `foundations` (k06_foundations) |
| Max obrazów/call | 1 | 6; sub-batch + merge JSON w C# |
| CrossReference | Deterministyczny C# linker | Wchłonięty do Consolidation LLM |
| SharedState | `TechnicalDocumentationSharedStatePropagator` | Usunięty; dane w Consolidation |
| Agent C | Brak dedykowanego vision retry | Zawsze przy critical diff |
| Details JSON | `ProjectTechnicalDocumentationDetails` (legacy summaries) | `ProjectModel` §8.1 + MaterialSchedule + AuditResult |

---

## BLOK 3 — Inwentaryzacja plików

### 3.1 DO USUNIĘCIA (po `UseGroupPipeline=true` stabilizacji, api-fix-11)

| Plik | Powód |
|------|-------|
| `Pipeline/ImageExtractionPipelineAgent.cs` | Zastąpiony fazami 1–5 |
| `Pipeline/CrossReferencePipelineAgent.cs` | Wchłonięty do Consolidation |
| `Pipeline/RoomsPipelineAgent.cs` | Wchłonięty do Consolidation |
| `Pipeline/OpeningsPipelineAgent.cs` | Wchłonięty do Consolidation |
| `ArchitecturalExtractionAgentService.cs` | Per-drawing Agent A |
| `ExtractionAgentBService.cs` | Per-drawing Agent B |
| `UniversalExtractionAgentService.cs` | Per-drawing universal |
| `ExtractionFocusRouter.cs` | Per-drawing routing |
| `ExtractionFocusPromptLoader.cs` | Per-drawing prompty |
| `MaterialDrawingGroupResolver.cs` | Zastąpiony `DrawingThematicGroupResolver` |
| `MaterialDrawingGroupClassifier.cs` | 4 grupy materiałowe |
| `TechnicalDocumentationSharedStatePropagator.cs` | SharedState usunięty |
| `TechnicalDocumentationCrossReferenceLinker.cs` | CrossRef w Consolidation |
| `universal_extraction_agent.md`, `universal_extraction_agent_b.md` | Per-drawing prompty |
| `extraction_focus_prompts.md` | Per-drawing focus |

### 3.2 DO REFAKTORU

| Plik | Zmiana | api-fix |
|------|--------|---------|
| `TechnicalDocumentationPipelineRunner.cs` | 9 faz; `UseGroupPipeline` branch | 10 |
| `TechnicalDocumentationOrchestratorService.cs` | Wybór legacy vs group runner | 10 |
| `TechnicalDocumentationOptions.cs` | Nowe flagi i mapowanie | 01 |
| `TechnicalDocumentationImagePreprocessor.cs` | Próg 3MB | 03 |
| `TechnicalDocumentationAgentInvoker.cs` | `CompleteWithImagesAsync` | 02 |
| `AzureAICompletionService.cs` | Multi-image vision | 02 |
| `IAICompletionService.cs` | Nowa metoda | 02 |
| `TechnicalDocumentationProcessingService.cs` | `CompletedWithWarnings` logic | 14 |
| `ProjectModel.cs` + modele | Spec §8.1: slab, elevations, warnings, extractionMetadata | 13 |
| `ProjectTechnicalDocumentationDetails.cs` | Nowy kontrakt DetailsJson | 13 |
| `ProjectTechnicalDocumentationDetailsBuilder.cs` | Deprecate lub adapter | 13 |
| `TechnicalDocumentationDetailsSerializer.cs` | Nowy schemat serializacji | 13 |
| `MaterialCalculationAgentService.cs` | Wejście ProjectModel-only | 09 |
| `MaterialsCalculationPipelineAgent.cs` | Bez Drawings/SharedState | 09 |
| `ReportPipelineAgent.cs` | Podział na Output + Audit | 09, 10 |
| `ComparatorAgentService.cs` | Bazowy diff → `ExtractionDiffEngine` | 07 |
| `FloorPlanDrawingMerger.cs` | Logika merge do group-level | 07 |
| `ProjectModelFallbackBuilder.cs` | Fallback Consolidation | 08 |
| `DetailsValidationPipelineAgent.cs` | Dostosowanie do nowego modelu | 12 |
| `DetailsValidationAgentService.cs` | Nowy ground truth schema | 12 |
| `ServiceCollectionExtensions.cs` | DI nowych agentów | 10 |
| `TechnicalDocumentationStatus.cs` | `CompletedWithWarnings = 4` | 14 |
| `GetTechnicalDocumentationDetailsQueryHandler.cs` | Details dla CompletedWithWarnings | 14 |

### 3.3 DO ZACHOWANIA (z adaptacją minimalną)

| Plik | Uwagi |
|------|-------|
| `DrawingClassificationAgentService.cs` | Faza 2 — bez zmian logiki |
| `ObviousDrawingTypeDetector.cs` | Skip LLM dla oczywistych typów |
| `DrawingSheetNumberInferrer.cs` | Metadata |
| `TechnicalDocumentationDeterministicAuditor.cs` | Faza 8 |
| `AuditAgentService.cs` | Faza 8 |
| `MaterialScheduleBuilder.cs` | Faza 7 |
| `MaterialScheduleMerger.cs` | Faza 7 |
| `MaterialOrchestrationService.cs` | Faza 7 (opcjonalnie) |
| `TechnicalDocumentationJsonHelper.cs` | Serializacja |
| `AiGeneratedJsonSanitizer.cs` | Parsowanie LLM JSON |
| `FloorPlanDrawingJsonParser.cs` | Wewnętrzne typy pośrednie (opcjonalnie) |
| `DetailsSchemaReferenceLoader.cs` | Ground truth tests |
| `DetailsValidationDiffBuilder.cs` | Dev validation |
| `CompletionTokenUsageRecorder.cs` | Token tracking |
| `TransientAiCompletionRetry.cs` | Retry policy |
| `TechnicalDocumentationWorker.cs` | Bez zmian strukturalnych |
| `TechnicalDocumentationProcessingService.cs` | Tylko status logic (api-fix-14) |
| Wszystkie CQRS handlery (poza Details) | Bez zmian |
| `TechnicalDocumentationController.cs` | Bez zmian |
| `TechnicalDocumentationHub.cs` | Bez zmian (DTO rozszerzyć) |

### 3.4 DO UTWORZENIA (nowe)

| Plik | Faza | api-fix |
|------|------|---------|
| `Pipeline/IngestionPipelineAgent.cs` | 1 | 03 |
| `Pipeline/ClassificationPipelineAgent.cs` | 2 | 04 |
| `DrawingThematicGroupResolver.cs` | 3 | 05 |
| `ThematicDrawingGroup.cs` (model wewnętrzny) | 3 | 05 |
| `Pipeline/GroupExtractionPipelineAgent.cs` | 4 | 06 |
| `GroupExtractionAgentService.cs` (A/B) | 4 | 06 |
| `GroupExtractionJsonMerger.cs` | 4/5 | 06, 07 |
| `Pipeline/VerificationPipelineAgent.cs` | 5 | 07 |
| `ExtractionDiffEngine.cs` | 5 | 07 |
| `ExtractionVerificationAgentService.cs` (Agent C) | 5 | 07 |
| `Pipeline/ConsolidationPipelineAgent.cs` | 6 | 08 |
| `ConsolidationAgentService.cs` | 6 | 08 |
| `Pipeline/AuditPipelineAgent.cs` | 8 | 09 |
| `Pipeline/OutputPipelineAgent.cs` | 9 | 10 |
| `LegacyTechnicalDocumentationPipelineRunner.cs` | flag=false | 10 |
| Prompty: `group_extraction_*.md`, `consolidation_agent.md`, `extraction_verification_agent.md` | 4–8 | 06–08 |
| Schematy JSON per grupa: `k06.json`, `k06_foundations.json` | 4 | 06 |

---

## BLOK 4 — Wpływ zmiany modelu: `ProjectTechnicalDocumentationDetails` → `ProjectModel` §8.1

### Obecny kontrakt (`ProjectTechnicalDocumentationDetails`)

```csharp
// ProjectTechnicalDocumentationDetails.cs
[JsonIgnore] public ProjectModel? ProjectModel { get; set; }  // NIE serializowany do API!
public ProjectInfo Project { get; set; }
public List<RoomFloorGroup> Rooms { get; set; }
public RoofSummary? Roof { get; set; }
// ... 15+ pól summary legacy
public DetailsMaterialSchedule? MaterialSchedule { get; set; }
public AuditResult? AuditResult { get; set; }
```

**Problem:** `ProjectModel` ma `[JsonIgnore]` — UI dostaje zdenormalizowane summaries z `ProjectTechnicalDocumentationDetailsBuilder`, nie surowy model.

### Docelowy kontrakt (decyzja §8.1)

Główny zapis w `DetailsJson`:

| Pole | Źródło fazy |
|------|-------------|
| `projectModel.project` | Consolidation |
| `projectModel.site` | Consolidation (grupa `site`) |
| `projectModel.floors[]` | Consolidation (grupa `floor_plans`) |
| `projectModel.foundations` | Consolidation (grupa `foundations`) |
| `projectModel.slab` | Consolidation (grupa `reinforcement`) — **brak w PDM** |
| `projectModel.roof` | Consolidation (grupa `roof_structure`) |
| `projectModel.walls` | Consolidation (grupy `sections` + `floor_plans`) |
| `projectModel.elevations` | Consolidation (grupa `elevations`) — **brak w PDM** |
| `projectModel.warnings[]` | Verification + Audit |
| `projectModel.extractionMetadata{}` | Output (pipeline version, grupy, tokeny) |
| `materialSchedule` | Calculation (faza 7) |
| `auditResult` | Audit (faza 8) |

### Gap modelowy PDM vs spec

| Spec §8.1 | PDM `ProjectModel` | Akcja api-fix-13 |
|-----------|-------------------|------------------|
| `slab` | `Ceilings[]` | Dodać `Slab` lub mapować |
| `elevations` | brak | Nowy typ `ProjectModelElevation[]` |
| `warnings[]` | `Conflicts`, `MissingData` | Ujednolicić do `Warnings[]` |
| `extractionMetadata` | brak | Nowy typ |
| `Columns/Beams/Lintels` | istnieją | Rozszerzenie PDM — zachować lub przenieść |

### Pliki dotknięte migracją modelu

| Warstwa | Pliki |
|---------|-------|
| Business modele | `Models/ProjectModel.cs`, `ProjectTechnicalDocumentationDetails.cs` |
| Serializer | `TechnicalDocumentationDetailsSerializer.cs`, `TechnicalDocumentationJsonHelper.cs` |
| Builder | `ProjectTechnicalDocumentationDetailsBuilder.cs` (deprecate) |
| Output | `OutputPipelineAgent.cs`, `ReportPipelineAgent.cs` |
| Ground truth | `details_schema_reference.json`, `DetailsValidationAgentService.cs` |
| Testy | `ProjectModelSerializationTests.cs`, `TechnicalDocumentationDetailsAggregatorTests.cs` |
| UI (później) | `technicalDocumentation.types.ts`, `TechnicalDocumentationDetailsView.tsx` |

### Ryzyko breaking change

**Krytyczne:** Istniejące rekordy w DB mają stary format `DetailsJson`. Strategia:
1. Nowe przetwarzania (`UseGroupPipeline=true`) zapisują nowy format
2. Stare rekordy: deserializacja backward-compatible lub brak migracji danych (tylko nowe uploady)
3. UI: obsługa obu formatów krótkoterminowo lub wymuszenie re-process

---

## BLOK 5 — Wpływ `CompletedWithWarnings`

### Encja

```csharp
// Entities/Enums/TechnicalDocumentationStatus.cs — OBECNIE
Pending = 0, Processing = 1, Completed = 2, Failed = 3
// DODAĆ:
CompletedWithWarnings = 4
```

**Migracja EF wymagana:** `dotnet ef migrations add add-technical-documentation-completed-with-warnings`

### Logika ustawiania statusu

`TechnicalDocumentationProcessingService.cs` (linie 65–66):

```csharp
documentation.Status = TechnicalDocumentationStatus.Completed;  // ZAWSZE Completed dziś
```

**Nowa logika (api-fix-14):**
- `Failed` — wyjątek pipeline lub wszystkie grupy failed
- `CompletedWithWarnings` — pipeline OK, ale: `FailedPages` niepuste LUB `warnings[]` niepuste LUB critical diff nierozwiązany LUB audit warnings
- `Completed` — czysty sukces

### CQRS

`GetTechnicalDocumentationDetailsQueryHandler.cs` (linia 37):

```csharp
documentation.Status == TechnicalDocumentationStatus.Completed  // Details tylko dla Completed!
```

**Zmiana:** `Completed || CompletedWithWarnings` → deserializuj Details.

### SignalR

`TechnicalDocumentationProcessingResultDto` — enum rozszerzony automatycznie (shared `TechnicalDocumentationStatus`).

`useTechnicalDocumentationHub.ts` — toast tylko dla `Completed` i `Failed`; **dodać** `CompletedWithWarnings` (info toast, nie error).

### Web DTOs (bez zmian strukturalnych)

- `TechnicalDocumentationListItemWeb.Status`
- `TechnicalDocumentationDetailsWeb.Status`
- `TechnicalDocumentationCreatedWeb.Status`

Wszystkie używają `TechnicalDocumentationStatus` — wystarczy rozszerzyć enum.

### Retry

`RetryTechnicalDocumentationCommandValidator` — retry tylko dla `Failed`. `CompletedWithWarnings` **nie** wymaga retry (sukces z ostrzeżeniami).

---

## BLOK 6 — Blokery techniczne

### BLOKER 1: Multi-image w `IAICompletionService` (krytyczny)

**Stan:** Tylko single `CreateImagePart` per message.

**Wymagane:**
```csharp
Task<string> CompleteWithImagesAsync(
    string systemPrompt,
    string? userText,
    IReadOnlyList<(byte[] ImageBytes, string MediaType)> images,
    CancellationToken cancellationToken,
    int maxOutputTokens = 8192,
    float? temperature = null,
    bool jsonMode = false);
```

**Implementacja `AzureAICompletionService`:**
- `UserChatMessage` z wieloma `ChatMessageContentPart.CreateImagePart`
- Kolejność: text part → image parts (lub wg best practice OpenAI vision)
- Limit 6 obrazów enforced w `GroupExtractionPipelineAgent`, nie w serwisie

**Sub-batch merge (decyzja #9):**
- Gdy grupa > 6 obrazów: podziel na chunk'i po 6
- Każdy chunk: osobny call A i B
- `GroupExtractionJsonMerger.Merge(chunkResults)` w C# przed Verification
- Testy jednostkowe merge dla overlapping keys

### BLOKER 2: Breaking change DetailsJson (krytyczny)

Migracja z legacy summaries do ProjectModel §8.1 wymaga zsynchronizowania:
- Serializer
- Ground truth tests
- UI types

Mitygacja: api-fix-13 wcześnie w kolejności (przed fazami 6–9).

### RYZYKO WYSOKIE (nie bloker)

| Ryzyko | Opis |
|--------|------|
| Token limit vision | 6 × 3MB obrazów może przekroczyć kontekst — preprocessor + batch |
| K-06 koszt | 2× call'e (reinforcement + foundations) — akceptowane |
| Dwa pipeline | Utrzymanie legacy + group pod flagą — podwójny effort testów |
| Consolidation LLM | Jakość merge 7 grup — `ProjectModelFallbackBuilder` jako safety net |

---

## BLOK 7 — Lista konkretnych zmian per plik (api-fix mapping)

### api-fix-01 — `TechnicalDocumentationOptions`

| Plik | Zmiana |
|------|--------|
| `TechnicalDocumentationOptions.cs` | `UseGroupPipeline`, `MaxImagesPerGroup=6`, `CompressionThresholdBytes=3_145_728`, `Dictionary<string, string[]> DrawingTypeToThematicGroups` |
| `appsettings.Development.json` | `UseGroupPipeline: true` |
| `appsettings.json` | `UseGroupPipeline: false` |

### api-fix-02 — Multi-image AI

| Plik | Zmiana |
|------|--------|
| `IAICompletionService.cs` | `CompleteWithImagesAsync` |
| `AzureAICompletionService.cs` | Implementacja multi-part vision |
| `TechnicalDocumentationAgentInvoker.cs` | Wrapper + walidacja max images |

### api-fix-03 — Ingestion

| Plik | Zmiana |
|------|--------|
| `TechnicalDocumentationImagePreprocessor.cs` | `RecompressThresholdBytes = 3_145_728` |
| `Pipeline/IngestionPipelineAgent.cs` | **NOWY** — wydzielone z ImageExtraction |
| `TechnicalDocumentationImagePreprocessorTests.cs` | Aktualizacja progów |

### api-fix-04 — Classification

| Plik | Zmiana |
|------|--------|
| `Pipeline/ClassificationPipelineAgent.cs` | **NOWY** — `ClassifyAllImagesAsync` z ImageExtraction |
| `TechnicalDocumentationAgentContext` | `Classifications[]` property |

### api-fix-05 — Grouping

| Plik | Zmiana |
|------|--------|
| `DrawingThematicGroupResolver.cs` | **NOWY** |
| `ThematicDrawingGroup.cs` | **NOWY** model wewnętrzny |
| `DrawingThematicGroupResolverTests.cs` | **NOWY** — K-06 dual membership |

### api-fix-06 — Group Extraction

| Plik | Zmiana |
|------|--------|
| `Pipeline/GroupExtractionPipelineAgent.cs` | **NOWY** |
| `GroupExtractionAgentService.cs` | **NOWY** — A/B parallel per grupa |
| `GroupExtractionJsonMerger.cs` | **NOWY** — sub-batch merge |
| `Resources/.../group_extraction_agent_a.md` | **NOWY** per grupa |
| `Resources/.../group_extraction_agent_b.md` | **NOWY** per grupa |
| `Resources/.../schemas/k06.json` | **NOWY** |
| `Resources/.../schemas/k06_foundations.json` | **NOWY** |

### api-fix-07 — Verification

| Plik | Zmiana |
|------|--------|
| `ExtractionDiffEngine.cs` | **NOWY** — diff A vs B per grupa, critical field detection |
| `ExtractionVerificationAgentService.cs` | **NOWY** — Agent C vision, **zawsze** przy critical |
| `Pipeline/VerificationPipelineAgent.cs` | **NOWY** |
| `ComparatorAgentService.cs` | Refaktor lub delegacja do DiffEngine |

### api-fix-08 — Consolidation

| Plik | Zmiana |
|------|--------|
| `Pipeline/ConsolidationPipelineAgent.cs` | **NOWY** |
| `ConsolidationAgentService.cs` | **NOWY** — text-only LLM |
| `consolidation_agent.md` | **NOWY** prompt |
| `ProjectModelFallbackBuilder.cs` | Wywołanie gdy LLM fail |

### api-fix-09 — Calculation + Audit

| Plik | Zmiana |
|------|--------|
| `MaterialsCalculationPipelineAgent.cs` | Wejście: `context.ProjectModel` only |
| `MaterialCalculationAgentService.cs` | Usunąć zależność od `FloorPlanDrawing[]` jako primary |
| `Pipeline/AuditPipelineAgent.cs` | **NOWY** — wydzielone z Report |
| `AuditAgentService.cs` | Bez zmian logiki |

### api-fix-10 — Runner + DI

| Plik | Zmiana |
|------|--------|
| `TechnicalDocumentationPipelineRunner.cs` | 9 faz sekwencyjnych |
| `LegacyTechnicalDocumentationPipelineRunner.cs` | **NOWY** — obecna logika 6 faz |
| `TechnicalDocumentationOrchestratorService.cs` | Branch `UseGroupPipeline` |
| `ITechnicalDocumentationPipelineAgent.cs` | Nowe nazwy agentów |
| `ServiceCollectionExtensions.cs` | Rejestracja wszystkich nowych agentów |

### api-fix-11 — Deprecacja

Usunięcie plików z §3.1 po przełączeniu domyślnego `UseGroupPipeline=true` w prod.

### api-fix-12 — Testy

| Plik | Zmiana |
|------|--------|
| `TechnicalDocumentationPipelineRunnerTests.cs` | Nowa struktura faz |
| `MaterialDrawingGroupResolverTests.cs` | Zastąpić ThematicGroup tests |
| `ComparatorAgentServiceTests.cs` | → ExtractionDiffEngine tests |
| `details_schema_reference.json` | Nowy schema |
| Nowy: `GroupPipelineIntegrationTests.cs` | K-02 mass 1170.30 kg gate |

### api-fix-13 — Model §8.1

| Plik | Zmiana |
|------|--------|
| `Models/ProjectModel.cs` | slab, elevations, warnings, extractionMetadata |
| `ProjectTechnicalDocumentationDetails.cs` | Nowy root DTO lub rename |
| `TechnicalDocumentationDetailsSerializer.cs` | Nowy format |
| `ProjectTechnicalDocumentationDetailsBuilder.cs` | Deprecate / adapter |

### api-fix-14 — CompletedWithWarnings

| Plik | Zmiana |
|------|--------|
| `TechnicalDocumentationStatus.cs` | `CompletedWithWarnings = 4` |
| `Entities/Migrations/*` | Nowa migracja |
| `TechnicalDocumentationProcessingService.cs` | Logika wyboru statusu |
| `GetTechnicalDocumentationDetailsQueryHandler.cs` | Details dla warnings status |
| `TechnicalDocumentationProcessingResultDto.cs` | (auto via enum) |

---

## BLOK 8 — Prompty AI (Business.AIAgent)

### Obecne (per-drawing) — do zastąpienia

| Plik | Status |
|------|--------|
| `drawing_classification_agent.md` | **Zachować** |
| `universal_extraction_agent.md` | Usunąć |
| `universal_extraction_agent_b.md` | Usunąć |
| `extraction_focus_prompts.md` | Usunąć |
| `material_calculation_agent.md` | **Zachować** (faza 7) |
| `material_orchestration_agent.md` | **Zachować** |
| `details_validation_agent.md` | **Zachować** (dev) |
| `details_validation_vision_agent.md` | **Zachować** (dev) |

### Nowe (per-group)

| Plik | Grupa |
|------|-------|
| `group_extraction_reinforcement_a.md` | reinforcement |
| `group_extraction_foundations_a.md` | foundations (w tym k06_foundations) |
| `group_extraction_floor_plans_a.md` | floor_plans |
| ... (per 7+1 grup) | |
| `consolidation_agent.md` | faza 6 |
| `extraction_verification_agent.md` | Agent C |

---

## BLOK 9 — Pytania otwarte (do audytu UI / implementacji)

1. **Backward compatibility DetailsJson** — czy UI ma czytać stary format dla istniejących rekordów, czy wymusimy re-upload?
2. **Definicja „critical diff”** — proponowane pola: `totalMassKg`, `totalVolumeM3`, `areaM2`, `concreteClass`, `reinforcement` — potwierdzić lista w api-fix-07.
3. **`Columns/Beams/Lintels`** w ProjectModel PDM — zachować jako rozszerzenie ponad spec §8.1, czy usunąć?
4. **Ground truth** — czy zaktualizować `details_schema_reference.json` przed czy po pierwszym działającym group pipeline?

---

## ZAŁĄCZNIK — Porównanie z audytem RAG (2026-06-22)

| Obszar | RAG audit (czerwiec) | Stan dziś |
|--------|---------------------|-----------|
| Encje | Brak | ✅ Zaimplementowane |
| CQRS | Brak | ✅ 5 operacji |
| Worker | Brak | ✅ Działa |
| Docnet | Brak pakietu | ✅ Zainstalowany |
| Pipeline | 5 agentów planowanych | ✅ 6 faz MVP + 37 testów |
| Multi-image | Nie dotyczyło | ❌ **Bloker** |
| Group extraction | Nie dotyczyło | ❌ Do zrobienia |

**Wniosek:** Reimplementacja to **refactor warstwy agentowej + model JSON + status**, nie greenfield. Infrastruktura async (queue, blob, SignalR, CQRS) pozostaje bez zmian.
