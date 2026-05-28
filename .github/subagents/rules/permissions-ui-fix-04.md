# permissions-ui-fix-04 — AddProjectMemberModal + EditProjectMemberModal → checkboxy

## Zadanie

Przepisz oba modale — zastąp dropdowny poziomów dostępu checkboxami modułów. Napraw brakujący Tracker w liście modułów.

## Krok 1 — Przeczytaj istniejące pliki

Przed zmianami przeczytaj w całości:
- `src/components/AddProjectMemberModal.tsx`
- `src/components/EditProjectMemberModal.tsx`

## Krok 2 — AddProjectMemberModal.tsx

### Zmiany do wprowadzenia:

**1. Usuń stałą ACCESS_LEVELS** (cała definicja Record z opcjami poziomów)

**2. Zamień state:**
```typescript
// STARE
const [modulePermissions, setModulePermissions] = useState<Record<number, number>>({});

// NOWE
const [selectedModules, setSelectedModules] = useState<Set<number>>(new Set());
```

**3. Dodaj listę modułów z etykietami** (wszystkie 9 modułów łącznie z Tracker):
```typescript
import { ProjectModule, PROJECT_MODULE_LABELS } from '../types/projectModulePermissions';

const ALL_MODULES = Object.values(ProjectModule) as number[];
```

**4. Zamień render sekcji uprawnień:**

Zamiast tabeli z Select per moduł — wyrenderuj checkboxy:
```tsx
<Stack spacing={2}>
  <Text fontWeight="medium" fontSize="sm">Dostęp do modułów:</Text>
  {ALL_MODULES.map((mod) => (
    <Checkbox
      key={mod}
      isChecked={selectedModules.has(mod)}
      onChange={(e) => {
        setSelectedModules((prev) => {
          const next = new Set(prev);
          if (e.target.checked) {
            next.add(mod);
          } else {
            next.delete(mod);
          }
          return next;
        });
      }}
    >
      {PROJECT_MODULE_LABELS[mod as ProjectModule]}
    </Checkbox>
  ))}
</Stack>
```

**5. Zamień payload przy submit:**
```typescript
// STARE
const permissionsArray = Object.entries(modulePermissions)
  .filter(([, level]) => level > 0)
  .map(([mod, level]) => ({ module: Number(mod), accessLevel: level }));
await projectApi.addProjectMember(tenantId, projectId, userId, permissionsArray);

// NOWE
const modules = Array.from(selectedModules);
await projectApi.addProjectMember(tenantId, projectId, userId, modules);
```

**6. Reset state przy zamknięciu/otwarciu:**
Tam gdzie stary `setModulePermissions({})` → `setSelectedModules(new Set())`

**7. Usuń presety** (przyciski Admin/Editor/Viewer/Contractor/Investor jeśli są w modalach) — usunąć całkowicie.

## Krok 3 — EditProjectMemberModal.tsx

Identyczne zmiany jak powyżej, PLUS:

**Inicjalizacja state z istniejącymi uprawnieniami:**
```typescript
// STARE
const initialPermissions = member.modulePermissions.reduce<Record<number, number>>(
  (acc, mp) => ({ ...acc, [mp.module]: mp.accessLevel }),
  {}
);
const [modulePermissions, setModulePermissions] = useState(initialPermissions);

// NOWE
const initialModules = new Set(member.modules ?? []);
const [selectedModules, setSelectedModules] = useState<Set<number>>(initialModules);
```

Zaktualizuj też payload przy submit (analogicznie jak w Add).

## Krok 4 — Importy

Usuń importy które nie są już potrzebne:
- `ModuleAccessLevel` z `projectModulePermissions`
- `ProjectMemberPresets` jeśli był importowany
- `Select, option` jeśli były używane tylko dla modułów (sprawdź czy używane gdzie indziej)

Dodaj importy:
- `Checkbox` z `@chakra-ui/react` (jeśli nie jest już importowany)
- `PROJECT_MODULE_LABELS, ProjectModule` z `../types/projectModulePermissions`

## Weryfikacja

```bash
npx tsc --noEmit 2>&1 | grep "AddProjectMemberModal\|EditProjectMemberModal\|error TS" | head -20
```
