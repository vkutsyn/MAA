import { useWizardStore } from "./store";
import { calculateProgress } from "./flow";

/**
 * Progress indicator showing current step and completion percentage.
 * Meets WCAG 2.1 AA with semantic elements and ARIA attributes.
 * Uses conditional flow to show only visible questions in progress.
 */
export function WizardProgress() {
  const questions = useWizardStore((state) => state.questions);
  const currentStep = useWizardStore((state) => state.currentStep);
  const answers = useWizardStore((state) => state.answers);

  // Calculate progress based on visible questions (respects conditional flow)
  const progress = calculateProgress(questions, currentStep, answers);
  const { currentVisibleIndex, totalVisibleQuestions, completionPercent } =
    progress;

  // Calculate step display (1-based for users)
  const currentStepDisplay = currentVisibleIndex + 1;
  const totalSteps = totalVisibleQuestions;

  return (
    <div className="space-y-2" role="region" aria-label="Progress indicator">
      {/* Step counter */}
      <div className="flex items-center justify-between text-sm">
        <span className="font-medium">
          Question {currentStepDisplay} of {totalSteps}
        </span>
        <span className="text-muted-foreground">
          {completionPercent}% complete
        </span>
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
  );
}
