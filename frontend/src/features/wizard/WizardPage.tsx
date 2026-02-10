import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useWizardStore } from './store'
import { WizardProgress } from './WizardProgress'
import { WizardStep } from './WizardStep'
import { saveAnswer } from './answerApi'
import { Answer } from './types'

/**
 * Main wizard page that orchestrates the question flow.
 * Displays current question, handles navigation, and persists answers.
 */
export function WizardPage() {
  const navigate = useNavigate()
  const {
    session,
    questions,
    currentStep,
    selectedState,
    setAnswer,
    goToNextStep,
    goToPreviousStep,
  } = useWizardStore()

  // Redirect to landing if no session or questions
  useEffect(() => {
    if (!session || questions.length === 0) {
      navigate('/')
    }
  }, [session, questions, navigate])

  // Handle answer submission and move to next step
  const handleNext = async (answer: Answer) => {
    // Save answer to store
    setAnswer(answer.fieldKey, answer)

    try {
      // Persist answer to backend
      await saveAnswer({
        fieldKey: answer.fieldKey,
        fieldType: answer.fieldType,
        answerValue: answer.answerValue,
        isPii: answer.isPii,
      })

      // Move to next step
      goToNextStep()

      // If we've completed all questions, navigate to results (placeholder)
      if (currentStep >= questions.length - 1) {
        // TODO: Navigate to results page in future phase
        alert('Wizard complete! Results page coming in next phase.')
        navigate('/')
      }
    } catch (error) {
      console.error('Failed to save answer:', error)
      alert('Failed to save your answer. Please try again.')
    }
  }

  // Handle back navigation
  const handleBack = () => {
    goToPreviousStep()
  }

  // Guard: Show loading if not ready
  if (!session || questions.length === 0) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <p className="text-muted-foreground">Loading...</p>
      </div>
    )
  }

  const currentQuestion = questions[currentStep]

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      {/* Header */}
      <div className="space-y-2">
        <h1 className="text-2xl font-bold">
          {selectedState?.name} Medicaid Eligibility
        </h1>
        <p className="text-sm text-muted-foreground">
          Answer each question to check your eligibility. Your progress is automatically saved.
        </p>
      </div>

      {/* Progress indicator */}
      <WizardProgress />

      {/* Question card */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Question {currentStep + 1}</CardTitle>
        </CardHeader>
        <CardContent>
          <WizardStep
            question={currentQuestion}
            onNext={handleNext}
            onBack={handleBack}
          />
        </CardContent>
      </Card>

      {/* Help section */}
      <Card className="bg-muted/50">
        <CardContent className="pt-6">
          <p className="text-sm text-muted-foreground">
            <strong>Need help?</strong> Your answers are saved automatically. You can close this
            page and return later - your session will remain active for 30 minutes of inactivity.
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
