import type {
  INetworkModule,
  NetworkRequestOptions,
  NetworkResponse,
} from "@azure/msal-common/browser";
import { resolveNativeAuthProxyUrl } from "./resolveNativeAuthProxyUrl";

/**
 * Refresh token z native auth (password) + Origin na ciamlogin.com → AADSTS9002326.
 * Wszystkie POST /oauth2/v2.0/token idą przez ten sam proxy co authApiProxyUrl
 * (na Azure: API `/api/native-auth`, nie SWA `/native-auth`).
 */
function rewriteTokenUrlIfNeeded(url: string): string {
  try {
    const parsed: URL = new URL(url);
    const isCiamToken: boolean =
      parsed.hostname.endsWith(".ciamlogin.com") &&
      parsed.pathname.includes("/oauth2/v2.0/token");
    if (!isCiamToken) {
      return url;
    }
    return `${resolveNativeAuthProxyUrl()}/oauth2/v2.0/token${parsed.search}`;
  } catch {
    return url;
  }
}

async function parseJsonBody<T>(response: Response): Promise<T> {
  const text: string = await response.text();
  if (!text) {
    return {} as T;
  }
  try {
    return JSON.parse(text) as T;
  } catch {
    // Proxy / SW czasem zwraca HTML 200 — nie wywalaj całego flow wyjątkami JSON.
    return {
      error: "invalid_json_response",
      error_description: `Non-JSON response (HTTP ${response.status}): ${text.slice(0, 120)}`,
    } as T;
  }
}

export const nativeAuthNetworkClient: INetworkModule = {
  async sendGetRequestAsync<T>(
    url: string,
    options?: NetworkRequestOptions
  ): Promise<NetworkResponse<T>> {
    const response: Response = await fetch(url, {
      method: "GET",
      headers: options?.headers,
    });
    return {
      headers: Object.fromEntries(response.headers.entries()),
      body: await parseJsonBody<T>(response),
      status: response.status,
    };
  },

  async sendPostRequestAsync<T>(
    url: string,
    options?: NetworkRequestOptions
  ): Promise<NetworkResponse<T>> {
    const targetUrl: string = rewriteTokenUrlIfNeeded(url);
    const response: Response = await fetch(targetUrl, {
      method: "POST",
      headers: options?.headers,
      body: options?.body,
    });
    return {
      headers: Object.fromEntries(response.headers.entries()),
      body: await parseJsonBody<T>(response),
      status: response.status,
    };
  },
};
