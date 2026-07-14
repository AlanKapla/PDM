# UI Fix 01 — Typy TypeScript + API Client + Hook useAICostDocumentParser

## Cel
Stwórz warstwę danych UI dla feature AI Cost Document Import:
1. Typy TypeScript (`src/types/ai.types.ts`)
2. Klient API (`src/api/aiCostApi.ts`)
3. Hook React Query (`src/hooks/useAICostDocumentParser.ts`)

## Krok 1 — Przeczytaj przed implementacją

Przeczytaj:
- `src/api/costTrackerApi.ts` — PEŁNA treść (wzorzec axiosClient, FormData, typy)
- `src/hooks/queries/useCostTracker.ts` — wzorzec useMutation lub useQuery
- Sprawdź jak skonfigurowany jest axiosClient: `src/api/axiosClient.ts` lub `src/lib/` — sprawdź baseURL (czy zawiera `/api/` czy nie)
- `src/types/costTracker.types.ts` — żeby zobaczyć wzorzec definicji typów

## Krok 2 — Stwórz src/types/ai.types.ts

```typescript
export type CostDocumentType = 'TrackedCost' | 'ProjectCost';

export interface SuggestedContractor {
  name: string;
  nip?: string;
  address?: string;
}

export interface ParsedCostDto {
  /** Nazwa kosztu — co zostało zakupione */
  name: string;
  /** Rozszerzony opis z detalami */
  description?: string;
  /** Numer faktury/rachunku */
  number?: string;
  /** Suma netto całego dokumentu */
  net?: number;
  /** Suma brutto całego dokumentu */
  gross?: number;
  /** Data wystawienia (ISO 8601 string) */
  date?: string;
  /** GUID kontrahenta — wypełniony gdy contractorFound = true */
  contractorId?: string;
  /** Nazwa kontrahenta z dokumentu */
  contractorName?: string;
  /** NIP kontrahenta z dokumentu */
  contractorNip?: string;
  /** Adres kontrahenta z dokumentu */
  contractorAddress?: string;
  /** Czy kontrahent znaleziony w bazie */
  contractorFound: boolean;
  /** Sugestia nowego kontrahenta gdy nie znaleziono w bazie */
  suggestedContractor?: SuggestedContractor;
  /** Pewność AI 0–1 */
  confidence: number;
  /** Surowy tekst z dokumentu (debug) */
  rawText?: string;
}

export interface ParseCostDocumentRequest {
  file: File;
  costType: CostDocumentType;
}
```

## Krok 3 — Stwórz src/api/aiCostApi.ts

Wzoruj się DOKŁADNIE na wzorcu z costTrackerApi.ts.

```typescript
import type { ParsedCostDto, ParseCostDocumentRequest } from '../types/ai.types';
// Import axiosClient tak jak w costTrackerApi.ts

export const aiCostApi = {
  /**
   * Parsuje dokument kosztowy przez AI.
   * POST /tenants/{tenantId}/projects/{projectId}/ai/cost/parse
   * Nie zapisuje do bazy — zwraca tylko sugestię do zatwierdzenia przez usera.
   */
  parseCostDocument: async (
    tenantId: string,
    projectId: string,
    data: ParseCostDocumentRequest
  ): Promise<ParsedCostDto> => {
    const form = new FormData();
    form.append('file', data.file);
    form.append('costType', data.costType);

    const res = await axiosClient.post<ParsedCostDto>(
      `tenants/${tenantId}/projects/${projectId}/ai/cost/parse`,
      form,
      {
        headers: { 'Content-Type': 'multipart/form-data' },
        timeout: 60_000,  // 60 sekund — GPT-4o Vision może trwać długo
      }
    );
    return res.data;
  },
};
```

Uwaga: Dopasuj ścieżkę URL do baseURL axiosClient:
- Jeśli baseURL = `http://localhost:PORT/api/` → użyj `tenants/${tenantId}/...`
- Jeśli baseURL = `http://localhost:PORT/` → użyj `api/tenants/${tenantId}/...`

## Krok 4 — Stwórz src/hooks/useAICostDocumentParser.ts

Wzoruj się na wzorcu useMutation z React Query 5.

```typescript
import { useMutation } from '@tanstack/react-query';
import { aiCostApi } from '../api/aiCostApi';
import type { ParsedCostDto, ParseCostDocumentRequest } from '../types/ai.types';

interface UseAICostDocumentParserParams {
  tenantId: string;
  projectId: string;
}

export function useAICostDocumentParser({
  tenantId,
  projectId,
}: UseAICostDocumentParserParams) {
  return useMutation<ParsedCostDto, Error, ParseCostDocumentRequest>({
    mutationFn: (data: ParseCostDocumentRequest) =>
      aiCostApi.parseCostDocument(tenantId, projectId, data),
  });
}
```

Wzorzec React Query 5: używaj `isPending` (nie `isLoading`) przy destrukturyzacji.

## Weryfikacja

```
npx tsc --noEmit
```
Nie powinno być błędów TypeScript w nowych plikach.
