import type { CustomAuthAccountData } from "@azure/msal-browser/custom-auth";
import { msalInstance } from "./msalInstance";
import { nativeApiScope } from "../config/customAuthConfig";
import { notifyNativeSessionReady } from "./nativeSessionEvents";
import { clearSoftLoggedOut, rememberSignInEmail } from "./rememberedSignIn";
import { withTimeout } from "./withTimeout";

const TOKEN_WARMUP_MS = 8_000;
const API_SCOPES: string[] = [nativeApiScope];

export interface FinalizeNativeSessionOptions {
  /** Domyślnie true — hard redirect (sessionStorage przeżywa reload w tej samej karcie). */
  redirectToDashboard?: boolean;
}

/**
 * Po native sign-in / sign-up: active account + wymagany AT API w cache.
 * Bez działającego tokenu API NIE nawiguje — inaczej axios cicho wraca na /login.
 */
export async function finalizeNativeSession(
  accountData: CustomAuthAccountData | undefined,
  options?: FinalizeNativeSessionOptions
): Promise<void> {
  if (!accountData) {
    throw new Error("Brak danych konta po uwierzytelnieniu native.");
  }

  const redirectToDashboard: boolean = options?.redirectToDashboard !== false;
  const account = accountData.getAccount();
  msalInstance.setActiveAccount(account);
  rememberSignInEmail(account.username);
  // clearSoftLoggedOut dopiero po tokenie — inaczej AuthContext odpala /user/me
  // na /login (mobile: axios fail → czerwony alert → dopiero potem redirect).

  let tokenReady = false;

  try {
    const tokenResult = await withTimeout(
      accountData.getAccessToken({
        forceRefresh: false,
        scopes: API_SCOPES,
      }),
      TOKEN_WARMUP_MS,
      "getAccessToken timed out"
    );
    tokenReady = !tokenResult.isFailed();
  } catch {
    tokenReady = false;
  }

  if (!tokenReady) {
    try {
      await withTimeout(
        msalInstance.acquireTokenSilent({
          scopes: API_SCOPES,
          account,
          forceRefresh: false,
        }),
        TOKEN_WARMUP_MS,
        "acquireTokenSilent timed out"
      );
      tokenReady = true;
    } catch (caught: unknown) {
      const detail =
        caught instanceof Error ? caught.message : "unknown acquireTokenSilent error";
      throw new Error(
        `Uwierzytelniono w Entra, ale brak tokenu API (${detail}). Sprawdź scope api://…/access_as_user i proxy /native-auth.`
      );
    }
  }

  if (!msalInstance.getActiveAccount() && msalInstance.getAllAccounts().length > 0) {
    msalInstance.setActiveAccount(msalInstance.getAllAccounts()[0]);
  }

  clearSoftLoggedOut();
  notifyNativeSessionReady();

  if (redirectToDashboard) {
    window.location.assign("/dashboard");
  }
}
