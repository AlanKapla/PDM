const LAST_SIGN_IN_EMAIL_KEY = "pdm:auth.lastSignInEmail";
const SOFT_LOGGED_OUT_KEY = "pdm:auth.softLoggedOut";

/** Klucze auth zachowywane przy wylogowaniu (podpowiedź e-maila, flaga soft logout). */
export const PRESERVED_AUTH_STORAGE_KEYS: readonly string[] = [
  LAST_SIGN_IN_EMAIL_KEY,
  SOFT_LOGGED_OUT_KEY,
];

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

/**
 * Soft logout: użytkownik świadomie wyszedł z UI, ale refresh tokeny MSAL
 * zostają w sessionStorage — „Kontynuuj jako…” może wznowić sesję bez hasła.
 */
export function markSoftLoggedOut(): void {
  try {
    localStorage.setItem(SOFT_LOGGED_OUT_KEY, "1");
  } catch {
    // ignore
  }
}

export function clearSoftLoggedOut(): void {
  try {
    localStorage.removeItem(SOFT_LOGGED_OUT_KEY);
  } catch {
    // ignore
  }
}

export function isSoftLoggedOut(): boolean {
  try {
    return localStorage.getItem(SOFT_LOGGED_OUT_KEY) === "1";
  } catch {
    return false;
  }
}
