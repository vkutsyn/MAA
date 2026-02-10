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
    <div className="space-y-6">
      {/* ZIP Code Lookup Section */}
      <div className="space-y-2">
        <Label htmlFor="zip-input">
          Find Your State by ZIP Code (Optional)
        </Label>
        <div className="flex gap-2">
          <Input
            id="zip-input"
            type="text"
            inputMode="numeric"
            pattern="[0-9]*"
            placeholder="Enter 5-digit ZIP"
            value={zipCode}
            onChange={handleZipChange}
            onKeyDown={handleZipKeyDown}
            disabled={disabled || isLookingUpZip}
            aria-describedby={zipError ? 'zip-error' : undefined}
            aria-invalid={zipError ? 'true' : 'false'}
            className="flex-1"
            maxLength={5}
          />
          <Button
            type="button"
            onClick={handleZipLookup}
            disabled={disabled || isLookingUpZip || zipCode.length !== 5}
            variant="secondary"
            aria-label="Look up state by ZIP code"
          >
            {isLookingUpZip ? 'Looking up...' : 'Lookup'}
          </Button>
        </div>
        {zipError && (
          <p id="zip-error" role="alert" className="text-sm text-destructive">
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
      <div className="space-y-2">
        <Label htmlFor="state-select">Select Your State</Label>
        <Select
          value={selectedState}
          onValueChange={handleStateChange}
          disabled={disabled || isLoadingStates}
        >
          <SelectTrigger id="state-select" aria-label="Select your state">
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
          <p className="text-sm text-muted-foreground" role="status">
            Selected: {states.find((s) => s.code === selectedState)?.name}
          </p>
        )}
      </div>
    </div>
  )
}
