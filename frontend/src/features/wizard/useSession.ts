import { useEffect, useState } from 'react'
import { apiClient } from '@/lib/api'
import { useWizardStore } from './store'
import { SessionDto } from './types'

/**
 * Hook to bootstrap wizard session on initial load.
 * Checks for existing session cookie and restores state if valid.
 */
export function useSessionBootstrap() {
  const [isBootstrapped, setIsBootstrapped] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const { setSession, reset } = useWizardStore()

  useEffect(() => {
    const bootstrap = async () => {
      try {
        // Try to fetch existing session (middleware will validate cookie)
        // If no cookie or expired, a 401 will be returned
        const response = await apiClient.get<SessionDto>('/sessions/me')
        
        if (response.data) {
          // Valid session exists - restore it
          setSession({
            sessionId: response.data.id,
            stateCode: '', // Will be set when user selects state
            stateName: '',
            currentStep: 0,
            totalSteps: 0,
            expiresAt: response.data.expiresAt,
          })
        }
      } catch (err: any) {
        if (err.response?.status === 401 || err.response?.status === 404) {
          // No valid session - start fresh
          reset()
        } else {
          // Other error
          setError('Failed to bootstrap session')
          console.error('Session bootstrap error:', err)
        }
      } finally {
        setIsBootstrapped(true)
      }
    }

    bootstrap()
  }, [setSession, reset])

  return {
    isBootstrapped,
    error,
  }
}
