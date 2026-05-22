# Audyt domeny CQRS — Chat

## BLOK 1 — INWENTARYZACJA

### Pliki CQRS

Domena żyje w `src/Chat/CQRS/` (poza standardowym `src/CQRS/`), w dwóch podkatalogach: `Conversations/` i `Messages/`.

| Plik | Typ | Ścieżka |
|------|-----|---------|
| `CreateChatCommand` | Command | [src/Chat/CQRS/Conversations/CreateChat/CreateChatCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/CreateChat/CreateChatCommand.cs) |
| `CreateChatCommandValidator` | Validator | [src/Chat/CQRS/Conversations/CreateChat/CreateChatCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/CreateChat/CreateChatCommandValidator.cs) |
| `CreateChatCommandHandler` | Handler | [src/Chat/CQRS/Conversations/CreateChat/CreateChatCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/CreateChat/CreateChatCommandHandler.cs) |
| `AddChatMemberCommand` | Command | [src/Chat/CQRS/Conversations/AddChatMember/AddChatMemberCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/AddChatMember/AddChatMemberCommand.cs) |
| `AddChatMemberCommandValidator` | Validator | [src/Chat/CQRS/Conversations/AddChatMember/AddChatMemberCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/AddChatMember/AddChatMemberCommandValidator.cs) |
| `AddChatMemberCommandHandler` | Handler | [src/Chat/CQRS/Conversations/AddChatMember/AddChatMemberCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/AddChatMember/AddChatMemberCommandHandler.cs) |
| `RemoveChatMemberCommand` | Command | [src/Chat/CQRS/Conversations/RemoveChatMember/RemoveChatMemberCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/RemoveChatMember/RemoveChatMemberCommand.cs) |
| `RemoveChatMemberCommandValidator` | Validator | [src/Chat/CQRS/Conversations/RemoveChatMember/RemoveChatMemberCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/RemoveChatMember/RemoveChatMemberCommandValidator.cs) |
| `RemoveChatMemberCommandHandler` | Handler | [src/Chat/CQRS/Conversations/RemoveChatMember/RemoveChatMemberCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/RemoveChatMember/RemoveChatMemberCommandHandler.cs) |
| `LeaveChatCommand` | Command | [src/Chat/CQRS/Conversations/LeaveChat/LeaveChatCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/LeaveChat/LeaveChatCommand.cs) |
| `LeaveChatCommandValidator` | Validator | [src/Chat/CQRS/Conversations/LeaveChat/LeaveChatCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/LeaveChat/LeaveChatCommandValidator.cs) |
| `LeaveChatCommandHandler` | Handler | [src/Chat/CQRS/Conversations/LeaveChat/LeaveChatCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/LeaveChat/LeaveChatCommandHandler.cs) |
| `RenameGroupChatCommand` | Command | [src/Chat/CQRS/Conversations/RenameGroupChat/RenameGroupChatCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/RenameGroupChat/RenameGroupChatCommand.cs) |
| `RenameGroupChatCommandValidator` | Validator | [src/Chat/CQRS/Conversations/RenameGroupChat/RenameGroupChatCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/RenameGroupChat/RenameGroupChatCommandValidator.cs) |
| `RenameGroupChatCommandHandler` | Handler | [src/Chat/CQRS/Conversations/RenameGroupChat/RenameGroupChatCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/RenameGroupChat/RenameGroupChatCommandHandler.cs) |
| `DeleteChatCommand` | Command | [src/Chat/CQRS/Conversations/DeleteChat/DeleteChatCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/DeleteChat/DeleteChatCommand.cs) |
| `DeleteChatCommandValidator` | Validator | [src/Chat/CQRS/Conversations/DeleteChat/DeleteChatCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/DeleteChat/DeleteChatCommandValidator.cs) |
| `DeleteChatCommandHandler` | Handler | [src/Chat/CQRS/Conversations/DeleteChat/DeleteChatCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/DeleteChat/DeleteChatCommandHandler.cs) |
| `GetUserChatsQuery` | Query | [src/Chat/CQRS/Conversations/GetUserChats/GetUserChatsQuery.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetUserChats/GetUserChatsQuery.cs) |
| `GetUserChatsQueryValidator` | Validator | [src/Chat/CQRS/Conversations/GetUserChats/GetUserChatsQueryValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetUserChats/GetUserChatsQueryValidator.cs) |
| `GetUserChatsQueryHandler` | Handler | [src/Chat/CQRS/Conversations/GetUserChats/GetUserChatsQueryHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetUserChats/GetUserChatsQueryHandler.cs) |
| `GetChatMembersQuery` | Query | [src/Chat/CQRS/Conversations/GetChatMembers/GetChatMembersQuery.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetChatMembers/GetChatMembersQuery.cs) |
| `GetChatMembersQueryValidator` | Validator | [src/Chat/CQRS/Conversations/GetChatMembers/GetChatMembersQueryValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetChatMembers/GetChatMembersQueryValidator.cs) |
| `GetChatMembersQueryHandler` | Handler | [src/Chat/CQRS/Conversations/GetChatMembers/GetChatMembersQueryHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetChatMembers/GetChatMembersQueryHandler.cs) |
| `GetAvailableMembersQuery` | Query | [src/Chat/CQRS/Conversations/GetAvailableMembers/GetAvailableMembersQuery.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetAvailableMembers/GetAvailableMembersQuery.cs) |
| `GetAvailableMembersQueryValidator` | Validator | [src/Chat/CQRS/Conversations/GetAvailableMembers/GetAvailableMembersQueryValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetAvailableMembers/GetAvailableMembersQueryValidator.cs) |
| `GetAvailableMembersQueryHandler` | Handler | [src/Chat/CQRS/Conversations/GetAvailableMembers/GetAvailableMembersQueryHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetAvailableMembers/GetAvailableMembersQueryHandler.cs) |
| `GetProjectMatesQuery` | Query | [src/Chat/CQRS/Conversations/GetProjectMates/GetProjectMatesQuery.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetProjectMates/GetProjectMatesQuery.cs) |
| `GetProjectMatesQueryValidator` | Validator | [src/Chat/CQRS/Conversations/GetProjectMates/GetProjectMatesQueryValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetProjectMates/GetProjectMatesQueryValidator.cs) |
| `GetProjectMatesQueryHandler` | Handler | [src/Chat/CQRS/Conversations/GetProjectMates/GetProjectMatesQueryHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetProjectMates/GetProjectMatesQueryHandler.cs) |
| `FindChatsByMembersQuery` | Query | [src/Chat/CQRS/Conversations/FindChatsByMembers/FindChatsByMembersQuery.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/FindChatsByMembers/FindChatsByMembersQuery.cs) |
| `FindChatsByMembersQueryValidator` | Validator | [src/Chat/CQRS/Conversations/FindChatsByMembers/FindChatsByMembersQueryValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/FindChatsByMembers/FindChatsByMembersQueryValidator.cs) |
| `FindChatsByMembersQueryHandler` | Handler | [src/Chat/CQRS/Conversations/FindChatsByMembers/FindChatsByMembersQueryHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/FindChatsByMembers/FindChatsByMembersQueryHandler.cs) |
| `SearchChatsQuery` | Query | [src/Chat/CQRS/Conversations/SearchChats/SearchChatsQuery.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/SearchChats/SearchChatsQuery.cs) |
| `SearchChatsQueryValidator` | Validator | [src/Chat/CQRS/Conversations/SearchChats/SearchChatsQueryValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/SearchChats/SearchChatsQueryValidator.cs) |
| `SearchChatsQueryHandler` | Handler | [src/Chat/CQRS/Conversations/SearchChats/SearchChatsQueryHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/SearchChats/SearchChatsQueryHandler.cs) |
| `SendMessageCommand` | Command | [src/Chat/CQRS/Messages/SendMessage/SendMessageCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/SendMessage/SendMessageCommand.cs) |
| `SendMessageCommandValidator` | Validator | [src/Chat/CQRS/Messages/SendMessage/SendMessageCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/SendMessage/SendMessageCommandValidator.cs) |
| `SendMessageCommandHandler` | Handler | [src/Chat/CQRS/Messages/SendMessage/SendMessageCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/SendMessage/SendMessageCommandHandler.cs) |
| `EditMessageCommand` | Command | [src/Chat/CQRS/Messages/EditMessage/EditMessageCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/EditMessage/EditMessageCommand.cs) |
| `EditMessageCommandValidator` | Validator | [src/Chat/CQRS/Messages/EditMessage/EditMessageCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/EditMessage/EditMessageCommandValidator.cs) |
| `EditMessageCommandHandler` | Handler | [src/Chat/CQRS/Messages/EditMessage/EditMessageCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/EditMessage/EditMessageCommandHandler.cs) |
| `DeleteMessageCommand` | Command | [src/Chat/CQRS/Messages/DeleteMessage/DeleteMessageCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/DeleteMessage/DeleteMessageCommand.cs) |
| `DeleteMessageCommandValidator` | Validator | [src/Chat/CQRS/Messages/DeleteMessage/DeleteMessageCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/DeleteMessage/DeleteMessageCommandValidator.cs) |
| `DeleteMessageCommandHandler` | Handler | [src/Chat/CQRS/Messages/DeleteMessage/DeleteMessageCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/DeleteMessage/DeleteMessageCommandHandler.cs) |
| `GetChatMessagesQuery` | Query | [src/Chat/CQRS/Messages/GetChatMessages/GetChatMessagesQuery.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/GetChatMessages/GetChatMessagesQuery.cs) |
| `GetChatMessagesQueryValidator` | Validator | [src/Chat/CQRS/Messages/GetChatMessages/GetChatMessagesQueryValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/GetChatMessages/GetChatMessagesQueryValidator.cs) |
| `GetChatMessagesQueryHandler` | Handler | [src/Chat/CQRS/Messages/GetChatMessages/GetChatMessagesQueryHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/GetChatMessages/GetChatMessagesQueryHandler.cs) |
| `MarkAsReadCommand` | Command | [src/Chat/CQRS/Messages/MarkAsRead/MarkAsReadCommand.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/MarkAsRead/MarkAsReadCommand.cs) |
| `MarkAsReadCommandValidator` | Validator | [src/Chat/CQRS/Messages/MarkAsRead/MarkAsReadCommandValidator.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/MarkAsRead/MarkAsReadCommandValidator.cs) |
| `MarkAsReadCommandHandler` | Handler | [src/Chat/CQRS/Messages/MarkAsRead/MarkAsReadCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/MarkAsRead/MarkAsReadCommandHandler.cs) |

### Web modele

Wszystkie web modele Chat trafiły do `src/Chat/DTOs/`, **a nie** do `src/Business/Interfaces/WebModels/Chats/` jak nakazuje konwencja:

| WebModel | Ścieżka |
|----------|---------|
| `ChatWeb` | [src/Chat/DTOs/ChatWeb.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/DTOs/ChatWeb.cs) |
| `ChatMemberWeb` | [src/Chat/DTOs/ChatMemberWeb.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/DTOs/ChatMemberWeb.cs) |
| `MessageWeb` | [src/Chat/DTOs/MessageWeb.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/DTOs/MessageWeb.cs) |
| `AvailableMemberWeb` | [src/Chat/DTOs/AvailableMemberWeb.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/DTOs/AvailableMemberWeb.cs) |
| `ChatSearchResultWeb` | [src/Chat/DTOs/ChatSearchResultWeb.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/DTOs/ChatSearchResultWeb.cs) |
| `CreateChatResultWeb` | [src/Chat/DTOs/CreateChatResultWeb.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/DTOs/CreateChatResultWeb.cs) |
| `ProjectContactsGroupWeb` | [src/Chat/DTOs/ProjectContactsGroupWeb.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/DTOs/ProjectContactsGroupWeb.cs) |
| `ProjectMateWeb` | [src/Chat/DTOs/ProjectMateWeb.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/DTOs/ProjectMateWeb.cs) |

### Kontroler i endpointy

[src/WebApi/Controllers/ChatController.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/WebApi/Controllers/ChatController.cs) — route `api/chats`. Endpointy:

| Metoda | Endpoint | Mediator |
|--------|----------|---------|
| GET    | `/api/chats` | `GetUserChatsQuery` |
| GET    | `/api/chats/contacts` | `GetProjectMatesQuery` |
| GET    | `/api/chats/search?q=` | `SearchChatsQuery` |
| POST   | `/api/chats` | `CreateChatCommand` |
| GET    | `/api/chats/by-members?memberIds=` | `FindChatsByMembersQuery` |
| PATCH  | `/api/chats/{chatId}` | `RenameGroupChatCommand` |
| DELETE | `/api/chats/{chatId}` | `DeleteChatCommand` |
| GET    | `/api/chats/{chatId}/members` | `GetChatMembersQuery` |
| GET    | `/api/chats/{chatId}/available-members` | `GetAvailableMembersQuery` |
| POST   | `/api/chats/{chatId}/members` | `AddChatMemberCommand` |
| DELETE | `/api/chats/{chatId}/members/{userId}` | `RemoveChatMemberCommand` |
| POST   | `/api/chats/{chatId}/leave` | `LeaveChatCommand` |
| GET    | `/api/chats/{chatId}/messages?before=&pageSize=` | `GetChatMessagesQuery` |
| POST   | `/api/chats/{chatId}/messages` | `SendMessageCommand` |
| PATCH  | `/api/chats/{chatId}/messages/{messageId}` | `EditMessageCommand` |
| DELETE | `/api/chats/{chatId}/messages/{messageId}` | `DeleteMessageCommand` |
| PUT    | `/api/chats/{chatId}/read` | `MarkAsReadCommand` |

Kontroler ma wyłącznie `[Authorize]` (klasowe, bez polityki). Inline records w pliku: `CreateChatRequest`, `RenameChatRequest`, `AddChatMemberRequest`, `SendMessageRequest`, `EditMessageRequest`. Endpointy nie są zgodne z wzorcem URL `api/tenants/{tenantId}/...`.

### Encje

| Encja | Plik | Bazowa klasa |
|-------|------|--------------|
| `Chat` | [src/Entities/Models/Chats/Chat.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Entities/Models/Chats/Chat.cs) | `BaseEntity` |
| `ChatMember` | [src/Entities/Models/Chats/ChatMember.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Entities/Models/Chats/ChatMember.cs) | `BaseEntity` |
| `MessageHistory` | [src/Entities/Models/Chats/MessageHistory.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Entities/Models/Chats/MessageHistory.cs) | `DeletableEntity` |

`Chat.TenantId` i `Chat.ProjectId` są **nullable** — direct chats nie mają TenantId.

### SignalR Hub i kontrakty

| Plik | Rola |
|------|------|
| [src/Chat/Hubs/ChatHub.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/Hubs/ChatHub.cs) | `sealed class ChatHub : Hub<IChatClient>` z `[Authorize]` |
| [src/Chat/Hubs/IChatClient.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/Hubs/IChatClient.cs) | typed-client interface + payloady (`MessageEditedPayload`, `MessageDeletedPayload`, `UserTypingPayload`, `ReadReceiptPayload`, `RemovedFromChatPayload`, `MemberAddedPayload`) |
| [src/Chat/Hubs/ChatHubGroups.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/Hubs/ChatHubGroups.cs) | helper grup SignalR (`chat:{id}`, `user:{id}`) |

Dodatkowo: [src/Chat/Services/ChatDirectService.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/Services/ChatDirectService.cs), [src/Chat/ChatOptions.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/ChatOptions.cs), [src/Chat/Registration/ChatServiceExtensions.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/Registration/ChatServiceExtensions.cs).

## BLOK 2 — COMMANDS I QUERIES — STRUKTURA

### 2.1 Positional parameters vs explicit properties

**Wszystkie 17 Commands/Queries** używają primary constructor (positional). Konwencja projektu dopuszcza obie formy, ale dla Commands z `IAuthorizableRequest` preferuje się body z `required { get; init; }`.

| Command/Query | Positional | Przykład |
|---------------|------------|---------|
| `CreateChatCommand` | tak | `(Guid? ProjectId, List<Guid> MemberUserIds, string? Name = null)` |
| `AddChatMemberCommand` | tak | `(Guid ChatId, Guid UserId, Guid? ProjectId = null)` |
| `RemoveChatMemberCommand` | tak | `(Guid ChatId, Guid UserId)` |
| `LeaveChatCommand` | tak | `(Guid ChatId)` |
| `RenameGroupChatCommand` | tak | `(Guid ChatId, string NewName)` |
| `DeleteChatCommand` | tak | `(Guid ChatId)` |
| `GetUserChatsQuery` | tak (puste) | `()` |
| `GetChatMembersQuery` | tak | `(Guid ChatId)` |
| `GetAvailableMembersQuery` | tak | `(Guid ChatId)` |
| `GetProjectMatesQuery` | tak (puste) | `()` |
| `FindChatsByMembersQuery` | tak | `(List<Guid> MemberUserIds)` |
| `SearchChatsQuery` | tak | `(string Phrase)` |
| `SendMessageCommand` | tak | `(Guid ChatId, string Content, Guid? ReplyToMessageId = null)` |
| `EditMessageCommand` | tak | `(Guid ChatId, Guid MessageId, string NewContent)` |
| `DeleteMessageCommand` | tak | `(Guid ChatId, Guid MessageId)` |
| `GetChatMessagesQuery` | tak | `(Guid ChatId, Guid? Before = null, int PageSize = 50)` |
| `MarkAsReadCommand` | tak | `(Guid ChatId)` |

### 2.2 Sealed

| Command/Query | Sealed | Uwagi |
|---------------|--------|-------|
| Wszystkie 17 | tak | `public sealed record …` — pełne pokrycie |

### 2.3 Interfejsy i autoryzacja

| Command/Query | Interfejs | IAuthorizableRequest | PermissionCode |
|---------------|-----------|---------------------|----------------|
| `CreateChatCommand` | `IRequestCommand<CreateChatResultWeb>` | **NIE** | brak |
| `AddChatMemberCommand` | `IRequestCommand<Unit>` | **NIE** | brak |
| `RemoveChatMemberCommand` | `IRequestCommand<Unit>` | **NIE** | brak |
| `LeaveChatCommand` | `IRequestCommand<Unit>` | **NIE** | brak |
| `RenameGroupChatCommand` | `IRequestCommand<Unit>` | **NIE** | brak |
| `DeleteChatCommand` | `IRequestCommand<Unit>` | **NIE** | brak |
| `GetUserChatsQuery` | `IRequestQuery<List<ChatWeb>>` | **NIE** | brak |
| `GetChatMembersQuery` | `IRequestQuery<List<ChatMemberWeb>>` | **NIE** | brak |
| `GetAvailableMembersQuery` | `IRequestQuery<List<AvailableMemberWeb>>` | **NIE** | brak |
| `GetProjectMatesQuery` | `IRequestQuery<List<ProjectContactsGroupWeb>>` | **NIE** | brak |
| `FindChatsByMembersQuery` | `IRequestQuery<List<ChatWeb>>` | **NIE** | brak |
| `SearchChatsQuery` | `IRequestQuery<List<ChatSearchResultWeb>>` | **NIE** | brak |
| `SendMessageCommand` | `IRequestCommand<Guid>` | **NIE** | brak |
| `EditMessageCommand` | `IRequestCommand<Unit>` | **NIE** | brak |
| `DeleteMessageCommand` | `IRequestCommand<Unit>` | **NIE** | brak |
| `GetChatMessagesQuery` | `IRequestQuery<List<MessageWeb>>` | **NIE** | brak |
| `MarkAsReadCommand` | `IRequestCommand<Unit>` | **NIE** | brak |

**Żaden Command ani Query nie korzysta z pipeline'u autoryzacji.** Cała autoryzacja w domenie Chat jest implementowana ad-hoc w handlerach (sprawdzanie członkostwa w czacie). Kontroler ma jedynie `[Authorize]` (uwierzytelnienie), bez żadnego `[Authorize(Policy = …)]`. Jest to świadoma decyzja domenowa (autoryzacja oparta o członkostwo, nie o role tenant/project), ale rozjeżdża się z konwencją reszty solution. **Brak magic stringów** — bo brak jakichkolwiek `PermissionCode`.

### 2.4 Wspólne pola — kandydaci do klasy bazowej

| Pole wspólne | Występuje w | Kandydat do wydzielenia |
|--------------|-------------|------------------------|
| `Guid ChatId` | 12 z 17 (wszystkie operacje na pojedynczym czacie) | `ChatScopedCommandBase` / `ChatScopedQueryBase` z `required Guid ChatId` |
| `Guid ChatId + Guid MessageId` | `EditMessageCommand`, `DeleteMessageCommand` | `MessageScopedCommandBase` |

Przy ewentualnym dodaniu `IAuthorizableRequest` (po decyzji domenowej) klasa bazowa stałaby się oczywista.

## BLOK 3 — WALIDATORY

### 3.1 Pokrycie walidatorami

**100% pokrycia** — każdy Command i Query ma odpowiadający walidator (17/17), nawet gdy walidator jest pusty (`GetUserChatsQueryValidator`, `GetProjectMatesQueryValidator`).

| Command/Query | Walidator | Brakujące reguły |
|---------------|-----------|-----------------|
| `CreateChatCommand` | jest | brak walidacji długości `MemberUserIds` (DoS), brak uniqueIds, brak `RequiredId()` na elementach listy |
| `AddChatMemberCommand` | jest | OK |
| `RemoveChatMemberCommand` | jest | OK |
| `LeaveChatCommand` | jest | OK |
| `RenameGroupChatCommand` | jest | OK |
| `DeleteChatCommand` | jest | OK |
| `GetUserChatsQuery` | jest (pusty) | – |
| `GetChatMembersQuery` | jest | OK |
| `GetAvailableMembersQuery` | jest | OK |
| `GetProjectMatesQuery` | jest (pusty) | – |
| `FindChatsByMembersQuery` | jest | brak `MaximumLength` na `MemberUserIds`, brak `UniqueIds()` |
| `SearchChatsQuery` | jest | OK |
| `SendMessageCommand` | jest | brak walidacji `ReplyToMessageId.NotEmpty()` gdy podany |
| `EditMessageCommand` | jest | OK |
| `DeleteMessageCommand` | jest | OK |
| `GetChatMessagesQuery` | jest | brak `Before.NotEmpty()` gdy podany |
| `MarkAsReadCommand` | jest | OK |

### 3.2 Reguły szczegółowe — użycie `CommonValidationExtensions`

**Żaden walidator** w domenie Chat nie używa extension methods (`RequiredId()`, `NonNegativeOrder()`, `UniqueIds()`, `NotCurrentUser()`) z [src/CQRS/Extensions/CommonValidationExtensions.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/CQRS/Extensions/CommonValidationExtensions.cs). Wszystkie reguły są zapisane ręcznie:

| Walidator | Pole | Obecna reguła | Brakująca reguła | Uzasadnienie |
|-----------|------|---------------|-----------------|-------------|
| `AddChatMemberCommandValidator` | `ChatId`, `UserId` | `NotEmpty().WithMessage(…)` | `RequiredId()` | spójność komunikatu i mniej kodu |
| `RemoveChatMemberCommandValidator` | `ChatId`, `UserId` | jw. | `RequiredId()` | jw. |
| `LeaveChatCommandValidator` | `ChatId` | jw. | `RequiredId()` | jw. |
| `RenameGroupChatCommandValidator` | `ChatId` | jw. | `RequiredId()` | jw. |
| `DeleteChatCommandValidator` | `ChatId` | jw. | `RequiredId()` | jw. |
| `GetChatMembersQueryValidator` | `ChatId` | jw. | `RequiredId()` | jw. |
| `GetAvailableMembersQueryValidator` | `ChatId` | jw. | `RequiredId()` | jw. |
| `SendMessageCommandValidator` | `ChatId` | jw. | `RequiredId()` | jw. |
| `EditMessageCommandValidator` | `ChatId`, `MessageId` | jw. | `RequiredId()` | jw. |
| `DeleteMessageCommandValidator` | `ChatId`, `MessageId` | jw. | `RequiredId()` | jw. |
| `GetChatMessagesQueryValidator` | `ChatId` | jw. | `RequiredId()` | jw. |
| `MarkAsReadCommandValidator` | `ChatId` | jw. | `RequiredId()` | jw. |
| `FindChatsByMembersQueryValidator` | `MemberUserIds` | `NotNull + Must(Count >= 1)` | `UniqueIds()`, `Must(ids => ids.All(id => id != Guid.Empty))` | spójność |
| `CreateChatCommandValidator` | `MemberUserIds` | `Must(!ids.Contains(currentUser.Id))` | `NotCurrentUser(currentUser)` (jeśli istnieje) + `UniqueIds()` | spójność |

### 3.3 Spójność — sealed, komunikaty EN/PL, usingi

- **Sealed:** wszystkie 17 walidatorów są `public sealed class …Validator`. ✓
- **Komunikaty:** wszystkie po angielsku, z kropką końcową, ujednolicony styl ✓ (lepiej niż mieszane EN/PL w innych domenach).
- **Usingi:** czyste, brak nieużywanych. ✓
- **`CreateChatCommandValidator`** wstrzykuje `ICurrentUser` do walidatora — działa, ale to jedyny przypadek w domenie. Można rozważyć przeniesienie reguły self-check do handlera lub do extension `NotCurrentUser`.

### 3.4 Wspólne reguły walidacji

| Reguła wspólna | Walidatory | Kandydat do extension |
|----------------|-----------|---------------------|
| `RuleFor(x => x.ChatId).NotEmpty().WithMessage("ChatId is required.")` | 12 walidatorów | `RequiredId()` (już istnieje) |
| `Content/NewContent NotEmpty + MaxLength(4000)` | `SendMessage`, `EditMessage` | extension `MessageContent()` |
| `Name/NewName NotEmpty + MaxLength(200)` | `CreateChat` (warunkowo), `RenameGroupChat` | extension `ChatName()` |

## BLOK 4 — HANDLERY

### 4.1 Struktura

| Handler | Sealed | Brak `var` | Uwagi |
|---------|--------|-----------|-------|
| `CreateChatCommandHandler` | tak | tak | – |
| `AddChatMemberCommandHandler` | tak | tak | – |
| `RemoveChatMemberCommandHandler` | tak | tak | – |
| `LeaveChatCommandHandler` | tak | tak | – |
| `RenameGroupChatCommandHandler` | tak | tak | – |
| `DeleteChatCommandHandler` | tak | tak | – |
| `GetUserChatsQueryHandler` | tak | **NIE** | `var chat = cm.Chat;` w lambdzie (linia ~83) |
| `GetChatMembersQueryHandler` | tak | tak | – |
| `GetAvailableMembersQueryHandler` | tak | tak | – |
| `GetProjectMatesQueryHandler` | tak | tak | – |
| `FindChatsByMembersQueryHandler` | tak | tak | – |
| `SearchChatsQueryHandler` | tak | tak | – |
| `SendMessageCommandHandler` | tak | tak | – |
| `EditMessageCommandHandler` | tak | tak | – |
| `DeleteMessageCommandHandler` | tak | tak | – |
| `GetChatMessagesQueryHandler` | tak | tak | – |
| `MarkAsReadCommandHandler` | tak | tak | – |

Wszystkie 17 handlerów `sealed`. Tylko 1 użycie `var` (w lambdzie wewnątrz LINQ).

### 4.2 Logika biznesowa — długość metody `Handle`

| Handler | Linie ~ | Za dużo logiki | Co wydzielić |
|---------|---------|---------------|-------------|
| `CreateChatCommandHandler` | ~90 (orkiestrator) + dwie metody po ~70 | tak | logika idempotencji direct chat (`HandleDirectAsync` ~55 linii), kompozycja nazwy grupy, `BuildChatWeb` to OK |
| `AddChatMemberCommandHandler` | ~85 (Handle) | tak | `GetAndValidateChatAsync`, `EnsureRequesterCanAddAsync`, `EnsureNewMemberInProjectAsync`, `RecalculateGroupStateAsync` |
| `GetUserChatsQueryHandler` | ~70 (Handle) | tak | `BuildChatWeb`, `ComputeUnreadCount`, `MapMembers` — duża LINQ łańcuch |
| `GetChatMessagesQueryHandler` | ~60 | częściowo | wydzielić paginację/cursor do prywatnej metody |
| `SearchChatsQueryHandler` | ~60 | częściowo | wydzielić budowanie indeksów `messageIdsByChatId` i `chatsWithMemberNameMatch` |
| `RemoveChatMemberCommandHandler` | ~55 | częściowo | reguły uprawnień do `EnsureCanRemoveAsync` |
| `LeaveChatCommandHandler` | ~30 (Handle) + 2 metody | nie | OK — orkiestrator + metody |
| pozostałe | <40 | nie | OK |

Bardzo żaden handler nie ma metod `Map…To…` ani `GetAndValidate…Async` — mapowanie jest wstawione bezpośrednio w `Handle()` (najbardziej widoczne w `GetUserChatsQueryHandler` i `FindChatsByMembersQueryHandler`).

### 4.3 SOLID i DRY

| Handler | Podobny do | Wspólna logika | Kandydat do wydzielenia |
|---------|-----------|---------------|------------------------|
| `CreateChatCommandHandler` `BuildChatWeb` | `AddChatMemberCommandHandler.NotifyAsync` (też tworzy `ChatWeb`), `FindChatsByMembersQueryHandler` (też mapuje `Chat → ChatWeb`) | składanie `ChatWeb` z encji + listy memberów | serwis `IChatProjectionService.MapChatToWeb(...)` lub static `ChatMapper` |
| `GetUserChatsQueryHandler.MapLastMessage` + `GetChatMessagesQueryHandler` (lambda mapująca) + `SendMessageCommandHandler` (ręczne stworzenie `MessageWeb`) + `EditMessageCommandHandler` | mapowanie `MessageHistory → MessageWeb` | static `MessageMapper.MapToWeb(...)` |
| `GetUserChatsQueryHandler` + `GetChatMembersQueryHandler` + `SearchChatsQueryHandler` | pobranie `userNames` + budowa `ChatMemberWeb` | `IChatMemberProjection` lub static helper |
| `DeleteChatCommandHandler.Handle` + `LeaveChatCommandHandler.DissolveGroupAsync` | identyczna sekwencja: `SelectAsync(memberIds) → Group(user:{}).ChatDeleted(chatId) → ExecuteDeleteAsync` | prywatny `ChatDissolutionService` |
| `RemoveChatMemberCommandHandler` + `LeaveChatCommandHandler.LeaveGroupAsync` + `AddChatMemberCommandHandler` | sprawdzanie membership + ChatHubGroups.User notifications | wspólna metoda `EnsureMembershipAsync(chatId, userId)` |
| Wszystkie handlery | duplikacja konstruktora (5–6 zależności) i `using Entities.Models.*` (8 zbędnych namespace per plik) | – | bazowa abstrakcja (np. `ChatHandlerBase` z `currentUser`, `hubContext`, `logger`) |

### 4.4 Obsługa błędów

Sprawdzenia:

| Handler | Problem | Ryzyko |
|---------|---------|--------|
| Wszystkie | `chat == null` / `member == null` zamiast `is null` (konwencja projektu nakazuje `is null`/`is not null`) | styl, brak ryzyka funkcjonalnego |
| Wszystkie wywołania `NotFoundApiException("Chat", …)`, `NotFoundApiException("ChatMember", …)`, `NotFoundApiException("Message", …)` | hardkodowane stringi zamiast `nameof(Chat)`, `nameof(ChatMember)`, `nameof(MessageHistory)` | refactor risk gdy ktoś zmieni nazwę encji |
| `AddChatMemberCommandHandler` linia ~76 | `ConflictApiException("ChatMember", request.UserId.ToString())` — komunikat dla użytkownika nie wyjaśnia co poszło nie tak | UX |
| `SendMessageCommandHandler` | brak walidacji że `ReplyToMessageId` istnieje i należy do tego samego chatu | spójność danych — można wstawić reply do nieistniejącej/cudzej wiadomości |
| `AddChatMemberCommandHandler` linia ~93 | `ValidationApiException("ProjectId is required when adding a member to a direct chat.")` — to powinno być `BadRequest`, jest OK, ale mogłoby być w walidatorze warunkowo | – |

Brak rzucania `InvalidOperationException` jako zamiennika dla `ApiException` ✓.

### 4.5 Zapytania do DB

| Handler | Problem | Ryzyko |
|---------|---------|--------|
| `CreateChatCommandHandler.HandleDirectAsync` linie 70–88 | **N+1**: pobiera wszystkie membership użytkownika z `Include(Chat)`, potem w pętli `chatMemberRepo.AnyAsync(...)` per direct-chat | wydajność rośnie z liczbą czatów użytkownika |
| `FindChatsByMembersQueryHandler.Handle` linie 47–58 | **N+1**: pętla `foreach (Guid memberId in request.MemberUserIds.Distinct())` → osobne query per członek | wydajność O(M) zapytań |
| `GetUserChatsQueryHandler.Handle` linia 49 | `messageRepo.GetBySearch(m => chatIds.Contains(m.ChatId) && m.DeletedAt == null)` — **ładuje WSZYSTKIE** wiadomości z wszystkich czatów do pamięci, potem w C# wybiera ostatnią i liczy unread | krytyczna wydajność przy długich czatach; potencjalny OOM |
| `GetChatMessagesQueryHandler.Handle` linie 48–63 | `messageRepo.GetBySearch(m => m.ChatId == request.ChatId)` — **ładuje WSZYSTKIE** wiadomości czatu do pamięci, potem `OrderByDescending` + `Skip`/`Take` w C# zamiast w SQL | wydajność/pamięć; obala sens kursorowej paginacji |
| `SearchChatsQueryHandler.Handle` linia 64 | `m.Content.Contains(phrase)` — przekłada się na SQL `LIKE %x%`; przy dużym wolumenie wymaga indeksu pełnotekstowego | wydajność przy >100k wiadomości |
| `SearchChatsQueryHandler.Handle` linie 79–88 | matching imion w pamięci po pobraniu **wszystkich** członków wszystkich moich czatów | wydajność przy dużej liczbie czatów |
| `RenameGroupChatCommandHandler.Handle` linia 50 | `chatRepo.GetById(request.ChatId)` — **bez** `cancellationToken` (signature repozytorium nie przyjmuje CT) | brak anulowania dla operacji DB |
| `LeaveChatCommandHandler.Handle` linia 60 | `chatMemberRepo.GetFirstBySearch(...)` **bez** CT (parametr opcjonalny pominięty) | brak anulowania |
| `AddChatMemberCommandHandler.Handle` linie 60, 65, 86 | `GetFirstBySearch(...)` / niektóre wywołania `AnyAsync` **bez** CT mimo że sygnatura przyjmuje | brak anulowania |
| `RemoveChatMemberCommandHandler.Handle` linie 60, 65, 71 | `GetFirstBySearch(...)` **bez** CT | brak anulowania |
| `MarkAsReadCommandHandler.Handle` linia 47 | `chatMemberRepo.GetFirstBySearch(...)` **bez** CT | brak anulowania |
| `EditMessageCommandHandler` + `DeleteMessageCommandHandler` + `MarkAsReadCommandHandler` + `SendMessageCommandHandler` | wywołują `SaveChangesAsync` **wewnątrz handlera** mimo że pipeline ma `TransactionBehavior` (commit na końcu); w `AddChatMemberCommandHandler` brak `SaveChangesAsync` po `Insert` (polega się na pipeline) | niespójność — niektóre handlery zapisują same, inne polegają na `TransactionBehavior`; ryzyko podwójnego commit/inconsistent transaction scope |
| Wszystkie handlery | predykaty zawierają tylko `ChatId` (multi-tenant safety oparta wyłącznie na membership w czacie); **żaden** predykat nie filtruje po `TenantId`/`ProjectId` | akceptowalne dla domeny user-centric (chat), bo membership jest "twardszą" granicą — ale brak `defense in depth` |
| `IRepository<ChatMember> chatMemberRepo` w `GetUserChatsQueryHandler` (linia 23 sygnatura) | używa `IRepository` zamiast `IReadRepository` w handlerze tylko-do-odczytu — ale **akurat w GetUserChatsQueryHandler jest `IReadRepository`** ✓ |
| `GetAvailableMembersQueryHandler` | `IRepository<ChatMember> chatMemberRepo` (linia 22) — pure read, powinno być `IReadRepository<ChatMember>` | drobne — narusza ISP |
| `FindChatsByMembersQueryHandler` | `IRepository<ChatMember> chatMemberRepo` (linia 22) — pure read, powinno być `IReadRepository<ChatMember>` | drobne — narusza ISP |

## BLOK 5 — WEB MODELE

### 5.1 Sealed record z explicit properties

| WebModel | Sealed record | Explicit properties | Uwagi |
|----------|--------------|--------------------|----|
| `ChatWeb` | **NIE** (`public record`) | nie (primary ctor) | brakuje `sealed` |
| `ChatMemberWeb` | **NIE** | nie (primary ctor) | brakuje `sealed` |
| `MessageWeb` | **NIE** | nie (primary ctor) | brakuje `sealed` |
| `AvailableMemberWeb` | **NIE** | nie | brakuje `sealed` |
| `ChatSearchResultWeb` | **NIE** | nie | brakuje `sealed` |
| `CreateChatResultWeb` | **NIE** | nie | brakuje `sealed` |
| `ProjectContactsGroupWeb` | **NIE** | nie | brakuje `sealed` |
| `ProjectMateWeb` | **NIE** | nie | brakuje `sealed` |
| Payloady SignalR (`MessageEditedPayload`, `MessageDeletedPayload`, `UserTypingPayload`, `ReadReceiptPayload`, `RemovedFromChatPayload`, `MemberAddedPayload`) | **NIE** | nie | publiczne, w pliku `IChatClient.cs`, brak `sealed` |
| Inline w kontrolerze: `CreateChatRequest`, `RenameChatRequest`, `AddChatMemberRequest`, `SendMessageRequest`, `EditMessageRequest` | **NIE** | nie | inline w `ChatController.cs` zamiast osobnych plików |

Wszystkie web modele to czyste DTO bez pól technicznych EF (FK shadow properties, kolekcje nawigacyjne) — ✓. Lokalizacja jest jednak niezgodna z konwencją: powinny być w `Business/Interfaces/WebModels/Chats/`.

### 5.2 Duplikacje

| Duplikowane pola | W modelach | Kandydat do wydzielenia |
|------------------|-----------|------------------------|
| `Guid UserId, string FirstName, string LastName` | `ChatMemberWeb`, `AvailableMemberWeb`, `ProjectMateWeb` | wspólny `UserSummaryWeb(Guid Id, string FirstName, string LastName)` lub typ bazowy |
| `Guid ChatId, Guid MessageId` w payloadach | `MessageEditedPayload`, `MessageDeletedPayload` | – akceptowalne dla typed-client SignalR |
| `Guid? ProjectId, Guid? TenantId` | `ChatWeb`, `ChatSearchResultWeb` | wspólny `ChatScopeWeb` |

## BLOK 6 — PROBLEMY I REKOMENDACJE

### Krytyczne

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|-------------|
| K1 | **Brak autoryzacji opartej na uprawnieniach.** Kontroler ma tylko `[Authorize]` (uwierzytelnienie). Żaden Command/Query nie implementuje `IAuthorizableRequest`. Cała kontrola dostępu opiera się wyłącznie na membership w czacie sprawdzanym ad-hoc w handlerach. | [ChatController.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/WebApi/Controllers/ChatController.cs), wszystkie Commands/Queries | brak warstwowej obrony, brak audytu uprawnień przez pipeline, niespójność z resztą solution; trudno wymusić blokadę funkcji per tenant/role | dodać dedykowane `PermissionCodes.ChatRead/Write/Manage`, wstawić `IAuthorizableRequest` w Commands/Queries; alternatywnie świadomie udokumentować decyzję i wyłączyć handler-pomijanie autoryzacji |
| K2 | **`CreateChatCommandHandler` — broadcast SignalR przed jawnym SaveChanges drugiej operacji.** Pierwszy `SaveChangesAsync` zachowuje `Chat`, ale `Insert` członków jest BEZ kolejnego `SaveChangesAsync`; opieramy się o `TransactionBehavior`. Tymczasem `hubContext.…ChatCreated(...)` jest wywoływane **przed** commitem transakcji pipeline'u — klient może odpytać API i nie znaleźć członków. | [CreateChatCommandHandler.cs#L99-L117](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/CreateChat/CreateChatCommandHandler.cs#L99) | wyścig: SignalR notification przed commitem DB; klient widzi event, robi GET i dostaje pustą listę członków | przebudować na `outbox`/post-commit dispatch albo wymusić `SaveChangesAsync` przed broadcastem (jak w `SendMessageCommandHandler`), spójnie we wszystkich handlerach |
| K3 | **`AddChatMemberCommandHandler` — broadcast SignalR (`MemberAdded`, `ChatCreated`) przed commitem.** `Insert(newMember)` i `Update(chat)` bez jawnego `SaveChangesAsync`; tymczasem `NotifyAsync(...)` jest wywoływane **w trakcie `Handle`**, czyli przed commitem `TransactionBehavior`. | [AddChatMemberCommandHandler.cs#L122-L140](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/AddChatMember/AddChatMemberCommandHandler.cs#L122) | jw. — wyścig event vs. stan DB | jw. |
| K4 | **`DeleteChatCommandHandler` — `ExecuteDeleteAsync` po wysłaniu eventu `ChatDeleted`.** Jeśli usuwanie się nie powiedzie, klient już dostał event. | [DeleteChatCommandHandler.cs#L70-L83](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/DeleteChat/DeleteChatCommandHandler.cs#L70) | klient lokalnie czyści dane, baza zachowuje czat → desync | wyślij event po pomyślnym `ExecuteDeleteAsync` |
| K5 | **`LeaveChatCommandHandler.DissolveGroupAsync` — analogicznie**: event `ChatDeleted` przed `ExecuteDeleteAsync`. | [LeaveChatCommandHandler.cs#L77-L90](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/LeaveChat/LeaveChatCommandHandler.cs#L77) | jw. | jw. |
| K6 | **`SendMessageCommandHandler` — brak walidacji że `ReplyToMessageId` istnieje i należy do tego samego czatu.** | [SendMessageCommandHandler.cs#L57-L68](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/SendMessage/SendMessageCommandHandler.cs#L57) | leak referencji między czatami: użytkownik może odpowiedzieć na wiadomość z innego czatu (jeśli zna `MessageId`) | sprawdzić `messageRepo.AnyAsync(m => m.Id == request.ReplyToMessageId && m.ChatId == request.ChatId)` |

### Wysokie

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|-------------|
| W1 | **Web modele w `Chat/DTOs/`** zamiast `Business/Interfaces/WebModels/Chats/`. | wszystkie pliki w [src/Chat/DTOs/](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/DTOs/) | niespójność architekturalna, łamie wzorzec gdzie WebApi nie zna `Chat.DTOs` | przenieść; ewentualnie pozostawić tylko payloady SignalR (one są specyficzne dla hub'a) |
| W2 | **Brak `sealed` na wszystkich web modelach i payloadach SignalR.** | [Chat/DTOs/*.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/DTOs/), [IChatClient.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/Hubs/IChatClient.cs) | dziedziczenie record może łamać kontrakt API/SignalR (slicing, polymorphism) | dodać `sealed` |
| W3 | **`GetChatMessagesQueryHandler` ładuje wszystkie wiadomości czatu do pamięci**, paginacja kursorowa wykonywana w C#. | [GetChatMessagesQueryHandler.cs#L48-L63](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/GetChatMessages/GetChatMessagesQueryHandler.cs#L48) | wydajność/OOM przy długich czatach | użyć `IQueryable`-style metody repozytorium (`OrderByDescending(CreatedAt).Where(m.CreatedAt < cursorCreatedAt).Take(pageSize)`) — wymaga rozszerzenia repo lub raw EF |
| W4 | **`GetUserChatsQueryHandler` ładuje WSZYSTKIE niezdejlitowane wiadomości** ze wszystkich czatów użytkownika. | [GetUserChatsQueryHandler.cs#L49](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetUserChats/GetUserChatsQueryHandler.cs#L49) | wydajność krytyczna przy aktywnym użytkowniku | osobne zapytanie per chat o `LastMessage` (subquery / window function) lub `GROUP BY ChatId` z agregatem |
| W5 | **N+1 w `CreateChatCommandHandler.HandleDirectAsync`** (loop `AnyAsync` per chat). | [CreateChatCommandHandler.cs#L70-L88](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/CreateChat/CreateChatCommandHandler.cs#L70) | wydajność + race condition przy idempotencji | jedno zapytanie: znajdź `Chat` z `IsGroupChat=false` i dwoma członkami `{currentUser.Id, targetUserId}` |
| W6 | **N+1 w `FindChatsByMembersQueryHandler`** (loop per member). | [FindChatsByMembersQueryHandler.cs#L47-L58](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/FindChatsByMembers/FindChatsByMembersQueryHandler.cs#L47) | wydajność przy dużej liczbie członków zapytania | jedno zapytanie: `GROUP BY ChatId HAVING COUNT(DISTINCT UserId WHERE UserId IN (...)) = N` |
| W7 | **Brak `IAuthorizableRequest` + brak ról w `ChatHub`**: `JoinChat` sprawdza membership, ale `SendMessage` i `MarkAsRead` w hubie wywołują `mediator.Send`, gdzie autoryzacja również nie istnieje. | [ChatHub.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/Hubs/ChatHub.cs) | spójność z K1 | jw. |
| W8 | **Inline DTO klasy w `ChatController.cs`** (`CreateChatRequest` etc.) zamiast osobnych plików w `WebModels`. | [ChatController.cs#L209-L214](02-ApplicationServices/ProductDataManagementWebAPI/src/WebApi/Controllers/ChatController.cs#L209) | trudniejszy reuse, brak `sealed` | przenieść do `Business/Interfaces/WebModels/Chats/Requests/` |
| W9 | **Endpointy nie używają wzorca `api/tenants/{tenantId}/...`**, ponieważ chat może być cross-tenant (direct chat) lub tenant-scoped (group chat). | [ChatController.cs#L26](02-ApplicationServices/ProductDataManagementWebAPI/src/WebApi/Controllers/ChatController.cs#L26) | niespójność z resztą API | świadoma decyzja domenowa do udokumentowania (każdy zasób user-scoped, nie tenant-scoped) |
| W10 | **`ChatController` używa `var` we wszystkich akcjach.** | [ChatController.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/WebApi/Controllers/ChatController.cs) | konwencja projektu zakazuje `var` | zamienić na typy explicit (`List<ChatWeb> result = await Send(...)`) |
| W11 | **`ChatHub.SendMessage`/`MarkAsRead` używają `var command = …`.** | [ChatHub.cs#L113-L122](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/Hubs/ChatHub.cs#L113) | jw. | jw. |
| W12 | **`SearchChatsQueryHandler` ładuje wszystkie member-rows i wszystkie chat-rows usera** (członków i czatów), filtrowanie nazwami w pamięci. | [SearchChatsQueryHandler.cs#L57-L88](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/SearchChats/SearchChatsQueryHandler.cs#L57) | wydajność | przesunąć filtry do SQL (joinem do `User`) lub dodać limit |
| W13 | **`AddChatMemberCommandHandler` — wstrzykuje **dwa** repo (`IReadRepository<Chat>` + `IRepository<Chat>`)** dla tej samej encji. | [AddChatMemberCommandHandler.cs#L26-L28](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/AddChatMember/AddChatMemberCommandHandler.cs#L26) | bałagan DI, łamie ISP w drugą stronę | użyć tylko `IRepository<Chat>` (write-capable obejmuje read) |
| W14 | **Brak DRY mapowań** `MessageHistory → MessageWeb` i `Chat → ChatWeb` powtórzonych w 4–5 handlerach. | [GetUserChatsQueryHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetUserChats/GetUserChatsQueryHandler.cs), [GetChatMessagesQueryHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/GetChatMessages/GetChatMessagesQueryHandler.cs), [SendMessageCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/SendMessage/SendMessageCommandHandler.cs), [FindChatsByMembersQueryHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/FindChatsByMembers/FindChatsByMembersQueryHandler.cs), [CreateChatCommandHandler.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/CreateChat/CreateChatCommandHandler.cs) | rozjazd kontraktów po zmianie w jednym miejscu | static `ChatMapper.MapMessage(...)`, `ChatMapper.MapChatToWeb(...)` |

### Normalne

| # | Problem | Plik | Ryzyko | Rekomendacja |
|---|---------|------|--------|-------------|
| N1 | Każdy handler ma 8 nieużywanych `using Entities.Models.{Costs,Files,Notifications,Projects,Roles,Tenants,Users,WorkSchedules}` (copy-paste). | wszystkie *Handler.cs | hałas w plikach | usunąć nieużywane usingi |
| N2 | `chat == null` zamiast `chat is null`, `member == null` zamiast `member is null` we wszystkich handlerach. | wszystkie handlery | styl niezgodny z konwencją projektu | zamienić na `is null` / `is not null` |
| N3 | `NotFoundApiException("Chat", …)` / `("ChatMember", …)` / `("Message", …)` — magic stringi. | wszystkie handlery | refactor risk | zamienić na `nameof(Chat)`, `nameof(ChatMember)`, `nameof(MessageHistory)` |
| N4 | Walidatory nie używają `RequiredId()`/`UniqueIds()` z `CommonValidationExtensions`. | wszystkie walidatory | DRY | zastosować extension methods |
| N5 | `CreateChatCommandHandler` używa raw `$"user:{targetUserId}"` zamiast `ChatHubGroups.User(targetUserId)` | [CreateChatCommandHandler.cs#L114, L189](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/CreateChat/CreateChatCommandHandler.cs#L114) | niespójność, magic string | użyć `ChatHubGroups.User(...)` |
| N6 | `AddChatMemberCommandHandler.NotifyAsync` używa `$"chat:{chat.Id}"` i `$"user:{newMember.UserId}"` zamiast `ChatHubGroups.*`. | [AddChatMemberCommandHandler.cs#L155, L168](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/AddChatMember/AddChatMemberCommandHandler.cs#L155) | jw. | jw. |
| N7 | Brak metod `Map…To…Web(...)` i `GetAndValidate…Async(...)` jako prywatnych pomocników; mapowania wstawione bezpośrednio w `Handle`. | wszystkie handlery z mapowaniem | czytelność, atomowość | wydzielić jak w wzorcu projektu |
| N8 | `GetAvailableMembersQueryHandler`, `FindChatsByMembersQueryHandler` używają `IRepository<ChatMember>` mimo tylko-czytania. | [GetAvailableMembersQueryHandler.cs#L24](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetAvailableMembers/GetAvailableMembersQueryHandler.cs#L24), [FindChatsByMembersQueryHandler.cs#L23](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/FindChatsByMembers/FindChatsByMembersQueryHandler.cs#L23) | ISP | użyć `IReadRepository<ChatMember>` |
| N9 | `GetUserChatsQueryHandler` używa `var chat = cm.Chat;` w lambdzie. | [GetUserChatsQueryHandler.cs#L83](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/GetUserChats/GetUserChatsQueryHandler.cs#L83) | konwencja `no var` | zamienić na `Chat chat = cm.Chat;` |
| N10 | Wywołania repozytorium bez `cancellationToken` w wielu miejscach (mimo że sygnatura przyjmuje). | większość handlerów (`AddChatMember`, `RemoveChatMember`, `LeaveChat`, `MarkAsRead`, `RenameGroupChat`) | brak anulowania | dopisać `cancellationToken` |
| N11 | Niespójność jawnego `SaveChangesAsync` — niektóre handlery wywołują same, inne polegają na `TransactionBehavior`. | porównaj `EditMessageCommandHandler` (jest) vs `AddChatMemberCommandHandler` (brak) | dwuznaczność transakcyjna | ustalić jedną konwencję; preferowana — polegać na pipeline w Commands |
| N12 | `CreateChatCommandValidator` wstrzykuje `ICurrentUser` — jedyny taki przypadek w domenie. | [CreateChatCommandValidator.cs#L8](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/CreateChat/CreateChatCommandValidator.cs#L8) | niespójność | przenieść self-check do handlera lub do extension `NotCurrentUser(currentUser)` |
| N13 | Inline records `CreateChatRequest` etc. nie są `sealed`. | [ChatController.cs#L209-L214](02-ApplicationServices/ProductDataManagementWebAPI/src/WebApi/Controllers/ChatController.cs#L209) | spójność | dodać `sealed` po przeniesieniu do osobnych plików |
| N14 | `EditMessageCommandHandler` używa `IReadRepository<MessageHistory>` + `IRepository<MessageHistory>` — analogicznie do W13. | [EditMessageCommandHandler.cs#L25-L26](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/EditMessage/EditMessageCommandHandler.cs#L25), [DeleteMessageCommandHandler.cs#L24-L25](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Messages/DeleteMessage/DeleteMessageCommandHandler.cs#L24) | bałagan DI | użyć jednego `IRepository<MessageHistory>` |
| N15 | `RenameGroupChatCommandHandler` używa `chatRepo.GetById(...)` (bez CT) podczas gdy reszta używa `GetFirstBySearch(... , ct)`. | [RenameGroupChatCommandHandler.cs#L50](02-ApplicationServices/ProductDataManagementWebAPI/src/Chat/CQRS/Conversations/RenameGroupChat/RenameGroupChatCommandHandler.cs#L50) | brak CT | użyć `GetFirstBySearch(c => c.Id == request.ChatId, ct)` |
| N16 | Encje `Chat`, `ChatMember`, `MessageHistory` mają publiczne settery i nie hermetyzują stanu (np. `IsGroupChat` modyfikowane bezpośrednio z handlera). | [Chat.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Entities/Models/Chats/Chat.cs), [ChatMember.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Entities/Models/Chats/ChatMember.cs), [MessageHistory.cs](02-ApplicationServices/ProductDataManagementWebAPI/src/Entities/Models/Chats/MessageHistory.cs) | konwencja projektu zaleca metody biznesowe na encjach | dodać `Rename(string)`, `AddMember(ChatMember)`, `MarkRead(DateTime)`, `Edit(string)`, `SoftDelete()` |

## PODSUMOWANIE

| Metryka | Wartość |
|---------|---------|
| Liczba Commands | 9 |
| Liczba Queries | 8 |
| Liczba Walidatorów | 17 |
| Liczba Handlerów | 17 |
| Pokrycie walidatorami | **100% (17/17)** |
| Commands/Queries z positional params | 17/17 (100%) |
| Commands/Queries `sealed` | 17/17 (100%) |
| Commands/Queries z `IAuthorizableRequest` | **0/17 (0%)** |
| Walidatory `sealed` | 17/17 (100%) |
| Walidatory używające `CommonValidationExtensions` | **0/17 (0%)** |
| Handlery `sealed` | 17/17 (100%) |
| Handlery z explicit types (brak `var`) | 16/17 (94%) — 1 wystąpienie `var` w lambdzie |
| Wystąpienia `var` w domenie (handlery+kontroler+hub) | ~22 (1 handler + ~17 kontroler + 2 hub + inne) |
| Web modele `sealed record` | **0/14 (0%)** |
| Web modele w docelowej lokalizacji `Business/Interfaces/WebModels/Chats/` | **0/14 (0%)** |
| Problemy krytyczne | 6 |
| Problemy wysokie | 14 |
| Problemy normalne | 16 |
