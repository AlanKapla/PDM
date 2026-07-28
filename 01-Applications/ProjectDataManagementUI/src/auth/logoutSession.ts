import type { AccountInfo } from "@azure/msal-browser";
import type { ICustomAuthPublicClientApplication } from "@azure/msal-browser/custom-auth";
import { PRESERVED_AUTH_STORAGE_KEYS } from "./rememberedSignIn";

const POST_LOGOUT_PATH = "/logged-out";

function clearNonMsalStorage(): void {
  const preserve: Set<string> = new Set(PRESERVED_AUTH_STORAGE_KEYS);
  Object.keys(localStorage).forEach((key) => {
    if (key.startsWith("msal.") || preserve.has(key)) {
      return;
    }
    localStorage.removeItem(key);
  });
  sessionStorage.clear();
}

function clearMsalStorage(): void {
  Object.keys(localStorage)
    .filter((key) => key.startsWith("msal."))
    .forEach((key) => localStorage.removeItem(key));
}

function goToLoggedOut(): void {
  window.location.assign(`${window.location.origin}${POST_LOGOUT_PATH}`);
}

/**
 * Wylogowanie lokalne — czyści cache MSAL, ale NIE kończy sesji Entra (IdP).
 * Dzięki temu w PWA „Zaloguj się” może znów przejść SSO jednym kliknięciem.
 */
export async function logoutMsalSession(
  instance: ICustomAuthPublicClientApplication,
  _fallbackAccount?: AccountInfo | null
): Promise<void> {
  clearNonMsalStorage();

  try {
    const accountResult = instance.getCurrentAccount();
    if (accountResult.isCompleted() && accountResult.data) {
      // Lokalny signOut Custom Auth (bez wymuszania logoutRedirect na ciamlogin).
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
