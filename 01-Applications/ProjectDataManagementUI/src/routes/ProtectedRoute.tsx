import { useEffect, useState, type ReactNode } from "react";
import { Navigate } from "react-router-dom";

export default function ProtectedRoute({ children }: { children: ReactNode }) {
  const [loading, setLoading] = useState(true);
  const [authenticated, setAuthenticated] = useState<boolean | null>(null);

  useEffect(() => {
    async function check() {
      try {
        const res = await fetch("/api/User/me", {
          credentials: "include"
        });

        if (res.ok) {
          setAuthenticated(true);
        } else {
          setAuthenticated(false);
        }
      } catch (err) {
        setAuthenticated(false);
      } finally {
        setLoading(false);
      }
    }

    check();
  }, []);

  if (loading) return null; // można dać spinner

  if (!authenticated) return <Navigate to="/login" replace />;

  return children;
}
