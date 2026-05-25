# UI Fix 04 — TenantAccessGuard — pomijanie zablokowanych tenantów

## Feature
subscription-access-guard

## Cel
`TenantAccessGuard` przy auto-selekcji pierwszego dostępnego tenanta powinien
pomijać tenantów z zablokowaną subskrypcją (chyba że user jest adminem tego tenanta).

## Wymagania wstępne
Musi być wykonany po `ui-fix-02` (typy `UserTenant.subscriptionStatus`).

---

## Krok 1 — Przeczytaj kontekst

Przed implementacją przeczytaj:
- `src/components/TenantAccessGuard.tsx` — pełna zawartość
- `src/constants/roleCodes.ts` — stała dla roli TenantAdmin
- `src/types/subscription.ts` — enum `SubscriptionStatus`
- `src/types/auth.types.ts` — interfejs `UserTenant`

---

## Krok 2 — Zaktualizuj logikę auto-selekcji tenanta

W `TenantAccessGuard.tsx` znajdź miejsce gdzie wybierany jest pierwszy dostępny tenant
(zazwyczaj `tenants[0]` lub `tenants.find(...)`).

Zmodyfikuj selekcję tak, żeby:
1. **Najpierw** szukać tenanta z aktywną subskrypcją (`!isSubscriptionBlocked(t.subscriptionStatus)`)
2. **Jeśli nie ma** takiego tenanta → wybierz tenant gdzie `t.roleCode === TENANT_ADMIN_ROLE_CODE`
   (admin może wejść nawet do zablokowanego tenanta)
3. **Jeśli nadal** nie ma → zachowanie bez zmian (użytkownik nie ma dostępnych tenantów)

Pomocnicza funkcja (skopiuj z CollaboratingTenants lub zaimportuj jeśli zostanie wyeksportowana):
```typescript
function isSubscriptionBlocked(status: SubscriptionStatus | undefined | null): boolean {
  if (status == null) return false;
  return (
    status === SubscriptionStatus.PastDue ||
    status === SubscriptionStatus.Canceled ||
    status === SubscriptionStatus.GracePeriod
  );
}
```

### Przykładowa logika selekcji:
```typescript
const activeTenant =
  tenants.find(t => !isSubscriptionBlocked(t.subscriptionStatus)) ??
  tenants.find(t => t.roleCode === TENANT_ADMIN_ROLE_CODE) ??
  tenants[0];
```

---

## Krok 3 — Obsługa po auto-selekcji zablokowanego tenanta

Jeśli wybrany tenant ma `isSubscriptionBlocked(tenant.subscriptionStatus)` i user jest adminem:
- Po wywołaniu `changeActiveTenant` sprawdź `result.isSubscriptionBlocked`
- Jeśli `true` → `navigate('/tenants/${tenantId}/subscription')`

Jeśli wybrany tenant jest zablokowany i user NIE jest adminem — nie zmieniaj tenanta
(powinno to być pokryte przez selekcję z kroku 2, ale warto mieć defensywny check).

---

## Weryfikacja
- `npx tsc --noEmit` — musi przejść bez błędów TypeScript
