// ============================================
//   Setup mock interceptors on axios instance
//   Odczytuje sessionStorage "demoMode"
// ============================================

import type { AxiosInstance, AxiosResponseHeaders, InternalAxiosRequestConfig } from "axios";
import { handleMockRequest, type MockResponse } from "./mockHandlers";

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
  mockData: unknown,
  responseHeaders?: Record<string, string>
): void {
  const headers: Record<string, string> = {
    "content-type": "application/json",
    ...responseHeaders,
  };

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (config as any).adapter = () =>
    Promise.resolve({
      data: mockData,
      status,
      statusText: "OK (mock)",
      headers: headers as AxiosResponseHeaders,
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
        const mockResult: MockResponse = await handleMockRequest(
          config.method || "get",
          url,
          config.data
        );
        const status: number = mockResult[0];
        const mockData: unknown = mockResult[1];
        const responseHeaders: Record<string, string> | undefined = mockResult[2];
        applyMockAdapter(config, status, mockData, responseHeaders);
      } catch (err) {
        console.error("[PDMDemo] Mock handler error:", err);
        applyMockAdapter(config, 200, []);
      }

      return config;
    },
    (error) => Promise.reject(error)
  );
}
