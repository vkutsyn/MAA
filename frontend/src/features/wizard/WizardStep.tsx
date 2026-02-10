import { useState, useEffect } from 'react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useWizardStore } from './store'
import { QuestionDto, Answer } from './types'

interface WizardStepProps {
  question: QuestionDto
  onNext: (answer: Answer) => void
  onBack: () => void
  canGoBack?: boolean
  isSaving?: boolean
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
  onNext, 
  onBack, 
  canGoBack: canGoBackProp, 
  isSaving = false 
}: WizardStepProps) {
  const { getAnswer } = useWizardStore()
  const existingAnswer = getAnswer(question.key)
  const canGoBack = canGoBackProp !== undefined ? canGoBackProp : false

  const [value, setValue] = useState<string>(existingAnswer?.answerValue || '')
  const [error, setError] = useState<string | null>(null)

  // Update value when question changes (navigation)
  useEffect(() => {
    const answer = getAnswer(question.key)
    setValue(answer?.answerValue || '')
    setError(null)
  }, [question.key, getAnswer])

  // Validate and submit answer
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()

    // Required field validation
    if (question.required && !value.trim()) {
      setError('This field is required')
      return
    }

    // Type-specific validation
    const validationError = validateByType(value, question.type)
    if (validationError) {
      setError(validationError)
      return
    }

    // Create answer object
    const answer: Answer = {
      fieldKey: question.key,
      answerValue: value,
      fieldType: question.type as Answer['fieldType'],
      isPii: isPiiField(question.key), // Heuristic: income, SSN, etc.
    }

    onNext(answer)
  }

  // Handle value change
  const handleChange = (newValue: string) => {
    setValue(newValue)
    setError(null)
  }

  // Render input based on question type
  const renderInput = () => {
    const inputId = `question-${question.key}`
    const ariaDescribedBy = [
      question.helpText ? `${inputId}-help` : null,
      error ? `${inputId}-error` : null,
    ]
      .filter(Boolean)
      .join(' ')

    switch (question.type) {
      case 'select':
        return (
          <Select value={value} onValueChange={handleChange}>
            <SelectTrigger
              id={inputId}
              aria-describedby={ariaDescribedBy || undefined}
              aria-invalid={error ? 'true' : 'false'}
              aria-required={question.required}
            >
              <SelectValue placeholder="Choose an option" />
            </SelectTrigger>
            <SelectContent>
              {question.options?.map((option) => (
                <SelectItem key={option.value} value={option.value}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )

      case 'boolean':
        return (
          <Select value={value} onValueChange={handleChange}>
            <SelectTrigger
              id={inputId}
              aria-describedby={ariaDescribedBy || undefined}
              aria-invalid={error ? 'true' : 'false'}
              aria-required={question.required}
            >
              <SelectValue placeholder="Select yes or no" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="true">Yes</SelectItem>
              <SelectItem value="false">No</SelectItem>
            </SelectContent>
          </Select>
        )

      case 'date':
        return (
          <Input
            id={inputId}
            type="date"
            value={value}
            onChange={(e) => handleChange(e.target.value)}
            aria-describedby={ariaDescribedBy || undefined}
            aria-invalid={error ? 'true' : 'false'}
            aria-required={question.required}
          />
        )

      case 'currency':
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
              value={value}
              onChange={(e) => {
                // Allow only numbers, decimal, and comma
                const cleaned = e.target.value.replace(/[^0-9.,]/g, '')
                handleChange(cleaned)
              }}
              className="pl-7"
              aria-describedby={ariaDescribedBy || undefined}
              aria-invalid={error ? 'true' : 'false'}
              aria-required={question.required}
            />
          </div>
        )

      case 'integer':
        return (
          <Input
            id={inputId}
            type="text"
            inputMode="numeric"
            pattern="[0-9]*"
            placeholder="Enter a number"
            value={value}
            onChange={(e) => {
              // Allow only digits
              const cleaned = e.target.value.replace(/\D/g, '')
              handleChange(cleaned)
            }}
            aria-describedby={ariaDescribedBy || undefined}
            aria-invalid={error ? 'true' : 'false'}
            aria-required={question.required}
          />
        )

      case 'text':
        return (
          <textarea
            id={inputId}
            rows={4}
            value={value}
            onChange={(e) => handleChange(e.target.value)}
            className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-base ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 md:text-sm"
            aria-describedby={ariaDescribedBy || undefined}
            aria-invalid={error ? 'true' : 'false'}
            aria-required={question.required}
          />
        )

      default:
        // string type
        return (
          <Input
            id={inputId}
            type="text"
            value={value}
            onChange={(e) => handleChange(e.target.value)}
            aria-describedby={ariaDescribedBy || undefined}
            aria-invalid={error ? 'true' : 'false'}
            aria-required={question.required}
          />
        )
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6" aria-label="Question response form">
      {/* Question */}
      <fieldset className="space-y-3 border-0 p-0">
        <legend className="text-base font-medium">
          {question.label}
          {question.required && (
            <span className="ml-1 text-destructive" aria-label="required">
              *
            </span>
          )}
        </legend>

        {/* Help text */}
        {question.helpText && (
          <p
            id={`question-${question.key}-help`}
            className="text-sm text-muted-foreground"
          >
            {question.helpText}
          </p>
        )}

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
          disabled={isSaving}
          className="min-h-10 min-w-10"
          aria-label={isSaving ? 'Saving your answer' : 'Save answer and continue to next question'}
        >
          {isSaving ? (
            <>
              <span aria-hidden="true">Saving...</span>
              <span className="sr-only">Saving your answer</span>
            </>
          ) : (
            'Next'
          )}
        </Button>
      </div>
    </form>
  )
}

// Helper: Validate value by field type
function validateByType(value: string, type: QuestionDto['type']): string | null {
  if (!value) return null // Empty is OK if not required

  switch (type) {
    case 'currency':
      if (!/^\d+(\.\d{1,2})?$/.test(value.replace(/,/g, ''))) {
        return 'Please enter a valid dollar amount (e.g., 1000 or 1000.50)'
      }
      break

    case 'integer':
      if (!/^\d+$/.test(value)) {
        return 'Please enter a whole number'
      }
      break

    case 'date':
      if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
        return 'Please enter a valid date'
      }
      break
  }

  return null
}

// Helper: Determine if field contains PII (heuristic)
function isPiiField(fieldKey: string): boolean {
  const piiKeywords = ['income', 'ssn', 'social', 'name', 'address', 'phone', 'email', 'dob', 'birth']
  return piiKeywords.some((keyword) => fieldKey.toLowerCase().includes(keyword))
}
