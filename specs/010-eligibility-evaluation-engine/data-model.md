# Data Model: Eligibility Evaluation Engine

**Date**: 2026-02-11
**Feature**: 010-eligibility-evaluation-engine

## Entities

### RuleSetVersion

Represents a versioned collection of eligibility rules for a state.

**Fields**
- `ruleSetVersionId` (UUID, PK)
- `stateCode` (string, 2-letter)
- `version` (string, e.g., v1.0)
- `effectiveDate` (date)
- `endDate` (date, nullable)
- `status` (enum: active, retired)
- `createdAt` (timestamp)

**Validation**
- `stateCode` must match `^[A-Z]{2}$`.
- `effectiveDate` must be <= `endDate` when `endDate` is provided.
- Only one active rule set per state for a given date range.

### EligibilityRule

A declarative rule definition tied to a program and rule set version.

**Fields**
- `eligibilityRuleId` (UUID, PK)
- `ruleSetVersionId` (UUID, FK)
- `programCode` (string)
- `ruleLogic` (JSON)
- `priority` (int)
- `createdAt` (timestamp)

**Validation**
- `ruleLogic` must be valid JSONLogic.
- `priority` must be >= 0.

### ProgramDefinition

Represents a Medicaid program for matching.

**Fields**
- `programCode` (string, PK)
- `stateCode` (string, 2-letter)
- `programName` (string)
- `description` (string)
- `category` (enum: magi, nonMagi, pregnancy, ssiLinked, other)
- `isActive` (boolean)

**Validation**
- `programCode` unique within a state.

### FederalPovertyLevel

Yearly poverty level reference values.

**Fields**
- `fplId` (UUID, PK)
- `year` (int)
- `householdSize` (int)
- `annualAmount` (decimal)
- `stateCode` (string, nullable; null for default)

**Validation**
- `householdSize` >= 1.
- `annualAmount` >= 0.

## DTOs (Not Persisted)

### EligibilityRequest

**Fields**
- `stateCode` (string)
- `effectiveDate` (date)
- `answers` (object)

**Validation**
- `stateCode` must be supported.
- `effectiveDate` must be a valid date.
- `answers` must conform to the wizard answer schema.

### EligibilityResult

**Fields**
- `status` (enum: Likely, Possibly, Unlikely)
- `matchedPrograms` (array of ProgramMatch)
- `confidenceScore` (int, 0-100)
- `explanation` (string)
- `explanationItems` (array of ExplanationItem)
- `ruleVersionUsed` (string)
- `evaluatedAt` (timestamp)

**Validation**
- `confidenceScore` between 0 and 100.

### ProgramMatch

**Fields**
- `programCode` (string)
- `programName` (string)
- `confidenceScore` (int, 0-100)
- `explanation` (string)

### ExplanationItem

**Fields**
- `ruleId` (string)
- `message` (string)
- `status` (enum: met, unmet, missing)

## Relationships

- `RuleSetVersion` 1-to-many `EligibilityRule`.
- `ProgramDefinition` 1-to-many `EligibilityRule` (by `programCode`).
- `FederalPovertyLevel` referenced by evaluation logic based on `year` and `householdSize`.

## State Transitions

- `RuleSetVersion.status`: active -> retired (when superseded).
