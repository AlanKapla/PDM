# cost-estimate-export-api-fix-05 — Testy jednostkowe (uzupełnienie)

## Kontekst

- Feature: `.opencode/features/cost-estimate-export.md`
- Audyt: `.opencode/subagents/rules/cost-estimate-export-api-audit.md`
- Wymaga: fix-01..04
- Skills: `.opencode/skills/api-unit-tests/SKILL.md`

## Cel

Domknąć pokrycie testami eksporterów i kontrolera.

## Zadania

1. `Business.Tests` — `CostEstimateExportServiceTests`:
   - Flatten: kolejność group → item → option → component
   - IsSelected=false nadal w eksporcie
   - Additional field pojawia się w wierszu
   - FileName sanitize + data
   - Smoke Xlsx i Pdf (Length > 0)

2. `CQRS.Tests` — `ExportCostEstimateQueryHandlerTests` (jeśli nie kompletne w fix-04):
   - NotFound, Forbidden, Success dla Pdf i Xlsx

3. Opcjonalnie `WebApi.Tests` — `CostEstimateController` export:
   - Verify `FileContentResult` / content type (Moq mediator)

4. Uruchom:
```powershell
dotnet test tests/Business.Tests --filter CostEstimateExport
dotnet test tests/CQRS.Tests --filter ExportCostEstimate
```

## Poza zakresem

- Testy UI / E2E

## Kryteria done

- [ ] Filtry testów zielone
- [ ] Brak flaky zależności od kultury hosta (ustaw CultureInfo w teście jeśli trzeba)
