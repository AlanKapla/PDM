# Refaktor obliczeń pól finansowych w kosztorysach — UI + API

## Cel
Ujednolicić logikę obliczeń i blokowania pól finansowych pozycji kosztorysu zgodnie z poniższymi regułami biznesowymi. Zmiany muszą być spójne między warstwą API i UI.

## Reguły biznesowe

### 1. Domyślna ilość
Po dodaniu nowej pozycji (RelationType=None, Component, Option) **ilość domyślnie = 1**.

### 2. Pola edytowalne przez użytkownika
- `quantity` (ilość)
- `unitPriceNet` (cena jednostkowa netto)
- `vatRate` (stawka VAT — ułamek, np. 0.23)
- `unitPriceGross` (cena jednostkowa brutto) — **tylko gdy VAT NIE jest podany**

### 3. Pola obliczane automatycznie i zablokowane do edycji
- `netValue` = `unitPriceNet × quantity` (gdy oba podane)
- `vatValue` = `netValue × vatRate` (gdy netValue i vatRate podane)
- `grossValue`:
  - gdy `unitPriceGross` znane (wpisane ręcznie lub obliczone): `unitPriceGross × quantity`
  - gdy `netValue` i `vatValue` znane: `netValue + vatValue`
  - gdy `netValue` i `vatRate` znane (bez vatValue): `netValue × (1 + vatRate)`
- `unitPriceGross`:
  - gdy `unitPriceNet` i `vatRate` podane: `unitPriceNet × (1 + vatRate)` — **zablokowane do edycji**
  - gdy brak VAT, a podane `grossValue` i `quantity`: `grossValue / quantity`
  - gdy brak VAT, użytkownik może wpisać `unitPriceGross` ręcznie

### 4. Priorytety obliczeń
1. Jeśli `unitPriceNet` + `vatRate` → `unitPriceGross` obliczone, zablokowane
2. Jeśli `unitPriceGross` znane + `quantity` → `grossValue = unitPriceGross × quantity`
3. Wartości netto/VAT zawsze z unitPriceNet × quantity × vatRate

### 5. Istniejące reguły blokowania (zachować)
- Pozycja z komponentami → pola finansowe zablokowane (suma komponentów)
- Pozycja z zaznaczoną opcją → pola finansowe zablokowane (wartości z opcji)

---

## Pliki kluczowe (już istniejące — refaktor, nie przepisywanie od zera)

### API
- `02-ApplicationServices/ProductDataManagementWebAPI/src/Business/Implementation/Helpers/CostEstimateItemFinancialCalculator.cs` — **główna logika obliczeń**
- `02-ApplicationServices/ProductDataManagementWebAPI/src/Business/Implementation/Services/CostEstimateCalculationService.cs` — używa kalkulatora
- `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/CostEstimates/AddCostEstimateItem/AddCostEstimateItemCommandHandler.cs` — **dodać Quantity = 1**
- `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/CostEstimates/UpdateItemBaseFields/UpdateItemBaseFieldsCommandHandler.cs` — walidacja pól obliczanych
- `02-ApplicationServices/ProductDataManagementWebAPI/tests/Business.Tests/Services/CostEstimateItemFinancialCalculatorTests.cs` — rozszerzyć testy

### UI
- `01-Applications/ProjectDataManagementUI/src/utils/costEstimateItemFinancial.ts` — **mirror logiki API**
- `01-Applications/ProjectDataManagementUI/src/utils/costEstimateItemFlags.ts` — flagi blokowania pól
- `01-Applications/ProjectDataManagementUI/src/components/CostEstimate/TreeView/TreeViewRow.tsx` — renderowanie pól z isDisabled
- `01-Applications/ProjectDataManagementUI/src/components/CostEstimate/CardView/PositionDetailModal.tsx` — modal szczegółów
- `01-Applications/ProjectDataManagementUI/src/pages/CostEstimateEditPage.tsx` — obsługa zmian pól i autosave
- Szukaj `recalculateCostEstimateDetails` — używany do przeliczania lokalnego

---

## Kroki implementacji

### Krok 1 — API: domyślna ilość
W `AddCostEstimateItemCommandHandler` ustaw `Quantity = 1` przy tworzeniu nowej pozycji.

### Krok 2 — API: ujednolicenie kalkulatora
Zaktualizuj `CostEstimateItemFinancialCalculator`:
- `CalculateValueGross` — gdy `unitPriceGross` i `quantity` podane, zwróć `unitPriceGross × quantity` (priorytet nad net+vat)
- `IsUnitPriceGrossComputed` — true tylko gdy `unitPriceNet` i `vatRate` oba podane (NIE gdy gross/quantity — to inna ścieżka)
- `IsGrossValueComputed` — true gdy można obliczyć z dostępnych danych (unitPriceGross×qty, net+vat, net×(1+vat))
- Dodaj `CalculateGrossValueFromUnitPriceGross(unitPriceGross, quantity)` jeśli potrzebne

Zaktualizuj `ValidateComputedFieldEdit` w `UpdateItemBaseFieldsCommandHandler` zgodnie z nowymi regułami:
- Odrzuć edycję `unitPriceGross` gdy `vatRate` jest podany (bo jest obliczane)
- Odrzuć edycję `netValue`, `vatValue`, `grossValue` gdy są obliczane

### Krok 3 — API: testy
Rozszerz `CostEstimateItemFinancialCalculatorTests` o przypadki:
- Domyślna ilość (test handlera jeśli istnieje aktywny)
- grossValue z unitPriceGross × quantity
- unitPriceGross zablokowane gdy VAT podany
- unitPriceGross edytowalne gdy VAT brak

### Krok 4 — UI: mirror logiki
Zaktualizuj `costEstimateItemFinancial.ts` (`deriveItemFinancialState`) aby mirrorowało API:
- `unitPriceGrossComputed` = `unitNet != null && vat != null` (nie z gross/qty)
- `grossValue` priorytet: unitPriceGross × qty > net + vat > net × (1+vat)
- Flagi w `costEstimateItemFlags.ts` muszą być spójne

### Krok 5 — UI: blokowanie pól
W `TreeViewRow.tsx` i `PositionDetailModal.tsx`:
- `netValue`, `grossValue`, `vatValue` — zawsze disabled w trybie edycji (computed)
- `unitPriceGross` — disabled gdy `flags.unitPriceGrossComputed` (czyli gdy VAT podany)
- `unitPriceNet`, `vatRate`, `quantity` — edytowalne (chyba że lockedByComponents/hasSelectedOption)

### Krok 6 — UI: domyślna ilość
Sprawdź czy UI przy dodawaniu pozycji lokalnie ustawia quantity=1 (optimistic update). Jeśli nie — dodaj w miejscu tworzenia pozycji w `CostEstimateEditPage.tsx` lub hooku.

### Krok 7 — Build i testy
```powershell
# API
cd 02-ApplicationServices/ProductDataManagementWebAPI
dotnet build --configuration Release
dotnet test tests/Business.Tests --configuration Release --no-build --filter "CostEstimateItemFinancial"

# UI
cd 01-Applications/ProjectDataManagementUI
npm run build
```

---

## Konwencje (OBOWIĄZKOWE)
- API: zakaz `var`, `is null`/`is not null`, handlery `sealed`, wyjątki domenowe
- UI: zakaz `any`, logika w utils/hooks, kolory przez Chakra tokens
- Minimalny diff — nie refaktoruj niepowiązanego kodu
- Nie twórz migracji DB — Quantity już istnieje w encji

## Oczekiwany raport
Zwróć raport w formacie z refactor-agent.agent.md z listą zmodyfikowanych plików i wynikami build/test.
