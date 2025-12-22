/**
 * ⚠️ DEPRECATED - This file is no longer used.
 * All API calls now use axiosClient from ../api/axiosClient.ts
 * which provides automatic MSAL Bearer token injection.
 * 
 * This legacy apiClient used fetch with cookie-based authentication.
 */
import { handleApiError } from "../utils/handleApiError";

/** @deprecated Use axiosClient instead */
export const apiClient = async (url: string, options: RequestInit = {}) => {
  try {
    const response = await fetch(url, options);

    if (!response.ok) {
      const msg = await handleApiError(response);
      throw new Error(msg as unknown as string);
    }

    return response.json();
  } catch (err) {
    if (err instanceof Error) {
      throw err;
    }
    throw new Error("Wystąpił nieoczekiwany błąd.");
  }
};
