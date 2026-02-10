/**
 * State Override Component
 * Feature: State Context Initialization Step
 * User Story 2: Manual state selection for edge cases
 */

import { useState, useMemo } from "react";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { AlertCircle } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

/**
 * List of all US states and territories (for state selector)
 * Used for manual state override (User Story 2)
 */
const US_STATES = [
  { code: "AL", name: "Alabama" },
  { code: "AK", name: "Alaska" },
  { code: "AZ", name: "Arizona" },
  { code: "AR", name: "Arkansas" },
  { code: "CA", name: "California" },
  { code: "CO", name: "Colorado" },
  { code: "CT", name: "Connecticut" },
  { code: "DE", name: "Delaware" },
  { code: "DC", name: "District of Columbia" },
  { code: "FL", name: "Florida" },
  { code: "GA", name: "Georgia" },
  { code: "HI", name: "Hawaii" },
  { code: "ID", name: "Idaho" },
  { code: "IL", name: "Illinois" },
  { code: "IN", name: "Indiana" },
  { code: "IA", name: "Iowa" },
  { code: "KS", name: "Kansas" },
  { code: "KY", name: "Kentucky" },
  { code: "LA", name: "Louisiana" },
  { code: "ME", name: "Maine" },
  { code: "MD", name: "Maryland" },
  { code: "MA", name: "Massachusetts" },
  { code: "MI", name: "Michigan" },
  { code: "MN", name: "Minnesota" },
  { code: "MS", name: "Mississippi" },
  { code: "MO", name: "Missouri" },
  { code: "MT", name: "Montana" },
  { code: "NE", name: "Nebraska" },
  { code: "NV", name: "Nevada" },
  { code: "NH", name: "New Hampshire" },
  { code: "NJ", name: "New Jersey" },
  { code: "NM", name: "New Mexico" },
  { code: "NY", name: "New York" },
  { code: "NC", name: "North Carolina" },
  { code: "ND", name: "North Dakota" },
  { code: "OH", name: "Ohio" },
  { code: "OK", name: "Oklahoma" },
  { code: "OR", name: "Oregon" },
  { code: "PA", name: "Pennsylvania" },
  { code: "RI", name: "Rhode Island" },
  { code: "SC", name: "South Carolina" },
  { code: "SD", name: "South Dakota" },
  { code: "TN", name: "Tennessee" },
  { code: "TX", name: "Texas" },
  { code: "UT", name: "Utah" },
  { code: "VT", name: "Vermont" },
  { code: "VA", name: "Virginia" },
  { code: "WA", name: "Washington" },
  { code: "WV", name: "West Virginia" },
  { code: "WI", name: "Wisconsin" },
  { code: "WY", name: "Wyoming" },
];

interface StateOverrideProps {
  /**
   * Current state code (for pre-selection)
   */
  currentStateCode: string;

  /**
   * Current ZIP code (used for the update request)
   */
  currentZipCode: string;

  /**
   * Callback when user selects a new state
   * @param stateCode The selected state code
   */
  onStateChange: (stateCode: string) => void;

  /**
   * Whether the operation is loading
   */
  isLoading?: boolean;

  /**
   * Error message to display, if any
   */
  error?: string | null;

  /**
   * Callback to close/cancel the override
   */
  onCancel: () => void;
}

/**
 * Component to allow users to manually override their detected state
 *
 * Features:
 * - Dropdown list of all US states
 * - Pre-selects current state
 * - Shows error messages
 * - Keyboard accessible (Tab, Enter, Arrow keys)
 * - Screen reader support
 *
 * @example
 * <StateOverride
 *   currentStateCode="CA"
 *   currentZipCode="90210"
 *   onStateChange={(code, name) => // update state}
 *   onCancel={() => // go back}
 * />
 */
export function StateOverride({
  currentStateCode,
  currentZipCode,
  onStateChange,
  isLoading = false,
  error = null,
  onCancel,
}: StateOverrideProps) {
  const [selectedStateCode, setSelectedStateCode] = useState(currentStateCode);

  // Find current and selected state names for display
  const currentState = useMemo(
    () => US_STATES.find((s) => s.code === currentStateCode),
    [currentStateCode],
  );

  const selectedState = useMemo(
    () => US_STATES.find((s) => s.code === selectedStateCode),
    [selectedStateCode],
  );

  // Handle state selection
  const handleSelectState = () => {
    if (selectedState && selectedState.code !== currentStateCode) {
      onStateChange(selectedState.code);
    }
  };

  // Handle cancel
  const handleCancel = () => {
    setSelectedStateCode(currentStateCode);
    onCancel();
  };

  return (
    <Card className="p-6">
      <div className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold">Select Your State</h2>
          <p className="text-sm text-muted-foreground">
            If we detected the wrong state, you can change it here.
          </p>
        </div>

        {/* Current State Display */}
        <div className="rounded-md bg-muted p-3">
          <p className="text-sm">
            <strong>Current State:</strong> {currentState?.name || "Unknown"}
          </p>
          <p className="text-sm">
            <strong>ZIP Code:</strong> {currentZipCode}
          </p>
        </div>

        {/* Error Display */}
        {error && (
          <div className="rounded-md border border-red-200 bg-red-50 p-3 dark:border-red-900 dark:bg-red-950/50">
            <div className="flex items-start gap-2">
              <AlertCircle
                className="h-4 w-4 flex-shrink-0 text-red-600 dark:text-red-500"
                aria-hidden="true"
              />
              <p className="text-sm text-red-800 dark:text-red-300">{error}</p>
            </div>
          </div>
        )}

        {/* State Selector */}
        <div className="space-y-2">
          <label
            htmlFor="state-select"
            className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
          >
            Select State
          </label>
          <Select value={selectedStateCode} onValueChange={setSelectedStateCode}>
            <SelectTrigger id="state-select" aria-label="Select a state">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {US_STATES.map((state) => (
                <SelectItem key={state.code} value={state.code}>
                  {state.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <p className="text-xs text-muted-foreground">
            Choose the state where you're applying for Medicaid
          </p>
        </div>

        {/* Action Buttons */}
        <div className="flex gap-3 pt-2">
          <Button
            variant="outline"
            onClick={handleCancel}
            disabled={isLoading}
            className="flex-1"
          >
            Cancel
          </Button>
          <Button
            onClick={handleSelectState}
            disabled={isLoading || selectedStateCode === currentStateCode}
            className="flex-1"
          >
            {isLoading ? "Updating..." : "Update State"}
          </Button>
        </div>
      </div>
    </Card>
  );
}
