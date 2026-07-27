import { useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import type { AccountInfo } from "@azure/msal-browser";
import { silentRequest } from "../config/authConfig";
import { useEffect, useState } from "react";

export interface UseAuthReturn {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: AccountInfo | null;
  login: () => Promise<void>;
  logout: () => Promise<void>;
  getAccessToken: () => Promise<string | null>;
}

/**
 * Custom hook for native / MSAL session helpers.
 */
export const useAuth = (): UseAuthReturn => {
  const { instance, accounts, inProgress } = useMsal();
  const [isLoading, setIsLoading] = useState(true);

  const activeAccount = instance.getActiveAccount();
  const isAuthenticated = accounts.length > 0 && activeAccount !== null;

  useEffect(() => {
    if (inProgress === InteractionStatus.None) {
      setIsLoading(false);
    }
  }, [inProgress]);

  const login = async (): Promise<void> => {
    window.location.assign("/login");
  };

  const logout = async (): Promise<void> => {
    try {
      setIsLoading(true);
      const { logoutMsalSession } = await import("../auth/logoutSession");
      const { msalInstance } = await import("../auth/msalInstance");
      await logoutMsalSession(msalInstance, activeAccount);
    } catch (error) {
      setIsLoading(false);
      throw error;
    }
  };

  const getAccessToken = async (): Promise<string | null> => {
    if (!activeAccount) {
      return null;
    }

    try {
      const response = await instance.acquireTokenSilent({
        ...silentRequest,
        account: activeAccount,
      });
      return response.accessToken;
    } catch {
      window.location.assign("/login");
      return null;
    }
  };

  return {
    isAuthenticated,
    isLoading: isLoading || inProgress !== InteractionStatus.None,
    user: activeAccount,
    login,
    logout,
    getAccessToken,
  };
};
