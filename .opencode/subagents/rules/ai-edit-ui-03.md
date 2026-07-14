# Prompt: ai-edit-ui-03 — Integracja w CostEstimateToolbar + CostEstimateEditPage

## Cel

Dodać przycisk "Edytuj z AI" w toolbarze i zintegrować modal AIEditCostEstimateModal w CostEstimateEditPage.

## Pliki do modyfikacji

### 1. `ProjectDataManagementUI/src/components/CostEstimateToolbar.tsx`

**W Props dodać:**
```typescript
canUseAI?: boolean;
onAIEdit: () => void;
```

**W ActionDef[] (nowa grupa lub w otherActions, około linii 150):**
Dodać po istniejących `otherActions` lub jako osobna grupa przed harmonogramem:
```typescript
{
  id: "ai-edit",
  icon: <Zap size={14} />,
  label: "Edytuj z AI",
  tooltip: "Edytuj kosztorys przy pomocy AI",
  onClick: onAIEdit,
  colorScheme: "purple",
  variant: "outline",
  isVisible: canUseAI && isEditMode,
}
```

**W renderze toolbara:**
Grupa przycisków AI powinna być widoczna między `otherActions` a harmonogramem. Użyj tego samego wzorca co istniejące akcje (badanie `bp` dla widoczności etykiet).

### 2. `ProjectDataManagementUI/src/pages/CostEstimateEditPage.tsx`

**Importy dodać:**
```typescript
import { useDisclosure } from '@chakra-ui/react';
import AIEditCostEstimateModal from '../components/AIEditCostEstimateModal';
```

**Stan modala (około linii gdzie są inne useDisclosure):**
```typescript
const { isOpen: isAIEditOpen, onOpen: onAIEditOpen, onClose: onAIEditClose } = useDisclosure();
```

**Handler przed otwarciem AI modala:**
```typescript
const handleAIEditOpen = useCallback(() => {
  // Blokuj jeśli są niezapisane zmiany
  if (hasChanges) {
    showWarningToast('Najpierw zapisz lub anuluj bieżące zmiany przed edycją AI.');
    return;
  }
  onAIEditOpen();
}, [hasChanges, onAIEditOpen, showWarningToast]);
```

**Callback po sukcesie AI edycji:**
```typescript
const handleAIEditSuccess = useCallback(() => {
  onAIEditClose();
  setHasChanges(false);
  loadCostEstimate(); // przeładowanie danych z API
}, [onAIEditClose, loadCostEstimate]);
```

**W toolbar — podmiana propów (około linii gdzie jest `<CostEstimateToolbar>`):**
```typescript
<CostEstimateToolbar
  ...
  canUseAI={canFullEdit}
  onAIEdit={handleAIEditOpen}
/>
```

**Modal w JSX (przed zamykającym `</MainLayout>` lub po toolbarze):**
```typescript
{canFullEdit && (
  <AIEditCostEstimateModal
    isOpen={isAIEditOpen}
    onClose={onAIEditClose}
    tenantId={user.activeTenantId}
    projectId={projectId}
    costEstimateId={estimateId}
    onEditSuccess={handleAIEditSuccess}
  />
)}
```

**Zmiana `canFullEdit` do props toolbara:**
Upewnij się że `canFullEdit` jest dostępne w scope strony (zazwyczaj już istnieje jako `accessLevel === 'Full'`).

## Weryfikacja

1. TypeScript kompiluje bez błędów
2. Przycisk "Edytuj z AI" pojawia się w toolbarze gdy `isEditMode=true && canUseAI=true`
3. Kliknięcie przycisku gdy `hasChanges=true` → blokada + toast
4. Kliknięcie przycisku gdy `hasChanges=false` → otwiera modal
5. Po sukcesie AI edycji → modal zamyka się, dane się przeładowują, `isEditMode` pozostaje true
