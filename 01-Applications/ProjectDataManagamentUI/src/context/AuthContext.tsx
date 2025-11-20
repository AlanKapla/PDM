import { createContext, useEffect, useState, type ReactNode } from "react";
import { authApi } from "../api/authApi";

export interface User {
  email: string;
  firstName: string;
  lastName: string;
}

interface AuthContextType {
  isAuthenticated: boolean;
  user: User | null;
  setIsAuthenticated: (value: boolean) => void;
  login: (email: string, password: string) => Promise<boolean>;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextType>({
  isAuthenticated: false,
  user: null,
  setIsAuthenticated: () => {},
  login: async () => false,
  logout: async () => {},
});

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    const checkSession = async () => {
      try {
        const res = await authApi.getProfile();

        if (res.ok) {
          const json = await res.json();

          setIsAuthenticated(true);
          setUser({
            email: json.email,
            firstName: json.firstName,
            lastName: json.lastName,
          });
        } else {
          setIsAuthenticated(false);
          setUser(null);
        }
      } catch {
        setIsAuthenticated(false);
        setUser(null);
      }
    };

    checkSession();
  }, []);

  const login = async (email: string, password: string): Promise<boolean> => {
    const payload = {
      email,
      password,
      externalToken: "",
      provider: 0,
    };

    try {
      const res = await authApi.login(payload);

      if (res.ok) {
        const profile = await authApi.getProfile();

        if (profile.ok) {
          const json = await profile.json();

          setIsAuthenticated(true);
          setUser({
            email: json.email,
            firstName: json.firstName,
            lastName: json.lastName,
          });

          return true;
        }
      }

      return false;
    } catch {
      return false;
    }
  };

  const logout = async () => {
  try {
    await authApi.logout();
  } catch {}

  setIsAuthenticated(false);

  window.location.href = "/login"; 
};


  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        user,
        setIsAuthenticated,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
