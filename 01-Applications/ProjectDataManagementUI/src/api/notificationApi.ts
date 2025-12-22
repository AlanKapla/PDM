import { axiosClient } from "./axiosClient";

export const notificationApi = {
  // Pobierz nieprzeczytane powiadomienia
  getUnreadNotifications: async () => {
    return axiosClient.get("/Notification/unread");
  },

  // Oznacz powiadomienie jako przeczytane
  markAsRead: async (notificationId: string) => {
    return axiosClient.put(`/Notification/${notificationId}/mark-as-read`);
  },
};
