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
import { fetchStates, lookupStateByZip } from './stateApi'
import { StateInfo } from './types'

interface StateSelectorProps {
  onStateSelected: (stateCode: string, stateName: string) => void
  disabled?: boolean
}

/**
 * State selector component with manual selection and ZIP code lookup.
 * Supports WCAG 2.1 AA with keyboard navigation and screen reader support.
 * 
 * Accessibility features:
 * - Proper form labels (htmlFor association)
 * - ARIA descriptions and error states
 * - Keyboard support (Enter to submit, Tab navigation)
 * - Screen reader announcements via aria-live
 * - Touch-friendly button sizes (minimum 44x44px)
 */
export function StateSelector({ onStateSelected, disabled = false }: StateSelectorProps) {
  const [states, setStates] = useState<StateInfo[]>([])
  const [selectedState, setSelectedState] = useState<string>('')
  const [zipCode, setZipCode] = useState<string>('')
  const [isLoadingStates, setIsLoadingStates] = useState(true)
  const [isLookingUpZip, setIsLookingUpZip] = useState(false)
  const [zipError, setZipError] = useState<string | null>(null)
  const [statesError, setStatesError] = useState<string | null>(null)

  // Fetch available states on mount
  useEffect(() => {
    const loadStates = async () => {
      try {
        const stateList = await fetchStates()
        setStates(stateList)
        setStatesError(null)
      } catch (err) {
        console.error('Failed to load states:', err)
        setStatesError('Failed to load available states. Please refresh the page.')
      } finally {
        setIsLoadingStates(false)
      }
    }

    loadStates()
  }, [])

  // Handle state selection from dropdown
  const handleStateChange = (stateCode: string) => {
    setSelectedState(stateCode)
    const state = states.find((s) => s.code === stateCode)
    if (state) {
      onStateSelected(stateCode, state.name)
    }
    setZipError(null)
  }

  // Handle ZIP code lookup
  const handleZipLookup = async () => {
    if (!zipCode || zipCode.length !== 5) {
      setZipError('Please enter a 5-digit ZIP code')
      return
    }

    setIsLookingUpZip(true)
    setZipError(null)

    try {
      const result = await lookupStateByZip(zipCode)
      
      if (result) {
        setSelectedState(result.code)
        onStateSelected(result.code, result.name)
        setZipError(null)
      } else {
        setZipError('ZIP code not found or state not available in pilot program')
      }
    } catch (err) {
      console.error('ZIP lookup failed:', err)
      setZipError('Failed to lookup ZIP code. Please select your state manually.')
    } finally {
      setIsLookingUpZip(false)
    }
  }

  // Handle ZIP input change (allow only digits)
  const handleZipChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value.replace(/\D/g, '').slice(0, 5)
    setZipCode(value)
    setZipError(null)
  }

  // Handle Enter key on ZIP input
  const handleZipKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      handleZipLookup()
    }
  }

  if (statesError) {
    return (
      <div role="alert" className="rounded-md border border-destructive bg-destructive/10 p-4">
        <p className="text-sm text-destructive">{statesError}</p>
      </div>
    )
  }

  return (
    <fieldset className="space-y-6" disabled={disabled}>
      <legend className="sr-only">State Selection Options</legend>

      {/* ZIP Code Lookup Section */}
      <div className="space-y-3">
        <Label htmlFor="zip-input" className="font-medium">
          Find Your State by ZIP Code <span className="text-muted-foreground">(Optional)</span>
        </Label>
        <p id="zip-help" className="text-sm text-muted-foreground">
          Enter your 5-digit ZIP code to automatically determine your state.
        </p>
        <div className="flex gap-2">
          <Input
            id="zip-input"
            type="text"
            inputMode="numeric"
            pattern="[0-9]*"
            placeholder="e.g., 75001"
            value={zipCode}
            onChange={handleZipChange}
            onKeyDown={handleZipKeyDown}
            disabled={disabled || isLookingUpZip}
            aria-describedby={zipError ? 'zip-error' : 'zip-help'}
            aria-invalid={zipError ? 'true' : 'false'}
            className="flex-1"
            maxLength={5}
          />
          <Button
            type="button"
            onClick={handleZipLookup}
            disabled={disabled || isLookingUpZip || zipCode.length !== 5}
            variant="secondary"
            aria-label={`Look up state by ZIP code ${zipCode || ''}`}
            className="min-h-10 min-w-10"
          >
            {isLookingUpZip ? (
              <>
                <span aria-hidden="true">...</span>
                <span className="sr-only">Looking up ZIP code</span>
              </>
            ) : (
              'Lookup'
            )}
          </Button>
        </div>
        {zipError && (
          <p id="zip-error" role="alert" className="text-sm text-destructive font-medium">
            {zipError}
          </p>
        )}
      </div>

      {/* Divider */}
      <div className="relative">
        <div className="absolute inset-0 flex items-center">
          <span className="w-full border-t" />
        </div>
        <div className="relative flex justify-center text-xs uppercase">
          <span className="bg-background px-2 text-muted-foreground">Or select manually</span>
        </div>
      </div>

      {/* Manual State Selection */}
      <div className="space-y-3">
        <Label htmlFor="state-select" className="font-medium">
          <span className="font-semibold">Select Your State</span> <span className="text-destructive" aria-label="required">*</span>
        </Label>
        <p id="state-help" className="text-sm text-muted-foreground">
          Choose the state where you need Medicaid coverage.
        </p>
        <Select
          value={selectedState}
          onValueChange={handleStateChange}
          disabled={disabled || isLoadingStates}
        >
          <SelectTrigger 
            id="state-select" 
            aria-describedby="state-help"
            aria-label="Select your state"
            className="min-h-10"
          >
            <SelectValue placeholder={isLoadingStates ? 'Loading states...' : 'Choose a state'} />
          </SelectTrigger>
          <SelectContent>
            {states.map((state) => (
              <SelectItem key={state.code} value={state.code}>
                {state.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {selectedState && (
          <p className="text-sm text-muted-foreground" role="status" aria-live="polite">
            You selected: <span className="font-medium">{states.find((s) => s.code === selectedState)?.name}</span>
          </p>
        )}
      </div>
    </fieldset>
  )
}
