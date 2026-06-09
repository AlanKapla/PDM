# Prompt: ai-edit-ui-01 — Typy TypeScript + API client

## Cel

Dodać typy TypeScript dla feature AI Cost Estimate Edit oraz nowe metody w API client.

## Pliki do modyfikacji

### 1. `ProjectDataManagementUI/src/types/costEstimate.types.new.ts`

Dodać po istniejących typach AI (około linii 659):

```typescript
// ─── AI Cost Estimate Edit ───────────────────────────────────────────────

export interface AICostEditRequestDto {
  userRequest: string;
}

export interface AICostEditPreviewDto {
  summary: string;
  suggestedName: string | null;
  suggestedDescription: string | null;
  groups: AIGroupPreviewDto[];
  warnings: string[];
}

export interface ApplyAICostEditDto {
  preview: AICostEditPreviewDto;
}
```

Uwaga: `AIGroupPreviewDto` już istnieje w tym pliku — nie trzeba go tworzyć.

### 2. `ProjectDataManagementUI/src/api/costEstimateApi.ts`

Dodać po istniejących metodach AI (około linii 455, po `createFromAIPreview`):

```typescript
// ─── AI Edit ────────────────────────────────────────────────────────────

generateAIEditPreview: async (
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  request: AICostEditRequestDto
): Promise<AICostEditPreviewDto> => {
  const response = await axiosClient.post<AICostEditPreviewDto>(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/ai/edit-preview`,
    request
  );
  return response.data;
},

applyAIEdit: async (
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  body: ApplyAICostEditDto
): Promise<void> => {
  await axiosClient.post(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/ai/apply-edit`,
    body
  );
},
```

Importy dodać na górze pliku (jeśli nie istnieją):
```typescript
import type {
  AICostEditRequestDto,
  AICostEditPreviewDto,
  ApplyAICostEditDto,
} from '../types/costEstimate.types.new';
```

## Weryfikacja

1. TypeScript kompiluje bez błędów (`tsc -b`)
2. Typy są zgodne z backendowymi DTOs
3. API metody mają poprawne URL-e i typy
