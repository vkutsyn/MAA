import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { StateSelector } from './StateSelector'
import { useStartWizard } from './useStartWizard'

/**
 * Landing page for the Medicaid eligibility wizard.
 * Displays information, state selection, and Start button.
 * Meets WCAG 2.1 AA and mobile-first design requirements.
 */
export function LandingPage() {
  const [selectedState, setSelectedState] = useState<{
    code: string
    name: string
  } | null>(null)
  
  const { startWizard, isStarting, error } = useStartWizard()

  const handleStateSelected = (stateCode: string, stateName: string) => {
    setSelectedState({ code: stateCode, name: stateName })
  }

  const handleStart = () => {
    if (selectedState) {
      startWizard(selectedState.code, selectedState.name)
    }
  }

  return (
    <div className="mx-auto max-w-2xl space-y-8">
      {/* Hero Section */}
      <div className="space-y-4 text-center">
        <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">
          Check Your Medicaid Eligibility
        </h1>
        <p className="text-lg text-muted-foreground">
          Answer a few quick questions to see if you may qualify for Medicaid in your state.
          This tool is free, confidential, and takes about 5 minutes.
        </p>
      </div>

      {/* Main Card */}
      <Card>
        <CardHeader>
          <CardTitle>Get Started</CardTitle>
          <CardDescription>
            Select your state to begin the eligibility check. We'll ask you questions specific
            to your state's Medicaid program.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* State Selection */}
          <StateSelector
            onStateSelected={handleStateSelected}
            disabled={isStarting}
          />

          {/* Error Message */}
          {error && (
            <div
              role="alert"
              className="rounded-md border border-destructive bg-destructive/10 p-4"
            >
              <p className="text-sm text-destructive">{error}</p>
            </div>
          )}

          {/* Start Button */}
          <div className="flex justify-end">
            <Button
              onClick={handleStart}
              disabled={!selectedState || isStarting}
              size="lg"
              className="w-full sm:w-auto"
              aria-label={
                selectedState
                  ? `Start eligibility check for ${selectedState.name}`
                  : 'Select a state to start'
              }
            >
              {isStarting ? 'Starting...' : 'Start Eligibility Check'}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Info Section */}
      <Card className="bg-muted/50">
        <CardHeader>
          <CardTitle className="text-base">What to Expect</CardTitle>
        </CardHeader>
        <CardContent>
          <ul className="space-y-2 text-sm text-muted-foreground">
            <li className="flex gap-2">
              <span className="font-semibold">•</span>
              <span>
                <strong>Quick questions:</strong> We'll ask about your household size, income,
                age, and other basic information.
              </span>
            </li>
            <li className="flex gap-2">
              <span className="font-semibold">•</span>
              <span>
                <strong>Save your progress:</strong> You can leave and come back - your answers
                will be saved for 30 minutes.
              </span>
            </li>
            <li className="flex gap-2">
              <span className="font-semibold">•</span>
              <span>
                <strong>Get results:</strong> At the end, we'll tell you if you may be eligible
                and what steps to take next.
              </span>
            </li>
            <li className="flex gap-2">
              <span className="font-semibold">•</span>
              <span>
                <strong>Privacy:</strong> Your information is encrypted and not stored
                permanently unless you choose to save your application.
              </span>
            </li>
          </ul>
        </CardContent>
      </Card>

      {/* Disclaimer */}
      <p className="text-center text-xs text-muted-foreground">
        This tool provides an estimate only. Final eligibility is determined by your state's
        Medicaid agency after you submit a complete application.
      </p>
    </div>
  )
}
