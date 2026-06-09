# UI Audit Report — AI Schedule Generator

**Feature**: `ai-schedule-generator`
**Date**: 2026-06-09
**Scope**: Pełny audyt warstwy UI pod kątem implementacji generowania harmonogramu z kosztorysu wspieranego przez AI.

---

## BLOK 1 — Stan obecny UI

### 1.1 WorkScheduleFormModal.tsx (1796 linii)

| Komponent/Strona | Lokalizacja | Opis | Powiązane z feature |
|-----------------|------------|------|---------------------|
| `WorkScheduleFormModal` | `src/components/WorkScheduleFormModal.tsx` | Główny modal tworzenia/edycji harmonogramu | **Kluczowy** — tu będą dodane pola ram czasowych i przycisk AI |
| `ConstrainedDateInput` | `src/components/ConstrainedDateInput.tsx` | Wrapper na `<Input type="date">` z walidacją dat | Wzorzec do naśladowania dla pól date range |
| `projectApi` | `src/api/projectApi.ts` | Klient API dla projektów i harmonogramów | **Kluczowy** — tu dodany nowy endpoint |
| `WorkScheduleFormModal` (usage) | `src/pages/CostEstimateEditPage.tsx` linie 1568-1581 | Otwierany z kosztorysu z `initialCostEstimateId` | Punkt wejścia dla flow "z kosztorysu" |
| `WorkScheduleFormModal` (usage) | `src/pages/ProjectSchedules.tsx` linia 425 | Otwierany z listy harmonogramów | Drugi punkt wejścia |
| `WorkScheduleFormModal` (usage) | `src/pages/ProjectDetails.tsx` linia 884 | Otwierany z dashboardu projektu | Trzeci punkt wejścia |

### 1.2 Istniejący przepływ 'linked' (kosztorys)

**Tryb 'linked'** w modalu działa następująco:

1. **Otwarcie modala** z kosztorysu (`CostEstimateEditPage.tsx` linie 1568-1581):
   - `mode='create'`, `initialCostEstimateId=estimateId`, `initialCostEstimateName=details.name`
   - Zmienna `isFromCostEstimate = mode === 'create' && !!initialCostEstimateId` (linia 1376)

2. **Gdy `isFromCostEstimate = true`**:
   - Modal ma rozmiar `md` (linia 1388): `size={{ base: "full", md: "md" }}`
   - Tytuł: "Nowy harmonogram z kosztorysu" (linia 1378-1379)
   - Pokazuje tylko: pole nazwy (linie 1395-1407) + alert info o kosztorysie (linie 1443-1451)
   - **Nie pokazuje**: sekcji etapów (linia 1462: `{!isFromCostEstimate && ...}`), sekcji zależności (linia 1502)
   - Przycisk submit: "Utwórz harmonogram"

3. **Po submit** (linie 930-943):
   ```typescript
   // create case
   const response = await projectApi.createWorkSchedule(tenantId, projectId, command);
   // ... toast success
   onSuccess?.();
   onClose();
   const newId = response?.data?.id;
   if (newId) {
     navigate(`/projects/${projectId}/schedules/${newId}`);
   }
   ```
   - Tworzy harmonogram przez API z `costEstimateId`
   - Natychmiast nawiguje do widoku harmonogramu
   - **Brak synchronizacji lub AI w tym momencie**

4. **Gdy `isFromCostEstimate = false` + wybrano 'linked'** (linie 1409-1441):
   - RadioGroup: manual/linked
   - Select z listą kosztorysów (pobierane z API)
   - Po wybraniu kosztorysu i submit → ten sam create z `costEstimateId`
   - Etapy są puste (backend synchronizuje strukturę podczas create)

### 1.3 Istniejący `syncWorkScheduleWithEstimate` w API

- **Endpoint**: `POST /tenants/{tenantId}/projects/{projectId}/work-schedule/{workScheduleId}/sync-with-estimate`
- **UI call**: `projectApi.syncWorkScheduleWithEstimate(tenantId, projectId, workScheduleId)` (linia 374-376)
- **Używane w**: `CostEstimateEditPage.tsx` linia 493 — przycisk synchronizacji po utworzeniu harmonogramu
- **Body**: brak (POST bez body)

### 1.4 Istniejące typy TypeScript

Wszystkie w `src/types/workSchedule.types.ts` (221 linii):

| Typ | Opis | Status |
|-----|------|--------|
| `WorkScheduleDetailsWeb` | Główny model odpowiedzi API | Istnieje ✓ |
| `WorkScheduleStageWeb` | Etap harmonogramu | Istnieje ✓ |
| `WorkScheduleStageWorkWeb` | Zakres robót | Istnieje ✓ |
| `WorkScheduleStageWorkPeriodWeb` | Okres pracy | Istnieje ✓ |
| `WorkScheduleWorkDependencyWeb` | Zależność (odpowiedź) | Istnieje ✓ |
| `WorkScheduleWorkDependencyDto` | Zależność (DTO wejściowe) | Istnieje ✓ |
| `WorkDependencyType` | Enum typów zależności | Istnieje ✓ |
| `CreateWorkScheduleCommand` | Command create | Istnieje ✓ |
| `UpdateWorkScheduleCommand` | Command update | Istnieje ✓ |
| `GenerateScheduleFromEstimateAIRequest` | Request dla AI generation | **NOWY** |
| `WorkScheduleStageWorkAssigneeWeb` | Przypisany użytkownik | Istnieje ✓ |
| `WorkScheduleStageWorkCommentWeb` | Komentarz | Istnieje ✓ |

---

## BLOK 2 — Luki i braki w UI

| Brak / Luka | Typ | Priorytet | Opis |
|-------------|-----|-----------|------|
| Pola ram czasowych (OverallStartDate, OverallEndDate) | Komponent (date input) | HIGH | Dwa `<Input type="date">` do wyboru daty rozpoczęcia i zakończenia harmonogramu |
| Przycisk "Generuj harmonogram z AI" | Komponent (Button) | HIGH | Przycisk w modalu po utworzeniu harmonogramu, przed nawigacją |
| Stan generowania (AI loading) | Stan UI | HIGH | Spinner/progress podczas wywołania AI (może trwać kilka sekund) |
| Obsługa błędu AI generation | Stan UI | HIGH | Alert błędu z możliwością ponowienia |
| Nowy krok w przepływie po create | Przepływ | HIGH | Po create: zamiast nawigacji → pokaż sekcję AI z date range |
| Nowy endpoint API | API call | HIGH | `generateScheduleFromEstimateAI` w `projectApi.ts` |
| Nowy typ request | TypeScript | MEDIUM | Interfejs dla body requestu z datami |

---

## BLOK 3 — Typy TypeScript do dodania

| Typ | Plik | Nowy/Modyfikacja | Opis zmian |
|-----|------|-----------------|------------|
| `GenerateScheduleFromEstimateAIRequest` | `src/types/workSchedule.types.ts` | NOWY | `{ overallStartDate: string; overallEndDate: string }` — body dla endpointu AI |

**Szczegóły nowego typu:**
```typescript
// src/types/workSchedule.types.ts — dodać przed lub po CreateWorkScheduleCommand

export interface GenerateScheduleFromEstimateAIRequest {
    overallStartDate: string; // ISO 8601 date string
    overallEndDate: string;   // ISO 8601 date string
}
```

**Uwaga**: Odpowiedź API to `WorkScheduleDetailsWeb` — już istnieje, nie trzeba dodawać.

---

## BLOK 4 — Serwisy API (src/api/)

### 4.1 Modyfikacja projectApi.ts

| Funkcja API | Plik | Nowa/Modyfikacja | Endpoint | Opis |
|-------------|------|-----------------|---------|------|
| `generateScheduleFromEstimateAI` | `src/api/projectApi.ts` | NOWA (po linii 376) | `POST .../work-schedule/{workScheduleId}/generate-from-ai` | Wywołuje AI generowanie harmonogramu |

**Kod do dodania — po linii 376 (po `syncWorkScheduleWithEstimate`):**

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

**Import do dodania na górze pliku** (linia 2):
```typescript
import type { WorkScheduleDetailsWeb, GenerateScheduleFromEstimateAIRequest } from "../types/workSchedule.types";
```

---

## BLOK 5 — Hooki React Query

| Hook | Plik | Nowy/Modyfikacja | Query/Mutation | Opis |
|------|------|-----------------|---------------|------|
| — | — | — | — | **Nie potrzebujemy osobnego hooka.** Operacja AI jest jednorazowa, wywoływana bezpośrednio przez `projectApi.generateScheduleFromEstimateAI()` w handlerze submit. Używamy istniejącego wzorca: `try/catch` z `handleApiError`. |

---

## BLOK 6 — Nowe komponenty

| Komponent | Lokalizacja | Opis | Zależy od |
|-----------|------------|------|-----------|
| — | — | — | **Nie potrzebujemy osobnego komponentu.** Sekcja AI będzie osadzona bezpośrednio w `WorkScheduleFormModal` z użyciem istniejących `Input` i `Button`. |

---

## BLOK 7 — Modyfikacje istniejących komponentów

### 7.1 WorkScheduleFormModal.tsx — Szczegółowy plan zmian

#### 7.1.1 Nowe zmienne stanu (okolica linii 299)

Dodać po istniejących state (po `loadingCostEstimates`):

```typescript
// ——— Stan dla AI Schedule Generator ———
const [overallStartDate, setOverallStartDate] = useState<string>("");
const [overallEndDate, setOverallEndDate] = useState<string>("");
const [isGenerating, setIsGenerating] = useState(false);
const [aiGenerationError, setAiGenerationError] = useState<string | null>(null);
// ID utworzonego harmonogramu — gdy ustawiony, pokazujemy sekcję AI zamiast nawigować
const [createdScheduleId, setCreatedScheduleId] = useState<string | null>(null);
```

#### 7.1.2 Nowy handler `handleGenerateFromAI` (dodać po `handleSubmit`, okolice linii 966)

```typescript
const handleGenerateFromAI = async () => {
    if (!createdScheduleId) return;
    
    // Walidacja dat
    if (!overallStartDate || !overallEndDate) {
        toast({
            title: "Błąd walidacji",
            description: "Podaj datę rozpoczęcia i zakończenia harmonogramu",
            status: "error",
            duration: 3000,
        });
        return;
    }
    
    if (new Date(overallStartDate) >= new Date(overallEndDate)) {
        toast({
            title: "Błąd walidacji",
            description: "Data rozpoczęcia musi być wcześniejsza niż data zakończenia",
            status: "error",
            duration: 3000,
        });
        return;
    }
    
    setIsGenerating(true);
    setAiGenerationError(null);
    
    try {
        await projectApi.generateScheduleFromEstimateAI(
            tenantId,
            projectId,
            createdScheduleId,
            {
                overallStartDate: new Date(overallStartDate).toISOString(),
                overallEndDate: new Date(overallEndDate).toISOString(),
            }
        );
        
        showSuccess("Sukces", "Harmonogram został wygenerowany przez AI");
        onSuccess?.();
        onClose();
        navigate(`/projects/${projectId}/schedules/${createdScheduleId}`);
    } catch (error) {
        const { title, description } = handleApiError(error);
        setAiGenerationError(description || title);
        toast({
            title,
            description,
            status: "error",
            duration: 5000,
        });
    } finally {
        setIsGenerating(false);
    }
};
```

#### 7.1.3 Modyfikacja `handleSubmit` — create case (linie 930-943)

**Obecny kod** (linie 930-943):
```typescript
if (mode === 'create') {
    const response = await projectApi.createWorkSchedule(tenantId, projectId, command);
    toast({
        title: "Sukces",
        description: "Harmonogram został utworzony",
        status: "success",
        duration: 3000,
    });
    onSuccess?.();
    onClose();
    const newId = response?.data?.id;
    if (newId) {
        navigate(`/projects/${projectId}/schedules/${newId}`);
    }
}
```

**Nowy kod** (linie 930-943 — tylko dla przypadku `isFromCostEstimate`):
```typescript
if (mode === 'create') {
    const response = await projectApi.createWorkSchedule(tenantId, projectId, command);
    const newId = response?.data?.id;
    
    if (isFromCostEstimate && newId) {
        // Dla flow "z kosztorysu" — nie nawiguj, pokaż sekcję AI
        setCreatedScheduleId(newId);
        // Ustaw domyślne ramy czasowe: dzisiaj + 30 dni
        const today = new Date();
        const defaultEnd = new Date(today);
        defaultEnd.setDate(defaultEnd.getDate() + 30);
        setOverallStartDate(today.toISOString().split("T")[0]);
        setOverallEndDate(defaultEnd.toISOString().split("T")[0]);
        
        showSuccess("Sukces", "Harmonogram został utworzony. Teraz wygeneruj harmonogram z AI.");
    } else {
        // Dla zwykłego create — istniejący flow
        showSuccess("Sukces", "Harmonogram został utworzony");
        onSuccess?.();
        onClose();
        if (newId) {
            navigate(`/projects/${projectId}/schedules/${newId}`);
        }
    }
}
```

**Uwaga**: Nowa funkcja pomocnicza `showSuccess` (z `useToastNotification`) lub użyć istniejącego toast bezpośrednio. W komponencie jest już `showSuccess` z hooka `useToastNotification` (linia 287).

#### 7.1.4 Nowa sekcja JSX — "Krok 2: Generowanie z AI" (dodać przed ModalFooter, po linii 1763)

**Miejsce**: po zamknięciu `</VStack>` (linia 1764), przed `</ModalBody>` (linia 1765).

Dodać:
```tsx
{/* ——— Sekcja AI Generation (po utworzeniu harmonogramu z kosztorysu) ——— */}
{createdScheduleId && (
    <>
        <Divider mt={4} />
        <Box>
            <VStack align="flex-start" spacing={4}>
                <Box>
                    <Text fontWeight="bold" fontSize={{ base: "md", md: "lg" }}>
                        Krok 2: Generowanie harmonogramu z AI
                    </Text>
                    <Text fontSize="sm" color="neutral.600">
                        Podaj ramy czasowe całego harmonogramu. AI automatycznie dobierze czasy trwania
                        i zależności między zakresami robót na podstawie struktury kosztorysu.
                    </Text>
                </Box>

                {/* Ramy czasowe */}
                <HStack spacing={4} width="100%" flexWrap={{ base: "wrap", md: "nowrap" }}>
                    <FormControl isRequired>
                        <FormLabel fontSize="sm">Data rozpoczęcia</FormLabel>
                        <Input
                            type="date"
                            value={overallStartDate}
                            onChange={(e) => setOverallStartDate(e.target.value)}
                            min={new Date().toISOString().split("T")[0]}
                            size="md"
                        />
                    </FormControl>
                    <FormControl isRequired>
                        <FormLabel fontSize="sm">Data zakończenia</FormLabel>
                        <Input
                            type="date"
                            value={overallEndDate}
                            onChange={(e) => setOverallEndDate(e.target.value)}
                            min={overallStartDate || new Date().toISOString().split("T")[0]}
                            size="md"
                        />
                    </FormControl>
                </HStack>

                {/* Przycisk AI + stany */}
                <HStack spacing={3}>
                    <Button
                        colorScheme="primary"
                        leftIcon={isGenerating ? undefined : <Sparkles size={16} />}
                        onClick={handleGenerateFromAI}
                        isLoading={isGenerating}
                        loadingText="AI generuje harmonogram..."
                        isDisabled={isGenerating || !overallStartDate || !overallEndDate}
                        size="md"
                    >
                        Generuj harmonogram z AI
                    </Button>
                    
                    {isGenerating && (
                        <Text fontSize="sm" color="neutral.500">
                            To może potrwać kilka sekund...
                        </Text>
                    )}
                </HStack>

                {/* Błąd AI generation */}
                {aiGenerationError && (
                    <Alert status="error" borderRadius="md" role="alert">
                        <AlertIcon aria-hidden="true" />
                        <AlertDescription fontSize="sm">
                            {aiGenerationError}
                        </AlertDescription>
                    </Alert>
                )}

                {/* Informacja o możliwości pominięcia */}
                <Text fontSize="xs" color="neutral.500">
                    Możesz też pominąć ten krok i wygenerować harmonogram później z poziomu widoku harmonogramu.
                </Text>
                <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => {
                        onSuccess?.();
                        onClose();
                        navigate(`/projects/${projectId}/schedules/${createdScheduleId}`);
                    }}
                    isDisabled={isGenerating}
                >
                    Pomiń i przejdź do harmonogramu
                </Button>
            </VStack>
        </Box>
    </>
)}
```

**Uwaga**: Import `Sparkles` z `lucide-react` — dodać do istniejącego importu (linia 43):
```typescript
import { Plus, Trash2, GripVertical, FolderPlus, ArrowRight, Sparkles } from "lucide-react";
```

#### 7.1.5 Modyfikacja rozmiaru modala (linia 1388)

Obecnie: `size={{ base: "full", md: isFromCostEstimate ? "md" : "6xl" }}`

Gdy `createdScheduleId` jest ustawiony, potrzebujemy więcej miejsca. Zmienić na:
```tsx
size={{ base: "full", md: isFromCostEstimate || createdScheduleId ? "md" : "6xl" }}
```

#### 7.1.6 Modyfikacja tytułu modala (linie 1378-1382)

Dodać warunek dla kroku AI:
```typescript
const modalTitle = createdScheduleId
    ? 'Generuj harmonogram z AI'
    : isFromCostEstimate
    ? 'Nowy harmonogram z kosztorysu'
    : mode === 'create'
    ? `Utwórz harmonogram prac - ${projectName}`
    : `Edytuj harmonogram - ${schedule?.name || ''}`;
```

#### 7.1.7 Reset stanu AI w `resetForm` (linie 387-405)

Dodać na końcu `resetForm`:
```typescript
const resetForm = () => {
    setScheduleName("");
    setStages([]);
    setDependencies([]);
    // Reset AI state
    setCreatedScheduleId(null);
    setOverallStartDate("");
    setOverallEndDate("");
    setIsGenerating(false);
    setAiGenerationError(null);
    // ... reszta istniejącego kodu
};
```

#### 7.1.8 Nowy import (linia 43)

Dodać `Sparkles` do importu z `lucide-react`:
```typescript
import { Plus, Trash2, GripVertical, FolderPlus, ArrowRight, Sparkles } from "lucide-react";
```

### 7.2 Podsumowanie zmian w WorkScheduleFormModal.tsx

| # | Lokalizacja | Rodzaj zmiany | Opis |
|---|-------------|--------------|------|
| 1 | Linia 43 | Modyfikacja | Dodaj `Sparkles` do importu z `lucide-react` |
| 2 | Okolice linii 299 | Dodanie | 5 nowych zmiennych stanu (createdScheduleId, overallStartDate, overallEndDate, isGenerating, aiGenerationError) |
| 3 | Okolice linii 405 | Modyfikacja | Reset stanu AI w `resetForm` |
| 4 | Linie 930-943 | Modyfikacja | Zmiana przepływu po create dla `isFromCostEstimate` — zamiast nawigacji, pokaż sekcję AI |
| 5 | Po linii 966 (przed `renderWork`) | Dodanie | Nowy handler `handleGenerateFromAI` |
| 6 | Linie 1378-1382 | Modyfikacja | Warunkowy tytuł modala dla kroku AI |
| 7 | Linia 1388 | Modyfikacja | Rozmiar modala uwzględniający `createdScheduleId` |
| 8 | Po linii 1763 (przed `</ModalBody>`) | Dodanie | Nowa sekcja JSX z date pickerami, przyciskiem AI, loading/error |

---

## BLOK 8 — Spójność UI

| Wzorzec | Istniejąca implementacja | Czy feature musi się dostosować |
|---------|------------------------|--------------------------------|
| Date input | `<Input type="date" />` przez `ConstrainedDateInput` | TAK — użyć `<Input type="date">` bez `ConstrainedDateInput` (ramy czasowe nie mają zależności) |
| Obsługa błędów | `handleApiError` + `useToastNotification` | TAK — użyć istniejącego wzorca |
| Obsługa loadingu | `isLoading` + `Button isLoading` + `Spinner` | TAK — `isGenerating` + `Button isLoading="AI generuje harmonogram..."` |
| Alerty błędów | `<Alert status="error">` z `role="alert"` | TAK — użyć istniejącego wzorca |
| Nawigacja po sukcesie | `navigate()` po `onClose()` | TAK — po AI sukcesie nawigować do widoku harmonogramu |
| Nazewnictwo handlerów | `handleSubmit`, `handleSyncSchedule` | TAK — `handleGenerateFromAI` |
| Kolory | `primary` dla głównych akcji, `ghost` dla pomijania | TAK — `colorScheme="primary"` dla przycisku AI |
| Ikony akcji | `lucide-react` | TAK — `Sparkles` dla przycisku AI |

### Zgodność z istniejącymi wzorcami:

1. **Obsługa błędów** (linia 955-962) — używa `handleApiError` + `toast` → w `handleGenerateFromAI` używamy tego samego wzorca, dodatkowo zapisujemy błąd w `aiGenerationError` dla wyświetlenia w UI.

2. **Obsługa loadingu** — istniejący `submitting` + `Button isLoading` dla submit → analogicznie `isGenerating` + `Button isLoading` dla AI.

3. **Walidacja** — przed AI sprawdzamy czy daty są wypełnione i czy start < end, wzorując się na istniejącej walidacji (linie 877-917).

---

## BLOK 9 — Dostępność (WCAG AA / AXE)

### 9.1 Istniejący WorkScheduleFormModal

| Element | Problem | Rekomendacja |
|---------|---------|-------------|
| `<IconButton>` (linie 1008, 1063, 1318, 1744) | ✅ Mają `aria-label` | OK |
| `color="neutral.500"` (linie 1005, 1314, 1488, 1493, 1552, 1556) | ⚠ Kontrast ~4.48:1 — graniczny dla tekstu < 18px | Rozważyć `neutral.600` dla lepszego kontrastu |
| `<Box>` z `onClick` + `cursor="pointer"` dla kolorów (linie 1101-1112, 1137-1144) | ✗ Brak `role="button"`, `tabIndex`, `onKeyDown` | Dodać obsługę klawiatury |
| Chakra `<AccordionButton>` z `draggable` + D&D (linie 977-983) | ⚠ Przeciąganie niedostępne z klawiatury | Poza scope — istniejący problem |
| Chakra `<Modal>` | ✅ Auto focus trap, `role="dialog"`, `aria-modal="true"`, Escape | OK |

### 9.2 Nowa sekcja AI Generation

| Kategoria | Status | Uwagi |
|----------|--------|-------|
| Kontrast kolorów | ✓ | Użycie `neutral.600` dla tekstu pomocniczego, `primary` dla przycisku |
| Atrybuty ARIA | ✓ | `<Alert role="alert">` dla błędów, `<AlertIcon aria-hidden="true">` |
| Klawiatura / fokus | ✓ | `<Input type="date">` natywnie dostępne, `<Button>` Chakra dostępny |
| Testy AXE | ⚠ | Należy dodać test dla `WorkScheduleFormModal` w trybie AI |

**Flagi do sprawdzenia w nowej sekcji:**
- `color="neutral.500"` na tekstach informacyjnych — graniczny kontrast; użyć `neutral.600` jeśli treść jest ważna
- Ikona `Sparkles` w przycisku — użyć jako `leftIcon` (Chakra automatycznie ustawia `aria-hidden`)
- `Button isLoading` — Chakra zarządza `aria-busy` i `aria-label` automatycznie
- `FormLabel` + `Input type="date"` — Chakra auto-paruje przez `htmlFor`

### Podsumowanie dostępności dla feature

| Kategoria | Status | Uwagi |
|----------|--------|-------|
| Kontrast kolorów | ✓ | Użyto bezpiecznych kolorów |
| Atrybuty ARIA | ✓ | Alert z `role="alert"`, ikony z `aria-hidden` |
| Klawiatura / fokus | ✓ | Input type="date" natywnie dostępny klawiaturą |
| Testy AXE | ⚠ | Wymagany nowy test dla `WorkScheduleFormModal` w trybie AI |

---

## BLOK 10 — Problemy i ryzyka

| # | Problem | Komponent/Plik | Ryzyko | Rekomendacja |
|---|---------|---------------|--------|-------------|
| 1 | **Podwójne kliknięcie "Generuj"** — user może kliknąć wielokrotnie | WorkScheduleFormModal | Niskie | `isDisabled={isGenerating}` na przycisku + guard w handlerze |
| 2 | **Długi czas generowania AI** — brak progress bara | WorkScheduleFormModal | Średnie | Dodać text "To może potrwać kilka sekund..." obok loadera |
| 3 | **Zamknięcie modala podczas generowania** — utrata kontekstu | WorkScheduleFormModal | Średnie | Zablokować `onClose` gdy `isGenerating` (Chakra `closeOnOverlayClick=false`, `isDisabled` na close button) |
| 4 | **Brak synchronizacji przed AI** — jeśli create nie zsynchronizował struktury | WorkScheduleFormModal | Wysokie | API handler musi najpierw wywołać sync przed AI (patrz raport API, ryzyko #1) |
| 5 | **Nazwy zakresów zmienione** — AI dostało stare nazwy | API → UI | Średnie | Backend synchronizuje przed AI (patrz API audit, rekomendacja) |
| 6 | **Brak debounce na create** — szybki create → AI w tym samym czasie | WorkScheduleFormModal | Niskie | Ustawić `createdScheduleId` dopiero po sukcesie create + ukryć przycisk AI do tego czasu |
| 7 | **Modal size `md` za mały** — sekcja AI z date pickerami może być ciasna | WorkScheduleFormModal | Niskie | `md` wystarczy dla 2 pól + przycisk, ale można dać `lg` |
| 8 | **Brak testów dla nowego przepływu** — ryzyko regresji | Testy UI | Średnie | Dodać testy AXE dla nowej sekcji |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe komponenty | 0 (sekcja osadzona w istniejącym modalu) |
| Zmodyfikowane komponenty | 1 (`WorkScheduleFormModal.tsx`) |
| Nowe hooki | 0 |
| Nowe typy TypeScript | 1 (`GenerateScheduleFromEstimateAIRequest`) |
| Nowe wywołania API | 1 (`generateScheduleFromEstimateAI` w `projectApi.ts`) |
| Naruszenia WCAG AA | 1 ✗ (istniejący — `<Box>` color picker bez klawiatury) |
| Pytania domenowe | 3 |

---

## Pytania domenowe wymagające decyzji

1. **Czy po create z kosztorysem trzeba najpierw synchronizować (`sync-with-estimate`) przed AI?** W API audit (ryzyko #1) rekomendacja: handler powinien najpierw zsynchronizować, potem AI. UI wysyła tylko `generate-from-ai`, backend robi sync wewnątrz handlera. **Decyzja**: backend wykonuje sync przed AI — UI nie musi wysyłać osobnego żądania.

2. **Czy user może pominąć AI i przejść do harmonogramu?** Feature spec mówi o "User widzi gotowy harmonogram z wypełnionymi datami", ale user może chcieć zrobić to później. **Decyzja**: dodać przycisk "Pomiń i przejdź do harmonogramu" — harmonogram zostanie utworzony ale bez dat/zależności.

3. **Domyślne wartości ram czasowych?** Gdy modal otwiera się po create, warto zaproponować domyślne daty. **Decyzja**: dzisiaj → dzisiaj + 30 dni jako domyślne, user może zmienić.

4. **Czy zablokować zamknięcie modala podczas generowania AI?** AI może trwać kilka sekund, zamknięcie modala przerwie operację (bo komponent się odmontuje). **Decyzja**: ustawić `closeOnOverlayClick={false}` i `closeOnEsc={false}` gdy `isGenerating=true`, oraz `isDisabled` na `ModalCloseButton`.

---

## Schemat przepływu użytkownika (nowy)

```
CostEstimateEditPage
    │
    ├── User klika "Utwórz harmonogram"
    │
    ▼
WorkScheduleFormModal (mode='create', initialCostEstimateId set)
    │
    ├── Krok 1: Nazwa harmonogramu
    │   └── User wpisuje nazwę
    │
    ├── Kliknięcie "Utwórz harmonogram"
    │   └── API: createWorkSchedule(costEstimateId)
    │       └── Sukces → setCreatedScheduleId(id), pokaż sekcję AI
    │
    ├── Krok 2: Generowanie harmonogramu z AI
    │   ├── Pole: Data rozpoczęcia (OverallStartDate)
    │   ├── Pole: Data zakończenia (OverallEndDate)
    │   ├── Przycisk: "Generuj harmonogram z AI"
    │   │   └── API: generateScheduleFromEstimateAI(overallStartDate, overallEndDate)
    │   │       ├── Loading: Spinner + "AI generuje harmonogram..."
    │   │       ├── Sukces → Toast + navigate(/projects/{id}/schedules/{id})
    │   │       └── Błąd → Alert error + możliwość ponowienia
    │   └── Przycisk: "Pomiń i przejdź do harmonogramu"
    │       └── navigate(/projects/{id}/schedules/{id})
    │
    └── [Modal zamknięty podczas generowania = zablokowany]
```

---

## Lista plików wymagających modyfikacji (UI)

| # | Plik | Zmiana |
|---|------|--------|
| 1 | `src/components/WorkScheduleFormModal.tsx` | Dodanie stanów AI, handlera `handleGenerateFromAI`, sekcji JSX z date pickerami i przyciskiem, modyfikacja przepływu create + reset |
| 2 | `src/api/projectApi.ts` | Dodanie metody `generateScheduleFromEstimateAI` + import typu |
| 3 | `src/types/workSchedule.types.ts` | Dodanie interfejsu `GenerateScheduleFromEstimateAIRequest` |
