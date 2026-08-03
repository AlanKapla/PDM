/**
 * Po hard kill PWA (szczególnie iOS) MSAL zostawia w localStorage / cookies
 * tymczasowe klucze requestu. Przy cold starcie handleRedirect / acquireTokenSilent
 * wiszą → wieczny spinner.
 *
 * Czyścimy TYLKO stan interakcji / requestu — konta i tokeny zostają.
 */

const TEMPORARY_KEY_FRAGMENTS: readonly string[] = [
  "interaction.status",
  "request.origin",
  "request.params",
  "request.native",
  "urlHash",
  "code.verifier",
];

function isTemporaryMsalKey(key: string): boolean {
  if (!key.includes("msal") && !key.startsWith("msal.")) {
    // MSAL często prefixuje clientId: `msal.<clientId>.interaction.status`
    // ale TemporaryCacheKeys bywają też bez "msal" w środku fragmentu — wymagamy msal.
    return false;
  }
  return TEMPORARY_KEY_FRAGMENTS.some((fragment) => key.includes(fragment));
}

function clearStorageTemporaryKeys(storage: Storage): void {
  const keysToRemove: string[] = [];
  for (let index = 0; index < storage.length; index++) {
    const key: string | null = storage.key(index);
    if (key !== null && isTemporaryMsalKey(key)) {
      keysToRemove.push(key);
    }
  }
  for (const key of keysToRemove) {
    try {
      storage.removeItem(key);
    } catch {
      // ignore
    }
  }
}

/** Cookies z storeAuthStateInCookie — tylko tymczasowy stan interakcji.
 * NIGDY nie ruszaj `msal.cache.encryption` — bez tego MSAL nie odszyfruje localStorage.
 */
function clearMsalAuthCookies(): void {
  try {
    const cookies: string[] = document.cookie.split(";");
    for (const raw of cookies) {
      const name: string = raw.split("=")[0]?.trim() ?? "";
      if (!name) {
        continue;
      }
      const lower: string = name.toLowerCase();
      if (lower.includes("cache.encryption")) {
        continue;
      }
      const isTemporary: boolean = TEMPORARY_KEY_FRAGMENTS.some((fragment) =>
        lower.includes(fragment)
      );
      if (!isTemporary) {
        continue;
      }
      document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/`;
      document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/;SameSite=None;Secure`;
    }
  } catch {
    // ignore
  }
}

export function clearStaleMsalInteraction(): void {
  try {
    clearStorageTemporaryKeys(localStorage);
  } catch {
    // ignore
  }
  try {
    clearStorageTemporaryKeys(sessionStorage);
  } catch {
    // ignore
  }
  clearMsalAuthCookies();
}
