import { apiClient } from "@/lib/api";
import { StateInfo, StateLookupResponse } from "./types";

/**
 * API functions for state metadata and ZIP code lookup.
 */

/**
 * Fetch the list of available pilot states.
 */
export async function fetchStates(): Promise<StateInfo[]> {
  const response = await apiClient.get<StateInfo[]>("/states");
  return response.data;
}

/**
 * Lookup state by ZIP code.
 * @param zip 5-digit ZIP code
 * @returns State information if found
 */
export async function lookupStateByZip(
  zip: string,
): Promise<StateLookupResponse | null> {
  try {
    const response = await apiClient.get<StateLookupResponse>(
      `/states/lookup`,
      {
        params: { zip },
      },
    );
    return response.data;
  } catch (error: any) {
    if (error.response?.status === 404) {
      // ZIP not found or not in pilot states
      return null;
    }
    throw error;
  }
}
