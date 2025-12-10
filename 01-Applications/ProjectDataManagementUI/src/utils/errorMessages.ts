// Mapowanie ApiExceptionReason z backendu na komunikaty po polsku
export const apiExceptionReasonMessages: Record<string, string> = {
  ValidationError: "Niepoprawne dane. Sprawdź formularz.",
  NotFound: "Nie znaleziono żądanego zasobu.",
  Unauthorized: "Brak autoryzacji. Zaloguj się ponownie.",
  Forbidden: "Brak uprawnień do wykonania tej operacji.",
  Conflict: "Konflikt danych — element już istnieje.",
  InternalServerError: "Wystąpił błąd serwera. Spróbuj ponownie.",
};

// Stare mapowanie dla kompatybilności wstecznej
export const exceptionMessages: Record<string, string> = {
  ConflictApiException: "Konflikt danych — element już istnieje.",
  NotFoundApiException: "Nie znaleziono żądanego zasobu.",
  UnauthorizedApiException: "Brak autoryzacji. Zaloguj się ponownie.",
  ValidationApiException: "Niepoprawne dane. Sprawdź formularz.",
  ApiException: "Wystąpił błąd podczas przetwarzania żądania.",
};

export const defaultErrorMessage =
  "Wystąpił nieoczekiwany błąd. Spróbuj ponownie.";

// Mapowanie kodów HTTP na komunikaty
export const httpStatusMessages: Record<number, string> = {
  400: "Nieprawidłowe żądanie.",
  401: "Brak autoryzacji. Zaloguj się ponownie.",
  403: "Brak uprawnień do wykonania tej operacji.",
  404: "Nie znaleziono żądanego zasobu.",
  409: "Konflikt danych — element już istnieje.",
  500: "Wystąpił błąd serwera. Spróbuj ponownie.",
  503: "Usługa tymczasowo niedostępna. Spróbuj ponownie później.",
};
