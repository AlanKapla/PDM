export interface ChatMemberWeb {
  userId: string;
  firstName: string;
  lastName: string;
  joinedAt: string;
  isAdmin: boolean;
  lastReadAt: string | null;
}

export interface MessageWeb {
  id: string;
  chatId: string;
  senderId: string;
  senderFirstName: string;
  senderLastName: string;
  content: string;
  isDeleted: boolean;
  isEdited: boolean;
  sentAt: string;
  editedAt: string | null;
  replyToMessageId: string | null;
}

export interface ChatWeb {
  id: string;
  name: string;
  isGroupChat: boolean;
  projectId: string | null;
  tenantId: string | null;
  createdAt: string;
  createdByUserId: string;
  unreadCount: number;
  lastMessage: MessageWeb | null;
  members: ChatMemberWeb[];
}

// ---- Contact picker types ----

export interface ProjectMateWeb {
  userId: string;
  firstName: string;
  lastName: string;
}

export interface ProjectContactsGroupWeb {
  projectId: string;
  projectName: string;
  tenantId: string;
  tenantName: string;
  members: ProjectMateWeb[];
}

export interface AvailableMemberWeb {
  userId: string;
  firstName: string;
  lastName: string;
}

export interface ChatSearchResultWeb {
  chatId: string;
  chatName: string;
  isGroupChat: boolean;
  projectId: string | null;
  tenantId: string | null;
  matchingMessageIds: string[];
}

// ---- Request types ----

export interface CreateChatRequest {
  projectId?: string | null;
  memberUserIds: string[];
  name?: string | null;
}

export interface CreateChatResultWeb {
  id: string;
  isGroupChat: boolean;
}

export interface SendMessageRequest {
  content: string;
  replyToMessageId?: string | null;
}

export interface EditMessageRequest {
  content: string;
}

export interface RenameChatRequest {
  newName: string;
}

export interface AddMemberRequest {
  userId: string;
  projectId?: string | null;
}

// ---- SignalR payload types ----

export interface MessageEditedPayload {
  messageId: string;
  chatId: string;
  newContent: string;
  editedAt: string;
}

export interface MessageDeletedPayload {
  messageId: string;
  chatId: string;
}

export interface ReadReceiptPayload {
  chatId: string;
  userId: string;
  readAt: string;
}

export interface UserTypingPayload {
  chatId: string;
  userId: string;
  isTyping: boolean;
}

export interface MemberAddedPayload {
  chatId: string;
  member: ChatMemberWeb;
}

export interface RemovedFromChatPayload {
  chatId: string;
  redirectToChatId: string | null;
}

export interface ChatDeletedPayload {
  chatId: string;
}
