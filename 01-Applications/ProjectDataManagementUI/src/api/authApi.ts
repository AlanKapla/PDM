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
let hasRedirected = false; // Flaga zapobiegająca wielokrotnym przekierowaniom

// Wrapper dla fetch z obsługą wygasłych tokenów i refresh flow
const fetchWithAuth = async (url: string, options: RequestInit = {}): Promise<Response> => {
  const response = await fetch(url, options);
  
  // Jeśli to /me i dostaliśmy 401, nie próbuj refresh - user nie jest zalogowany
  if (response.status === 401 && url.includes("/me")) {
    return response;
  }
  
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
        if (!hasRedirected) {
          hasRedirected = true;
          console.warn("Sesja wygasła - przekierowanie na login");
          window.location.href = "/login";
        }
        isRefreshing = false;
        return response;
      }

      if (refreshResponse.ok) {
        // Token odświeżony pomyślnie - ponów oryginalny request
        isRefreshing = false;
        return fetch(url, options);
      } else {
        // Inny błąd (500, 503 etc.)
        if (!hasRedirected) {
          hasRedirected = true;
          console.error("Błąd serwera podczas refresh:", refreshResponse.status);
          window.location.href = "/login";
        }
        isRefreshing = false;
        return response;
      }
    } catch (error) {
      // Błąd sieciowy
      if (!hasRedirected) {
        hasRedirected = true;
        console.error("Błąd sieci podczas refresh token:", error);
        window.location.href = "/login";
      }
      isRefreshing = false;
      return response;
    }
  }
  
  // Jeśli 401 ale isRefreshing=true (inny request już refreshuje), zwróć 401
  if (response.status === 401 && isRefreshing) {
    return response;
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

  registerGoogle: async (googleToken: string) => {
    return fetch(`${API_URL}/register/google`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ googleToken }),
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

export { fetchWithAuth };

