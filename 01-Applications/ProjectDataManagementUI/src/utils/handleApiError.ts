import { AxiosError } from "axios";
import { 
  defaultErrorMessage, 
  apiExceptionReasonMessages,
  httpStatusMessages 
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
      title: "Błąd połączenia",
      description: "Nie udało się połączyć z serwerem"
    };
  }

  const data = response.data as ApiExceptionResponse | null;

  // Obsługa struktury ApiException z backendu
  if (data && 'error' in data && typeof data.error === 'string') {
    const { error: errorCode, message } = data;
    
    // Tytuł z kategorii błędu, opis ze szczegółowego message
    const title = apiExceptionReasonMessages[errorCode] || errorCode;
    return {
      title,
      description: message || undefined
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
