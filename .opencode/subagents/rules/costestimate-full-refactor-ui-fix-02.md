# UI Fix 02: Nowy API client

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Dostosowanie `costEstimateApi.ts` do nowej struktury endpointów.

## Do zrobienia

### 1. Modyfikacja `src/api/costEstimateApi.ts`

#### Dodaj nowe funkcje API:

```typescript
// === ADDITIONAL FIELDS SCHEMA ===

export async function getAdditionalFields(
  tenantId: string,
  projectId: string,
  costEstimateId: string
): Promise<CostEstimateAdditionalFieldWeb[]> {
  const { data } = await axiosClient.get(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/additional-fields`
  );
  return data;
}

export async function addAdditionalField(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  data: { name: string; fieldType: AdditionalFieldType; order?: number }
): Promise<string> {
  const { data: id } = await axiosClient.post(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/additional-fields`,
    data
  );
  return id;
}

export async function updateAdditionalField(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  fieldId: string,
  data: { name?: string; fieldType?: AdditionalFieldType; order?: number }
): Promise<void> {
  await axiosClient.put(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/additional-fields/${fieldId}`,
    data
  );
}

export async function deleteAdditionalField(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  fieldId: string
): Promise<void> {
  await axiosClient.delete(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/additional-fields/${fieldId}`
  );
}

export async function reorderAdditionalFields(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  fieldIds: string[]
): Promise<void> {
  await axiosClient.post(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/additional-fields/reorder`,
    { fieldIds }
  );
}
```

#### Dodaj funkcje dla wartości pól dodatkowych:

```typescript
export async function upsertGroupAdditionalField(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  groupId: string,
  data: { additionalFieldId: string; stringValue?: string | null; decimalValue?: number | null; boolValue?: boolean | null; dateTimeValue?: string | null }
): Promise<string> {
  const { data: id } = await axiosClient.patch(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/groups/${groupId}/additional-fields`,
    data
  );
  return id;
}

export async function upsertItemAdditionalField(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  itemId: string,
  data: { additionalFieldId: string; stringValue?: string | null; decimalValue?: number | null; boolValue?: boolean | null; dateTimeValue?: string | null }
): Promise<string> {
  const { data: id } = await axiosClient.patch(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}/additional-fields`,
    data
  );
  return id;
}
```

#### Dodaj endpointy dla base fields (PATCH item/group):

```typescript
export async function updateItemBaseFields(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  itemId: string,
  data: { name?: string; quantity?: number | null; unit?: string | null; unitPriceNet?: number | null; vatRate?: number | null }
): Promise<void> {
  await axiosClient.patch(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}`,
    data
  );
}

export async function updateGroupBaseFields(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  groupId: string,
  data: { name?: string }
): Promise<void> {
  await axiosClient.patch(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/groups/${groupId}`,
    data
  );
}

export async function setItemIsSelected(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  itemId: string,
  isSelected: boolean
): Promise<void> {
  await axiosClient.patch(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}/select`,
    { isSelected }
  );
}
```

#### Dodaj endpointy plików (uproszczone):

```typescript
export async function uploadItemFiles(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  itemId: string,
  files: File[]
): Promise<string[]> {
  const formData = new FormData();
  files.forEach((file) => formData.append('files', file));
  const { data: ids } = await axiosClient.post(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}/files`,
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  );
  return ids;
}

export async function deleteItemFile(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  itemId: string,
  fileId: string
): Promise<void> {
  await axiosClient.delete(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}/files/${fileId}`
  );
}

export async function replaceItemFiles(
  tenantId: string,
  projectId: string,
  costEstimateId: string,
  itemId: string,
  files: File[]
): Promise<string[]> {
  const formData = new FormData();
  files.forEach((file) => formData.append('files', file));
  const { data: ids } = await axiosClient.put(
    `/tenants/${tenantId}/projects/${projectId}/cost-estimate/${costEstimateId}/items/${itemId}/files`,
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  );
  return ids;
}
```

#### Usuń stare funkcje API:

- `upsertGroupField` — zastąpione przez `updateGroupBaseFields` + `upsertGroupAdditionalField`
- `upsertItemField` — zastąpione przez `updateItemBaseFields` + `upsertItemAdditionalField`
- `addFieldDefinition` — zastąpione przez `addAdditionalField`
- `updateFieldDefinition` — zastąpione przez `updateAdditionalField`
- `deleteFieldDefinition` — zastąpione przez `deleteAdditionalField`
- `reorderFieldDefinitions` — zastąpione przez `reorderAdditionalFields`
- `uploadCostEstimateItemFiles` (stare z fieldDefinitionId) — zastąpione przez `uploadItemFiles`

### Build

```powershell
npm run build
```
Jeśli build failed, przerwij i zgłoś błędy.
