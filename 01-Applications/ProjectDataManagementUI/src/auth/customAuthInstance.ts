import type { ICustomAuthPublicClientApplication } from "@azure/msal-browser/custom-auth";
import { getMsalInstance, initializeMsalInstance } from "./msalInstance";

/**
 * Ten sam singleton co MsalProvider / axios — nie twórz drugiej CustomAuth PCA.
 */
export async function getCustomAuthClient(): Promise<ICustomAuthPublicClientApplication> {
  try {
    return getMsalInstance();
  } catch {
    return initializeMsalInstance();
  }
}
