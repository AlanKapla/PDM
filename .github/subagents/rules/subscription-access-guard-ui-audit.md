# Audyt UI — Feature: subscription-access-guard

## BLOK 1 — Stan obecny UI

| Komponent/Strona | Lokalizacja | Opis | Powiązane z feature |
|-----------------|------------|------|---------------------|
| `axiosClient.ts` | `src/api/axiosClient.ts` | Axios instance z interceptorami request (token) i response (obsługa 401 + retry). Brak obsługi 402. | TAK — brakuje interceptora 402 |
| `CollaboratingTenants.tsx` | `src/pages/CollaboratingTenants.tsx` | Lista tenantów użytkownika. RadioGroup + Radio, handleTenantChange z changeActiveTenant. Brak badge subscription, Radio disabled tylko gdy `changingTenant`. | TAK — główny widok do modyfikacji |
| `TenantAccessGuard.tsx` | `src/components/TenantAccessGuard.tsx` | Auto-select pierwszego aktywnego tenanta. Sprawdza `user.activeTenantId`, potem listę tenantów i zaproszeń. Nie sprawdza statusu subskrypcji. | TAK — brakuje blokady wejścia |
| `TenantSubscriptionPage.tsx` | `src/pages/TenantSubscriptionPage.tsx` | Strona zarządzania subskrypcją — podgląd planu, historia płatności, przycisk "Opłać". Dostępna pod `/tenants/:tenantId/subscription`. | TAK — target redirectu dla admina |
| `ManagedTenants.tsx` | `src/pages/ManagedTenants.tsx` | Lista tenantów zarządzanych przez admina (`/tenants/managed`). Nie ma parametru `:tenantId` w ścieżce. | POŚREDNIO — używany przez feature ale nie modyfikowany |
| `useSubscriptionStatus` | `src/hooks/queries/useTenantSubscription.ts` | Istniejący hook — `useQuery<SubscriptionStatusInfo>` dla `/tenants/:id/subscription/status`. | TAK — można użyć do odczytu statusu |
| `SubscriptionStatus` enum | `src/types/subscription.ts` | Enum z wartościami: Active=0, Trialing=1, PastDue=2, Canceled=3, GracePeriod=4. | TAK — gotowy do użycia |
| `handleApiError` | `src/utils/handleApiError.ts` | Parsuje AxiosError → `{ title, description }`. Obsługuje `data.error` (ApiExceptionReason string) + fallback na HTTP status. Brak wpisu dla 402. | TAK — wymaga rozszerzenia |
| `errorMessages.ts` | `src/utils/errorMessages.ts` | `apiExceptionReasonMessages` — mapa ApiExceptionReason → PL string. `httpStatusMessages` — mapa kodów HTTP. `successMessages` — predefiniowane klucze sukcesu. | TAK — wymaga rozszerzenia |
| `useTenantPermissions` | `src/hooks/useTenantPermissions.ts` | Hook sprawdzający uprawnienia aktywnego tenanta przez `user.activeTenantPermissions`. | TAK — do sprawdzenia czy user jest adminem |
| `isTenantAdminRole` | `src/constants/roleCodes.ts` | `roleCode === "TENANT.ADMIN"` | TAK — do sprawdzenia roli per tenant w liście |

---

## BLOK 2 — Luki i braki w UI

| Brak / Luka | Typ | Priorytet | Opis |
|-------------|-----|----------|------|
| Obsługa HTTP 402 w interceptorze | interceptor (axiosClient.ts) | KRYTYCZNY | `axiosClient.ts` obsługuje tylko 401. Brak gałęzi dla 402 — nie ma ani redirectu ani toastu |
| Pole `subscriptionStatus` w `UserTenant` | typ TypeScript | KRYTYCZNY | `UserTenant` (`src/types/auth.types.ts`) nie zawiera statusu subskrypcji. Bez tego `CollaboratingTenants` nie może renderować badge'a bez dodatkowego fetch |
| Pole `isSubscriptionBlocked` w odpowiedzi `changeActiveTenant` | typ TypeScript | KRYTYCZNY | `changeActiveTenant` w `tenantApi.ts` zwraca `AxiosResponse<any>` — brak typowania. Potrzebny interfejs `ActiveTenantResponse` z polem `isSubscriptionBlocked: boolean` |
| Badge "Nieaktywna subskrypcja" w CollaboratingTenants | komponent (modyfikacja) | WYSOKI | Brak wizualnej informacji o statusie subskrypcji na liście tenantów |
| Wyłączenie Radio dla non-adminów | komponent (modyfikacja) | WYSOKI | Przełącznik tenanta powinien być `isDisabled` dla non-admina gdy `subscriptionStatus` jest blokujący |
| Post-switch redirect dla admina | logika (CollaboratingTenants.tsx) | WYSOKI | Po `changeActiveTenant` nie sprawdzamy `isSubscriptionBlocked` — brak redirectu do strony subskrypcji |
| Toast "Subskrypcja wygasła" | toast | WYSOKI | Brak toastu dla zwykłego membera po błędzie 402 |
| Wpis `SubscriptionSuspended` w errorMessages | konfiguracja | ŚREDNI | `apiExceptionReasonMessages` nie ma klucza `SubscriptionSuspended` — fallback pokaże techniczny kod |
| Route `/tenants/managed/:tenantId` | routing | ŚREDNI | Feature spec mówi o redirectzie do `/tenants/managed/{tenantId}` ale taka ścieżka NIE ISTNIEJE. Istnieje `/tenants/:tenantId/subscription` (TenantSubscriptionPage) i `/tenants/managed` (bez parametru). Wymaga decyzji |
| Wpis 402 w `httpStatusMessages` | konfiguracja | NISKI | `httpStatusMessages` nie ma klucza `402` — fallback error handler pokaże "Błąd" |

---

## BLOK 3 — Typy TypeScript

| Typ | Plik | Nowy/Modyfikacja | Opis zmian |
|-----|------|-----------------|------------|
| `UserTenant` | `src/types/auth.types.ts` | **Modyfikacja** | Dodaj pole `subscriptionStatus?: SubscriptionStatus` (nullable — Free plan może nie mieć subskrypcji) |
| `ActiveTenantResponse` | `src/types/auth.types.ts` | **Nowy** | `interface ActiveTenantResponse { activeTenantId: string; isSubscriptionBlocked: boolean }` — odpowiedź z `PUT /tenants/active` |

Przykład:
```typescript
// src/types/auth.types.ts
import type { SubscriptionStatus } from './subscription';

export interface UserTenant {
  id: string;
  name: string;
  createdAt: string;
  isActive: boolean;
  roleCode: string;
  isActiveTenant: boolean;
  subscriptionStatus?: SubscriptionStatus;  // NOWE
}

export interface ActiveTenantResponse {
  activeTenantId: string;
  isSubscriptionBlocked: boolean;
}
```

---

## BLOK 4 — Serwisy API (src/api/)

| Funkcja API | Plik | Nowa/Modyfikacja | Endpoint | Opis |
|-------------|------|-----------------|---------|------|
| `changeActiveTenant` | `src/api/tenantApi.ts` | **Modyfikacja** | `PUT /tenants/active` | Zmień typ zwracany z `Promise<AxiosResponse<any>>` na `Promise<AxiosResponse<ActiveTenantResponse>>` |

Obecny kod:
```typescript
changeActiveTenant: async (tenantId: string) => {
  return axiosClient.put("/tenants/active", { tenantId });
},
```

Po zmianie:
```typescript
import type { ActiveTenantResponse } from "../types/auth.types";

changeActiveTenant: async (tenantId: string): Promise<{ data: ActiveTenantResponse }> => {
  return axiosClient.put<ActiveTenantResponse>("/tenants/active", { tenantId });
},
```

---

## BLOK 5 — Hooki React Query

| Hook | Plik | Nowy/Modyfikacja | Query/Mutation | Opis |
|------|------|-----------------|---------------|------|
| `useSubscriptionStatus` | `src/hooks/queries/useTenantSubscription.ts` | **Bez zmian** | Query — `GET /tenants/:id/subscription/status` | Już istnieje — można użyć per-tenant do sprawdzenia statusu |

**Uwaga**: W CollaboratingTenants lista może zawierać wiele tenantów. Wywołanie `useSubscriptionStatus` per tenant (wiele hooków) jest nieefektywne. Lepiej jeśli `UserTenant.subscriptionStatus` będzie zwracane przez `GET /tenants/my-tenants` (zmiana API).

Alternatywnie — jeśli API nie zwraca statusu w liście — można użyć `useQueries` z TanStack Query:
```typescript
// Nie zalecane jeśli lista tenantów jest długa
const statusQueries = useQueries({
  queries: tenants.map(t => ({
    queryKey: tenantSubscriptionKeys.status(t.id),
    queryFn: () => tenantSubscriptionApi.getSubscriptionStatus(t.id).then(r => r.data),
  }))
});
```

---

## BLOK 6 — Nowe komponenty

| Komponent | Lokalizacja | Opis | Zależy od |
|-----------|------------|------|-----------|
| `SubscriptionStatusBadge` | `src/components/ui/SubscriptionStatusBadge.tsx` | (opcjonalny) Badge wyświetlający status subskrypcji z kolorową etykietą. Wzorzec: istniejący `Badge` z `colorScheme` bazującym na `SubscriptionStatus` | `SubscriptionStatus`, `StatusLabels` z `types/subscription.ts` |

Można też pominąć osobny komponent i dodać badge inline w `CollaboratingTenants.tsx` (prostsze).

---

## BLOK 7 — Modyfikacje istniejących komponentów

| Komponent | Plik | Typ zmiany | Opis |
|-----------|------|-----------|------|
| `axiosClient.ts` | `src/api/axiosClient.ts` | Rozszerzenie response interceptora | Dodaj gałąź `if (error.response?.status === 402)` z logiką: sprawdź rolę usera (`activeTenantPermissions`) → admin: navigate do strony subskrypcji + toast warning; member: toast error |
| `CollaboratingTenants.tsx` | `src/pages/CollaboratingTenants.tsx` | Logika + UI | 1) W `handleTenantChange`: odczytaj `response.data.isSubscriptionBlocked` → redirect. 2) Per tenant: badge "Nieaktywna subskrypcja" gdy status blokujący. 3) `Radio isDisabled` dla non-admina gdy status blokujący |
| `tenantApi.ts` | `src/api/tenantApi.ts` | Dodanie typowania | Zmień return type `changeActiveTenant` na `Promise<{ data: ActiveTenantResponse }>` |
| `tenantService.ts` | `src/services/tenantService.ts` | Zmiana sygnatury | `changeActiveTenant` zwraca `Promise<void>` → zmień na `Promise<ActiveTenantResponse>`, propaguj dane response zamiast je porzucać |
| `errorMessages.ts` | `src/utils/errorMessages.ts` | Rozszerzenie map | Dodaj `SubscriptionSuspended: "Subskrypcja nieaktywna"` do `apiExceptionReasonMessages`, `402: "Subskrypcja nieaktywna"` do `httpStatusMessages` |
| `auth.types.ts` | `src/types/auth.types.ts` | Rozszerzenie interfejsów | Dodaj `subscriptionStatus?: SubscriptionStatus` do `UserTenant`, nowy `ActiveTenantResponse` |

---

## BLOK 8 — Spójność UI

| Wzorzec | Istniejąca implementacja | Czy feature musi się dostosować |
|---------|------------------------|--------------------------------|
| Toast — error | `useToastNotification().showError(title, description)` | TAK — użyj `showError` lub `showWarning` dla 402 |
| Toast — success | `showApiSuccess(key: SuccessMessageKey)` z `errorMessages.ts` | TAK — rozważ `subscriptionBlocked` jako nowy klucz w `successMessages` (jako warning toast, nie success) |
| Badge roli | `<Badge colorScheme={getRoleColor(tenant.roleCode)}>` | TAK — badge statusu subskrypcji powinien być obok badge'a roli (ten sam wzorzec `Badge` z `colorScheme`) |
| Obsługa błędów API | `catch (error) { const { title, description } = handleApiError(error); showError(title, description); }` | TAK — w `handleTenantChange` ten wzorzec już jest używany |
| Routing redirect | `useNavigate(); navigate('/path')` | TAK — użyj `useNavigate` (już importowany w ManagedTenants, nie w CollaboratingTenants — trzeba dodać) |
| Kolory | `appColors` z `theme/tokens/colors.ts` lub Chakra `colorScheme` | TAK — badge "Nieaktywna" powinien używać `colorScheme="orange"` lub `"red"` (spójne z Chakra UI) |

### Schemat kolorów statusów subskrypcji (rekomendacja)
```typescript
function getSubscriptionStatusColor(status: SubscriptionStatus): string {
  switch (status) {
    case SubscriptionStatus.Active:      return "green";
    case SubscriptionStatus.Trialing:    return "blue";
    case SubscriptionStatus.PastDue:     return "orange";
    case SubscriptionStatus.GracePeriod: return "orange";
    case SubscriptionStatus.Canceled:    return "red";
  }
}
```
(Wzorzec: `getStatusColorScheme` jest już zdefiniowany w `TenantSubscriptionPage.tsx` — przenieść do `subscription.ts` lub `roleCodes.ts`)

---

## BLOK 9 — Problemy i ryzyka

| # | Problem | Komponent/Plik | Ryzyko | Rekomendacja |
|---|---------|---------------|--------|-------------|
| 1 | `axiosClient.ts` nie ma dostępu do `useNavigate` ani `useToast` (nie jest hookiem/komponentem) | `src/api/axiosClient.ts` | WYSOKI | Użyj event emitter / custom event / globalny state (np. Zustand) lub przenieś logikę 402 do komponentu wyżej (App.tsx lub layout). Alternatywnie: eksportuj `navigate` przez singleton z `src/lib/navigationService.ts` |
| 2 | `tenantService.ts` zwraca `Promise<void>` dla `changeActiveTenant` | `src/services/tenantService.ts` | WYSOKI | Zmiana sygnatury może wpłynąć na `TenantAccessGuard.tsx` (linia `await changeActiveTenant(activeTenants[0].id)`) — trzeba sprawdzić wszystkie wywołania |
| 3 | Route `/tenants/managed/:tenantId` NIE istnieje | `src/routes/AppRouter.tsx` | WYSOKI | Feature spec mówi o redirectzie do `/tenants/managed/{tenantId}` ale ta ścieżka nie istnieje. Istniejąca strona subskrypcji to `/tenants/:tenantId/subscription`. Wymaga decyzji: (a) użyć istniejącej ścieżki lub (b) dodać nową trasę |
| 4 | `UserTenant` nie zawiera `subscriptionStatus` | `src/types/auth.types.ts` | ŚREDNI | Wymaga zmiany API backendu — endpoint `GET /tenants/my-tenants` musi zwracać status subskrypcji. Do uzgodnienia z backendem |
| 5 | `useSubscriptionStatus` hook wymaga tenantId — nie da się wywołać dla wielu tenantów efektywnie | `src/hooks/queries/useTenantSubscription.ts` | ŚREDNI | Jeśli backend nie doda statusu do listy tenantów, trzeba N+1 requestów. Rozważyć dodanie statusu do odpowiedzi `my-tenants` |
| 6 | Sprawdzenie "czy user jest adminem" w interceptorze 402 | `src/api/axiosClient.ts` | ŚREDNI | `axiosClient` nie ma dostępu do `user.activeTenantPermissions`. Trzeba czytać z globalnego store lub przekazywać przez mechanizm poza hookami |
| 7 | `changeActiveTenant` w `tenantApi.ts` jest `async (tenantId: string) =>` bez explicit return type | `src/api/tenantApi.ts` | NISKI | Zmiana typowania nie łamie runtime, ale wymaga uważnego dostosowania `tenantService.ts` |

---

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Nowe komponenty | 0–1 (opcjonalny `SubscriptionStatusBadge`) |
| Zmodyfikowane komponenty | 5 (`axiosClient.ts`, `CollaboratingTenants.tsx`, `tenantApi.ts`, `tenantService.ts`, `errorMessages.ts`) |
| Nowe hooki | 0 |
| Nowe typy TypeScript | 1 (`ActiveTenantResponse`) + modyfikacja `UserTenant` |
| Nowe wywołania API | 0 (tylko zmiana typowania istniejących) |
| Pytania domenowe | 3 |

---

## Pytania domenowe wymagające decyzji

1. **Ścieżka redirectu dla admina**: Feature spec mówi `/tenants/managed/{tenantId}`, ale taka trasa nie istnieje. Czy redirect powinien iść na:
   - `/tenants/{tenantId}/subscription` (istniejąca `TenantSubscriptionManagePage`) — **rekomendowane**, zero dodatkowego kodu
   - Nową stronę pod `/tenants/managed/{tenantId}` — wymaga nowej trasy i komponentu

2. **Skąd wziąć `subscriptionStatus` w liście CollaboratingTenants**: Czy backend rozszerzy odpowiedź `GET /tenants/my-tenants` o pole `subscriptionStatus`, czy UI ma wykonywać osobne requesty per tenant? Pierwsze podejście jest zdecydowanie lepsze.

3. **Obsługa 402 w interceptorze bez dostępu do hooków**: Interceptor Axios działa poza drzewem React — nie ma dostępu do `useNavigate` ani `useToast`. Preferowany mechanizm:
   - **Event emitter** (`window.dispatchEvent(new CustomEvent('subscription-blocked', { detail: tenantId }))`) + listener w `App.tsx` — prostsze
   - **Globalna funkcja nawigacji** (singleton `navigationService.ts` z `navigate` ref) — bardziej typowe w projektach React

---

## Pliki do modyfikacji — podsumowanie ścieżek

| Plik | Zmiana |
|------|--------|
| `src/api/axiosClient.ts` | Dodaj interceptor response dla 402 |
| `src/api/tenantApi.ts` | Zmień typ `changeActiveTenant` → `ActiveTenantResponse` |
| `src/services/tenantService.ts` | Zmień `changeActiveTenant` z `Promise<void>` na `Promise<ActiveTenantResponse>` |
| `src/types/auth.types.ts` | Dodaj `subscriptionStatus?` do `UserTenant`, dodaj `ActiveTenantResponse` |
| `src/utils/errorMessages.ts` | Dodaj `SubscriptionSuspended`, `402` do map |
| `src/pages/CollaboratingTenants.tsx` | Badge per tenant, disable Radio, post-switch redirect, import `useNavigate` |
| `src/routes/AppRouter.tsx` | Ewentualnie nowa trasa `/tenants/managed/:tenantId` (zależnie od decyzji domenowej #1) |
