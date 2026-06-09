# permissions-ui-fix-05 — projectApi.ts (zmiana sygnatur)

## Zadanie

Zaktualizuj klienta API — zmień sygnatury `addProjectMember` i `updateProjectMemberPermissions` aby przyjmowały `modules: number[]` zamiast `modulePermissions: {module, accessLevel}[]`.

## Krok 1 — Przeczytaj plik

Przeczytaj `src/api/projectApi.ts` w całości.

## Krok 2 — Zmień sygnaturę addProjectMember

Znajdź funkcję `addProjectMember` i zaktualizuj:

**Stara sygnatura:**
```typescript
addProjectMember(
  tenantId: string,
  projectId: string,
  userId: string,
  modulePermissions: Array<{ module: number; accessLevel: number }>
): Promise<void>
```

**Nowa sygnatura:**
```typescript
addProjectMember(
  tenantId: string,
  projectId: string,
  userId: string,
  modules: number[]
): Promise<void>
```

**Stare ciało (body requestu):**
```typescript
{ userId, modulePermissions }
```

**Nowe ciało:**
```typescript
{ userId, modules }
```

## Krok 3 — Zmień sygnaturę updateProjectMemberPermissions

Znajdź funkcję `updateProjectMemberPermissions` (lub podobną) i zaktualizuj:

**Stara sygnatura:**
```typescript
updateProjectMemberPermissions(
  tenantId: string,
  projectId: string,
  userId: string,
  isAdmin: boolean,
  modulePermissions: Array<{ module: number; accessLevel: number }>
): Promise<void>
```

**Nowa sygnatura:**
```typescript
updateProjectMemberPermissions(
  tenantId: string,
  projectId: string,
  userId: string,
  isAdmin: boolean,
  modules: number[]
): Promise<void>
```

**Nowe ciało:**
```typescript
{ isAdmin, modules }
```

## Krok 4 — Usuń niepotrzebne typy

Jeśli w pliku jest lokalny typ `ModulePermissionInput` lub `{ module: number; accessLevel: number }` — usuń.

## Weryfikacja końcowa

```bash
npx tsc --noEmit 2>&1; echo "Exit: $?"
```

Oczekiwany rezultat: 0 błędów TypeScript.
