import React from "react";
import ReactDOM from "react-dom/client";
import { ChakraProvider} from "@chakra-ui/react";
import { MsalProvider } from "@azure/msal-react";
import { PublicClientApplication, EventType } from "@azure/msal-browser";
import type { EventMessage, AuthenticationResult } from "@azure/msal-browser";
import App from "./App.tsx";
import theme from "./theme.ts";
import { msalConfig } from "./config/authConfig";

// Initialize MSAL instance
const msalInstance = new PublicClientApplication(msalConfig);

// Initialize MSAL and render app
async function initializeApp() {
  // Initialize MSAL before using it
  await msalInstance.initialize();

  // Note: handleRedirectPromise is now handled in AuthCallback component
  // This ensures proper routing after OAuth code exchange

  // Default to using the first account if no account is active on page load
  if (!msalInstance.getActiveAccount() && msalInstance.getAllAccounts().length > 0) {
    msalInstance.setActiveAccount(msalInstance.getAllAccounts()[0]);
  }

  // Optional - This will update account state if a user signs in from another tab or window
  msalInstance.enableAccountStorageEvents();

  // Listen for sign-in event and set active account
  msalInstance.addEventCallback((event: EventMessage) => {
    console.log("🎯 MSAL Event:", event.eventType, event);
    
    if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
      const payload = event.payload as AuthenticationResult;
      const account = payload.account;
      msalInstance.setActiveAccount(account);
      console.log("✅ Login success event:", account);
    }
    
    if (event.eventType === EventType.LOGIN_FAILURE) {
      console.error("❌ Login failure event:", event);
    }
    
    if (event.eventType === EventType.ACQUIRE_TOKEN_FAILURE) {
      console.error("❌ Acquire token failure event:", event);
    }
    
    // NIE restartuj SignalR przy odświeżeniu tokenu - powoduje niestabilność
    // SignalR automatycznie pobierze świeży token przez accessTokenFactory przy:
    // - negotiate endpoint
    // - automatycznym reconnect (gdy backend zwróci 401/403)
    
    if (event.eventType === EventType.HANDLE_REDIRECT_END) {
      console.log("🏁 Handle redirect end event:", event);
    }
    
    if (event.eventType === EventType.HANDLE_REDIRECT_START) {
      console.log("🚦 Handle redirect start event:", event);
    }
  });

  // 🚫 Nie uruchamiamy Reacta na ścieżce /swagger
  if (!window.location.pathname.startsWith("/swagger")) {
    const root = document.getElementById("root");

    if (root) {
      ReactDOM.createRoot(root).render(
        <React.StrictMode>
          <MsalProvider instance={msalInstance}>
            <ChakraProvider theme={theme}>
              <App />
            </ChakraProvider>
          </MsalProvider>
        </React.StrictMode>
      );
    } else {
      console.error("Nie znaleziono elementu #root – React nie został zamontowany.");
    }
  }
}

// Register Service Worker for PWA
if ("serviceWorker" in navigator) {
  window.addEventListener("load", () => {
    navigator.serviceWorker
      .register("/sw.js")
      .then((reg) => console.log("Service Worker registered:", reg))
      .catch((err) => console.error("Service Worker registration failed:", err));
  });
}

// Expose for dev debugging in browser console
if (import.meta.env.DEV) {
  (window as any).msalInstance = msalInstance;
  
  // Diagnostyka SignalR - importuj service dynamicznie żeby uniknąć circular dependency
  (window as any).signalRDiag = async () => {
    const { notificationHubService: service } = await import("./services/notificationHubService");
    return {
      ...service.getConnectionDiagnostics(),
      backendUserId: await service.testConnection()
    };
  };
  
  console.log("🛠 Dev mode: use window.signalRDiag() to check SignalR connection");
}

// Start the app
initializeApp().catch(console.error);

// Export msalInstance for use in other modules (e.g., axios interceptors)
export { msalInstance };
