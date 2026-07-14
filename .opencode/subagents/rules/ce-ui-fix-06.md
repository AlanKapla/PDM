# ce-ui-fix-06 — Unit combobox ze słownikiem jednostek

## Cel
Pole "Jednostka" w wierszu pozycji zastąpić comboboxem, który:
- Pokazuje dropdown z jednostkami pobranymi z API (per projekt, z cachingiem)
- Pozwala wpisać własną jednostkę z palca (free-text)
- Autosave po wyborze lub wpisaniu

## Przeczytaj skill przed implementacją
`.github/skills/ui-hooks/SKILL.md`
`.github/skills/ui-api-client/SKILL.md`
`.github/skills/ui-components/SKILL.md`

---

## 1. Endpoint API

Po wdrożeniu `ce-api-fix-01`, endpoint to:
```
GET /api/tenants/{tenantId}/projects/{projectId}/units
```
Zwraca: `Array<{ id: string; name: string; order: number }>`

---

## 2. Klient API

Plik: `src/api/projectApi.ts` (lub utwórz `src/api/projectUnitsApi.ts`)

Dodaj funkcję:
```typescript
import axiosClient from './axiosClient';

export interface ProjectUnitDto {
  id: string;
  name: string;
  order: number;
}

export async function getProjectUnits(
  tenantId: string,
  projectId: string
): Promise<ProjectUnitDto[]> {
  const response = await axiosClient.get<ProjectUnitDto[]>(
    `/api/tenants/${tenantId}/projects/${projectId}/units`
  );
  return response.data;
}
```

---

## 3. Hook useProjectUnits

Plik: `src/hooks/useProjectUnits.ts`

```typescript
import { useQuery } from '@tanstack/react-query';
import { getProjectUnits } from '../api/projectApi'; // lub projectUnitsApi

export function useProjectUnits(tenantId: string, projectId: string) {
  return useQuery({
    queryKey: ['projectUnits', tenantId, projectId],
    queryFn: () => getProjectUnits(tenantId, projectId),
    staleTime: 5 * 60 * 1000, // 5 minut cache
    enabled: !!tenantId && !!projectId,
  });
}
```

---

## 4. Komponent UnitCombobox

Plik: `src/components/CostEstimate/UnitCombobox.tsx`

```typescript
import React, { useState, useRef, useEffect } from 'react';
import {
  Box,
  Input,
  List,
  ListItem,
  Text,
  Popover,
  PopoverTrigger,
  PopoverContent,
  PopoverBody,
} from '@chakra-ui/react';

interface UnitComboboxProps {
  value: string;
  units: string[];         // lista jednostek ze słownika
  onChange: (value: string) => void;
  onBlur?: () => void;
  isDisabled?: boolean;
  placeholder?: string;
  w?: string;
}

export const UnitCombobox: React.FC<UnitComboboxProps> = ({
  value,
  units,
  onChange,
  onBlur,
  isDisabled,
  placeholder = 'szt',
  w = 'full',
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [inputValue, setInputValue] = useState(value);
  const inputRef = useRef<HTMLInputElement>(null);

  // Sync from outside
  useEffect(() => {
    setInputValue(value);
  }, [value]);

  const filtered = units.filter((u) =>
    u.toLowerCase().startsWith(inputValue.toLowerCase())
  );

  const handleSelect = (unit: string) => {
    setInputValue(unit);
    onChange(unit);
    setIsOpen(false);
    onBlur?.();
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const v = e.target.value;
    setInputValue(v);
    onChange(v);
    setIsOpen(v.length > 0 && units.length > 0);
  };

  const handleBlur = () => {
    // Opóźnij zamknięcie żeby klik na opcji zdążył się zarejestrować
    setTimeout(() => {
      setIsOpen(false);
      onBlur?.();
    }, 150);
  };

  return (
    <Box position="relative" w={w}>
      <Input
        ref={inputRef}
        value={inputValue}
        onChange={handleInputChange}
        onFocus={() => setIsOpen(units.length > 0)}
        onBlur={handleBlur}
        isDisabled={isDisabled}
        placeholder={placeholder}
        size="sm"
        fontSize="12.5px"
        bg="transparent"
        border="none"
        borderRadius="6px"
        px={1}
        _focus={{
          bg: 'white',
          border: '1px solid',
          borderColor: 'primary.300',
          boxShadow: '0 0 0 2px rgba(47,108,236,0.12)',
        }}
        _hover={!isDisabled ? { bg: 'neutral.50' } : undefined}
        cursor={isDisabled ? 'not-allowed' : 'text'}
        autoComplete="off"
        aria-label="Jednostka miary"
        aria-autocomplete="list"
        aria-expanded={isOpen}
      />
      {isOpen && filtered.length > 0 && (
        <Box
          position="absolute"
          top="100%"
          left={0}
          zIndex={20}
          bg="white"
          border="1px solid"
          borderColor="neutral.200"
          borderRadius="8px"
          boxShadow="0 4px 16px rgba(20,33,47,0.12)"
          maxH="200px"
          overflowY="auto"
          minW="120px"
          mt={0.5}
        >
          <List>
            {filtered.map((unit) => (
              <ListItem
                key={unit}
                px={3}
                py={1.5}
                fontSize="13px"
                cursor="pointer"
                _hover={{ bg: 'primary.50', color: 'primary.700' }}
                bg={unit === value ? 'primary.25' : undefined}
                fontWeight={unit === value ? 600 : 400}
                onMouseDown={() => handleSelect(unit)} // onMouseDown żeby przed onBlur
                role="option"
                aria-selected={unit === value}
              >
                {unit}
              </ListItem>
            ))}
          </List>
        </Box>
      )}
    </Box>
  );
};
```

---

## 5. Integracja w TreeViewRow

Plik: `src/components/CostEstimate/TreeView/TreeViewRow.tsx`

`UnitCombobox` potrzebuje listy jednostek. Dodaj prop do `TreeViewRowProps` i `ItemRowProps`:
```typescript
projectUnits: string[];  // lista nazw jednostek ze słownika
```

W `CostEstimateTreeView.tsx` — pobierz jednostki i przekaż do TreeViewRow:

```typescript
// Import hooka
import { useProjectUnits } from '../../../hooks/useProjectUnits';

// W komponencie
const { data: unitsData } = useProjectUnits(tenantId, projectId);
const projectUnits = useMemo(
  () => (unitsData ?? []).map((u) => u.name),
  [unitsData]
);
```

Upewnij się że `CostEstimateTreeViewProps` ma `tenantId` i `projectId` — jeśli nie, dodaj je.

W `ItemRow`, w `renderBaseFieldCells`, zastąp case `col.id === 'unit'`:

```tsx
if (col.id === 'unit') {
  return (
    <Flex key="unit" flex="0 0 auto" w={w} pr={1}>
      <UnitCombobox
        value={item.unit ?? ''}
        units={projectUnits}
        onChange={(v) => {
          onFieldChange(groupId, item.id, 'unit', v);
        }}
        onBlur={() => {
          triggerBaseAutosave('unit', 'string', item.unit ?? '');
        }}
        isDisabled={!isEditMode}
        w="full"
      />
    </Flex>
  );
}
```

---

## 6. Przekazywanie tenantId i projectId

Sprawdź gdzie `CostEstimateTreeView` jest używany (`CostEstimateEditPage.tsx`). Jeśli `tenantId` i `projectId` nie są już przekazywane jako props — dodaj je do `CostEstimateTreeViewProps` i przekaż z page.

---

## Weryfikacja

1. Kliknięcie pola Jednostka otwiera dropdown z listą jednostek projektu
2. Wpisanie liter filtruje listę
3. Kliknięcie opcji zamyka dropdown i ustawia wartość
4. Wpisanie własnej jednostki (nie ma w liście) jest akceptowane (free-text)
5. Lista jednostek jest cachowana (kolejne otwarcia nie powodują nowego requesta)
6. Pole jest zablokowane w trybie view
