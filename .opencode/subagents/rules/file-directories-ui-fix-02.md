# UI Fix 02 — Hooki React Query

## Cel
Dodanie hooka `useCreateDirectory` do `useProjectFiles.ts`.

## Workspace
`C:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI`

## Skill
Przeczytaj: `.github/skills/ui/skill-ui-hooks.md`

## Plik do zmiany

### `src/hooks/queries/useProjectFiles.ts`

Dodać nowy hook `useCreateDirectory` wzorując się na innych `useMutation` hookach w tym pliku.

Hook powinien:
- Wywoływać `projectApi.createDirectory(tenantId, projectId, directoryName, parentId)`
- Po `onSuccess`: invalidować `fileKeys.all` lub `fileKeys.packages` (sprawdź jakie query keys są używane w pliku)
- Zwracać standardowy wynik `useMutation`

Szablon:
```typescript
export function useCreateDirectory() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      tenantId,
      projectId,
      directoryName,
      parentId,
    }: {
      tenantId: string;
      projectId: string;
      directoryName: string;
      parentId?: string | null;
    }) => projectApi.createDirectory(tenantId, projectId, directoryName, parentId),

    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: fileKeys.packages(variables.tenantId, variables.projectId) });
      // lub fileKeys.all jeśli taki istnieje
    },
  });
}
```

Sprawdź dokładnie strukturę `fileKeys` w pliku i użyj poprawnych kluczy.

## Weryfikacja
```
npx tsc --noEmit
```
Brak błędów TypeScript.
