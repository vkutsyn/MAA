import { create } from "zustand";
import { devtools } from "zustand/middleware";
import { authApiErrors, loginUser, logoutUser, registerUser } from "./authApi";

export type AuthStatus =
  | "unauthenticated"
  | "authenticating"
  | "authenticated"
  | "renewing";

export interface SessionCredential {
  accessToken: string;
  tokenType: string;
  expiresInSeconds: number;
  refreshToken?: string | null;
}

export interface AuthUser {
  userId?: string;
  email?: string;
  fullName?: string;
  role?: string;
}

export interface AuthState {
  status: AuthStatus;
  user: AuthUser | null;
  sessionCredential: SessionCredential | null;
  lastError: string | null;
  returnPath: string | null;
  initialized: boolean;
  setReturnPath: (path: string | null) => void;
  clearReturnPath: () => void;
  setStatus: (status: AuthStatus) => void;
  setError: (message: string | null) => void;
  setInitialized: (value: boolean) => void;
  setSessionCredential: (credential: SessionCredential | null) => void;
  clearAuth: () => void;
  login: (
    email: string,
    password: string,
  ) => Promise<{ ok: boolean; message?: string }>;
  register: (
    email: string,
    password: string,
    fullName: string,
  ) => Promise<{ ok: boolean; message?: string }>;
  logout: () => Promise<void>;
}

const defaultState = {
  status: "unauthenticated" as AuthStatus,
  user: null,
  sessionCredential: null,
  lastError: null,
  returnPath: null,
  initialized: false,
};

const ignoredReturnPaths = ["/login", "/register"];

export const useAuthStore = create<AuthState>()(
  devtools((set, get) => ({
    ...defaultState,
    setReturnPath: (path) => {
      if (!path) {
        set({ returnPath: null });
        return;
      }
      if (ignoredReturnPaths.some((route) => path.startsWith(route))) {
        return;
      }
      set({ returnPath: path });
    },
    clearReturnPath: () => set({ returnPath: null }),
    setStatus: (status) => set({ status }),
    setError: (message) => set({ lastError: message }),
    setInitialized: (value) => set({ initialized: value }),
    setSessionCredential: (credential) => {
      if (credential) {
        // Persist tokens to localStorage
        localStorage.setItem("sessionCredential", JSON.stringify(credential));
      } else {
        localStorage.removeItem("sessionCredential");
      }
      set({ sessionCredential: credential });
    },
    clearAuth: () => {
      // Clear sessionId and auth tokens from localStorage when clearing auth state
      localStorage.removeItem("sessionId");
      localStorage.removeItem("sessionCredential");
      set({ ...defaultState, initialized: get().initialized });
    },
    login: async (email, password) => {
      // Clear any existing sessionId to ensure fresh session for new user
      localStorage.removeItem("sessionId");
      set({ status: "authenticating", lastError: null });
      try {
        const response = await loginUser({ email, password });
        const credential = {
          accessToken: response.accessToken,
          tokenType: response.tokenType,
          expiresInSeconds: response.expiresIn,
          refreshToken: response.refreshToken,
        };
        // Persist tokens to localStorage
        localStorage.setItem("sessionCredential", JSON.stringify(credential));
        set({
          status: "authenticated",
          sessionCredential: credential,
        });
        return { ok: true };
      } catch (error) {
        const message = authApiErrors.getLoginError(error);
        set({ status: "unauthenticated", lastError: message });
        return { ok: false, message };
      }
    },
    register: async (email, password, fullName) => {
      set({ status: "authenticating", lastError: null });
      try {
        await registerUser({ email, password, fullName });
        set({ status: "unauthenticated" });
        return { ok: true };
      } catch (error) {
        const message = authApiErrors.getRegisterError(error);
        set({ status: "unauthenticated", lastError: message });
        return { ok: false, message };
      }
    },
    logout: async () => {
      try {
        await logoutUser();
      } finally {
        // Clear sessionId and auth tokens from localStorage on logout
        localStorage.removeItem("sessionId");
        localStorage.removeItem("sessionCredential");
        set({
          status: "unauthenticated",
          sessionCredential: null,
          user: null,
          lastError: null,
          returnPath: null,
        });
      }
    },
  })),
);

export const getAccessToken = () =>
  useAuthStore.getState().sessionCredential?.accessToken ?? null;

export const getRefreshToken = () =>
  useAuthStore.getState().sessionCredential?.refreshToken ?? null;
