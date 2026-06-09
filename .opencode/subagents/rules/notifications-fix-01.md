# Notifications — Fix 01: Krytyczne walidatory + porządek w validatorach

## Kontekst
Decyzja domenowa: notyfikacje są **user-wide** — bez `TenantId` w predykatach, routing pozostaje `/api/notification`.

## Zakres
Pliki w `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/Notifications/`:

- `MarkNotificationAsRead/MarkNotificationAsReadCommandValidator.cs`
- `GetUnreadNotifications/GetUnreadNotificationsQueryValidator.cs`
- `GetAllNotifications/GetAllNotificationsQueryValidator.cs`

## Zmiany

### 1. `MarkNotificationAsReadCommandValidator.cs` (K1)
- **Usuń** regułę `RuleFor(x => x.NotificationId).MustAsync(NotificationMustExistAndBelongToUser).WithMessage(...)` oraz całą metodę `NotificationMustExistAndBelongToUser`.
- **Usuń** wstrzykiwanie `IRepository<Notification>` i `ICurrentUser` z konstruktora — validator ma być bezstanowy.
- Pozostaw jedynie `RuleFor(x => x.NotificationId).NotEmpty().WithMessage("NotificationId is required");` (na razie bez `RequiredId()` — to dojdzie w fix-02).
- Dodaj `sealed`.
- Usuń wszystkie nadmiarowe usingi (`Entities.Models.Chats/Costs/Files/Projects/Roles/Tenants/Users/WorkSchedules`, `Repositories`, `Business.Interfaces.Model`, `Business.Interfaces.Exceptions` itp.) — zostaw tylko `FluentValidation` i namespace komendy.
- Usuń komentarze po polsku.

Sprawdzanie istnienia + ownership pozostaje WYŁĄCZNIE w handlerze (`MarkNotificationAsReadCommandHandler`) — jest tam już poprawna logika rzucająca `NotFoundApiException` (HTTP 404).

### 2. `GetUnreadNotificationsQueryValidator.cs` (K2)
- **Usuń** całą regułę autentykacji: `RuleFor(x => x).Must(_ => currentUser.IsAuthenticated && currentUser.Id != Guid.Empty).WithErrorCode("401")...`
- **Usuń** wstrzykiwanie `ICurrentUser` z konstruktora.
- Pozostaw walidacje `Take` i `Skip` w obecnej formie (tu też tylko sprzątanie składni; ekstensje przyjdą w fix-02).
- Dodaj `sealed`.
- Usuń nadmiarowe usingi (`Business.Interfaces.Exceptions`, modele encji itp.).

Autentykację załatwia `[Authorize]` na kontrolerze (HTTP 401) — to nie jest zadanie FluentValidation.

### 3. `GetAllNotificationsQueryValidator.cs`
- Usuń ewentualne nadmiarowe usingi.
- `sealed` już jest — zostaw.
- Bez zmian funkcjonalnych.

## Kryteria akceptacji
- 0 zapytań do DB w jakimkolwiek validatorze domeny Notifications.
- Wszystkie 3 validatory są `sealed`.
- Konstruktory validatorów są bezparametrowe.
- Build solution `ProductDataManagementWebAPI.sln`: 0 błędów.

## Raport końcowy
- Status build (errors/warnings).
- Lista zmodyfikowanych plików.
- Potwierdzenie usunięcia obu antipatternów (DB w validatorze + auth w validatorze).
