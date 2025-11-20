const API_URL = "http://localhost:5121/api/User";

export const authApi = {
  register: async (data: any) => {
    return fetch(`${API_URL}/register`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  login: async (data: any) => {
    return fetch(`${API_URL}/login`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  logout: async () => {
    return fetch(`${API_URL}/logout`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },

      body: JSON.stringify({ refreshToken: "" }),
    });
  },

  getProfile: async () => {
    return fetch(`${API_URL}/me`, {
      method: "GET",
      credentials: "include",
    });
  },
};
