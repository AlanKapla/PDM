// ============================================
//   DemoContext — stan demo mode
//   Synchronizowany z sessionStorage
//   Przełączenie czyści React Query cache
//   aby natychmiast pokazać dane z nowego źródła
// ============================================

import { createContext, useContext, useState, useCallback, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { isDemoModeActive, setDemoMode as setStorage } from "../api/mock";
import { chatHubService } from "../services/chatHubService";
import { clearAllTabCache } from "../hooks/useTabCache";

interface DemoContextType {
  isDemoMode: boolean;
  toggleDemoMode: () => void;
  enterDemoMode: () => Promise<void>;
  exitDemoMode: () => Promise<void>;
}

const DemoContext = createContext<DemoContextType>({
  isDemoMode: false,
  toggleDemoMode: () => {},
  enterDemoMode: async () => {},
  exitDemoMode: async () => {},
});

export function DemoProvider({ children }: { children: ReactNode }) {
  const [isDemoMode, setIsDemoMode] = useState(() => isDemoModeActive());
  const queryClient = useQueryClient();

  const applyDemoModeChange = useCallback(
    (next: boolean) => {
      setStorage(next);
      setIsDemoMode(next);
      void chatHubService.stopConnection();
      queryClient.cancelQueries();
      clearAllTabCache();
      queryClient.clear();
      window.dispatchEvent(new CustomEvent("pdm:demoModeChanged"));
    },
    [queryClient]
  );

  const toggleDemoMode = useCallback(() => {
    applyDemoModeChange(!isDemoMode);
  }, [applyDemoModeChange, isDemoMode]);

  const enterDemoMode = useCallback(async () => {
    if (isDemoMode) {
      return;
    }
    applyDemoModeChange(true);
  }, [applyDemoModeChange, isDemoMode]);

  const exitDemoMode = useCallback(async () => {
    if (!isDemoMode) {
      return;
    }
    applyDemoModeChange(false);
  }, [applyDemoModeChange, isDemoMode]);

  return (
    <DemoContext.Provider value={{ isDemoMode, toggleDemoMode, enterDemoMode, exitDemoMode }}>
      {children}
    </DemoContext.Provider>
  );
}

export function useDemoMode(): DemoContextType {
  return useContext(DemoContext);
}