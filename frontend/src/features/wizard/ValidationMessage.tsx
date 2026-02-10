import { ReactNode } from "react";
import { cn } from "@/lib/utils";

interface ValidationMessageProps {
  id?: string;
  children: ReactNode;
  type?: "error" | "warning" | "success" | "info";
  className?: string;
  /** If true, announces immediately to screen readers (use for critical errors) */
  assertive?: boolean;
}

/**
 * Validation message component for form field feedback.
 *
 * Types:
 * - error: Validation errors that prevent submission (red)
 * - warning: Non-blocking issues or cautions (amber)
 * - success: Confirmation of valid input (green)
 * - info: Neutral informational feedback (blue)
 *
 * Accessibility:
 * - role="alert" for screen reader announcements
 * - aria-live="polite|assertive" for dynamic updates
 * - Associated with form fields via aria-describedby
 * - Clear, actionable error messages
 *
 * Best practices:
 * - Be specific: "Please enter a 5-digit ZIP code" vs "Invalid input"
 * - Be helpful: Explain how to fix the error
 * - Be timely: Show errors inline as user types or on blur
 * - Use plain language: Avoid technical jargon
 *
 * Example:
 * ```tsx
 * <Input
 *   id="email"
 *   aria-describedby="email-error"
 *   aria-invalid={hasError}
 * />
 * <ValidationMessage id="email-error" type="error">
 *   Please enter a valid email address (e.g., name@example.com)
 * </ValidationMessage>
 * ```
 */
export function ValidationMessage({
  id,
  children,
  type = "error",
  className,
  assertive = false,
}: ValidationMessageProps) {
  const typeStyles = {
    error: "text-destructive bg-destructive/10 border-destructive",
    warning:
      "text-amber-700 bg-amber-50 border-amber-300 dark:text-amber-400 dark:bg-amber-950 dark:border-amber-800",
    success:
      "text-green-700 bg-green-50 border-green-300 dark:text-green-400 dark:bg-green-950 dark:border-green-800",
    info: "text-blue-700 bg-blue-50 border-blue-300 dark:text-blue-400 dark:bg-blue-950 dark:border-blue-800",
  };

  const iconMap = {
    error: "⚠",
    warning: "⚠",
    success: "✓",
    info: "ℹ",
  };

  return (
    <div
      id={id}
      role="alert"
      aria-live={assertive ? "assertive" : "polite"}
      className={cn(
        "flex items-start gap-2 rounded-md border px-3 py-2 text-sm font-medium",
        typeStyles[type],
        className,
      )}
    >
      <span aria-hidden="true" className="flex-shrink-0 mt-0.5">
        {iconMap[type]}
      </span>
      <span className="flex-1">{children}</span>
    </div>
  );
}

interface FieldErrorProps {
  id?: string;
  children: ReactNode;
  className?: string;
}

/**
 * Simplified field error component for common use cases.
 * Automatically styled as an error with proper accessibility.
 *
 * Example:
 * ```tsx
 * {error && (
 *   <FieldError id="field-error">
 *     {error}
 *   </FieldError>
 * )}
 * ```
 */
export function FieldError({ id, children, className }: FieldErrorProps) {
  return (
    <ValidationMessage id={id} type="error" className={className}>
      {children}
    </ValidationMessage>
  );
}

interface FormErrorSummaryProps {
  errors: Array<{ field: string; message: string }>;
  className?: string;
  onErrorClick?: (field: string) => void;
}

/**
 * Form error summary component for displaying multiple validation errors.
 * Typically shown at the top of a form to provide an overview of issues.
 *
 * Accessibility:
 * - Announced immediately with aria-live="assertive"
 * - Links to individual error fields for easy navigation
 * - Clear heading for screen reader users
 *
 * Example:
 * ```tsx
 * {errors.length > 0 && (
 *   <FormErrorSummary
 *     errors={errors}
 *     onErrorClick={(field) => focusField(field)}
 *   />
 * )}
 * ```
 */
export function FormErrorSummary({
  errors,
  className,
  onErrorClick,
}: FormErrorSummaryProps) {
  if (errors.length === 0) return null;

  return (
    <div
      role="alert"
      aria-live="assertive"
      aria-atomic="true"
      className={cn(
        "rounded-md border border-destructive bg-destructive/10 p-4",
        className,
      )}
    >
      <div className="flex items-start gap-3">
        <span aria-hidden="true" className="text-destructive text-xl">
          ⚠
        </span>
        <div className="flex-1">
          <h2 className="text-base font-semibold text-destructive mb-2">
            There {errors.length === 1 ? "is" : "are"} {errors.length} error
            {errors.length === 1 ? "" : "s"} with your submission
          </h2>
          <ul className="space-y-2 text-sm">
            {errors.map((error, index) => (
              <li key={index}>
                {onErrorClick ? (
                  <button
                    type="button"
                    onClick={() => onErrorClick(error.field)}
                    className="text-destructive underline hover:no-underline focus:outline-2 focus:outline-ring"
                  >
                    {error.field}: {error.message}
                  </button>
                ) : (
                  <span className="text-destructive">
                    {error.field}: {error.message}
                  </span>
                )}
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}

interface InlineValidationProps {
  children: ReactNode;
  isValid?: boolean;
  errorMessage?: string;
  successMessage?: string;
  showValidation?: boolean;
  className?: string;
}

/**
 * Inline validation feedback component.
 * Shows real-time validation as user types or on blur.
 *
 * Features:
 * - Auto-switches between error and success states
 * - Only shows when showValidation is true (avoid premature errors)
 * - Smooth transitions between states
 *
 * Example:
 * ```tsx
 * <InlineValidation
 *   isValid={zipCode.length === 5}
 *   errorMessage="ZIP code must be 5 digits"
 *   successMessage="Valid ZIP code"
 *   showValidation={zipCode.length > 0}
 * >
 *   <Input value={zipCode} onChange={...} />
 * </InlineValidation>
 * ```
 */
export function InlineValidation({
  children,
  isValid,
  errorMessage,
  successMessage,
  showValidation = false,
  className,
}: InlineValidationProps) {
  return (
    <div className={cn("space-y-2", className)}>
      {children}
      {showValidation && (
        <>
          {!isValid && errorMessage && (
            <ValidationMessage type="error">{errorMessage}</ValidationMessage>
          )}
          {isValid && successMessage && (
            <ValidationMessage type="success">
              {successMessage}
            </ValidationMessage>
          )}
        </>
      )}
    </div>
  );
}
