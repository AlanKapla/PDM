# UI Fix 01: Nowe typy TypeScript

## Kontekst
Feature: costestimate-full-refactor — patrz `.opencode/features/costestimate-full-refactor.md`

Dostosowanie typów TypeScript do nowej architektury: direct properties zamiast FieldValues, AdditionalFieldValues zamiast FieldDefinition/FieldScope, pliki na itemie.

## Do zrobienia

### 1. Modyfikacja `costEstimate.types.new.ts`

#### Dodaj nowe typy:

```typescript
// === ADDITIONAL FIELDS ===

export enum AdditionalFieldType {
  String = 0,
  Decimal = 1,
  Boolean = 2,
  DateTime = 3,
}

/**
 * Definicja pola dodatkowego w kosztorysie
 */
export interface CostEstimateAdditionalFieldWeb {
  id: string;
  costEstimateId: string;
  name: string;           // "Kod CPV", "Uwagi"
  fieldType: AdditionalFieldType;
  order: number;
  createdAt: string;
  updatedAt?: string;
}

/**
 * Wartość pola dodatkowego (wspólna dla grup i pozycji)
 */
export interface CostEstimateAdditionalFieldValueWeb {
  id: string;
  additionalFieldId: string;
  stringValue?: string;
  decimalValue?: number;
  boolValue?: boolean;
  dateTimeValue?: string; // ISO 8601
}

/**
 * Plik na pozycji (zastępuje CostEstimateFieldFileWeb)
 */
export interface CostEstimateItemFileWeb {
  id: string;
  itemId: string;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  order: number;
  sasUriPreview: string | null;
  sasUriDownload: string | null;
  createdAt: string;
}
```

#### Zmodyfikuj `CostEstimateItemWeb`:

```typescript
export interface CostEstimateItemWeb {
  id: string;
  groupId: string;
  parentItemId?: string;
  relationType: number;  // ItemRelationType: None=0, Option=1, Component=2 — wymagane, nie opcjonalne
  order: number;
  name: string;           // NOWE — direct property
  quantity?: number;      // NOWE — direct property
  unit?: string;          // NOWE
  unitPriceNet?: number;  // NOWE
  vatRate?: number;       // NOWE
  unitPriceGross?: number;// NOWE
  netValue?: number;
  grossValue?: number;
  vatValue?: number;
  isSelected: boolean;    // NOWE — default true
  isStageWork: boolean;   // NOWE — default false
  additionalFieldValues: CostEstimateAdditionalFieldValueWeb[]; // NOWE
  options?: CostEstimateItemWeb[];
  components?: CostEstimateItemWeb[];
  files?: CostEstimateItemFileWeb[]; // NOWE
  createdAt: string;
  updatedAt?: string;
}
```

#### Zmodyfikuj `CostEstimateGroupWeb`:

```typescript
export interface CostEstimateGroupWeb {
  id: string;
  parentGroupId?: string;
  level: number;
  order: number;
  name: string;                            // Zamiast FieldValues
  totalNet?: number;
  totalGross?: number;
  totalVat?: number;
  additionalFieldValues: CostEstimateAdditionalFieldValueWeb[]; // NOWE
  lastCalculatedAt?: string;
  childGroups: CostEstimateGroupWeb[];
  items: CostEstimateItemWeb[];
  createdAt: string;
  updatedAt?: string;
}
```

#### Zmodyfikuj `CostEstimateDetailsWeb`:

```typescript
export interface CostEstimateDetailsWeb {
  id: string;
  tenantId: string;
  projectId: string;
  selectedCurrencyCode: string;
  selectedCurrencySymbol?: string;
  name: string;
  description?: string;
  status: CostEstimateStatus;
  rootGroups: CostEstimateGroupWeb[];
  additionalFields: CostEstimateAdditionalFieldWeb[]; // NOWE — schema pól dodatkowych
  totalNet?: number;
  totalGross?: number;
  totalVat?: number;
  createdAt: string;
  updatedAt?: string;
  lastCalculatedAt?: string;
  ownerId: string;
  ownerName: string;
  workScheduleId?: string;
  accessLevel: CostEstimateAccessLevel;
  sharedWithUsers: CostEstimateShareWeb[];
}
```

**Usuń**: `schema: CostEstimateSchemaWeb` z `CostEstimateDetailsWeb` (zastąpione przez `additionalFields`)

#### Zmodyfikuj DTO:

```typescript
export interface CostEstimateItemDto {
  id?: string;
  parentItemId?: string;
  relationType: number;
  order: number;
  name?: string;
  quantity?: number;
  unit?: string;
  unitPriceNet?: number;
  vatRate?: number;
  additionalFieldValues: CostEstimateAdditionalFieldValueDto[]; // NOWE
  options?: CostEstimateItemDto[];
  components?: CostEstimateItemDto[];
}

export interface CostEstimateAdditionalFieldValueDto {
  id?: string;
  additionalFieldId: string;
  stringValue?: string;
  decimalValue?: number;
  boolValue?: boolean;
  dateTimeValue?: string;
}
```

```typescript
export interface CostEstimateGroupDto {
  id?: string;
  parentGroupId?: string;
  level: number;
  order: number;
  name?: string;
  additionalFieldValues: CostEstimateAdditionalFieldValueDto[];
  items: CostEstimateItemDto[];
  childGroups: CostEstimateGroupDto[];
}
```

### 2. Usuń stare typy (oznacz jako deprecated lub usuń)

- `CostEstimateSchemaWeb` — usuń
- `CostEstimateFieldDefinitionWeb` — usuń
- `CostEstimateFieldFileWeb` (stary) — usuń
- `CostEstimateFieldValueWeb` — usuń
- `CostEstimateFieldTypeConfigWeb` — usuń
- `FieldScope` enum — usuń
- `getFieldValueTypeFromFieldType`, `getFieldValueType`, `getFieldValueAsString`, `getFieldValueAsNumber`, `getFieldValueAsBoolean`, `convertFieldValueWebToDto`, `isFieldValueEmpty`, `convertItemWebToDto`, `convertGroupWebToDto`, `convertDetailsWebToUpdateDto` — oznaczone jako deprecated lub usunięte

**Uwaga**: Sprawdź czy stare typy są używane gdzieś indziej — jeśli tak, zostaw aliasy tymczasowo.

### Build

```powershell
npm run build
```
Jeśli build failed, przerwij i zgłoś błędy.
