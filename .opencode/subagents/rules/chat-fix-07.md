# Chat — Fix 07: Refaktor wydajnościowy zapytań

Cel: zlikwidować problemy O(N) i N+1 w zapytaniach domeny Chat.

Kontekst: audyt `.opencode/subagents/rules/chat-audit.md`, problemy W3, W4, W5, W6, W12.

## Wymagania wstępne — repozytorium

Sprawdź czy `IRepository<T>` udostępnia:
- `Task<List<TResult>> SelectAsync<TResult>(...)` z możliwością `OrderBy`/`Take`/`Skip` w lambdzie projection — najpewniej **nie**.
- `IQueryable<T> AsQueryable()` lub `Query()` — sprawdź.

Jeśli repo nie pozwala na pełne SQL w paginacji kursorowej, dodaj nową
metodę do `IRepository<T>`:

```csharp
Task<List<T>> GetPagedBySearch(
    Expression<Func<T, bool>> predicate,
    Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
    int take,
    CancellationToken cancellationToken,
    Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null);
```

(Jeśli istnieje — użyj. Sprawdź też `GetBySearch` z parametrami `take`/`skip`.)

## Problem 1 — `GetChatMessagesQueryHandler` (W3)

Obecnie: ładuje **wszystkie** wiadomości czatu, sortuje i paginuje w C#.

Refaktor:
```csharp
List<MessageHistory> messages;
if (request.Before is null)
{
    messages = await messageRepo.GetPagedBySearch(
        m => m.ChatId == request.ChatId && m.DeletedAt == null,
        q => q.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id),
        request.PageSize,
        cancellationToken);
}
else
{
    // Cursor: pobierz CreatedAt cursora, paginuj wstecz
    MessageHistory? cursor = await messageRepo.GetFirstBySearch(
        m => m.Id == request.Before.Value,
        cancellationToken);
    if (cursor is null)
    {
        throw new NotFoundApiException(nameof(MessageHistory), request.Before.Value.ToString());
    }
    messages = await messageRepo.GetPagedBySearch(
        m => m.ChatId == request.ChatId
             && m.DeletedAt == null
             && (m.CreatedAt < cursor.CreatedAt
                 || (m.CreatedAt == cursor.CreatedAt && m.Id.CompareTo(cursor.Id) < 0)),
        q => q.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id),
        request.PageSize,
        cancellationToken);
}
```

Mapowanie do `MessageWeb` przez `ChatMapper.MapMessage`.

## Problem 2 — `GetUserChatsQueryHandler` (W4)

Obecnie: ładuje wszystkie wiadomości ze wszystkich czatów usera, w C# liczy
last message i unread.

Refaktor — **dwa zapytania agregujące**:

```csharp
// 1. Last message per chat (subquery / GroupBy w SQL)
List<MessageHistory> lastMessages = await messageRepo
    .GetPagedBySearch(
        m => chatIds.Contains(m.ChatId) && m.DeletedAt == null,
        q => q.GroupBy(m => m.ChatId)
              .Select(g => g.OrderByDescending(m => m.CreatedAt).First()),
        chatIds.Count,
        cancellationToken);
// (Jeśli to nie działa w EF — rozbić na 2 zapytania: GROUP BY ChatId MAX(CreatedAt) + JOIN)
```

Lub alternatywnie (niezawodnie) — dodaj metodę projekcji do repo
`GetLastMessagePerChatAsync(IEnumerable<Guid> chatIds, ct)`.

Unread count per chat:
```csharp
// Per chat: count messages with CreatedAt > member.LastReadAt
Dictionary<Guid, int> unreadByChatId = await messageRepo.SelectGroupedAsync(
    m => chatIds.Contains(m.ChatId)
         && m.DeletedAt == null
         && m.AuthorId != currentUser.Id
         && m.CreatedAt > /* memberLastRead per chatId */,
    m => m.ChatId,
    cancellationToken);
```

Jeśli brak takiego API — użyj raw EF Core przez `AppDbContext`.
Jeśli to wymaga refaktoru repo — zrób ostrożnie i opisz w raporcie.

## Problem 3 — `CreateChatCommandHandler.HandleDirectAsync` (W5)

Obecnie: pobiera wszystkie membership usera + Include(Chat), iteruje.

Refaktor — jedno zapytanie:
```csharp
// Znajdź direct chat z dokładnie dwoma członkami {currentUser, target}
Chat? existing = await chatRepo.GetFirstBySearch(
    c => !c.IsGroupChat
         && c.Members.Any(m => m.UserId == currentUser.Id)
         && c.Members.Any(m => m.UserId == targetUserId)
         && c.Members.Count == 2,
    cancellationToken,
    include => include.Include(c => c.Members));
```

## Problem 4 — `FindChatsByMembersQueryHandler` (W6)

Obecnie: pętla `foreach` z osobnym query per member.

Refaktor — jedno zapytanie:
```csharp
List<Guid> requestedIds = request.MemberUserIds.Distinct().ToList();
int requiredCount = requestedIds.Count;

List<Chat> chats = await chatRepo.GetBySearch(
    c => c.Members.Count(m => requestedIds.Contains(m.UserId)) == requiredCount
         && c.Members.Count == requiredCount,
    cancellationToken,
    include => include.Include(c => c.Members));
```

Mapowanie do `ChatWeb` przez `ChatMapper`.

## Problem 5 — `SearchChatsQueryHandler` (W12)

Obecnie: pobiera wszystkich członków wszystkich moich czatów do pamięci,
filtruje po nazwiskach w C#.

Refaktor — przesuń filtr nazw do SQL przez `JOIN` z `User`:
```csharp
List<Guid> chatIdsByNameMatch = await userRepo.SelectAsync(
    u => (u.FirstName + " " + u.LastName).Contains(request.Phrase)
         && u.ChatMemberships.Any(m => myChatIds.Contains(m.ChatId)),
    u => /* projection: chat ids via membership */,
    cancellationToken);
```

Lub jeśli brak takiej nawigacji — użyj `chatMemberRepo` + `userRepo` z
`Where(u => u.ChatMemberships.Any(...))`.

Zachowaj wynik max N (dodaj `Take(50)`).

## Inne

- Wszędzie gdzie `SelectAsync` lub `GetBySearch` zwraca duże listy — dodaj limity (`Take`).
- Sprawdź czy któryś handler wykonuje `.ToList()` lub `.Distinct()` w C# zamiast SQL.

## Zakaz

- Nie zmieniaj kontraktów Web modeli.
- Nie zmieniaj autoryzacji ani routingu.
- Nie ruszaj logiki SignalR.
- Nie wprowadzaj cache (Redis itd.) — to osobny temat.

## Kryterium akceptacji

- `dotnet build` — 0 błędów.
- `GetChatMessagesQueryHandler` nie wywołuje `GetBySearch` bez `Take`/paginacji.
- `GetUserChatsQueryHandler` nie ładuje wszystkich `MessageHistory` do pamięci
  (sprawdzalne: brak `messageRepo.GetBySearch(m => chatIds.Contains(m.ChatId))` bez agregacji).
- `CreateChatCommandHandler` i `FindChatsByMembersQueryHandler` nie mają już
  pętli z `AnyAsync` per element.

## Raport końcowy

- Status buildu.
- Lista zmodyfikowanych handlerów.
- Lista nowych metod w `IRepository<T>` (jeśli były).
- Krótko: nowe zapytania per problem (które agregaty / joiny).
