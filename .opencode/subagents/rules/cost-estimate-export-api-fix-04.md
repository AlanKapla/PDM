# cost-estimate-export-api-fix-04 — CQRS Query + Controller + auth

## Kontekst

- Feature: `.opencode/features/cost-estimate-export.md`
- Audyt: `.opencode/subagents/rules/cost-estimate-export-api-audit.md`
- Wymaga: fix-01..03 (Export service działa dla Pdf i Xlsx)
- Skills: `.opencode/skills/api-cqrs/SKILL.md`, `api-controllers/SKILL.md`

## Cel

Wystawić endpointy HTTP zwracające plik, z auth jak GetCostEstimateDetails.

## Zadania

1. Utwórz `CQRS/CostEstimates/ExportCostEstimate/`:
   - `ExportCostEstimateQuery` : `IRequestQuery<CostEstimateExportFile>`, `IAuthorizableRequest`
     - `TenantId`, `ProjectId`, `CostEstimateId`, `Format`
     - `PermissionCode => PermissionCodes.ProjectEstimates`
     - `GetResource()` jak inne CE queries
   - `ExportCostEstimateQueryValidator` — Guids not empty; Format is defined enum
   - `ExportCostEstimateQueryHandler` (`sealed`):
     - Załaduj CE z `ICostEstimateCacheService` → NotFound jeśli brak
     - `GetAccessLevelAsync` → Forbidden jeśli `None`
     - Załaduj groups/items (jak details) + additional fields definitions
     - **Nie** wymagaj Full/Restricted modify
     - Wywołaj `ICostEstimateExportService.Export(...)`
     - Zwróć `CostEstimateExportFile`
   - Wzoruj orkiestrację na `GetCostEstimateDetailsQueryHandler` (max krótkie `Handle`, prywatne metody)

2. W `CostEstimateController` dodaj:
```csharp
[HttpGet("{id:guid}/export/xlsx")]
[Authorize(Policy = PermissionCodes.ProjectEstimates)]
[ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
// + 403, 404
public async Task<IActionResult> ExportXlsx(...)

[HttpGet("{id:guid}/export/pdf")]
// analogicznie
```
   - `return File(file.Content, file.ContentType, file.FileName);`
   - Bez logiki biznesowej w kontrolerze

3. Upewnij się, że routing nie koliduje z `{scope}` / `details/{id}`.

4. Testy handlera (CQRS.Tests): NotFound, Forbidden (AccessLevel.None), success (Moq export service).

## Poza zakresem

- UI
- Rozszerzanie testów flatten (fix-05)

## Kryteria done

- [ ] Oba GET zwracają plik z poprawnym Content-Type
- [ ] 403 / 404 działają
- [ ] `dotnet build` Release OK
