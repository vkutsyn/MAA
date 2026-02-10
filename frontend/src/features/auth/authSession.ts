import { authApiErrors, refreshSession } from "./authApi";
import { useAuthStore, getRefreshToken } from "./authStore";

let refreshPromise: Promise<string | null> | null = null;

interface RenewOptions {
  redirectOnFailure?: boolean;
  silent?: boolean;
}

export async function renewSession(
  options: RenewOptions = {},
): Promise<string | null> {
  if (refreshPromise) {
    return refreshPromise;
  }

  const { setStatus, setSessionCredential, setError, clearAuth } =
    useAuthStore.getState();
  const refreshToken = getRefreshToken();

  // If no refresh token, user must log in again
  if (!refreshToken) {
    setStatus("unauthenticated");
    if (options.redirectOnFailure) {
      redirectToLogin(window.location.pathname + window.location.search);
    }
    return null;
  }

  setStatus("renewing");

  refreshPromise = refreshSession(refreshToken)
    .then((response) => {
      const updatedCredential = {
        accessToken: response.accessToken,
        tokenType: response.tokenType,
        expiresInSeconds: response.expiresIn,
        refreshToken: refreshToken, // Keep the same refresh token
      };
      setSessionCredential(updatedCredential);
      setStatus("authenticated");
      if (!options.silent) {
        setError(null);
      }
      return response.accessToken;
    })
    .catch((error) => {
      // Clear auth on refresh failure
      clearAuth();
      if (!options.silent) {
        setError(authApiErrors.getRefreshError(error));
      }
      return null;
    })
    .finally(() => {
      refreshPromise = null;
    });

  const token = await refreshPromise;
  if (!token && options.redirectOnFailure) {
    redirectToLogin(window.location.pathname + window.location.search);
  }

  return token;
}

export function redirectToLogin(returnPath?: string) {
  const { setReturnPath, clearAuth } = useAuthStore.getState();
  if (returnPath) {
    setReturnPath(returnPath);
  }
  // Clear sessionId and tokens to ensure fresh session on next login
  localStorage.removeItem("sessionId");
  localStorage.removeItem("sessionCredential");
  clearAuth();

  if (window.location.pathname !== "/login") {
    window.location.href = "/login";
  }
}
