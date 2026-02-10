/**
 * ZIP Code Form Component
 * Feature: State Context Initialization Step
 * User Story 1: ZIP Code Entry with State Auto-Detection
 */

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

/**
 * Zod schema for ZIP code validation
 * Validates 5-digit US ZIP code format
 */
export const zipCodeSchema = z.object({
  zipCode: z
    .string()
    .min(1, "ZIP code is required")
    .regex(/^\d{5}$/, "Please enter a valid 5-digit ZIP code")
    .transform((val) => val.trim()),
});

export type ZipCodeFormData = z.infer<typeof zipCodeSchema>;

interface ZipCodeFormProps {
  onSubmit: (data: ZipCodeFormData) => void;
  isLoading?: boolean;
  error?: string | null;
  defaultValue?: string;
}

/**
 * ZIP Code input form with validation
 * 
 * Features:
 * - 5-digit ZIP code validation
 * - React Hook Form + Zod integration
 * - Accessible form markup (WCAG 2.1 AA)
 * - Error messages with aria-live announcements
 * 
 * @param onSubmit - Callback when form is submitted with valid ZIP
 * @param isLoading - Whether submission is in progress
 * @param error - Server error message to display
 * @param defaultValue - Default ZIP code value
 */
export function ZipCodeForm({
  onSubmit,
  isLoading = false,
  error = null,
  defaultValue = "",
}: ZipCodeFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue,
    watch,
  } = useForm<ZipCodeFormData>({
    resolver: zodResolver(zipCodeSchema),
    defaultValues: {
      zipCode: defaultValue,
    },
  });

  const zipValue = watch("zipCode");

  // Clear server error when user starts typing
  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    // Only allow numeric input (max 5 digits)
    const numericValue = value.replace(/\D/g, "").slice(0, 5);
    setValue("zipCode", numericValue, { shouldValidate: false });
  };

  const hasError = !!errors.zipCode || !!error;
  const errorMessage = errors.zipCode?.message || error;

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      <div className="space-y-2">
        <Label
          htmlFor="zipCode"
          className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
        >
          ZIP Code
        </Label>
        <div className="space-y-2">
          <Input
            id="zipCode"
            type="text"
            inputMode="numeric"
            pattern="[0-9]*"
            maxLength={5}
            placeholder="Enter your 5-digit ZIP code"
            aria-invalid={hasError}
            aria-describedby={hasError ? "zipCode-error" : undefined}
            aria-required="true"
            className={hasError ? "border-red-500 focus-visible:ring-red-500" : ""}
            disabled={isLoading}
            {...register("zipCode")}
            onChange={handleInputChange}
            value={zipValue}
          />
          {hasError && (
            <p
              id="zipCode-error"
              role="alert"
              aria-live="polite"
              className="text-sm font-medium text-red-500"
            >
              {errorMessage}
            </p>
          )}
        </div>
        <p className="text-sm text-muted-foreground">
          We'll use your ZIP code to determine which state's Medicaid program
          applies to you.
        </p>
      </div>

      <Button
        type="submit"
        className="w-full"
        disabled={isLoading || !zipValue || zipValue.length !== 5}
      >
        {isLoading ? "Checking..." : "Continue"}
      </Button>
    </form>
  );
}
