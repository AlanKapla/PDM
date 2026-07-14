# UI Fix 01 — Typy TypeScript + API client

## Cel
Aktualizacja typów TypeScript i klienta API o obsługę hierarchii katalogów.

## Workspace
`C:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI`

## Skill
Przeczytaj: `.opencode/skills/ui/skill-ui-types.md`
Przeczytaj: `.opencode/skills/ui/skill-ui-api-client.md`

## Pliki do zmiany

### 1. `src/types/project.types.ts`

**Modyfikacja `ProjectFilePackageWeb`:**

Obecny interfejs (fragment):
```typescript
export interface ProjectFilePackageWeb {
  id: string;
  name: string;
  createdAt: string;
  ownerId: string;
  ownerName: string;
  files: ProjectFileWeb[];
  totalFiles: number;
}
```

Dodać dwa pola:
```typescript
parentId: string | null;           // null = katalog główny (root)
subCatalogs: ProjectFilePackageWeb[]; // rekurencja — TypeScript obsługuje to w interface
```

**Nowy typ `CreateDirectoryPayload`** (może być inline albo jako osobny interfejs):
```typescript
export interface CreateDirectoryPayload {
  directoryName: string;
  parentId?: string | null;
}
```

### 2. `src/api/projectApi.ts`

**Modyfikacja `createPackageAndUploadFiles`:**

Dodać opcjonalny parametr `parentId?: string` do sygnatury i append do FormData:
```typescript
if (parentId) {
  formData.append('ParentId', parentId);
}
```

**Nowa funkcja `createDirectory`:**
```typescript
createDirectory: async (
  tenantId: string,
  projectId: string,
  directoryName: string,
  parentId?: string | null
) => {
  return axiosClient.post<void>(
    `/tenants/${tenantId}/projects/${projectId}/file/directories`,
    { directoryName, parentId: parentId ?? null }
  );
},
```

Dodać tę funkcję do obiektu `projectApi` (lub pliku API, w którym są funkcje plików) — sprawdź jak inne funkcje są organizowane.

## Weryfikacja
```
npx tsc --noEmit
```
Brak błędów TypeScript.
