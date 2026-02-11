/**
 * Eligibility Result Route
 * Wrapper component that integrates the results page with navigation and session context
 */

import { useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { RequireAuth } from "@/features/auth/RequireAuth";
import { useWizardStore } from "@/features/wizard/store";
import { EligibilityResultPage } from "../features/results/EligibilityResultPage";
import { mapAnswersToEligibilityInput } from "../features/results/eligibilityInputMapper";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { AlertCircle } from "lucide-react";
import type { Answer } from "../features/wizard/types";

export function EligibilityResultRoute() {
  const navigate = useNavigate();
  const session = useWizardStore((state) => state.session);
  const answersRecord = useWizardStore((state) => state.answers);

  // Memoize eligibility input to prevent unnecessary recalculations
  const eligibilityInput = useMemo(() => {
    const answers: Answer[] = Object.values(answersRecord);

    if (!session || answers.length === 0) {
      return null;
    }

    try {
      return mapAnswersToEligibilityInput(session, answers);
    } catch (error) {
      console.error("Failed to map answers to eligibility input:", error);
      return null;
    }
  }, [session, answersRecord]);

  // Handle error state - missing session data
  if (!session) {
    return (
      <RequireAuth>
        <div className="container mx-auto py-12">
          <Alert variant="destructive" className="mb-6">
            <AlertCircle className="h-4 w-4" />
            <AlertTitle>Session Not Found</AlertTitle>
            <AlertDescription className="mt-2">
              <p className="mb-4">
                Unable to load your session. Please start over.
              </p>
              <Button variant="outline" onClick={() => navigate("/wizard")}>
                Go Back to Wizard
              </Button>
            </AlertDescription>
          </Alert>
        </div>
      </RequireAuth>
    );
  }

  // Render results page with navigation callbacks
  return (
    <RequireAuth>
      <div className="container mx-auto py-8 px-4">
        <div className="mb-6">
          <Button
            variant="ghost"
            onClick={() => navigate("/wizard")}
            className="flex items-center gap-2"
          >
            ← Back to Wizard
          </Button>
        </div>

        <div className="max-w-2xl mx-auto">
          <EligibilityResultPage
            input={eligibilityInput}
            onBack={() => navigate("/wizard")}
            onRetry={() => {
              // Refetch by remounting component - React Query will handle it
              window.location.reload();
            }}
          />
        </div>
      </div>
    </RequireAuth>
  );
}
