# api-fix-04 — Faza 2: Classification

## Cel i zakres

Wydzielić fazę Classification do `ClassificationPipelineAgent` — przenieść `ClassifyAllImagesAsync` z `ImageExtractionPipelineAgent`.

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `Pipeline/ClassificationPipelineAgent.cs` | **NOWY** |
| `TechnicalDocumentationAgentContext` | Property `Classifications[]` |
| `DrawingClassificationAgentService.cs` | Bez zmian logiki — reuse |
| `ObviousDrawingTypeDetector.cs` | Reuse |

## Wymagania techniczne

- Skills: `api-services`
- Wejście: prepared images z Ingestion
- Wyjście: `DrawingClassification[]` per obraz
- Równoległość per rysunek — zachować obecny wzorzec

## Kryteria akceptacji

- [ ] Agent implementuje `ITechnicalDocumentationPipelineAgent`
- [ ] Classifications dostępne w context dla Grouping
- [ ] `dotnet build` OK

## Zależności

- Po: **api-fix-03**
- Przed: **api-fix-05**, **api-fix-10**
