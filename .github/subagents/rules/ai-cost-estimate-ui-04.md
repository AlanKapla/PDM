# Prompt UI-04: GenerateCostEstimateWithAIModal — Step 3, 4, 5

## Cel
Uzupełnij modal `GenerateCostEstimateWithAIModal.tsx` o kroki 3-5:
- **Step 3** — Loading (AI generuje)
- **Step 4** — Podgląd drzewa grup i pozycji
- **Step 5** — Edycja nazwy/opisu + zatwierdzenie

---

## Pliki do modyfikacji

`src/components/GenerateCostEstimateWithAIModal.tsx`

---

## Zmiany do wprowadzenia

### 1. Dodaj nowe importy na górze pliku

```tsx
import { useContext } from 'react';
import {
  Accordion,
  AccordionItem,
  AccordionButton,
  AccordionPanel,
  AccordionIcon,
  Tag,
} from '@chakra-ui/react';
import { Check, FileText, Folder } from 'lucide-react';
import { AuthContext } from '../context/AuthContext';
import { useGenerateCostEstimateWithAI } from '../hooks/useGenerateCostEstimateWithAI';
```

### 2. W komponencie głównym `GenerateCostEstimateWithAIModal` — dodaj

Wewnątrz ciała funkcji, **po** deklaracji `[finalDescription, setFinalDescription]`, dodaj:

```tsx
  const { user } = useContext(AuthContext);
  const { generatePreview, createFromPreview } = useGenerateCostEstimateWithAI(tenantId, projectId);

  // Uruchom generowanie gdy wchodzimy na step 3
  useEffect(() => {
    if (step === 3 && !preview && !generatePreview.isPending) {
      const request = buildRequest();
      generatePreview.mutate(request, {
        onSuccess: (result) => {
          setPreview(result);
          setFinalName(result.suggestedName);
          setFinalDescription(result.suggestedDescription ?? '');
          setStep(4);
        },
        onError: (error: Error) => {
          const { title, description } = handleApiError(error);
          showError(title, description);
          setStep(2); // cofnij do wyboru szablonu
        },
      });
    }
  }, [step]);

  const handleSave = () => {
    if (!preview) return;
    if (!finalName.trim()) {
      showError('Nazwa wymagana', 'Podaj nazwę kosztorysu przed zapisem.');
      return;
    }
    createFromPreview.mutate(
      { name: finalName.trim(), description: finalDescription.trim() || undefined, preview },
      {
        onSuccess: (id) => {
          onCostEstimateCreated(id);
          handleClose();
        },
        onError: (error: Error) => {
          const { title, description } = handleApiError(error);
          showError(title, description);
        },
      }
    );
  };
```

### 3. W sekcji renderowania kroków — zastąp placeholder `step === 3 || step === 4 || step === 5`

Zastąp istniejący placeholder:
```tsx
          {(step === 3 || step === 4 || step === 5) && (
            <Box py={4}>
              <Text color="gray.500" textAlign="center">
                Ładowanie kolejnych kroków...
              </Text>
            </Box>
          )}
```

Nowym kodem:
```tsx
          {step === 3 && (
            <Step3Generating />
          )}
          {step === 4 && preview && (
            <Step4Preview preview={preview} />
          )}
          {step === 5 && preview && (
            <Step5Confirm
              preview={preview}
              finalName={finalName}
              finalDescription={finalDescription}
              onNameChange={setFinalName}
              onDescriptionChange={setFinalDescription}
            />
          )}
```

### 4. W sekcji `<ModalFooter>` — zastąp istniejące przyciski kroków 1 i 2

Zachowaj istniejące przyciski dla step 1 i 2, dodaj **po** istniejących `{step === 2 && ...}`:

```tsx
            {step === 4 && (
              <Button
                colorScheme="purple"
                rightIcon={<ChevronRight size={16} />}
                onClick={() => setStep(5)}
              >
                Zatwierdź podgląd
              </Button>
            )}
            {step === 5 && (
              <Button
                colorScheme="green"
                leftIcon={<Check size={16} />}
                onClick={handleSave}
                isLoading={createFromPreview.isPending}
                loadingText="Zapisywanie..."
              >
                Zapisz kosztorys
              </Button>
            )}
```

---

## Nowe komponenty — dodaj na końcu pliku

### Step 3: Generowanie AI

```tsx
function Step3Generating() {
  return (
    <VStack spacing={6} py={8} align="center">
      <Spinner size="xl" color="purple.500" thickness="4px" />
      <VStack spacing={1}>
        <Text fontWeight="semibold" fontSize="lg">AI generuje kosztorys...</Text>
        <Text color="gray.500" fontSize="sm" textAlign="center">
          Analizuję opis inwestycji i strukturę szablonu.
          Może to potrwać do 30 sekund.
        </Text>
      </VStack>
    </VStack>
  );
}
```

### Step 4: Podgląd drzewa kosztorysu

```tsx
interface Step4PreviewProps {
  preview: AICostEstimatePreviewDto;
}

function Step4Preview({ preview }: Step4PreviewProps) {
  return (
    <VStack spacing={4} align="stretch">
      {preview.warnings.length > 0 && (
        <Alert status="warning" borderRadius="md" fontSize="sm">
          <AlertIcon />
          <VStack align="flex-start" spacing={0}>
            <Text fontWeight="semibold">Ostrzeżenia AI</Text>
            {preview.warnings.map((w, i) => (
              <Text key={i} fontSize="xs">{w}</Text>
            ))}
          </VStack>
        </Alert>
      )}

      <Box>
        <Text fontWeight="semibold" fontSize="sm" mb={2} color="gray.600">
          Sugerowana nazwa: <Text as="span" color="purple.600">{preview.suggestedName}</Text>
        </Text>
        {preview.suggestedDescription && (
          <Text fontSize="sm" color="gray.500" mb={2}>{preview.suggestedDescription}</Text>
        )}
      </Box>

      <Text fontWeight="semibold" mb={1}>
        Struktura kosztorysu ({preview.groups.length} grup)
      </Text>

      <Accordion allowMultiple defaultIndex={preview.groups.map((_, i) => i)}>
        {preview.groups
          .filter((g) => !g.parentTempId)
          .sort((a, b) => a.order - b.order)
          .map((group) => (
            <GroupPreviewItem
              key={group.tempId}
              group={group}
              allGroups={preview.groups}
              indent={0}
            />
          ))}
      </Accordion>
    </VStack>
  );
}

interface GroupPreviewItemProps {
  group: import('../types/costEstimate.types.new').AIGroupPreviewDto;
  allGroups: import('../types/costEstimate.types.new').AIGroupPreviewDto[];
  indent: number;
}

function GroupPreviewItem({ group, allGroups, indent }: GroupPreviewItemProps) {
  const subGroups = allGroups
    .filter((g) => g.parentTempId === group.tempId)
    .sort((a, b) => a.order - b.order);

  return (
    <AccordionItem borderColor="purple.100">
      <AccordionButton pl={indent * 4 + 2}>
        <HStack flex={1} textAlign="left" spacing={2}>
          <Icon as={Folder} size={14} color="purple.500" />
          <Text fontWeight="medium" fontSize="sm">{group.name}</Text>
          <Tag size="sm" colorScheme="blue" variant="subtle">
            {group.items.length} poz.
          </Tag>
        </HStack>
        <AccordionIcon />
      </AccordionButton>
      <AccordionPanel pb={2} pl={indent * 4 + 4}>
        {/* Pozycje */}
        {group.items.length > 0 && (
          <VStack align="stretch" spacing={1} mb={2}>
            {group.items.sort((a, b) => a.order - b.order).map((item) => (
              <HStack
                key={item.tempId}
                spacing={2}
                py={1}
                px={2}
                borderRadius="md"
                bg="gray.50"
                fontSize="sm"
              >
                <Icon as={FileText} size={12} color="gray.400" />
                <Text flex={1} noOfLines={1}>{item.name}</Text>
                {item.fieldValues.length > 0 && (
                  <Tag size="sm" colorScheme="gray" variant="subtle">
                    {item.fieldValues.length} pól
                  </Tag>
                )}
              </HStack>
            ))}
          </VStack>
        )}
        {/* Podgrupy */}
        {subGroups.length > 0 && (
          <Accordion allowMultiple>
            {subGroups.map((sg) => (
              <GroupPreviewItem
                key={sg.tempId}
                group={sg}
                allGroups={allGroups}
                indent={indent + 1}
              />
            ))}
          </Accordion>
        )}
        {group.items.length === 0 && subGroups.length === 0 && (
          <Text fontSize="xs" color="gray.400" fontStyle="italic">Pusta grupa</Text>
        )}
      </AccordionPanel>
    </AccordionItem>
  );
}
```

### Step 5: Potwierdzenie + edycja nazwy

```tsx
interface Step5ConfirmProps {
  preview: AICostEstimatePreviewDto;
  finalName: string;
  finalDescription: string;
  onNameChange: (v: string) => void;
  onDescriptionChange: (v: string) => void;
}

function Step5Confirm({
  preview,
  finalName,
  finalDescription,
  onNameChange,
  onDescriptionChange,
}: Step5ConfirmProps) {
  const totalItems = preview.groups.reduce((sum, g) => sum + g.items.length, 0);

  return (
    <VStack spacing={4} align="stretch">
      <Alert status="success" borderRadius="md">
        <AlertIcon />
        <VStack align="flex-start" spacing={0}>
          <Text fontWeight="semibold">Kosztorys gotowy do zapisu</Text>
          <Text fontSize="sm">
            {preview.groups.length} grup, {totalItems} pozycji. Sprawdź nazwę i kliknij "Zapisz kosztorys".
          </Text>
        </VStack>
      </Alert>

      <FormControl isRequired>
        <FormLabel>Nazwa kosztorysu</FormLabel>
        <Input
          value={finalName}
          onChange={(e) => onNameChange(e.target.value)}
          placeholder="Nazwa kosztorysu"
          maxLength={200}
        />
      </FormControl>

      <FormControl>
        <FormLabel>Opis (opcjonalny)</FormLabel>
        <Textarea
          value={finalDescription}
          onChange={(e) => onDescriptionChange(e.target.value)}
          placeholder="Krótki opis kosztorysu..."
          rows={3}
          maxLength={2000}
        />
      </FormControl>

      {preview.warnings.length > 0 && (
        <Alert status="warning" borderRadius="md" fontSize="sm">
          <AlertIcon />
          <VStack align="flex-start" spacing={0}>
            <Text fontWeight="semibold">Pola pominięte przez AI</Text>
            {preview.warnings.slice(0, 5).map((w, i) => (
              <Text key={i} fontSize="xs">{w}</Text>
            ))}
            {preview.warnings.length > 5 && (
              <Text fontSize="xs" color="gray.500">...i {preview.warnings.length - 5} więcej</Text>
            )}
          </VStack>
        </Alert>
      )}
    </VStack>
  );
}
```

---

## Uwagi implementacyjne

1. `handleApiError` jest już importowany w kroku UI-03 — upewnij się że nie duplikujesz importu.
2. `AuthContext` — sprawdź czy `user` jest już używany w komponencie (może być z `useContext(AuthContext)`) — jeśli tak, nie dodawaj ponownej deklaracji.
3. `HStack` z `Icon` — sprawdź czy `Icon` przyjmuje `as` prop w wersji Chakra UI 2 używanej w projekcie. Alternatywnie użyj `<Folder size={14} />` bezpośrednio.
4. `useEffect` z `step === 3` — upewnij się że masz `generatePreview` i `preview` w dependency array jeśli linter tego wymaga. Możesz pominąć `generatePreview.mutate` z dep array (stable reference).

## Weryfikacja
```
npx tsc --noEmit 2>&1 | Select-String "GenerateCostEstimateWithAIModal|error TS" | Select-Object -First 20
```
Oczekiwany wynik: brak błędów.
