# Chat — Fix 05: Pipeline autoryzacji dla group/project chats

Cel: dodać pipeline autoryzacji (`IAuthorizableRequest` + `[Authorize(Policy)]`)
**tylko** dla operacji na group/project chats. Direct chats zostają na
membership-based authorization (świadoma decyzja domenowa — direct chats
są cross-tenant, brak wspólnego scope autoryzacji).

Kontekst: audyt `.opencode/subagents/rules/chat-audit.md`, problem K1, W7.

## Część A — Nowe `PermissionCodes`

Plik: `src/Business/Interfaces/Constants/PermissionCodes.cs`.

Dodaj nowe stałe (na końcu klasy, przed `All`):

```csharp
// CHAT – GROUP/PROJECT
public const string ChatRead = "CHAT.READ";
public const string ChatWrite = "CHAT.WRITE";
public const string ChatMembersManage = "CHAT.MEMBERS.MANAGE";
public const string ChatRename = "CHAT.RENAME";
public const string ChatDelete = "CHAT.DELETE";
```

Dodaj te stałe do tablicy `All`.

## Część B — Mapowanie permissions na role

Sprawdź jak inne moduły dodają mapowanie permissions → roles (typowo seed
w EF lub plik DbInitializer). Dodaj:
- `TENANT.ADMIN` — wszystkie `CHAT.*`
- `TENANT.MEMBER` — `CHAT.READ`, `CHAT.WRITE`, `CHAT.MEMBERS.MANAGE`, `CHAT.RENAME`
- `PROJECT.ADMIN` — wszystkie `CHAT.*` w scope projektu
- `PROJECT.EDITOR` — `CHAT.READ`, `CHAT.WRITE`, `CHAT.MEMBERS.MANAGE`, `CHAT.RENAME`
- `PROJECT.VIEWER` — `CHAT.READ`

Sprawdź istniejący wzorzec w `Roles` / `RolePermission` seed.

## Część C — `IAuthorizableRequest` w Commands/Queries (group/project only)

Wymagane: dodaj `IAuthorizableRequest` do następujących, zakładając że
**wszystkie te operacje wymagają znajomości `TenantId` i opcjonalnie `ProjectId`**.
Po fix-06 pojawią się one w sygnaturach. **W tym fixie** dodaj nowe pola
i implementację.

| Command/Query | PermissionCode | Resource |
|---|---|---|
| `RenameGroupChatCommand` | `ChatRename` | `(TenantId, ProjectId?)` |
| `DeleteChatCommand` | `ChatDelete` | `(TenantId, ProjectId?)` |
| `AddChatMemberCommand` | `ChatMembersManage` | `(TenantId, ProjectId?)` |
| `RemoveChatMemberCommand` | `ChatMembersManage` | `(TenantId, ProjectId?)` |
| `LeaveChatCommand` | `ChatRead` | `(TenantId, ProjectId?)` (lub bez polityki — to membership) |
| `GetChatMembersQuery` | `ChatRead` | `(TenantId, ProjectId?)` |
| `GetAvailableMembersQuery` | `ChatMembersManage` | `(TenantId, ProjectId?)` |
| `SendMessageCommand` | `ChatWrite` | `(TenantId, ProjectId?)` |
| `EditMessageCommand` | `ChatWrite` | `(TenantId, ProjectId?)` |
| `DeleteMessageCommand` | `ChatWrite` | `(TenantId, ProjectId?)` |
| `GetChatMessagesQuery` | `ChatRead` | `(TenantId, ProjectId?)` |
| `MarkAsReadCommand` | `ChatRead` | `(TenantId, ProjectId?)` |
| `SearchChatsQuery` | `ChatRead` | `(TenantId)` |
| `GetUserChatsQuery` | brak (user-scoped, pokazuje tylko czaty usera) |
| `GetProjectMatesQuery` | `ChatMembersManage` (tenant-level) | `(TenantId)` |
| `FindChatsByMembersQuery` | brak (user-scoped) |
| `CreateChatCommand` | różnie (patrz niżej) |

### Specjalne: `CreateChatCommand`

Operacja jest hybrydowa:
- direct chat (bez `ProjectId`, bez `TenantId`) — **nie** wymaga uprawnień, membership-based.
- group/project chat (z `TenantId` lub `ProjectId`) — wymaga `ChatWrite`.

Rozwiązanie: rozdziel na **dwa Commands**:
- `CreateDirectChatCommand` — bez `IAuthorizableRequest`, bez `TenantId`/`ProjectId`.
- `CreateGroupChatCommand` — z `IAuthorizableRequest`, z `TenantId` i opcjonalnym `ProjectId`.

Każdy ma swój handler, walidator, route w kontrolerze (po fix-06).

### Wzorzec sealed record z IAuthorizableRequest

```csharp
public sealed record DeleteChatCommand : IRequestCommand<Unit>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public Guid? ProjectId { get; init; }
    public required Guid ChatId { get; init; }

    public string PermissionCode => PermissionCodes.ChatDelete;
    public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
}
```

Uwaga: pola `TenantId`, `ProjectId` muszą zostać uzupełnione przez kontroler
z route (po fix-06). W tym fixie **dodaj te pola** ze sensownymi defaultami
lub `required` — jeśli `required` to dodaj też przekazywanie z kontrolera
(`command = command with { TenantId = tenantId, ChatId = chatId }`).

### Klasa bazowa

Wprowadź:
```csharp
namespace Chat.CQRS.Shared;

public abstract record ChatScopedRequestBase : IAuthorizableRequest
{
    public Guid TenantId { get; init; }
    public Guid? ProjectId { get; init; }
    public Guid ChatId { get; init; }

    public abstract string PermissionCode { get; }
    public virtual ResourceRef GetResource() =>
        new(TenantId: TenantId, ProjectId: ProjectId);
}
```

Stosuj dla wszystkich Commands/Queries chat-scoped.

## Część D — `[Authorize(Policy = ...)]` na endpointach kontrolera

Po fix-06 (split kontrolera) doda się policy per endpoint. **W tym fixie**:
- na ChatController dodaj `[Authorize]` na klasie (już jest).
- Dla każdego endpointa group/project dodaj `[Authorize(Policy = PermissionCodes.ChatXxx)]`.
- Endpointy direct (`CreateDirectChat`, `GetUserChats`, `SearchChats` jeśli user-scoped) — bez polityki.

## Część E — Handlery

W handlerach group/project chat usuń ręczne sprawdzanie membership tam,
gdzie pipeline `AuthorizationBehavior` już to pokrywa **na poziomie roli**.
**Pozostaw** sprawdzanie membership w czacie jako dodatkowe (defense in depth):
posiadanie `ChatRead` na poziomie tenanta nie znaczy że masz dostęp do
konkretnego czatu — musisz być jego członkiem. To jest naturalna granica
domenowa Chat.

## Zakaz

- Nie zmieniaj routingu (osobny fix-06).
- Nie ruszaj direct-chat handlerów (zostają membership-based).
- Nie usuwaj sprawdzania membership w handlerach.
- Nie zmieniaj UI / frontend constants — tylko backend.

## Kryterium akceptacji

- `dotnet build` — 0 błędów.
- `PermissionCodes.All` zawiera nowe `CHAT.*`.
- Każdy Command/Query group/project ma `IAuthorizableRequest` + `PermissionCode`.
- Nowe `CreateDirectChatCommand` i `CreateGroupChatCommand` istnieją (split).
- Mapowanie ról → uprawnień zaktualizowane (seed/migration).

## Raport końcowy

- Status buildu.
- Lista nowych Commands/Queries (split CreateChat).
- Czy potrzebna była nowa migracja EF (dla seedu permissions).
- Lista handlerów zmodyfikowanych.
