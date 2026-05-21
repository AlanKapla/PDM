# contractors-ui-fix-01 — Typy TypeScript + API client + hooki React Query

## Cel
Stworzenie fundamentów UI dla feature Kontrahenci:
1. Typy TypeScript (`contractor.types.ts`)
2. API client (`contractorApi.ts`)
3. Hooki React Query (`useContractors.ts`)
4. Aktualizacja istniejących typów (usunięcie `contractor: string`, dodanie `contractorId + contractorName`)

## Skill
Przeczytaj `.github/skills/ui/skill-ui-types.md`, `.github/skills/ui/skill-ui-api-client.md`, `.github/skills/ui/skill-ui-hooks.md` przed implementacją.

## Kontekst
- Raport audytu UI: `.github/subagents/rules/contractors-ui-audit.md`
- Wzorzec typów: `src/types/costTracker.types.ts`
- Wzorzec API client: `src/api/costTrackerApi.ts` lub `src/api/tenantApi.ts`
- Wzorzec hooków: `src/hooks/queries/useCostTracker.ts`

## Zmiany do wykonania

### 1. Nowy plik: `src/types/contractor.types.ts`

```typescript
export interface ContractorWeb {
  id: string;
  tenantId: string;
  name: string;
  taxId: string | null;
  email: string | null;
  phoneNumber: string | null;
  street: string | null;
  city: string | null;
  postalCode: string | null;
  country: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface ContractorListItemWeb {
  id: string;
  name: string;
  taxId: string | null;
  city: string | null;
}

export interface CreateContractorRequest {
  name: string;
  taxId?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
  street?: string | null;
  city?: string | null;
  postalCode?: string | null;
  country?: string | null;
  notes?: string | null;
}

export interface UpdateContractorRequest extends CreateContractorRequest {
  id: string;
}
```

### 2. Modyfikacja `src/types/costTracker.types.ts`

Znajdź interfejsy `CostFormValues`, `TrackedCostWeb`, `CreateCostRequest`, `UpdateCostRequest`.

W `CostFormValues`:
- Usunąć: `contractor?: string | null`
- Dodać: `contractorId?: string | null`

W `TrackedCostWeb`:
- Usunąć: `contractor: string | null`
- Dodać: `contractorId: string | null` i `contractorName: string | null`

W `CreateCostRequest` (i `UpdateCostRequest` jeśli osobny):
- Usunąć: `contractor?: string | null`
- Dodać: `contractorId?: string | null`

### 3. Modyfikacja `src/types/project.types.ts` (lub odpowiednik)

Znajdź `ProjectCostListItemWeb`, `CreateProjectCostCommand`, `UpdateProjectCostCommand`.

W `ProjectCostListItemWeb`:
- Usunąć: `contractor: string | null`
- Dodać: `contractorId: string | null` i `contractorName: string | null`

W `CreateProjectCostCommand` i `UpdateProjectCostCommand`:
- Usunąć: `contractor?: string | null`
- Dodać: `contractorId?: string | null`

### 4. Modyfikacja `src/features/dashboard/types/projectDashboard.types.ts`

Znajdź `TrackedCostWeb`, `CreateTrackedCostRequest`, `UpdateTrackedCostRequest` (mogą być inne nazwy).

Analogiczne zmiany jak w punkcie 2 i 3:
- `contractor` string → `contractorId + contractorName` (dla web models)
- `contractor` string → `contractorId` (dla request types)

### 5. Nowy plik: `src/api/contractorApi.ts`

```typescript
import { axiosClient } from './axiosClient';
import type { ContractorWeb, ContractorListItemWeb, CreateContractorRequest, UpdateContractorRequest } from '../types/contractor.types';

export const contractorApi = {
  getAll: async (tenantId: string, search?: string): Promise<ContractorListItemWeb[]> => {
    const params = search ? { search } : undefined;
    const res = await axiosClient.get<ContractorListItemWeb[]>(
      `/tenants/${tenantId}/contractors`,
      { params }
    );
    return res.data;
  },

  getById: async (tenantId: string, contractorId: string): Promise<ContractorWeb> => {
    const res = await axiosClient.get<ContractorWeb>(
      `/tenants/${tenantId}/contractors/${contractorId}`
    );
    return res.data;
  },

  create: async (tenantId: string, data: CreateContractorRequest): Promise<ContractorWeb> => {
    const res = await axiosClient.post<ContractorWeb>(
      `/tenants/${tenantId}/contractors`,
      data
    );
    return res.data;
  },

  update: async (tenantId: string, contractorId: string, data: UpdateContractorRequest): Promise<ContractorWeb> => {
    const res = await axiosClient.put<ContractorWeb>(
      `/tenants/${tenantId}/contractors/${contractorId}`,
      data
    );
    return res.data;
  },

  delete: async (tenantId: string, contractorId: string): Promise<void> => {
    await axiosClient.delete(`/tenants/${tenantId}/contractors/${contractorId}`);
  },
};
```

### 6. Nowy plik: `src/hooks/queries/useContractors.ts`

```typescript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { contractorApi } from '../../api/contractorApi';
import type { CreateContractorRequest, UpdateContractorRequest } from '../../types/contractor.types';

export const contractorKeys = {
  all: ['contractors'] as const,
  byTenant: (tenantId: string) => ['contractors', tenantId] as const,
  detail: (tenantId: string, id: string) => ['contractors', tenantId, id] as const,
};

export function useContractors(tenantId: string | undefined, search?: string) {
  return useQuery({
    queryKey: [...contractorKeys.byTenant(tenantId ?? ''), search],
    queryFn: () => contractorApi.getAll(tenantId!, search),
    enabled: Boolean(tenantId),
  });
}

export function useCreateContractor(tenantId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateContractorRequest) =>
      contractorApi.create(tenantId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractorKeys.byTenant(tenantId) });
    },
  });
}

export function useUpdateContractor(tenantId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateContractorRequest }) =>
      contractorApi.update(tenantId, id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractorKeys.byTenant(tenantId) });
    },
  });
}

export function useDeleteContractor(tenantId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => contractorApi.delete(tenantId, id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractorKeys.byTenant(tenantId) });
    },
  });
}
```

### 7. Aktualizacja `src/hooks/queries/index.ts`

Dodać eksport:
```typescript
export { useContractors, useCreateContractor, useUpdateContractor, useDeleteContractor, contractorKeys } from './useContractors';
```

### 8. Modyfikacja `src/api/costTrackerApi.ts`

Znajdź funkcję `buildCostFormData` lub `createCost`/`updateCost`.
Zmienić: `form.append('contractor', data.contractor ?? '')` → `if (data.contractorId) form.append('contractorId', data.contractorId);`

### 9. Modyfikacja `src/features/dashboard/services/dashboardApi.ts`

Znajdź `createTrackedCost` i `updateTrackedCost`.
Zmienić: `formData.append('contractor', data.contractor)` → `if (data.contractorId) formData.append('contractorId', data.contractorId);`

### 10. Modyfikacja `src/api/projectApi.ts` (jeśli dotyczy)

Znajdź `createProjectCost` i `updateProjectCost`. 
Zmienić: `contractor` → `contractorId` w przesyłanych danych.

## Weryfikacja
```
npx tsc --noEmit 2>&1 | Select-Object -Last 20
```
TypeScript kompilacja musi zakończyć się bez błędów.
