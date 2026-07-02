# UI Fix 03 — SignalR Hub Service + globalny toast (jak NotificationBell)

## Cel
Połączenie z `TechnicalDocumentationHub`, globalne powiadomienia toast po zakończeniu przetwarzania — **niezależnie od aktualnej strony** (wzorzec `NotificationBell`).

## Decyzje MVP
- Toast **globalnie** — mount w komponencie globalnym (Header lub AuthContext), nie tylko na stronie dokumentacji
- Invalidate queries list/detail/count po evencie
- Filtruj eventy po `tenantId` aktywnego tenanta (opcjonalnie `projectId` jeśli user w kontekście projektu)
- Hub path: `/api/hubs/technical-documentation`
- Event serwera: `ProcessingCompleted`

## Workspace
`C:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI`

## Skills
- `.cursor/skills/ui-hooks/SKILL.md`

## Zależności
- **ui-fix-01** — typy eventu
- **ui-fix-02** — query keys
- **api-fix-08** — hub musi być wdrożony do testów E2E (można implementować UI wcześniej)

## Pliki referencyjne
- `src/services/notificationHubService.ts` — singleton, MSAL token
- `src/services/chatHubService.ts` — wzorzec handlerów
- `src/components/NotificationBell.tsx` — globalny `useEffect` + toast
- `src/context/AuthContext.tsx` — lifecycle hub notifications

---

## 1. `technicalDocumentationHubService.ts`

Plik: `src/services/technicalDocumentationHubService.ts`

Wzoruj się na `chatHubService.ts`:
- Singleton class + `export const technicalDocumentationHubService`
- URL: `${API_BASE_URL}/api/hubs/technical-documentation`
- `accessTokenFactory` z MSAL (`msalInstance`, `silentRequest`)
- `withAutomaticReconnect`
- Metoda `onProcessingCompleted(handler)` → zwraca `unsubscribe`
- SignalR event name: `ProcessingCompleted` (dopasuj do `ITechnicalDocumentationClient`)
- `startConnection()`, `stopConnection()`, `getConnectionState()`

## 2. `useTechnicalDocumentationHub.ts`

Plik: `src/hooks/useTechnicalDocumentationHub.ts`

```typescript
export function useTechnicalDocumentationHub(): void
```

- `useQueryClient()`, `useToastNotification()`
- `useAuth()` — `user?.activeTenantId`
- `useEffect`:
  1. `technicalDocumentationHubService.startConnection()`
  2. Subscribe `onProcessingCompleted`
  3. Handler:
     - Jeśli `event.tenantId !== activeTenantId` → return (opcjonalny filtr)
     - `invalidateQueries` dla `list`, `detail`, `count`
     - `Completed` → `showSuccess('Przetwarzanie zakończone', ...)`
     - `Failed` → `showError('Przetwarzanie nie powiodło się', event.errorMessage ?? ...)`
  4. Cleanup unsubscribe

**Nie** filtruj toastów tylko do strony dokumentacji — decyzja: globalnie.

## 3. Globalny mount

Wybierz jedną opcję (preferowana: nowy lekki bridge):

### Opcja A — `TechnicalDocumentationToastBridge.tsx`
Nowy komponent w `src/components/common/`:
```typescript
export function TechnicalDocumentationToastBridge(): null {
  useTechnicalDocumentationHub();
  return null;
}
```

Dodaj do `App.tsx` obok `ApiErrorToastBridge`:
```tsx
<TechnicalDocumentationToastBridge />
```

### Opcja B — `AuthContext.tsx`
Start hub przy logowaniu (jak `notificationHubService`) + listener w bridge.

Nie duplikuj listenera w stronach listy/szczegółów jeśli bridge jest globalny.

## 4. Lifecycle połączenia

W `AuthContext` (opcjonalnie, wzorzec notifications):
- Start `technicalDocumentationHubService` po `isAuthenticated`
- Stop przy logout

## Weryfikacja
```powershell
npx tsc --noEmit
```

## Następny krok
Komponenty UI w **ui-fix-04** do **ui-fix-07**.
