import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { HubConnectionState } from "@microsoft/signalr";
import { axiosClient } from "../api/axiosClient";
import { notificationHubService } from "../services/notificationHubService";
import { technicalDocumentationHubService } from "../services/technicalDocumentationHubService";
import type { UserProfile } from "../types/auth.types";

interface AuthContextType {
  isAuthenticated: boolean;
  user: UserProfile | null;
  loading: boolean;
  refreshUser: () => Promise<void>;
  setIsAuthenticated: (value: boolean) => void;
  login: (email: string, password: string) => Promise<{ success: boolean; message?: string }> ;
  googleLogin: (token: string) => Promise<{ success: boolean; message?: string }> ;
  googleRegister: (token: string) => Promise<{ success: boolean; message?: string }> ;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextType>({
  isAuthenticated: false,
  user: null,
  loading: true,
  refreshUser: async () => {},
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
  }, [isAuthenticated, inProgress, user]); // Czekaj na MSAL initialization

  // ✅ SignalR init - startuje gdy isAuthenticated (NIE czekaj na user/me!)
  useEffect(() => {

    if (!isAuthenticated) {
      setSignalRInitialized(false);
      return;
    }

    // Jeśli już zainicjalizowane, nie rób ponownie
    if (signalRInitialized) {
      return;
    }

    // Sprawdź czy mamy account (token)
    const account = instance.getActiveAccount() || accounts[0];
    if (!account) {
      return;
    }

    // Oznacz jako inicjalizowane
    setSignalRInitialized(true);

    // Set resync callback (no cache needed - just log)
    notificationHubService.setAfterReconnect(async () => {
    });

    // Inicjalizacja: NAJPIERW connect, POTEM cache
    const initializeSignalR = async () => {
      try {
        // 1. Uruchom połączenie NAJPIERW (żeby nie tracić eventów)
        await notificationHubService.startConnection();
        await technicalDocumentationHubService.startConnection();
      } catch (error) {
        // Jeśli init failed, loguj w DEV i spróbuj ponownie za 5s
        if (import.meta.env.DEV) {
          console.error("Błąd inicjalizacji SignalR:", error);
        }
        setTimeout(() => {
          initializeSignalR().catch((retryError) => {
            if (import.meta.env.DEV) {
              console.error("Błąd retry inicjalizacji SignalR:", retryError);
            }
          });
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
          notificationHubService.forceRestart().catch((restartError) => {
            if (import.meta.env.DEV) {
              console.error("Błąd restartu SignalR po nieudanym ping:", restartError);
            }
          });
        }
      } else if (state === HubConnectionState.Disconnected || state === null) {
        notificationHubService.forceRestart().catch((restartError) => {
          if (import.meta.env.DEV) {
            console.error("Błąd restartu rozłączonego SignalR:", restartError);
          }
        });
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
        const state = notificationHubService.getConnectionState();
        
        if (state !== HubConnectionState.Connected) {
          try {
            await notificationHubService.forceRestart();
          } catch (error) {
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
    await instance.loginRedirect();
    return { success: true };
  };

  // Legacy Google login - now deprecated, use B2C Google provider
  const googleLogin = async (_token: string) => {
    await instance.loginRedirect();
    return { success: true };
  };

  // Legacy Google register - now deprecated, use B2C
  const googleRegister = async (_token: string) => {
    await instance.loginRedirect();
    return { success: true };
  };

  // Logout
  // Logout - clear state and redirect to MSAL logout
  const logout = async () => {
    
    // ✅ Zatrzymaj SignalR przed wylogowaniem
    try {
      await notificationHubService.stopConnection();
      await technicalDocumentationHubService.stopConnection();
    } catch (error) {
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
  };

  // Refresh user data from /user/me (po zmianie aktywnego tenanta)
  const refreshUser = async () => {
    if (!isAuthenticated) return;
    
    try {
      const response = await axiosClient.get("/user/me");
      setUser(response.data);
    } catch (error) {
    }
  };

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        user,
        loading,
        refreshUser,
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
