import type { AccountInfo } from "@azure/msal-browser";
import type { ICustomAuthPublicClientApplication } from "@azure/msal-browser/custom-auth";
import { nativeApiScope } from "../config/customAuthConfig";
import { clearSoftLoggedOut, rememberSignInEmail } from "./rememberedSignIn";
import { withTimeout } from "./withTimeout";

const TOKEN_TIMEOUT_MS = 10_000;
const API_SCOPES: string[] = [nativeApiScope];

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

    try {
      const tokenResult = await withTimeout(
        accountData.getAccessToken({
          forceRefresh: false,
          scopes: API_SCOPES,
        }),
        TOKEN_TIMEOUT_MS,
        "getAccessToken timed out"
      );

      if (!tokenResult.isFailed()) {
        markResumed(account.username);
        if (redirectToDashboard) {
          window.location.assign("/dashboard");
        }
        return { resumed: true, accountEmail: account.username };
      }
    } catch {
      // Timeout or unexpected failure — try forceRefresh below.
    }

    try {
      const retry = await withTimeout(
        accountData.getAccessToken({
          forceRefresh: true,
          scopes: API_SCOPES,
        }),
        TOKEN_TIMEOUT_MS,
        "getAccessToken(forceRefresh) timed out"
      );
      if (!retry.isFailed()) {
        markResumed(account.username);
        if (redirectToDashboard) {
          window.location.assign("/dashboard");
        }
        return { resumed: true, accountEmail: account.username };
      }
    } catch {
      // Fall through to acquireTokenSilent / fail soft.
    }
  }

  const accounts: AccountInfo[] = instance.getAllAccounts();
  if (accounts.length === 0) {
    return { resumed: false, accountEmail: null };
  }

  const account: AccountInfo = instance.getActiveAccount() || accounts[0];
  instance.setActiveAccount(account);

  try {
    await withTimeout(
      instance.acquireTokenSilent({
        scopes: API_SCOPES,
        account,
      }),
      TOKEN_TIMEOUT_MS,
      "acquireTokenSilent timed out"
    );
    markResumed(account.username);
    if (redirectToDashboard) {
      window.location.assign("/dashboard");
    }
    return { resumed: true, accountEmail: account.username };
  } catch {
    return { resumed: false, accountEmail: account.username };
  }
}
