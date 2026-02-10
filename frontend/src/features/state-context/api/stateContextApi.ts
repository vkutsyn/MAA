/**
 * API client for State Context endpoints
 * Feature: State Context Initialization Step
 */

import axios from "axios";
import { apiClient } from "@/lib/api";
import type {
  InitializeStateContextRequest,
  StateContextResponse,
  UpdateStateContextRequest,
  ValidateZipRequest,
  ValidateZipResponse,
  ErrorResponse,
} from "../types/stateContext.types";

/**
 * Initialize state context from ZIP code
 * POST /api/state-context
 */
export async function initializeStateContext(
  request: InitializeStateContextRequest,
): Promise<StateContextResponse> {
  try {
    const response = await apiClient.post<StateContextResponse>(
      "/state-context",
      request,
    );
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error) && error.response) {
      const errorResponse = error.response.data as ErrorResponse;
      throw new Error(
        errorResponse.message || "Failed to initialize state context",
      );
    }
    throw error;
  }
}

/**
 * Get state context for a session
 * GET /api/state-context?sessionId={sessionId}
 */
export async function getStateContext(
  sessionId: string,
): Promise<StateContextResponse> {
  try {
    const response = await apiClient.get<StateContextResponse>("/state-context", {
      params: { sessionId },
    });
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error) && error.response) {
      if (error.response.status === 404) {
        throw new Error("State context not found for this session");
      }
      const errorResponse = error.response.data as ErrorResponse;
      throw new Error(errorResponse.message || "Failed to get state context");
    }
    throw error;
  }
}

/**
 * Update state context (manual state override)
 * PUT /api/state-context
 */
export async function updateStateContext(
  request: UpdateStateContextRequest,
): Promise<StateContextResponse> {
  try {
    const response = await apiClient.put<StateContextResponse>(
      "/state-context",
      request,
    );
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error) && error.response) {
      const errorResponse = error.response.data as ErrorResponse;
      throw new Error(
        errorResponse.message || "Failed to update state context",
      );
    }
    throw error;
  }
}

/**
 * Validate ZIP code (client-side helper)
 * POST /api/state-context/validate-zip
 */
export async function validateZipCode(
  zipCode: string,
): Promise<ValidateZipResponse> {
  try {
    const response = await apiClient.post<ValidateZipResponse>(
      "/state-context/validate-zip",
      {
        zipCode,
      } as ValidateZipRequest,
    );
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error) && error.response) {
      const errorResponse = error.response.data as ErrorResponse;
      throw new Error(errorResponse.message || "Failed to validate ZIP code");
    }
    throw error;
  }
}
