import { axiosClient } from "./axiosClient";
import type {
  ChatWeb,
  ChatMemberWeb,
  MessageWeb,
  CreateChatRequest,
  CreateChatResultWeb,
  SendMessageRequest,
  EditMessageRequest,
  RenameChatRequest,
  AddMemberRequest,
  ProjectContactsGroupWeb,
  AvailableMemberWeb,
  ChatSearchResultWeb,
} from "../types/chat.types";

// ─────────────────────────────────────────────────────────────────────────
// URL helpers — group/project chats live under a tenant route, direct chats
// are cross-tenant. For chat-scoped operations (messages, leave, mark read)
// pass `chat.tenantId` (null for direct chats).
// ─────────────────────────────────────────────────────────────────────────
const tenantBase = (tenantId: string): string => `/tenants/${tenantId}/chats`;
const directBase = "/chats/direct";

const chatBase = (tenantId: string | null): string =>
  tenantId ? tenantBase(tenantId) : directBase;

export const chatApi = {
  /**
   * Returns the current user's tenant chats AND direct chats.
   * If `tenantId` is null, only direct chats are returned.
   */
  getChats: async (tenantId: string | null): Promise<ChatWeb[]> => {
    const requests: Promise<ChatWeb[]>[] = [];
    if (tenantId) {
      requests.push(
        axiosClient.get<ChatWeb[]>(tenantBase(tenantId)).then((r) => r.data)
      );
    }
    requests.push(
      axiosClient.get<ChatWeb[]>(directBase).then((r) => r.data)
    );
    const results = await Promise.all(requests);
    return results.flat();
  },

  getContacts: async (tenantId: string): Promise<ProjectContactsGroupWeb[]> => {
    const response = await axiosClient.get<ProjectContactsGroupWeb[]>(
      `${tenantBase(tenantId)}/contacts`
    );
    return response.data;
  },

  searchChats: async (
    tenantId: string,
    q: string
  ): Promise<ChatSearchResultWeb[]> => {
    const response = await axiosClient.get<ChatSearchResultWeb[]>(
      `${tenantBase(tenantId)}/search`,
      { params: { q } }
    );
    return response.data;
  },

  getChatsByMembers: async (memberIds: string[]): Promise<ChatWeb[]> => {
    const response = await axiosClient.get<ChatWeb[]>(`${directBase}/by-members`, {
      params: { memberIds },
    });
    return response.data;
  },

  /** Create a 1-1 direct chat (cross-tenant). */
  createDirectChat: async (targetUserId: string): Promise<CreateChatResultWeb> => {
    const response = await axiosClient.post<CreateChatResultWeb>(directBase, {
      targetUserId,
    });
    return response.data;
  },

  /** Create a group chat bound to a project within the given tenant. */
  createGroupChat: async (
    tenantId: string,
    data: CreateChatRequest
  ): Promise<CreateChatResultWeb> => {
    const response = await axiosClient.post<CreateChatResultWeb>(
      tenantBase(tenantId),
      data
    );
    return response.data;
  },

  renameGroupChat: async (
    tenantId: string,
    chatId: string,
    data: RenameChatRequest
  ): Promise<void> => {
    await axiosClient.patch(`${tenantBase(tenantId)}/${chatId}`, data);
  },

  getMembers: async (
    tenantId: string,
    chatId: string
  ): Promise<ChatMemberWeb[]> => {
    const response = await axiosClient.get<ChatMemberWeb[]>(
      `${tenantBase(tenantId)}/${chatId}/members`
    );
    return response.data;
  },

  getAvailableMembers: async (
    tenantId: string,
    chatId: string
  ): Promise<AvailableMemberWeb[]> => {
    const response = await axiosClient.get<AvailableMemberWeb[]>(
      `${tenantBase(tenantId)}/${chatId}/available-members`
    );
    return response.data;
  },

  addMember: async (
    tenantId: string,
    chatId: string,
    data: AddMemberRequest
  ): Promise<void> => {
    await axiosClient.post(`${tenantBase(tenantId)}/${chatId}/members`, data);
  },

  removeMember: async (
    tenantId: string,
    chatId: string,
    userId: string
  ): Promise<void> => {
    await axiosClient.delete(
      `${tenantBase(tenantId)}/${chatId}/members/${userId}`
    );
  },

  leaveChat: async (
    tenantId: string | null,
    chatId: string
  ): Promise<void> => {
    await axiosClient.post(`${chatBase(tenantId)}/${chatId}/leave`);
  },

  /** Group chat delete only — direct chats use leaveChat. */
  deleteChat: async (tenantId: string, chatId: string): Promise<void> => {
    await axiosClient.delete(`${tenantBase(tenantId)}/${chatId}`);
  },

  getMessages: async (
    tenantId: string | null,
    chatId: string,
    pageSize = 50,
    before?: string
  ): Promise<MessageWeb[]> => {
    const response = await axiosClient.get<MessageWeb[]>(
      `${chatBase(tenantId)}/${chatId}/messages`,
      { params: { pageSize, ...(before ? { before } : {}) } }
    );
    return response.data;
  },

  sendMessage: async (
    tenantId: string | null,
    chatId: string,
    data: SendMessageRequest
  ): Promise<{ id: string }> => {
    const response = await axiosClient.post<{ id: string }>(
      `${chatBase(tenantId)}/${chatId}/messages`,
      data
    );
    return response.data;
  },

  editMessage: async (
    tenantId: string | null,
    chatId: string,
    messageId: string,
    data: EditMessageRequest
  ): Promise<void> => {
    await axiosClient.patch(
      `${chatBase(tenantId)}/${chatId}/messages/${messageId}`,
      data
    );
  },

  deleteMessage: async (
    tenantId: string | null,
    chatId: string,
    messageId: string
  ): Promise<void> => {
    await axiosClient.delete(
      `${chatBase(tenantId)}/${chatId}/messages/${messageId}`
    );
  },

  markAsRead: async (
    tenantId: string | null,
    chatId: string
  ): Promise<void> => {
    await axiosClient.put(`${chatBase(tenantId)}/${chatId}/read`);
  },
};
