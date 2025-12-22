import type { 
  LoginRequest, 
  RegisterRequest, 
  LogoutRequest,
  PasswordResetRequest,
  ResetPasswordRequest
} from "../types/auth.types";

const API_BASE = import.meta.env.VITE_API_BASE_URL || "";
const API_URL = `${API_BASE}/api/User`;

/**
 * ⚠️ DEPRECATED - This file contains LEGACY authentication methods.
 * 
 * The app now uses MSAL (Microsoft Authentication Library) for authentication:
 * - Login: via AuthContext + MSAL instance
 * - Logout: via AuthContext.logout() → instance.logoutRedirect()
 * - Token management: automatic via axiosClient + acquireTokenSilent()
 * 
 * Methods below (register, login, logout, getProfile, updateProfile) are NOT used.
 * Only keep: activateAccount, requestPasswordReset, resetPassword if still needed by backend.
 */

// Simple fetch wrapper - only for non-MSAL endpoints
const fetchWithAuth = async (url: string, options: RequestInit = {}): Promise<Response> => {
  const response = await fetch(url, options);
  
  if (response.status === 401) {
    console.warn("⚠️ 401 Unauthorized (legacy endpoint)");
  }
  
  return response;
};

export const authApi = {
  /** @deprecated Use MSAL AuthContext instead */
  register: async (data: RegisterRequest) => {
    return fetchWithAuth(`${API_URL}/register`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  /** @deprecated Use MSAL AuthContext instead */
  registerGoogle: async (googleToken: string) => {
    return fetch(`${API_URL}/register/google`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ googleToken }),
    });
  },

  /** @deprecated Use MSAL AuthContext instead */
  login: async (data: LoginRequest) => {
    return fetch(`${API_URL}/login`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  /** @deprecated Use AuthContext.logout() instead */
  logout: async (data: LogoutRequest) => {
    return fetchWithAuth(`${API_URL}/logout`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  /** @deprecated Use AuthContext.user instead */
  getProfile: async () => {
    return fetchWithAuth(`${API_URL}/me`, {
      method: "GET",
      credentials: "include",
    });
  },

  /** 
   * Request password reset (public endpoint - no auth required)
   * ⚠️ Check if this is still used with MSAL - password reset may be handled by Azure
   */
  requestPasswordReset: async (data: PasswordResetRequest) => {
    return fetch(`${API_URL}/reset-password-request`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  /**
   * Reset password with token (public endpoint - no auth required)
   * ⚠️ Check if this is still used with MSAL - password reset may be handled by Azure
   */
  resetPassword: async (data: ResetPasswordRequest) => {
    return fetch(`${API_URL}/reset-password`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  /**
   * Activate account with token (public endpoint - no auth required)
   * ⚠️ Check if this is still used with MSAL - account activation may be handled by Azure
   */
  activateAccount: async (data: { token: string }) => {
    return fetch(`${API_URL}/activate-account`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  /** @deprecated Profile updates should use axiosClient + MSAL tokens */
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
