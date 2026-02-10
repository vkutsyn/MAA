import axios from "axios";
import { apiClient } from "@/lib/api";

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
}

export interface RegisterResponse {
  userId: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  refreshToken?: string | null;
}

export interface RefreshResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
}

export interface LoginConflictResponse {
  error?: string;
  activeSessions?: Array<{
    sessionId: string;
    device?: string;
    ipAddress?: string;
    loginTime?: string;
  }>;
}

export interface ApiErrorResponse {
  error?: string;
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (axios.isAxiosError<ApiErrorResponse | LoginConflictResponse>(error)) {
    const message = error.response?.data?.error;
    if (message) {
      return message;
    }
  }
  return fallback;
}

export async function registerUser(
  request: RegisterRequest,
): Promise<RegisterResponse> {
  const response = await apiClient.post<RegisterResponse>(
    "/auth/register",
    request,
  );
  return response.data;
}

export async function loginUser(request: LoginRequest): Promise<LoginResponse> {
  const response = await apiClient.post<LoginResponse>("/auth/login", request);
  return response.data;
}

export async function refreshSession(
  refreshToken?: string,
): Promise<RefreshResponse> {
  const response = await apiClient.post<RefreshResponse>(
    "/auth/refresh",
    refreshToken ? { refreshToken } : undefined,
  );
  return response.data;
}

export async function logoutUser(): Promise<void> {
  await apiClient.post("/auth/logout");
}

export const authApiErrors = {
  getLoginError(error: unknown): string {
    return getErrorMessage(error, "Unable to login with those credentials.");
  },
  getRegisterError(error: unknown): string {
    return getErrorMessage(
      error,
      "Unable to create account. Please try again.",
    );
  },
  getRefreshError(error: unknown): string {
    return getErrorMessage(error, "Unable to renew your session.");
  },
};
