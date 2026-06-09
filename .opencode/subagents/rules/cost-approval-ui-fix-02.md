# UI Fix 02 — Usunięcie komponentów sharingu + ProjectDetails

## Cel
Usunąć wszystkie komponenty i logikę związaną z udostępnianiem kosztów.

---

## Krok 1 — Usuń pliki komponentów sharingu

Usuń następujące pliki:
1. `src/components/ShareCostModal.tsx`
2. `src/components/ManageCostShareModal.tsx`
3. `src/components/ShareCostsModal.tsx`

---

## Krok 2 — Wyczyść `ProjectDetails.tsx`

Plik: `src/pages/ProjectDetails.tsx`

Znajdź i usuń:
- Import `ShareCostModal`
- Stan `sharedCosts: SharedProjectCostWeb[]`
- Wywołanie `projectApi.getSharedProjectCosts()`
- Użycie `<ShareCostModal />` w JSX
- Import `SharedProjectCostWeb` z typów
- Wszelkie funkcje `fetchSharedProjectCosts`, `handleShareCost` itp.

---

## Krok 3 — Sprawdź inne miejsca użycia

Wyszukaj w całym projekcie UI `ShareCostModal|ManageCostShareModal|ShareCostsModal|shareProjectCosts|updateCostShare|getSharedProjectCosts|SharedProjectCostWeb|sharedWithUserIds` i usuń wszystkie odwołania.

---

## Weryfikacja
```
npx tsc --noEmit 2>&1 | Select-String "ShareCostModal|ManageCostShareModal|ShareCostsModal|sharedWithUserIds|SharedProjectCostWeb|error TS" | Select-Object -First 20
```

Oczekiwany wynik: brak błędów związanych z usuniętymi komponentami.
