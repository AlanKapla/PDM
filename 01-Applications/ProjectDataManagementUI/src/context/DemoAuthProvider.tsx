/**
 * DemoAuthProvider – zastępuje AuthProvider w trybie demo.
 * Nie używa MSAL ani żadnych żądań do backendu.
 * Użytkownik jest od razu zalogowany jako Anna Kowalska.
 *
 * Używa tego samego obiektu AuthContext co AuthContext.tsx,
 * dzięki czemu wszystkie komponenty importujące AuthContext działają bez zmian.
 */

import { type ReactNode } from "react";
import { AuthContext } from "./AuthContext";
import { DEMO_CURRENT_USER, DEMO_TENANT_ID } from "../mocks/data/users";

export const DemoAuthProvider = ({ children }: { children: ReactNode }) => {
  return (
    <AuthContext.Provider
      value={{
        isAuthenticated: true,
        user: { ...DEMO_CURRENT_USER, activeTenantId: DEMO_TENANT_ID },
        loading: false,
        refreshUser: async () => {},
        setIsAuthenticated: () => {},
        login: async () => ({ success: true }),
        googleLogin: async () => ({ success: true }),
        googleRegister: async () => ({ success: true }),
        logout: async () => {},
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
