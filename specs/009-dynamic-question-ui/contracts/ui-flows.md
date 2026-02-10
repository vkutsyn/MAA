# UI Contracts: Dynamic Eligibility Question UI

**Feature**: 009-dynamic-question-ui  
**Date**: February 10, 2026  
**Purpose**: Document user flows, component contracts, and interaction patterns

## Overview

This document defines the contracts between components and user interaction flows for dynamic question rendering. Since this is a frontend-only feature with no backend API changes, contracts focus on component interfaces and user journeys.

---

## User Flow 1: Basic Question Rendering (P1)

### Flow Description

User navigates to wizard, sees questions, answers them, and proceeds.

### Sequence Diagram

```
User                 WizardPage              QuestionRenderer        Backend API
│                    │                      │                       │
├─Navigate to wizard─►                      │                       │
│                    ├─Fetch questions──────┼──────────────────────►│
│                    │                      │                       │
│                    ◄─────────────────Return QuestionDto[]─────────┤
│                    │                      │                       │
│                    ├─Render questions────►│                       │
│                    │                      ├─Render input controls │
│                    │                      ├─Render tooltip (if helpText)
│◄───Display questions────────────────────┤                       │
│                    │                      │                       │
├─Answer question───►│                      │                       │
│                    ├─Update state         │                       │
│                    ├─Save answer──────────┼──────────────────────►│
│                    │                      │                       │
├─Click Next────────►│                      │                       │
│                    ├─Navigate next step   │                       │
```

### Component Contract

**Input**: `QuestionDto[]` from API  
**Output**: Rendered questions with input controls  
**Invariants**:

- All questions rendered regardless of conditions (conditional logic in Flow 2)
- Input type matches question.type
- Labels properly associated with inputs

---

## User Flow 2: Conditional Question Rendering (P2)

### Flow Description

User answers a trigger question, conditional questions appear/disappear based on answer.

### Sequence Diagram

```
User                 WizardPage              ConditionalContainer   ConditionEvaluator
│                    │                      │                       │
├─Answer trigger Q──►│                      │                       │
│                    ├─Update answerMap     │                       │
│                    ├─Recompute visibility─┼──────────────────────►│
│                    │                      │   evaluateCondition() │
│                    ◄────────────────────Return VisibilityState────┤
│                    ├─Filter questions     │                       │
│                    ├─Render visible only─►│                       │
│                    │                    Check conditions against answerMap
│                    │                      │                       │
│                    │                      ├─If visible: render children
│                    │                      ├─If hidden: return null
│◄───Questions appear/disappear─────────────┤                       │
│                    │                      │                       │
├─Answer conditional Q────────────────────►│                       │
│                    ├─Save answer          │                       │
│                    │                      │                       │
├─Change trigger answer──────────────────►│                       │
│                    ├─Recompute visibility─┼──────────────────────►│
│                    ◄────────────────────Return VisibilityState────┤
│◄───Questions hide (but answers preserved)─┤                       │
```

### Component Contract: ConditionalQuestionContainer

**Props**:

```typescript
interface ConditionalQuestionContainerProps {
  question: QuestionDto;
  answers: AnswerMap;
  children: React.ReactNode;
  onVisibilityChange?: (visible: boolean) => void;
}
```

**Behavior**:

- **Input**: Question with conditions, current answers
- **Output**: Renders children if all conditions pass, null otherwise
- **Side Effects**: Calls onVisibilityChange when visibility changes

**Contract Rules**:

1. If `question.conditions` is null/empty → always render children
2. If `question.conditions` exists → evaluate all conditions with AND logic
3. If ALL conditions pass → render children
4. If ANY condition fails → return null (do not render)
5. When visibility changes → call onVisibilityChange callback

**Accessibility Requirements**:

- Use `aria-live="polite"` announcements for appearing questions
- Preserve focus if currently focused question remains visible
- Move focus to safe location if currently focused question disappears

---

## User Flow 3: Tooltip Interaction (P3)

### Flow Description

User clicks/hovers help icon, sees "Why we ask this" explanation, closes tooltip.

### Sequence Diagram

```
User                 QuestionRenderer        QuestionTooltip        Radix UI Tooltip
│                    │                      │                       │
◄───Render question with help icon──────────┤                       │
│                    │                      │                       │
├─Click help icon───►│                      │                       │
│                    ├─Toggle tooltip──────►│                       │
│                    │                      ├─Open tooltip─────────►│
│                    │                      │                    Set aria-describedby
│◄─────────────────Display tooltip content──────────────────────────┤
│                    │                      │                       │
├─Press Escape──────►│                      │                       │
│                    │                      ├─Close tooltip────────►│
│◄─────────────────Tooltip hidden───────────────────────────────────┤
│                    │                      │                       │
├─Click outside─────►│                      │                       │
│                    │                      ├─Close tooltip────────►│
│◄─────────────────Tooltip hidden───────────────────────────────────┤
```

### Component Contract: QuestionTooltip

**Props**:

```typescript
interface QuestionTooltipProps {
  questionKey: string;
  helpText: string;
  triggerLabel?: string; // Defaults to "Why we ask this"
}
```

**Behavior**:

- **Input**: Help text string, optional trigger label
- **Output**: Rendered help icon with accessible tooltip
- **Trigger**: Click or keyboard (Enter/Space)
- **Dismissal**: Escape key, click outside, or re-click trigger

**Contract Rules**:

1. Tooltip trigger MUST be a `<button type="button">` for keyboard accessibility
2. Trigger MUST have `aria-label="Why we ask this"` (or custom label)
3. Tooltip content MUST be linked via `aria-describedby`
4. Tooltip MUST close on Escape key
5. Tooltip MUST close when clicking outside
6. Tooltip positioning MUST adapt to viewport (Radix auto-positioning)

**Accessibility Requirements**:

- Keyboard navigation: Tab to focus, Enter/Space to open
- Screen reader: aria-describedby announces content when focused
- Focus trap: Focus remains on trigger, content read via aria
- Color contrast: Text meets WCAG 2.1 AA (4.5:1 ratio)

---

## Component Interaction Contract

### WizardPage → ConditionalQuestionContainer

**Data Flow**:

```typescript
// WizardPage computes visible questions
const answerMap = useAnswerMap(sessionAnswers)
const visibilityState = useMemo(
  () => computeVisibility(questions, answerMap),
  [questions, answerMap]
)
const visibleQuestions = questions.filter(q =>
  visibilityState.visibleQuestionKeys.has(q.key)
)

// WizardPage renders only visible questions
{visibleQuestions.map(question => (
  <ConditionalQuestionContainer
    key={question.key}
    question={question}
    answers={answerMap}
  >
    <QuestionRenderer question={question} />
  </ConditionalQuestionContainer>
))}
```

**Contract**:

- WizardPage MUST provide current answerMap to ConditionalQuestionContainer
- WizardPage MUST recompute visibility when answerMap changes
- WizardPage MUST NOT render questions that fail visibility check

---

## State Management Contract

### Zustand Store Actions

```typescript
// Update answer and trigger visibility recomputation
updateAnswer: (fieldKey: string, value: string) => {
  // 1. Update answerMap
  set(state => ({
    answerMap: new Map(state.answerMap).set(fieldKey, value)
  }))

  // 2. Recompute visibility
  get().recomputeVisibility()

  // 3. Submit to backend
  submitAnswer({ fieldKey, answerValue: value, ... })
}

// Recompute visibility state
recomputeVisibility: () => {
  const { questions, answerMap } = get()
  const newVisibility = computeVisibility(questions, answerMap)
  set({ visibilityState: newVisibility })
}
```

**Contract Rules**:

1. Every answer update MUST trigger visibility recomputation
2. Visibility recomputation MUST happen synchronously (before render)
3. AnswerMap MUST remain immutable (new Map on each update for React reactivity)

---

## Performance Contract

### Condition Evaluation Performance

**Requirements** (from Constitution IV):

- Question rendering: <1 second for 50 questions
- Conditional evaluation: <200ms after answer change
- Tooltip display: <100ms on interaction

**Implementation Contract**:

```typescript
// Memoize condition evaluation
const evaluateCondition = useMemo(
  () => (condition: QuestionCondition, answers: AnswerMap) => {
    // O(1) map lookup, O(1) comparison
    const answer = answers.get(condition.fieldKey);
    if (!answer) return false;
    return performComparison(condition.operator, answer, condition.value);
  },
  [], // Pure function, no deps
);

// Memoize visibility computation
const visibilityState = useMemo(
  () => computeVisibility(questions, answerMap),
  [questions, answerMap], // Only recompute on change
);
```

**Guarantees**:

- Condition evaluation is O(1) per condition (Map lookup)
- Visibility computation is O(n\*m) where n=questions, m=avg conditions per question
- For typical case (20 questions, 2 conditions avg) = 40 evaluations < 200ms ✓
- React.memo prevents re-rendering unchanged questions

---

## Error Handling Contract

### Invalid Condition References

**Scenario**: Condition references non-existent fieldKey

**Contract**:

```typescript
function evaluateCondition(
  condition: QuestionCondition,
  answers: AnswerMap,
): boolean {
  const answer = answers.get(condition.fieldKey);

  // If referenced question not answered (or doesn't exist), condition fails
  if (answer === undefined) {
    console.warn(
      `Condition references unanswered question: ${condition.fieldKey}`,
    );
    return false; // Safe default: hide conditional question
  }

  // Proceed with evaluation...
}
```

**Rules**:

1. Missing answer → condition fails (safe default: hide question)
2. Invalid operator → log error, return false
3. Type mismatch (e.g., "gt" on boolean) → log error, return false

---

## Accessibility Contract

### Screen Reader Announcements

**Requirement**: Dynamic content MUST be announced (WCAG SC 4.1.3)

**Implementation**:

```tsx
<div
  role="region"
  aria-live="polite"
  aria-atomic="false"
  aria-relevant="additions removals"
>
  {conditionalQuestions.map((q) => (
    <ConditionalQuestionContainer key={q.key} question={q} answers={answers}>
      <QuestionRenderer question={q} />
    </ConditionalQuestionContainer>
  ))}
</div>
```

**Contract**:

- `aria-live="polite"`: Announce changes after current speech
- `aria-atomic="false"`: Only announce changed elements
- `aria-relevant="additions removals"`: Announce both appearing and disappearing questions

---

## Test Contract

### Required Test Coverage

**Unit Tests** (conditionEvaluator.test.ts):

- ✓ All operators (equals, not_equals, gt, gte, lt, lte, includes)
- ✓ Edge cases (missing answer, invalid operator, type mismatch)
- ✓ Coverage target: 90%+

**Component Tests** (ConditionalQuestionContainer.test.tsx):

- ✓ Shows children when conditions pass
- ✓ Hides children when conditions fail
- ✓ Updates visibility when answers change
- ✓ Coverage target: 85%+

**Accessibility Tests** (QuestionTooltip.test.tsx):

- ✓ Tooltip has proper aria attributes
- ✓ Keyboard navigation works (Tab, Enter, Escape)
- ✓ Screen reader compatibility (aria-describedby)
- ✓ Coverage target: 80%+

**E2E Tests** (conditional-flow.e2e.test.tsx):

- ✓ Trigger question → conditional appears
- ✓ Change trigger → conditional disappears
- ✓ Answers preserved when conditional hidden then shown
- ✓ Coverage: All P1 and P2 user scenarios

---

## Summary

Key contracts:

1. **ConditionalQuestionContainer**: Evaluates conditions, renders children only if visible
2. **QuestionTooltip**: Accessible tooltip with keyboard support and aria-describedby
3. **State Management**: Immutable updates, synchronous visibility recomputation
4. **Performance**: <200ms evaluation, memoized computations
5. **Accessibility**: aria-live regions, semantic HTML, keyboard navigation
6. **Error Handling**: Safe defaults, logging, no crashes on invalid data

All contracts align with MAA Constitution principles (I-IV).
