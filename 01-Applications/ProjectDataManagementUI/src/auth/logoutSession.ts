import type { AccountInfo } from "@azure/msal-browser";
import type { ICustomAuthPublicClientApplication } from "@azure/msal-browser/custom-auth";

const POST_LOGOUT_PATH = "/logged-out";

function clearNonMsalStorage(): void {
  Object.keys(localStorage).forEach((key) => {
    if (!key.startsWith("msal.")) {
      localStorage.removeItem(key);
    }
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
 * Wylogowanie dla wspólnej CustomAuth PCA:
 * 1) Native: getCurrentAccount().signOut() — czyści cache i idzie na postLogoutRedirectUri
 * 2) Redirect CIAM: logoutRedirect (kończy też sesję IdP, gdy endpoint istnieje)
 * 3) Fallback: twarde czyszczenie localStorage + /logged-out
 */
export async function logoutMsalSession(
  instance: ICustomAuthPublicClientApplication,
  fallbackAccount?: AccountInfo | null
): Promise<void> {
  clearNonMsalStorage();

  const accountResult = instance.getCurrentAccount();
  if (accountResult.isCompleted() && accountResult.data) {
    const signOutResult = await accountResult.data.signOut();
    if (signOutResult.isCompleted()) {
      // signOut sam nawiguje na postLogoutRedirectUri
      return;
    }
  }

  const account =
    instance.getActiveAccount() ||
    fallbackAccount ||
    instance.getAllAccounts()[0] ||
    undefined;

  try {
    await instance.logoutRedirect({
      account,
      postLogoutRedirectUri: `${window.location.origin}${POST_LOGOUT_PATH}`,
    });
    return;
  } catch (error: unknown) {
    if (import.meta.env.DEV) {
      console.warn("[auth] logoutRedirect failed, clearing cache locally", error);
    }
  }

  try {
    instance.setActiveAccount(null);
  } catch {
    // ignore
  }
  clearMsalStorage();
  goToLoggedOut();
}
