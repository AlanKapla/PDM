# Notifications — Fix 03: Commands/Queries + Web model — sealed record + required init

## Kontekst
Wszystkie 5 Commands/Queries i `NotificationWeb` muszą być `sealed record` z `public required ... { get; init; }`.

## Zakres

### Commands/Queries (`02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/Notifications/`)

#### 1. `GetAllNotifications/GetAllNotificationsQuery.cs`
```csharp
public sealed record GetAllNotificationsQuery : IRequestQuery<IEnumerable<NotificationWeb>>
{
    public int Take { get; init; } = 50;
    public int Skip { get; init; } = 0;
}
```
(Take/Skip są opcjonalne z domyślnymi — bez `required`.)

#### 2. `GetUnreadNotifications/GetUnreadNotificationsQuery.cs`
Analogicznie do `GetAllNotificationsQuery` — sealed, init, defaults.

#### 3. `GetUnreadCounter/GetUnreadCounterQuery.cs`
```csharp
public sealed record GetUnreadCounterQuery : IRequestQuery<int>;
```

#### 4. `MarkNotificationAsRead/MarkNotificationAsReadCommand.cs`
```csharp
public sealed record MarkNotificationAsReadCommand : IRequestCommand<Unit>
{
    public required Guid NotificationId { get; init; }
}
```

#### 5. `MarkAllNotificationsAsRead/MarkAllNotificationsAsReadCommand.cs`
```csharp
public sealed record MarkAllNotificationsAsReadCommand : IRequestCommand<int>;
```

### Web model

#### 6. `Business/Interfaces/WebModels/Notifications/NotificationWeb.cs`
Zamień positional record na sealed record z explicit properties:

```csharp
public sealed record NotificationWeb
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required Guid TenantId { get; init; }
    public required string TenantName { get; init; }
    public Guid? ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required bool IsRead { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}
```

**Zachowaj dokładnie te same pola, typy i nullowalność co obecne** (zweryfikuj przez `read_file` przed edycją). Jeśli nazwa/typ/nullable jest inny — dopasuj się do aktualnego kontraktu (nie zmieniaj API).

Zmień `Metadata` z `Dictionary<string, object?>?` na `IReadOnlyDictionary<string, object?>?`.

### Kontroler

#### 7. `WebApi/Controllers/NotificationController.cs`
- Zaktualizuj wszystkie miejsca tworzenia komend/query z positional na object initializer:

```csharp
MarkNotificationAsReadCommand command = new MarkNotificationAsReadCommand
{
    NotificationId = notificationId
};
```

- Zamień `var` w akcjach na typy konkretne (`IEnumerable<NotificationWeb> result`, `int counter`, `int updated` itp.) — albo zwracaj bezpośrednio `Ok(await Send(...))`.

### Handlery — minimalny update sygnatur

Po zmianie modeli handlery mogą wymagać aktualizacji konstrukcji `NotificationWeb` (positional → object initializer). Zaktualizuj **tylko miejsca które się złamały** — pełny refaktor handlerów (sealed, var, mapper, ExecuteUpdateAsync) przyjdzie w fix-04.

## Kryteria akceptacji
- 5/5 Commands i Queries: `sealed record` z `{ get; init; }`.
- `NotificationWeb`: `sealed record` z `required { get; init; }` i `IReadOnlyDictionary` dla Metadata.
- Kontroler nie używa `var`, używa object initializerów.
- Build: 0 błędów.
- Brak zmian w API publicznym (te same pola, te same typy nullowalności).

## Raport końcowy
- Status build.
- Lista zmodyfikowanych plików.
- Potwierdzenie że żadne pole web modelu nie zostało dodane/usunięte/przemianowane.
