/**
 * Eligibility Result Page
 * Main page component that orchestrates the results view
 */

import { ReactNode } from 'react';
import { EligibilityResultView, UserEligibilityInput } from './types';
import { useEligibilityResult } from './useEligibilityResult';
import { EligibilityStatusCard } from './components/EligibilityStatusCard';
import { ProgramMatchesCard } from './components/ProgramMatchesCard';
import { ConfidenceIndicator } from './components/ConfidenceIndicator';
import { ExplanationList } from './components/ExplanationList';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { AlertCircle, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface EligibilityResultPageProps {
  input?: UserEligibilityInput | null;
  onRetry?: () => void;
  onBack?: () => void;
}

function LoadingState(): ReactNode {
  return (
    <div className="flex flex-col items-center justify-center py-12 space-y-4">
      <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
      <div className="text-center">
        <h3 className="font-semibold text-gray-900">Evaluating Eligibility</h3>
        <p className="text-sm text-gray-600 mt-1">This may take a few moments...</p>
      </div>
    </div>
  );
}

function ErrorState({
  error,
  onRetry,
  onBack,
}: {
  error: string;
  onRetry?: () => void;
  onBack?: () => void;
}): ReactNode {
  return (
    <Alert variant="destructive" className="mb-6">
      <AlertCircle className="h-4 w-4" />
      <AlertTitle>Evaluation Error</AlertTitle>
      <AlertDescription className="mt-2">
        <p className="mb-4">{error}</p>
        <div className="flex gap-2">
          {onRetry && (
            <Button variant="outline" size="sm" onClick={onRetry}>
              Retry
            </Button>
          )}
          {onBack && (
            <Button variant="outline" size="sm" onClick={onBack}>
              Go Back
            </Button>
          )}
        </div>
      </AlertDescription>
    </Alert>
  );
}

function EmptyState(): ReactNode {
  return (
    <Alert className="mb-6">
      <AlertCircle className="h-4 w-4" />
      <AlertTitle>No Results</AlertTitle>
      <AlertDescription>
        Unable to retrieve eligibility results. Please try again or contact support.
      </AlertDescription>
    </Alert>
  );
}

interface ResultsContentProps {
  result: EligibilityResultView;
  children?: ReactNode;
}

export function ResultsContent({ result, children }: ResultsContentProps) {
  return (
    <div className="space-y-6">
      <EligibilityStatusCard result={result} />
      <ProgramMatchesCard result={result} />
      <ConfidenceIndicator
        confidenceScore={result.confidenceScore}
        confidenceLabel={result.confidenceLabel}
        description={result.confidenceDescription}
      />
      <ExplanationList
        explanation={result.explanation}
        bullets={result.explanationBullets}
      />
      {children}
    </div>
  );
}

export function EligibilityResultPage({
  input,
  onRetry,
  onBack,
}: EligibilityResultPageProps) {
  const { data, isLoading, isError, error, refetch } = useEligibilityResult(input || null);

  // Determine state and render accordingly
  if (isLoading) {
    return <LoadingState />;
  }

  if (isError) {
    const errorMessage = error instanceof Error ? error.message : 'An unknown error occurred';
    return (
      <ErrorState
        error={errorMessage}
        onRetry={() => {
          refetch();
          onRetry?.();
        }}
        onBack={onBack}
      />
    );
  }

  if (!data) {
    return <EmptyState />;
  }

  // Render success state with results content
  return (
    <ResultsContent result={data}>
      {/* Additional sections will be integrated here in later tasks */}
    </ResultsContent>
  );
}
