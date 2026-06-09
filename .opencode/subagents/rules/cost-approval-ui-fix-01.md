# UI Fix 01 — Typy TypeScript + API client

## Cel
Zaktualizować typy i klienta API:
1. Usunąć `isAccepted`, `sharedWithUserIds` z `ProjectCostListItemWeb`
2. Dodać `approvalStatus`, `approvedByUserId`, `approvedAt`
3. Usunąć `SharedProjectCostWeb`
4. Zaktualizować `CreateProjectCostCommand` / `UpdateProjectCostCommand`
5. Zaktualizować `ResourceScope` — dodać `PendingApproval = 3`
6. Zaktualizować `projectApi.ts` — usunąć share metody, dodać 4 nowe (submit/withdraw/approve/reject)

Przeczytaj skill `.github/skills/ui/skill-ui-types.md` i `.github/skills/ui/skill-ui-api-client.md`.

---

## Krok 1 — Typy `project.types.ts`

Plik: `src/types/project.types.ts`

### 1a. Zaktualizuj `ProjectCostListItemWeb`

Zastąp pola:
```ts
isAccepted: boolean;
sharedWithUserIds: string[];
```

Na:
```ts
approvalStatus: CostApprovalStatus;
approvedByUserId: string | null;
approvedAt: string | null;
```

### 1b. Usuń `SharedProjectCostWeb` (cały interface/type)

### 1c. Zaktualizuj `CreateProjectCostCommand` — usuń pole `isAccepted?: boolean`

### 1d. Zaktualizuj `UpdateProjectCostCommand` — usuń pole `isAccepted: boolean`

### 1e. Dodaj enum `CostApprovalStatus`

```ts
export type CostApprovalStatus = 'Draft' | 'PendingApproval' | 'Approved';
```

---

## Krok 2 — `ResourceScope` w `projectApi.ts`

Plik: `src/api/projectApi.ts`

Dodaj wartość:
```ts
export enum ResourceScope {
  All = 0,
  Mine = 1,
  Shared = 2,        // zachowaj jeśli używane przez inne moduły (np. WorkSchedule)
  PendingApproval = 3
}
```

---

## Krok 3 — `projectApi.ts` — usuń metody share, dodaj nowe

Plik: `src/api/projectApi.ts`

**Usuń:**
- `getSharedProjectCosts()`
- `shareProjectCosts()`
- `updateCostShare()`

**Dodaj po `deleteProjectCost`:**

```ts
export const submitProjectCostForApproval = async (
  tenantId: string,
  projectId: string,
  costId: string
): Promise<ProjectCostListItemWeb> => {
  const response = await apiClient.post<ProjectCostListItemWeb>(
    `/tenants/${tenantId}/projects/${projectId}/cost/${costId}/submit`
  );
  return response.data;
};

export const withdrawProjectCostFromApproval = async (
  tenantId: string,
  projectId: string,
  costId: string
): Promise<ProjectCostListItemWeb> => {
  const response = await apiClient.post<ProjectCostListItemWeb>(
    `/tenants/${tenantId}/projects/${projectId}/cost/${costId}/withdraw`
  );
  return response.data;
};

export const approveProjectCost = async (
  tenantId: string,
  projectId: string,
  costId: string
): Promise<ProjectCostListItemWeb> => {
  const response = await apiClient.post<ProjectCostListItemWeb>(
    `/tenants/${tenantId}/projects/${projectId}/cost/${costId}/approve`
  );
  return response.data;
};

export const rejectProjectCost = async (
  tenantId: string,
  projectId: string,
  costId: string
): Promise<ProjectCostListItemWeb> => {
  const response = await apiClient.post<ProjectCostListItemWeb>(
    `/tenants/${tenantId}/projects/${projectId}/cost/${costId}/reject`
  );
  return response.data;
};
```

---

## Krok 4 — Hook `useProjectCostMutations`

Plik: `src/hooks/useProjectCostMutations.ts`

Sprawdź definicje lokalnych interfejsów `CreateProjectCostRequest` i `UpdateProjectCostRequest` — usuń z nich `isAccepted`.

Dodaj 4 nowe mutacje (submit, withdraw, approve, reject) używające nowych funkcji z `projectApi`.

---

## Weryfikacja
```
cd 01-Applications/ProjectDataManagementUI
npx tsc --noEmit 2>&1 | Select-String "project.types|projectApi|useProjectCostMutations|error TS" | Select-Object -First 20
```
