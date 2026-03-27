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

export const chatApi = {
  getChats: async (): Promise<ChatWeb[]> => {
    const response = await axiosClient.get<ChatWeb[]>("/chats");
    return response.data;
  },

  getContacts: async (): Promise<ProjectContactsGroupWeb[]> => {
    const response = await axiosClient.get<ProjectContactsGroupWeb[]>("/chats/contacts");
    return response.data;
  },

  searchChats: async (q: string): Promise<ChatSearchResultWeb[]> => {
    const response = await axiosClient.get<ChatSearchResultWeb[]>("/chats/search", {
      params: { q },
    });
    return response.data;
  },

  getChatsByMembers: async (memberIds: string[]): Promise<ChatWeb[]> => {
    const response = await axiosClient.get<ChatWeb[]>("/chats/by-members", {
      params: { memberIds },
    });
    return response.data;
  },

  createChat: async (data: CreateChatRequest): Promise<CreateChatResultWeb> => {
    const response = await axiosClient.post<CreateChatResultWeb>("/chats", data);
    return response.data;
  },

  renameGroupChat: async (chatId: string, data: RenameChatRequest): Promise<void> => {
    await axiosClient.patch(`/chats/${chatId}`, data);
  },

  getMembers: async (chatId: string): Promise<ChatMemberWeb[]> => {
    const response = await axiosClient.get<ChatMemberWeb[]>(`/chats/${chatId}/members`);
    return response.data;
  },

  getAvailableMembers: async (chatId: string): Promise<AvailableMemberWeb[]> => {
    const response = await axiosClient.get<AvailableMemberWeb[]>(
      `/chats/${chatId}/available-members`
    );
    return response.data;
  },

  addMember: async (chatId: string, data: AddMemberRequest): Promise<void> => {
    await axiosClient.post(`/chats/${chatId}/members`, data);
  },

  removeMember: async (chatId: string, userId: string): Promise<void> => {
    await axiosClient.delete(`/chats/${chatId}/members/${userId}`);
  },

  leaveChat: async (chatId: string): Promise<void> => {
    await axiosClient.post(`/chats/${chatId}/leave`);
  },

  deleteChat: async (chatId: string): Promise<void> => {
    await axiosClient.delete(`/chats/${chatId}`);
  },

  getMessages: async (
    chatId: string,
    pageSize = 50,
    before?: string
  ): Promise<MessageWeb[]> => {
    const response = await axiosClient.get<MessageWeb[]>(`/chats/${chatId}/messages`, {
      params: { pageSize, ...(before ? { before } : {}) },
    });
    return response.data;
  },

  sendMessage: async (chatId: string, data: SendMessageRequest): Promise<{ id: string }> => {
    const response = await axiosClient.post<{ id: string }>(`/chats/${chatId}/messages`, data);
    return response.data;
  },

  editMessage: async (
    chatId: string,
    messageId: string,
    data: EditMessageRequest
  ): Promise<void> => {
    await axiosClient.patch(`/chats/${chatId}/messages/${messageId}`, data);
  },

  deleteMessage: async (chatId: string, messageId: string): Promise<void> => {
    await axiosClient.delete(`/chats/${chatId}/messages/${messageId}`);
  },

  markAsRead: async (chatId: string): Promise<void> => {
    await axiosClient.put(`/chats/${chatId}/read`);
  },
};
