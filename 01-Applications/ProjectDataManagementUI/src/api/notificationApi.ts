import { axiosClient } from "./axiosClient";
import type { NotificationWeb } from "../types/notification.types";

export const notificationApi = {
  async getAll(take: number = 50, skip: number = 0): Promise<NotificationWeb[]> {
    const response = await axiosClient.get(`/notification`, {
      params: { take, skip },
    });
    return response.data;
  },

  async getUnread(take: number = 50, skip: number = 0): Promise<NotificationWeb[]> {
    const response = await axiosClient.get(`/notification/unread`, {
      params: { take, skip },
    });
    return response.data;
  },

  async getUnreadCounter(): Promise<number> {
    const response = await axiosClient.get(`/notification/unread-counter`);
    return response.data;
  },

  async markAsRead(notificationId: string): Promise<void> {
    await axiosClient.put(`/notification/${notificationId}/mark-as-read`);
  },

  async markAllAsRead(): Promise<{ markedCount: number }> {
    const response = await axiosClient.put(`/notification/mark-all-as-read`);
    return response.data;
  },
};
