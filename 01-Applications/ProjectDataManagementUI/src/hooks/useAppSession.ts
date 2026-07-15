import { useAuth } from "../context/AuthContext";
import { useDemoMode } from "../context/DemoContext";

export interface AppSessionState {
  hasSession: boolean;
  isDemoOnlySession: boolean;
  isAuthenticated: boolean;
  isDemoMode: boolean;
}

export function useAppSession(): AppSessionState {
  const { isAuthenticated, user } = useAuth();
  const { isDemoMode } = useDemoMode();

  const hasSession = user !== null && (isAuthenticated || isDemoMode);
  const isDemoOnlySession = isDemoMode && !isAuthenticated && user !== null;

  return {
    hasSession,
    isDemoOnlySession,
    isAuthenticated,
    isDemoMode,
  };
}
