import { useEffect, useRef } from "react";
import { useAuthStore } from "./authStore";
import type { SessionCredential } from "./authStore";

export function useAuthBootstrap() {
  const hasInitialized = useRef(false);

  useEffect(() => {
    if (hasInitialized.current) {
      return;
    }

    hasInitialized.current = true;

    // Try to restore session from localStorage
    const storedCredential = localStorage.getItem("sessionCredential");

    if (storedCredential) {
      try {
        const credential = JSON.parse(storedCredential) as SessionCredential;

        // Batch all state updates into a single update
        useAuthStore.setState({
          sessionCredential: credential,
          status: "authenticated",
          initialized: true,
        });
      } catch (error) {
        console.error("Failed to restore session from localStorage:", error);
        // Clear invalid data
        localStorage.removeItem("sessionCredential");
        useAuthStore.setState({ initialized: true });
      }
    } else {
      useAuthStore.setState({ initialized: true });
    }

    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
}
