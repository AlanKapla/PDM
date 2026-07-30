/**
 * Po zabiciu / zminimalizowaniu aplikacji na mobile MSAL może zostawić
 * `*.interaction.status` w localStorage (cacheLocation: localStorage).
 * Przy kolejnym starcie PCA uważa, że interakcja trwa → acquireTokenSilent /
 * handleRedirect wiszą, a UI kręci spinerem w nieskończoność.
 *
 * Czyścimy tylko status interakcji — konta i tokeny zostają.
 */
export function clearStaleMsalInteraction(): void {
  const storages: Storage[] = [];
  try {
    storages.push(localStorage);
  } catch {
    // ignore
  }
  try {
    storages.push(sessionStorage);
  } catch {
    // ignore
  }

  for (const storage of storages) {
    const keysToRemove: string[] = [];
    for (let index = 0; index < storage.length; index++) {
      const key: string | null = storage.key(index);
      if (key !== null && key.includes("interaction.status")) {
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
}
