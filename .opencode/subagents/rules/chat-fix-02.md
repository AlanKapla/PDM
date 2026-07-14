# Chat — Fix 02: Przeniesienie web modeli do `Business/Interfaces/WebModels/Chats/`

Cel: web modele Chat mają mieszkać w `Business/Interfaces/WebModels/Chats/`
zgodnie z konwencją reszty solution. Inline DTO z kontrolera wydzielić do
osobnych plików (`Requests/`).

Kontekst: audyt `.opencode/subagents/rules/chat-audit.md`, problemy W1, W8, N13.

## Zakres

### Pliki do przeniesienia z `src/Chat/DTOs/` do `src/Business/Interfaces/WebModels/Chats/`

| Plik źródłowy | Docelowy namespace |
|---|---|
| `ChatWeb.cs` | `Business.Interfaces.WebModels.Chats` |
| `ChatMemberWeb.cs` | `Business.Interfaces.WebModels.Chats` |
| `MessageWeb.cs` | `Business.Interfaces.WebModels.Chats` |
| `AvailableMemberWeb.cs` | `Business.Interfaces.WebModels.Chats` |
| `ChatSearchResultWeb.cs` | `Business.Interfaces.WebModels.Chats` |
| `CreateChatResultWeb.cs` | `Business.Interfaces.WebModels.Chats` |
| `ProjectContactsGroupWeb.cs` | `Business.Interfaces.WebModels.Chats` |
| `ProjectMateWeb.cs` | `Business.Interfaces.WebModels.Chats` |

Po przeniesieniu — usuń pusty katalog `src/Chat/DTOs/`.

### Inline DTOs z `ChatController.cs` → osobne pliki w `Business/Interfaces/WebModels/Chats/Requests/`

| Inline w kontrolerze | Plik docelowy |
|---|---|
| `CreateChatRequest` | `CreateChatRequest.cs` |
| `RenameChatRequest` | `RenameChatRequest.cs` |
| `AddChatMemberRequest` | `AddChatMemberRequest.cs` |
| `SendMessageRequest` | `SendMessageRequest.cs` |
| `EditMessageRequest` | `EditMessageRequest.cs` |

Wszystkie `public sealed record` w namespace `Business.Interfaces.WebModels.Chats.Requests`.
Usuń inline definicje z `ChatController.cs`.

### Payloady SignalR — **NIE ruszamy**

Payloady z `IChatClient.cs` (`MessageEditedPayload` itd.) zostają w pliku huba —
są ściśle związane z kontraktem SignalR, nie z REST API.

### Aktualizacja referencji

Zaktualizuj wszystkie `using` we wszystkich plikach domeny Chat (handlery,
walidatory, kontroler, hub, services) tam gdzie używane są przeniesione typy:
- usunąć `using Chat.DTOs;`
- dodać `using Business.Interfaces.WebModels.Chats;`
- dla request DTO w kontrolerze: `using Business.Interfaces.WebModels.Chats.Requests;`

Sprawdź czy któryś z tych typów nie jest używany poza domeną Chat (np. w UI-shared
contractach, AI agent itd.) — jeśli tak, zaktualizuj również tam.

## Zasady

- Wszystkie web modele i request DTO: `public sealed record` (jeśli już nie są po fix-01).
- Bez zmian w polach / strukturze typów.
- Bez zmian w handlerach poza usingami.

## Zakaz

- Nie zmieniaj zawartości typów (pól, konstruktorów).
- Nie dotykaj payloadów SignalR.
- Nie zmieniaj logiki kontrolera (tylko usunięcie inline records + nowe usingi).

## Kryterium akceptacji

- `dotnet build src/WebApi/WebApi.csproj --nologo` — 0 błędów.
- Katalog `src/Chat/DTOs/` nie istnieje (lub jest pusty i usunięty).
- Wszystkie typy zdefiniowane w `Business.Interfaces.WebModels.Chats(.Requests)?`.
- `grep -r "Chat.DTOs" src/` — brak wyników.

## Raport końcowy

- Status buildu.
- Lista przeniesionych plików (źródło → cel).
- Lista plików gdzie zaktualizowano `using`.
