import { isDemoModeActive } from "../api/mock";
import { msalInstance } from "../auth/msalInstance";

/** Demo bez konta MSAL — API mockowane, SignalR wyłączony. */
export function isDemoOnlySession(): boolean {
  return isDemoModeActive() && msalInstance.getAllAccounts().length === 0;
}
