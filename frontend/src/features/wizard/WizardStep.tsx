import { useState, useEffect } from 'react'
import { Label } from '@/components/ui/label'
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
}

/**
 * Renders a single wizard question with appropriate input control.
 * Supports various field types and includes validation and accessibility.
 */
export function WizardStep({ question, onNext, onBack }: WizardStepProps) {
  const { getAnswer, canGoBack } = useWizardStore()
  const existingAnswer = getAnswer(question.key)

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
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Question */}
      <div className="space-y-2">
        <Label htmlFor={`question-${question.key}`} className="text-base font-medium">
          {question.label}
          {question.required && <span className="ml-1 text-destructive">*</span>}
        </Label>

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
          <p
            id={`question-${question.key}-error`}
            role="alert"
            className="text-sm text-destructive"
          >
            {error}
          </p>
        )}
      </div>

      {/* Navigation buttons */}
      <div className="flex justify-between gap-4">
        <Button
          type="button"
          variant="outline"
          onClick={onBack}
          disabled={!canGoBack()}
        >
          Back
        </Button>
        <Button type="submit">
          Next
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
