import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '@/lib/api'
import { useWizardStore } from './store'
import { fetchQuestions } from './questionApi'
import { saveWizardState } from './useResumeWizard'
import { CreateSessionDto, SessionDto } from './types'

/**
 * Hook to start the eligibility wizard.
 * Creates a session, fetches questions, and navigates to the wizard.
 */
export function useStartWizard() {
  const [isStarting, setIsStarting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const navigate = useNavigate()
  const { setSession, setQuestions, setSelectedState, reset } = useWizardStore()

  const startWizard = async (stateCode: string, stateName: string) => {
    setIsStarting(true)
    setError(null)

    try {
      // Step 1: Create a new session
      const sessionPayload: CreateSessionDto = {
        timeoutMinutes: 30,
        inactivityTimeoutMinutes: 30,
      }
      
      const sessionResponse = await apiClient.post<SessionDto>(
        '/sessions',
        sessionPayload
      )

      const session = sessionResponse.data

      // Step 2: Fetch questions for the selected state
      const questionSet = await fetchQuestions(stateCode)

      // Step 3: Update store with session and questions
      setSession({
        sessionId: session.id,
        stateCode,
        stateName,
        currentStep: 0,
        totalSteps: questionSet.questions.length,
        expiresAt: session.expiresAt,
      })

      setQuestions(questionSet.questions)
      setSelectedState(stateCode, stateName)

      // Step 4: Save wizard state to localStorage for resume capability
      saveWizardState(stateCode, stateName, 0)

      // Step 5: Navigate to the wizard
      navigate('/wizard')
    } catch (err: any) {
      console.error('Failed to start wizard:', err)
      setError(err.response?.data?.message || 'Failed to start wizard. Please try again.')
      // Reset store on error
      reset()
    } finally {
      setIsStarting(false)
    }
  }

  return {
    startWizard,
    isStarting,
    error,
  }
}
