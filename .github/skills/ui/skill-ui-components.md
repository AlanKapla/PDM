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
