# api-fix-10 — Runner 9 faz + DI + feature flag branch

## Cel i zakres

Przebudować `TechnicalDocumentationPipelineRunner` na 9 faz sekwencyjnych. `LegacyTechnicalDocumentationPipelineRunner` — obecna logika 6 faz. `TechnicalDocumentationOrchestratorService` — branch `UseGroupPipeline`.

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `TechnicalDocumentationPipelineRunner.cs` | 9 faz group pipeline |
| `LegacyTechnicalDocumentationPipelineRunner.cs` | **NOWY** |
| `TechnicalDocumentationOrchestratorService.cs` | Branch flag |
| `ITechnicalDocumentationPipelineAgent.cs` | Nowe nazwy agentów |
| `ServiceCollectionExtensions.cs` | Rejestracja wszystkich nowych agentów |
| `TechnicalDocumentationPipelineRunnerTests.cs` | Adaptacja |

## Wymagania techniczne

- Skills: `api-services`, `api-cqrs`
- Kolejność faz: 1 Ingestion → 2 Classification → 3 Grouping → 4 Extraction → 5 Verification → 6 Consolidation → 7 Calculation → 8 Audit → 9 Output
- `UseGroupPipeline=false` → `LegacyTechnicalDocumentationPipelineRunner` (bez zmiany zachowania MVP)
- `UseGroupPipeline=true` → nowy runner
- Opcjonalnie: DetailsValidation faza dev (`EnableTestValidation`) na końcu

## Kryteria akceptacji

- [ ] DI rejestruje wszystkie agenty z api-fix-03–09
- [ ] Flag=false: istniejące testy pipeline runner green
- [ ] Flag=true: runner wykonuje 9 faz (test integracyjny mock)
- [ ] `dotnet build --configuration Release` OK

## Zależności

- Po: **api-fix-03** przez **api-fix-09**
- Przed: **api-fix-12**, **api-fix-14**
