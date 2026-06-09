# Prompt UI-01: Typy TypeScript + API client

## Cel
Dodaj typy TypeScript i metody API client dla feature "Generuj kosztorys z AI".

---

## Plik 1: Nowe typy — `src/types/costEstimate.types.new.ts`

Dopisz na końcu pliku (NIE modyfikuj istniejących typów):

```typescript
// ========== AI COST ESTIMATE GENERATION ==========

/**
 * Dane wejściowe od użytkownika — opis inwestycji.
 * Mapuje się na AICostEstimateRequestWeb po stronie API.
 */
export interface AICostEstimateRequestDto {
  /** ID szablonu wybranego przez użytkownika */
  templateId: string;
  /** Co budujesz? (wolny tekst) */
  investmentType: string;
  /** Stan wykończenia */
  finishingStandard?: string;
  /** Szacowany budżet brutto w PLN */
  budget?: number;
  /** Powierzchnia/zakres */
  area?: number;
  /** Jednostka powierzchni (m², mb, szt) */
  areaUnit?: string;
  /** Lokalizacja inwestycji */
  location?: string;
  /** Rok ukończenia */
  completionYear?: number;
  /** Dodatkowe wymagania */
  additionalRequirements?: string;
}

/**
 * Wartość pola wygenerowana przez AI.
 */
export interface AIFieldValueDto {
  fieldDefinitionId: string;
  decimalValue?: number;
  stringValue?: string;
  boolValue?: boolean;
  dateTimeValue?: string;
}

/**
 * Pozycja kosztorysowa w podglądzie AI.
 */
export interface AIItemPreviewDto {
  tempId: string;
  name: string;
  order: number;
  fieldValues: AIFieldValueDto[];
}

/**
 * Grupa kosztorysowa w podglądzie AI.
 */
export interface AIGroupPreviewDto {
  tempId: string;
  parentTempId?: string | null;
  name: string;
  order: number;
  fieldValues: AIFieldValueDto[];
  items: AIItemPreviewDto[];
}

/**
 * Podgląd kosztorysu wygenerowanego przez AI.
 * NIE jest zapisany w bazie danych — służy do prezentacji i zatwierdzenia przez użytkownika.
 */
export interface AICostEstimatePreviewDto {
  templateId: string;
  suggestedName: string;
  suggestedDescription?: string | null;
  groups: AIGroupPreviewDto[];
  warnings: string[];
}

/**
 * Żądanie zapisu zatwierdzonego podglądu AI.
 */
export interface CreateCostEstimateFromAIPreviewDto {
  name: string;
  description?: string;
  preview: AICostEstimatePreviewDto;
}
```

---

## Plik 2: API client — `src/api/costEstimateApi.ts`

Dopisz dwie nowe metody do obiektu `costEstimateApi` (za istniejącymi metodami):

```typescript
  /**
   * Generuje podgląd kosztorysu przez AI.
   * NIE zapisuje do bazy danych — zwraca podgląd do zatwierdzenia.
   */
  generateAIPreview: async (
    tenantId: string,
    projectId: string,
    request: import('../types/costEstimate.types.new').AICostEstimateRequestDto
  ): Promise<import('../types/costEstimate.types.new').AICostEstimatePreviewDto> => {
    const response = await axiosClient.post<import('../types/costEstimate.types.new').AICostEstimatePreviewDto>(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/generate-ai-preview`,
      request
    );
    return response.data;
  },

  /**
   * Zapisuje kosztorys zatwierdzony przez użytkownika z podglądu AI.
   * Zwraca ID nowo utworzonego kosztorysu.
   */
  createFromAIPreview: async (
    tenantId: string,
    projectId: string,
    body: import('../types/costEstimate.types.new').CreateCostEstimateFromAIPreviewDto
  ): Promise<string> => {
    const response = await axiosClient.post<string>(
      `/tenants/${tenantId}/projects/${projectId}/cost-estimate/create-from-ai-preview`,
      body
    );
    return response.data;
  },
```

**Uwaga:** Dodaj metody wewnątrz obiektu `costEstimateApi = { ... }`, przed zamykającym `}`.

---

## Weryfikacja
```
npx tsc --noEmit
```
Oczekiwany wynik: Exit 0, brak błędów dla nowych plików.
