# Chat — Fix 04: Hermetyzacja encji + ChatMapper + helpery handlerów

Cel: zlikwidować duplikację mapowań i wprowadzić enkapsulację encji
(metody biznesowe zamiast publicznych setterów).

Kontekst: audyt `.github/subagents/rules/chat-audit.md`, problemy W14, N7, N16.

## Część A — Hermetyzacja encji

### `Chat` (`src/Entities/Models/Chats/Chat.cs`)

- Settery → `private set;` dla wszystkich własności mutowalnych
  (poza `Id` z `BaseEntity`).
- Konstruktor parameterless — zostaw `protected` albo `private` (EF wymaga).
- Dodaj fabryczne metody static:
  - `Chat CreateDirect(Guid userA, Guid userB)`
  - `Chat CreateGroup(string name, Guid? tenantId, Guid? projectId, Guid createdByUserId)`
- Dodaj metody biznesowe:
  - `void Rename(string newName)` — waliduje że to group chat i nie pusty.
  - `void TouchUpdated(DateTime nowUtc)` — bumps `UpdatedAt`.
- Pole `Members` (`ICollection<ChatMember>`) — jeśli istnieje publicznie:
  zamień na `private List<ChatMember> _members = new();` z `IReadOnlyCollection<ChatMember> Members => _members.AsReadOnly();`.

### `ChatMember`

- Settery → `private set;`.
- Konstruktor `private ChatMember()` dla EF + `public ChatMember(Guid chatId, Guid userId, ChatMemberRole role)` (lub odpowiednik).
- Metody:
  - `void MarkRead(DateTime nowUtc)` — ustawia `LastReadAt`.

### `MessageHistory`

- Settery → `private set;`.
- Konstruktor `private` + `public static MessageHistory Create(Guid chatId, Guid authorId, string content, Guid? replyToId)`.
- Metody:
  - `void Edit(string newContent, DateTime nowUtc)` — ustawia `Content`, `EditedAt`.
  - `void SoftDelete(DateTime nowUtc)` — ustawia `DeletedAt`.
- Walidacja długości i pustki w metodach.

### Aktualizacja handlerów

Każde miejsce gdzie handler mutuje encję bezpośrednio (`chat.Name = ...`,
`message.Content = ...`, `member.LastReadAt = ...`) zastąp wywołaniem
metody biznesowej. Ekspozycja `Members` w `CreateChatCommandHandler` —
przejdź na fabrykę `Chat.CreateGroup(...)` która tworzy chat i pustą listę
członków, a członkowie są dodawani osobno przez `chatMemberRepo.Insert(...)`.

## Część B — ChatMapper (DRY mapowania)

Lokalizacja: `src/Chat/Mappers/ChatMapper.cs` (lub `src/Business/Implementation/Mappers/ChatMapper.cs` jeśli wolimy).

```csharp
public static class ChatMapper
{
    public static MessageWeb MapMessage(MessageHistory message, string authorName);

    public static ChatMemberWeb MapMember(ChatMember member, string fullName);

    public static ChatWeb MapChat(
        Chat chat,
        IReadOnlyCollection<ChatMemberWeb> members,
        MessageWeb? lastMessage,
        int unreadCount);
}
```

Zastąp ręczne `new ChatWeb(...)`, `new MessageWeb(...)`, `new ChatMemberWeb(...)`
w handlerach wywołaniami z `ChatMapper`. Zwróć uwagę na warianty wymagające
dodatkowych pól (np. `displayName` dla direct chat) — dopuść opcjonalne
parametry lub osobne metody:

- `MapDirectChat(Chat, ChatMemberWeb other, MessageWeb? lastMessage, int unread)`
- `MapGroupChat(Chat, IReadOnlyCollection<ChatMemberWeb> members, ...)`

## Część C — Helpery `GetAndValidate*Async` w handlerach

W handlerach gdzie powtarzany jest pattern „pobierz + rzuć NotFound":
- `GetAndValidateChatAsync(Guid chatId, CancellationToken ct)`
- `GetAndValidateMembershipAsync(Guid chatId, Guid userId, CancellationToken ct)`
- `GetAndValidateMessageAsync(Guid chatId, Guid messageId, CancellationToken ct)`

Każdy zwraca encję lub rzuca `NotFoundApiException`.

`Handle()` w każdym handlerze zostaje krótkim orkiestratorem (≤ 20 linii):
1. wywołanie helperów `GetAndValidate…`
2. logika biznesowa (delegowana do encji metodami z części A)
3. `chatMapper`/`ChatMapper.MapChat(...)` na końcu
4. enqueue eventów SignalR (z fix-03)

## Zakaz

- Nie ruszaj struktury Commands/Queries.
- Nie zmieniaj kontraktów Web modeli (pól) — tylko mapowanie do nich.
- Nie ruszaj walidatorów.
- Nie zmieniaj routingu / autoryzacji (osobne fixy).
- Nie wprowadzaj AutoMappera / Mapstera — czysty static mapper.

## Kryterium akceptacji

- `dotnet build` — 0 błędów.
- `grep -rn "new ChatWeb(" src/Chat/` — tylko w `ChatMapper`.
- `grep -rn "new MessageWeb(" src/Chat/` — tylko w `ChatMapper`.
- `grep -rn "chat\.Name\s*=" src/Chat/` — brak (zamiast tego `chat.Rename(...)`).
- Każda klasa encji `Chat`, `ChatMember`, `MessageHistory` ma `private set` (lub brak settera) na własnościach domenowych.

## Raport końcowy

- Status buildu.
- Lista nowych plików (`ChatMapper.cs` itd.).
- Lista zmodyfikowanych handlerów + krótki opis (ile linii Handle przed/po).
- Lista zmodyfikowanych encji + krótki opis publicznych metod.
