import { useState, useEffect } from "react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { QuestionTooltip } from "./QuestionTooltip";
import { QuestionDto, Answer } from "./types";

interface WizardStepProps {
  question: QuestionDto;
  answer: Answer | null;
  onAnswer: (answer: Answer, rawValue: string | string[]) => void;
  onNext: (answer: Answer) => void;
  onBack: () => void;
  canGoBack?: boolean;
  canGoNext?: boolean;
  isSaving?: boolean;
  showNavigation?: boolean;
}

/**
 * Renders a single wizard question with appropriate input control.
 * Supports various field types and includes validation and accessibility.
 *
 * Accessibility features:
 * - aria-describedby links to help text and error messages
 * - aria-invalid/aria-required for validation state
 * - role="alert" for error messages (announced to screen readers)
 * - Enhanced labels and field descriptions
 * - Touch-friendly button sizes
 */
export function WizardStep({
  question,
  answer,
  onAnswer,
  onNext,
  onBack,
  canGoBack: canGoBackProp,
  canGoNext: canGoNextProp,
  isSaving = false,
  showNavigation = true,
}: WizardStepProps) {
  const canGoBack = canGoBackProp !== undefined ? canGoBackProp : false;
  const canGoNext = canGoNextProp !== undefined ? canGoNextProp : true;

  const [value, setValue] = useState<string | string[]>(
    normalizeAnswerValue(question.type, answer?.answerValue),
  );
  const [error, setError] = useState<string | null>(null);

  // Update value when question changes (navigation)
  useEffect(() => {
    setValue(normalizeAnswerValue(question.type, answer?.answerValue));
    setError(null);
  }, [question.key, question.type, answer?.answerValue]);

  // Validate and submit answer
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (!showNavigation) {
      return;
    }

    const rawValue = Array.isArray(value) ? value : value;
    const valueString = Array.isArray(value) ? value.join(",") : value;

    // Required field validation
    if (
      question.required &&
      ((Array.isArray(rawValue) && rawValue.length === 0) ||
        (typeof rawValue === "string" && !rawValue.trim()))
    ) {
      setError("This field is required");
      return;
    }

    // Type-specific validation
    const validationError = validateByType(valueString, question.type);
    if (validationError) {
      setError(validationError);
      return;
    }

    // Map question type to storage field type
    // select/multiselect are UI types that store values as strings
    const storageFieldType = mapToStorageFieldType(question.type);

    // Create answer object
    const answer: Answer = {
      fieldKey: question.key,
      answerValue: valueString,
      fieldType: storageFieldType,
      isPii: isPiiField(question.key), // Heuristic: income, SSN, etc.
    };

    onNext(answer);
  };

  // Handle value change
  const handleChange = (newValue: string | string[]) => {
    setValue(newValue);
    setError(null);

    const answerValue = Array.isArray(newValue) ? newValue.join(",") : newValue;
    const storageFieldType = mapToStorageFieldType(question.type);

    onAnswer(
      {
        fieldKey: question.key,
        answerValue,
        fieldType: storageFieldType,
        isPii: isPiiField(question.key),
      },
      newValue,
    );
  };

  // Render input based on question type
  const renderInput = () => {
    const inputId = `question-${question.key}`;
    const ariaDescribedBy = [error ? `${inputId}-error` : null]
      .filter(Boolean)
      .join(" ");

    const inputAriaLabel = question.label;

    switch (question.type) {
      case "select":
        return (
          <select
            id={inputId}
            value={typeof value === "string" ? value : ""}
            onChange={(e) => handleChange(e.target.value)}
            aria-label={inputAriaLabel}
            aria-describedby={ariaDescribedBy || undefined}
            aria-invalid={error ? "true" : "false"}
            aria-required={question.required}
            className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
          >
            <option value="" disabled>
              Choose an option
            </option>
            {question.options?.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        );

      case "boolean":
        return (
          <div
            role="radiogroup"
            aria-label={inputAriaLabel}
            aria-describedby={ariaDescribedBy || undefined}
            className="flex gap-4"
          >
            <label className="inline-flex items-center gap-2">
              <input
                type="radio"
                name={inputId}
                value="true"
                checked={value === "true"}
                onChange={(e) => handleChange(e.target.value)}
                aria-required={question.required}
              />
              <span>Yes</span>
            </label>
            <label className="inline-flex items-center gap-2">
              <input
                type="radio"
                name={inputId}
                value="false"
                checked={value === "false"}
                onChange={(e) => handleChange(e.target.value)}
                aria-required={question.required}
              />
              <span>No</span>
            </label>
          </div>
        );

      case "date":
        return (
          <Input
            id={inputId}
            type="date"
            value={typeof value === "string" ? value : ""}
            onChange={(e) => handleChange(e.target.value)}
            aria-label={inputAriaLabel}
            aria-describedby={ariaDescribedBy || undefined}
            aria-invalid={error ? "true" : "false"}
            aria-required={question.required}
          />
        );

      case "currency":
        return (
          <div className="relative">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground">
              $
            </span>
            <Input
              id={inputId}
              type="text"
              inputMode="decimal"
              placeholder="0.00"
              value={typeof value === "string" ? value : ""}
              onChange={(e) => {
                // Allow only numbers, decimal, and comma
                const cleaned = e.target.value.replace(/[^0-9.,]/g, "");
                handleChange(cleaned);
              }}
              className="currency-input pl-7"
              aria-label={inputAriaLabel}
              aria-describedby={ariaDescribedBy || undefined}
              aria-invalid={error ? "true" : "false"}
              aria-required={question.required}
            />
          </div>
        );

      case "integer":
        return (
          <Input
            id={inputId}
            type="number"
            inputMode="numeric"
            placeholder="Enter a number"
            value={typeof value === "string" ? value : ""}
            onChange={(e) => handleChange(e.target.value)}
            aria-label={inputAriaLabel}
            aria-describedby={ariaDescribedBy || undefined}
            aria-invalid={error ? "true" : "false"}
            aria-required={question.required}
          />
        );

      case "text":
        return (
          <textarea
            id={inputId}
            rows={4}
            value={typeof value === "string" ? value : ""}
            onChange={(e) => handleChange(e.target.value)}
            className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-base ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 md:text-sm"
            aria-label={inputAriaLabel}
            aria-describedby={ariaDescribedBy || undefined}
            aria-invalid={error ? "true" : "false"}
            aria-required={question.required}
          />
        );

      case "multiselect": {
        const selectedValues = Array.isArray(value) ? value : [];

        return (
          <div className="space-y-2" aria-label={inputAriaLabel}>
            {question.options?.map((option) => (
              <label key={option.value} className="flex items-center gap-2">
                <input
                  type="checkbox"
                  value={option.value}
                  checked={selectedValues.includes(option.value)}
                  onChange={(e) => {
                    const isChecked = e.target.checked;
                    const nextValues = isChecked
                      ? [...selectedValues, option.value]
                      : selectedValues.filter((v) => v !== option.value);
                    handleChange(nextValues);
                  }}
                  aria-describedby={ariaDescribedBy || undefined}
                />
                <span>{option.label}</span>
              </label>
            ))}
          </div>
        );
      }

      default:
        // string type
        return (
          <Input
            id={inputId}
            type="text"
            value={typeof value === "string" ? value : ""}
            onChange={(e) => handleChange(e.target.value)}
            aria-label={inputAriaLabel}
            aria-describedby={ariaDescribedBy || undefined}
            aria-invalid={error ? "true" : "false"}
            aria-required={question.required}
          />
        );
    }
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="space-y-6"
      aria-label="Question response form"
    >
      {/* Question */}
      <fieldset
        className="space-y-3 border-0 p-0"
        aria-labelledby={`question-${question.key}-label`}
      >
        <div className="flex items-start justify-between gap-2">
          <h2
            id={`question-${question.key}-label`}
            className="text-base font-medium"
          >
            {question.label}
            {question.required && (
              <span className="ml-1 text-destructive" aria-label="required">
                *
              </span>
            )}
          </h2>
          {question.helpText && (
            <QuestionTooltip
              helpText={question.helpText}
              questionLabel={question.label}
            />
          )}
        </div>

        {/* Input */}
        {renderInput()}

        {/* Error message */}
        {error && (
          <div
            id={`question-${question.key}-error`}
            role="alert"
            aria-live="assertive"
            className="rounded-md bg-destructive/10 p-3 text-sm text-destructive font-medium"
          >
            <span aria-hidden="true">⚠ </span>
            {error}
          </div>
        )}
      </fieldset>

      {/* Navigation buttons */}
      {showNavigation && (
        <div className="flex gap-4 justify-between">
          <Button
            type="button"
            variant="outline"
            onClick={onBack}
            disabled={!canGoBack || isSaving}
            className="min-h-10 min-w-10"
            aria-label="Go back to previous question"
          >
            Back
          </Button>
          <Button
            type="submit"
            disabled={isSaving || !canGoNext}
            className="min-h-10 min-w-10"
            aria-label={
              isSaving
                ? "Saving your answer"
                : "Save answer and continue to next question"
            }
          >
            {isSaving ? (
              <>
                <span aria-hidden="true">Saving...</span>
                <span className="sr-only">Saving your answer</span>
              </>
            ) : (
              "Next"
            )}
          </Button>
        </div>
      )}
    </form>
  );
}

function normalizeAnswerValue(
  questionType: QuestionDto["type"],
  answerValue?: string | null,
): string | string[] {
  if (questionType === "multiselect") {
    if (!answerValue) return [];
    return answerValue
      .split(",")
      .map((value) => value.trim())
      .filter(Boolean);
  }

  return answerValue ?? "";
}

// Helper: Map question type to storage field type
// UI types like select/multiselect are stored as strings in the backend
function mapToStorageFieldType(
  questionType: QuestionDto["type"],
): Answer["fieldType"] {
  switch (questionType) {
    case "select":
    case "multiselect":
      return "string";
    default:
      return questionType as Answer["fieldType"];
  }
}

// Helper: Validate value by field type
function validateByType(
  value: string,
  type: QuestionDto["type"],
): string | null {
  if (!value) return null; // Empty is OK if not required

  switch (type) {
    case "currency":
      if (!/^\d+(\.\d{1,2})?$/.test(value.replace(/,/g, ""))) {
        return "Please enter a valid dollar amount (e.g., 1000 or 1000.50)";
      }
      break;

    case "integer":
      if (!/^\d+$/.test(value)) {
        return "Please enter a whole number";
      }
      break;

    case "date":
      if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
        return "Please enter a valid date";
      }
      break;
  }

  return null;
}

// Helper: Determine if field contains PII (heuristic)
function isPiiField(fieldKey: string): boolean {
  const piiKeywords = [
    "income",
    "ssn",
    "social",
    "name",
    "address",
    "phone",
    "email",
    "dob",
    "birth",
  ];
  return piiKeywords.some((keyword) =>
    fieldKey.toLowerCase().includes(keyword),
  );
}
