import { fetchWithAuth } from "./authApi";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export const notificationApi = {
  // Pobierz nieprzeczytane powiadomienia
  getUnreadNotifications: async (): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/Notification/unread`, {
      method: "GET",
      credentials: "include",
    });
  },
};
