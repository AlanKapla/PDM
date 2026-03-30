/**
 * Punkt wejścia warstwy danych demo.
 * Ustaw VITE_DEMO_MODE=true w .env.demo aby uruchomić aplikację bez backendu.
 */

export const isMockMode = (): boolean =>
  import.meta.env.VITE_DEMO_MODE === "true";
