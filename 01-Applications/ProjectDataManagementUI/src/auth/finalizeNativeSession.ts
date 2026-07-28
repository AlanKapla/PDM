import type { CustomAuthAccountData } from "@azure/msal-browser/custom-auth";
import { msalInstance } from "./msalInstance";
import { nativeSignInScopes } from "../config/customAuthConfig";
import { rememberSignInEmail } from "./rememberedSignIn";

/**
 * Po native sign-in / sign-up: active account + token API w tej samej PCA, potem reload dashboard.
 */
export async function finalizeNativeSession(
  accountData: CustomAuthAccountData | undefined
): Promise<void> {
  if (!accountData) {
    throw new Error("Brak danych konta po uwierzytelnieniu native.");
  }

  const account = accountData.getAccount();
  msalInstance.setActiveAccount(account);
  rememberSignInEmail(account.username);

  const tokenResult = await accountData.getAccessToken({
    forceRefresh: false,
    scopes: nativeSignInScopes,
  });

  if (tokenResult.isFailed()) {
    const retry = await accountData.getAccessToken({
      forceRefresh: true,
      scopes: nativeSignInScopes,
    });
    if (retry.isFailed()) {
      const description =
        retry.error?.errorData?.errorDescription ?? "unknown token error";
      throw new Error(
        `Uwierzytelniono, ale nie udało się pobrać tokenu API (${description}).`
      );
    }
  }

  if (!msalInstance.getActiveAccount() && msalInstance.getAllAccounts().length > 0) {
    msalInstance.setActiveAccount(msalInstance.getAllAccounts()[0]);
  }

  window.location.assign("/dashboard");
}
