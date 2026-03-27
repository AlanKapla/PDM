# Chat Module — UI Developer Reference

## Overview

The chat system is a standalone module **above the tenant/project layer**.  
A chat conversation simply connects users by their `userId` — it carries no tenant or project context.

Two types of conversations exist:

| Type | `isGroupChat` | Description |
|------|--------------|-------------|
| Direct | `false` | 1-to-1 between two users. Idempotent — creating it twice returns the same chat. |
| Group | `true` | Multiple users. Has an admin role. Supports rename, add/remove members. |

**Authorization:** every endpoint requires a valid bearer token (`[Authorize]`).

---

## Data Models

### `ChatWeb`
Returned by `GET /api/chats` and pushed via SignalR `ChatCreated`.

```json
{
  "id": "guid",
  "name": "string",
  "isGroupChat": true,
  "createdAt": "2025-01-01T00:00:00Z",
  "createdByUserId": "guid",
  "unreadCount": 3,
  "lastMessage": MessageWeb | null,
  "members": [ ChatMemberWeb ]
}
```

> **Direct chat name:** auto-generated as `"FirstName LastName, FirstName LastName"` (initiator, target).  
> **Group chat name:** provided by the creator.

---

### `ChatMemberWeb`

```json
{
  "userId": "guid",
  "firstName": "string",
  "lastName": "string",
  "joinedAt": "2025-01-01T00:00:00Z",
  "isAdmin": false,
  "lastReadAt": "2025-01-01T00:00:00Z" | null
}
```

> `lastReadAt` is `null` if the member has never marked the chat as read.  
> The **creator of a group chat** has `isAdmin: true`. Direct chat members are never admins.

---

### `MessageWeb`

```json
{
  "id": "guid",
  "chatId": "guid",
  "senderId": "guid",
  "senderFirstName": "string",
  "senderLastName": "string",
  "content": "string",
  "isDeleted": false,
  "isEdited": false,
  "sentAt": "2025-01-01T00:00:00Z",
  "editedAt": "2025-01-01T00:00:00Z" | null,
  "replyToMessageId": "guid" | null
}
```

> When `isDeleted` is `true`, `content` is an **empty string**. Render a placeholder (e.g. *"This message was deleted"*).

---

## REST Endpoints

All routes require `Authorization: Bearer <token>`.

---

### Conversations

#### `GET /api/chats`
Returns all chats the current user belongs to, ordered by last activity (descending).

**Response `200`:** `ChatWeb[]`

---

#### `POST /api/chats/direct`
Creates a direct 1-to-1 chat. **Idempotent** — if a direct chat already exists between the two users, the existing `chatId` is returned without creating a duplicate.

**Business rules:**
- `targetUserId` cannot be the current user's own ID (`400`).
- Both users must share at least one project — otherwise `403`.

**Request body:**
```json
{ "targetUserId": "guid" }
```

**Response `201`:** `{ "id": "guid" }`  
**SignalR side-effect:** `ChatCreated` is pushed to the **target user's** personal group (`user:{targetUserId}`).

---

#### `POST /api/chats/group`
Creates a group chat.

**Business rules:**
- `name` is required and must not be empty.
- `memberUserIds` must contain at least one entry (other than the creator).
- All members (including the creator) must share at least one common project — otherwise `403`.
- The creator is automatically added as **admin** (`isAdmin: true`).
- All other members are added as non-admins.

**Request body:**
```json
{
  "name": "string",
  "memberUserIds": [ "guid", "guid" ]
}
```

**Response `201`:** `{ "id": "guid" }`  
**SignalR side-effect:** `ChatCreated` is pushed to each invited member's personal group.

---

#### `PATCH /api/chats/{chatId}`
Renames a group chat.

**Business rules:**
- Only works on group chats (`isGroupChat: true`) — otherwise `400`.
- Requester must be a member — otherwise `403`.
- Requester must be an **admin** — otherwise `403`.

**Request body:**
```json
{ "newName": "string" }
```

**Response `204`**

---

### Members

#### `GET /api/chats/{chatId}/members`
Returns all members of a chat with their names and admin status.

**Business rules:**
- Requester must be a member — otherwise `403`.

**Response `200`:** `ChatMemberWeb[]`

---

#### `POST /api/chats/{chatId}/members`
Adds a new member to a group chat.

**Business rules:**
- Only group chats — otherwise `400`.
- Requester must be a member — otherwise `403`.
- Requester must be an **admin** — otherwise `403`.
- Target user must not already be a member — otherwise `409`.
- Target user must share at least one project with the admin — otherwise `403`.
- New member is added with `isAdmin: false`.

**Request body:**
```json
{ "userId": "guid" }
```

**Response `204`**  
**SignalR side-effects:**
- `MemberAdded` is broadcast to all current members of the chat group (`chat:{chatId}`).
- `ChatCreated` is pushed to the **new member's** personal group so they receive the full conversation object.

---

#### `DELETE /api/chats/{chatId}/members/{userId}`
Removes a member from a group chat.

**Business rules:**
- Only group chats — otherwise `400`.
- Requester must be a member — otherwise `403`.
- **Self-removal:** any member can leave voluntarily (pass own `userId`).
- **Admin removal of another:** admin can remove non-admins only.
- An admin **cannot** be forcibly removed by another admin — they must leave voluntarily.

**Response `204`**  
**SignalR side-effect:** `RemovedFromChat` is pushed to the **removed user's** personal group.

---

### Messages

#### `GET /api/chats/{chatId}/messages?pageSize=50&before={messageId}`
Returns a page of messages ordered **newest → oldest**.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `before` | `guid?` | — | Id of the oldest message the client already holds. Omit to load the most recent page. |
| `pageSize` | `int` | `50` | Clamped to `[1, 100]`. |

**Business rules:**
- Requester must be a member — otherwise `403`.
- Deleted messages are included but `content` is empty and `isDeleted` is `true`.
- When the response contains fewer items than `pageSize`, there are no more older messages.

**Response `200`:** `MessageWeb[]` (newest first)

**Load-more pattern:**
```
First page:  GET /api/chats/{chatId}/messages?pageSize=50
Next page:   GET /api/chats/{chatId}/messages?pageSize=50&before={oldest.id}
             where oldest.id is messages[messages.length - 1].id
```

---

#### `POST /api/chats/{chatId}/messages`
Sends a message.

**Business rules:**
- Requester must be a member — otherwise `403`.
- `content` must not be empty, max 4 000 characters.
- `replyToMessageId` is optional.

**Request body:**
```json
{
  "content": "string",
  "replyToMessageId": "guid" | null
}
```

**Response `201`:** `{ "id": "guid" }`  
**SignalR side-effect:** `ReceiveMessage` is broadcast to all members of the chat group (`chat:{chatId}`).

> Sending via SignalR hub (`SendMessage`) is also supported and produces the same broadcast.

---

#### `PATCH /api/chats/{chatId}/messages/{messageId}`
Edits a message.

**Business rules:**
- Requester must be the **author** of the message — otherwise `404` (author mismatch treated as not found).
- Message must not be deleted — otherwise `404`.
- Edit window: **15 minutes** from `sentAt` — otherwise `400`.

**Request body:**
```json
{ "content": "string" }
```

**Response `204`**  
**SignalR side-effect:** `MessageEdited` is broadcast to the chat group.

---

#### `DELETE /api/chats/{chatId}/messages/{messageId}`
Soft-deletes a message (sets `deletedAt`, content becomes empty).

**Business rules:**
- Requester must be the **author** — otherwise `404`.
- Message must not already be deleted — otherwise `404`.

**Response `204`**  
**SignalR side-effect:** `MessageDeleted` is broadcast to the chat group.

---

#### `PUT /api/chats/{chatId}/read`
Marks the chat as fully read for the current user. Updates `lastReadAt` on the membership record.

**Response `204`**  
**SignalR side-effect:** `ReadReceipt` is broadcast to the chat group.

> Also available as a SignalR hub invocation: `MarkAsRead(chatId)`.

---

## SignalR Hub

**URL:** `wss://<host>/api/hubs/chat`  
**Auth:** pass the bearer token via query string or header (standard SignalR negotiation).

### Connection lifecycle

```
connect  →  server auto-joins connection to  user:{currentUserId}
```

The personal group `user:{userId}` receives cross-chat notifications  
(new chat created, removed from chat) without requiring an explicit join.

> **Subscription strategy:** `JoinChat` / `LeaveChat` is explicit and managed by the frontend.  
> Join a chat group when the user opens it, leave when they navigate away.  
> There is no auto-subscription on connect — keep only the groups you currently need.

---

### Client → Server methods

Call these from the client to join/leave chat rooms or send real-time actions.

| Method | Parameters | Description |
|--------|-----------|-------------|
| `JoinChat` | `chatId: Guid` | Subscribes the connection to chat-level events for this chat. **Call this after loading a chat.** |
| `LeaveChat` | `chatId: Guid` | Unsubscribes from chat-level events. Call when navigating away. |
| `SendMessage` | `chatId: Guid, content: string, replyToMessageId?: Guid` | Sends and persists a message. Equivalent to `POST /api/chats/{chatId}/messages`. |
| `MarkAsRead` | `chatId: Guid` | Marks the chat as read. Equivalent to `PUT /api/chats/{chatId}/read`. |
| `StartTyping` | `chatId: Guid` | Broadcasts a typing-start indicator to other members. |
| `StopTyping` | `chatId: Guid` | Broadcasts a typing-stop indicator to other members. |

---

### Server → Client events

The server pushes these events to connected clients. Subscribe in the SignalR client after connecting.

| Event | Payload | Delivered to | Trigger |
|-------|---------|-------------|---------|
| `ChatCreated` | `ChatWeb` | `user:{userId}` | User was added to a new direct or group chat |
| `ReceiveMessage` | `MessageWeb` | `chat:{chatId}` | A message was sent |
| `MessageEdited` | `MessageEditedPayload` | `chat:{chatId}` | A message was edited |
| `MessageDeleted` | `MessageDeletedPayload` | `chat:{chatId}` | A message was soft-deleted |
| `ReadReceipt` | `ReadReceiptPayload` | `chat:{chatId}` | A member marked the chat as read |
| `UserTyping` | `UserTypingPayload` | `chat:{chatId}` (others only) | A member is typing or stopped typing |
| `MemberAdded` | `MemberAddedPayload` | `chat:{chatId}` | A new member was added to the group |
| `RemovedFromChat` | `RemovedFromChatPayload` | `user:{userId}` | The user was removed from a group chat |

---

### Payload shapes

```ts
// ChatCreated
ChatWeb  // full conversation object (see Data Models)

// ReceiveMessage
MessageWeb  // full message object (see Data Models)

// MessageEdited
{ messageId: Guid, chatId: Guid, newContent: string, editedAt: string }

// MessageDeleted
{ messageId: Guid, chatId: Guid }

// ReadReceipt
{ chatId: Guid, userId: Guid, readAt: string }

// UserTyping
{ chatId: Guid, userId: Guid, isTyping: boolean }

// MemberAdded
{ chatId: Guid, member: ChatMemberWeb }

// RemovedFromChat
{ chatId: Guid }
```

---

## Typical UI Flows

### Opening the chat list

```
1. GET /api/chats
   → render list ordered by lastMessage.sentAt (already sorted by server)
   → unreadCount badge per chat
2. hub.invoke("JoinChat", chatId) for each visible chat (or lazily on open)
```

### Opening a conversation

```
1. GET /api/chats/{chatId}/messages?pageSize=50
   → render messages bottom-up (newest last visually)
   → store messages[last].id as the cursor
2. PUT  /api/chats/{chatId}/read
3. hub.invoke("JoinChat", chatId)  ← if not already joined
```

### Loading older messages (infinite scroll upward)

```
User scrolls to top →
  GET /api/chats/{chatId}/messages?pageSize=50&before={cursor}
  → prepend to message list
  → update cursor = newMessages[last].id
  → if newMessages.length < pageSize → no more history, hide loader
```

### Sending a message

```
Option A (REST):  POST /api/chats/{chatId}/messages
Option B (hub):   hub.invoke("SendMessage", chatId, content, replyToMessageId)

Both produce → ReceiveMessage to all members in chat:{chatId}
The sender receives their own message via the broadcast.
```

### Typing indicator

```
User starts typing → hub.invoke("StartTyping", chatId)
User stops / sends  → hub.invoke("StopTyping", chatId)

Other members receive → UserTyping { isTyping: true/false }
```

### Receiving a new chat while idle (e.g. another user starts a DM)

```
Server pushes → ChatCreated to user:{currentUserId}
UI adds conversation to the list without polling.
hub.invoke("JoinChat", newChat.id)  ← subscribe to messages
```

### Being removed from a group chat

```
Server pushes → RemovedFromChat { chatId } to user:{currentUserId}
UI removes the conversation from the list and closes the view if open.
hub.invoke("LeaveChat", chatId)  ← clean up the subscription
```

---

## Error Reference

| HTTP | Meaning |
|------|---------|
| `400` | Validation error (e.g. empty content, past edit window, renaming a DM) |
| `401` | Missing or invalid bearer token |
| `403` | Not a member, not an admin, no shared project |
| `404` | Chat or message not found (also returned when editing/deleting another user's message) |
| `409` | User is already a member of the chat |
