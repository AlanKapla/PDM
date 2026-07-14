# multi-tab-resources-fix-01 — Naprawa ERR_INSUFFICIENT_RESOURCES przy wielu kartach

## Kontekst audytu

### Objaw
Przy ≥2 otwartych kartach aplikacji w konsoli pojawiają się błędy:
`net::ERR_INSUFFICIENT_RESOURCES` na endpointach:
- `/api/tenants/invitations`
- `/api/projects/invitations`
- `/api/tenants/{id}/projects`
- `/api/tenants/my-tenants`

Stack trace wskazuje na `queryClient.resetQueries()` → `refetchQueries()` → masowy refetch wszystkich query.

### Główna przyczyna (P0)
**`DemoContext.tsx`** nasłuchuje `window.addEventListener("storage", ...)` i przy KAŻDYM evencie `storage` wywołuje `queryClient.resetQueries()`.

Problem:
- Demo mode jest przechowywany w **sessionStorage** (`src/api/mock/index.ts`, klucz `demoMode`)
- Event `storage` odpala się **tylko** dla zmian **localStorage** w innych kartach — NIE dla sessionStorage
- MSAL (`enableAccountStorageEvents()` w `main.tsx`) intensywnie zapisuje do localStorage (tokeny, cache)
- Każde odświeżenie tokenu MSAL w jednej karcie → event `storage` w pozostałych kartach → `resetQueries()` → lawina HTTP requestów → wyczerpanie puli połączeń przeglądarki

### Dodatkowe czynniki (P1)
1. **`Sidebar.tsx`** — `refetchInterval: 30000` na `useActiveInvitations` i `useActiveProjectInvitations` działa także w tle (brak `refetchIntervalInBackground: false`)
2. **`AuthContext.tsx`** — ping SignalR co 15s i `forceRestart` przy `visibilitychange` działają w każdej karcie niezależnie od widoczności
3. **`TenantAccessGuard.tsx`** — bezpośrednie wywołania API poza React Query (mniejszy wpływ, ale duplikuje requesty)

### Już OK
- `main.tsx` ma `refetchOnWindowFocus: false` globalnie

---

## Zmiany do wykonania

### 1. Napraw `DemoContext.tsx` (P0 — krytyczne)

Usuń globalny listener `storage` który resetuje query przy każdej zmianie localStorage.

Zamiast tego:
- Usuń `useEffect` z `window.addEventListener("storage", onStorage)` — jest błędny (sessionStorage nie emituje storage events) i szkodliwy (reaguje na MSAL)
- Demo mode pozostaje w sessionStorage (per-tab) — to jest poprawne zachowanie
- `toggleDemoMode` nadal wywołuje `resetQueries()` lokalnie — bez zmian

Jeśli w przyszłości potrzebna synchronizacja cross-tab demo mode:
- użyć `BroadcastChannel` lub przenieść klucz do localStorage i filtrować `e.key === 'demoMode'`
- Na razie NIE implementuj cross-tab sync — tylko usuń szkodliwy listener

### 2. Ogranicz polling w tle — `Sidebar.tsx` (P1)

W `useActiveInvitations` i `useActiveProjectInvitations` dodaj opcje:
```typescript
refetchIntervalInBackground: false,
```

Dodaj też domyślne `refetchIntervalInBackground: false` w hookach:
- `src/hooks/queries/useTenants.ts` — `useActiveInvitations`
- `src/hooks/queries/useProjectInvitations.ts` — `useActiveProjectInvitations`

### 3. Ogranicz SignalR health check do widocznej karty — `AuthContext.tsx` (P1)

W efekcie z pingiem co 15s:
- Na początku callbacka interval: `if (document.hidden) return;`
- Nie wywołuj `forceRestart` gdy karta jest w tle

W `handleVisibilityChange`:
- Zostaw restart tylko gdy `!document.hidden` (już jest) — OK
- Dodaj debounce/throttle jeśli łatwe (opcjonalnie, nie blokuj jeśli skomplikowane)

### 4. Testy (opcjonalnie, tylko jeśli proste)

Jeśli istnieją testy dla DemoContext — zaktualizuj.
Nie twórz nowych plików testowych jeśli nie ma istniejących.

---

## Konwencje UI
- Brak `any` — explicit types
- Logika w hookach/kontekstach
- Minimalny diff — tylko powyższe zmiany

## Weryfikacja
Po zmianach uruchom z katalogu `01-Applications/ProjectDataManagementUI`:
```
npm run build
```

## Raport końcowy
Zwróć standardowy raport refactor-agent z listą zmodyfikowanych plików i wynikiem builda.
