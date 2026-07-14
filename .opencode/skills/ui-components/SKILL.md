---
name: ui-components
description: "Tworzenie komponentów React z Chakra UI — wzorce, stany loading/error/empty, renderowanie list. Użyj gdy tworzysz lub modyfikujesz komponent React (*.tsx)."
---

# Skill: UI / Komponenty React

## Opis
Tworzenie komponentów React z Chakra UI — wzorce, stany loading/error/empty, renderowanie list.

## Kiedy używać
Użyj tego skilla gdy tworzysz lub modyfikujesz komponent React (*.tsx).

---

## Lokalizacja

```
src/features/{domain}/components/    ← komponenty domenowe
src/components/ui/                   ← bazowe elementy UI (AppModal, DeleteAlertDialog…)
src/components/common/               ← komponenty pomocnicze (LoadingSpinner…)
```

## Wzorzec komponentu

```tsx
// src/features/dashboard/components/DashboardHeader.tsx
import React from 'react';
import { Box, HStack, Text } from '@chakra-ui/react';
import type { ProjectDashboardWeb } from '../types/projectDashboard.types';

export interface DashboardHeaderProps {
    data: ProjectDashboardWeb;
    projectName: string;
    onEdit?: () => void;
}

export function DashboardHeader({
    data,
    projectName,
    onEdit,
}: DashboardHeaderProps): React.ReactElement {
    return (
        <Box mb={5}>
            <HStack justify="space-between">
                <Text fontSize="lg" fontWeight="semibold">
                    {projectName}
                </Text>
                {onEdit && (
                    <Button size="sm" onClick={onEdit}>
                        Edytuj
                    </Button>
                )}
            </HStack>
        </Box>
    );
}
```

## Stany loading/error/empty

```tsx
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { EmptyState } from '../components/ui/EmptyState';
import { ErrorState } from '../components/ui/ErrorState';

function ProjectList(): React.ReactElement {
    const { data, isLoading, error } = useProjects(tenantId);

    if (isLoading) {
        return <LoadingSpinner />;
    }

    if (error) {
        return <ErrorState message={error} />;
    }

    if (!data || data.length === 0) {
        return <EmptyState title="Brak projektów" />;
    }

    return (
        <Box>
            {data.map((project) => (
                <ProjectCard key={project.id} project={project} />
            ))}
        </Box>
    );
}
```

## Lista z kluczem

```tsx
// Zawsze key przez unikalne id, nie przez index
{items.map((item) => (
    <ItemCard key={item.id} item={item} />
))}
```

## Warunkowe renderowanie

```tsx
// Short circuit (proste warunki)
{canEdit && <EditButton onClick={handleEdit} />}

// Ternary (dwa warianty)
{isActive ? <ActiveBadge /> : <InactiveBadge />}

// Osobna zmienna (złożone warunki)
const content = isLoading
    ? <LoadingSpinner />
    : <ProjectDetails data={data} />;

return <Box>{content}</Box>;
```

## Klikalny wiersz tabeli otwierający modal szczegółów

Wzorzec obowiązujący w całej aplikacji. Kliknięcie wiersza otwiera modal edycji/szczegółów. Przyciski akcji w wierszu zatrzymują propagację (`e.stopPropagation()`), żeby nie wywoływać obu handlerów jednocześnie.

```tsx
// Wiersz klikalny
<Tr
  key={item.id}
  cursor="pointer"
  _hover={{ bg: "neutral.50" }}
  onClick={() => handleOpenEdit(item)}
>
  <Td>{item.name}</Td>
  {canEdit && (
    <Td>
      <HStack spacing={1}>
        <Tooltip label="Edytuj">
          <IconButton
            aria-label="Edytuj"
            icon={<Edit2 size={14} />}
            size="xs"
            variant="ghost"
            onClick={(e) => { e.stopPropagation(); handleOpenEdit(item); }}
          />
        </Tooltip>
        <Tooltip label="Usuń">
          <IconButton
            aria-label="Usuń"
            icon={<Trash2 size={14} />}
            size="xs"
            variant="ghost"
            colorScheme="red"
            onClick={(e) => { e.stopPropagation(); handleOpenDelete(item); }}
          />
        </Tooltip>
      </HStack>
    </Td>
  )}
</Tr>
```

**Zasady:**
- `cursor="pointer"` + `_hover={{ bg: "neutral.50" }}` na `<Tr>`
- `onClick` na `<Tr>` otwiera modal szczegółów / edycji
- Każdy `onClick` w przyciskach akcji wewnątrz wiersza musi wywołać `e.stopPropagation()`

## Zasady

- Jeden plik = jeden komponent
- Zawsze `interface {Component}Props`
- Zwracaj `React.ReactElement` (nie `JSX.Element`)
- Logika w hookach, komponent tylko renderuje
- Named exports dla komponentów domenowych
- Default export dla komponentów z `components/ui/` (konwencja biblioteki)
- Zakaz inline styles — zawsze Chakra UI props
- Zakaz hardkodowanych kolorów — używaj tokenów (`primary.600`, `level1.500`)
- Eventy przez callbacki w props (`onEdit`, `onDelete`, `onChange`)

## Dostępność — WCAG AA (obowiązkowe)

Czytaj `skill-ui-accessibility.md` dla pełnych zasad. Skrót:

- Każdy `IconButton` musi mieć `aria-label`
- Ikony obok tekstu muszą mieć `aria-hidden="true"`
- `div`/`span` z `onClick` musi mieć `role="button"`, `tabIndex={0}` i `onKeyDown` (Enter + Space)
- Tekst treści: kontrast ≥ 4.5:1 (używaj `neutral.600+`, nie `neutral.500` dla treści)
- Komunikaty błędów: `role="alert"` lub Chakra `Alert status="error"`
- Każdy komponent musi przechodzić test AXE (patrz `skill-ui-unit-tests.md`)
