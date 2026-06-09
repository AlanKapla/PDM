# Prompt UI-05: Przycisk "Stwórz z AI" w ProjectCosts.tsx

## Cel
Dodaj przycisk "Stwórz z AI" do strony listy kosztorysów i podłącz go do nowego modalu.

---

## Plik do modyfikacji

`src/pages/ProjectCosts.tsx`

---

## Krok 1: Dodaj import modalu

Na górze pliku, przy pozostałych importach komponentów:

```tsx
import GenerateCostEstimateWithAIModal from "../components/GenerateCostEstimateWithAIModal";
```

Dodaj też ikonę `Bot` do importu z lucide-react:
```tsx
import { Trash2, Plus, FileText, Copy, Share2, Users, Bot } from "lucide-react";
```

---

## Krok 2: Dodaj useDisclosure dla nowego modalu

W ciele funkcji `ProjectCosts`, **po** istniejących `useDisclosure`:

```tsx
  const { isOpen: isAIModalOpen, onOpen: onAIModalOpen, onClose: onAIModalClose } = useDisclosure();
```

---

## Krok 3: Dodaj prop onAIModalOpen do CostEstimatesTabProps

Znajdź interfejs `CostEstimatesTabProps` i dodaj nowy prop:

```tsx
  /** Otwiera modal generowania kosztorysu z AI */
  onAIModalOpen: () => void;
```

---

## Krok 4: Dodaj prop do CostEstimatesTable

Znajdź destrukturyzację propsów w `const CostEstimatesTable = React.memo<CostEstimatesTabProps>(({` i dodaj `onAIModalOpen`.

---

## Krok 5: Dodaj przycisk w CostEstimatesTable

W komponencie `CostEstimatesTable` znajdź miejsce gdzie renderowany jest przycisk "Nowy kosztorys".
Wygląda to tak:

```tsx
<Button 
  colorScheme="primary" 
  leftIcon={<Plus size={18} />}
  onClick={onCreateModalOpen}
>
  Nowy kosztorys
</Button>
```

**Zastąp** (lub uzupełnij za pomocą `HStack`) tak, żeby były dwa przyciski obok siebie:

```tsx
<HStack spacing={2}>
  <Button
    colorScheme="purple"
    variant="outline"
    leftIcon={<Bot size={18} />}
    onClick={onAIModalOpen}
    size="sm"
  >
    Stwórz z AI
  </Button>
  <Button
    colorScheme="primary"
    leftIcon={<Plus size={18} />}
    onClick={onCreateModalOpen}
    size="sm"
  >
    Nowy kosztorys
  </Button>
</HStack>
```

**Ważne:** Przyciski powinny być widoczne tylko gdy `resourcePerms.actions.canCreate` jest true (jeśli taka flaga istnieje w `resourcePerms`). Sprawdź strukturę `resourcePerms` i zastosuj odpowiednie warunkowanie — jeśli tworzenie jest już warunkowane przez `resourcePerms`, przenieś oba przyciski do tego samego bloku warunkowego.

---

## Krok 6: Przekaż prop onAIModalOpen do wszystkich wywołań CostEstimatesTable

W funkcji `ProjectCosts`, gdzie renderowane są `CostEstimatesTable` (dla zakładki Moje, Wszystkie, Udostępnione), dodaj:

```tsx
onAIModalOpen={onAIModalOpen}
```

---

## Krok 7: Dodaj modal na końcu JSX komponentu ProjectCosts

W sekcji z modalami (obok `CreateCostEstimateModal`, `CopyCostEstimateModal` itp.), dodaj:

```tsx
<GenerateCostEstimateWithAIModal
  isOpen={isAIModalOpen}
  onClose={onAIModalClose}
  tenantId={user?.activeTenantId ?? ''}
  projectId={projectId ?? ''}
  onCostEstimateCreated={(id) => {
    onAIModalClose();
    refreshData();
    navigate(`/projects/${projectId}/cost-estimates/${id}`);
  }}
/>
```

---

## Uwagi implementacyjne

1. **`size="sm"`** na przyciskach — dopasuj do istniejącego stylu przycisków na stronie. Jeśli inne przyciski nie mają `size`, usuń tę właściwość.

2. **Duplikacja HStack** — sprawdź czy przyciski są już opakowane w `HStack`. Jeśli tak, po prostu dodaj nowy przycisk do istniejącego `HStack`.

3. **`user?.activeTenantId`** — sprawdź czy `user` jest dostępny w tym zasięgu. Jeśli tak używany w reszcie komponentu, użyj tego samego wzorca.

4. **Nawigacja po zapisie** — `navigate` jest już importowany i używany w `ProjectCosts`. Przekazanie `onCostEstimateCreated` z nawigacją przeniesie użytkownika od razu do nowego kosztorysu.

---

## Weryfikacja

```
npx tsc --noEmit 2>&1 | Select-String "ProjectCosts|error TS" | Select-Object -First 20
Write-Host "Exit: $LASTEXITCODE"
```

Oczekiwany wynik: brak błędów dla ProjectCosts.tsx.
