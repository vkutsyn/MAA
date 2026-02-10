/**
 * State Confirmation Component
 * Feature: State Context Initialization Step
 * User Story 1: Display detected state and configuration
 */

import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import type { StateConfiguration } from "../types/stateContext.types";
import { CheckCircle2, MapPin, ExternalLink } from "lucide-react";

interface StateConfirmationProps {
  stateName: string;
  stateCode: string;
  zipCode: string;
  stateConfiguration: StateConfiguration;
  isManualOverride?: boolean;
  onContinue: () => void;
  onChangeState?: () => void;
  isLoading?: boolean;
}

/**
 * Displays detected state and state-specific Medicaid program information
 *
 * Features:
 * - Shows detected state with visual confirmation
 * - Displays state Medicaid program name and contact info
 * - Optional "Change State" button for manual override (User Story 2)
 * - Accessible card layout with semantic HTML
 *
 * @param stateName - Full state name (e.g., "California")
 * @param stateCode - 2-letter state code (e.g., "CA")
 * @param zipCode - User's ZIP code
 * @param stateConfiguration - State-specific Medicaid configuration
 * @param isManualOverride - Whether state was manually selected
 * @param onContinue - Callback when user clicks Continue
 * @param onChangeState - Optional callback for manual state override
 * @param isLoading - Whether an action is in progress
 */
export function StateConfirmation({
  stateName,
  stateCode: _stateCode,
  zipCode,
  stateConfiguration,
  isManualOverride = false,
  onContinue,
  onChangeState,
  isLoading = false,
}: StateConfirmationProps) {
  return (
    <div className="space-y-4">
      {/* Detection Confirmation */}
      <Card className="p-6">
        <div className="flex items-start gap-4">
          <div
            className="flex-shrink-0 rounded-full bg-green-100 p-2 text-green-600 dark:bg-green-900/20"
            aria-hidden="true"
          >
            <CheckCircle2 className="h-6 w-6" />
          </div>
          <div className="flex-grow space-y-1">
            <h2 className="text-lg font-semibold">
              {isManualOverride ? "State Selected" : "State Detected"}
            </h2>
            <div className="flex items-center gap-2 text-muted-foreground">
              <MapPin className="h-4 w-4" aria-hidden="true" />
              <span>
                ZIP Code: <strong>{zipCode}</strong>
              </span>
            </div>
            <p className="text-2xl font-bold text-primary">{stateName}</p>
            {onChangeState && (
              <Button
                variant="link"
                className="h-auto p-0 text-sm"
                onClick={onChangeState}
                disabled={isLoading}
              >
                Change State
              </Button>
            )}
          </div>
        </div>
      </Card>

      {/* State Medicaid Program Information */}
      <Card className="p-6">
        <div className="space-y-4">
          <div>
            <h3 className="text-base font-semibold">Medicaid Program</h3>
            <p className="text-lg text-muted-foreground">
              {stateConfiguration.medicaidProgramName}
            </p>
          </div>

          <div className="space-y-2">
            <h4 className="text-sm font-medium">Contact Information</h4>
            <div className="space-y-1 text-sm text-muted-foreground">
              <p>
                <strong>Phone:</strong>{" "}
                <a
                  href={`tel:${stateConfiguration.contactInfo.phone.replace(/\D/g, "")}`}
                  className="underline hover:text-foreground"
                >
                  {stateConfiguration.contactInfo.phone}
                </a>
              </p>
              <p>
                <strong>Website:</strong>{" "}
                <a
                  href={stateConfiguration.contactInfo.website}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1 underline hover:text-foreground"
                >
                  Visit Program Website
                  <ExternalLink className="h-3 w-3" aria-hidden="true" />
                  <span className="sr-only">(opens in new tab)</span>
                </a>
              </p>
            </div>
          </div>

          {stateConfiguration.additionalNotes && (
            <div className="rounded-md bg-muted p-3">
              <p className="text-sm">{stateConfiguration.additionalNotes}</p>
            </div>
          )}
        </div>
      </Card>

      {/* Continue Button */}
      <div className="flex justify-end">
        <Button
          onClick={onContinue}
          disabled={isLoading}
          size="lg"
          className="min-w-[200px]"
        >
          {isLoading ? "Loading..." : "Continue to Application"}
        </Button>
      </div>
    </div>
  );
}
