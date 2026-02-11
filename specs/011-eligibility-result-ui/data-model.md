# Phase 1 Data Model - Eligibility Result UI

## Entities

### EligibilityResultView

Represents the eligibility outcome displayed in the UI.

**Fields**:

- `evaluationDate` (ISO 8601 string, required)
- `overallStatus` (string, required; values: "Likely Eligible", "Possibly Eligible", "Unlikely Eligible")
- `confidenceScore` (integer, required; 0-100)
- `confidenceLabel` (string, required; derived from score)
- `confidenceDescription` (string, required; plain-language description)
- `explanation` (string, required; plain-language summary)
- `explanationBullets` (string[], required; top reasons, may be empty)
- `matchedPrograms` (ProgramMatchView[], required; may be empty)
- `stateCode` (string, required; two-letter code)
- `ruleVersionUsed` (number | null)
- `evaluationDurationMs` (number, optional)

**Validation rules**:

- `overallStatus` must be one of the allowed status values.
- `confidenceScore` must be between 0 and 100.
- `explanation` must be non-empty and under 2000 characters.
- `explanationBullets` should be limited to a UI-friendly count (e.g., top 5) with remaining items omitted or collapsed.

### ProgramMatchView

Represents a program match displayed in the results list.

**Fields**:

- `programId` (string UUID, required)
- `programName` (string, required)
- `eligibilityStatus` (string | null; same allowed values as overall status)
- `confidenceScore` (integer, required; 0-100)
- `explanation` (string | null)
- `matchingFactors` (string[])
- `disqualifyingFactors` (string[])

**Validation rules**:

- `programName` must be non-empty.
- `confidenceScore` must be between 0 and 100.

### ConfidenceLabel

Derived display metadata for the confidence indicator.

**Fields**:

- `label` (string)
- `range` (string; e.g., "60-79")
- `description` (string; plain-language meaning)

**Label thresholds**:

- 0-19: "Uncertain"
- 20-39: "Low confidence"
- 40-59: "Some confidence"
- 60-79: "High confidence"
- 80-100: "Very confident"

## Relationships

- `EligibilityResultView` has many `ProgramMatchView` entries.
- `EligibilityResultView` derives `ConfidenceLabel` from `confidenceScore`.

## State Transitions

- `EligibilityResultView` transitions from `loading` -> `ready` or `error` based on API response.
- Missing or partial data maps to a `fallback` state with user guidance per spec.
