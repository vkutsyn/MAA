import { LandingPage } from '../features/wizard/LandingPage'
import { useSessionBootstrap } from '../features/wizard/useSession'

/**
 * Route wrapper for the landing page.
 * Handles session bootstrap on initial load.
 */
export function WizardLandingRoute() {
  const { isBootstrapped, error } = useSessionBootstrap()

  if (!isBootstrapped) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <p className="text-muted-foreground">Loading...</p>
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
