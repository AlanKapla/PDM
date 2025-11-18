import { useContext, type ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { AuthContext } from "../context/AuthContext";

export default function PublicRoute({ children }: { children: ReactNode }) {
  const { token } = useContext(AuthContext);

  if (token) {
    return <Navigate to="/" replace />;
  }

  return children;
}
