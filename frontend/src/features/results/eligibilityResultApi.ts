/**
 * API client for eligibility results
 */

import axios, { AxiosError } from 'axios';
import { UserEligibilityInput, EligibilityResultDto, ApiErrorResponse } from './types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

/**
 * Evaluate eligibility using the backend rules engine
 */
export async function evaluateEligibility(
  input: UserEligibilityInput,
  token?: string
): Promise<EligibilityResultDto> {
  try {
    const response = await axios.post<EligibilityResultDto>(
      `${API_BASE_URL}/api/rules/evaluate`,
      input,
      {
        headers: {
          'Content-Type': 'application/json',
          ...(token && { Authorization: `Bearer ${token}` }),
        },
      }
    );

    return response.data;
  } catch (error) {
    throw handleApiError(error);
  }
}

/**
 * Handle API errors with consistent messaging
 */
function handleApiError(error: unknown): Error {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<ApiErrorResponse>;

    // Handle specific HTTP status codes
    if (axiosError.response?.status === 401) {
      return new Error('Unauthorized: Please log in again');
    }

    if (axiosError.response?.status === 404) {
      return new Error('Eligibility evaluation not available for this state');
    }

    if (axiosError.response?.status === 400) {
      const errorData = axiosError.response.data;
      if (errorData.errors && Array.isArray(errorData.errors)) {
        const messages = errorData.errors.map((e) => e.message).join('; ');
        return new Error(`Validation error: ${messages}`);
      }
      return new Error(errorData.message || 'Invalid input provided');
    }

    if (axiosError.response?.status === 500) {
      return new Error('Server error: Unable to evaluate eligibility');
    }

    if (axiosError.message === 'Network Error') {
      return new Error('Network error: Unable to connect to the evaluation service');
    }

    return new Error(axiosError.message || 'Unknown error occurred');
  }

  if (error instanceof Error) {
    return error;
  }

  return new Error('An unexpected error occurred');
}

/**
 * Check if the API is available
 */
export async function checkApiHealth(): Promise<boolean> {
  try {
    const response = await axios.get(`${API_BASE_URL}/api/health`, {
      timeout: 5000,
    });
    return response.status === 200;
  } catch {
    return false;
  }
}
