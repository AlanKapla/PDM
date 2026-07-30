import {
  CustomAuthPublicClientApplication,
  type ICustomAuthPublicClientApplication,
} from "@azure/msal-browser/custom-auth";
import { customAuthConfig } from "../config/customAuthConfig";
import { clearStaleMsalInteraction } from "./clearStaleMsalInteraction";
import { withTimeout } from "./withTimeout";

/**
 * Jedna wspólna PCA (Custom Auth + redirect + axios + MsalProvider).
 * Dwie instancje (standard + custom) psują cache — po native login apka wraca na login.
 *
 * Live binding: `export let` — po initializeMsalInstance() wszystkie importy widzą tę samą PCA.
 */
export let msalInstance: ICustomAuthPublicClientApplication =
  null as unknown as ICustomAuthPublicClientApplication;

const MSAL_INIT_TIMEOUT_MS = 15_000;

export async function initializeMsalInstance(): Promise<ICustomAuthPublicClientApplication> {
  if (msalInstance) {
    return msalInstance;
  }

  // Mobile / iOS PWA: zabicie appki w trakcie token refresh zostawia interaction.status.
  clearStaleMsalInteraction();

  const instance: ICustomAuthPublicClientApplication = await withTimeout(
    CustomAuthPublicClientApplication.create(customAuthConfig),
    MSAL_INIT_TIMEOUT_MS,
    "CustomAuthPublicClientApplication.create timed out"
  );
  await withTimeout(
    instance.initialize(),
    MSAL_INIT_TIMEOUT_MS,
    "msal initialize timed out"
  );
  msalInstance = instance;
  return instance;
}

export function getMsalInstance(): ICustomAuthPublicClientApplication {
  if (!msalInstance) {
    throw new Error("MSAL nie jest zainicjalizowany — wywołaj initializeMsalInstance().");
  }
  return msalInstance;
}
