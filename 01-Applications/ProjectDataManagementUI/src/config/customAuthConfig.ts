import type { CustomAuthConfiguration } from "@azure/msal-browser/custom-auth";
import { nativeAuthNetworkClient } from "../auth/nativeAuthNetworkClient";
import { resolveNativeAuthProxyUrl } from "../auth/resolveNativeAuthProxyUrl";
import { loginScopes } from "./authConfig";

const tenantSubdomain: string = import.meta.env.VITE_AZURE_B2C_DOMAIN || "pdmapp";
const tenantName: string = import.meta.env.VITE_AZURE_B2C_TENANT_NAME || "pdmapp";
/** Directory (tenant) ID — External ID (pdmapp). NIE f8cdef31-... (Microsoft Services). */
const tenantId: string =
  import.meta.env.VITE_AZURE_B2C_TENANT_ID || "77b1686a-7dc5-4d4d-976c-2c78a8f040d2";
const clientId: string =
  import.meta.env.VITE_AZURE_B2C_CLIENT_ID || "717bb844-7994-43f3-a047-55e7bc9ec367";
const apiClientId: string =
  import.meta.env.VITE_AZURE_B2C_API_CLIENT_ID || "acdbf0bf-609d-44a2-9c6a-3f2790508b3f";

const customAuthAuthority: string = `https://${tenantSubdomain}.ciamlogin.com/${tenantName}.onmicrosoft.com`;

export const customAuthConfig: CustomAuthConfiguration = {
  customAuth: {
    challengeTypes: ["password", "oob"],
    authApiProxyUrl: resolveNativeAuthProxyUrl(),
  },
  auth: {
    clientId,
    authority: customAuthAuthority,
    knownAuthorities: [`${tenantSubdomain}.ciamlogin.com`],
    redirectUri: `${window.location.origin}/auth/callback`,
    postLogoutRedirectUri: `${window.location.origin}/logged-out`,
    navigateToLoginRequestUrl: false,
  },
  cache: {
    // localStorage w MSAL 4 szyfruje cache kluczem w cookie `msal.cache.encryption`
    // (Secure + SameSite=None). Po hard redirect cookie często nie wraca / rotuje ID
    // → getAllAccounts()=0 mimo danych w storage. sessionStorage = plaintext, przeżywa reload w tej samej karcie.
    cacheLocation: "sessionStorage",
    storeAuthStateInCookie: true,
  },
  system: {
    networkClient: nativeAuthNetworkClient,
  },
};

/** Te same scope'y co redirect login — inaczej axios nie ma tokenu API. */
export const nativeSignInScopes: string[] = loginScopes;

/** Sam scope API — odczyt AT z cache. */
export const nativeApiScope: string = `api://${apiClientId}/access_as_user`;

export const externalTenantId: string = tenantId;
