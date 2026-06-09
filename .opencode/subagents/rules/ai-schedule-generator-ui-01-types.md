# UI-01: Nowy typ TypeScript dla AI Schedule Generator

## Zadanie
Dodaj interfejs `GenerateScheduleFromEstimateAIRequest` do pliku typów work schedule.

## Plik do modyfikacji
`01-Applications/ProjectDataManagementUI/src/types/workSchedule.types.ts`

## Miejsce
Dodaj przed lub po istniejącym `CreateWorkScheduleCommand` interfejsem (około linii 90-100).

## Kod do dodania
```typescript
export interface GenerateScheduleFromEstimateAIRequest {
    /** ISO 8601 date string — overall project start date */
    overallStartDate: string;
    /** ISO 8601 date string — overall project end date */
    overallEndDate: string;
}
```

## Uwagi
- Nie usuwaj żadnych istniejących typów
- Nie modyfikuj istniejących typów
- Tylko dodaj nowy interfejs
