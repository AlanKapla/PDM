import React from "react";
import ReactDOM from "react-dom/client";
import { ChakraProvider, ColorModeScript } from "@chakra-ui/react";
import App from "./App.tsx";
import theme from "./theme.ts";
import { GoogleOAuthProvider } from "@react-oauth/google";

// Register Service Worker for PWA
if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker
      .register('/sw.js')
      .then((registration) => {
        console.log('SW registered: ', registration);
      })
      .catch((registrationError) => {
        console.log('SW registration failed: ', registrationError);
      });
  });
}

// 👇 Nie montujemy Reacta na /swagger – tam działa osobno Swagger UI z backendu
if (!window.location.pathname.startsWith("/swagger")) {
  ReactDOM.createRoot(document.getElementById("root")!).render(
    <React.StrictMode>
      <GoogleOAuthProvider clientId="644147776869-f671l471e26q29cvnfpcqvkncm8h47m2.apps.googleusercontent.com">
        <ChakraProvider theme={theme}>
          <ColorModeScript initialColorMode={theme.config.initialColorMode} />
          <App />
        </ChakraProvider>
      </GoogleOAuthProvider>
    </React.StrictMode>
  );
}
