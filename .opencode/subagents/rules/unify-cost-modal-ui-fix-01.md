# UI Fix-01 — Ujednolicenie typów TypeScript dla kosztów

## Cel

Usunąć duplikaty typów TS dla TrackedCost i zaktualizować typy ProjectCost po zmianach API (fix-01).
Po tym kroku codebase ma jeden kanoniczny typ dla każdej encji.

## Kontekst

Raport UI audytu: `.github/subagents/rules/unify-cost-modal-ui-audit.md`
Raport API audytu: `.github/subagents/rules/unify-cost-modal-api-audit.md`

API fix-01 zmieniło kontrakt ProjectCost:
- `NetAmount` → `Net`, `GrossAmount` → `Gross`
- Dodano: `Contractor`, `Number`
- Usunięto: `Place`
- Create/Update response: teraz pełny obiekt `ProjectCostWeb`

## Problem — duplikaty typów TrackedCost

`TrackedCostWeb` i `CreateTrackedCostRequest` są zdefiniowane w **2-3 miejscach** z różnymi kształtami:

1. `src/features/dashboard/types/projectDashboard.types.ts` — **kanoniczne, bogate** (użyj tych)
2. `src/types/costTracker.types.ts` — starsze, niekompletne (usuń lub wyczyść duplikaty)
3. `src/api/costTrackerApi.ts` — inline typy requestów (zastąp importem z kanonicznych)

## Zadania

### Zadanie 1 — Audyt i usunięcie duplikatów TrackedCost

1. Przeczytaj `src/features/dashboard/types/projectDashboard.types.ts` — to jest kanoniczne źródło
2. Przeczytaj `src/types/costTracker.types.ts` — zidentyfikuj duplikaty
3. Przeczytaj `src/api/costTrackerApi.ts` — zidentyfikuj inline typy

Działanie:
- Jeśli typ w `costTracker.types.ts` lub `costTrackerApi.ts` jest zduplikowany z kanonicznymi → usuń go i zastąp importem
- Jeśli typ w `costTracker.types.ts` NIE istnieje w kanonicznych ale jest używany → przenieś do kanonicznych
- Zaktualizuj wszystkie importy w plikach korzystających z usuniętych typów

### Zadanie 2 — Aktualizacja typów ProjectCost

Plik do aktualizacji: `src/types/project.types.ts` (lub gdzie są zdefiniowane typy ProjectCost)

Zmiany w interfejsie/typie dla `ProjectCostListItemWeb` (lub jak się nazywa):
- `netAmount: number | null` → `net: number | null`
- `grossAmount: number | null` → `gross: number | null`
- Dodać: `contractor: string | null`
- Dodać: `number: string | null`
- Usunąć: `place: string | null` (jeśli istnieje)
- Dodać response type `ProjectCostWeb` jeśli Create/Update API teraz zwraca pełny obiekt

Zmiany w interfejsie/typie dla requestów (`CreateProjectCostRequest`, `UpdateProjectCostRequest`):
- `netAmount` → `net`
- `grossAmount` → `gross`
- Dodać: `contractor?: string | null`
- Dodać: `number?: string | null`
- Usunąć: `place?: string | null`

### Zadanie 3 — Aktualizacja wywołań API

Przeszukaj pliki używające starych nazw pól ProjectCost i zaktualizuj:
- `projectApi.ts` lub gdzie są funkcje `createProjectCost`/`updateProjectCost` — zaktualizuj body żądania
- Wszędzie gdzie odczytywane jest `cost.netAmount` lub `cost.grossAmount` → zmień na `cost.net` / `cost.gross`
- Wszędzie gdzie odczytywane jest `cost.place` → usuń lub zastąp

Prawdopodobne miejsca użycia:
- `src/api/projectApi.ts`
- `src/pages/ProjectSimpleCosts.tsx`
- `src/components/ExpenseFormModal.tsx`
- `src/components/ExpenseCard.tsx`

### Zadanie 4 — Weryfikacja

Uruchom: `npx tsc --noEmit` w katalogu `01-Applications/ProjectDataManagementUI`

Jeśli są błędy — napraw je.

## Kryteria sukcesu

- `npx tsc --noEmit` → 0 błędów
- Brak duplikatów typów `TrackedCostWeb`, `CreateTrackedCostRequest`
- Brak odwołań do `netAmount`, `grossAmount`, `place` w kontekście ProjectCost
- Typy ProjectCost odzwierciedlają nowy kontrakt API
