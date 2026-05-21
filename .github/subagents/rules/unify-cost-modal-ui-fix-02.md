# UI Fix-02 — Hook useProjectCostMutations

## Cel

Wydzielić logikę mutacji ProjectCost z `ProjectSimpleCosts.tsx` do dedykowanego hooka `useProjectCostMutations`.
To odciąży 700-liniową stronę i stworzy symetrię z istniejącym `useTrackedCostMutations`.

## Kontekst

Raport UI audytu: `.github/subagents/rules/unify-cost-modal-ui-audit.md`

Istniejący wzorzec (TrackedCost):
- `src/features/dashboard/hooks/useTrackedCostMutations.ts` — hook mutacji dla TrackedCost

Do stworzenia (ProjectCost):
- `src/hooks/useProjectCostMutations.ts` — hook mutacji dla ProjectCost

## Stan obecny

`ProjectSimpleCosts.tsx` (~700 linii) zawiera inline:
- Wywołania `projectApi.createProjectCost(...)` 
- Wywołania `projectApi.updateProjectCost(...)`
- Wywołania `projectApi.deleteProjectCost(...)`
- Lokalny state `isSaving`, `isDeleting`
- Error handling dla mutacji
- `onSuccess` callback odświeżający listę

## Nowy hook — specyfikacja

### Lokalizacja

`src/hooks/useProjectCostMutations.ts`

### Interfejs

```typescript
export interface UseProjectCostMutationsResult {
  createCost: (data: CreateProjectCostRequest, document?: File | null) => Promise<ProjectCostWeb>;
  updateCost: (costId: string, data: UpdateProjectCostRequest, document?: File | null) => Promise<ProjectCostWeb>;
  deleteCost: (costId: string) => Promise<void>;
  isCreating: boolean;
  isUpdating: boolean;
  isDeleting: boolean;
}

export function useProjectCostMutations(
  tenantId: string,
  projectId: string
): UseProjectCostMutationsResult
```

### Implementacja

Wzoruj się na `useTrackedCostMutations.ts`:
- Każda operacja ma osobny loading state (`isCreating`, `isUpdating`, `isDeleting`)
- Błędy są rzucane (nie łykane) — obsługa w komponencie
- `createCost` wysyła multipart/form-data jeśli `document` jest podany
- `updateCost` wysyła multipart/form-data jeśli `document` jest podany
- Response type po API fix-01: `ProjectCostWeb` (pełny obiekt, nie `Guid`)

### Funkcje API do użycia

Z `src/api/projectApi.ts` (lub dedykowanego serwisu jeśli istnieje):
- `createProjectCost(tenantId, projectId, data)` — po API fix-01 zwraca `ProjectCostWeb`
- `updateProjectCost(tenantId, projectId, costId, data)` — po API fix-01 zwraca `ProjectCostWeb`
- `deleteProjectCost(tenantId, projectId, costId)` — zwraca `void`

Jeśli `projectApi.ts` nie ma dedykowanych funkcji lub są inline w stronie — wydziel je do oddzielnego serwisu `src/api/projectCostApi.ts` ALBO dodaj do `projectApi.ts`.

## Aktualizacja ProjectSimpleCosts.tsx

Po stworzeniu hooka:
1. Dodaj `const { createCost, updateCost, deleteCost, isCreating, isUpdating, isDeleting } = useProjectCostMutations(tenantId, projectId)`
2. Zastąp inline mutacje wywołaniami hooka
3. Usuń lokalne stany `isSaving`, `isDeleting` które duplikują stan hooka
4. Zachowaj całą logikę odświeżania listy (onSuccess callbacks) — przenieś je do hooka lub zostaw w komponencie jako callback prop

## Kryteria sukcesu

- `npx tsc --noEmit` → 0 błędów
- `useProjectCostMutations` jest eksportowany z `src/hooks/useProjectCostMutations.ts`
- `ProjectSimpleCosts.tsx` używa hooka zamiast inline mutacji
- Każda operacja (create/update/delete) ma osobny loading state
- Istniejąca funkcjonalność ProjectSimpleCosts nie jest naruszona
