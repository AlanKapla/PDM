# Feature: AI Cost Document Import (JPG/PDF → ProjectCost / TrackedCost)

## Cel

User uploaduje zdjęcie (JPG/PNG) lub dokument PDF faktury/rachunku.
Agent AI (GPT-4o Vision) wyciąga dane kosztu i prezentuje je użytkownikowi do walidacji w UI.
User może edytować dane, zatwierdzić lub anulować — po zatwierdzeniu encja jest zapisywana przez istniejące komendy CQRS.

## Zakres

### API — Business.AIAgent
- Nowy tool `ParseCostDocumentTool` w `Business.AIAgent/Tools/CostDocument/`
- Nowa definicja agenta `cost_document_parser.md` w `Resources/Agents/sub_agents/`
- Używa Azure OpenAI GPT-4o Vision (model `gpt-4o`)
- Obsługuje: JPG, PNG, PDF (konwersja PDF→base64 image)

### API — CQRS
- Nowa query: `ParseCostDocumentQuery` → zwraca `ParsedCostDto`
- Handler: `ParseCostDocumentQueryHandler`
- **Nie zapisuje do bazy** — tylko parsuje i zwraca sugestię

### API — WebApi
- Nowy kontroler: `AICostController`
- Endpoint: `POST /api/projects/{projectId}/ai/cost/parse`
- Przyjmuje: multipart/form-data (plik JPG/PNG/PDF + typ kosztu: TrackedCost/ProjectCost)
- Zwraca: `ParsedCostDto`

### API — Business (opcjonalnie)
- Serwis `ContractorSearchService` lub rozszerzenie istniejącego — wyszukiwanie kontrahenta po nazwie, NIP, adresie

### UI — Typy
- `ParsedCostDto` — typ TypeScript

### UI — API Client
- `aiCostApi.ts` — klient API dla endpointu parsowania

### UI — Hook
- `useAICostDocumentParser` — hook zarządzający stanem parsowania

### UI — Komponent
- `AICostImportModal` — modal z procesem: upload → loading → edycja danych → zatwierdzenie
- Integracja w `CostFormModal` (TrackedCost) — przycisk "Importuj z dokumentu"
- Integracja w formularzach ProjectCost — ten sam przycisk

## Specyfikacja ParsedCostDto

```typescript
interface ParsedCostDto {
  // Wyciągnięte przez AI
  name: string;              // co zostało zakupione
  description?: string;      // rozszerzenie nazwy z detalami
  number?: string;           // numer faktury/rachunku
  net?: number;              // suma netto całego dokumentu
  gross?: number;            // suma brutto całego dokumentu
  date?: string;             // data wystawienia (ISO)
  
  // Kontrahent
  contractorId?: string;     // GUID jeśli znaleziony w bazie
  contractorName?: string;   // nazwa z dokumentu
  contractorNip?: string;    // NIP z dokumentu
  contractorAddress?: string; // adres z dokumentu
  contractorFound: boolean;  // czy znaleziono kontrahenta w bazie
  
  // Sugestia dla nowego kontrahenta (jeśli nie znaleziono)
  suggestedContractor?: {
    name: string;
    nip?: string;
    address?: string;
  };
  
  // Metadane
  confidence: number;        // 0-1, pewność AI
  rawText?: string;          // surowy tekst z dokumentu (debug)
}
```

## Reguły biznesowe

1. **Kwoty**: AI wyciąga SUMĘ netto i brutto całego dokumentu (nie pozycji)
2. **Nazwa**: Bazuje na tym co zostało zakupione (np. "Materiały budowlane", "Usługa instalacyjna")
3. **Opis**: Rozszerzenie nazwy z drobnymi detalami (konkretne pozycje, ilości itp.)
4. **Kontrahent**: Szukaj po WSZYSTKICH możliwych parametrach (nazwa, NIP, adres)
   - Jeśli znaleziono → wypełnij `contractorId` i `contractorFound: true`
   - Jeśli nie znaleziono → `contractorFound: false` + `suggestedContractor` z danymi z dokumentu
   - UI sugeruje user'owi dodanie nowego kontrahenta
5. **Zawsze user potwierdza** — AI tylko sugeruje, user zatwierdza/edytuje przed zapisem

## Przepływ (UX)

```
1. User klika "Importuj z dokumentu" w CostFormModal / ProjectCost form
2. Modal AICostImportModal: krok 1 — upload pliku (JPG/PNG/PDF)
3. Loading spinner + "AI analizuje dokument..."
4. Krok 2 — formularz z danymi wyciągniętymi przez AI (edytowalne)
   - Jeśli contractor NOT found → baner z info + przycisk "Dodaj kontrahenta"
5. User edytuje jeśli potrzeba → "Potwierdź i dodaj koszt"
6. Zapis przez istniejące komendy (CreateTrackedCostCommand / CreateProjectCostCommand)
   - ProjectCost zapisuje w statusie Draft
```

## PermissionCodes

- `CostType.ProjectCost` → `"PROJECT.COSTS"` (`PermissionCodes.ProjectCosts`)
- `CostType.TrackedCost` → `"PROJECT.DASHBOARD_TRACKER"` (`PermissionCodes.ProjectDashboardTracker`)
- Endpoint sprawdza permission na podstawie przesłanego `costType` (walidacja w handlerze przed wywołaniem AI)

## Stack

- Azure OpenAI GPT-4o (model: `gpt-4o`)
- Base64 encoding pliku dla Vision API
- PDF → `Docnet.Core` (renderowanie strony 1 PDF → bitmap PNG → Vision API)

## Zależności

- Istniejące CQRS: `CreateTrackedCostCommand`, `CreateProjectCostCommand`
- Istniejące kontrolery: `CostTrackerController`, `ProjectCostController`
- Istniejące UI: `CostFormModal`, `CostFormDrawer`
- `AzureAIAgentOptions` — konfiguracja Azure OpenAI już w projekcie
