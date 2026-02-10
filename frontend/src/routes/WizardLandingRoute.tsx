import { LandingPage } from '../features/wizard/LandingPage'
import { useResumeWizard } from '../features/wizard/useResumeWizard'

/**
 * Route wrapper for the landing page.
 * Handles session resume on initial load.
 */
export function WizardLandingRoute() {
  const { isResuming, error } = useResumeWizard()

  if (isResuming) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <p className="text-muted-foreground">Checking for previous session...</p>
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <div role="alert" className="max-w-md rounded-md border border-destructive bg-destructive/10 p-4">
          <p className="text-sm text-destructive">
            {error}. Please refresh the page to try again.
          </p>
        </div>
      </div>
    )
  }

  return <LandingPage />
}
