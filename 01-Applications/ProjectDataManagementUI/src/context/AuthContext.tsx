import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { HubConnectionState } from "@microsoft/signalr";
import { axiosClient } from "../api/axiosClient";
import { notificationHubService } from "../services/notificationHubService";
import type { UserProfile } from "../types/auth.types";

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
  const { instance, accounts, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  
  // Flaga zapobiegająca wielokrotnej inicjalizacji SignalR
  const [signalRInitialized, setSignalRInitialized] = useState(false);

  // Fetch user profile when authenticated
  useEffect(() => {
    let isMounted = true;

    const fetchUserProfile = async () => {
      // Czekaj aż MSAL zakończy inicjalizację
      if (inProgress !== "none") {
        return;
      }

      if (!isAuthenticated) {
        if (isMounted) {
          setUser(null);
          setLoading(false);
        }
        return;
      }

      // Already have user - skip fetch
      if (user) {
        if (isMounted) setLoading(false);
        return;
      }

      // Start fetching
      if (isMounted) setLoading(true);
      
      try {
        await axiosClient.post("/user/sync-b2c");
        const response = await axiosClient.get("/user/me");
        
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
      isMounted = false;
    };
  }, [isAuthenticated, inProgress]); // Czekaj na MSAL initialization

  // ✅ SignalR init - startuje gdy isAuthenticated (NIE czekaj na user/me!)
  useEffect(() => {
    console.log("AUTH state", { 
      isAuthenticated, 
      hasUser: !!user, 
      accountCount: accounts.length,
      signalRInitialized
    });

    if (!isAuthenticated) {
      setSignalRInitialized(false);
      return;
    }

    // Jeśli już zainicjalizowane, nie rób ponownie
    if (signalRInitialized) {
      console.log("✅ SignalR already initialized, skipping");
      return;
    }

    // Sprawdź czy mamy account (token)
    const account = instance.getActiveAccount() || accounts[0];
    if (!account) {
      console.warn("⚠️ No MSAL account available yet");
      return;
    }

    // Oznacz jako inicjalizowane
    setSignalRInitialized(true);
    console.log("🚀 Initializing SignalR for the first time...");

    // Ustaw callback resync po reconnect
    notificationHubService.setAfterReconnect(async () => {
      try {
        console.log("🔄 SignalR resync after reconnect...");
        const response = await axiosClient.get("/Notification/unread");
        await notificationHubService.initializeCache(response.data);
        console.log("✅ SignalR resync completed");
      } catch (error) {
        console.error("❌ SignalR resync failed:", error);
      }
    });

    // Inicjalizacja: NAJPIERW connect, POTEM cache
    const initializeSignalR = async () => {
      try {
        // 1. Uruchom połączenie NAJPIERW (żeby nie tracić eventów)
        console.log("🔌 Starting SignalR connection (before cache)...");
        await notificationHubService.startConnection();
        console.log("✅ SignalR connected");

        // 2. Dopiero teraz pobierz i zainicjalizuj cache
        console.log("💾 Loading initial notifications cache...");
        const response = await axiosClient.get("/Notification/unread");
        await notificationHubService.initializeCache(response.data);
        console.log("✅ SignalR cache initialized:", response.data.length, "notifications");
      } catch (error) {
        console.error("❌ Failed to initialize SignalR:", error);
        // Jeśli init failed, spróbuj ponownie za 5s
        setTimeout(() => {
          console.log("🔄 Retrying SignalR init...");
          initializeSignalR().catch(e => console.error("❌ Retry failed:", e));
        }, 5000);
      }
    };

    initializeSignalR();

    return () => {
      // Cleanup - NIE stopuj SignalR (singleton)
    };
  }, [isAuthenticated, accounts.length]); // <-- BEZ user! Start wcześniej

  // ✅ Health check - ping co 15s + force restart przy fail
  useEffect(() => {
    if (!isAuthenticated) return;

    // Ping co 15s (lepiej niż 60s do wykrywania problemów)
    const pingInterval = setInterval(async () => {
      const state = notificationHubService.getConnectionState();
      
      if (state === HubConnectionState.Connected) {
        try {
          await notificationHubService.ping();
        } catch (error) {
          console.warn("⚠️ SignalR ping failed -> force restart");
          notificationHubService.forceRestart().catch(e => 
            console.error("❌ Force restart failed:", e)
          );
        }
      } else if (state === HubConnectionState.Disconnected || state === null) {
        console.warn("⚠️ SignalR disconnected -> force restart");
        notificationHubService.forceRestart().catch(e =>
          console.error("❌ Force restart failed:", e)
        );
      }
    }, 15000); // 15s

    return () => {
      clearInterval(pingInterval);
    };
  }, [isAuthenticated]);

  // ✅ Sprawdź połączenie gdy użytkownik wraca do karty
  useEffect(() => {
    if (!isAuthenticated) return;

    const handleVisibilityChange = async () => {
      if (!document.hidden) {
        console.log("👁️ Tab visible again, checking SignalR...");
        const state = notificationHubService.getConnectionState();
        
        if (state !== HubConnectionState.Connected) {
          console.warn("⚠️ SignalR not connected (state:", state, ") -> force restart");
          try {
            await notificationHubService.forceRestart();
          } catch (error) {
            console.error("❌ Failed to restart SignalR:", error);
          }
        }
      }
    };

    document.addEventListener("visibilitychange", handleVisibilityChange);

    return () => {
      document.removeEventListener("visibilitychange", handleVisibilityChange);
    };
  }, [isAuthenticated]);

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
    
    // ✅ Zatrzymaj SignalR przed wylogowaniem
    console.log("🔔 Stopping SignalR connection...");
    try {
      await notificationHubService.stopConnection();
      notificationHubService.clearCache();
    } catch (error) {
      console.error("❌ Error stopping SignalR:", error);
    }
    
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

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
