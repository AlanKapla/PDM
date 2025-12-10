import { 
  exceptionMessages, 
  defaultErrorMessage, 
  apiExceptionReasonMessages,
  httpStatusMessages 
} from "./errorMessages";

// Nowa struktura ApiException z backendu
interface ApiExceptionResponse {
  error: string; // ApiExceptionReason (ValidationError, NotFound, etc.)
  message: string;
  objectType?: string;
  objectId?: string;
}

// Stara struktura dla kompatybilności wstecznej
interface LegacyApiErrorResponse {
  type?: string;
  message?: string;
  errorType?: string;
  errors?: Record<string, string[]>;
}

type ApiErrorResponse = ApiExceptionResponse | LegacyApiErrorResponse;

function isNewApiException(data: ApiErrorResponse): data is ApiExceptionResponse {
  return 'error' in data && typeof data.error === 'string';
}

function isLegacyApiException(data: ApiErrorResponse): data is LegacyApiErrorResponse {
  return 'type' in data || 'errorType' in data;
}

export const handleApiError = async (response: Response): Promise<string> => {
  let data: ApiErrorResponse | null = null;

  try {
    const text = await response.text();
    if (text) {
      data = JSON.parse(text);
    }
  } catch {
    // Response nie zawierał poprawnego JSON
  }

  // Obsługa nowej struktury ApiException
  if (data && isNewApiException(data)) {
    const { error, message } = data;
    
    // Zawsze zwróć message z backendu - to jest dokładny komunikat błędu
    if (message) {
      return message;
    }
    
    // Fallback na zmapowany komunikat jeśli brak message
    if (apiExceptionReasonMessages[error]) {
      return apiExceptionReasonMessages[error];
    }
  }

  // Obsługa starej struktury dla kompatybilności wstecznej
  if (data && isLegacyApiException(data)) {
    if (data.type && exceptionMessages[data.type]) {
      return data.message || exceptionMessages[data.type];
    }

    if (data.errorType === "ValidationApiException" && data.errors) {
      const messages = Object.values(data.errors).flat();
      return messages.join("\n");
    }

    if (data.message) {
      return data.message;
    }
  }

  // Fallback na kod HTTP
  if (httpStatusMessages[response.status]) {
    return httpStatusMessages[response.status];
  }

  // Ostateczny fallback
  return defaultErrorMessage;
};

// Helper do formatowania błędów z dodatkowymi informacjami
export const formatApiError = (error: ApiExceptionResponse): string => {
  let errorMessage = error.message || apiExceptionReasonMessages[error.error] || defaultErrorMessage;
  
  if (error.objectType && error.objectId) {
    errorMessage += ` (${error.objectType}: ${error.objectId})`;
  }
  
  return errorMessage;
};
