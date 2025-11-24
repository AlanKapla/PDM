import { exceptionMessages, defaultErrorMessage } from "./errorMessages";

interface ApiErrorResponse {
  type?: string;
  message?: string;
  errorType?: string;
  errors?: Record<string, string[]>;
}

export const handleApiError = async (response: Response): Promise<string> => {
  let data: ApiErrorResponse | null = null;

  try {
    data = await response.json();
  } catch {
    // Response nie zawierał JSON
  }

  if (data?.type && exceptionMessages[data.type]) {
    return data.message || exceptionMessages[data.type];
  }

  if (data?.errorType === "ValidationApiException" && data.errors) {
    const messages = Object.values(data.errors).flat();
    return messages.join("\n");
  }

  if (data?.message) {
    return data.message;
  }

  if (exceptionMessages[response.status]) {
    return exceptionMessages[response.status];
  }

  return defaultErrorMessage;
};
