# UI Fix 01 — Event emitter + Axios interceptor 402 + errorMessages

## Feature
subscription-access-guard

## Cel
Obsługa HTTP 402 w warstwie API:
- Event emitter singleton do komunikacji poza drzewem React
- Interceptor Axios emitujący event przy 402
- Rozszerzenie `errorMessages.ts`

---

## Krok 1 — Przeczytaj kontekst

Przed implementacją przeczytaj:
- `src/api/axiosClient.ts` — pełna zawartość
- `src/utils/errorMessages.ts` — pełna zawartość (struktura `apiExceptionReasonMessages`, `httpStatusMessages`)
- `src/utils/handleApiError.ts` — pełna zawartość
- `src/types/subscription.ts` — enum `SubscriptionStatus`

---

## Krok 2 — Utwórz `subscriptionEventEmitter.ts`

**Plik:** `src/services/subscriptionEventEmitter.ts`

Singleton event emitter oparty na natywnym `EventTarget`.
Emituje event gdy backend zwraca 402, zawiera `tenantId` i `isAdmin`.

```typescript
type SubscriptionBlockedPayload = {
  tenantId: string;
  isAdmin: boolean;
};

const SUBSCRIPTION_BLOCKED_EVENT = 'subscription:blocked';

class SubscriptionEventEmitter extends EventTarget {
  emitBlocked(payload: SubscriptionBlockedPayload): void {
    this.dispatchEvent(
      new CustomEvent(SUBSCRIPTION_BLOCKED_EVENT, { detail: payload })
    );
  }

  onBlocked(handler: (payload: SubscriptionBlockedPayload) => void): () => void {
    const listener = (e: Event) => {
      handler((e as CustomEvent<SubscriptionBlockedPayload>).detail);
    };
    this.addEventListener(SUBSCRIPTION_BLOCKED_EVENT, listener);
    return () => this.removeEventListener(SUBSCRIPTION_BLOCKED_EVENT, listener);
  }
}

export const subscriptionEventEmitter = new SubscriptionEventEmitter();
```

---

## Krok 3 — Rozszerz interceptor w `axiosClient.ts`

**Plik:** `src/api/axiosClient.ts`

W response interceptorze (gałąź `error`) dodaj obsługę 402 **przed** obsługą 401.

Potrzebne:
- Import `subscriptionEventEmitter` z `../services/subscriptionEventEmitter`
- Dostęp do `tenantId` z odpowiedzi błędu: `error.response?.data?.objectId` (backend ustawia `objectId = tenantId.ToString()` w `SubscriptionSuspendedException`)

Logika:
```typescript
if (error.response?.status === 402) {
  const tenantId: string = error.response.data?.objectId ?? '';
  // Nie mamy tu dostępu do store Reacta — emituj event
  // Komponent nadrzędny nasłucha i zdecyduje o redirectcie
  subscriptionEventEmitter.emitBlocked({ tenantId, isAdmin: false });
  // isAdmin zostanie sprawdzone przez nasłuchujący komponent
  return Promise.reject(error);
}
```

**Uwaga:** `isAdmin` w evencie ustawiamy na `false` jako default — komponent nasłuchujący
sam sprawdzi uprawnienia przez `useTenantPermissions` lub dane z profilu.

---

## Krok 4 — Rozszerz `errorMessages.ts`

**Plik:** `src/utils/errorMessages.ts`

Dodaj wpisy:
- W `apiExceptionReasonMessages`: `SubscriptionSuspended: 'Subskrypcja nieaktywna'`
- W `httpStatusMessages`: `402: 'Subskrypcja nieaktywna — opłać abonament aby kontynuować'`

---

## Weryfikacja
- `npx tsc --noEmit` w katalogu `01-Applications/ProjectDataManagementUI` — musi przejść bez błędów TypeScript
