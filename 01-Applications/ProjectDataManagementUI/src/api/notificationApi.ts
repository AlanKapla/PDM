import { axiosClient } from "./axiosClient";

export const notificationApi = {
  // Pobierz wszystkie powiadomienia (historia, limit domyślnie 50)
  getAllNotifications: async (limit: number = 50) => {
    return axiosClient.get(`/Notification?limit=${limit}`);
  },

  // Pobierz nieprzeczytane powiadomienia
  getUnreadNotifications: async () => {
    return axiosClient.get("/Notification/unread");
  },

  // Oznacz powiadomienie jako przeczytane
  markAsRead: async (notificationId: string) => {
    return axiosClient.put(`/Notification/${notificationId}/mark-as-read`);
  },
};
