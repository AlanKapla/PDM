import { useContext, type ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { AuthContext } from "../context/AuthContext";

export default function PublicRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useContext(AuthContext);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return children;
}