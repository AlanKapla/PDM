# api-fix-11 — Deprecacja starych agentów per-drawing

## Cel i zakres

Usunąć pliki legacy pipeline po stabilizacji `UseGroupPipeline=true` w produkcji. **Wykonać dopiero po zatwierdzeniu przez użytkownika i przełączeniu flagi domyślnej.**

## Pliki do usunięcia

| Plik | Powód |
|------|-------|
| `Pipeline/ImageExtractionPipelineAgent.cs` | Zastąpiony fazami 1–5 |
| `Pipeline/CrossReferencePipelineAgent.cs` | Consolidation |
| `Pipeline/RoomsPipelineAgent.cs` | Consolidation |
| `Pipeline/OpeningsPipelineAgent.cs` | Consolidation |
| `ArchitecturalExtractionAgentService.cs` | Per-drawing |
| `ExtractionAgentBService.cs` | Per-drawing |
| `UniversalExtractionAgentService.cs` | Per-drawing |
| `ExtractionFocusRouter.cs` | Per-drawing |
| `ExtractionFocusPromptLoader.cs` | Per-drawing |
| `MaterialDrawingGroupResolver.cs` | ThematicGroup |
| `MaterialDrawingGroupClassifier.cs` | 4 grupy |
| `TechnicalDocumentationSharedStatePropagator.cs` | Usunięty |
| `TechnicalDocumentationCrossReferenceLinker.cs` | Consolidation |
| Prompty: `universal_extraction_agent*.md`, `extraction_focus_prompts.md` | Per-drawing |

## Pliki do modyfikacji

| Plik | Akcja |
|------|-------|
| `LegacyTechnicalDocumentationPipelineRunner.cs` | Usunąć po deprecacji lub zostawić do migracji danych |
| `ServiceCollectionExtensions.cs` | Usunąć rejestracje legacy |
| `TechnicalDocumentationOptions.cs` | Usunąć `UseGroupPipeline` (opcjonalnie) |

## Wymagania techniczne

- Skills: `api-services`
- Usunąć `LegacyTechnicalDocumentationPipelineRunner` gdy flaga usunięta
- Upewnić się że żaden handler/test nie referencuje usuniętych typów

## Kryteria akceptacji

- [ ] `dotnet build` + `dotnet test` — full green
- [ ] Brak martwego kodu w grep dla usuniętych klas
- [ ] `UseGroupPipeline` domyślnie `true` w appsettings przed usunięciem legacy runner

## Zależności

- Po: **api-fix-10**, **api-fix-12**, **api-fix-14**, **ui-fix-01–05**
- **Ostatni krok** — nie wykonywać przed stabilizacją prod
