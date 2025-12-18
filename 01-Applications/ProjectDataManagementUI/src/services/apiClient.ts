import { handleApiError } from "../utils/handleApiError";

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
