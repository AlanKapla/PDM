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

function finishResume(
  accountEmail: string,
  redirectToDashboard: boolean
): ResumeNativeSessionResult {
  rememberSignInEmail(accountEmail);
  // Soft-logout flag dopiero tu — wcześniej AuthContext nie strzela API na /login.
  clearSoftLoggedOut();
  if (redirectToDashboard) {
    window.location.assign("/dashboard");
  }
  return { resumed: true, accountEmail };
}

/**
 * Wznawia sesję z cache MSAL (access/refresh token) bez ponownego hasła.
 * Native Auth nie ustawia cookies IdP — jedyna „pamięć” sesji to RT w sessionStorage.
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
        return finishResume(account.username, redirectToDashboard);
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
        return finishResume(account.username, redirectToDashboard);
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
    return finishResume(account.username, redirectToDashboard);
  } catch {
    return { resumed: false, accountEmail: account.username };
  }
}
