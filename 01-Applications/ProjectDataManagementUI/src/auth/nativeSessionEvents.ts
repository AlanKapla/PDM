/** Event: Native Auth ustawił konto w PCA — AuthContext ma przeliczyć isAuthenticated. */
export const NATIVE_SESSION_READY_EVENT = "pdm:native-session-ready";

export function notifyNativeSessionReady(): void {
  window.dispatchEvent(new CustomEvent(NATIVE_SESSION_READY_EVENT));
}
