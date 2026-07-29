import axios from "axios";
import { InteractionRequiredAuthError } from "@azure/msal-browser";
import { msalInstance } from "../auth/msalInstance";
import { isSoftLoggedOut } from "../auth/rememberedSignIn";
import { nativeSilentRequest } from "../config/authConfig";
import { setupMockInterceptors, isDemoModeActive } from "./mock";

// Wymagamy jawnego ustawienia zmiennych środowiskowych, aby uniknąć cichego łączenia z błędnym backendem.
function requireEnvVar(key: string): string {
  const value = import.meta.env[key];
  if (!value) {
    throw new Error(
      `${key} is not defined. Configure it in your environment (e.g. .env or build pipeline).`
    );
  }
  return value;
}

const API_BASE_URL = requireEnvVar("VITE_API_BASE_URL");

// Zapobiega wielokrotnym równoległym przekierowaniom na /login
// przy równoczesnych 401 / InteractionRequired.
let interactiveRedirectTriggered = false;

/** Czy MSAL ma już trwającą interakcję (localStorage / sessionStorage). */
function isMsalInteractionInProgress(): boolean {
  if (interactiveRedirectTriggered) {
    return true;
  }

  try {
    const storages: Storage[] = [sessionStorage, localStorage];
    for (const storage of storages) {
      for (let index = 0; index < storage.length; index++) {
        const key: string | null = storage.key(index);
        if (key === null || !key.includes("interaction.status")) {
          continue;
        }
        const value: string | null = storage.getItem(key);
        if (value !== null && value !== "" && value !== "none") {
          return true;
        }
      }
    }
  } catch {
    // Storage niedostępny — zakładamy brak interakcji.
  }

  return false;
}

async function redirectToLoginSafely(): Promise<void> {
  if (isMsalInteractionInProgress()) {
    return;
  }

  interactiveRedirectTriggered = true;
  // Native auth only — nie używamy loginRedirect (hosted Microsoft UI)
  window.location.assign("/login");
}

export const axiosClient = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  withCredentials: false, // Changed to false - using Bearer tokens instead of cookies
});

// ---- Demo Mode — mock interceptors ----
// Rejestrujemy NAJPIERW, aby mock interceptor działał jako OSTATNI w łańcuchu
// (Axios odwraca kolejność rejestracji). Dzięki temu żaden inny interceptor
// nie może odrzucić requestu po ustawieniu mock adaptera.
setupMockInterceptors(axiosClient);

// Request interceptor to add access token
// Follows MSAL best practice: acquireTokenSilent first, then fallback to interactive
// See: https://learn.microsoft.com/en-us/entra/identity-platform/scenario-spa-acquire-token
axiosClient.interceptors.request.use(
  async (config) => {
    // W demo mode nie potrzebujemy tokena — wszystkie requesty są mockowane.
    // Token interceptor odpala się PRZED mock interceptorem (bo jest zarejestrowany później),
    // więc musimy go pominąć, aby nie odrzucił requestu przed ustawieniem mock adaptera.
    if (isDemoModeActive()) {
      return config;
    }

    // Soft logout: nie doklejaj tokenu — użytkownik świadomie wyszedł z sesji UI.
    if (isSoftLoggedOut()) {
      return config;
    }

    const accounts = msalInstance.getAllAccounts();

    if (accounts.length > 0) {
      const account = msalInstance.getActiveAccount() || accounts[0];

      try {
        // Try to acquire token silently from cache
        const response = await msalInstance.acquireTokenSilent({
          ...nativeSilentRequest,
          account: account,
        });

        // Add token to Authorization header
        config.headers.Authorization = `Bearer ${response.accessToken}`;
      } catch (error: unknown) {
        // InteractionRequiredAuthError lub inny fail silent = sesja wygasła.
        // Czyścimy active account i przekierowujemy do logowania (koniec zombie sesji).
        if (error instanceof InteractionRequiredAuthError) {
          msalInstance.setActiveAccount(null);
        }
        try {
          await redirectToLoginSafely();
        } catch {
          // Redirect failed or interaction already in progress — reject below.
        }
        return Promise.reject(
          new Error("Token acquisition required - redirecting to login")
        );
      }
    } else {
      // Don't add Authorization header - let the request proceed
      // Backend will return 401 and trigger error interceptor
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle 401 errors
axiosClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (isDemoModeActive()) {
      return Promise.reject(error);
    }

    const originalRequest = error.config;

    // If 401 Unauthorized
    if (error.response?.status === 401) {
      // Special case: /user/sync-b2c failed - token is invalid
      if (originalRequest.url?.includes("/user/sync-b2c")) {
        // Don't redirect here - ProtectedRoute will handle it when user becomes null
      }

      // For other endpoints, try to refresh token once
      if (!originalRequest._retry) {
        originalRequest._retry = true;

        const accounts = msalInstance.getAllAccounts();

        if (accounts.length > 0) {
          const account = msalInstance.getActiveAccount() || accounts[0];

          try {
            // Try to acquire a new token
            const response = await msalInstance.acquireTokenSilent({
              ...nativeSilentRequest,
              account: account,
              forceRefresh: true,
            });

            // Update the Authorization header with new token
            originalRequest.headers.Authorization = `Bearer ${response.accessToken}`;

            // Retry the original request
            return axiosClient(originalRequest);
          } catch {
            // Refresh token wygasł lub nieważny — sesja martwa.
            // Czyścimy active account i przekierowujemy do logowania.
            msalInstance.setActiveAccount(null);
            await redirectToLoginSafely();
            return Promise.reject(new Error("Session expired - redirecting to login"));
          }
        } else {
          // Brak kont — wymuś logowanie
          await redirectToLoginSafely();
        }
      }
    }

    return Promise.reject(error);
  }
);
