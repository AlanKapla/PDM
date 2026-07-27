import React from "react";
import ReactDOM from "react-dom/client";
import "./index.css";
import { ChakraProvider} from "@chakra-ui/react";
import { MsalProvider } from "@azure/msal-react";
import { EventType } from "@azure/msal-browser";
import type { EventMessage, AuthenticationResult } from "@azure/msal-browser";
import App from "./App.tsx";
import theme from "./theme.ts";
import { initializeMsalInstance, msalInstance } from "./auth/msalInstance";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,        // dane świeże przez 5 minut
      gcTime: 10 * 60 * 1000,          // garbage collect po 10 minutach
      retry: 1,                         // 1 retry przy błędzie
      refetchOnWindowFocus: false,      // nie refetchuj przy focus okna
    },
    mutations: {
      retry: 0,                         // mutacje bez retry
    },
  },
});

export { queryClient };

// Initialize MSAL and render app
async function initializeApp() {
  // Jedna CustomAuth PCA (native + redirect + axios) — musi być przed MsalProvider
  await initializeMsalInstance();

  // handleRedirectPromise: wywoływane przez MsalProvider przy mount.
  // AuthCallback tylko czeka na inProgress === None i nawiguje dalej.

  // Default to using the first account if no account is active on page load
  if (!msalInstance.getActiveAccount() && msalInstance.getAllAccounts().length > 0) {
    msalInstance.setActiveAccount(msalInstance.getAllAccounts()[0]);
  }

  // Optional - This will update account state if a user signs in from another tab or window
  msalInstance.enableAccountStorageEvents();

  // Listen for sign-in event and set active account
  msalInstance.addEventCallback((event: EventMessage) => {
    
    if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
      const payload = event.payload as AuthenticationResult;
      const account = payload.account;
      msalInstance.setActiveAccount(account);
    }
    
    if (event.eventType === EventType.LOGIN_FAILURE) {
    }
    
    if (event.eventType === EventType.ACQUIRE_TOKEN_FAILURE) {
    }
    
    // NIE restartuj SignalR przy odświeżeniu tokenu - powoduje niestabilność
    // SignalR automatycznie pobierze świeży token przez accessTokenFactory przy:
    // - negotiate endpoint
    // - automatycznym reconnect (gdy backend zwróci 401/403)
    
    if (event.eventType === EventType.HANDLE_REDIRECT_END) {
    }
    
    if (event.eventType === EventType.HANDLE_REDIRECT_START) {
    }
  });

  // 🚫 Nie uruchamiamy Reacta na ścieżce /swagger
  if (!window.location.pathname.startsWith("/swagger")) {
    const root = document.getElementById("root");

    if (root) {
      ReactDOM.createRoot(root).render(
        <QueryClientProvider client={queryClient}>
          <MsalProvider instance={msalInstance}>
            <ChakraProvider
              theme={theme}
              toastOptions={{
                defaultOptions: {
                  position: "top-right",
                  isClosable: true,
                },
              }}
            >
              <App />
            </ChakraProvider>
          </MsalProvider>
          {import.meta.env.DEV && (
            <ReactQueryDevtools initialIsOpen={false} />
          )}
        </QueryClientProvider>
      );
    } else {
    }
  }
}

// Service worker tylko w produkcji — w dev cache SW powoduje konflikt z HMR
// (duplicate React, invalid hook call przy mieszaniu starych i nowych chunków).
if (!import.meta.env.DEV && "serviceWorker" in navigator) {
  window.addEventListener("load", () => {
    navigator.serviceWorker
      .register("/sw.js")
      .then((registration) => {
        registration.update();
      })
      .catch((error) => {
        console.error("Service worker registration failed:", error);
      });
  });
} else if (import.meta.env.DEV && "serviceWorker" in navigator) {
  void navigator.serviceWorker.getRegistrations().then((registrations) => {
    registrations.forEach((registration) => {
      void registration.unregister();
    });
  });
}

// Expose for dev debugging in browser console
if (import.meta.env.DEV) {
  (window as any).msalInstance = msalInstance;
  
  // Diagnostyka SignalR - importuj service dynamicznie żeby uniknąć circular dependency
  (window as any).signalRDiag = async () => {
    const { notificationHubService: service } = await import("./services/notificationHubService");
    return {
      backendUserId: await service.testConnection()
    };
  };
}

// Start the app
initializeApp().catch((error) => {
  // Logujemy błąd inicjalizacji w DEV, aby ułatwić diagnostykę
  if (import.meta.env.DEV) {
    console.error("Błąd inicjalizacji aplikacji:", error);
  }
});
