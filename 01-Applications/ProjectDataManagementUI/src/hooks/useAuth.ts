import { useContext } from "react";
import { AuthContext } from "../context/AuthContext";

/**
 * Hook do zarządzania autentykacją użytkownika
 * Upraszcza dostęp do AuthContext
 */
export const useAuth = () => {
  const context = useContext(AuthContext);
  
  if (!context) {
    throw new Error("useAuth musi być użyty wewnątrz AuthProvider");
  }
  
  return context;
};
