import {
  CustomAuthPublicClientApplication,
  type ICustomAuthPublicClientApplication,
} from "@azure/msal-browser/custom-auth";
import { customAuthConfig } from "../config/customAuthConfig";
import { clearStaleMsalInteraction } from "./clearStaleMsalInteraction";

/**
 * Jedna wspólna PCA (Custom Auth + redirect + axios + MsalProvider).
 * Dwie instancje (standard + custom) psują cache — po native login apka wraca na login.
 *
 * Live binding: `export let` — po initializeMsalInstance() wszystkie importy widzą tę samą PCA.
 */
export let msalInstance: ICustomAuthPublicClientApplication =
  null as unknown as ICustomAuthPublicClientApplication;

export async function initializeMsalInstance(): Promise<ICustomAuthPublicClientApplication> {
  if (msalInstance) {
    return msalInstance;
  }

  // Mobile: zabicie appki w trakcie token refresh zostawia interaction.status w localStorage.
  clearStaleMsalInteraction();

  const instance: ICustomAuthPublicClientApplication =
    await CustomAuthPublicClientApplication.create(customAuthConfig);
  await instance.initialize();
  msalInstance = instance;
  return instance;
}

export function getMsalInstance(): ICustomAuthPublicClientApplication {
  if (!msalInstance) {
    throw new Error("MSAL nie jest zainicjalizowany — wywołaj initializeMsalInstance().");
  }
  return msalInstance;
}
