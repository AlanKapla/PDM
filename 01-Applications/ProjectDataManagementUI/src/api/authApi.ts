import type { 
  LoginRequest, 
  RegisterRequest, 
  LogoutRequest,
  PasswordResetRequest,
  ResetPasswordRequest
} from "../types/auth.types";

const API_BASE = import.meta.env.VITE_API_BASE_URL || "";
const API_URL = `${API_BASE}/api/User`;

export const authApi = {
  register: async (data: RegisterRequest) => {
    return fetch(`${API_URL}/register`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  login: async (data: LoginRequest) => {
    return fetch(`${API_URL}/login`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  logout: async (data: LogoutRequest) => {
    return fetch(`${API_URL}/logout`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  getProfile: async () => {
    return fetch(`${API_URL}/me`, {
      method: "GET",
      credentials: "include",
    });
  },

  requestPasswordReset: async (data: PasswordResetRequest) => {
    return fetch(`${API_URL}/reset-password-request`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  resetPassword: async (data: ResetPasswordRequest) => {
    return fetch(`${API_URL}/reset-password`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  activateAccount: async (data: { token: string }) => {
    return fetch(`${API_URL}/activate-account`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },

  updateProfile: async (data: { firstName: string; lastName: string }) => {
    return fetch(`${API_URL}/me`, {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data),
    });
  },
};
