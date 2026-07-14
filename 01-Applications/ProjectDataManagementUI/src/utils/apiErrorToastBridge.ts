type ApiErrorListener = (error: unknown) => void;

let listener: ApiErrorListener | null = null;

/** Rejestruje listener toastów — wywoływane z ApiErrorToastBridge w drzewie React. */
export function registerApiErrorListener(next: ApiErrorListener | null): void {
  listener = next;
}

/** Raportuje błąd API poza komponentem React (np. w onError mutacji React Query). */
export function reportApiError(error: unknown): void {
  listener?.(error);
}
