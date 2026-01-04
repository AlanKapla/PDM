import { axiosClient } from "./axiosClient";

export interface ChatWeb {
  id: string;
  name: string;
  isGroupChat: boolean;
  memberCount: number;
  lastMessageAt?: string;
  unreadCount: number;
}

export interface MessageWeb {
  id: string;
  chatId: string;
  content: string;
  senderId: string;
  senderName: string;
  sentAt: string;
  isRead: boolean;
}

export interface CreateChatRequest {
  name: string;
  isGroupChat: boolean;
  memberUserIds: string[];
}

export interface SendMessageRequest {
  content: string;
}

export const chatApi = {
  /**
   * Get all chats for project
   */
  getProjectChats: async (tenantId: string, projectId: string): Promise<ChatWeb[]> => {
    const response = await axiosClient.get<ChatWeb[]>(
      `/tenants/${tenantId}/project/${projectId}/chat`
    );
    return response.data;
  },

  /**
   * Create new chat
   */
  createChat: async (
    tenantId: string,
    projectId: string,
    data: CreateChatRequest
  ): Promise<string> => {
    const response = await axiosClient.post<string>(
      `/tenants/${tenantId}/project/${projectId}/chat`,
      {
        tenantId,
        projectId,
        ...data,
      }
    );
    return response.data;
  },

  /**
   * Get messages for specific chat
   */
  getChatMessages: async (
    tenantId: string,
    projectId: string,
    chatId: string,
    pageNumber: number = 1,
    pageSize: number = 50
  ): Promise<MessageWeb[]> => {
    const response = await axiosClient.get<MessageWeb[]>(
      `/tenants/${tenantId}/project/${projectId}/chat/${chatId}/messages`,
      {
        params: { pageNumber, pageSize },
      }
    );
    return response.data;
  },

  /**
   * Send message to chat
   */
  sendMessage: async (
    tenantId: string,
    projectId: string,
    chatId: string,
    content: string
  ): Promise<string> => {
    const response = await axiosClient.post<string>(
      `/tenants/${tenantId}/project/${projectId}/chat/${chatId}/messages`,
      {
        tenantId,
        projectId,
        chatId,
        content,
      }
    );
    return response.data;
  },

  /**
   * Mark all messages in chat as read
   */
  markMessagesAsRead: async (
    tenantId: string,
    projectId: string,
    chatId: string
  ): Promise<number> => {
    const response = await axiosClient.put<number>(
      `/tenants/${tenantId}/project/${projectId}/chat/${chatId}/read`,
      {
        tenantId,
        projectId,
        chatId,
      }
    );
    return response.data;
  },
};
