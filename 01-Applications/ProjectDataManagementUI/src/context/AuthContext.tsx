import { createContext, useEffect, useState, type ReactNode } from "react";
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { axiosClient } from "../api/axiosClient";

interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  systemRole: string;
  activeTenantId?: string;
}

interface AuthContextType {
  isAuthenticated: boolean;
  user: UserProfile | null;
  loading: boolean;
  setIsAuthenticated: (value: boolean) => void;
  login: (email: string, password: string) => Promise<{ success: boolean; message?: string }>;
  googleLogin: (token: string) => Promise<{ success: boolean; message?: string }>;
  googleRegister: (token: string) => Promise<{ success: boolean; message?: string }>;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextType>({
  isAuthenticated: false,
  user: null,
  loading: true,
  setIsAuthenticated: () => {},
  login: async () => ({ success: false }),
  googleLogin: async () => ({ success: false }),
  googleRegister: async () => ({ success: false }),
  logout: async () => {},
});

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const { instance, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);

  // Fetch user profile when authenticated
  useEffect(() => {
    let isMounted = true; // Prevent state updates on unmounted component

    const fetchUserProfile = async () => {
      // Not authenticated - clear state and finish
      if (!isAuthenticated) {
        console.log("🔴 AuthContext: Not authenticated");
        if (isMounted) {
          setUser(null);
          setLoading(false);
        }
        return;
      }

      // Already have user - don't fetch again
      if (user) {
        console.log("✅ AuthContext: User already loaded");
        if (isMounted) setLoading(false);
        return;
      }

      // Start fetching
      console.log("🟢 AuthContext: Fetching user profile...");
      if (isMounted) setLoading(true);
      
      try {
        // Sync user from B2C
        await axiosClient.post("/user/sync-b2c");
        
        // Fetch user details
        const response = await axiosClient.get("/user/me");
        console.log("✅ AuthContext: User profile loaded");
        
        if (isMounted) {
          setUser(response.data);
          setLoading(false);
        }
      } catch (error: any) {
        console.error("❌ AuthContext: Error fetching user:", error);
        if (isMounted) {
          setUser(null);
          setLoading(false);
        }
      }
    };

    fetchUserProfile();

    return () => {
      isMounted = false; // Cleanup
    };
  }, [isAuthenticated]); // Only re-run when authentication state changes

  // Legacy login method - now deprecated, redirects to B2C
  const login = async (_email: string, _password: string) => {
    console.warn("Legacy login method called. Redirecting to Azure AD B2C...");
    await instance.loginRedirect();
    return { success: true };
  };

  // Legacy Google login - now deprecated, use B2C Google provider
  const googleLogin = async (_token: string) => {
    console.warn("Legacy Google login called. Redirecting to Azure AD B2C...");
    await instance.loginRedirect();
    return { success: true };
  };

  // Legacy Google register - now deprecated, use B2C
  const googleRegister = async (_token: string) => {
    console.warn("Legacy Google register called. Redirecting to Azure AD B2C...");
    await instance.loginRedirect();
    return { success: true };
  };

  // Logout
  // Logout - clear state and redirect to MSAL logout
  const logout = async () => {
    console.log("🚪 Logging out...");
    
    // Clear app state
    setUser(null);
    
    // Clear app storage (MSAL will handle its own cache)
    Object.keys(localStorage).forEach(key => {
      if (!key.startsWith('msal.')) {
        localStorage.removeItem(key);
      }
    });
    sessionStorage.clear();
    
    // Redirect to MSAL logout (will clear MSAL cache and redirect to Azure logout)
    const account = instance.getActiveAccount() || accounts[0];
    await instance.logoutRedirect({ account });
  };

  const setIsAuthenticated = (_value: boolean) => {
    console.warn("setIsAuthenticated is deprecated with Azure AD B2C");
  };

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        user,
        loading,
        setIsAuthenticated,
        login,
        googleLogin,
        googleRegister,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
