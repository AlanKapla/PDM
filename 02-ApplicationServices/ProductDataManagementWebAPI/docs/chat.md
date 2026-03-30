# Chat Module — Developer Reference

## Table of Contents

1. [Overview](#1-overview)
2. [Core Concepts](#2-core-concepts)
3. [Authentication](#3-authentication)
4. [Configuration](#4-configuration)
5. [REST API](#5-rest-api)
   - [Conversations](#conversations)
   - [Messages](#messages)
6. [SignalR Hub](#6-signalr-hub)
   - [Connection](#connection)
   - [Client → Server Invocations](#client--server-invocations)
   - [Server → Client Events](#server--client-events)
7. [Business Flows](#7-business-flows)
   - [Creating a Direct Chat](#71-creating-a-direct-chat)
   - [Creating a Group Chat](#72-creating-a-group-chat)
   - [Finding Contacts](#73-finding-contacts)
   - [Finding Available Members to Add](#74-finding-available-members-to-add)
   - [Adding a Member](#75-adding-a-member)
   - [Removing a Member](#76-removing-a-member)
   - [Leaving a Chat](#77-leaving-a-chat)
   - [Deleting a Chat](#78-deleting-a-chat)
   - [Finding Chats by Participants](#79-finding-chats-by-participants)
   - [Searching Chats](#710-searching-chats)
   - [Sending Messages](#711-sending-messages)
   - [Editing a Message](#712-editing-a-message)
   - [Deleting a Message](#713-deleting-a-message)
   - [Cursor Pagination](#714-cursor-pagination)
   - [Mark as Read](#715-mark-as-read)
8. [DTO Reference](#8-dto-reference)
9. [Error Reference](#9-error-reference)

---

## 1. Overview

The Chat module provides real-time messaging between users who share at least one project. It supports two conversation types:

| Type | Members | `isGroupChat` | `projectId` | `tenantId` |
|------|---------|:---:|:---:|:---:|
| **Direct** | exactly 2 | `false` | `null` | `null` |
| **Group** | 3 or more | `true` | non-null | non-null |

The boundary is always 2 members for direct and 3+ for group. The server enforces this automatically — `isGroupChat` is never set manually and recalculates after every membership change.

REST endpoints are served at `/api/chats/**`. Real-time events are delivered via SignalR at `/api/hubs/chat`.

---

## 2. Core Concepts

### Direct Chat
- Created between exactly two users.
- Both users must share at least one project.
- **Idempotent**: calling `POST /api/chats` again with the same single member always returns the existing chat.
- Has no name (the client displays the other member's name), no `projectId`, no `tenantId`.
- Can be converted to a group chat when a third member is added (see [Adding a Member](#75-adding-a-member)).

### Group Chat
- Created with 2 or more target members (3+ total including the creator).
- All members must belong to the specified `projectId`.
- Has an optional name, a `projectId`, and a `tenantId`.
- Can shrink back to a direct chat when members are removed down to 2 (see [Removing a Member](#76-removing-a-member) and [Leaving a Chat](#77-leaving-a-chat)).
- The creator is automatically assigned `isAdmin = true`. First listed member in `memberUserIds` is **not** an admin — only the creator is.
- When the admin leaves, the entire group is **dissolved** (physically deleted — see [Leaving a Chat](#77-leaving-a-chat)).

### Chat Member Roles

| Role | `isAdmin` | Permissions |
|------|:---:|---|
| **Member** | `false` | Send messages; edit/delete own messages; leave chat |
| **Admin** | `true` | Everything above + rename group; add members; remove any non-admin member; dissolve group by leaving |

### Soft-Delete for Messages
Messages are not physically removed. `DeleteMessageCommand` sets `deletedAt`. The `MessageWeb.content` field is returned as an empty string and `isDeleted = true` — clients must render a placeholder (e.g. *"Wiadomość usunięta"*). Only the **author** may delete their own message.

---

## 3. Authentication

All REST endpoints and the SignalR hub require a valid **JWT Bearer** token.

```
Authorization: Bearer <token>
```

The hub reads the token from the `access_token` query-string parameter when the standard `Authorization` header is not available (standard SignalR JS client behaviour):

```
wss://host/api/hubs/chat?access_token=<token>
```

---

## 4. Configuration

Bind the `Chat` section in `appsettings.json`:

```json
"Chat": {
  "MaxMessageEditWindowMinutes": 15,
  "MessagePageSize": 50
}
```

| Key | Default | Description |
|-----|---------|-------------|
| `MaxMessageEditWindowMinutes` | `15` | Minutes after `sentAt` within which the author may edit a message |
| `MessagePageSize` | `50` | Default number of messages returned per page |

---

## 5. REST API

All endpoints require `Authorization: Bearer <token>`.  
All request and response bodies are JSON.

---

### Conversations

#### `GET /api/chats`
Returns all chats the current user is a member of, ordered by last activity descending.

**Response:** `200 OK` — `ChatWeb[]`

---

#### `GET /api/chats/contacts`
Returns users who share at least one project with the current user, grouped by project. Use this to populate the contact picker when starting a new chat.

**Response:** `200 OK` — `ProjectContactsGroupWeb[]`

```json
[
  {
    "projectId": "...",
    "projectName": "Budynek A",
    "tenantId": "...",
    "tenantName": "Acme Corp",
    "members": [
      { "userId": "...", "firstName": "Anna", "lastName": "Kowalska" }
    ]
  }
]
```

> The same user may appear under multiple projects if they share several with the current user.

---

#### `GET /api/chats/search?q={phrase}`
Searches chats the current user belongs to. Matches against:
- Chat name (case-insensitive, in-memory)
- Member full names (case-insensitive, in-memory)
- Message content (SQL `LIKE`, translated from EF Core `Contains`)

**Query parameters:**

| Parameter | Required | Description |
|-----------|:--------:|-------------|
| `q` | ✅ | Search phrase (min 1 character) |

**Response:** `200 OK` — `ChatSearchResultWeb[]`

`matchingMessageIds` contains IDs of messages whose content matched. Empty when the match was on chat name or member name only. The client should highlight these messages when the user opens the chat.

---

#### `POST /api/chats`
Creates a chat. Behaviour differs by the number of members supplied.

**Request body:**
```json
{
  "projectId": "guid | null",
  "memberUserIds": ["guid", "..."],
  "name": "string | null"
}
```

| `memberUserIds.length` | Behaviour |
|---|---|
| `1` | Creates or returns an existing **direct** chat with that user. Both users must share at least one project. `projectId` is ignored. |
| `2+` | Creates a new **group** chat. `projectId` is **required**. All members (including the creator) must belong to the project. |

**Response:** `201 Created` — `CreateChatResultWeb`
```json
{ "id": "guid", "isGroupChat": false }
```

**Errors:**
| Code | Reason |
|------|--------|
| `400` | `memberUserIds` is empty; `projectId` missing for group |
| `403` | Users share no common project (direct); any member not in project (group) |

---

#### `GET /api/chats/by-members?memberIds={guid}&memberIds={guid}`
Returns all chats that contain the current user **and** every listed member. Useful for checking whether a group with a specific set of participants already exists before creating one.

**Query parameters:**

| Parameter | Required | Description |
|-----------|:--------:|-------------|
| `memberIds` | ✅ | One or more user GUIDs (repeat the key) |

**Response:** `200 OK` — `ChatWeb[]`

---

#### `DELETE /api/chats/{chatId}`
Deletes a chat and all of its messages and members.

**Route:** `chatId` — target chat GUID

| Chat type | Who can delete |
|---|---|
| **Group chat** | Admin only |
| **Direct chat** | Any member |

All members (including the caller) receive a `ChatDeleted` SignalR event on their personal `user:{userId}` group before the record is removed.

**Response:** `204 No Content`

**Errors:**
| Code | Reason |
|------|--------|
| `403` | Caller is not a member; caller is not admin (group chat) |
| `404` | Chat not found |

---

#### `PATCH /api/chats/{chatId}`
Renames a group chat. **Admin only.**

**Route:** `chatId` — target chat GUID

**Request body:**
```json
{ "newName": "string" }
```

**Response:** `204 No Content`

**Errors:**
| Code | Reason |
|------|--------|
| `400` | Chat is a direct chat (`isGroupChat = false`) |
| `403` | Caller is not a member, or not an admin |

---

#### `GET /api/chats/{chatId}/members`
Returns all members of a chat. The caller must be a member.

**Response:** `200 OK` — `ChatMemberWeb[]`

---

#### `GET /api/chats/{chatId}/available-members`
Returns project members who are **not yet** in the chat. Only valid for group chats that have an associated `ProjectId`. Use this to populate the *Add Member* picker.

**Guards:**
- Chat must be a group chat (`IsGroupChat = true`)
- Chat must have a non-null `ProjectId`
- Caller must be a member

**Response:** `200 OK` — `AvailableMemberWeb[]`
```json
[{ "userId": "guid", "firstName": "Jan", "lastName": "Kowalski" }]
```

---

#### `POST /api/chats/{chatId}/members`
Adds a member to a chat.

**Request body:**
```json
{
  "userId": "guid",
  "projectId": "guid | null"
}
```

| Scenario | Behaviour |
|---|---|
| **Group chat** | Caller must be **admin**. Target must be a member of `chat.projectId`. New member is added with `isAdmin = false`. SignalR `MemberAdded` broadcast to `chat:{chatId}`. SignalR `ChatCreated` sent to `user:{newMemberId}`. |
| **Direct chat** | `projectId` is **required**. Both the existing member and the new member must belong to the project. The direct chat is **converted to a group** (`isGroupChat`, `projectId`, `tenantId` set). SignalR `ChatCreated` sent to all involved users. |

**Response:** `204 No Content`

**Errors:**
| Code | Reason |
|------|--------|
| `400` | `projectId` missing when adding to direct chat |
| `403` | Caller is not admin (group); target not in project |
| `409` | Target user is already a member |

---

#### `DELETE /api/chats/{chatId}/members/{userId}`
Removes a member from a group chat.

**Route:**
- `chatId` — target chat GUID
- `userId` — user to remove

**Permission matrix:**

| Caller | Target | Allowed? |
|--------|--------|:--------:|
| Admin | Non-admin member | ✅ |
| Non-admin | Any other user | ❌ `403` |
| Admin | Another admin | ❌ `403` |
| Any member | Self | ✅ — prefer `POST /leave` |

If after removal exactly 2 members remain, the group is **converted to a direct chat**. The removed user receives `RemovedFromChat` with `redirectToChatId` pointing to the direct chat.

**Response:** `204 No Content`

---

#### `POST /api/chats/{chatId}/leave`
Current user leaves a chat.

| Caller role | Behaviour |
|---|---|
| **Member** | Removed from the chat. Group → direct conversion may occur (same logic as `DELETE /members/{userId}`). `RemovedFromChat` with optional `redirectToChatId` is sent to the leaving user. |
| **Admin** | The entire group is **dissolved**: `ChatDeleted` is pushed to every member's personal group (`user:{userId}`), then the chat and all its members and messages are **physically deleted** from the database via cascade. |

> An admin cannot leave a group without dissolving it. To transfer ownership, an admin role must first be granted to another member — this is a future feature; currently there is only one admin per group (the creator).

**Response:** `204 No Content`

---

### Messages

#### `GET /api/chats/{chatId}/messages`
Returns cursor-paginated messages for a chat, newest first. Caller must be a member.

**Query parameters:**

| Parameter | Default | Description |
|-----------|---------|-------------|
| `before` | — | Message GUID; returns messages **older** than this cursor |
| `pageSize` | `50` | Number of messages to return (max `100`) |

**Response:** `200 OK` — `MessageWeb[]` (newest → oldest within each page)

> See [Cursor Pagination](#713-cursor-pagination) for the full load-more pattern.

---

#### `POST /api/chats/{chatId}/messages`
Sends a message to a chat. Caller must be a member.

**Request body:**
```json
{
  "content": "string",
  "replyToMessageId": "guid | null"
}
```

**Validation:**
- `content` must not be empty and must not exceed 4 000 characters.

**Response:** `201 Created` — `{ "id": "guid" }`

After persistence, the server broadcasts `ReceiveMessage` to `chat:{chatId}`.

> Also available as hub invocation `SendMessage` — both paths share the same `SendMessageCommand`.

---

#### `PATCH /api/chats/{chatId}/messages/{messageId}`
Edits a message. **Author only**, within the configured edit window (`MaxMessageEditWindowMinutes`).

**Request body:**
```json
{ "content": "string" }
```

**Response:** `204 No Content`

Broadcasts `MessageEdited` to `chat:{chatId}`.

**Errors:**
| Code | Reason |
|------|--------|
| `403` | Edit window has passed |
| `404` | Message not found, already deleted, or caller is not the author |

---

#### `DELETE /api/chats/{chatId}/messages/{messageId}`
Soft-deletes a message. **Author only.** Sets `deletedAt`, clears `content` to `""`.

**Response:** `204 No Content`

Broadcasts `MessageDeleted` to `chat:{chatId}`.

**Errors:**
| Code | Reason |
|------|--------|
| `404` | Message not found, already deleted, or caller is not the author |

---

#### `PUT /api/chats/{chatId}/read`
Marks the chat as fully read for the current user. Updates `ChatMember.lastReadAt`.

**Response:** `204 No Content`

Broadcasts `ReadReceipt` to `chat:{chatId}`.

> Also available as hub invocation `MarkAsRead(chatId)`.

---

## 6. SignalR Hub

**URL:** `/api/hubs/chat`  
**Typed client interface:** `IChatClient`

### Connection

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/api/hubs/chat", { accessTokenFactory: () => getToken() })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

On connect, the server automatically joins the connection to the personal group `user:{userId}`. This group delivers cross-chat notifications (`ChatCreated`, `ChatDeleted`, `RemovedFromChat`) before the client has joined any chat-specific group.

### Joining Chat Groups

To receive real-time events for a specific chat the client **must** explicitly join its group:

```javascript
await connection.invoke("JoinChat", chatId);   // call when opening a conversation
await connection.invoke("LeaveChat", chatId);  // call when navigating away
```

**Recommended pattern:** after `connection.start()`, call `JoinChat` for every chat returned by `GET /api/chats`. Then call `JoinChat` again whenever `ChatCreated` is received.

---

### Client → Server Invocations

| Method | Parameters | Description |
|--------|-----------|-------------|
| `JoinChat` | `chatId: string` | Subscribe to `chat:{chatId}` events. Call when opening a conversation. |
| `LeaveChat` | `chatId: string` | Unsubscribe from `chat:{chatId}`. Call when navigating away. |
| `SendMessage` | `chatId: string, content: string, replyToMessageId?: string` | Send and persist a message. Equivalent to `POST /api/chats/{chatId}/messages`. |
| `MarkAsRead` | `chatId: string` | Mark chat as read. Equivalent to `PUT /api/chats/{chatId}/read`. |
| `StartTyping` | `chatId: string` | Broadcast typing-start to other members in `chat:{chatId}`. |
| `StopTyping` | `chatId: string` | Broadcast typing-stop to other members in `chat:{chatId}`. |

---

### Server → Client Events

| Event | Payload | Delivered to | Trigger |
|-------|---------|-------------|---------|
| `ReceiveMessage` | `MessageWeb` | `chat:{chatId}` | A message was sent |
| `MessageEdited` | `MessageEditedPayload` | `chat:{chatId}` | A message was edited |
| `MessageDeleted` | `MessageDeletedPayload` | `chat:{chatId}` | A message was soft-deleted |
| `UserTyping` | `UserTypingPayload` | `chat:{chatId}` (others only) | A member started/stopped typing |
| `ReadReceipt` | `ReadReceiptPayload` | `chat:{chatId}` | A member marked the chat as read |
| `MemberAdded` | `MemberAddedPayload` | `chat:{chatId}` | A new member was added to a group |
| `ChatCreated` | `ChatWeb` | `user:{userId}` | User added to a new or converted chat |
| `RemovedFromChat` | `RemovedFromChatPayload` | `user:{userId}` | User was removed from a group |
| `ChatDeleted` | `{ chatId: string }` | `user:{userId}` | Admin left — group was dissolved |

### Payload Types

```typescript
interface MessageEditedPayload {
  messageId: string;
  chatId: string;
  newContent: string;
  editedAt: string;            // ISO 8601
}

interface MessageDeletedPayload {
  messageId: string;
  chatId: string;
}

interface UserTypingPayload {
  chatId: string;
  userId: string;
  isTyping: boolean;
}

interface ReadReceiptPayload {
  chatId: string;
  userId: string;
  readAt: string;              // ISO 8601
}

interface MemberAddedPayload {
  chatId: string;
  member: ChatMemberWeb;
}

interface RemovedFromChatPayload {
  chatId: string;
  redirectToChatId: string | null;  // non-null when group shrank to 2 → direct
}
```

---

## 7. Business Flows

### 7.1 Creating a Direct Chat

```
Client                          Server
  |                                |
  |-- POST /api/chats ------------>|  { memberUserIds: [targetUserId] }
  |                                |  1. Verify both users share a project
  |                                |  2. Check if direct chat already exists (idempotent)
  |                                |     → yes: return existing chatId
  |                                |  3. Create Chat (isGroupChat=false, projectId=null, tenantId=null)
  |                                |  4. Insert ChatMember for creator + target
  |                                |  5. SignalR ChatCreated → user:{targetUserId}  (only if new)
  |<-- 201 { id, isGroupChat } ----|
  |
  |--> JoinChat(chatId) ---------->|
```

---

### 7.2 Creating a Group Chat

```
Client                          Server
  |                                |
  |-- POST /api/chats ------------>|  { projectId, memberUserIds: [id1, id2, ...] }
  |                                |  1. Validate projectId is provided
  |                                |  2. Verify all members belong to projectId
  |                                |  3. Create Chat (isGroupChat=true, projectId, tenantId set)
  |                                |  4. Insert ChatMember for creator (isAdmin=true) + all members (isAdmin=false)
  |                                |  5. SignalR ChatCreated → user:{each member}
  |<-- 201 { id, isGroupChat } ----|
```

---

### 7.3 Finding Contacts

Before creating any chat, populate the contact picker:

```
GET /api/chats/contacts
→ ProjectContactsGroupWeb[]
   └─ per project: { projectId, projectName, tenantId, tenantName, members[] }
```

Pass `memberUserIds` with one entry for direct chat, two or more for group.

---

### 7.4 Finding Available Members to Add

When adding someone to an **existing group chat**:

```
GET /api/chats/{chatId}/available-members
→ AvailableMemberWeb[]   (members of chat.projectId not yet in the chat)
```

Use the result to populate the Add Member picker before calling `POST /api/chats/{chatId}/members`.

---

### 7.5 Adding a Member

#### To a group chat

```
POST /api/chats/{chatId}/members  { userId, projectId: null }
```

1. Caller must be **admin**.
2. Target must be a member of `chat.projectId`.
3. `ChatMember` row inserted with `isAdmin = false`.
4. SignalR `MemberAdded` → `chat:{chatId}`.
5. SignalR `ChatCreated` → `user:{newMemberId}` (so the new member receives the full chat object).

#### To a direct chat (converts to group)

```
POST /api/chats/{chatId}/members  { userId, projectId: "required" }
```

1. `projectId` is required.
2. All three users (both existing members + new member) must belong to `projectId`.
3. Chat is updated: `isGroupChat = true`, `projectId`, `tenantId` set.
4. New `ChatMember` inserted with `isAdmin = false`.
5. SignalR `ChatCreated` → `user:{each of the three members}`.

> The original chat ID is preserved after conversion — no redirect needed.

---

### 7.6 Removing a Member

```
DELETE /api/chats/{chatId}/members/{userId}
```

**Permission matrix:**

| Caller | Target | Allowed? |
|--------|--------|:--------:|
| Admin | Non-admin member | ✅ |
| Non-admin | Any other user | ❌ `403` |
| Admin | Another admin | ❌ `403` |
| Any member | Self | ✅ — prefer `POST /leave` |

**Group → Direct conversion** (triggered when exactly 2 members remain after removal):

```
1. Remove target ChatMember
2. Count remaining members → 2
3. EnsureDirectChatAsync(userA, userB)
   a. Find existing direct chat between the two
   b. Create one if not found → SignalR ChatCreated → user:{non-requesting member}
4. SignalR RemovedFromChat → user:{removedUserId}
   { chatId: groupChatId, redirectToChatId: directChatId }
```

---

### 7.7 Leaving a Chat

```
POST /api/chats/{chatId}/leave
```

#### Member leaves

Same flow as [Removing a Member](#76-removing-a-member) (self-removal). Group → direct conversion may occur.

#### Admin leaves — group dissolution

```
1. Collect all member IDs
2. SignalR ChatDeleted → user:{each memberId}
3. Physically delete Chat (cascade deletes ChatMembers and Messages)
```

All clients receiving `ChatDeleted` must remove the chat from local state immediately and navigate away if the chat is currently open.

---

### 7.8 Deleting a Chat

```
DELETE /api/chats/{chatId}
```

| Chat type | Allowed caller |
|---|---|
| Group chat | Admin only |
| Direct chat | Any member |

```
1. Verify caller is a member
2. Group chat: verify caller is admin
3. Collect all member IDs (SelectAsync)
4. SignalR ChatDeleted → user:{each memberId}
5. ExecuteDeleteAsync on Chat
   └─ DB cascade: ChatMembers deleted, MessageHistories deleted
```

All clients receiving `ChatDeleted` must remove the chat from local state immediately. Unlike `POST /leave`, this endpoint is an explicit administrative deletion — the caller is also notified and the chat is gone permanently.

---

### 7.9 Finding Chats by Participants

```
GET /api/chats/by-members?memberIds=id1&memberIds=id2
```

Returns chats where the current user **and all listed members** are present. Use before creating a group to check if one already exists with this exact member set.

---

### 7.9 Searching Chats

```
GET /api/chats/search?q=phrase
```

**Search pipeline:**

```
1. Load all chatIds where current user is a member
2. Check chat name match (OrdinalIgnoreCase, in-memory)
3. Check member full name match (in-memory)
4. Query Messages WHERE content LIKE '%phrase%' AND chatId IN (...) AND deletedAt IS NULL
5. Return ChatSearchResultWeb for every chat that matched on any criterion
```

---

### 7.10 Sending Messages

Two equivalent paths:

| Path | When to use |
|------|-------------|
| `POST /api/chats/{chatId}/messages` | REST polling or non-hub clients |
| Hub invocation `SendMessage(chatId, content, replyToId?)` | Active SignalR connection |

Both dispatch `SendMessageCommand`. `SaveChangesAsync` is called explicitly before the SignalR broadcast so that a client fetching via HTTP immediately after the event finds the message already in the database.

---

### 7.11 Editing a Message

```
PATCH /api/chats/{chatId}/messages/{messageId}   { content: "new text" }
```

- Author only.
- Must be within `MaxMessageEditWindowMinutes` of `sentAt`. Returns `403` if the window has passed.
- Sets `editedAt = DateTime.UtcNow`, updates `content`.
- Broadcasts `MessageEdited` to `chat:{chatId}`.

---

### 7.12 Deleting a Message

```
DELETE /api/chats/{chatId}/messages/{messageId}
```

- **Author only** — `404` is returned if the caller is not the author (author mismatch treated as not found).
- Soft-delete: sets `deletedAt = DateTime.UtcNow`, clears `content` to `""`.
- Broadcasts `MessageDeleted` to `chat:{chatId}`.

---

### 7.13 Cursor Pagination

```
GET /api/chats/{chatId}/messages               → first page (most recent messages)
GET /api/chats/{chatId}/messages?before={id}   → page older than message {id}
GET /api/chats/{chatId}/messages?pageSize=25   → custom page size
```

**Pattern:**
```
Initial load:    GET /messages                → [ msg100, msg99, ... msg51 ]  (newest first)
                 → reverse to render oldest at bottom

Load older:      GET /messages?before=msg51   → [ msg50, msg49, ... msg1 ]
                 → reverse and prepend to top of the list

Stop condition:  result.length < pageSize     → no more pages
```

The `before` cursor is always the `id` of the **oldest message currently rendered**.

---

### 7.14 Mark as Read

Two equivalent paths:

| Path | When to use |
|------|-------------|
| `PUT /api/chats/{chatId}/read` | REST |
| Hub invocation `MarkAsRead(chatId)` | Active SignalR connection |

Both update `ChatMember.lastReadAt = DateTime.UtcNow` and broadcast `ReadReceipt { chatId, userId, readAt }` to `chat:{chatId}`.

`unreadCount` in `ChatWeb` is derived from messages with `sentAt > lastReadAt`.

---

## 8. DTO Reference

### `ChatWeb`

```typescript
interface ChatWeb {
  id: string;
  name: string;               // direct: "FirstName LastName, FirstName LastName" — use as-is
  isGroupChat: boolean;
  projectId: string | null;   // null for direct chats
  tenantId: string | null;    // null for direct chats
  createdAt: string;          // ISO 8601
  createdByUserId: string;
  unreadCount: number;
  lastMessage: MessageWeb | null;
  members: ChatMemberWeb[];
}
```

### `ChatMemberWeb`

```typescript
interface ChatMemberWeb {
  userId: string;
  firstName: string;
  lastName: string;
  joinedAt: string;           // ISO 8601
  isAdmin: boolean;
  lastReadAt: string | null;  // null if member has never marked the chat as read
}
```

### `MessageWeb`

```typescript
interface MessageWeb {
  id: string;
  chatId: string;
  senderId: string;
  senderFirstName: string;
  senderLastName: string;
  content: string;            // empty string when isDeleted = true
  isDeleted: boolean;
  isEdited: boolean;
  sentAt: string;             // ISO 8601
  editedAt: string | null;
  replyToMessageId: string | null;
}
```

### `CreateChatResultWeb`

```typescript
interface CreateChatResultWeb {
  id: string;
  isGroupChat: boolean;
}
```

### `ChatSearchResultWeb`

```typescript
interface ChatSearchResultWeb {
  chatId: string;
  chatName: string;
  isGroupChat: boolean;
  projectId: string | null;
  tenantId: string | null;
  matchingMessageIds: string[];  // empty when match was on name/member name only
}
```

### `ProjectContactsGroupWeb`

```typescript
interface ProjectContactsGroupWeb {
  projectId: string;
  projectName: string;
  tenantId: string;
  tenantName: string;
  members: ProjectMateWeb[];
}
```

### `ProjectMateWeb`

```typescript
interface ProjectMateWeb {
  userId: string;
  firstName: string;
  lastName: string;
}
```

### `AvailableMemberWeb`

```typescript
interface AvailableMemberWeb {
  userId: string;
  firstName: string;
  lastName: string;
}
```

---

## 9. Error Reference

| HTTP | Exception | Common causes |
|------|-----------|---------------|
| `400` | `ValidationApiException` | Missing required fields; invalid format; business rule violation (e.g. renaming a direct chat, `projectId` missing when adding to direct) |
| `401` | `UnauthorizedApiException` | Missing or invalid JWT token |
| `403` | `ForbiddenApiException` | Not a member; not an admin; edit window expired; no shared project; attempting to remove an admin |
| `404` | `NotFoundApiException` | Chat, message or member not found; editing/deleting another user's message |
| `409` | `ConflictApiException` | User already a member |

All errors are formatted as a standard problem details envelope by `ApiExceptionMiddleware`.
