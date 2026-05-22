# Chat — Fix 03: Post-commit dispatcher dla SignalR (K2-K5)

Cel: wyeliminować race-conditions, w których eventy SignalR są wysyłane przed
commitem transakcji `TransactionBehavior` lub przed `ExecuteDeleteAsync`.

Kontekst: audyt `.github/subagents/rules/chat-audit.md`, problemy K2, K3, K4, K5.
Decyzja domenowa: wprowadzić **outbox / post-commit dispatcher**.

## Architektura

### 1. Nowy interfejs — `IPostCommitDispatcher`

Lokalizacja: `src/CQRS/PostCommit/IPostCommitDispatcher.cs`

```csharp
namespace CQRS.PostCommit;

/// <summary>
/// Kolejkuje akcje (np. broadcast SignalR) wykonywane DOPIERO po
/// pomyślnym commicie transakcji bieżącego requestu MediatR.
/// </summary>
public interface IPostCommitDispatcher
{
    void Enqueue(Func<CancellationToken, Task> action);
    Task DispatchAsync(CancellationToken cancellationToken);
}
```

### 2. Implementacja — scoped

Lokalizacja: `src/CQRS/PostCommit/PostCommitDispatcher.cs`

- Scoped lifetime (per request).
- Wewnętrzna lista `Func<CT, Task>`.
- `DispatchAsync` wykonuje kolejno; błędy z pojedynczych akcji są łapane,
  logowane przez `ILogger<PostCommitDispatcher>` jako `LogError` i NIE
  przerywają pozostałych broadcastów (commit już się udał, klient i tak
  zauważy następnym razem).
- Po `DispatchAsync` lista jest czyszczona (idempotencja).

### 3. Modyfikacja `TransactionBehavior`

Plik: `src/CQRS/Behaviours/TransactionBehavior.cs`.

- Wstrzyknij `IPostCommitDispatcher` (przez ctor).
- Po pomyślnym `transaction.CommitAsync(...)` wywołaj
  `await dispatcher.DispatchAsync(cancellationToken);`.
- Dispatch poza blokiem `try/catch` transakcji — ale w try/catch swoim,
  żeby nie wpłynął na response.
- Jeśli zaszedł rollback — lista zostaje wyczyszczona przez nowe wywołanie
  (kolejny request) lub jawnie wyczyść w catch przed rzuceniem.

### 4. Rejestracja w DI

Plik: `src/WebApi/Extensions/ServiceCollectionExtensions.cs` (lub gdzie są
rejestrowane behaviors). Dodaj:
```csharp
services.AddScoped<IPostCommitDispatcher, PostCommitDispatcher>();
```

## Refaktor handlerów

### `CreateChatCommandHandler` (K2)

Wstrzyknij `IPostCommitDispatcher dispatcher`.

Wywołania `hubContext.Clients.Group(...).ChatCreated(...)` zastąp:
```csharp
dispatcher.Enqueue(ct =>
    hubContext.Clients.Group(ChatHubGroups.User(targetUserId))
              .ChatCreated(chatWeb, ct));
```
(Albo bez CT jeśli typed-client metody nie przyjmują — wtedy lambda po prostu zwraca Task).

Dotyczy obu broadcastów (direct + group/project).

### `AddChatMemberCommandHandler` (K3)

Wszystkie wywołania `hubContext...` w `NotifyAsync` przenieś do
`dispatcher.Enqueue(...)`. Sama metoda `NotifyAsync` może zostać prywatna,
ale w środku tylko enqueue, bez awaitów na hubContext.

### `DeleteChatCommandHandler` (K4)

```csharp
List<Guid> memberIds = await chatMemberRepo.SelectAsync(
    cm => cm.ChatId == request.ChatId,
    cm => cm.UserId,
    cancellationToken);

await chatRepo.ExecuteDeleteAsync(c => c.Id == request.ChatId, cancellationToken);

foreach (Guid memberId in memberIds)
{
    Guid capturedId = memberId;
    dispatcher.Enqueue(_ =>
        hubContext.Clients.Group(ChatHubGroups.User(capturedId))
                  .ChatDeleted(request.ChatId));
}
```

### `LeaveChatCommandHandler.DissolveGroupAsync` (K5)

Analogicznie — najpierw zbierz memberIds, wykonaj `ExecuteDeleteAsync`,
potem `dispatcher.Enqueue(...)` per member.

W `LeaveGroupAsync` (gdy nie rozwiązujemy grupy) — broadcast po `Insert`/`Update`
(czyli na końcu Handle) przenieś do dispatchera.

### Inne handlery wysyłające SignalR

Sprawdź również:
- `RemoveChatMemberCommandHandler` — wszystkie broadcasty → dispatcher.
- `RenameGroupChatCommandHandler` — broadcast renamed → dispatcher.
- `EditMessageCommandHandler` — broadcast `MessageEdited` → dispatcher (po `SaveChangesAsync`).
- `DeleteMessageCommandHandler` — broadcast `MessageDeleted` → dispatcher.
- `SendMessageCommandHandler` — broadcast `MessageReceived` → dispatcher.
- `MarkAsReadCommandHandler` — broadcast `ReadReceipt` → dispatcher.

**Wszystkie** broadcasty SignalR z handlerów Chat mają iść przez `IPostCommitDispatcher`.

## Zakaz

- Nie zmieniaj kontraktów typed-clienta `IChatClient`.
- Nie zmieniaj logiki biznesowej handlerów poza kolejnością wysyłania eventów.
- Nie wprowadzaj zewnętrznego outboxa (DB-backed) — w pamięci wystarczy.
- Nie ruszaj `MessageHistory.SaveChangesAsync` w handlerach (osobny temat).
- Nie zmieniaj `LoggingBehavior` ani `ValidationBehavior`.

## Kryterium akceptacji

- `dotnet build` — 0 błędów.
- `grep -rn "hubContext\.Clients" src/Chat/CQRS/` — wszystkie wywołania
  są w lambdach przekazanych do `dispatcher.Enqueue(...)`, NIE bezpośrednio
  z `Handle`.
- `TransactionBehavior` wywołuje `dispatcher.DispatchAsync` po commicie.
- Handlery delete (`DeleteChat`, `LeaveChat.DissolveGroup`) zbierają member IDs
  PRZED `ExecuteDeleteAsync`.

## Raport końcowy

- Status buildu.
- Plik(i) nowo utworzone (`PostCommitDispatcher.cs`, `IPostCommitDispatcher.cs`).
- Lista zmodyfikowanych handlerów.
- Wskazanie czy `TransactionBehavior` musiał zostać znacząco przebudowany.
