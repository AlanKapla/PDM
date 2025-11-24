import { authApi } from "../api/authApi";
import type { UserProfile } from "../types/auth.types";

export const getUserDetails = async (): Promise<UserProfile | null> => {
  const res = await authApi.getProfile();

  if (!res.ok) return null;

  return res.json();
};
