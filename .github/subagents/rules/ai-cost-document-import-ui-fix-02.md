# UI Fix 02 — DocumentDropzone + AICostImportModal + Integracja w formularzach

## Cel
Stwórz komponenty UI i zintegruj je w istniejących formularzach kosztów.

## Krok 1 — Przeczytaj przed implementacją

Przeczytaj PEŁNĄ treść:
- `src/components/ui/AppModal.tsx` — pełny wzorzec modala (props: isOpen, onClose, title, footer, itp.)
- `src/components/CostTracker/CostFormModal.tsx` — pełna treść (żeby wiedzieć gdzie dodać przycisk)
- `src/components/CostTracker/CostFormDrawer.tsx` — pełna treść (żeby wiedzieć gdzie dodać przycisk)
- `src/features/dashboard/components/CostModal.tsx` — pełna treść (żeby wiedzieć gdzie dodać przycisk)
- `src/components/CostTracker/CostForm.tsx` — jakie props przyjmuje (żeby pre-fillować wartości)
- `src/api/costTrackerApi.ts` — metoda createCost (żeby wiedzieć jak przekazać FormData po zatwierdzeniu)
- `src/api/projectApi.ts` — metoda createProjectCost
- Sprawdź jak `handleApiError` i toast notification są używane w CostFormModal lub CostFormDrawer

## Krok 2 — Stwórz DocumentDropzone

Plik: `src/components/ui/DocumentDropzone.tsx`

```tsx
import { useRef } from 'react';
import { Box, Text, Icon, VStack } from '@chakra-ui/react';
// Sprawdź dostępne ikony w projekcie (lucide-react lub @chakra-ui/icons)

interface DocumentDropzoneProps {
  accept?: string;       // default: ".jpg,.jpeg,.png,.pdf"
  maxSizeMB?: number;    // default: 20
  value: File | null;
  onChange: (file: File | null) => void;
  isDisabled?: boolean;
}

export function DocumentDropzone({
  accept = '.jpg,.jpeg,.png,.pdf',
  maxSizeMB = 20,
  value,
  onChange,
  isDisabled = false,
}: DocumentDropzoneProps) {
  const inputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    if (file && file.size > maxSizeMB * 1024 * 1024) {
      // Przekrocozny limit rozmiaru — zresetuj
      onChange(null);
      return;
    }
    onChange(file);
    // Reset input żeby można było wybrać ten sam plik ponownie
    e.target.value = '';
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    if (isDisabled) return;
    const file = e.dataTransfer.files?.[0] ?? null;
    if (file) {
      onChange(file);
    }
  };

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
  };

  return (
    <Box
      border="2px dashed"
      borderColor={value ? 'green.400' : 'gray.300'}
      borderRadius="md"
      p={6}
      textAlign="center"
      cursor={isDisabled ? 'not-allowed' : 'pointer'}
      opacity={isDisabled ? 0.6 : 1}
      bg={value ? 'green.50' : 'gray.50'}
      _hover={!isDisabled ? { borderColor: 'primary.400', bg: 'primary.50' } : {}}
      onClick={() => !isDisabled && inputRef.current?.click()}
      onDrop={handleDrop}
      onDragOver={handleDragOver}
    >
      <input
        ref={inputRef}
        type="file"
        accept={accept}
        style={{ display: 'none' }}
        onChange={handleFileChange}
        disabled={isDisabled}
      />
      <VStack spacing={2}>
        {/* Ikona — użyj dostępnej ikony z projektu (np. Upload, FileText, lub Paperclip) */}
        <Text fontSize="sm" color={value ? 'green.700' : 'gray.500'} fontWeight="medium">
          {value ? value.name : 'Przeciągnij plik lub kliknij, aby wybrać'}
        </Text>
        <Text fontSize="xs" color="gray.400">
          JPG, PNG, PDF · maks. {maxSizeMB} MB
        </Text>
        {value && (
          <Text fontSize="xs" color="gray.400">
            {(value.size / 1024 / 1024).toFixed(2)} MB
          </Text>
        )}
      </VStack>
    </Box>
  );
}
```

Uwagi:
- Dostosuj kolory do `appColors` z `theme/tokens/colors.ts` (nie używaj wartości hardcoded jeśli projekt ma tokeny)
- Sprawdź jaka ikona pasuje stylistycznie — przejrzyj inne komponenty UI

## Krok 3 — Stwórz AICostImportModal

Plik: `src/components/CostTracker/AICostImportModal.tsx`

### Props:
```typescript
interface AICostImportModalProps {
  isOpen: boolean;
  onClose: () => void;
  tenantId: string;
  projectId: string;
  costType: CostDocumentType;
  /** Wywoływane po pomyślnym zatwierdzeniu */
  onSuccess: () => void;
}
```

### Logika kroków (2 kroki zarządzane przez `useState<'upload' | 'preview'>`):

**Krok 'upload':**
- Title: "Importuj koszt z dokumentu"
- Treść: `DocumentDropzone` + opis "Załaduj fakturę lub rachunek w formacie JPG, PNG lub PDF."
- Stopka AppModal: `Button "Analizuj dokument"` (disabled gdy brak pliku, isLoading podczas parsowania)
- Po kliknięciu "Analizuj": wywołaj `mutateAsync({ file, costType })` → przejdź do kroku 'preview'
- Błąd: pokaż toast error

**Krok 'preview':**
- Title: "Sprawdź dane kosztu"
- Treść:
  1. Jeśli `confidence < 0.7` → `<Alert status="warning">Niska pewność odczytu AI. Sprawdź dane dokładnie.</Alert>`
  2. Jeśli `!parsedData.contractorFound && parsedData.suggestedContractor` → `<Alert status="info">Nie znaleziono kontrahenta "{parsedData.contractorName}" w bazie. Dodaj go ręcznie po zapisaniu kosztu.</Alert>`
  3. Formularz edytowalny (lokalny state `formData` inicjalizowany z `parsedData`):
     - `FormControl` Nazwa (required, `<Input>`)
     - `FormControl` Numer faktury (`<Input>`)
     - `FormControl` Kwota netto (`<NumberInput>`)
     - `FormControl` Kwota brutto (`<NumberInput>`) — TYLKO gdy `costType === 'ProjectCost'`
     - `FormControl` Wykonawca (`ContractorPicker` jeśli jest dostępny, lub `<Input>` z `contractorId`)
     - `FormControl` Data (`<Input type="date">`)
     - `FormControl` Opis (`<Textarea>`)
- Stopka: `Button "Wróć"` (variant="ghost") + `Button "Potwierdź i dodaj koszt"` (colorScheme="green", isLoading podczas zapisu)

### Mapowanie ParsedCostDto → stan formularza:
```typescript
const initialForm = {
  name: parsedData.name ?? '',
  number: parsedData.number ?? '',
  net: parsedData.net != null ? String(parsedData.net) : '',
  gross: parsedData.gross != null ? String(parsedData.gross) : '',
  date: parsedData.date ? parsedData.date.substring(0, 10) : '',
  contractorId: parsedData.contractorFound ? (parsedData.contractorId ?? null) : null,
  description: parsedData.description ?? '',
};
```

### Zapis po "Potwierdź i dodaj koszt":
```typescript
// Dla TrackedCost:
const formData = new FormData();
formData.append('TenantId', tenantId);
formData.append('ProjectId', projectId);
formData.append('Name', form.name);
// ... inne pola
await costTrackerApi.createCost(tenantId, projectId, formData);

// Dla ProjectCost:
await projectApi.createProjectCost({
  tenantId,
  projectId,
  name: form.name,
  // ... inne pola
  date: new Date(form.date),
});
```

**WAŻNE**: Przeczytaj dokładnie jak `costTrackerApi.createCost` i `projectApi.createProjectCost` przyjmują dane — dopasuj do ich interfejsów dokładnie. Nie zgaduj pól.

Po sukcesie: wywołaj `onSuccess()` i `onClose()`.
Po błędzie: obsłuż przez `handleApiError` + toast (wzorzec z CostFormModal).

### Wzorzec AppModal:
```tsx
<AppModal
  isOpen={isOpen}
  onClose={onClose}
  title={step === 'upload' ? 'Importuj koszt z dokumentu' : 'Sprawdź dane kosztu'}
  footer={/* stopka zależna od kroku */}
  size="lg"
>
  {/* treść zależna od kroku */}
</AppModal>
```
Przeczytaj AppModal.tsx — sprawdź dokładnie jakie props przyjmuje (title, footer, size, itp.).

## Krok 4 — Integracja w CostFormModal.tsx

W `CostFormModal.tsx`:

1. Dodaj `useState` dla `isAIImportOpen` (boolean, domyślnie `false`)
2. W renderStep case 3 — zawinąć zawartość w `<VStack spacing={4} align="stretch">` (jeśli nie jest już) i PRZED `<CostForm ...>` dodać:
```tsx
<Button
  leftIcon={/* ikona Wand2 lub podobna */}
  variant="outline"
  size="sm"
  colorScheme="blue"
  alignSelf="flex-start"
  onClick={() => setIsAIImportOpen(true)}
  isDisabled={isSubmitting}
>
  Importuj z dokumentu
</Button>
```
3. Po renderStep — dodaj `<AICostImportModal>` z:
   - `isOpen={isAIImportOpen}`
   - `onClose={() => setIsAIImportOpen(false)}`
   - `tenantId={tenantId}`
   - `projectId={projectId}`
   - `costType="TrackedCost"`
   - `onSuccess={() => { setIsAIImportOpen(false); handleClose(); onSuccess?.(); }}`

## Krok 5 — Integracja w CostFormDrawer.tsx

W `CostFormDrawer.tsx`:

1. Dodaj `useState` dla `isAIImportOpen`
2. W DrawerBody, jako PIERWSZY element wewnątrz głównego VStack (gdy `!isEdit`):
```tsx
{!isEdit && (
  <Button
    leftIcon={/* ikona */}
    variant="outline"
    size="sm"
    colorScheme="blue"
    alignSelf="flex-start"
    onClick={() => setIsAIImportOpen(true)}
  >
    Importuj z dokumentu
  </Button>
)}
```
3. Dodaj `<AICostImportModal>` tak samo jak w CostFormModal

## Krok 6 — Integracja w CostModal.tsx (dashboard)

W `src/features/dashboard/components/CostModal.tsx`:

1. Dodaj `useState` dla `isAIImportOpen`
2. W body AppModal, w głównym VStack, jako PIERWSZY element (gdy `mode === 'create'`):
```tsx
{mode === 'create' && (
  <Button
    leftIcon={/* ikona */}
    variant="outline"
    size="sm"
    colorScheme="blue"
    alignSelf="flex-start"
    onClick={() => setIsAIImportOpen(true)}
  >
    Importuj z dokumentu
  </Button>
)}
```
3. Dodaj `<AICostImportModal>`:
   - `costType` — sprawdź czy CostModal ma prop określający typ kosztu (`type: 'tracked' | 'project'`) — przekaż odpowiednio jako `CostDocumentType`
   - `onSuccess` → wywołuje `onClose()` + invaliduje query

## Weryfikacja

```
npx tsc --noEmit
```
Napraw wszystkie błędy TypeScript przed zgłoszeniem gotowości.

Sprawdź szczególnie:
- Importy `CostDocumentType` z `../../types/ai.types`
- Props `AppModal` — czy wszystkie required props są przekazane
- Props `ContractorPicker` — czy pasuje do interfejsu
