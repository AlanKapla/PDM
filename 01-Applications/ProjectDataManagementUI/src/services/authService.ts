import { authApi } from "../api/authApi";
import type { UserProfile } from "../types/auth.types";

export interface RegisterForm {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface LoginForm {
  email: string;
  password: string;
}

export const registerUser = async (form: RegisterForm): Promise<boolean> => {
  const payload = {
    email: form.email,
    password: form.password,
    firstName: form.firstName,
    lastName: form.lastName,
    externalToken: "",
    provider: 0,
  };

  const res = await authApi.register(payload);
  return res.ok;
};

export const loginUser = async (form: LoginForm): Promise<{ success: boolean; message?: string }> => {
  const payload = {
    email: form.email,
    password: form.password,
    externalToken: "",
    provider: 0,
  };

  const res = await authApi.login(payload);

  if (res.ok) {
    return { success: true };
  }

  // Wydobycie błędu z odpowiedzi API
  try {
    const errorData = await res.json();
    return {
      success: false,
      message: errorData.message || "Invalid login credentials",
    };
  } catch {
    return {
      success: false,
      message: "Invalid login credentials",
    };
  }
};

export const getUserProfile = async (): Promise<UserProfile | null> => {
  const res = await authApi.getProfile();

  if (!res.ok) return null;

  return res.json();
};

export const requestPasswordReset = async (email: string): Promise<boolean> => {
  const res = await authApi.requestPasswordReset({ email });
  return res.ok;
};

export const resetPassword = async (token: string, password: string): Promise<boolean> => {
  const res = await authApi.resetPassword({ token, password });
  return res.ok;
};

export const activateAccount = async (token: string): Promise<boolean> => {
  const res = await authApi.activateAccount({ token });
  return res.ok;
};
