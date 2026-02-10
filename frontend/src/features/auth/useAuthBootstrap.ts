import { useEffect } from "react";
import { renewSession } from "./authSession";
import { useAuthStore } from "./authStore";
import type { SessionCredential } from "./authStore";

export function useAuthBootstrap() {
  const initialized = useAuthStore((state) => state.initialized);
  const setInitialized = useAuthStore((state) => state.setInitialized);
  const setSessionCredential = useAuthStore(
    (state) => state.setSessionCredential,
  );
  const setStatus = useAuthStore((state) => state.setStatus);

  useEffect(() => {
    if (initialized) {
      return;
    }

    // Set initialized FIRST to prevent re-entry
    setInitialized(true);

    // Try to restore session from localStorage
    const storedCredential = localStorage.getItem("sessionCredential");
    if (storedCredential) {
      try {
        const credential = JSON.parse(storedCredential) as SessionCredential;
        // Restore the credential to the store
        setSessionCredential(credential);
        setStatus("authenticated");

        // Then attempt to renew session asynchronously to get a fresh token
        renewSession({ redirectOnFailure: false, silent: true });
      } catch (error) {
        console.error("Failed to restore session from localStorage:", error);
        // Clear invalid data
        localStorage.removeItem("sessionCredential");
      }
    }

    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialized]);
}
