import { apiClient } from "@/lib/api";
import { SaveAnswerDto, SessionAnswerDto } from "./types";

/**
 * API functions for saving and retrieving answers.
 */

/**
 * Save or update an answer for the current session.
 * @param sessionId The session ID
 * @param answer Answer data to save
 */
export async function saveAnswer(
  sessionId: string,
  answer: SaveAnswerDto,
): Promise<SessionAnswerDto> {
  const response = await apiClient.post<SessionAnswerDto>(
    `/sessions/${sessionId}/answers`,
    answer,
  );
  return response.data;
}

/**
 * Get all answers for a session.
 * @param sessionId The session ID
 */
export async function fetchAnswers(
  sessionId: string,
): Promise<SessionAnswerDto[]> {
  const response = await apiClient.get<SessionAnswerDto[]>(
    `/sessions/${sessionId}/answers`,
  );
  return response.data;
}

/**
 * Get a specific answer by field key.
 * @param sessionId The session ID
 * @param fieldKey The field key to lookup
 */
export async function fetchAnswer(
  sessionId: string,
  fieldKey: string,
): Promise<SessionAnswerDto | null> {
  try {
    const response = await apiClient.get<SessionAnswerDto>(
      `/sessions/${sessionId}/answers/${fieldKey}`,
    );
    return response.data;
  } catch (error: any) {
    if (error.response?.status === 404) {
      return null;
    }
    throw error;
  }
}
