# Data Model: Eligibility Wizard UI

## Entities

### WizardSession

- Fields:
  - sessionId (uuid, required)
  - stateCode (string, required, 2 chars)
  - currentStep (integer, required, >= 0)
  - totalSteps (integer, required, >= 1)
  - completionPercent (integer, 0-100)
  - lastSavedAt (datetime, required)
- Relationships:
  - 1:many with Answer (sessionId)
- Validation:
  - sessionId must be a valid UUID
  - completionPercent derived from currentStep/totalSteps

### StateSelection

- Fields:
  - stateCode (string, required, 2 chars)
  - stateName (string, required)
  - source (string, required: auto, manual)
  - zip (string, optional, 5 digits)
- Validation:
  - stateCode must be a valid USPS code
  - zip is 5 digits if present

### Question

- Fields:
  - key (string, required, max 200)
  - label (string, required)
  - type (string, required: currency, integer, string, boolean, date, text, select, multiselect)
  - required (bool, required)
  - helpText (string, optional)
  - options (array, optional)
  - conditions (array, optional)
- Validation:
  - key must match backend taxonomy keys
  - options required for select/multiselect

### Answer

- Fields (matches API contract):
  - id (uuid)
  - sessionId (uuid, required)
  - fieldKey (string, required, max 200)
  - fieldType (string, required)
  - answerValue (string, required, max 10000)
  - isPii (bool, required)
  - keyVersion (integer)
  - validationErrors (string, optional)
  - createdAt (datetime)
  - updatedAt (datetime)
- Validation:
  - fieldType must be one of currency, integer, string, boolean, date, text
  - answerValue validated by fieldType rules

### ProgressState

- Fields:
  - currentIndex (integer, required)
  - totalSteps (integer, required)
  - canGoBack (bool, required)
  - canGoNext (bool, required)
- Validation:
  - currentIndex in range 0..totalSteps-1

## State Transitions

- WizardSession:
  - draft -> in_progress -> completed
  - in_progress -> draft (user edits previous steps)
  - in_progress -> expired (session timeout)

## Relationships Summary

- WizardSession 1:many Answer
- StateSelection 1:1 WizardSession
- Question 1:many Answer (by fieldKey)
