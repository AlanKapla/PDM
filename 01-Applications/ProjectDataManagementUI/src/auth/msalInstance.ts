import { PublicClientApplication } from "@azure/msal-browser";
import { msalConfig } from "../config/authConfig";

/**
 * Singleton MSAL — osobny moduł, żeby uniknąć circular import
 * `main.tsx` → App → AuthContext → axiosClient → main.tsx
 * (psuje HMR Vite i może zostawiać niespójny stan sesji).
 */
export const msalInstance = new PublicClientApplication(msalConfig);
