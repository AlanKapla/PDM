import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

export default function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, loading } = useAuth();
  const location = useLocation();

  if (loading) return null; // tutaj możesz wrzucić spinner

  if (!isAuthenticated) {
    // Zapisz aktualny URL w state, aby móc wrócić po zalogowaniu
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <>{children}</>;
}
