# Data Model: Dynamic Eligibility Question UI

**Feature**: 009-dynamic-question-ui  
**Date**: February 10, 2026  
**Purpose**: Document data structures for conditional questions, tooltips, and visibility state

## Overview

This data model documents the TypeScript interfaces and state structures for dynamic question rendering. Since this is a frontend-only feature, all entities are client-side types (no database changes).

## Core Entities

### 1. QuestionDto (Existing, Reference Only)

Represents a single eligibility question from the backend API.

**Properties**:

- `key: string` - Unique identifier for the question (e.g., "household_income")
- `label: string` - Display text for the question
- `type: QuestionType` - Input control type (currency, integer, string, boolean, date, text, select, multiselect)
- `required: boolean` - Whether answer is mandatory
- `helpText?: string` - Optional explanatory text for "Why we ask this" tooltip
- `options?: QuestionOption[]` - For select/multiselect types
- `conditions?: QuestionCondition[]` - Rules determining question visibility

**Relationships**:

- Contains 0..n `QuestionOption` (for select/multiselect)
- Contains 0..n `QuestionCondition` (conditional visibility rules)

**Validation Rules**:

- `key` must be unique within question set
- `type` must match one of enum values
- `options` required if type is select/multiselect
- `conditions` optional; empty array = always visible

**State Transitions**:

- Visible → Hidden: When conditions evaluated to false
- Hidden → Visible: When conditions evaluated to true
- Always Visible: When conditions is null/undefined/empty

---

### 2. QuestionCondition (Existing, Reference Only)

Defines a single rule for conditional question visibility.

**Properties**:

- `fieldKey: string` - Key of the trigger question (question being checked)
- `operator: ConditionOperator` - Comparison operator (equals, not_equals, gt, gte, lt, lte, includes)
- `value: string` - Expected value for condition to pass

**Relationships**:

- Belongs to 1 `QuestionDto`
- References 1 trigger question by `fieldKey`

**Validation Rules**:

- `fieldKey` must reference a valid question key in the same question set
- `operator` must be one of enum values
- `value` must be compatible with trigger question's type
- Numeric operators (gt, gte, lt, lte) only valid for currency/integer types

**Evaluation Logic**:

```typescript
function evaluateCondition(
  condition: QuestionCondition,
  answers: AnswerMap,
): boolean {
  const answer = answers.get(condition.fieldKey);
  if (!answer) return false; // No answer = condition fails

  switch (condition.operator) {
    case "equals":
      return answer === condition.value;
    case "not_equals":
      return answer !== condition.value;
    case "gt":
      return Number(answer) > Number(condition.value);
    case "gte":
      return Number(answer) >= Number(condition.value);
    case "lt":
      return Number(answer) < Number(condition.value);
    case "lte":
      return Number(answer) <= Number(condition.value);
    case "includes":
      return answer.includes(condition.value);
    default:
      return false;
  }
}
```

---

### 3. AnswerMap (New Frontend Type)

Frontend-only structure for efficient answer lookups during condition evaluation.

**Structure**:

```typescript
type AnswerMap = Map<string, string>;
// Key: question fieldKey, Value: answer value as string
```

**Purpose**:

- O(1) lookup for condition evaluation
- Consistent interface for all question types (all answers normalized to strings)

**Population**:

```typescript
const answerMap = new Map<string, string>();
sessionAnswers.forEach((answer) => {
  answerMap.set(answer.fieldKey, answer.answerValue);
});
```

**Invariants**:

- Keys match QuestionDto.key values
- Values are string representation of answers (even for numbers/dates)
- Updated on every answer submission

---

### 4. VisibilityState (New Frontend Type)

Tracks which questions are currently visible based on conditional evaluation.

**Structure**:

```typescript
interface VisibilityState {
  visibleQuestionKeys: Set<string>;
  evaluatedAt: number; // timestamp
}
```

**Properties**:

- `visibleQuestionKeys`: Set of question keys that should be displayed
- `evaluatedAt`: Timestamp of last evaluation (for debugging/logging)

**Computation**:

```typescript
function computeVisibility(
  questions: QuestionDto[],
  answers: AnswerMap,
): VisibilityState {
  const visible = new Set<string>();

  questions.forEach((question) => {
    const isVisible =
      !question.conditions ||
      question.conditions.every((c) => evaluateCondition(c, answers));

    if (isVisible) {
      visible.add(question.key);
    }
  });

  return {
    visibleQuestionKeys: visible,
    evaluatedAt: Date.now(),
  };
}
```

**Usage**:

- Recomputed via useMemo when answers change
- Used to filter questions before rendering

---

### 5. TooltipState (New Frontend Type)

Tracks open/closed state for question tooltips (managed per-question).

**Structure**:

```typescript
interface TooltipState {
  openTooltipKey: string | null;
}
```

**Properties**:

- `openTooltipKey`: Key of question with visible tooltip, or null if all closed

**State Transitions**:

- Closed → Open: User clicks/focuses help icon → set to question.key
- Open → Closed: User clicks outside, presses Escape, or clicks another tooltip → set to null
- Open (Q1) → Open (Q2): User clicks Q2 tooltip while Q1 open → set to Q2.key

**Rationale**:

- Single-tooltip-at-a-time prevents UI clutter
- Radix UI handles accessibility; this state just tracks which is open

---

## Frontend State Management

### Zustand Store Extension

The existing wizard store (`store.ts`) will be extended to support conditional questions:

```typescript
interface WizardStore {
  // Existing state
  session: SessionDto | null;
  questions: QuestionDto[];
  currentStep: number;
  selectedState: StateInfo | null;

  // New state for conditional questions
  answerMap: AnswerMap; // NEW: Fast lookup for condition evaluation
  visibilityState: VisibilityState; // NEW: Cached visibility computation

  // New actions
  updateAnswerMap: (fieldKey: string, value: string) => void;
  recomputeVisibility: () => void;
}
```

**Invariants**:

- `answerMap` always synced with session answers
- `visibilityState` recomputed after every answer change
- `currentStep` only references visible questions

---

## Component Props Interfaces

### ConditionalQuestionContainer Props

```typescript
interface ConditionalQuestionContainerProps {
  question: QuestionDto;
  answers: AnswerMap;
  children: React.ReactNode;
  onVisibilityChange?: (visible: boolean) => void;
}
```

**Behavior**:

- Evaluates `question.conditions` against `answers`
- Renders `children` only if all conditions pass
- Calls `onVisibilityChange` when visibility changes (for focus management)

---

### QuestionTooltip Props

```typescript
interface QuestionTooltipProps {
  questionKey: string;
  helpText: string;
  triggerLabel?: string; // Defaults to "Why we ask this"
}
```

**Behavior**:

- Renders Radix UI Tooltip with help icon trigger
- Displays `helpText` in popover
- Keyboard-accessible (Enter/Space to open, Escape to close)

---

### QuestionRenderer Props

```typescript
interface QuestionRendererProps {
  question: QuestionDto;
  value?: string;
  onChange: (value: string) => void;
  error?: string;
  showTooltip?: boolean; // Default true
}
```

**Behavior**:

- Renders appropriate input control based on `question.type`
- Shows QuestionTooltip if `question.helpText` exists and `showTooltip` is true
- Emits `onChange` for answer updates

---

## Data Flow Diagram

```
┌─────────────────┐
│  Question API   │
│ (Backend)       │
└────────┬────────┘
         │ GET /api/questions/{state}
         ▼
┌─────────────────────┐
│  QuestionDto[]      │
│  (with conditions,  │
│   helpText)         │
└────────┬────────────┘
         │
         ▼
┌─────────────────────────────────┐
│  WizardPage Component           │
│  - questions: QuestionDto[]     │
│  - answerMap: AnswerMap         │
│  - visibilityState: Visibility  │
└────────┬────────────────────────┘
         │
         ├──► computeVisibility(questions, answerMap)
         │    → VisibilityState
         │
         ├──► Filter questions by visibilityState
         │    → visibleQuestions[]
         │
         └──► Map visibleQuestions → Components
              │
              ▼
         ┌────────────────────────────┐
         │ ConditionalQuestionContainer│
         │  - Checks conditions       │
         │  - Renders if visible      │
         └────────┬───────────────────┘
                  │
                  ▼
         ┌────────────────────┐
         │  QuestionRenderer  │
         │  - Renders input   │
         │  - Shows tooltip   │
         └────────┬───────────┘
                  │
                  ▼
         ┌────────────────────┐
         │  QuestionTooltip   │
         │  - Help icon       │
         │  - Popover content │
         └────────────────────┘
```

---

## Backend Contract (Reference Only)

The backend `/api/questions/{stateCode}` endpoint already returns:

```json
{
  "state": "TX",
  "version": "1.0.0",
  "questions": [
    {
      "key": "household_income",
      "label": "What is your household's monthly income?",
      "type": "currency",
      "required": true,
      "helpText": "We ask for your income to determine if you meet the financial eligibility criteria for Medicaid programs in your state.",
      "conditions": null
    },
    {
      "key": "is_pregnant",
      "label": "Are you currently pregnant?",
      "type": "boolean",
      "required": true,
      "helpText": "Pregnant individuals may qualify for additional benefits or expedited coverage.",
      "conditions": null
    },
    {
      "key": "pregnancy_due_date",
      "label": "When is your due date?",
      "type": "date",
      "required": true,
      "helpText": "This helps us determine the duration of pregnancy-related coverage.",
      "conditions": [
        {
          "fieldKey": "is_pregnant",
          "operator": "equals",
          "value": "true"
        }
      ]
    }
  ]
}
```

**No backend changes required** - the API already supports all necessary fields.

---

## Summary

All data structures are frontend TypeScript types. Key entities:

1. **QuestionDto** (existing): Question definitions with conditions and helpText
2. **QuestionCondition** (existing): Visibility rules
3. **AnswerMap** (new): Fast lookup for condition evaluation
4. **VisibilityState** (new): Computed visibility cache
5. **Component Props** (new): Type-safe interfaces for React components

No database migrations, no backend changes - purely client-side state management.
