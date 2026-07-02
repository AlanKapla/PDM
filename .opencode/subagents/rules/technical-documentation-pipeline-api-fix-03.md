# api-fix-03 — Faza 1: Ingestion + preprocessor 3MB

## Cel i zakres

Wydzielić fazę Ingestion z `ImageExtractionPipelineAgent`. Zmienić próg kompresji preprocessora z 1MB na 3MB.

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `TechnicalDocumentationImagePreprocessor.cs` | `RecompressThresholdBytes = 3_145_728` |
| `Pipeline/IngestionPipelineAgent.cs` | **NOWY** |
| `ITechnicalDocumentationPipelineAgent.cs` / context | Rozszerzenie context o prepared images |
| `TechnicalDocumentationImagePreprocessorTests.cs` | Aktualizacja progów |

## Wymagania techniczne

- Skills: `api-services`, `api-unit-tests`
- `IngestionPipelineAgent`: walidacja wejścia, wywołanie preprocessora, OUT: `TechnicalDocumentationImageInput[]`
- Logika wydzielona z `ImageExtractionPipelineAgent.PrepareImagesForVisionAsync`
- Handlery `sealed`, bez `var`

## Kryteria akceptacji

- [ ] Preprocessor używa 3MB threshold
- [ ] `IngestionPipelineAgent` zarejestrowany w DI (tymczasowo lub w api-fix-10)
- [ ] Testy preprocessora zaktualizowane
- [ ] Legacy pipeline (`UseGroupPipeline=false`) nadal działa

## Zależności

- Po: **api-fix-01**
- Przed: **api-fix-10** (runner 9 faz)
