import type { CustomAuthConfiguration } from "@azure/msal-browser/custom-auth";
import { loginScopes } from "./authConfig";

const tenantSubdomain: string = import.meta.env.VITE_AZURE_B2C_DOMAIN || "pdmapp";
const tenantName: string = import.meta.env.VITE_AZURE_B2C_TENANT_NAME || "pdmapp";
/** Directory (tenant) ID — External ID (pdmapp). NIE f8cdef31-... (Microsoft Services). */
const tenantId: string =
  import.meta.env.VITE_AZURE_B2C_TENANT_ID || "77b1686a-7dc5-4d4d-976c-2c78a8f040d2";
const clientId: string =
  import.meta.env.VITE_AZURE_B2C_CLIENT_ID || "717bb844-7994-43f3-a047-55e7bc9ec367";

/**
 * Proxy CORS dla Native Auth API (Entra nie wysyła CORS).
 * - Dev: `npm run dev:cors` → http://localhost:3001/api
 * - Prod (Azure SWA): WebAPI `/api/native-auth` (VITE_API_BASE_URL)
 * - Docker (same origin): opcjonalnie `/native-auth` na nginx + VITE_NATIVE_AUTH_PROXY_URL
 */
function resolveAuthApiProxyUrl(): string {
  const configured: string | undefined = import.meta.env.VITE_NATIVE_AUTH_PROXY_URL;
  if (configured) {
    return configured.replace(/\/$/, "");
  }

  if (import.meta.env.DEV) {
    return "http://localhost:3001/api";
  }

  const apiBase: string | undefined = import.meta.env.VITE_API_BASE_URL;
  if (apiBase) {
    return `${apiBase.replace(/\/$/, "")}/api/native-auth`;
  }

  return `${window.location.origin}/native-auth`;
}

/**
 * Ta sama baza authority co msalConfig (bez /v2.0 — wymóg custom-auth),
 * żeby konto/tokeny wpadły do wspólnego cache localStorage.
 */
const customAuthAuthority: string = `https://${tenantSubdomain}.ciamlogin.com/${tenantName}.onmicrosoft.com`;

export const customAuthConfig: CustomAuthConfiguration = {
  customAuth: {
    challengeTypes: ["password", "oob"],
    authApiProxyUrl: resolveAuthApiProxyUrl(),
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
    cacheLocation: "localStorage",
    storeAuthStateInCookie: true,
  },
};

/** Te same scope'y co redirect login — inaczej axios nie ma tokenu API i wraca na login. */
export const nativeSignInScopes: string[] = loginScopes;

export const externalTenantId: string = tenantId;
