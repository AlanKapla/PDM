# UI Fix 02 — API client + hooki React Query

## Cel
Warstwa danych: `technicalDocumentationApi.ts` + hooki `useTechnicalDocumentation*` z query keys.

## Workspace
`C:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI`

## Skills
- `.cursor/skills/ui-api-client/SKILL.md`
- `.cursor/skills/ui-hooks/SKILL.md`

## Zależności
- **ui-fix-01** — typy

## Pliki referencyjne
- `src/api/costTrackerApi.ts` — wzorzec axiosClient
- `src/hooks/queries/useCostTracker.ts` — query keys + mutations
- `src/hooks/queries/index.ts` — barrel export

---

## 1. `src/api/technicalDocumentationApi.ts`

Stała lokalna (nie używaj `FILE_UPLOAD` — ma 10 MB):
```typescript
const MAX_FILE_SIZE_BYTES = 52_428_800; // 50 MB
```

Funkcje:

| Metoda | HTTP | Endpoint |
|--------|------|----------|
| `getCount` | GET | `/tenants/{t}/projects/{p}/technical-documentation/count` → `number` |
| `getList` | GET | `/tenants/{t}/projects/{p}/technical-documentation` → `TechnicalDocumentationListItemWeb[]` |
| `getById` | GET | `/tenants/{t}/projects/{p}/technical-documentation/{id}` → `TechnicalDocumentationDetailsWeb` |
| `create` | POST multipart | `/tenants/{t}/projects/{p}/technical-documentation` → `{ id: string }` (HTTP **202**) |
| `retry` | POST | `/tenants/{t}/projects/{p}/technical-documentation/{id}/retry` → `void` (HTTP **202**) |

### `create` — FormData
```typescript
form.append('name', data.name);
if (data.description) form.append('description', data.description);
data.files.forEach((file) => form.append('files', file));
```

Walidacja klienta przed wysłaniem (opcjonalnie w hooku):
- MIME: `application/pdf`, `image/jpeg`
- Rozmiar ≤ 50 MB

Eksportuj obiekt:
```typescript
export const technicalDocumentationApi = { getCount, getList, getById, create, retry };
```

## 2. `src/hooks/queries/useTechnicalDocumentation.ts`

### Query keys
```typescript
export const technicalDocumentationKeys = {
  all: ['technicalDocumentation'] as const,
  list: (tenantId: string, projectId: string) =>
    [...technicalDocumentationKeys.all, 'list', tenantId, projectId] as const,
  detail: (tenantId: string, projectId: string, id: string) =>
    [...technicalDocumentationKeys.all, 'detail', tenantId, projectId, id] as const,
  count: (tenantId: string, projectId: string) =>
    [...technicalDocumentationKeys.all, 'count', tenantId, projectId] as const,
};
```

### Hooki
| Hook | Typ | Uwagi |
|------|-----|-------|
| `useTechnicalDocumentationCount` | `useQuery<number>` | `enabled` gdy tenantId + projectId + uprawnienie |
| `useTechnicalDocumentationList` | `useQuery` | `refetchInterval: 5000` gdy jakikolwiek item ma status Pending/Processing (polling fallback) |
| `useTechnicalDocumentationDetails` | `useQuery` | `enabled` gdy id present |
| `useCreateTechnicalDocumentation` | `useMutation` | onSuccess: invalidate list + count |
| `useRetryTechnicalDocumentation` | `useMutation` | onSuccess: invalidate detail + list |

Używaj `isPending` (React Query 5) w mutacjach.

## 3. Barrel export

W `src/hooks/queries/index.ts` dodaj eksport:
```typescript
export {
  technicalDocumentationKeys,
  useTechnicalDocumentationCount,
  useTechnicalDocumentationList,
  useTechnicalDocumentationDetails,
  useCreateTechnicalDocumentation,
  useRetryTechnicalDocumentation,
} from './useTechnicalDocumentation';
```

## Weryfikacja
```powershell
npx tsc --noEmit
```

## Następny krok
Hub + global toast w **ui-fix-03**.
