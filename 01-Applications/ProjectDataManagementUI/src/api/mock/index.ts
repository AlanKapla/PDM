// ============================================
//   Setup mock interceptors on axios instance
//   Odczytuje sessionStorage "demoMode"
// ============================================

import type { AxiosInstance, InternalAxiosRequestConfig } from "axios";
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

function applyMockAdapter(
  config: InternalAxiosRequestConfig,
  status: number,
  mockData: unknown
): void {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (config as any).adapter = () =>
    Promise.resolve({
      data: mockData,
      status,
      statusText: "OK (mock)",
      headers: { "content-type": "application/json" },
      config,
    });
}

export function setupMockInterceptors(instance: AxiosInstance): void {
  instance.interceptors.request.use(
    async (config) => {
      if (!isDemoModeActive()) {
        return config;
      }

      const url = `${config.baseURL || ""}${config.url || ""}`;

      if (!url.includes("/api/")) {
        return config;
      }

      try {
        const [status, mockData] = await handleMockRequest(
          config.method || "get",
          url,
          config.data
        );
        applyMockAdapter(config, status, mockData);
      } catch (err) {
        console.error("[PDMDemo] Mock handler error:", err);
        applyMockAdapter(config, 200, []);
      }

      return config;
    },
    (error) => Promise.reject(error)
  );
}
