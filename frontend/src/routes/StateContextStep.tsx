/**
 * State Context Step - establishes Medicaid jurisdiction context before eligibility evaluation
 * Feature: State Context Initialization Step
 * User Story 1: ZIP Code Entry with State Auto-Detection
 * User Story 2: State Override Option
 */

import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { ZipCodeForm, type ZipCodeFormData } from "../features/state-context/components/ZipCodeForm";
import { StateConfirmation } from "../features/state-context/components/StateConfirmation";
import { StateOverride } from "../features/state-context/components/StateOverride";
import {
  useInitializeStateContext,
  useGetStateContext,
  useUpdateStateContext,
} from "../features/state-context/hooks/useStateContext";
import { Card } from "@/components/ui/card";
import { AlertCircle } from "lucide-react";
import { apiClient } from "@/lib/api";
import { CreateSessionDto, SessionDto } from "@/features/wizard/types";

export function StateContextStep() {
  const navigate = useNavigate();
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [isCreatingSession, setIsCreatingSession] = useState(false);
  const [sessionError, setSessionError] = useState<string | null>(null);
  const [hasInitialized, setHasInitialized] = useState(false);
  const [showStateOverride, setShowStateOverride] = useState(false);

  // Hooks for API calls
  const {
    mutate: initializeStateContext,
    isPending: isInitializing,
    error: initError,
  } = useInitializeStateContext();

  const {
    mutate: updateStateContext,
    isPending: isUpdatingState,
    error: updateError,
  } = useUpdateStateContext();

  const {
    data: existingContext,
    isLoading: isLoadingContext,
    refetch: refetchStateContext,
  } = useGetStateContext(sessionId || "", {
    enabled: !!sessionId && !hasInitialized,
    retry: false,
  });

  // Create session on component mount
  useEffect(() => {
    const createSession = async () => {
      // Check if session already exists in cookie or localStorage
      const existingSessionId = localStorage.getItem("sessionId");
      if (existingSessionId) {
        setSessionId(existingSessionId);
        return;
      }

      setIsCreatingSession(true);
      setSessionError(null);

      try {
        const sessionPayload: CreateSessionDto = {
          timeoutMinutes: 30,
          inactivityTimeoutMinutes: 15,
        };

        const response = await apiClient.post<SessionDto>(
          "/sessions",
          sessionPayload
        );

        const session = response.data;
        setSessionId(session.id);
        localStorage.setItem("sessionId", session.id);
      } catch (err: any) {
        console.error("Failed to create session:", err);
        setSessionError(
          err.response?.data?.message || 
          err.response?.data?.error ||
          "Failed to create session. Please try again."
        );
      } finally {
        setIsCreatingSession(false);
      }
    };

    createSession();
  }, []);

  // Auto-navigate if state context already exists
  useEffect(() => {
    if (existingContext && !hasInitialized) {
      setHasInitialized(true);
    }
  }, [existingContext, hasInitialized]);

  // Handle ZIP code form submission
  const handleZipSubmit = (data: ZipCodeFormData) => {
    if (!sessionId) {
      setSessionError("No session available. Please refresh the page.");
      return;
    }

    initializeStateContext(
      {
        sessionId,
        zipCode: data.zipCode,
      },
      {
        onSuccess: () => {
          setHasInitialized(true);
        },
      },
    );
  };

  // Handle continue to wizard
  const handleContinue = () => {
    navigate("/wizard/step-1");
  };

  // Handle state override - show override form
  const handleShowStateOverride = () => {
    setShowStateOverride(true);
  };

  // Handle cancel state override
  const handleCancelStateOverride = () => {
    setShowStateOverride(false);
  };

  // Handle state change (apply override)
  const handleStateChange = (newStateCode: string) => {
    if (!sessionId || !existingContext?.stateContext) {
      setSessionError("Session or state context not available");
      return;
    }

    updateStateContext(
      {
        sessionId,
        stateCode: newStateCode,
        zipCode: existingContext.stateContext.zipCode,
        isManualOverride: true,
      },
      {
        onSuccess: () => {
          // Refetch state context to update with new state
          refetchStateContext().then(() => {
            setShowStateOverride(false);
          });
        },
        onError: (error) => {
          console.error("Failed to update state context:", error);
        },
      },
    );
  };

  // Loading state while creating session or checking for existing context
  if (isCreatingSession || isLoadingContext) {
    return (
      <div className="container mx-auto max-w-2xl px-4 py-8">
        <div className="space-y-6">
          <div className="text-center">
            <h1 className="text-3xl font-bold tracking-tight">
              Let's Get Started
            </h1>
            <p className="mt-2 text-muted-foreground">
              {isCreatingSession ? "Setting up your session..." : "Loading..."}
            </p>
          </div>
        </div>
      </div>
    );
  }

  // Show session error if session creation failed
  if (sessionError || !sessionId) {
    return (
      <div className="container mx-auto max-w-2xl px-4 py-8">
        <div className="space-y-6">
          <div className="text-center">
            <h1 className="text-3xl font-bold tracking-tight">
              Let's Get Started
            </h1>
          </div>
          <Card className="border-red-200 bg-red-50 p-4 dark:border-red-900 dark:bg-red-950/50">
            <div className="flex items-start gap-3">
              <AlertCircle
                className="h-5 w-5 flex-shrink-0 text-red-600 dark:text-red-500"
                aria-hidden="true"
              />
              <div className="flex-grow space-y-2">
                <h3 className="font-semibold text-red-900 dark:text-red-200">
                  Session Error
                </h3>
                <p className="text-sm text-red-800 dark:text-red-300">
                  {sessionError || "Failed to create session"}
                </p>
                <button
                  onClick={() => window.location.reload()}
                  className="mt-2 text-sm font-medium text-red-900 underline underline-offset-4 dark:text-red-200"
                >
                  Try Again
                </button>
              </div>
            </div>
          </Card>
        </div>
      </div>
    );
  }

  // Determine current state data
  const stateData = existingContext?.stateContext;
  const stateConfig = existingContext?.stateConfiguration;

  return (
    <div className="container mx-auto max-w-2xl px-4 py-8">
      <div className="space-y-6">
        {/* Header */}
        <div className="text-center">
          <h1 className="text-3xl font-bold tracking-tight">
            Let's Get Started
          </h1>
          <p className="mt-2 text-muted-foreground">
            First, we need to know which state you're applying in.
          </p>
        </div>

        {/* Error Display */}
        {(initError || updateError) && (
          <Card className="border-red-200 bg-red-50 p-4 dark:border-red-900 dark:bg-red-950/50">
            <div className="flex items-start gap-3">
              <AlertCircle
                className="h-5 w-5 flex-shrink-0 text-red-600 dark:text-red-500"
                aria-hidden="true"
              />
              <div className="flex-grow space-y-1">
                <h3 className="font-semibold text-red-900 dark:text-red-200">
                  Error
                </h3>
                <p className="text-sm text-red-800 dark:text-red-300">
                  {initError?.message || updateError?.message || "An error occurred"}
                </p>
              </div>
            </div>
          </Card>
        )}

        {/* Main Content */}
        {!hasInitialized ? (
          /* Show ZIP code form if no state context exists */
          <Card className="p-6">
            <div className="space-y-4">
              <div>
                <h2 className="text-lg font-semibold">Enter Your ZIP Code</h2>
                <p className="text-sm text-muted-foreground">
                  We'll automatically detect which state's Medicaid program
                  applies to you based on your ZIP code.
                </p>
              </div>
              <ZipCodeForm
                onSubmit={handleZipSubmit}
                isLoading={isInitializing}
                error={initError?.message || null}
              />
            </div>
          </Card>
        ) : stateData && stateConfig ? (
          <>
            {showStateOverride ? (
              /* Show state override form if state change requested */
              <StateOverride
                currentStateCode={stateData.stateCode}
                currentZipCode={stateData.zipCode}
                onStateChange={handleStateChange}
                isLoading={isUpdatingState}
                error={updateError?.message || null}
                onCancel={handleCancelStateOverride}
              />
            ) : (
              /* Show state confirmation if state context exists */
              <StateConfirmation
                stateName={stateData.stateName}
                stateCode={stateData.stateCode}
                zipCode={stateData.zipCode}
                stateConfiguration={stateConfig}
                isManualOverride={stateData.isManualOverride}
                onContinue={handleContinue}
                onChangeState={handleShowStateOverride}
                isLoading={false}
              />
            )}
          </>
        ) : null}

        {/* Back Button (only show when entering ZIP) */}
        {!hasInitialized && (
          <div className="flex justify-start">
            <button
              type="button"
              onClick={() => navigate("/")}
              className="text-sm text-muted-foreground underline-offset-4 hover:underline"
            >
              ← Back to Home
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
