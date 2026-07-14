# permissions-ui-fix-01 — Typy TypeScript

## Zadanie

Zaktualizuj typy TypeScript — usuń `ModuleAccessLevel`, presets; uprość `ProjectMemberWeb` do listy modułów.

## Krok 1 — projectModulePermissions.ts

Plik: `src/types/projectModulePermissions.ts`

Usuń:
- Cały `const ModuleAccessLevel = { ... }` i `type ModuleAccessLevel = ...`
- Cały `ProjectMemberPresets` (Admin, Editor, Viewer, Contractor, Investor)
- `interface ProjectMemberModulePermission` (jeśli istnieje) — lub uprość do `{ module: ProjectModule }`

Zachowaj:
- `const ProjectModule = { Settings: 0, Members: 1, Files: 2, Estimates: 3, Costs: 4, Schedule: 5, Dashboard: 6, Chat: 7, Tracker: 8 } as const`
- `type ProjectModule = typeof ProjectModule[keyof typeof ProjectModule]`

Nowa zawartość pliku:
```typescript
export const ProjectModule = {
  Settings: 0,
  Members: 1,
  Files: 2,
  Estimates: 3,
  Costs: 4,
  Schedule: 5,
  Dashboard: 6,
  Chat: 7,
  Tracker: 8,
} as const;

export type ProjectModule = (typeof ProjectModule)[keyof typeof ProjectModule];

export const PROJECT_MODULE_LABELS: Record<ProjectModule, string> = {
  [ProjectModule.Settings]: "Ustawienia",
  [ProjectModule.Members]: "Członkowie",
  [ProjectModule.Files]: "Pliki",
  [ProjectModule.Estimates]: "Kosztorysy",
  [ProjectModule.Costs]: "Koszty",
  [ProjectModule.Schedule]: "Harmonogram",
  [ProjectModule.Dashboard]: "Dashboard",
  [ProjectModule.Chat]: "Chat",
  [ProjectModule.Tracker]: "Tracker",
};
```

## Krok 2 — project.types.ts

Plik: `src/types/project.types.ts`

Znajdź i zaktualizuj interfejs `ProjectMemberWeb`:

**Stare:**
```typescript
export interface ModulePermissionWeb {
  module: number;
  accessLevel: number;
}

export interface ProjectMemberWeb {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  joinedAt: string;
  isAdmin: boolean;
  modulePermissions: ModulePermissionWeb[];
}
```

**Nowe:**
```typescript
export interface ProjectMemberWeb {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  joinedAt: string;
  isAdmin: boolean;
  modules: number[];
}
```

Usuń `ModulePermissionWeb` — nie jest już potrzebny.

## Krok 3 — Weryfikacja importów

Sprawdź czy `ModuleAccessLevel`, `ModulePermissionWeb`, `ProjectMemberPresets` są importowane gdziekolwiek:

```bash
# W terminalu:
grep -r "ModuleAccessLevel\|ModulePermissionWeb\|ProjectMemberPresets" src/ --include="*.ts" --include="*.tsx" -l
```

Pliki które importują te typy będą naprawiane w kolejnych krokach (fix-02, fix-03, fix-04).

## Weryfikacja TypeScript

```bash
npx tsc --noEmit 2>&1 | grep "projectModulePermissions\|project.types" | head -20
```
