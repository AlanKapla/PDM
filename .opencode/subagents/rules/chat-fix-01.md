# Chat — Fix 01: Quick wins i cleanup

Cel: ujednolicić styl kodu w domenie Chat z konwencjami projektu, bez zmian
strukturalnych. To podstawa pod kolejne fixy.

Kontekst: pełny audyt w `.github/subagents/rules/chat-audit.md`.
Zasada nadrzędna: **zakaz `var`** — zawsze explicit types.
**Brak zmian behawioralnych** poza punktem 9 (K6 walidacja ReplyToMessageId).

## Zakres

Dotykamy plików (przeczytaj każdy przed edycją!):
- `02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/**/*Handler.cs` (17 handlerów)
- `02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/**/*Validator.cs` (17 walidatorów)
- `02-ApplicationServices/ProductDataManagementWebAPI/src/WebApi/Controllers/ChatController.cs`
- `02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/Hubs/ChatHub.cs`
- `02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/Hubs/IChatClient.cs`
- `02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/DTOs/*.cs`

## Zmiany

### 1. Usuń nieużywane usingi (N1)
W każdym handlerze są zbędne `using Entities.Models.{Costs,Files,Notifications,Projects,Roles,Tenants,Users,WorkSchedules}`. Usuń.

### 2. `is null` / `is not null` (N2)
Zamień **wszystkie** `== null` / `!= null` na `is null` / `is not null` w handlerach Chat.

### 3. `nameof` zamiast magic stringów w `NotFoundApiException` (N3)
- `"Chat"` → `nameof(Chat)`
- `"ChatMember"` → `nameof(ChatMember)`
- `"Message"` → `nameof(MessageHistory)`

Tak samo w `ConflictApiException` jeśli dotyczy nazwy encji.

### 4. Eliminacja `var` (W10, W11, N9)
- `ChatController.cs` — wszystkie akcje używają `var`. Zamień na typy explicit.
- `ChatHub.cs` (`SendMessage`, `MarkAsRead`) — `var command = …` → typ explicit.
- `GetUserChatsQueryHandler.cs` — `var chat = cm.Chat;` → `Chat chat = cm.Chat;`.

### 5. CancellationToken w wywołaniach repo (N10, N15)
Dopisz `cancellationToken` do wszystkich wywołań `GetFirstBySearch`, `AnyAsync`, `SelectAsync`, `GetBySearch`, `ExecuteDeleteAsync` w handlerach gdzie został pominięty.

W `RenameGroupChatCommandHandler`: `chatRepo.GetById(request.ChatId)` → `chatRepo.GetFirstBySearch(c => c.Id == request.ChatId, cancellationToken)`.

### 6. `IReadRepository<T>` dla pure-read (N8, W13, N14)
- `GetAvailableMembersQueryHandler` — `IRepository<ChatMember>` → `IReadRepository<ChatMember>`.
- `FindChatsByMembersQueryHandler` — `IRepository<ChatMember>` → `IReadRepository<ChatMember>`.
- `AddChatMemberCommandHandler` — usuń duplikat `IReadRepository<Chat>`, zostaw tylko `IRepository<Chat>` (write-capable obejmuje read).
- `EditMessageCommandHandler` i `DeleteMessageCommandHandler` — usuń `IReadRepository<MessageHistory>`, zostaw tylko `IRepository<MessageHistory>`.

### 7. `CommonValidationExtensions` (N4, N12)
W każdym walidatorze:
- `RuleFor(x => x.ChatId).NotEmpty().WithMessage("ChatId is required.")` → `RuleFor(x => x.ChatId).RequiredId();`
- to samo dla `UserId`, `MessageId`, `Before` (jeśli wymagane).

`FindChatsByMembersQueryValidator`:
- Zamień `Must(Count >= 1)` na czytelną regułę i dodaj `RuleFor(x => x.MemberUserIds).UniqueIds()`.
- Dodaj `Must(ids => ids.All(id => id != Guid.Empty)).WithMessage("MemberUserIds cannot contain empty GUIDs.")`.
- Dodaj `MaximumLength`/`Must(Count <= 50)` (DoS guard).

`CreateChatCommandValidator`:
- Zamień `Must(!ids.Contains(currentUser.Id))` na `RuleFor(x => x.MemberUserIds).NotCurrentUser(currentUser).UniqueIds()`.
- Dodaj limit liczby memberów (np. <= 50).

### 8. `ChatHubGroups` zamiast magic stringów SignalR (N5, N6)
W `CreateChatCommandHandler` i `AddChatMemberCommandHandler` zamień `$"user:{id}"` i `$"chat:{id}"` na `ChatHubGroups.User(id)` / `ChatHubGroups.Chat(id)`.

### 9. K6 — walidacja `ReplyToMessageId` należy do tego samego czatu
W `SendMessageCommandHandler.Handle`:
- Po sprawdzeniu membership, jeśli `request.ReplyToMessageId is not null`:
  - `bool replyExists = await messageRepo.AnyAsync(m => m.Id == request.ReplyToMessageId.Value && m.ChatId == request.ChatId && m.DeletedAt == null, cancellationToken);`
  - Jeśli nie — `throw new NotFoundApiException(nameof(MessageHistory), request.ReplyToMessageId.Value.ToString());`

### 10. `sealed` na web modelach i payloadach SignalR (W2)
- Wszystkie pliki w `src/Chat/DTOs/*.cs` — `public record X` → `public sealed record X`.
- `IChatClient.cs` — wszystkie payloady (`MessageEditedPayload`, `MessageDeletedPayload`, `UserTypingPayload`, `ReadReceiptPayload`, `RemovedFromChatPayload`, `MemberAddedPayload`) — dodaj `sealed`.
- Inline records w `ChatController.cs` (`CreateChatRequest`, `RenameChatRequest`, `AddChatMemberRequest`, `SendMessageRequest`, `EditMessageRequest`) — dodaj `sealed`.

## Zakaz

- Nie ruszaj struktury Commands/Queries/Handlerów (positional params zostają).
- Nie przenoś plików (zrobi to fix-02).
- Nie zmieniaj logiki SignalR ordering (zrobi to fix-03).
- Nie dodawaj `IAuthorizableRequest` (zrobi to fix-05).
- Nie zmieniaj routingu (zrobi to fix-06).
- Nie optymalizuj zapytań (zrobi to fix-07).

## Kryterium akceptacji

- `dotnet build src/WebApi/WebApi.csproj --nologo` — 0 błędów.
- `grep -r "\bvar\b" src/Chat/ src/WebApi/Controllers/ChatController.cs` powinien zwrócić tylko miejsca, których nie da się przepisać (np. `using var`).
- `grep -r "== null\|!= null" src/Chat/CQRS/` — brak wyników.
- `grep -r "NotFoundApiException(\"Chat\"" src/Chat/CQRS/` — brak wyników.

## Raport końcowy

Zwróć:
- Status buildu.
- Liczba zmodyfikowanych plików per kategoria (Handler/Validator/DTO/Hub/Controller).
- Lista pominiętych zmian z uzasadnieniem (jeśli były).
