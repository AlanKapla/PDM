import type { Configuration, PopupRequest, RedirectRequest } from "@azure/msal-browser";

// ==========================================
// Microsoft Entra External ID Configuration
// ==========================================
// This follows the official Microsoft pattern for External ID (CIAM)
// See: https://learn.microsoft.com/en-us/entra/external-id/customers/how-to-user-flow-sign-up-sign-in-customers

// Environment variables
const tenantSubdomain = import.meta.env.VITE_AZURE_B2C_DOMAIN || "pdmapp";
const tenantName = import.meta.env.VITE_AZURE_B2C_TENANT_NAME || "pdmapp";
const userFlow = import.meta.env.VITE_AZURE_B2C_USER_FLOW || "signupsignin1"; // User flow name
const clientId = import.meta.env.VITE_AZURE_B2C_CLIENT_ID || "717bb844-7994-43f3-a047-55e7bc9ec367";
const apiClientId = import.meta.env.VITE_AZURE_B2C_API_CLIENT_ID || "acdbf0bf-609d-44a2-9c6a-3f2790508b3f";

// Authority URL for External ID
// Format: https://{tenant}.ciamlogin.com/{tenant}.onmicrosoft.com/v2.0
// User flow is passed as query parameter in loginRequest
// See: https://learn.microsoft.com/en-us/entra/external-id/customers/how-to-user-flow-sign-up-sign-in-customers
const authorityUrl = `https://${tenantSubdomain}.ciamlogin.com/${tenantName}.onmicrosoft.com/v2.0`;

// Known authorities (for MSAL validation)
const knownAuthority = `${tenantSubdomain}.ciamlogin.com`;

// Redirect URI - must match EXACTLY what's configured in Azure Portal
const redirectUri = `${window.location.origin}/auth/callback`;

// API Scopes - requesting access to your custom API
// Format: api://{api-client-id}/scope-name
// offline_access jest wymagany do uzyskania refresh_token – bez niego sesja wygasa po ~1h
// See: https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-configure-app-expose-web-apis
export const loginScopes = ["openid", "profile", "offline_access", `api://${apiClientId}/access_as_user`];

// MSAL configuration for Microsoft Entra External ID
// See: https://learn.microsoft.com/en-us/entra/identity-platform/scenario-spa-acquire-token
export const msalConfig: Configuration = {
  auth: {
    clientId: clientId,
    authority: authorityUrl,
    knownAuthorities: [knownAuthority],
    redirectUri: redirectUri,
    postLogoutRedirectUri: `${window.location.origin}/logged-out`,
  },
  cache: {
    cacheLocation: "localStorage", // Recommended for SPA
    storeAuthStateInCookie: true, // Wymagane dla iOS Safari i przeglądarek blokujących third-party cookies
  },
  // system: {
  //   loggerOptions: {
  //     loggerCallback: (level, message, containsPii) => {
  //       if (containsPii) return;
  //       switch (level) {
  //         case 0: console.error(message); return;
  //         case 1: console.warn(message); return;
  //         case 2: console.info(message); return;
  //         case 3: console.debug(message); return;
  //       }
  //     },
  //   },
  // },
};

// Login request - acquires access token for your API
// User flow is passed as query parameter 'p'
// MSAL pattern: request scopes for your API, not Graph
// See: https://learn.microsoft.com/en-us/entra/identity-platform/scenario-spa-acquire-token
export const loginRequest: RedirectRequest = {
  scopes: loginScopes,
  // Brak prompt: "login" – pozwala MSAL na SSO/ciche logowanie gdy sesja jest aktywna
  extraQueryParameters: {
    p: userFlow, // User flow name as query param
  },
};

// Silent token request - for acquireTokenSilent() calls
// Używany przez redirect/popup flow (zachowany dla kompatybilności z msalConfig).
export const silentRequest = {
  scopes: loginScopes,
  extraQueryParameters: {
    p: userFlow,
  },
};

// Silent token request dla Native Auth (Custom Auth) — bez p:userFlow.
// Native Auth nie używa user flow; podanie p:userFlow powoduje wcześniejszy fail
// acquireTokenSilent i skraca efektywną sesję poniżej limitu refresh tokena (~24h).
export const nativeSilentRequest = {
  scopes: loginScopes,
};
