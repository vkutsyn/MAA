import { apiClient } from "@/lib/api";
import { SaveAnswerDto, SessionAnswerDto } from "./types";

/**
 * API functions for saving and retrieving answers.
 */

/**
 * Save or update an answer for the current session.
 * @param answer Answer data to save
 */
export async function saveAnswer(
  answer: SaveAnswerDto,
): Promise<SessionAnswerDto> {
  const response = await apiClient.post<SessionAnswerDto>(
    "/sessions/me/answers",
    answer,
  );
  return response.data;
}

/**
 * Get all answers for the current session.
 */
export async function fetchAnswers(): Promise<SessionAnswerDto[]> {
  const response = await apiClient.get<SessionAnswerDto[]>(
    "/sessions/me/answers",
  );
  return response.data;
}

/**
 * Get a specific answer by field key.
 * @param fieldKey The field key to lookup
 */
export async function fetchAnswer(
  fieldKey: string,
): Promise<SessionAnswerDto | null> {
  try {
    const response = await apiClient.get<SessionAnswerDto>(
      `/sessions/me/answers/${fieldKey}`,
    );
    return response.data;
  } catch (error: any) {
    if (error.response?.status === 404) {
      return null;
    }
    throw error;
  }
}
