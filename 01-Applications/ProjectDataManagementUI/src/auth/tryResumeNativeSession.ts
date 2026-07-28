import type { AccountInfo } from "@azure/msal-browser";
import type { ICustomAuthPublicClientApplication } from "@azure/msal-browser/custom-auth";
import { nativeSignInScopes } from "../config/customAuthConfig";
import { rememberSignInEmail } from "./rememberedSignIn";

export interface ResumeNativeSessionResult {
  resumed: boolean;
  accountEmail: string | null;
}

/**
 * Wznawia sesję z cache MSAL (access/refresh token) bez ponownego hasła.
 * Odpowiednik dawnego SSO przy redirect — Native Auth nie ustawia cookies na ciamlogin.com.
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
      rememberSignInEmail(account.username);
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
      rememberSignInEmail(account.username);
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
    rememberSignInEmail(account.username);
    if (redirectToDashboard) {
      window.location.assign("/dashboard");
    }
    return { resumed: true, accountEmail: account.username };
  } catch {
    return { resumed: false, accountEmail: account.username };
  }
}
