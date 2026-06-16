// Mapowanie ApiExceptionReason z backendu na komunikaty po polsku
export const apiExceptionReasonMessages: Record<string, string> = {
  ValidationError: "Niepoprawne dane",
  NotFound:        "Nie znaleziono",
  Unauthorized:    "Brak autoryzacji",
  Forbidden:       "Brak uprawnień",
  Conflict:        "Konflikt danych",
  InvalidOperation: "Błąd operacji",
};

export const defaultErrorMessage =
  "Wystąpił nieoczekiwany błąd. Spróbuj ponownie.";

// Mapowanie kodów HTTP na komunikaty
export const httpStatusMessages: Record<number, string> = {
  400: "Nieprawidłowe żądanie",
  401: "Brak autoryzacji",
  403: "Brak uprawnień",
  404: "Nie znaleziono",
  409: "Konflikt danych",
  500: "Błąd serwera",
  503: "Usługa niedostępna",
};

// ─── Komunikaty sukcesu per operacja ──────────────────────────────
// Krótkie tytuły (mobile) + opcjonalne opisy (desktop)
export const successMessages = {
  // Ogólne
  saved:    { title: "Zapisano",    description: undefined },
  updated:  { title: "Zaktualizowano", description: undefined },
  deleted:  { title: "Usunięto",    description: undefined },
  added:    { title: "Dodano",      description: undefined },
  created:  { title: "Utworzono",   description: undefined },
  sent:     { title: "Wysłano",     description: undefined },
  copied:   { title: "Skopiowano",  description: undefined },
  shared:   { title: "Udostępniono", description: undefined },
  removed:  { title: "Usunięto",    description: undefined },
  activated:   { title: "Aktywowano",    description: undefined },
  deactivated: { title: "Dezaktywowano", description: undefined },

  // Domenowe
  memberAdded:     { title: "Dodano członka",      description: undefined },
  memberRemoved:   { title: "Usunięto członka",    description: undefined },
  memberUpdated:   { title: "Zaktualizowano uprawnienia", description: undefined },
  inviteSent:      { title: "Wysłano zaproszenie", description: undefined },
  inviteCancelled: { title: "Anulowano zaproszenie", description: undefined },
  inviteAccepted:  { title: "Zaproszenie zaakceptowane", description: undefined },
  commentAdded:    { title: "Dodano komentarz",    description: undefined },
  fileSaved:       { title: "Plik zapisany",       description: undefined },
  syncDone:        { title: "Synchronizacja gotowa", description: undefined },
  costAdded:       { title: "Dodano koszt",        description: undefined },
  costUpdated:     { title: "Zaktualizowano koszt", description: undefined },
  costDeleted:     { title: "Usunięto koszt",      description: undefined },
  estimateCreated: { title: "Utworzono kosztorys", description: undefined },
  estimateCopied:  { title: "Skopiowano kosztorys", description: undefined },
  scheduleCreated: { title: "Utworzono harmonogram", description: undefined },
  projectCreated:  { title: "Utworzono projekt",   description: undefined },
  projectUpdated:  { title: "Zaktualizowano projekt", description: undefined },
  tenantUpdated:   { title: "Zaktualizowano organizację", description: undefined },
  tenantSwitched:  { title: "Przełączono organizację", description: undefined },
  roleUpdated:     { title: "Zaktualizowano rolę", description: undefined },
  nameUpdated:     { title: "Zmieniono nazwę",     description: undefined },
  statusUpdated:   { title: "Zaktualizowano status", description: undefined },
  filesUploaded:   { title: "Przesłano pliki", description: undefined },
} as const;

export type SuccessMessageKey = keyof typeof successMessages;
