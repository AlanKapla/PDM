// ============================================
//   Setup mock interceptors on axios instance
//   Odczytuje sessionStorage "demoMode"
// ============================================

import type { AxiosInstance } from "axios";
import { handleMockRequest } from "./mockHandlers";

const STORAGE_KEY = "demoMode";

export function isDemoModeActive(): boolean {
  return sessionStorage.getItem(STORAGE_KEY) === "true";
}

export function setDemoMode(active: boolean): void {
  if (active) {
    sessionStorage.setItem(STORAGE_KEY, "true");
  } else {
    sessionStorage.removeItem(STORAGE_KEY);
  }
}

export function setupMockInterceptors(instance: AxiosInstance): void {
  // Request interceptor — przechwytuje żądanie, zwraca mockowaną odpowiedź
  instance.interceptors.request.use(
    async (config) => {
      // Demo mode nieaktywny — przepuść normalnie
      if (!isDemoModeActive()) {
        return config;
      }

      const url = `${config.baseURL || ""}${config.url || ""}`;

      // Interceptuj tylko zapytania do naszego API (ścieżka zaczyna się od /api/)
      // Działa zarówno dla dev (https://localhost:5001/api/...) 
      // jak i prod (/api/...) oraz Vite proxy
      if (!url.includes("/api/")) {
        return config;
      }

      try {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const [status, mockData] = await handleMockRequest(
          config.method || "get",
          url,
          config.data
        );

        // Zwracamy odpowiedź bez wysyłania zapytania do sieci
        // Poprzez podmianę adaptera na funkcję, która natychmiast zwraca mock
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        (config as any).adapter = () => {
          return Promise.resolve({
            data: mockData,
            status,
            statusText: "OK (mock)",
            headers: { "content-type": "application/json" },
            config,
          });
        };
      } catch (err) {
        console.error("[PDMDemo] Mock handler error:", err);
      }

      return config;
    },
    (error) => Promise.reject(error)
  );

  console.log("[PDMDemo] Mock interceptors registered.");
}
