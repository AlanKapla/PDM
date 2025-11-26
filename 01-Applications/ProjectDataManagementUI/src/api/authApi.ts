import type { 
  LoginRequest, 
  RegisterRequest, 
  LogoutRequest,
  PasswordResetRequest,
  ResetPasswordRequest
} from "../types/auth.types";

const API_BASE = import.meta.env.VITE_API_BASE_URL || "";
const API_URL = `${API_BASE}/api/User`;

let isRefreshing = false;

// Wrapper dla fetch z obsługą wygasłych tokenów i refresh flow
const fetchWithAuth = async (url: string, options: RequestInit = {}): Promise<Response> => {
  const response = await fetch(url, options);
  
  if (response.status === 401 && !isRefreshing) {
    isRefreshing = true;
    
    try {
      // Spróbuj odświeżyć token
      const refreshResponse = await fetch(`${API_URL}/refresh`, {
        method: "POST",
        credentials: "include",
      });

      if (refreshResponse.status === 401) {
        // Refresh token też wygasł - sesja całkowicie wygasła
        console.warn("Sesja wygasła - przekierowanie na login");
        window.location.href = "/login";
        return response;
      }

      if (refreshResponse.ok) {
        // Token odświeżony pomyślnie - ponów oryginalny request
        isRefreshing = false;
        return fetch(url, options);
      } else {
        // Inny błąd (500, 503 etc.)
        console.error("Błąd serwera podczas refresh:", refreshResponse.status);
        window.location.href = "/login";
        return response;
      }
    } catch (error) {
      // Błąd sieciowy
      console.error("Błąd sieci podczas refresh token:", error);
      window.location.href = "/login";
      return response;
    } finally {
      isRefreshing = false;
    }
  }
  
  return response;
};

export const authApi = {
  register: async (data: RegisterRequest) => {
    return fetchWithAuth(`${API_URL}/register`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  login: async (data: LoginRequest) => {
    // Login nie powinien sprawdzać 401 - to pierwsze żądanie bez tokenu
    return fetch(`${API_URL}/login`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  logout: async (data: LogoutRequest) => {
    return fetchWithAuth(`${API_URL}/logout`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  getProfile: async () => {
    return fetchWithAuth(`${API_URL}/me`, {
      method: "GET",
      credentials: "include",
    });
  },

  requestPasswordReset: async (data: PasswordResetRequest) => {
    // Reset hasła nie wymaga autentykacji
    return fetch(`${API_URL}/reset-password-request`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  resetPassword: async (data: ResetPasswordRequest) => {
    // Reset hasła nie wymaga autentykacji (token w URL)
    return fetch(`${API_URL}/reset-password`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  activateAccount: async (data: { token: string }) => {
    // Aktywacja nie wymaga autentykacji (token w payload)
    return fetch(`${API_URL}/activate-account`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  updateProfile: async (data: { firstName: string; lastName: string }) => {
    return fetchWithAuth(`${API_URL}/me`, {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },
};
