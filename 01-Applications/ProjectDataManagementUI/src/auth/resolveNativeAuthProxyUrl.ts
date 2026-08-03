/**
 * Same-origin / API reverse proxy for Entra Native Auth (no CORS on ciamlogin).
 * Must be identical for Custom Auth `authApiProxyUrl` and token URL rewrites
 * in `nativeAuthNetworkClient` — otherwise initiate/challenge/token succeed via API,
 * then getAccessToken POSTs to SWA `/native-auth` (405 / HTML) and login never redirects.
 */
export function resolveNativeAuthProxyUrl(): string {
  const configured: string | undefined = import.meta.env.VITE_NATIVE_AUTH_PROXY_URL;
  if (configured) {
    return configured.replace(/\/$/, "");
  }

  if (import.meta.env.DEV) {
    return `${window.location.origin}/native-auth`;
  }

  const apiBase: string | undefined = import.meta.env.VITE_API_BASE_URL;
  if (!apiBase) {
    return `${window.location.origin}/native-auth`;
  }

  const base: string = apiBase.replace(/\/$/, "");

  if (/^https?:\/\//i.test(base)) {
    if (base.endsWith("/api")) {
      return `${base}/native-auth`;
    }
    return `${base}/api/native-auth`;
  }

  if (base === "/api" || base.endsWith("/api")) {
    return `${window.location.origin}${base}/native-auth`;
  }

  return `${window.location.origin}/api/native-auth`;
}
