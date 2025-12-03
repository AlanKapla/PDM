import React from "react";
import ReactDOM from "react-dom/client";
import { ChakraProvider} from "@chakra-ui/react";
import App from "./App.tsx";
import theme from "./theme.ts";
import { GoogleOAuthProvider } from "@react-oauth/google";

// Register Service Worker for PWA
if ("serviceWorker" in navigator) {
  window.addEventListener("load", () => {
    navigator.serviceWorker
      .register("/sw.js")
      .then((reg) => console.log("Service Worker registered:", reg))
      .catch((err) => console.error("Service Worker registration failed:", err));
  });
}

// 🚫 Nie uruchamiamy Reacta na ścieżce /swagger
if (!window.location.pathname.startsWith("/swagger")) {
  const root = document.getElementById("root");

  if (root) {
    ReactDOM.createRoot(root).render(
      <React.StrictMode>
        <GoogleOAuthProvider clientId="644147776869-f671l471e26q29cvnfpcqvkncm8h47m2.apps.googleusercontent.com">
          <ChakraProvider theme={theme}>
            <App />
          </ChakraProvider>
        </GoogleOAuthProvider>
      </React.StrictMode>
    );
  } else {
    console.error("Nie znaleziono elementu #root — React nie został zamontowany.");
  }
}
