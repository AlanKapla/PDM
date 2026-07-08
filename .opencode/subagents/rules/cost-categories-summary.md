# Feature wdrożony: Kategorie kosztów projektowych

> Data: 2026-07-08

## Co zostało zrobione

### API
- Encja `ProjectCostCategory` (TPH `ProjectParams`, discriminator `CostCategory`)
- `CategoryId` nullable na `BaseCost` (FK SetNull)
- CQRS CRUD + reorder kategorii (5 operacji)
- 5 endpointów REST w `ProjectController` (`/cost-categories`)
- Seed 10 domyślnych kategorii w `CreateProjectCommandHandler`
- Rozszerzenie Create/Update `ProjectCost` i `TrackedCost` o `CategoryId`
- Mapowanie `CategoryId`, `CategoryName`, `CategoryColor` w web modelach
- AI: `categoryName` w prompt, enrichment w `ParseCostDocumentQueryHandler`
- Dashboard: `CostByCategoryWeb[]` w `ProjectDashboardWeb`
- Testy istniejące zaktualizowane (mock categoryRepo)

### UI
- `useProjectCostCategories` + API client
- `CostCategoryManager` w Parametrach projektu
- `CostCategoryPicker` + `CostCategoryQuickAddModal` w `CostModal` (AI flow)
- `CostCategoryPieChart` na zakładce Finanse
- Typy TS rozszerzone
- Testy AXE zaktualizowane (8/8 pass)

## Nowe pliki

**API:** ~25 plików CQRS + encje + konfiguracja  
**UI:** `useProjectCostCategories.ts`, `CostCategoryManager.tsx`, `CostCategoryPicker.tsx`, `CostCategoryQuickAddModal.tsx`, `CostCategoryPieChart.tsx`

## Blokery

- **Migracja EF** — wymaga wygenerowania gdy WebApi nie jest uruchomiony (pliki DLL zablokowane przez `WebApi.exe`):
  ```powershell
  cd 02-ApplicationServices/ProductDataManagementWebAPI/src/Entities
  dotnet ef migrations add add-project-cost-categories --startup-project ../WebApi
  ```
- Po migracji: rozważyć backfill SQL dla istniejących projektów (10 kategorii)

## Następne kroki

1. Zatrzymać WebApi / VS debugger i wygenerować migrację
2. Opcjonalnie: backfill kategorii dla istniejących projektów w migracji SQL
3. Faza 2: kolumna kategorii w `CostsTab` / `RecentCostsList`
4. `CostFormModal` (legacy) — poza scope MVP
