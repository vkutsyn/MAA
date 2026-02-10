import { ReactNode, useEffect, useRef } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuthStore } from "./authStore";

interface RequireAuthProps {
  children: ReactNode;
}

export function RequireAuth({ children }: RequireAuthProps) {
  const location = useLocation();
  const status = useAuthStore((state) => state.status);
  const setReturnPath = useAuthStore((state) => state.setReturnPath);
  const hasSetReturnPath = useRef(false);

  useEffect(() => {
    if (status === "unauthenticated" && !hasSetReturnPath.current) {
      const returnPath = `${location.pathname}${location.search}`;
      setReturnPath(returnPath);
      hasSetReturnPath.current = true;
    }

    // Reset flag when authenticated
    if (status === "authenticated") {
      hasSetReturnPath.current = false;
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status]);

  if (status === "authenticating" || status === "renewing") {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <p className="text-muted-foreground">Checking your session...</p>
      </div>
    );
  }

  if (status !== "authenticated") {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
