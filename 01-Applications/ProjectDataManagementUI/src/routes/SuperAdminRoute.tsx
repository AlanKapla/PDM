import type { ReactElement, ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useContext } from "react";
import { AuthContext } from "../context/AuthContext";
import ProtectedRoute from "./ProtectedRoute";

function SuperAdminGuard({ children }: { children: ReactNode }): ReactElement {
  const { user } = useContext(AuthContext);

  if (!user?.isSuperAdmin) {
    return <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
}

export default function SuperAdminRoute({ children }: { children: ReactNode }): ReactElement {
  return (
    <ProtectedRoute>
      <SuperAdminGuard>{children}</SuperAdminGuard>
    </ProtectedRoute>
  );
}
