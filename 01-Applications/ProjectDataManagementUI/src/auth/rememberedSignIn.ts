const LAST_SIGN_IN_EMAIL_KEY = "pdm:auth.lastSignInEmail";

/** Klucze auth zachowywane przy wylogowaniu (np. podpowiedź e-maila). */
export const PRESERVED_AUTH_STORAGE_KEYS: readonly string[] = [LAST_SIGN_IN_EMAIL_KEY];

export function getRememberedSignInEmail(): string | null {
  try {
    const value: string | null = localStorage.getItem(LAST_SIGN_IN_EMAIL_KEY);
    if (!value) {
      return null;
    }
    const trimmed: string = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  } catch {
    return null;
  }
}

export function rememberSignInEmail(email: string): void {
  const trimmed: string = email.trim();
  if (!trimmed) {
    return;
  }
  try {
    localStorage.setItem(LAST_SIGN_IN_EMAIL_KEY, trimmed);
  } catch {
    // Storage niedostępny (tryb prywatny) — ignoruj.
  }
}
