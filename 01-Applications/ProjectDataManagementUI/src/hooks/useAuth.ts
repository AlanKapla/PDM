import { useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import type { AccountInfo } from "@azure/msal-browser";
import { loginRequest, silentRequest } from "../config/authConfig";
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
 * Custom hook for Azure AD B2C authentication using MSAL
 */
export const useAuth = (): UseAuthReturn => {
  const { instance, accounts, inProgress } = useMsal();
  const [isLoading, setIsLoading] = useState(true);

  const activeAccount = instance.getActiveAccount();
  const isAuthenticated = accounts.length > 0 && activeAccount !== null;

  useEffect(() => {
    // Set loading to false when interaction is complete
    if (inProgress === InteractionStatus.None) {
      setIsLoading(false);
    }
  }, [inProgress]);

  /**
   * Initiates the login flow using redirect
   */
  const login = async (): Promise<void> => {
    try {
      setIsLoading(true);
      await instance.loginRedirect(loginRequest);
    } catch (error) {
      console.error("Login error:", error);
      setIsLoading(false);
      throw error;
    }
  };

  /**
   * Logs out the current user
   */
  const logout = async (): Promise<void> => {
    try {
      setIsLoading(true);
      await instance.logoutRedirect({
        account: activeAccount,
      });
    } catch (error) {
      console.error("Logout error:", error);
      setIsLoading(false);
      throw error;
    }
  };

  /**
   * Acquires an access token silently, or falls back to interactive login
   */
  const getAccessToken = async (): Promise<string | null> => {
    if (!activeAccount) {
      console.warn("No active account found. User must login first.");
      return null;
    }

    try {
      // Try to acquire token silently
      const response = await instance.acquireTokenSilent({
        ...silentRequest,
        account: activeAccount,
      });

      return response.accessToken;
    } catch (error) {
      console.warn("Silent token acquisition failed. Attempting redirect...", error);

      try {
        // If silent acquisition fails, try redirect
        await instance.acquireTokenRedirect({
          ...silentRequest,
          account: activeAccount,
        });
        return null; // Redirect will refresh the page
      } catch (redirectError) {
        console.error("Token acquisition failed:", redirectError);
        throw redirectError;
      }
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
