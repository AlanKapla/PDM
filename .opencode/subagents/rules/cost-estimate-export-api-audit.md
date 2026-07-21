# Audyt API — cost-estimate-export

Data: 2026-07-21  
Źródło: feature-planner + `.opencode/features/cost-estimate-export.md`  
Skills: `api-cqrs`, `api-controllers`, `api-services`, `api-unit-tests`

## Podsumowanie

| Poziom | Liczba |
|--------|--------|
| Krytyczne | 3 |
| Wysokie | 4 |
| Normalne | 3 |

| Metryka | Wartość |
|---------|---------|
| Nowe encje / migracja | **0** |
| Nowe Queries | 1 (`ExportCostEstimateQuery` z `Format`) lub 2 osobne |
| Nowe endpointy | 2 (`…/export/xlsx`, `…/export/pdf`) |
| Nowe serwisy | 1 (`ICostEstimateExportService`) |
| Pytania blokujące | **0** (defaulty v1 w specu) |

---

## Krytyczne

1. **Brak eksporterów i NuGet** — w `Business.csproj` nie ma ClosedXML ani QuestPDF; w repo brak `*Export*` dla Cost Estimate.
2. **Brak endpointów FileResult** — w `WebApi` zero `return File(...)` / `FileContentResult`; downloady dziś przez SAS URI.
3. **Brak modelu ExportRow / flatten** — hierarchia grup/pozycji/opcji/komponentów wymaga dedykowanej warstwy mapowania przed rendererami.

## Wysokie

1. Handler musi **reuse’ować** ścieżkę dostępu z `GetCostEstimateDetailsQueryHandler` (cache + `AccessLevel != None`), nie `EnsureCanModifyStructure`.
2. Sumy eksportu = `CostEstimate.TotalNet/Gross/Vat` z encji — **bez** ponownego wywołania `ICostEstimateCalculationService` w v1 (sumy już przeliczone).
3. Additional fields: ładowanie jak w details (repo/cache + wartości na grupach/itemach) — kolumny dynamiczne wg `Order`.
4. Pierwszy sync binary response w API — Content-Type + `Content-Disposition` muszą być poprawne dla przeglądarki.

## Normalne

1. Sanitize nazwy pliku (znaki niedozwolone w Windows/URL).
2. Soft log warning przy bardzo dużych drzewach (>~5k wierszy) — bez soft-cap reject w v1.
3. Testy smoke ClosedXML/QuestPDF (niepusty byte[]) — bez snapshotów binarnych.

---

## Co już istnieje (reuse)

| Element | Ścieżka / uwagi |
|---------|-----------------|
| `CostEstimateController` | `api/tenants/{t}/projects/{p}/cost-estimate` — wzorzec routing + `[Authorize(Policy = ProjectEstimates)]` |
| `GetCostEstimateDetailsQueryHandler` | Cache CE + groups dict + items dict; access; currency; additional fields; map do Web |
| `ICostEstimateCacheService` | `GetCostEstimateAsync`, `GetGroupsDictionaryAsync`, `GetItemsDictionaryAsync` |
| `ICostEstimateAccessService` | `GetAccessLevelAsync` → `None` = Forbidden |
| Encje / totals | `CostEstimate.TotalNet/Gross/Vat`; grupy z Total*; itemy z Quantity, Unit, ceny, IsSelected, RelationType, Options/Components |
| `CostEstimateAdditionalField*` | Schema + wartości — jak w details |
| DI CE | `ServiceCollectionExtensions` — obok istniejących `ICostEstimate*` |
| Auth pipeline | `IAuthorizableRequest` + `PermissionCodes.ProjectEstimates` |

## Czego NIE reuse’ować / luki

| Element | Problem |
|---------|---------|
| `Docnet.Core` / `PdfToImageConverter` | Tylko **odczyt** PDF (AI import) — nie generowanie |
| `ICostEstimateCalculationService` | Nie wywoływać przy eksporcie v1 (koszt + ryzyko rozjazdu jeśli cache nieświeży — akceptujemy totals z encji jak details) |
| SAS / Blob download | Inny wzorzec (URL), nie FileResult |
| Cost Tracker controllers | Poza zakresem feature |

---

## Co dodać

### Business

```
Interfaces/Services/ICostEstimateExportService.cs
Implementation/Services/CostEstimates/CostEstimateExportService.cs  (lub CostEstimate/)
  - Flatten hierarchy → IReadOnlyList<CostEstimateExportRow>
  - ExportToXlsxAsync / ExportToPdfAsync → CostEstimateExportFile (bytes, contentType, fileName)
Interfaces/WebModels/CostEstimates/CostEstimateExportModels.cs
  - CostEstimateExportRow, CostEstimateExportFile, enum CostEstimateExportFormat
```

### NuGet (`Business.csproj`)

- `ClosedXML` (aktualna stabilna, .NET 10)
- `QuestPDF` (aktualna stabilna)

### CQRS

```
CQRS/CostEstimates/ExportCostEstimate/
  ExportCostEstimateQuery.cs          // TenantId, ProjectId, CostEstimateId, Format
  ExportCostEstimateQueryHandler.cs   // access + cache + export service
  ExportCostEstimateQueryValidator.cs // Guid not empty, Format enum
```

Preferuj **jeden** Query z `Format` enum → jeden handler; Controller ma 2 akcje wywołujące ten sam query.

### WebApi

```csharp
[HttpGet("{id:guid}/export/xlsx")]
[HttpGet("{id:guid}/export/pdf")]
// return File(result.Content, result.ContentType, result.FileName);
```

Uwaga routingu: istniejące `{id:guid}/…` (shares, additional-fields, items) — `export/xlsx` i `export/pdf` nie kolidują z `{scope}` (all/mine/shared) ani `details/{id}`.

### DI

```csharp
services.AddScoped<ICostEstimateExportService, CostEstimateExportService>();
```

### Testy

| Projekt | Zakres |
|---------|--------|
| `Business.Tests` | Flatten (grupa→item→option→component), nazwa pliku sanitize, smoke XLSX/PDF niepusty |
| `CQRS.Tests` | 404 brak CE, 403 AccessLevel.None, happy path Moq service |
| `WebApi.Tests` | Controller zwraca FileContentResult / poprawny content-type (opcjonalnie) |

---

## Flatten — reguły (zgodne ze spec defaultami)

1. DFS `RootGroups` po `Order`; child groups przed/itemami wg istniejącego porządku UI/API.
2. Dla każdej grupy: wiersz `RowType=Group` + wartości dodatkowe grupy.
3. Pozycje w grupie: główne (`RelationType.None`) → ich `Options` → ich `Components` (1 poziom).
4. Eksportuj **wszystkie** wiersze niezależnie od `IsSelected`; kolumna IsSelected = Tak/Nie.
5. Kolumny bazowe + dynamiczne `AdditionalFields` posortowane po `Order`.
6. Metadane w nagłówku PDF / arkuszu Podsumowanie: Name, Currency, TotalNet/Gross/Vat, data eksportu UTC/local.

---

## Pliki do zmiany / utworzenia

```
src/Business/Business.csproj
src/Business/Interfaces/Services/ICostEstimateExportService.cs                    (new)
src/Business/Interfaces/WebModels/CostEstimates/CostEstimateExportModels.cs       (new)
src/Business/Implementation/Services/CostEstimates/CostEstimateExportService.cs   (new)
src/CQRS/CostEstimates/ExportCostEstimate/ExportCostEstimateQuery.cs             (new)
src/CQRS/CostEstimates/ExportCostEstimate/ExportCostEstimateQueryHandler.cs      (new)
src/CQRS/CostEstimates/ExportCostEstimate/ExportCostEstimateQueryValidator.cs    (new)
src/WebApi/Controllers/CostEstimateController.cs                                 (2 endpointy)
src/WebApi/Extensions/ServiceCollectionExtensions.cs                             (DI)
tests/Business.Tests/.../CostEstimateExportServiceTests.cs                       (new)
tests/CQRS.Tests/CostEstimates/ExportCostEstimateQueryHandlerTests.cs            (new)
tests/WebApi.Tests/Controllers/CostEstimateControllerExportTests.cs              (new, opcjonalnie)
```

---

## Pytania przed refaktorem — ZAMKNIĘTE (spec v1)

| Pytanie | Decyzja |
|---------|---------|
| IsSelected filter | Eksportuj wszystkie + kolumna |
| Pola dodatkowe | Tak |
| Nazwa pliku | `{SanitizedName}_{yyyyMMdd}.{ext}` |
| Access | `!= None` |
| Modal opcji | Brak |
| Calculation refresh | Nie — totals z encji |
| Cost Tracker | Poza zakresem |

---

## Rekomendowana kolejność promptów

1. Modele + flatten + NuGet + DI stub  
2. ClosedXML  
3. QuestPDF  
4. CQRS + Controller  
5. Testy  

Po każdym: `dotnet build` / `dotnet test` w zakresie zmienionych projektów.
