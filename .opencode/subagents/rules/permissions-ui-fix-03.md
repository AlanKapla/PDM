# permissions-ui-fix-03 — useResourcePermissions

## Zadanie

Zaktualizuj `useResourcePermissions` — zachowaj istniejącą strukturę interfejsu `ResourcePermissions` (tabs/mine/all/shared), ale mapuj na nowe proste kody modułowe. To minimalizuje zmiany w stronach które korzystają z tego hooka.

## Strategia (Opcja A — minimalne zmiany)

Zachowaj interfejs `ResourcePermissions` bez zmian. Zmień tylko logikę wewnętrzną — wszystkie flagi mapują na jeden bool per moduł.

## Krok 1 — Przeczytaj istniejący plik

Przeczytaj `src/hooks/useResourcePermissions.ts` w całości przed wprowadzeniem zmian.

## Krok 2 — Zaktualizuj logikę

Znajdź w pliku `useResourcePermissions` fragment gdzie budowane są `ResourcePermissions`. Zastąp granularne sprawdzenia jednym:

**Dla plików (Files):**
```typescript
const canFiles = hasPermission(permissions, PermissionCodes.ProjectFiles);

// tabs
const tabs = {
  showAll: canFiles,
  showMine: canFiles,
  showShared: canFiles,
};

// mine
const mine = {
  showMine: canFiles,
  canCreate: canFiles,
  canEdit: canFiles,
  canDelete: canFiles,
  canShare: canFiles,
  canManageShare: canFiles,
};

// shared
const shared = {
  showShared: canFiles,
  canEdit: canFiles,
  canReadOnly: false,
};

// all
const all = {
  showAll: canFiles,
  canCreate: canFiles,
  canEdit: canFiles,
  canDelete: canFiles,
  canShare: canFiles,
  canManageShare: canFiles,
};
```

**Dla kosztorysów (Estimates):** analogicznie z `PermissionCodes.ProjectEstimates`

**Dla harmonogramów (Schedule):** analogicznie z `PermissionCodes.ProjectSchedule`

## Krok 3 — Sprawdź i zachowaj structurę zwracaną

Hook musi nadal zwracać ten sam shape co `ResourcePermissions` interface — sprawdź istniejący typ i nie zmieniaj struktury.

## Krok 4 — Usuń granularne sprawdzenia

Usuń wszelkie odwołania do starych `PermissionCodes`:
- `ProjectFilesReadShared`, `ProjectFilesReadOwn`, `ProjectFilesWriteOwn` itp.
- `ProjectEstimatesReadShared` itp.
- `ProjectScheduleReadShared` itp.

## Weryfikacja

```bash
npx tsc --noEmit 2>&1 | grep "useResourcePermissions\|error TS" | head -20
```
