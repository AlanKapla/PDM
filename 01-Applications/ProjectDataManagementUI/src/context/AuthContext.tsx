import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { useQueryClient } from "@tanstack/react-query";
import { HubConnectionState } from "@microsoft/signalr";
import { axiosClient } from "../api/axiosClient";
import { activityApi } from "../api/activityApi";
import { isDemoModeActive, setDemoMode } from "../api/mock";
import { notificationHubService } from "../services/notificationHubService";
import { chatHubService } from "../services/chatHubService";
import { logoutMsalSession } from "../auth/logoutSession";
import { msalInstance } from "../auth/msalInstance";
import type { UserProfile } from "../types/auth.types";

const LOGIN_ACTIVITY_RECORDED_KEY = "pdm:loginActivityRecorded";

function recordLoginActivityOnce(): void {
  if (sessionStorage.getItem(LOGIN_ACTIVITY_RECORDED_KEY)) {
    return;
  }
  void activityApi
    .recordLogin({ route: window.location.pathname })
    .catch(() => {});
  sessionStorage.setItem(LOGIN_ACTIVITY_RECORDED_KEY, "1");
}


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
  const queryClient = useQueryClient();
  const isAuthenticated = useIsAuthenticated();
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  
  // Flaga zapobiegająca wielokrotnej inicjalizacji SignalR
  const [signalRInitialized, setSignalRInitialized] = useState(false);

  // Fetch user profile when authenticated (MSAL) or in demo mode without login
  useEffect(() => {
    let isMounted = true;
    let fetchInFlight = false;

    const fetchUserProfile = async (force = false) => {
      // MSAL busy — nie blokuj UI na zawsze: zostaw loading tylko jeśli jeszcze
      // nie mamy profilu; gdy interakcja się skończy, effect odpali się ponownie.
      if (inProgress !== InteractionStatus.None) {
        if (isMounted && user) {
          setLoading(false);
        }
        return;
      }

      const demoActive = isDemoModeActive();
      const canLoadProfile = isAuthenticated || demoActive;

      if (!canLoadProfile) {
        if (isMounted) {
          setUser(null);
          setLoading(false);
        }
        return;
      }

      if (!force && user) {
        if (isMounted) {
          setLoading(false);
        }
        return;
      }

      if (fetchInFlight) {
        return;
      }

      fetchInFlight = true;

      if (isMounted) {
        setLoading(true);
      }

      try {
        if (isAuthenticated) {
          await axiosClient.post("/user/sync-b2c");
        }
        const response = await axiosClient.get("/user/me");

        if (isAuthenticated) {
          recordLoginActivityOnce();
        }

        if (isMounted) {
          setUser(response.data);
          setLoading(false);
        }
      } catch {
        if (isMounted) {
          setUser(null);
          setLoading(false);
        }
      } finally {
        fetchInFlight = false;
      }
    };

    void fetchUserProfile();

    const handleDemoModeChanged = () => {
      void fetchUserProfile(true);
    };
    window.addEventListener("pdm:demoModeChanged", handleDemoModeChanged);

    return () => {
      isMounted = false;
      window.removeEventListener("pdm:demoModeChanged", handleDemoModeChanged);
    };
  }, [isAuthenticated, inProgress]);

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
      if (document.hidden) {
        return;
      }

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

  // Legacy login — native auth na /login
  const login = async (_email: string, _password: string) => {
    window.location.assign("/login");
    return { success: true };
  };

  const googleLogin = async (_token: string) => {
    window.location.assign("/login");
    return { success: true };
  };

  const googleRegister = async (_token: string) => {
    window.location.assign("/register");
    return { success: true };
  };

  // Logout — native: CustomAuth signOut; redirect login: logoutRedirect; demo: home
  const logout = async () => {
    const demoOnlySession = isDemoModeActive() && !isAuthenticated;

    try {
      await notificationHubService.stopConnection();
    } catch {
      // ignore
    }
    try {
      await chatHubService.stopConnection();
    } catch {
      // ignore
    }

    setUser(null);
    setSignalRInitialized(false);
    setDemoMode(false);
    queryClient.clear();

    if (demoOnlySession) {
      sessionStorage.clear();
      window.location.assign("/");
      return;
    }

    const account = instance.getActiveAccount() || accounts[0] || null;
    await logoutMsalSession(msalInstance, account);
  };

  const setIsAuthenticated = (_value: boolean) => {
  };

  // Refresh user data from /user/me (po zmianie aktywnego tenanta lub trybu demo)
  const refreshUser = async () => {
    if (!isAuthenticated && !isDemoModeActive()) {
      setUser(null);
      return;
    }

    try {
      const response = await axiosClient.get("/user/me");
      setUser(response.data);
    } catch {
      setUser(null);
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
