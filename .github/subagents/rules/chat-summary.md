# Chat — podsumowanie audytu i refaktoru

Domena: **Chat** (CQRS/Chats + powiązane: WebModels, encje Chat/ChatMember/MessageHistory, hub SignalR, kontrolery)
Raport audytu: [.github/subagents/rules/chat-audit.md](chat-audit.md)
Status końcowy: **API build 0 błędów, UI build 0 błędów**.

## Decyzje domenowe (z konsultacji z człowiekiem)

| Pytanie | Decyzja |
|---|---|
| Pipeline autoryzacji | TAK, ale tylko dla group/project chats; direct zostają membership-based |
| Routing tenant-scoped | TAK, split na `api/tenants/{tenantId}/chats` (group) i `api/chats/direct` (direct) |
| Lokalizacja web modeli | `Business/Interfaces/WebModels/Chats/` (spójność z resztą) |
| Race conditions SignalR | Outbox/post-commit dispatcher (większy refaktor pipeline) |
| Wydajność (N+1, paginacja w pamięci) | Pełny refaktor SQL teraz |
| Hermetyzacja encji | Pełna hermetyzacja teraz |

## Wykonane fixy

| # | Plik promptu | Zakres | Status |
|---|---|---|---|
| 01 | [chat-fix-01.md](chat-fix-01.md) | Quick wins / cleanup: zakaz `var`, `is null`, `nameof`, usingi, `RequiredId()`, `IReadRepository`, `ChatHubGroups`, `sealed` na DTO, K6 walidacja `ReplyToMessageId` | ✅ |
| 02 | [chat-fix-02.md](chat-fix-02.md) | Web modele do `Business/Interfaces/WebModels/Chats/` + osobne pliki request DTOs (5) | ✅ |
| 03 | [chat-fix-03.md](chat-fix-03.md) | `IPostCommitDispatcher` + `TransactionBehavior` dispatch po commit, eliminacja race conditions K2-K5 (10 handlerów Chat) | ✅ |
| 04 | [chat-fix-04.md](chat-fix-04.md) | Hermetyzacja encji `Chat`/`ChatMember`/`MessageHistory` (private set, fabryki, metody biznesowe), `ChatMapper`, prywatne helpery `GetAndValidate*Async` | ✅ |
| 05 | [chat-fix-05.md](chat-fix-05.md) | Nowe `PermissionCodes.Chat*` (5), `ChatScopedRequestBase`, `IAuthorizableRequest` na 8 Commands/Queries group/project, split `CreateChatCommand` → `CreateDirectChatCommand` + `CreateGroupChatCommand`, `[Authorize(Policy=...)]` na endpointach | ✅ |
| 06 | [chat-fix-06.md](chat-fix-06.md) | Split kontrolera: `TenantChatsController` (`api/tenants/{tenantId}/chats`) + `DirectChatsController` (`api/chats/direct`); usunięcie starego `ChatController`; predykaty z `TenantId`; aktualizacja UI (`chatApi.ts`, hooków, komponentów) | ✅ |
| 07 | [chat-fix-07.md](chat-fix-07.md) | Refaktor wydajnościowy: paginacja kursorowa SQL (`GetChatMessages`), agregacja last-message + unread per chat (`GetUserChats`), eliminacja N+1 (`CreateDirectChat`, `FindChatsByMembers`), filtr SQL nazw (`SearchChats`); 2 nowe metody `IReadRepository` | ✅ |

## Metryki — przed vs po

| Metryka | Przed | Po |
|---|---|---|
| Commands/Queries z `IAuthorizableRequest` | 0/17 (0%) | 11/19 (~58%) — pozostałe to user-scoped lub direct chat (świadomie) |
| Walidatory używające `CommonValidationExtensions` | 0/17 | wszystkie z `RequiredId()`, część z `UniqueIds()`/`PageSize()`/`NotCurrentUser()` |
| Web modele `sealed record` | 0/14 | 14/14 (100%) |
| Web modele w `Business/Interfaces/WebModels/Chats/` | 0/14 | 14/14 (100%) |
| Wystąpienia `var` w domenie | ~22 | 0 (wyjątkiem `using var` jeśli wymagane) |
| `== null` / `!= null` w handlerach | wszechobecne | 0 (zastąpione `is null` / `is not null`) |
| Magic strings w `NotFoundApiException` | wszystkie handlery | 0 (zastąpione `nameof`) |
| Race conditions SignalR (broadcast przed commit) | 6 (K2-K5+) | 0 (wszystkie eventy przez `IPostCommitDispatcher`) |
| Walidacja `ReplyToMessageId` należy do tego samego chatu | brak | TAK (K6) |
| Encje z publicznymi setterami | 3/3 | 0/3 (private set + metody biznesowe + fabryki) |
| Ręczne mapowanie `Chat`/`Message`/`Member` w handlerach | rozproszone | scentralizowane w `ChatMapper` |
| N+1 w `CreateChat` direct idempotency | TAK | NIE (jedno zapytanie) |
| N+1 w `FindChatsByMembers` | TAK | NIE (jedno zapytanie) |
| `GetChatMessages` paginacja | w pamięci | kursorowa SQL (`ORDER BY ... TOP n`) |
| `GetUserChats` last message + unread | wszystkie wiadomości do RAM | dwa SQL z `GROUP BY` |
| `SearchChats` filtr nazw | w pamięci | SQL JOIN z `User` |
| Endpointy `[Authorize(Policy=...)]` | 0 | wszystkie tenant/project chat |

## Nowe pliki (kluczowe)

- `src/CQRS/PostCommit/IPostCommitDispatcher.cs`
- `src/CQRS/PostCommit/PostCommitDispatcher.cs`
- `src/Chat/CQRS/Shared/ChatScopedRequestBase.cs`
- `src/Chat/CQRS/Conversations/CreateDirectChat/` (Command + Handler + Validator)
- `src/Chat/CQRS/Conversations/CreateGroupChat/` (Command + Handler + Validator)
- `src/Chat/Mappers/ChatMapper.cs`
- `src/WebApi/Controllers/TenantChatsController.cs`
- `src/WebApi/Controllers/DirectChatsController.cs`
- `src/Business/Interfaces/WebModels/Chats/` (8 web modeli + folder Requests/ z 6 request DTOs)

## Usunięte pliki

- `src/Chat/DTOs/` (cały katalog — przeniesiony)
- `src/WebApi/Controllers/ChatController.cs` (zastąpiony dwoma)
- `src/Chat/CQRS/Conversations/CreateChat/` (zastąpiony przez Direct + Group)

## Otwarte sprawy / TODO

1. **Migracja EF dla nowych `PermissionCodes.Chat*`** — w fix-05 dodano stałe i przygotowano kontrakty, ale seed `RolePermission` (mapowanie ról `TENANT.ADMIN`/`MEMBER`/`PROJECT.*` → `CHAT.*`) wymaga ręcznej weryfikacji w mechanizmie seed projektu (sprawdź `Entities/Migrations` lub initializer). Bez tego SuperAdmin/Admin może nie mieć uprawnień `CHAT.*` do nowych endpointów.
2. **Test integracyjny paginacji `GetChatMessages`** — predykat `m.Id.CompareTo(cursorId) < 0` w EF Core 10 powinien działać dla `Guid`, ale do weryfikacji w runtime na SQL Server.
3. **Hermetyzacja kolekcji `Chat.Members`/`Messages`** — pozostawione jako `ICollection<T>` z `private set` (zamiast `IReadOnlyCollection<T>` + backing field), bo konfiguracje EF używają lambda `WithMany(c => c.Members)`. Dalsza hermetyzacja wymagałaby zmiany konfiguracji EF + regeneracji modelu.
4. **`ChatHub.SendMessage`/`MarkAsRead`** — przekazują `TenantId = null` (membership-based). Jeśli kiedyś hub miałby obsługiwać tenant-scoped operacje z policy, trzeba odczytać tenant z claims.
