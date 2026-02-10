import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";

/**
 * Landing page for the Medicaid eligibility wizard.
 * Displays information and Start button to begin the eligibility check.
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
  const navigate = useNavigate();

  const handleStart = () => {
    // Navigate to state context step where user will enter ZIP code
    navigate("/state-context");
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
            Click the button below to begin your eligibility check. We'll ask
            for your ZIP code to determine which state's Medicaid program
            applies to you.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* Start Button */}
          <div className="flex justify-center">
            <Button
              type="button"
              onClick={handleStart}
              size="lg"
              className="w-full sm:w-auto"
              aria-label="Start eligibility check"
            >
              Start Eligibility Check
            </Button>
          </div>
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
