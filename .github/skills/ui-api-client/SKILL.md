---
name: ui-api-client
description: "Tworzenie funkcji API przez axiosClient — wzorce GET/POST/PUT/DELETE i obsługa błędów. Użyj gdy tworzysz lub modyfikujesz klienta API (*Api.ts)."
---

# Skill: UI / Klienty API

## Opis
Tworzenie funkcji API przez axiosClient — wzorce GET/POST/PUT/DELETE i obsługa błędów.

## Kiedy używać
Użyj tego skilla gdy tworzysz lub modyfikujesz klienta API (*Api.ts).

---

## Lokalizacja

```
src/api/{domain}Api.ts              ← globalny klient per domena
src/features/{domain}/services/     ← klient domenowy feature
```

## Wzorzec globalnego klienta API

```typescript
// src/api/projectApi.ts
import { axiosClient } from './axiosClient';
import type {
    ProjectDetailsWeb,
    CreateProjectRequest,
    UpdateProjectRequest,
} from '../types/project.types';

export const projectApi = {
    getAll: async (tenantId: string): Promise<ProjectDetailsWeb[]> => {
        const response = await axiosClient.get<ProjectDetailsWeb[]>(
            `/tenants/${tenantId}/projects`
        );
        return response.data;
    },

    getDetails: async (
        tenantId: string,
        projectId: string
    ): Promise<ProjectDetailsWeb> => {
        const response = await axiosClient.get<ProjectDetailsWeb>(
            `/tenants/${tenantId}/projects/${projectId}`
        );
        return response.data;
    },

    create: async (
        tenantId: string,
        data: CreateProjectRequest
    ): Promise<ProjectDetailsWeb> => {
        const response = await axiosClient.post<ProjectDetailsWeb>(
            `/tenants/${tenantId}/projects`,
            data
        );
        return response.data;
    },

    update: async (
        tenantId: string,
        projectId: string,
        data: UpdateProjectRequest
    ): Promise<void> => {
        await axiosClient.put(
            `/tenants/${tenantId}/projects/${projectId}`,
            data
        );
    },

    delete: async (tenantId: string, projectId: string): Promise<void> => {
        await axiosClient.delete(
            `/tenants/${tenantId}/projects/${projectId}`
        );
    },
};
```

## Wzorzec domenowego klienta feature

```typescript
// src/features/dashboard/services/dashboardApi.ts
import { axiosClient } from '../../../api/axiosClient';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';

export const dashboardApi = {
    getProjectDashboard: async (
        tenantId: string,
        projectId: string
    ): Promise<ProjectDashboardWeb> => {
        const response = await axiosClient.get<ProjectDashboardWeb>(
            `/tenants/${tenantId}/projects/${projectId}/dashboard`
        );
        return response.data;
    },
};
```

## Upload pliku (multipart)

```typescript
uploadFiles: async (
    tenantId: string,
    projectId: string,
    files: File[]
): Promise<string[]> => {
    const formData = new FormData();
    files.forEach((file) => formData.append('files', file));

    const response = await axiosClient.post<string[]>(
        `/tenants/${tenantId}/projects/${projectId}/files`,
        formData,
        { headers: { 'Content-Type': 'multipart/form-data' } }
    );
    return response.data;
},
```

## Obsługa błędów API

```typescript
// axiosClient automatycznie rzuca dla status >= 400
// Obsługa w hooku:
try {
    const result = await projectApi.create(tenantId, data);
    return result;
} catch (err) {
    if (axios.isAxiosError(err)) {
        const message = err.response?.data?.message ?? 'Błąd serwera';
        setError(message);
    } else {
        setError('Nieoczekiwany błąd');
    }
    return null;
}
```

## Routing URL — konwencje

```
GET    /tenants/{id}/projects              → lista projektów
GET    /tenants/{id}/projects/{id}         → szczegóły projektu
POST   /tenants/{id}/projects              → nowy projekt
PUT    /tenants/{id}/projects/{id}         → aktualizacja
DELETE /tenants/{id}/projects/{id}         → usunięcie
PATCH  /tenants/{id}/projects/{id}/status  → zmiana statusu
```

## Zasady

- Zawsze używaj `axiosClient` — nie `fetch`, nie `axios` bezpośrednio
- Zawsze typuj response: `axiosClient.get<TypOdpowiedzi>(...)`
- Zwracaj `response.data` — nie cały response
- Nazewnictwo metod: `getAll`, `getDetails`, `create`, `update`, `delete`, `{akcja}`
- Zakaz `any` w typach request/response
- Osobny plik per domena
