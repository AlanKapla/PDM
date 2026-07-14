# Feature Summary: Refaktor szablonów kosztorysu i renderowania kosztorysu

## Status: Wdrożone (build passes, all tests pass)

## Co zostało zrobione

### API (Backend)
1. **DTO split** — `UiConfigurationDto` i `UiConfigurationWeb` rozdzielone na `GroupColumnLayout`/`ItemColumnLayout` i `GroupColumns`/`ItemColumns`
2. **Service refactor** — `CostEstimateTemplateService.cs` zaktualizowany: `BuildTemplateStructureAsync`, `UpdateTemplateAsync`, `BuildColumnLayoutOrderMaps`, `MapDefaultTemplateToStructure`, `CreateTemplateFromDefaultAsync`, `DuplicateTemplateAsync` — wszystkie produkują/konsumują osobne listy kolumn grup i pozycji
3. **Backward compatibility** — gdy frontend wysyła stare `ColumnLayout`, serwer dzieli go po `FieldScope`
4. **Handler filters** — `GetCostEstimateDetailsQueryHandler` filtruje `IsVisible` dla obu list osobno
5. **Auto-enforce ordering** — `GroupName` (FieldType=0) automatycznie pierwszy w GroupColumnLayout, `ItemSystemName` (FieldType=100) pierwszy w ItemColumnLayout
6. **Validator fix** — `UpdateCostEstimateTemplateCommandValidator` naprawiony (4 build errory po zmianach DTO)

### UI (Frontend)
1. **Types** — `UiConfigurationWeb.groupColumns`/`itemColumns`, `UiConfigurationDto.groupColumnLayout`/`itemColumnLayout`, `ExpandedColumn.fieldScope` dodane; `POSITION_COL_MIN_WIDTH` usunięte
2. **CostEstimateTableView** — `expandedColumns` budowane z połączonych `groupColumns` + `itemColumns`; kolumna "Pozycja" usunięta z headera, colgroup i podsumowania
3. **SortableGroupRow** — kolumna "ETAP" usunięta, zastąpiona ikoną expand/collapse
4. **SortableItemRow** — kolumna "POZYCJA" usunięta, zastąpiona ikoną expand/collapse
5. **SortableOptionRow** — kolumna "OPCJA" usunięta
6. **SortableComponentRow** — kolumna "KOMPONENT" usunięta
7. **CostEstimateTemplateEditor** — zaktualizowany do budowania `groupColumns`/`itemColumns` z listy kolumn; "Kolejność pól" rozdzielona na dwie osobne sekcje przeciągania (dla pól etapów i pól pozycji); zapis przez nowe `groupColumnLayout`/`itemColumnLayout` zamiast deprecated `columnLayout`; **GroupName/ItemSystemName zablokowane** (orange lock — nieprzeciągalne, zawsze pierwsze)
8. **CostEstimateTemplateSelector** — zaktualizowany do wyświetlania połączonych `groupColumns` + `itemColumns`

### Testy
- API: **1509 passed, 0 failed** (WebApi: 206, CQRS: 1034, Business: 269)
- UI: **8 passed, 0 failed** (Vitest + AXE accessibility tests)

## Nowe pliki
- `.opencode/subagents/rules/cost-estimate-template-refactor-summary.md`

## Zmodyfikowane pliki

### API
- `Business/Interfaces/WebModels/CostEstimateTemplates/CostEstimateTemplateStructureWeb.cs`
- `Business/Interfaces/WebModels/CostEstimateTemplates/CostEstimateTemplateDtos.cs`
- `Business/Implementation/Services/CostEstimateTemplateService.cs`
- `CQRS/CostEstimates/GetCostEstimateDetails/GetCostEstimateDetailsQueryHandler.cs`
- `CQRS/CostEstimateTemplates/UpdateCostEstimateTemplate/UpdateCostEstimateTemplateCommandValidator.cs`
- `CQRS/CostEstimateTemplates/UpdateCostEstimateTemplate/UpdateCostEstimateTemplateCommandHandler.cs`

### UI
- `src/types/costEstimate.types.ts`
- `src/utils/costEstimateConverters.ts`
- `src/api/costEstimateTemplateApi.ts`
- `src/components/CostEstimate/costEstimateTableTypes.ts`
- `src/components/CostEstimate/CostEstimateTableView.tsx`
- `src/components/CostEstimate/rows/SortableGroupRow.tsx` (prop `expandedColumns` → `columns`)
- `src/components/CostEstimate/rows/SortableItemRow.tsx` (prop `expandedColumns` → `columns`)
- `src/components/CostEstimate/rows/SortableOptionRow.tsx` (prop `expandedColumns` → `columns`)
- `src/components/CostEstimate/rows/SortableComponentRow.tsx` (prop `expandedColumns` → `columns`)
- `src/pages/CostEstimateTemplateEditor.tsx`
- `src/pages/CostEstimateTemplateSelector.tsx`

## Blokery
- Brak

## Usprawnienia już zrobione (po pierwszym summary)
1. **Split expandedColumns by fieldScope** — `expandedColumns` podzielone na `groupColumns`/`itemColumns` w `CostEstimateTableView`, przekazywane osobno do row components jako `columns`
2. **Collapsible field sections** — dodane globalne przyciski zwijania/rozwijania sekcji pól grup i pozycji na kosztorysie w `CostEstimateTableView`
3. **Filter by isVisible** — zrobione po stronie API w handlerze (`GetCostEstimateDetailsQueryHandler`)
4. **Prop rename** — we wszystkich row komponentach (`SortableGroupRow`, `SortableItemRow`, `SortableOptionRow`, `SortableComponentRow`) prop `expandedColumns` przemianowany na `columns`
5. **Osobne sekcje kolejności pól w edytorze szablonu** — zakładka "Kolejność pól" rozdzielona na dwie sekcje: "Kolejność pól etapów" i "Kolejność pól pozycji", każda z własnym drag-and-drop; zapis przez `groupColumnLayout`/`itemColumnLayout`
6. **Dynamiczna szerokość kolumny expand** — `40px + maxLevel * 24px` zamiast stałej 40px; wyliczana z `flatRows.reduce(max nesting level)`
7. **Wyrównanie kolumn** — group rows renderują puste Td dla item columns (`itemColumnCount`); item rows renderują puste Td dla group columns (`groupColumnCount`); tfoot renderuje puste Td dla obu; wszystkie wiersze mają tę samą liczbę komórek co header
8. **EnsureItemHasNoComponents guard** — przyjmuje opcjonalny `FieldType?`; `ItemSystemName(100)` i `ItemSystemSelected(104)` są dozwolone na pozycjach z komponentami (opisują pozycję, nie wartości)
9. **Nazwy kolumn zawsze widoczne** — `GroupName` i `ItemSystemName` zawsze widoczne nawet gdy sekcja pól zwinięta; `visibleGroupColumns`/`visibleItemColumns` filtrują tylko do kolumny nazwy gdy zwinięte
10. **Freeze kolumn nazw podczas scrolla** — `GroupName` i `ItemSystemName` mają `position: sticky` z wyliczonym `left` offsetem (`baseStickyLeft + sum(visibleGroupColumnWidths)`); header: `zIndex:11 bg:white`; body: `zIndex:5 bg:rowBg`; zaimplementowane w `SortableGroupRow`, `SortableItemRow`, `SortableComponentRow`, `SortableOptionRow` przez prop `stickyLeftForName`

## Następne kroki (opcjonalne usprawnienia)
1. **Invalidate Redis cache** — po deployu trzeba zinvalidować cache `platform:template:{id}` bo stara struktura zawiera stare `ColumnLayout`
