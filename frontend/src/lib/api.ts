import axios, { AxiosInstance, AxiosRequestConfig } from "axios";
import { getAccessToken } from "@/features/auth/authStore";
import { redirectToLogin, renewSession } from "@/features/auth/authSession";

/**
 * API client for the Medicaid Application Assistant backend.
 * Configured with credentials (cookies) for session management.
 */
class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: "/api", // Vite proxy handles /api -> localhost:5008
      withCredentials: true, // Include cookies (MAA_SessionId)
      timeout: 10000,
      headers: {
        "Content-Type": "application/json",
      },
    });

    this.client.interceptors.request.use((config) => {
      const token = getAccessToken();
      if (token) {
        config.headers = config.headers ?? {};
        if (!("Authorization" in config.headers)) {
          config.headers.Authorization = `Bearer ${token}`;
        }
      }
      return config;
    });

    // Response interceptor for error handling
    this.client.interceptors.response.use(
      (response) => response,
      async (error) => {
        const originalRequest = error.config as AxiosRequestConfig & {
          _retry?: boolean;
        };
        const status = error.response?.status;

        const requestUrl = originalRequest?.url ?? "";
        const isAuthRequest =
          requestUrl.includes("/auth/login") ||
          requestUrl.includes("/auth/register") ||
          requestUrl.includes("/auth/refresh");

        // Handle 401 Unauthorized - try to refresh token
        if (status === 401 && !isAuthRequest) {
          if (originalRequest._retry) {
            redirectToLogin(window.location.pathname + window.location.search);
            return Promise.reject(error);
          }

          originalRequest._retry = true;
          const token = await renewSession({ redirectOnFailure: false });

          if (token) {
            originalRequest.headers = originalRequest.headers ?? {};
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return this.client.request(originalRequest);
          }

          redirectToLogin(window.location.pathname + window.location.search);
        }

        // Handle 400 on refresh endpoint - missing token, redirect to login
        if (status === 400 && requestUrl.includes("/auth/refresh")) {
          redirectToLogin(window.location.pathname + window.location.search);
        }

        return Promise.reject(error);
      },
    );
  }

  /**
   * Get the axios instance for custom requests.
   */
  getClient(): AxiosInstance {
    return this.client;
  }
}

// Export singleton instance
export const apiClient = new ApiClient().getClient();
