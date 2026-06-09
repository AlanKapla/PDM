# Notifications — Fix 04: Handlery — sealed, mapper, ExecuteUpdateAsync, sprzątanie

## Kontekst
Po fix-01..03 walidatory i Commands/Queries/WebModel są zgodne ze wzorcem. Pozostaje refaktor handlerów: `sealed`, brak `var`, wydzielenie mappera, optymalizacja bulk-update, sprzątanie usingów, komentarzy i logów.

## Zakres

Wszystkie pliki w `02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/Notifications/`:
- `GetAllNotifications/GetAllNotificationsQueryHandler.cs`
- `GetUnreadNotifications/GetUnreadNotificationsQueryHandler.cs`
- `GetUnreadCounter/GetUnreadCounterQueryHandler.cs`
- `MarkNotificationAsRead/MarkNotificationAsReadCommandHandler.cs`
- `MarkAllNotificationsAsRead/MarkAllNotificationsAsReadCommandHandler.cs`

Plus nowy plik:
- `CQRS/Notifications/NotificationWebMapper.cs`

## Zmiany

### 1. Wydziel `NotificationWebMapper`

Utwórz plik `CQRS/Notifications/NotificationWebMapper.cs`:

```csharp
namespace CQRS.Notifications;

internal static class NotificationWebMapper
{
    public static NotificationWeb ToWeb(Notification notification)
    {
        return new NotificationWeb
        {
            // skopiuj 1:1 mapping z obecnego GetAllNotificationsQueryHandler
        };
    }

    public static string MapType(/* parametr istniejący w obecnych handlerach */) { ... }

    public static IReadOnlyDictionary<string, object?>? DeserializeMetadata(string? json) { ... }
}
```

Zachowaj **dokładnie tę samą logikę** co w `GetAllNotificationsQueryHandler` i `GetUnreadNotificationsQueryHandler` (przeczytaj oba przed edycją — muszą się zgadzać; jeśli się różnią, użyj wersji z `GetAllNotificationsQueryHandler` jako kanonicznej i zaznacz różnice w raporcie).

### 2. Zrefaktoruj 5 handlerów

Każdy handler:
- Dodaj `sealed`.
- Usuń wszystkie nadmiarowe usingi (`Entities.Models.Chats/Costs/Files/Projects/Roles/Tenants/Users/WorkSchedules`).
- Usuń `var` — explicit types.
- Przekazuj `cancellationToken` do wszystkich operacji repo (`GetFirstBySearch`, `GetBySearch`, `Update`, `UpdateRange`, `SaveChangesAsync`, `CountAsync`, `ExecuteUpdateAsync`).
- Komentarze po polsku → angielski lub usuń.
- `notification != null` → `notification is not null`; `== null` → `is null`.
- Usuń emoji z logów.

Specyficzne zmiany:

#### `GetAllNotificationsQueryHandler` + `GetUnreadNotificationsQueryHandler`
- Zastąp inline'owe mapowanie wywołaniem `NotificationWebMapper.ToWeb(n)`.
- Usuń lokalne metody `MapType`, `DeserializeMetadata` z obu handlerów.
- Usuń emoji z logów (📥 ✅ itp.).

#### `MarkAllNotificationsAsReadCommandHandler` (W3 + W5)
- Zamień load + foreach + UpdateRange na `ExecuteUpdateAsync`:

```csharp
int updated = await notificationRepository.ExecuteUpdateAsync(
    n => n.UserId == currentUser.Id && !n.IsRead,
    s => s.SetProperty(n => n.IsRead, true)
          .SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow), // jeśli takie pole istnieje
    cancellationToken);

return updated;
```

Sprawdź podpis `ExecuteUpdateAsync` w `IRepository<T>` (`#codebase` → `IRepository.cs` / `Repository.cs`) — zaadaptuj nazwy/sygnaturę. Jeśli interfejs nie udostępnia `ExecuteUpdateAsync` z `SetProperty`, sprawdź jak inne handlery w projekcie robią bulk-update (np. domena `Files`/`WorkSchedules`) i zastosuj ten sam wzorzec. Jeśli rzeczywiście brak tej metody w repo — zostaw load+foreach, ale w raporcie wyraźnie zaznacz blocker.

Sprawdź czy encja `Notification` ma pole typu `ReadAt` / `MarkedReadAt` — jeśli nie, ustaw tylko `IsRead`. Nie wymyślaj nowych pól.

#### `MarkNotificationAsReadCommandHandler`
- Zachowaj logikę: `GetFirstBySearch` po `Id == NotificationId && UserId == currentUser.Id`, jeśli `is null` → `NotFoundApiException`.
- Idempotencja: jeśli już `IsRead == true` — zwróć `Unit.Value` bez zapisu.
- Dodaj `sealed`, usuń `var`/usingi, przekaż `cancellationToken`.

### 3. Usingi nadmiarowe — sprzątanie globalne

We wszystkich 5 handlerach zostaw tylko faktycznie używane namespace'y (typowo: `MediatR`, `Microsoft.Extensions.Logging`, `Repositories`, `Entities.Models.Notifications`, `Business.Interfaces.WebModels.Notifications`, `Business.Interfaces.Exceptions` jeśli rzucane, `Business.Interfaces.Model` dla `ICurrentUser`, czasem `Entities.Models.Tenants`/`Projects` jeśli używane przez Include).

## Kryteria akceptacji
- 5/5 handlerów `sealed`.
- 0 wystąpień `var` w 5 handlerach.
- 0 zduplikowanych metod `MapType`/`DeserializeMetadata` (są tylko w `NotificationWebMapper`).
- `MarkAllNotificationsAsRead` używa `ExecuteUpdateAsync` (lub jeśli niedostępne — wyraźnie odnotowane w raporcie).
- Brak emoji w logach.
- Komentarze po angielsku.
- `cancellationToken` przekazywany wszędzie.
- Build: 0 błędów.
- API publiczne niezmienione (te same kody HTTP, te same shape'y odpowiedzi).

## Raport końcowy
- Status build.
- Lista zmodyfikowanych/utworzonych plików.
- Czy `ExecuteUpdateAsync` udało się zastosować (tak/nie + powód).
- Lista usingów które finalnie pozostały w handlerach (powinno być ~5-7 per plik).
