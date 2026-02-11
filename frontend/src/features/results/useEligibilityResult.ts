/**
 * React Query hook for eligibility results
 */

import { useQuery, QueryKey } from '@tanstack/react-query';
import { useAuthStore } from '@/features/auth';
import {
  UserEligibilityInput,
  EligibilityResultView,
  mapDtoToViewModel,
} from './types';
import { evaluateEligibility } from './eligibilityResultApi';

/**
 * Query key factory for eligibility results
 */
const queryKeyFactory = {
  all: () => ['eligibilityResult'] as const,
  evaluation: (input: UserEligibilityInput) => [
    ...queryKeyFactory.all(),
    'evaluation',
    input,
  ] as const,
};

/**
 * Hook to fetch eligibility results
 *
 * @param input - User eligibility input or null to skip query
 * @returns Query result with result data, loading/error states
 */
export function useEligibilityResult(input: UserEligibilityInput | null) {
  const status = useAuthStore((state) => state.status);
  const token = useAuthStore((state) => state.sessionCredential?.accessToken);
  
  const isAuthenticated = status === 'authenticated';
  const isValid = !!input;

  const queryKey: QueryKey = input ? queryKeyFactory.evaluation(input) : ['disabled'];

  return useQuery({
    queryKey,
    queryFn: async (): Promise<EligibilityResultView> => {
      if (!input) {
        throw new Error('No input provided');
      }

      if (!isAuthenticated || !token) {
        throw new Error('User is not authenticated');
      }

      const dto = await evaluateEligibility(input, token);
      return mapDtoToViewModel(dto);
    },
    enabled: !!input && isValid && isAuthenticated && !!token,
    staleTime: 1000 * 60 * 5, // 5 minutes
    gcTime: 1000 * 60 * 10, // 10 minutes (formerly cacheTime)
    retry: 3,
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
  });
}

/**
 * Hook to check if a query is in flight
 */
export function useEligibilityResultStatus(input: UserEligibilityInput | null) {
  const { isLoading, isError, error, data } = useEligibilityResult(input);

  return {
    isLoading,
    isError,
    error: error instanceof Error ? error.message : 'Unknown error',
    data,
  };
}
