# UI-03: Modyfikacja WorkScheduleFormModal.tsx — AI Schedule Generator

## Zadanie
Zmodyfikuj `WorkScheduleFormModal.tsx` aby dodać:
1. Nowe stany AI
2. Zmieniony przepływ po create dla `isFromCostEstimate`
3. Nowy handler `handleGenerateFromAI`
4. Nowa sekcja JSX z date pickerami i przyciskiem AI
5. Reset stanu AI w `resetForm`
6. Warunkowy tytuł i rozmiar modala
7. Ostrzeżenie przy pomijaniu AI

## Plik do modyfikacji
`01-Applications/ProjectDataManagementUI/src/components/WorkScheduleFormModal.tsx`

## Szczegółowe zmiany

### 1. Import — linia 43
Dodaj `Sparkles` do importu z `lucide-react`:
```typescript
import { Plus, Trash2, GripVertical, FolderPlus, ArrowRight, Sparkles } from "lucide-react";
```

### 2. Nowe stany — po linii 299 (po `loadingCostEstimates`)
Dodaj:
```typescript
  // ——— Stan dla AI Schedule Generator ———
  const [overallStartDate, setOverallStartDate] = useState<string>("");
  const [overallEndDate, setOverallEndDate] = useState<string>("");
  const [isGenerating, setIsGenerating] = useState(false);
  const [aiGenerationError, setAiGenerationError] = useState<string | null>(null);
  // ID utworzonego harmonogramu — gdy ustawiony, pokazujemy sekcję AI zamiast nawigować
  const [createdScheduleId, setCreatedScheduleId] = useState<string | null>(null);
  const [showSkipWarning, setShowSkipWarning] = useState(false);
```

### 3. Reset stanu AI w `resetForm` — linie 387-405
Na początku `resetForm` dodaj:
```typescript
    // Reset AI state
    setCreatedScheduleId(null);
    setOverallStartDate("");
    setOverallEndDate("");
    setIsGenerating(false);
    setAiGenerationError(null);
    setShowSkipWarning(false);
```

### 4. Zmieniony przepływ po create — linie 930-943 (w handleSubmit)
Zastąp istniejący kod create case:

**STARY kod (linie 930-943):**
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

**NOWY kod:**
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

          toast({
            title: "Harmonogram utworzony",
            description: "Teraz możesz wygenerować harmonogram z AI, aby automatycznie ustawić okresy i zależności.",
            status: "success",
            duration: 5000,
          });
        } else {
          // Dla zwykłego create — istniejący flow
          toast({
            title: "Sukces",
            description: "Harmonogram został utworzony",
            status: "success",
            duration: 3000,
          });
          onSuccess?.();
          onClose();
          if (newId) {
            navigate(`/projects/${projectId}/schedules/${newId}`);
          }
        }
      }
```

### 5. Nowy handler `handleGenerateFromAI` — dodaj po `handleSubmit` (przed `renderWork`)
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

      showSuccess("Sukces", "Harmonogram został wygenerowany przez AI z okresami i zależnościami");
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

  const handleSkipAI = () => {
    if (!createdScheduleId) return;

    if (!showSkipWarning) {
      setShowSkipWarning(true);
      return;
    }

    // User confirmed skip
    onSuccess?.();
    onClose();
    navigate(`/projects/${projectId}/schedules/${createdScheduleId}`);
  };
```

### 6. Warunkowy tytuł modala — linie 1378-1382
Zastąp istniejące:
**STARY kod:**
```typescript
  const isFromCostEstimate = mode === 'create' && !!initialCostEstimateId;

  const modalTitle = isFromCostEstimate
    ? 'Nowy harmonogram z kosztorysu'
    : mode === 'create'
    ? `Utwórz harmonogram prac - ${projectName}`
    : `Edytuj harmonogram - ${schedule?.name || ''}`;
```

**NOWY kod:**
```typescript
  const isFromCostEstimate = mode === 'create' && !!initialCostEstimateId;

  const modalTitle = createdScheduleId
    ? 'Krok 2: Generuj harmonogram z AI'
    : isFromCostEstimate
    ? 'Nowy harmonogram z kosztorysu'
    : mode === 'create'
    ? `Utwórz harmonogram prac - ${projectName}`
    : `Edytuj harmonogram - ${schedule?.name || ''}`;
```

### 7. Warunkowy rozmiar modala — linia 1388
Zastąp:
**STARY:**
```typescript
    <Modal isOpen={isOpen} onClose={onClose} size={{ base: "full", md: isFromCostEstimate ? "md" : "6xl" }} scrollBehavior="inside">
```
**NOWY:**
```typescript
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      closeOnOverlayClick={!isGenerating}
      closeOnEsc={!isGenerating}
      size={{ base: "full", md: isFromCostEstimate || createdScheduleId ? "md" : "6xl" }}
      scrollBehavior="inside"
    >
```

### 8. Nowa sekcja JSX — dodaj po istniejącym zamykającym `</VStack>` (linia 1764), przed `</ModalBody>` (linia 1765)

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

                    {/* Ostrzeżenie przy pomijaniu */}
                    {showSkipWarning && (
                      <Alert status="warning" borderRadius="md" role="alert">
                        <AlertIcon aria-hidden="true" />
                        <AlertDescription fontSize="sm">
                          Okresy i zależności nie zostaną wygenerowane bez AI. Możesz wrócić do tego później
                          z poziomu widoku harmonogramu.
                        </AlertDescription>
                      </Alert>
                    )}

                    {/* Pomiń krok AI */}
                    <Text fontSize="xs" color="neutral.500">
                      {showSkipWarning
                        ? "Kliknij ponownie aby potwierdzić pominięcie."
                        : "Możesz też pominąć ten krok i wygenerować harmonogram później."}
                    </Text>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={handleSkipAI}
                      isDisabled={isGenerating}
                    >
                      {showSkipWarning ? "Tak, pomiń generowanie AI" : "Pomiń i przejdź do harmonogramu"}
                    </Button>
                  </VStack>
                </Box>
              </>
            )}

            {/* ——— ModalCloseButton blokowany podczas generowania ——— */}
```

### 9. Blokada ModalCloseButton podczas generowania
Znajdź `<ModalCloseButton />` (linia 1392) i zmień na:
```tsx
        <ModalCloseButton isDisabled={isGenerating} />
```

### 10. Dodanie `projectApi.generateScheduleFromEstimateAI` do zakresu
Upewnij się że w istniejących importach na górze pliku `projectApi` jest już zaimportowany (linia 44):
```typescript
import { projectApi } from "../api/projectApi";
```
To już istnieje — nie zmieniaj.

Dodaj również import nowego typu jeśli potrzebny. Sprawdź czy `WorkScheduleDetailsWeb` jest już w importach (linia 48):
```typescript
import type { WorkScheduleDetailsWeb, WorkScheduleStageWeb, WorkScheduleWorkDependencyWeb, WorkScheduleWorkDependencyDto } from "../types/workSchedule.types";
```
Jeśli tak, to nie trzeba nic zmieniać — typ `WorkScheduleDetailsWeb` jest już zaimportowany.

## Uwagi końcowe
- Użyj istniejącego wzorca `showSuccess` z hooka `useToastNotification`
- Użyj istniejącego `handleApiError` do obsługi błędów
- Przycisk AI jest blokowany (`isDisabled`) gdy `isGenerating` lub brak dat
- Modal jest blokowany (`closeOnOverlayClick=false`, `closeOnEsc=false`) podczas generowania
- Ostrzeżenie przy pomijaniu: pierwsze kliknięcie pokazuje warning, drugie potwierdza
