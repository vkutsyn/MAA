import { useEffect } from "react";
import { renewSession } from "./authSession";
import { useAuthStore } from "./authStore";

export function useAuthBootstrap() {
  const initialized = useAuthStore((state) => state.initialized);
  const setInitialized = useAuthStore((state) => state.setInitialized);

  useEffect(() => {
    if (initialized) {
      return;
    }

    // Set initialized FIRST to prevent re-entry
    setInitialized(true);

    // Then attempt to renew session asynchronously
    renewSession({ redirectOnFailure: false, silent: true });

    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialized]);
}
