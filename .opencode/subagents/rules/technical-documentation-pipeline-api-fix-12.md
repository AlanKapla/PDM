# api-fix-12 — Adaptacja testów pipeline + ground truth

## Cel i zakres

Zaktualizować testy jednostkowe i integracyjne pod nową strukturę pipeline. Zaktualizować `details_schema_reference.json`. Dodać `GroupPipelineIntegrationTests` z gate K-02 (1170.30 kg).

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `TechnicalDocumentationPipelineRunnerTests.cs` | Nowa struktura faz |
| `MaterialDrawingGroupResolverTests.cs` | Zastąpić → `DrawingThematicGroupResolverTests` |
| `ComparatorAgentServiceTests.cs` | → `ExtractionDiffEngineTests` |
| `details_schema_reference.json` | Nowy schema §8.1 |
| `GroupPipelineIntegrationTests.cs` | **NOWY** |
| `ProjectModelSerializationTests.cs` | Nowy format |
| `TechnicalDocumentationDetailsAggregatorTests.cs` | Deprecate lub adapt |

## Wymagania techniczne

- Skills: `api-unit-tests`
- xUnit + FluentAssertions + Moq
- Ground truth gate: K-02 mass **1170.30 kg** w MaterialSchedule
- Testy legacy runner z `UseGroupPipeline=false` zachowane do api-fix-11

## Kryteria akceptacji

- [ ] `dotnet test --configuration Release` — wszystkie testy TechnicalDocumentation green
- [ ] `details_schema_reference.json` zgodny z api-fix-13
- [ ] Integration test group pipeline (mock AI lub recorded responses)

## Zależności

- Po: **api-fix-10**, **api-fix-13**, **api-fix-14**
- Przed: **api-fix-11**
