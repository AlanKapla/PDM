# cost-estimate-export-api-fix-01 — NuGet, modele, flatten, DI

## Kontekst

- Feature: `.opencode/features/cost-estimate-export.md`
- Audyt: `.opencode/subagents/rules/cost-estimate-export-api-audit.md`
- Skills: `.opencode/skills/api-services/SKILL.md`

## Cel

Dodać zależności ClosedXML + QuestPDF, model wiersza eksportu, wspólny flatten hierarchii oraz szkielet `ICostEstimateExportService` (metody mogą rzucać `NotImplementedException` dla PDF/XLSX — implementacja w fix-02/03).

## Zadania

1. W `src/Business/Business.csproj` dodaj pakiety:
   - `ClosedXML` (stabilna, kompatybilna z net10.0)
   - `QuestPDF` (stabilna, kompatybilna z net10.0)

2. Utwórz modele w `src/Business/Interfaces/WebModels/CostEstimates/CostEstimateExportModels.cs`:
   - `enum CostEstimateExportFormat { Pdf, Xlsx }`
   - `sealed record CostEstimateExportFile(byte[] Content, string ContentType, string FileName)`
   - `sealed record CostEstimateExportRow` z polami m.in.:
     - `RowType` (Group / Item / Option / Component — enum lub string stały)
     - `Level` (int), `Name`, `Quantity`, `Unit`, `UnitPriceNet`, `VatRate`, `UnitPriceGross`, `NetValue`, `VatValue`, `GrossValue`, `IsSelected`
     - `IReadOnlyDictionary<string, string?> AdditionalValues` (klucz = field key / id stabilny)
   - Opcjonalnie `CostEstimateExportMeta` (Name, CurrencyCode, CurrencySymbol, TotalNet/Gross/Vat, ExportedAt)

3. Interfejs `ICostEstimateExportService`:
```csharp
CostEstimateExportFile Export(
    CostEstimate costEstimate,
    IReadOnlyList<CostEstimateGroup> rootGroupsOrdered,
    // lub struktury już zmapowane — wybierz spójnie z cache handlera
    IReadOnlyList<CostEstimateAdditionalFieldWeb> additionalFields,
    CostEstimateExportFormat format);
```
   Preferuj sygnaturę przyjmującą dane domenowe/cache + additional field definitions, tak by handler nie budował Web DTO tylko po to by eksportować. Dopuszczalne też wejście z już zbudowanego drzewa Web (`CostEstimateDetailsWeb`) jeśli upraszcza — **jedna** ścieżka, bez duplikacji.

4. Implementacja `CostEstimateExportService` (`sealed`):
   - Metoda wewnętrzna `Flatten(...)` → `IReadOnlyList<CostEstimateExportRow>`
   - Reguły flatten ze spec/audytu: DFS grup, itemy + Options + Components, **wszystkie** IsSelected, additional fields wg Order
   - `BuildFileName(string estimateName, CostEstimateExportFormat format, DateTime utcNow)` → sanitize + `{name}_{yyyyMMdd}.pdf|xlsx`
   - `Export(...)`: switch format → na razie wywołaj prywatne stuby `BuildXlsx` / `BuildPdf` rzucające `NotImplementedException` **ALBO** zwróć pustą implementację tylko jeśli fix-02/03 idą natychmiast w tej samej sesji — preferuj stub jasny

5. DI w `ServiceCollectionExtensions.cs`:
   `services.AddScoped<ICostEstimateExportService, CostEstimateExportService>();`

6. Testy jednostkowe flatten w `tests/Business.Tests` (min. 1 scenariusz: grupa + item + option + component + additional field).

## Poza zakresem

- Pełny ClosedXML / QuestPDF rendering (fix-02, fix-03)
- Controller / CQRS (fix-04)

## Kryteria done

- [ ] Pakiety w csproj, solution buduje się
- [ ] Flatten pokryty testem
- [ ] DI zarejestrowane
- [ ] Sanitize nazwy pliku działa (np. `a/b*.xlsx` → bezpieczna nazwa)
