# Chat — Fix 06: Routing tenant-scoped + split direct/group chats

Cel: dostosować routing kontrolera do konwencji `api/tenants/{tenantId}/...`
dla operacji tenant/project-scoped, zachowując osobny endpoint dla
direct chats (cross-tenant).

Kontekst: audyt `.opencode/subagents/rules/chat-audit.md`, problem W9.
Wymaga ukończenia fix-05 (split CreateChat na Direct + Group).

## Architektura routingu

### Group/project chats — tenant-scoped

Route prefix: `api/tenants/{tenantId}/chats`

| HTTP | Endpoint | Mediator |
|---|---|---|
| GET    | `/api/tenants/{tenantId}/chats` | `GetTenantChatsQuery` (refaktor `GetUserChats` z filtrem TenantId) |
| GET    | `/api/tenants/{tenantId}/chats/search?q=` | `SearchChatsQuery` |
| POST   | `/api/tenants/{tenantId}/chats` | `CreateGroupChatCommand` |
| GET    | `/api/tenants/{tenantId}/chats/contacts` | `GetProjectMatesQuery` |
| PATCH  | `/api/tenants/{tenantId}/chats/{chatId}` | `RenameGroupChatCommand` |
| DELETE | `/api/tenants/{tenantId}/chats/{chatId}` | `DeleteChatCommand` |
| GET    | `/api/tenants/{tenantId}/chats/{chatId}/members` | `GetChatMembersQuery` |
| GET    | `/api/tenants/{tenantId}/chats/{chatId}/available-members` | `GetAvailableMembersQuery` |
| POST   | `/api/tenants/{tenantId}/chats/{chatId}/members` | `AddChatMemberCommand` |
| DELETE | `/api/tenants/{tenantId}/chats/{chatId}/members/{userId}` | `RemoveChatMemberCommand` |
| POST   | `/api/tenants/{tenantId}/chats/{chatId}/leave` | `LeaveChatCommand` |
| GET    | `/api/tenants/{tenantId}/chats/{chatId}/messages?before=&pageSize=` | `GetChatMessagesQuery` |
| POST   | `/api/tenants/{tenantId}/chats/{chatId}/messages` | `SendMessageCommand` |
| PATCH  | `/api/tenants/{tenantId}/chats/{chatId}/messages/{messageId}` | `EditMessageCommand` |
| DELETE | `/api/tenants/{tenantId}/chats/{chatId}/messages/{messageId}` | `DeleteMessageCommand` |
| PUT    | `/api/tenants/{tenantId}/chats/{chatId}/read` | `MarkAsReadCommand` |

### Direct chats — user-scoped (cross-tenant)

Route prefix: `api/chats/direct`

| HTTP | Endpoint | Mediator |
|---|---|---|
| GET    | `/api/chats/direct` | `GetUserDirectChatsQuery` (filtr `IsGroupChat == false`) |
| POST   | `/api/chats/direct` | `CreateDirectChatCommand` |
| GET    | `/api/chats/direct/by-members?memberIds=` | `FindChatsByMembersQuery` |
| GET    | `/api/chats/direct/{chatId}/messages?before=&pageSize=` | `GetChatMessagesQuery` (wariant direct?) |
| POST   | `/api/chats/direct/{chatId}/messages` | `SendDirectMessageCommand` (lub współdzielony) |
| ... etc dla operacji na direct chats |

### Decyzja o współdzieleniu Commands

Większość operacji message-level (`SendMessage`, `EditMessage`, `DeleteMessage`,
`MarkAsRead`, `GetChatMessages`) jest identyczna dla direct i group. Aby
nie duplikować:
- Te operacje pozostają jako jeden Command/Query.
- Mają **opcjonalny** `TenantId` (nullable Guid).
- Endpoint group przekazuje `TenantId` z route, endpoint direct pomija (null).
- W handlerze: jeśli `TenantId is not null` — predykat filtruje po nim.
  Jeśli null — fallback na membership-only (direct chat).

Operacje group-only (`Rename`, `Delete`, `AddMember`, `RemoveMember`, `Leave`,
`GetMembers`, `GetAvailableMembers`) — tylko w `TenantChatsController`.

### Split kontrolera

- `src/WebApi/Controllers/TenantChatsController.cs` — `[Route("api/tenants/{tenantId}/chats")]` z `[Authorize(Policy = ...)]` per endpoint (z fix-05).
- `src/WebApi/Controllers/DirectChatsController.cs` — `[Route("api/chats/direct")]` z samym `[Authorize]`.

## Predykaty TenantId/ProjectId w handlerach

W każdym handlerze gdzie istnieje `TenantId` w request:
- Predykat `GetFirstBySearch(c => c.Id == request.ChatId && c.TenantId == request.TenantId, ct)` zamiast samego `c.Id == request.ChatId`.
- Predykat `c.ProjectId == request.ProjectId` jeśli jest podany.
- Dla direct chat (TenantId == null) — predykat `c.IsGroupChat == false`.

Ten warunek to defense in depth — chroni przed cross-tenant data leak nawet
jeśli pipeline autoryzacji zostanie ominięty.

## Kontroler — uzupełnianie route params do Command

Wzorzec:
```csharp
[HttpDelete("{chatId}")]
[Authorize(Policy = PermissionCodes.ChatDelete)]
public async Task<IActionResult> DeleteChat(
    [FromRoute] Guid tenantId,
    [FromRoute] Guid chatId)
{
    DeleteChatCommand command = new DeleteChatCommand
    {
        TenantId = tenantId,
        ChatId = chatId
    };
    await Send(command);
    return NoContent();
}
```

## Ograniczenia

- ChatHub.SendMessage / MarkAsRead — sprawdź czy używają mediator.Send i jeśli
  tak, jak przekazują TenantId. Jeśli nie da się sensownie wyciągnąć z hub
  context — zostaw te ścieżki na membership-only (z notatką w komentarzu).
- UI musi się zaktualizować — **w tym fixie poprawiamy też frontend
  client API** (`src/api/chatApi.ts` lub `src/features/chat/services/`).
  Sprawdź gdzie są wywołania `/api/chats/...` i przepisz.

## Zakaz

- Nie zmieniaj kontraktów Web modeli.
- Nie zmieniaj logiki biznesowej handlerów (poza warunkami w predykatach).
- Nie ruszaj walidatorów (poza dodaniem `RequiredId()` na nowych polach `TenantId`).

## Kryterium akceptacji

- `dotnet build` — 0 błędów.
- Build UI: `cd 01-Applications/ProjectDataManagementUI; npm run build` — 0 błędów.
- Stary `ChatController.cs` usunięty (zastąpiony przez `TenantChatsController` + `DirectChatsController`).
- Wszystkie endpointy group/project mają `[Authorize(Policy = ...)]`.
- Handlery filtrują po `TenantId` gdzie podany.

## Raport końcowy

- Status buildu API i UI.
- Lista nowych/usuniętych kontrolerów.
- Lista zmodyfikowanych plików frontend (chat API, hooks, komponenty).
- Lista handlerów zmodyfikowanych z TenantId w predykacie.
