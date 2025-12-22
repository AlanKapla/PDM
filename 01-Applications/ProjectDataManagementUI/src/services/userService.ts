/**
 * ⚠️ DEPRECATED - Use AuthContext.user instead
 * This service uses legacy authApi endpoints
 */
import { authApi } from "../api/authApi";
import type { UserProfile } from "../types/auth.types";

/** @deprecated Use AuthContext.user instead */
export const getUserDetails = async (): Promise<UserProfile | null> => {
  try {
    const response = await authApi.getProfile();
    return response.data;
  } catch (error) {
    console.error("Get user details error:", error);
    return null;
  }
};

/** @deprecated Use backend API directly if needed */
export const updateUserProfile = async (
  firstName: string,
  lastName: string
): Promise<boolean> => {
  try {
    await authApi.updateProfile({ firstName, lastName });
    return true;
  } catch (error) {
    console.error("Update user profile error:", error);
    return false;
  }
};
