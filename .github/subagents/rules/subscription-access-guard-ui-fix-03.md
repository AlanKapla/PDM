# UI Fix 03 — CollaboratingTenants.tsx + SubscriptionBlockedHandler

## Feature
subscription-access-guard

## Cel
- Badge "Nieaktywna subskrypcja" na liście tenantów
- Blokada przełączania dla non-adminów gdy subskrypcja zablokowana
- Redirect do `/tenants/:tenantId/subscription` po przełączeniu na zablokowany tenant (dla admina)
- Globalny handler eventu 402 z `subscriptionEventEmitter`

## Wymagania wstępne
Musi być wykonany po `ui-fix-01` (subscriptionEventEmitter) i `ui-fix-02` (typy, tenantService).

---

## Krok 1 — Przeczytaj kontekst

Przed implementacją przeczytaj:
- `src/pages/CollaboratingTenants.tsx` — pełna zawartość
- `src/constants/roleCodes.ts` — jak sprawdzić rolę admina (`isTenantAdminRole` lub stała `TENANT_ADMIN`)
- `src/types/subscription.ts` — enum `SubscriptionStatus`
- `src/services/subscriptionEventEmitter.ts` (ui-fix-01)
- Przykład użycia toastu w projekcie — sprawdź w `src/hooks/useToastNotification.ts`

---

## Krok 2 — Pomocnicza funkcja `isSubscriptionBlocked`

W pliku `CollaboratingTenants.tsx` (lub wyeksportowana z utils) dodaj:
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

---

## Krok 3 — Badge statusu subskrypcji

W liście tenantów (`CollaboratingTenants.tsx`), przy każdym tenancie, jeśli `isSubscriptionBlocked(tenant.subscriptionStatus)`:
- Wyświetl `<Badge colorScheme="red">Nieaktywna subskrypcja</Badge>` obok nazwy tenanta
- Wzoruj się na istniejącym badge roli w tym komponencie (ten sam styl `Badge` z Chakra UI)

---

## Krok 4 — Blokada Radio dla non-adminów

Dla każdego `Radio` (lub przycisku przełączenia) odpowiadającego tenantowi:
- Dodaj `isDisabled` gdy:
  `isSubscriptionBlocked(tenant.subscriptionStatus) && tenant.roleCode !== TENANT_ADMIN_ROLE_CODE`
- Sprawdź dokładną nazwę stałej w `src/constants/roleCodes.ts`

---

## Krok 5 — Redirect po przełączeniu na zablokowany tenant

W funkcji `handleTenantChange` (lub odpowiedniej):
1. Odczytaj zwracaną wartość z `changeActiveTenant`:
   ```typescript
   const result = await changeActiveTenant(tenantId);
   ```
2. Jeśli `result.isSubscriptionBlocked`:
   ```typescript
   navigate(`/tenants/${tenantId}/subscription`);
   return;
   ```
3. Standardowa ścieżka (brak blokady) pozostaje bez zmian

Użyj `useNavigate` z `react-router-dom` — dodaj import jeśli go nie ma.

---

## Krok 6 — Handler globalnego eventu 402

W `CollaboratingTenants.tsx` (lub w komponencie layoutu — sprawdź gdzie najbardziej pasuje):

Dodaj `useEffect` nasłuchujący na `subscriptionEventEmitter.onBlocked`:
```typescript
useEffect(() => {
  const unsubscribe = subscriptionEventEmitter.onBlocked(({ tenantId }) => {
    // Sprawdź czy aktualny user jest adminem dla tego tenanta
    // Przez dane z listy tenantów lub przez `user.activeTenantPermissions`
    const tenant = tenants?.find(t => t.id === tenantId);
    const userIsAdmin = tenant?.roleCode === TENANT_ADMIN_ROLE_CODE;

    if (userIsAdmin) {
      navigate(`/tenants/${tenantId}/subscription`);
      showWarning('Subskrypcja nieaktywna', 'Opłać subskrypcję aby uzyskać pełny dostęp do tenanta.');
    } else {
      showError('Brak dostępu', 'Subskrypcja tenanta jest nieaktywna. Skontaktuj się z administratorem.');
    }
  });
  return unsubscribe;
}, [tenants, navigate]);
```

Użyj `showWarning` / `showError` z `useToastNotification()`.

---

## Weryfikacja
- `npx tsc --noEmit` — musi przejść bez błędów TypeScript
- Sprawdź że badge pojawia się tylko dla statusów blokujących
- Sprawdź że Radio jest zablokowane dla non-adminów przy blokującej subskrypcji
