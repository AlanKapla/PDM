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

export const handleApiError = async (response: Response): Promise<ApiErrorResult> => {
  let data: ApiExceptionResponse | null = null;

  try {
    const text = await response.text();
    if (text) {
      data = JSON.parse(text);
    }
  } catch {
    // Response nie zawierał poprawnego JSON
  }

  // Obsługa struktury ApiException z backendu
  if (data && 'error' in data && typeof data.error === 'string') {
    const { error, message } = data;
    
    // Tytuł z kategorii błędu, opis ze szczegółowego message
    const title = apiExceptionReasonMessages[error] || error;
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
    title: defaultErrorMessage
  };
};
