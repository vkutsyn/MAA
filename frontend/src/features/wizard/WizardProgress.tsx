import { useWizardStore } from './store'

/**
 * Progress indicator showing current step and completion percentage.
 * Meets WCAG 2.1 AA with semantic elements and ARIA attributes.
 */
export function WizardProgress() {
  const progress = useWizardStore((state) => state.getProgress())
  const { currentIndex, totalSteps, completionPercent } = progress

  // Calculate step display (1-based for users)
  const currentStepDisplay = currentIndex + 1

  return (
    <div className="space-y-2" role="region" aria-label="Progress indicator">
      {/* Step counter */}
      <div className="flex items-center justify-between text-sm">
        <span className="font-medium">
          Question {currentStepDisplay} of {totalSteps}
        </span>
        <span className="text-muted-foreground">{completionPercent}% complete</span>
      </div>

      {/* Progress bar */}
      <div
        className="h-2 w-full overflow-hidden rounded-full bg-secondary"
        role="progressbar"
        aria-valuenow={completionPercent}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={`${completionPercent}% complete`}
      >
        <div
          className="h-full bg-primary transition-all duration-300 ease-in-out"
          style={{ width: `${completionPercent}%` }}
        />
      </div>
    </div>
  )
}
