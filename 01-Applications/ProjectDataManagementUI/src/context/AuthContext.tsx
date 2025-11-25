import { createContext, useEffect, useState, type ReactNode } from "react";
import { loginUser, getUserProfile } from "../services/authService";
import { authApi } from "../api/authApi";
import type { UserProfile } from "../types/auth.types";

interface AuthContextType {
  isAuthenticated: boolean;
  user: UserProfile | null;
  loading: boolean;
  setIsAuthenticated: (value: boolean) => void;
  login: (email: string, password: string) => Promise<{ success: boolean; message?: string }>;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextType>({
  isAuthenticated: false,
  user: null,
  loading: true,
  setIsAuthenticated: () => {},
  login: async () => ({ success: false }),
  logout: async () => {},
});

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const checkSession = async () => {
      // ⛔️ NIE sprawdzamy sesji na swaggerze – tam auth nie jest potrzebny
      if (window.location.pathname.startsWith("/swagger")) {
        setLoading(false);
        return;
      }

      try {
        const profile = await getUserProfile();

        if (profile) {
          setIsAuthenticated(true);
          setUser(profile);
        } else {
          setIsAuthenticated(false);
          setUser(null);
        }
      } catch (error) {
        console.error("Błąd sprawdzania sesji:", error);
        setIsAuthenticated(false);
        setUser(null);
      } finally {
        setLoading(false);
      }
    };

    checkSession();
  }, []);

  const login = async (email: string, password: string): Promise<{ success: boolean; message?: string }> => {
    try {
      const result = await loginUser({ email, password });
      if (!result.success) {
        return { success: false, message: result.message };
      }

      const profile = await getUserProfile();
      if (!profile) {
        return { success: false, message: "Failed to load user profile" };
      }

      setIsAuthenticated(true);
      setUser(profile);

      return { success: true };
    } catch (error) {
      console.error("Błąd logowania:", error);
      return { success: false, message: "An unexpected error occurred" };
    }
  };

  const logout = async () => {
    try {
      await authApi.logout({ refreshToken: "" });
    } catch (error) {
      console.error("Błąd wylogowania:", error);
    }

    setIsAuthenticated(false);
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        user,
        loading,
        setIsAuthenticated,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
