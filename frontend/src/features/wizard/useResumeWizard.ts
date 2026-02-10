import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '@/lib/api'
import { useWizardStore } from './store'
import { fetchAnswers } from './answerApi'
import { fetchQuestions } from './questionApi'
import { SessionDto, SessionAnswerDto, Answer } from './types'
import { getVisibleQuestions } from './flow'

/**
 * Hook to resume wizard state after page refresh.
 * Checks for existing session, restores answers, and navigates to last step.
 */
export function useResumeWizard() {
  const [isResuming, setIsResuming] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const navigate = useNavigate()
  
  const {
    setSession,
    setQuestions,
    setAnswers,
    setCurrentStep,
    setSelectedState,
    reset,
  } = useWizardStore()

  useEffect(() => {
    const resumeWizard = async () => {
      try {
        // Check for wizard state in localStorage
        const savedWizardState = localStorage.getItem('maa_wizard_state')
        
        if (!savedWizardState) {
          // No saved state - user hasn't started wizard or cleared data
          setIsResuming(false)
          return
        }

        const wizardState = JSON.parse(savedWizardState) as {
          stateCode: string
          stateName: string
          lastStep: number
          timestamp: number
        }

        // Check if saved state is still valid (within 30 minutes)
        const now = Date.now()
        const thirtyMinutes = 30 * 60 * 1000
        if (now - wizardState.timestamp > thirtyMinutes) {
          // Saved state expired - clear it
          localStorage.removeItem('maa_wizard_state')
          setIsResuming(false)
          return
        }

        // Validate session cookie exists and is valid
        let session: SessionDto
        try {
          const sessionResponse = await apiClient.get<SessionDto>('/sessions/me')
          session = sessionResponse.data
        } catch (err: any) {
          if (err.response?.status === 401 || err.response?.status === 404) {
            // Session expired or not found - clear saved state
            localStorage.removeItem('maa_wizard_state')
            reset()
            setIsResuming(false)
            return
          }
          throw err
        }

        // Fetch saved answers from backend
        const savedAnswers = await fetchAnswers()

        // Fetch questions for the saved state
        const questionSet = await fetchQuestions(wizardState.stateCode)

        // Convert backend answers to store format
        const answersMap: Record<string, Answer> = {}
        savedAnswers.forEach((answer: SessionAnswerDto) => {
          answersMap[answer.fieldKey] = {
            fieldKey: answer.fieldKey,
            answerValue: answer.answerValue || '',
            fieldType: answer.fieldType as Answer['fieldType'],
            isPii: answer.isPii,
          }
        })

        // Calculate which step to resume from
        // Find the last answered question in the visible question flow
        const visibleQuestions = getVisibleQuestions(questionSet.questions, answersMap)
        let resumeStep = 0

        // Find the first unanswered question, or last question if all are answered
        for (let i = 0; i < questionSet.questions.length; i++) {
          const question = questionSet.questions[i]
          const isVisible = visibleQuestions.some((q) => q.key === question.key)
          
          if (isVisible) {
            if (!answersMap[question.key]) {
              // First unanswered question - resume here
              resumeStep = i
              break
            }
            // Update resumeStep to last answered question
            resumeStep = i
          }
        }

        // Restore wizard state
        setSession({
          sessionId: session.id,
          stateCode: wizardState.stateCode,
          stateName: wizardState.stateName,
          currentStep: resumeStep,
          totalSteps: questionSet.questions.length,
          expiresAt: session.expiresAt,
        })

        setQuestions(questionSet.questions)
        setAnswers(Object.values(answersMap))
        setCurrentStep(resumeStep)
        setSelectedState(wizardState.stateCode, wizardState.stateName)

        // Navigate to wizard page if answers exist
        if (savedAnswers.length > 0) {
          navigate('/wizard', { replace: true })
        }

        setError(null)
      } catch (err: any) {
        console.error('Failed to resume wizard:', err)
        setError('Failed to restore your previous session')
        
        // Clear invalid state
        localStorage.removeItem('maa_wizard_state')
        reset()
      } finally {
        setIsResuming(false)
      }
    }

    resumeWizard()
  }, [navigate, setSession, setQuestions, setAnswers, setCurrentStep, setSelectedState, reset])

  return {
    isResuming,
    error,
  }
}

/**
 * Save current wizard state to localStorage for resume capability.
 * Call this whenever the wizard state changes (state selection, step navigation).
 */
export function saveWizardState(
  stateCode: string,
  stateName: string,
  currentStep: number
): void {
  try {
    const wizardState = {
      stateCode,
      stateName,
      lastStep: currentStep,
      timestamp: Date.now(),
    }
    localStorage.setItem('maa_wizard_state', JSON.stringify(wizardState))
  } catch (err) {
    console.warn('Failed to save wizard state to localStorage:', err)
    // Non-critical - continue without saving
  }
}

/**
 * Clear wizard state from localStorage.
 * Call this when the wizard is completed or abandoned.
 */
export function clearWizardState(): void {
  try {
    localStorage.removeItem('maa_wizard_state')
  } catch (err) {
    console.warn('Failed to clear wizard state from localStorage:', err)
  }
}
