const LOGIN_ERROR_KEY = "pdm:auth.loginError";

/** Zapamiętaj powód cichego powrotu na /login (axios / guard) — LoginPage go pokaże. */
export function setPendingLoginError(message: string): void {
  try {
    sessionStorage.setItem(LOGIN_ERROR_KEY, message);
  } catch {
    // ignore
  }
}

export function consumePendingLoginError(): string | null {
  try {
    const value: string | null = sessionStorage.getItem(LOGIN_ERROR_KEY);
    if (value) {
      sessionStorage.removeItem(LOGIN_ERROR_KEY);
    }
    return value;
  } catch {
    return null;
  }
}
