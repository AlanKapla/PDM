# Refaktor szablonów kosztorysu i renderowania kosztorysu

## Opis
Refaktor sposobu definiowania i renderowania pól w kosztorysie. Wprowadza:
1. Osobne kolejności pól dla etapów (group fields) i pozycji (item fields)
2. Wymuszenie pozycji Nazwa etapu / Nazwa pozycji jako pierwsze (obligatoryjne)
3. Rozdzielenie wyświetlania pól: najpierw pola etapów, potem pola pozycji
4. Opcja zwijania/rozwijania sekcji pól etapów i pozycji na kosztorysie
5. Usunięcie kolumny "Pozycja" (z numerem ETAP/POZYCJA) - zbędna i myląca

## Domeny
- API (backend: CostEstimateTemplateService, DTOs, struktura szablonu)
- UI (frontend: CostEstimateTableView, SortableGroupRow, SortableItemRow, typy)

## Zmiany w encjach/DB
- Brak zmian w schemacie DB — wykorzystujemy istniejące pole `Order` na `CostEstimateTemplateFieldDefinitionBase`
- Pole `FieldScope` już istnieje i rozróżnia Group od Item*

## Zmiany w architekturze
- **Backend**: `UiConfiguration` zamiast jednej płaskiej listy `ColumnConfigurationWeb[]` musi dostarczać osobne listy dla grup i pozycji
- **Backend**: Budowanie struktury szablonu — osobne sortowanie group fields i item fields
- **UI**: `ExpandedColumn[]` podzielone na `groupColumns` i `itemColumns`
- **UI**: Renderowanie wierszy grup — tylko group columns
- **UI**: Renderowanie wierszy pozycji — tylko item columns
- **UI**: Nowy mechanizm zwijania/rozwijania sekcji pól (collapsible field sections)
- **UI**: Usunięcie kolumny "Pozycja" (sticky left column z ETAP/POZYCJA n)
