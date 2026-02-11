/**
 * Eligibility Result Route
 * Wrapper component that integrates the results page with navigation and session context
 */

import { useNavigate } from 'react-router-dom';
import { useWizardStore } from '@/features/wizard/store';
import { EligibilityResultPage } from '../features/results/EligibilityResultPage';
import { mapAnswersToEligibilityInput } from '../features/results/eligibilityInputMapper';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { AlertCircle } from 'lucide-react';

export function EligibilityResultRoute() {
  const navigate = useNavigate();
  const session = useWizardStore((state) => state.session);
  const answers = useWizardStore((state) => Object.values(state.answers));

  // Get eligibility input from session context
  let eligibilityInput = null;
  if (session && answers && answers.length > 0) {
    try {
      eligibilityInput = mapAnswersToEligibilityInput(session, answers);
    } catch (error) {
      console.error('Failed to map answers to eligibility input:', error);
    }
  }

  // Handle error state - missing session data
  if (!session) {
    return (
      <div className="container mx-auto py-12">
        <Alert variant="destructive" className="mb-6">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Session Not Found</AlertTitle>
          <AlertDescription className="mt-2">
            <p className="mb-4">Unable to load your session. Please start over.</p>
            <Button
              variant="outline"
              onClick={() => navigate('/wizard')}
            >
              Go Back to Wizard
            </Button>
          </AlertDescription>
        </Alert>
      </div>
    );
  }

  // Render results page with navigation callbacks
  return (
    <div className="container mx-auto py-8 px-4">
      <div className="mb-6">
        <Button
          variant="ghost"
          onClick={() => navigate('/wizard')}
          className="flex items-center gap-2"
        >
          ← Back to Wizard
        </Button>
      </div>

      <div className="max-w-2xl mx-auto">
        <EligibilityResultPage
          input={eligibilityInput}
          onBack={() => navigate('/wizard')}
          onRetry={() => {
            // Refetch by remounting component - React Query will handle it
            window.location.reload();
          }}
        />
      </div>
    </div>
  );
}
