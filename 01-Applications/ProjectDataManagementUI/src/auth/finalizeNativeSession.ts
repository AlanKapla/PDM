import type { CustomAuthAccountData } from "@azure/msal-browser/custom-auth";
import { msalInstance } from "./msalInstance";
import { nativeSignInScopes } from "../config/customAuthConfig";
import { clearSoftLoggedOut, rememberSignInEmail } from "./rememberedSignIn";
import { withTimeout } from "./withTimeout";

const TOKEN_TIMEOUT_MS = 10_000;

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
  clearSoftLoggedOut();

  try {
    const tokenResult = await withTimeout(
      accountData.getAccessToken({
        forceRefresh: false,
        scopes: nativeSignInScopes,
      }),
      TOKEN_TIMEOUT_MS,
      "getAccessToken timed out"
    );

    if (tokenResult.isFailed()) {
      const retry = await withTimeout(
        accountData.getAccessToken({
          forceRefresh: true,
          scopes: nativeSignInScopes,
        }),
        TOKEN_TIMEOUT_MS,
        "getAccessToken(forceRefresh) timed out"
      );
      if (retry.isFailed()) {
        const description =
          retry.error?.errorData?.errorDescription ?? "unknown token error";
        throw new Error(
          `Uwierzytelniono, ale nie udało się pobrać tokenu API (${description}).`
        );
      }
    }
  } catch (caught: unknown) {
    if (caught instanceof Error && caught.message.includes("timed out")) {
      throw new Error(
        "Uwierzytelniono, ale pobieranie tokenu API przekroczyło limit czasu. Spróbuj ponownie."
      );
    }
    throw caught;
  }

  if (!msalInstance.getActiveAccount() && msalInstance.getAllAccounts().length > 0) {
    msalInstance.setActiveAccount(msalInstance.getAllAccounts()[0]);
  }

  window.location.assign("/dashboard");
}
