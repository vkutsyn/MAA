import axios, { AxiosInstance } from 'axios'

/**
 * API client for the Medicaid Application Assistant backend.
 * Configured with credentials (cookies) for session management.
 */
class ApiClient {
  private client: AxiosInstance

  constructor() {
    this.client = axios.create({
      baseURL: '/api', // Vite proxy handles /api -> localhost:5008
      withCredentials: true, // Include cookies (MAA_SessionId)
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
      },
    })

    // Response interceptor for error handling
    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          // Session expired - handle by redirecting to landing
          console.warn('Session expired, redirecting to landing')
          window.location.href = '/' 
        }
        return Promise.reject(error)
      }
    )
  }

  /**
   * Get the axios instance for custom requests.
   */
  getClient(): AxiosInstance {
    return this.client
  }
}

// Export singleton instance
export const apiClient = new ApiClient().getClient()
