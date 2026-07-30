import { PRESERVED_AUTH_STORAGE_KEYS } from "./rememberedSignIn";
import { msalInstance } from "./msalInstance";

/**
 * Czyści cache MSAL lokalnie — BEZ clearCache()/logoutRedirect.
 * MSAL clearCache może odpalić ścieżkę logout i nawigację; assign("/login")
 * w trakcie signIn zabija request i czyści konsolę (log „znika bez błędu”).
 */
export function clearLocalMsalCache(): void {
  try {
    msalInstance.setActiveAccount(null);
  } catch {
    // ignore
  }

  try {
    const preserve: Set<string> = new Set(PRESERVED_AUTH_STORAGE_KEYS);
    Object.keys(localStorage).forEach((key) => {
      if (key.startsWith("msal.") && !preserve.has(key)) {
        localStorage.removeItem(key);
      }
    });
  } catch {
    // Storage niedostępny — ignoruj.
  }
}
