import type { AccountInfo } from "@azure/msal-browser";
import type { ICustomAuthPublicClientApplication } from "@azure/msal-browser/custom-auth";
import {
  markSoftLoggedOut,
  PRESERVED_AUTH_STORAGE_KEYS,
  rememberSignInEmail,
} from "./rememberedSignIn";

const POST_LOGOUT_PATH = "/logged-out";

export interface LogoutMsalSessionOptions {
  /**
   * hard = pełne wyczyszczenie cache MSAL (hasło wymagane ponownie).
   * soft (domyślne) = zostawia refresh tokeny — możliwy resume „Kontynuuj jako…”.
   */
  mode?: "soft" | "hard";
}

function isMsalStorageKey(key: string): boolean {
  return key.startsWith("msal.") || key.toLowerCase().includes("msal");
}

/**
 * Czyści stan aplikacji, zostawiając cache MSAL (sessionStorage) i klucze soft-logout.
 * MSAL używa sessionStorage — pełne sessionStorage.clear() zabija „Kontynuuj jako…”.
 */
export function clearAppStoragePreservingMsal(): void {
  const preserve: Set<string> = new Set(PRESERVED_AUTH_STORAGE_KEYS);
  Object.keys(localStorage).forEach((key) => {
    if (isMsalStorageKey(key) || preserve.has(key)) {
      return;
    }
    localStorage.removeItem(key);
  });
  Object.keys(sessionStorage).forEach((key) => {
    if (isMsalStorageKey(key)) {
      return;
    }
    sessionStorage.removeItem(key);
  });
}

function clearMsalStorage(): void {
  Object.keys(localStorage)
    .filter((key) => isMsalStorageKey(key))
    .forEach((key) => localStorage.removeItem(key));
  Object.keys(sessionStorage)
    .filter((key) => isMsalStorageKey(key))
    .forEach((key) => sessionStorage.removeItem(key));
}

function goToLoggedOut(): void {
  window.location.assign(`${window.location.origin}${POST_LOGOUT_PATH}`);
}

function rememberEmailFromInstance(
  instance: ICustomAuthPublicClientApplication,
  fallbackAccount?: AccountInfo | null
): void {
  const active = instance.getActiveAccount() || fallbackAccount || instance.getAllAccounts()[0];
  if (active?.username) {
    rememberSignInEmail(active.username);
  }
}

/**
 * Wylogowanie z aplikacji.
 * Domyślnie soft: czyści stan UI, zostawia cache MSAL (RT) pod resume bez hasła.
 * Native Auth nie ustawia cookies IdP — „SSO” = wyłącznie żywy refresh token w sessionStorage.
 */
export async function logoutMsalSession(
  instance: ICustomAuthPublicClientApplication,
  fallbackAccount?: AccountInfo | null,
  options?: LogoutMsalSessionOptions
): Promise<void> {
  const mode: "soft" | "hard" = options?.mode ?? "soft";

  rememberEmailFromInstance(instance, fallbackAccount);
  clearAppStoragePreservingMsal();

  if (mode === "soft") {
    markSoftLoggedOut();
    try {
      instance.setActiveAccount(null);
    } catch {
      // ignore
    }
    goToLoggedOut();
    return;
  }

  try {
    const accountResult = instance.getCurrentAccount();
    if (accountResult.isCompleted() && accountResult.data) {
      await accountResult.data.signOut();
      if (window.location.pathname.includes("logged-out")) {
        return;
      }
    }
  } catch (error: unknown) {
    if (import.meta.env.DEV) {
      console.warn("[auth] Custom Auth signOut failed, clearing cache locally", error);
    }
  }

  try {
    await instance.clearCache();
  } catch {
    // ignore
  }

  try {
    instance.setActiveAccount(null);
  } catch {
    // ignore
  }

  clearMsalStorage();
  goToLoggedOut();
}
