# Feature: Blokada dostępu przy nieopłaconej subskrypcji

## Cel
Ograniczenie dostępu do zasobów tenanta gdy subskrypcja nie jest aktywna.
Jedynym wyjątkiem jest TenantAdmin, który może wejść do tenanta wyłącznie w celu opłacenia subskrypcji.

## Reguły dostępu

### Statusy blokujące (SubscriptionStatus)
- `PastDue` → zablokowany
- `Canceled` → zablokowany
- `GracePeriod` → zablokowany

### Statusy zezwalające
- `Active` → pełny dostęp
- `Trialing` → pełny dostęp

### Wyjątek: TenantAdmin
- TenantAdmin MOŻE przełączyć się na zablokowany tenant
- Po przełączeniu jest od razu przekierowany na stronę zarządzania tenanta (`/tenants/managed/{tenantId}`)
- Dostęp do zasobów tenanta jest zablokowany nawet dla admina (projekty, członkowie, zaproszenia, kosztorysy, etc.)
- Admin ma dostęp TYLKO do endpointów subskrypcji (opłacenie, status)

### Zwykły member (nie-admin)
- NIE MOŻE przełączyć się na tenant z nieaktywną subskrypcją
- W liście tenantów widzi badge "Nieaktywna subskrypcja"
- Próba przełączenia → błąd 402

## Zmiany API

### 1. Nowy wyjątek HTTP 402
- `SubscriptionSuspendedException : ApiException` z HTTP 402
- Nowy `ApiExceptionReason.SubscriptionSuspended`

### 2. Nowy MediatR Behavior: `SubscriptionEnforcementBehavior`
- Wykonywany PO `AuthorizationBehavior`
- Sprawdza `TenantSubscription.Status` dla aktywnego tenanta z requestu
- Jeśli status blokujący:
  - Dla TenantAdmin: przepuszcza TYLKO requesty oznaczone `IBypassSubscriptionCheck` (np. płatność)
  - Dla zwykłego membera: zawsze blokuje → `SubscriptionSuspendedException`
- Marker interface `IBypassSubscriptionCheck` na komendach/zapytaniach które mają być przepuszczane

### 3. Komendy z `IBypassSubscriptionCheck` (zawsze przepuszczane)
- `ProcessMockPaymentCommand`
- `GetSubscriptionStatusQuery`
- `GetTenantSubscriptionQuery`
- `ChangeActiveTenantCommand` (osobna logika — patrz pkt 4)

### 4. Modyfikacja `ChangeActiveTenantCommandHandler`
- Przed zmianą aktywnego tenanta: sprawdź `TenantSubscription.Status`
- Jeśli status blokujący I user NIE jest TenantAdmin → `SubscriptionSuspendedException`
- Jeśli status blokujący I user JEST TenantAdmin → pozwól na zmianę, zwróć dodatkowe pole `IsSubscriptionBlocked: true`

### 5. `ActiveTenantWeb` response model
- Dodaj pole `IsSubscriptionBlocked: bool`

## Zmiany UI

### 1. Axios interceptor — obsługa HTTP 402
- Przechwytuj status 402
- Jeśli zalogowany user jest adminem tenanta → redirect do `/tenants/managed/{tenantId}` + toast
- Jeśli zwykły member → toast "Subskrypcja wygasła. Skontaktuj się z administratorem"

### 2. `CollaboratingTenants.tsx` (lista tenantów)
- Badge "Nieaktywna subskrypcja" dla tenantów z zablokowanym statusem
- Przycisk "Przełącz" nieaktywny dla zwykłych memberów gdy status blokujący

### 3. Po przełączeniu na zablokowany tenant (admin)
- Sprawdź `isSubscriptionBlocked` w odpowiedzi `changeActiveTenant`
- Jeśli true → redirect do `/tenants/managed/{tenantId}`

## Zależności
- `TenantSubscription` musi być dostępna w `TenantCtxSnapshot` lub pobierana osobno
- Behavior musi działać dla wszystkich `IAuthorizableRequest` z TenantId
