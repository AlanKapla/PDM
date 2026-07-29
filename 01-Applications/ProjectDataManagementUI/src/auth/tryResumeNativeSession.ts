import type { AccountInfo } from "@azure/msal-browser";
import type { ICustomAuthPublicClientApplication } from "@azure/msal-browser/custom-auth";
import { nativeSignInScopes } from "../config/customAuthConfig";
import { clearSoftLoggedOut, rememberSignInEmail } from "./rememberedSignIn";

export interface ResumeNativeSessionResult {
  resumed: boolean;
  accountEmail: string | null;
}

function markResumed(accountEmail: string): void {
  rememberSignInEmail(accountEmail);
  clearSoftLoggedOut();
}

/**
 * Wznawia sesję z cache MSAL (access/refresh token) bez ponownego hasła.
 * Native Auth nie ustawia cookies IdP — jedyna „pamięć” sesji to RT w localStorage.
 */
export async function tryResumeNativeSession(
  instance: ICustomAuthPublicClientApplication,
  options?: { redirectToDashboard?: boolean }
): Promise<ResumeNativeSessionResult> {
  const redirectToDashboard: boolean = options?.redirectToDashboard !== false;

  const current = instance.getCurrentAccount();
  if (current.isCompleted() && current.data) {
    const accountData = current.data;
    const account: AccountInfo = accountData.getAccount();
    instance.setActiveAccount(account);

    const tokenResult = await accountData.getAccessToken({
      forceRefresh: false,
      scopes: nativeSignInScopes,
    });

    if (!tokenResult.isFailed()) {
      markResumed(account.username);
      if (redirectToDashboard) {
        window.location.assign("/dashboard");
      }
      return { resumed: true, accountEmail: account.username };
    }

    const retry = await accountData.getAccessToken({
      forceRefresh: true,
      scopes: nativeSignInScopes,
    });
    if (!retry.isFailed()) {
      markResumed(account.username);
      if (redirectToDashboard) {
        window.location.assign("/dashboard");
      }
      return { resumed: true, accountEmail: account.username };
    }
  }

  const accounts: AccountInfo[] = instance.getAllAccounts();
  if (accounts.length === 0) {
    return { resumed: false, accountEmail: null };
  }

  const account: AccountInfo = instance.getActiveAccount() || accounts[0];
  instance.setActiveAccount(account);

  try {
    await instance.acquireTokenSilent({
      scopes: nativeSignInScopes,
      account,
    });
    markResumed(account.username);
    if (redirectToDashboard) {
      window.location.assign("/dashboard");
    }
    return { resumed: true, accountEmail: account.username };
  } catch {
    return { resumed: false, accountEmail: account.username };
  }
}
