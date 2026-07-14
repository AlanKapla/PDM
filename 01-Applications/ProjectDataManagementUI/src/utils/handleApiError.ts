import { AxiosError } from "axios";
import type { ApiErrorResult, ApiExceptionResponse } from "../types/apiError.types";
import {
  defaultErrorMessage,
  apiExceptionReasonMessages,
  httpStatusMessages,
  extractValidationErrorMessages,
  resolveKnownApiMessage,
} from "./errorMessages";

export type { ApiErrorResult } from "../types/apiError.types";

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
      description: defaultErrorMessage,
    };
  }

  const response = error.response;
  if (!response) {
    return {
      title: "Brak połączenia",
      description: "Nie udało się połączyć z serwerem",
    };
  }

  const data = response.data as ApiExceptionResponse | null;

  if (data && "error" in data && typeof data.error === "string") {
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

  if (httpStatusMessages[response.status]) {
    return {
      title: httpStatusMessages[response.status],
    };
  }

  return {
    title: "Błąd",
    description: defaultErrorMessage,
  };
};
