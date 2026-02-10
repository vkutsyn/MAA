import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { StateSelector } from "./StateSelector";
import { useStartWizard } from "./useStartWizard";

/**
 * Landing page for the Medicaid eligibility wizard.
 * Displays information, state selection, and Start button.
 * Meets WCAG 2.1 AA and mobile-first design requirements.
 *
 * Accessibility features:
 * - Semantic HTML (main landmark, sections, headings)
 * - Proper heading hierarchy (h1 > h2)
 * - ARIA labels for interactive elements
 * - Focus management on error states
 * - Clear, descriptive labels for form inputs
 */
export function LandingPage() {
  const [selectedState, setSelectedState] = useState<{
    code: string;
    name: string;
  } | null>(null);

  const { startWizard, isStarting, error } = useStartWizard();

  const handleStateSelected = (stateCode: string, stateName: string) => {
    setSelectedState({ code: stateCode, name: stateName });
  };

  const handleStart = () => {
    if (selectedState) {
      startWizard(selectedState.code, selectedState.name);
    }
  };

  return (
    <main className="mx-auto max-w-2xl space-y-8 px-4 py-8">
      {/* Hero Section */}
      <section className="space-y-4 text-center">
        <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">
          Check Your Medicaid Eligibility
        </h1>
        <p className="text-lg text-muted-foreground">
          Answer a few quick questions to see if you may qualify for Medicaid in
          your state. This tool is free, confidential, and takes about 5
          minutes.
        </p>
      </section>

      {/* Main Card */}
      <Card>
        <CardHeader>
          <CardTitle>
            <h2>Get Started</h2>
          </CardTitle>
          <CardDescription>
            Select your state to begin the eligibility check. We'll ask you
            questions specific to your state's Medicaid program.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* State Selection Form */}
          <form
            onSubmit={(e) => {
              e.preventDefault();
              handleStart();
            }}
            aria-label="Eligibility wizard startup form"
          >
            {/* State Selection */}
            <StateSelector
              onStateSelected={handleStateSelected}
              disabled={isStarting}
            />

            {/* Error Message */}
            {error && (
              <div
                role="alert"
                aria-live="assertive"
                aria-label="Startup error"
                className="my-6 rounded-md border border-destructive bg-destructive/10 p-4"
              >
                <p className="text-sm text-destructive font-medium">{error}</p>
              </div>
            )}

            {/* Start Button */}
            <div className="mt-6 flex justify-end">
              <Button
                type="submit"
                disabled={!selectedState || isStarting}
                size="lg"
                className="w-full sm:w-auto"
                aria-label={
                  selectedState
                    ? `Start eligibility check for ${selectedState.name}`
                    : "Select a state above then click to start"
                }
              >
                {isStarting ? (
                  <>
                    <span aria-hidden="true">Starting...</span>
                    <span className="sr-only">
                      Starting your eligibility check
                    </span>
                  </>
                ) : (
                  "Start Eligibility Check"
                )}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Info Section */}
      <Card className="bg-muted/50">
        <CardHeader>
          <CardTitle>
            <h2 className="text-base">What to Expect</h2>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <ul
            className="space-y-2 text-sm text-muted-foreground"
            aria-label="Wizard process information"
          >
            <li className="flex gap-2">
              <span className="font-semibold" aria-hidden="true">
                •
              </span>
              <span>
                <strong>Quick questions:</strong> We'll ask about your household
                size, income, age, and other basic information.
              </span>
            </li>
            <li className="flex gap-2">
              <span className="font-semibold" aria-hidden="true">
                •
              </span>
              <span>
                <strong>Save your progress:</strong> You can leave and come back
                - your answers will be saved for 30 minutes.
              </span>
            </li>
            <li className="flex gap-2">
              <span className="font-semibold" aria-hidden="true">
                •
              </span>
              <span>
                <strong>Get results:</strong> At the end, we'll tell you if you
                may be eligible and what steps to take next.
              </span>
            </li>
            <li className="flex gap-2">
              <span className="font-semibold" aria-hidden="true">
                •
              </span>
              <span>
                <strong>Privacy:</strong> Your information is encrypted and not
                stored permanently unless you choose to save your application.
              </span>
            </li>
          </ul>
        </CardContent>
      </Card>

      {/* Disclaimer */}
      <section className="text-center">
        <p className="text-xs text-muted-foreground">
          <span className="font-semibold">Disclaimer:</span> This tool provides
          an estimate only. Final eligibility is determined by your state's
          Medicaid agency after you submit a complete application.
        </p>
      </section>
    </main>
  );
}
