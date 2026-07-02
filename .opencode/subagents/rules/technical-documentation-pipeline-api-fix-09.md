# api-fix-09 — Faza 7–8: Calculation + Audit

## Cel i zakres

Refaktor `MaterialsCalculationPipelineAgent` — wejście tylko z `context.ProjectModel`. Wydzielić `AuditPipelineAgent` z `ReportPipelineAgent`. Zachować `AuditAgentService` + `TechnicalDocumentationDeterministicAuditor`.

## Pliki do modyfikacji/utworzenia

| Plik | Akcja |
|------|-------|
| `MaterialsCalculationPipelineAgent.cs` | Wejście ProjectModel-only |
| `MaterialCalculationAgentService.cs` | Usunąć primary dependency na `FloorPlanDrawing[]` |
| `Pipeline/AuditPipelineAgent.cs` | **NOWY** |
| `MaterialScheduleBuilder.cs` | Adaptacja do nowego modelu |
| Testy calculation | Adaptacja |

## Wymagania techniczne

- Skills: `api-services`, `api-unit-tests`
- MaterialSchedule jako output fazy 7
- AuditResult jako output fazy 8 (`warnings`, `missingMaterials`, `assumptions`)
- Gate regresji K-02: mass 1170.30 kg (w api-fix-12)

## Kryteria akceptacji

- [ ] Calculation nie wymaga `Drawings[]` w group pipeline
- [ ] `AuditPipelineAgent` produkuje `AuditResult`
- [ ] Legacy runner nadal działa z flag=false
- [ ] `dotnet test` — bez regresji istniejących testów calculation

## Zależności

- Po: **api-fix-08**, **api-fix-13**
- Przed: **api-fix-10**
