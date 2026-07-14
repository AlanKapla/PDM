// ============================================
//   DemoContext — stan demo mode
//   Synchronizowany z sessionStorage
//   Przełączenie czyści React Query cache
//   aby natychmiast pokazać dane z nowego źródła
// ============================================

import { createContext, useContext, useState, useCallback, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { isDemoModeActive, setDemoMode as setStorage } from "../api/mock";
import { useAuth } from "./AuthContext";

interface DemoContextType {
  isDemoMode: boolean;
  toggleDemoMode: () => void;
}

const DemoContext = createContext<DemoContextType>({
  isDemoMode: false,
  toggleDemoMode: () => {},
});

export function DemoProvider({ children }: { children: ReactNode }) {
  const [isDemoMode, setIsDemoMode] = useState(() => isDemoModeActive());
  const queryClient = useQueryClient();
  const { refreshUser } = useAuth();

  const toggleDemoMode = useCallback(() => {
    const next = !isDemoMode;
    // Zapisz do sessionStorage SYNCHRONICZNIE przed resetem —
    // inaczej mock interceptor przeczyta starą wartość podczas refetcha.
    setStorage(next);
    setIsDemoMode(next);
    // Reset wszystkich query — usuwa dane i wymusza refetch,
    // ale w przeciwieństwie do clear() nie niszczy obserwerów,
    // więc useQuery dostaje nowy snapshot i przerenderowuje się.
    queryClient.resetQueries();
    // Czyści globalny cache useTabCache — komponenty (np. kosztorysy)
    // które nie używają React Query odświeżą dane przy następnym renderze.
    window.dispatchEvent(new CustomEvent("pdm:demoModeChanged"));
    // Odśwież profil usera — /api/user/me zwraca inne dane
    // w zależności od trybu (mock vs rzeczywiste), w tym activeTenantId.
    refreshUser();
  }, [isDemoMode, queryClient, refreshUser]);

  return (
    <DemoContext.Provider value={{ isDemoMode, toggleDemoMode }}>
      {children}
    </DemoContext.Provider>
  );
}

export function useDemoMode(): DemoContextType {
  return useContext(DemoContext);
}