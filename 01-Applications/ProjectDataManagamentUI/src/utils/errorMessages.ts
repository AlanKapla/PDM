export const exceptionMessages: Record<string, string> = {
  ConflictApiException: "Konflikt danych — element już istnieje.",
  NotFoundApiException: "Nie znaleziono żądanego zasobu.",
  UnauthorizedApiException: "Brak autoryzacji. Zaloguj się ponownie.",
  ValidationApiException: "Niepoprawne dane. Sprawdź formularz.",
  ApiException: "Wystąpił błąd podczas przetwarzania żądania.",
};

export const defaultErrorMessage =
  "Wystąpił nieoczekiwany błąd. Spróbuj ponownie.";
