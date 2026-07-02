# api-fix-13 — Model §8.1 + serializacja DetailsJson

## Cel i zakres

Migracja kontraktu `DetailsJson` do **ProjectModel §8.1** + `MaterialSchedule` + `AuditResult`. Rozszerzyć `ProjectModel.cs`, zaktualizować serializer, deprecate `ProjectTechnicalDocumentationDetailsBuilder` dla group pipeline.

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `Models/ProjectModel.cs` | `slab`, `elevations`, `warnings[]`, `extractionMetadata` |
| `ProjectTechnicalDocumentationDetails.cs` | Nowy root DTO lub uproszczenie |
| `TechnicalDocumentationDetailsSerializer.cs` | Nowy format serializacji |
| `ProjectTechnicalDocumentationDetailsBuilder.cs` | Deprecate / adapter legacy |
| `OutputPipelineAgent.cs` / `ReportPipelineAgent.cs` | Zapis nowego formatu |
| `ProjectModelSerializationTests.cs` | Round-trip nowy schema |

## Wymagania techniczne

- Skills: `api-entities`, `api-services`, `api-unit-tests`
- Root JSON:
```json
{
  "projectModel": { /* §8.1 */ },
  "materialSchedule": { },
  "auditResult": { }
}
```
- `slab` — nowy typ lub mapowanie z `Ceilings[]` (PDM gap)
- `warnings[]` — ujednolicenie `Conflicts` + `MissingData`
- `extractionMetadata` — pipeline version, thematic groups, token usage
- Zachować `Columns/Beams/Lintels` jako rozszerzenie PDM (decyzja: zachować)
- **Nie** migrować danych DB — tylko nowe przetwarzania

## Kryteria akceptacji

- [ ] Serialize/deserialize round-trip test green
- [ ] Stary DetailsJson deserializuje się bez crash (backward read) lub documented breaking
- [ ] CQRS `GetTechnicalDocumentationDetails` zwraca nowy kształt dla nowych rekordów
- [ ] Blokuje ui-fix-02 (typy TS muszą mirrorować ten kontrakt)

## Zależności

- Po: **api-fix-01**, **api-fix-02** (wcześnie w kolejności — przed fazami 6–9)
- Blokuje: **api-fix-08**, **api-fix-09**, **ui-fix-02–05**
- Równolegle z: **api-fix-03–07** (agenty mogą używać wewnętrznych typów do czasu Consolidation)
