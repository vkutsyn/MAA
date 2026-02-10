import { apiClient } from '@/lib/api'
import { QuestionSet } from './types'

/**
 * API functions for question taxonomy.
 */

/**
 * Fetch questions for a specific state.
 * @param stateCode 2-letter state code (e.g., 'TX', 'CA')
 */
export async function fetchQuestions(stateCode: string): Promise<QuestionSet> {
  const response = await apiClient.get<QuestionSet>(`/questions`, {
    params: { state: stateCode },
  })
  return response.data
}
