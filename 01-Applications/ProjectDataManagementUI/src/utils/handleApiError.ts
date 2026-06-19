import { AxiosError } from "axios";
import {
  defaultErrorMessage,
  apiExceptionReasonMessages,
  httpStatusMessages,
  extractValidationErrorMessages,
  resolveKnownApiMessage,
  type ApiToastStatus,
} from "./errorMessages";

// Struktura ApiException z backendu
interface ApiExceptionResponse {
  error: string; // ApiExceptionReason (ValidationError, NotFound, etc.)
  message: string;
  objectType?: string;
  objectId?: string;
}

export interface ApiErrorResult {
  title: string;
  description?: string;
  toastStatus?: ApiToastStatus;
}

function resolveApiMessageDetails(message: string): Pick<ApiErrorResult, "title" | "description" | "toastStatus"> | null {
  const errorTexts = extractValidationErrorMessages(message);

  for (const text of errorTexts) {
    const resolved = resolveKnownApiMessage(text);
    if (resolved) {
      return {
        title: resolved.title,
        description: resolved.description,
        toastStatus: resolved.toastStatus,
      };
    }
  }

  return null;
}

function formatValidationDescription(message: string): string | undefined {
  const errors = extractValidationErrorMessages(message);
  if (errors.length === 0) {
    return message || undefined;
  }

  return errors.join(" ");
}

/**
 * Obsługuje błędy z Axios (AxiosError)
 * Axios automatycznie rzuca wyjątki dla 4xx/5xx - używaj w catch block
 */
export const handleApiError = (error: unknown): ApiErrorResult => {
  if (!(error instanceof AxiosError)) {
    return {
      title: "Błąd",
      description: defaultErrorMessage
    };
  }

  const response = error.response;
  if (!response) {
    return {
      title: "Brak połączenia",
      description: "Nie udało się połączyć z serwerem"
    };
  }

  const data = response.data as ApiExceptionResponse | null;

  // Obsługa struktury ApiException z backendu
  if (data && 'error' in data && typeof data.error === 'string') {
    const { error: errorCode, message } = data;

    if (message) {
      const resolved = resolveApiMessageDetails(message);
      if (resolved) {
        return resolved;
      }
    }

    const title = apiExceptionReasonMessages[errorCode] || errorCode;
    const description = errorCode === "ValidationError" && message
      ? formatValidationDescription(message)
      : message || undefined;

    return {
      title,
      description,
      toastStatus: "error",
    };
  }

  // Fallback na kod HTTP
  if (httpStatusMessages[response.status]) {
    return {
      title: httpStatusMessages[response.status]
    };
  }

  // Ostateczny fallback
  return {
    title: "Błąd",
    description: defaultErrorMessage
  };
};
