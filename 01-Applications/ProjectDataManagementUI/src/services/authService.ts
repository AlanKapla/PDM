/**
 * ⚠️ DEPRECATED - This service uses legacy authApi endpoints.
 * Most authentication is now handled by MSAL via AuthContext.
 * Only keep methods that are still used: activateAccount, requestPasswordReset, resetPassword
 */
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

/** @deprecated MSAL handles registration */
export const registerUser = async (form: RegisterForm): Promise<boolean> => {
  try {
    const payload = {
      email: form.email,
      password: form.password,
      firstName: form.firstName,
      lastName: form.lastName,
      externalToken: "",
      provider: 0,
    };

    await authApi.register(payload);
    return true;
  } catch (error) {
    console.error("Register error:", error);
    return false;
  }
};

/** @deprecated MSAL handles Google authentication */
export const registerGoogleUser = async (googleToken: string): Promise<{ success: boolean; message?: string }> => {
  try {
    await authApi.registerGoogle(googleToken);
    return { success: true };
  } catch (error: any) {
    return {
      success: false,
      message: error?.response?.data?.message || "Google registration failed",
    };
  }
};

/** @deprecated MSAL handles login */
export const loginUser = async (form: LoginForm): Promise<{ success: boolean; message?: string }> => {
  try {
    const payload = {
      email: form.email,
      password: form.password,
      externalToken: "",
      provider: 0,
    };

    await authApi.login(payload);
    return { success: true };
  } catch (error: any) {
    return {
      success: false,
      message: error?.response?.data?.message || "Invalid login credentials",
    };
  }
};

/** @deprecated Use AuthContext.user instead */
export const getUserProfile = async (): Promise<UserProfile | null> => {
  try {
    const response = await authApi.getProfile();
    return response.data;
  } catch (error) {
    console.error("Get profile error:", error);
    return null;
  }
};

export const requestPasswordReset = async (email: string): Promise<boolean> => {
  try {
    await authApi.requestPasswordReset({ email });
    return true;
  } catch (error) {
    console.error("Request password reset error:", error);
    return false;
  }
};

export const resetPassword = async (token: string, password: string): Promise<boolean> => {
  try {
    await authApi.resetPassword({ token, password });
    return true;
  } catch (error) {
    console.error("Reset password error:", error);
    return false;
  }
};

export const activateAccount = async (token: string): Promise<boolean> => {
  try {
    await authApi.activateAccount({ token });
    return true;
  } catch (error) {
    console.error("Activate account error:", error);
    return false;
  }
};
