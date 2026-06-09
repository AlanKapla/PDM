# UI-02: Nowa metoda API w projectApi.ts

## Zadanie
Dodaj metodę `generateScheduleFromEstimateAI` do `projectApi.ts`, która wywołuje nowy backendowy endpoint.

## Plik do modyfikacji
`01-Applications/ProjectDataManagementUI/src/api/projectApi.ts`

## Miejsce
Dodaj **po** istniejącej metodzie `syncWorkScheduleWithEstimate` (linia 376), **przed** metodą `getMyAssignedWorks` (linia 379).

## Kod do dodania
```typescript
  // Generuj harmonogram z kosztorysu wspierany przez AI
  generateScheduleFromEstimateAI: async (
    tenantId: string,
    projectId: string,
    workScheduleId: string,
    data: GenerateScheduleFromEstimateAIRequest
  ): Promise<WorkScheduleDetailsWeb> => {
    const response = await axiosClient.post<WorkScheduleDetailsWeb>(
      `/tenants/${tenantId}/projects/${projectId}/work-schedule/${workScheduleId}/generate-from-ai`,
      {
        tenantId,
        projectId,
        workScheduleId,
        overallStartDate: data.overallStartDate,
        overallEndDate: data.overallEndDate,
      }
    );
    return response.data;
  },
```

## Import do modyfikacji
Na górze pliku (linia 2 lub okolice istniejących importów), dodaj `GenerateScheduleFromEstimateAIRequest` do importu z `workSchedule.types.ts`:
```typescript
import type { WorkScheduleDetailsWeb, GenerateScheduleFromEstimateAIRequest } from "../types/workSchedule.types";
```

Jeśli import `WorkScheduleDetailsWeb` już istnieje, po prostu dodaj `GenerateScheduleFromEstimateAIRequest` do istniejącego importu.

## Uwagi
- Metoda zwraca `Promise<WorkScheduleDetailsWeb>` bezpośrednio (response.data), a nie cały axios response
- Użyj istniejącego `axiosClient` (już zaimportowany)
- Użyj istniejącego typu `WorkScheduleDetailsWeb` (już zaimportowany w innych miejscach pliku)
