# UI Fix 02 — Typy TypeScript + tenantApi + tenantService

## Feature
subscription-access-guard

## Cel
Aktualizacja typów TypeScript i klientów API:
- `UserTenant` ← dodanie `subscriptionStatus`
- `ActiveTenantResponse` ← nowy typ
- `tenantApi.changeActiveTenant` ← zwraca `ActiveTenantResponse`
- `tenantService.changeActiveTenant` ← propaguje dane zamiast je porzucać

---

## Krok 1 — Przeczytaj kontekst

Przed implementacją przeczytaj:
- `src/types/auth.types.ts` — pełna zawartość (UserTenant i inne typy)
- `src/types/subscription.ts` — enum `SubscriptionStatus`
- `src/api/tenantApi.ts` — pełna zawartość
- `src/services/tenantService.ts` — pełna zawartość

---

## Krok 2 — Rozszerz typy w `auth.types.ts`

**Plik:** `src/types/auth.types.ts`

### Modyfikacja `UserTenant`
Dodaj pole:
```typescript
subscriptionStatus?: SubscriptionStatus;
```
Zaimportuj `SubscriptionStatus` z `./subscription`.

### Nowy typ `ActiveTenantResponse`
Dodaj nowy interfejs (lub type):
```typescript
export interface ActiveTenantResponse {
  activeTenantId: string;
  isSubscriptionBlocked: boolean;
}
```

---

## Krok 3 — Zaktualizuj `tenantApi.ts`

**Plik:** `src/api/tenantApi.ts`

Znajdź funkcję `changeActiveTenant` i zmień jej sygnaturę zwracanego typu.
Zaimportuj `ActiveTenantResponse` z `../types/auth.types`.

Zmień tak, żeby `axiosClient.put<ActiveTenantResponse>(...)` był typowany.

---

## Krok 4 — Zaktualizuj `tenantService.ts`

**Plik:** `src/services/tenantService.ts`

Znajdź funkcję `changeActiveTenant`.
Zmień sygnaturę z `Promise<void>` na `Promise<ActiveTenantResponse>`.
Zaimportuj `ActiveTenantResponse` z odpowiedniego pliku typów.

Propaguj dane odpowiedzi:
```typescript
export const changeActiveTenant = async (tenantId: string): Promise<ActiveTenantResponse> => {
  const response = await tenantApi.changeActiveTenant(tenantId);
  return response.data;
};
```

---

## Weryfikacja
- `npx tsc --noEmit` — musi przejść bez błędów TypeScript
- Sprawdź że żadne inne miejsce używające `changeActiveTenant` nie jest zepsute
  (szukaj usages w `CollaboratingTenants.tsx`, `TenantAccessGuard.tsx`)
