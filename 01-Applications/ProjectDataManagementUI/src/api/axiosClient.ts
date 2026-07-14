import axios from "axios";
import { InteractionRequiredAuthError } from "@azure/msal-browser";
import { msalInstance } from "../main";
import { silentRequest } from "../config/authConfig";
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

// Zapobiega wielokrotnym równoległym wywołaniom loginRedirect.
// Bez tego kilka równoczesnych żądań API może zawiesić flagę MSAL
// `interaction_in_progress` w localStorage i trwale zablokować aplikację
// na ekranie "Sprawdzanie sesji...".
let interactiveRedirectTriggered = false;

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

    const accounts = msalInstance.getAllAccounts();
    
    if (accounts.length > 0) {
      const account = msalInstance.getActiveAccount() || accounts[0];
      
      try {
        // Try to acquire token silently from cache
        const response = await msalInstance.acquireTokenSilent({
          ...silentRequest,
          account: account,
        });
        
        // Add token to Authorization header
        config.headers.Authorization = `Bearer ${response.accessToken}`;
      } catch (error: any) {
        // Tylko InteractionRequiredAuthError oznacza, że użytkownik musi się zalogować interaktywnie.
        // Inne błędy (np. sieciowe) nie powinny wymuszać przekierowania do logowania.
        if (error instanceof InteractionRequiredAuthError) {
          if (!interactiveRedirectTriggered) {
            interactiveRedirectTriggered = true;
            await msalInstance.loginRedirect(silentRequest);
          }
          return Promise.reject(new Error("Token acquisition required - redirecting to login"));
        }
        // Dla innych błędów odrzuć żądanie bez przekierowania
        return Promise.reject(error);
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
    const originalRequest = error.config;

    // If 401 Unauthorized
    if (error.response?.status === 401) {
      // Special case: /user/sync-b2c failed - token is invalid
      if (originalRequest.url?.includes('/user/sync-b2c')) {
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
              ...silentRequest,
              account: account,
              forceRefresh: true, // Force refresh to get a new token
            });
            
            // Update the Authorization header with new token
            originalRequest.headers.Authorization = `Bearer ${response.accessToken}`;
            
            // Retry the original request
            return axiosClient(originalRequest);
          } catch (tokenError) {
            // Don't redirect - ProtectedRoute will handle when user is null
            return Promise.reject(tokenError);
          }
        } else {
          // No accounts - token acquisition failed
          // Don't redirect - ProtectedRoute will handle it
        }
      }
    }

    return Promise.reject(error);
  }
);