/**
 * React Query hooks for State Context management
 * Feature: State Context Initialization Step
 */

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  initializeStateContext,
  getStateContext,
  updateStateContext,
} from "../api/stateContextApi";
import type {
  InitializeStateContextRequest,
  StateContextResponse,
  UpdateStateContextRequest,
} from "../types/stateContext.types";

/**
 * Query key factory for state context queries
 */
export const stateContextKeys = {
  all: ["stateContext"] as const,
  bySession: (sessionId: string) =>
    [...stateContextKeys.all, sessionId] as const,
};

/**
 * Hook to initialize state context from ZIP code
 * Uses TanStack Query useMutation for POST /api/state-context
 *
 * @example
 * const { mutate: initialize, isPending, error } = useInitializeStateContext();
 * initialize({ sessionId: "123", zipCode: "90210" });
 */
export function useInitializeStateContext() {
  const queryClient = useQueryClient();

  return useMutation<
    StateContextResponse,
    Error,
    InitializeStateContextRequest
  >({
    mutationFn: initializeStateContext,
    onSuccess: (data, variables) => {
      // Update cache with new state context
      queryClient.setQueryData(
        stateContextKeys.bySession(variables.sessionId),
        data,
      );
    },
    onError: (error) => {
      console.error("Failed to initialize state context:", error);
    },
  });
}

/**
 * Hook to fetch state context for a session
 * Uses TanStack Query useQuery for GET /api/state-context
 *
 * @param sessionId - Session ID to fetch state context for
 * @param options - TanStack Query options
 *
 * @example
 * const { data, isLoading, error } = useGetStateContext(sessionId);
 */
export function useGetStateContext(
  sessionId: string | undefined,
  options?: {
    enabled?: boolean;
    retry?: boolean | number;
  },
) {
  return useQuery<StateContextResponse, Error>({
    queryKey: stateContextKeys.bySession(sessionId || ""),
    queryFn: () => {
      if (!sessionId) {
        throw new Error("Session ID is required");
      }
      return getStateContext(sessionId);
    },
    enabled: options?.enabled !== false && !!sessionId,
    retry: options?.retry ?? 1,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Hook to update state context (manual state override)
 * Uses TanStack Query useMutation for PUT /api/state-context
 *
 * @example
 * const { mutate: updateState, isPending } = useUpdateStateContext();
 * updateState({ sessionId: "123", stateCode: "NY", isManualOverride: true });
 */
export function useUpdateStateContext() {
  const queryClient = useQueryClient();

  return useMutation<StateContextResponse, Error, UpdateStateContextRequest>({
    mutationFn: updateStateContext,
    onSuccess: (data, variables) => {
      // Update cache with updated state context
      queryClient.setQueryData(
        stateContextKeys.bySession(variables.sessionId),
        data,
      );
    },
    onError: (error) => {
      console.error("Failed to update state context:", error);
    },
  });
}
