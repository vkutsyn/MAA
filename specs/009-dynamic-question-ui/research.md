# Research: Dynamic Eligibility Question UI

**Feature**: 009-dynamic-question-ui  
**Date**: February 10, 2026  
**Purpose**: Document research findings for conditional question rendering, tooltips, and accessibility patterns

## Research Overview

This document consolidates research findings for implementing dynamic question rendering with conditional logic and accessible tooltips in the Medicaid eligibility wizard.

## Research Areas

### 1. Conditional Rendering Patterns in React

**Decision**: Use composition with separate visibility logic + React.memo for performance

**Rationale**:

- Separating condition evaluation from rendering enables pure function testing
- React.memo prevents unnecessary re-renders when parent answers change but conditions don't
- Composition pattern (wrapper component for conditional logic) keeps QuestionRenderer focused on display

**Alternatives Considered**:

- Inline condition evaluation in WizardStep: Rejected (mixes concerns, untestable logic)
- CSS display:none for hidden questions: Rejected (screen readers would still announce, violates WCAG)
- Single monolithic question component: Rejected (violates single responsibility)

**Implementation Pattern**:

```typescript
// Separate pure evaluation function
function evaluateCondition(condition: QuestionCondition, answers: AnswerMap): boolean

// Wrapper component for conditional rendering
function ConditionalQuestionContainer({ question, answers, children })

// Composed in WizardPage
{visibleQuestions.map(q => (
  <ConditionalQuestionContainer key={q.key} question={q} answers={answers}>
    <QuestionRenderer question={q} />
  </ConditionalQuestionContainer>
))}
```

**Best Practices Source**: React docs on composition, Kent C. Dodds testing patterns

---

### 2. Accessibility for Dynamic Content (WCAG 2.1 AA)

**Decision**: Use `aria-live="polite"` regions + focus management for appearing questions

**Rationale**:

- WCAG SC 4.1.3 (Status Messages) requires dynamic content be announced to screen readers
- `aria-live="polite"` announces new questions without interrupting current focus
- Focus management ensures keyboard users don't lose context when questions disappear
- Semantic HTML ensures proper structure (fieldset, legend, label)

**Alternatives Considered**:

- `aria-live="assertive"`: Rejected (too disruptive for non-urgent question additions)
- No aria-live: Rejected (fails WCAG 4.1.3, screen reader users unaware of new content)
- Manual focus() on new questions: Rejected (disrupts user flow, unexpected behavior)

**Implementation Pattern**:

```tsx
<div role="region" aria-live="polite" aria-atomic="false">
  {conditionalQuestions.map((q) => (
    <fieldset key={q.key}>
      <legend>{q.label}</legend>
      <QuestionRenderer question={q} />
    </fieldset>
  ))}
</div>
```

**Best Practices Source**:

- WCAG 2.1 SC 4.1.3 (Status Messages)
- WAI-ARIA Authoring Practices Guide for dynamic content
- Deque University accessibility patterns

---

### 3. Tooltip Implementation with Keyboard Support

**Decision**: Use Radix UI Tooltip/Popover (via shadcn/ui) with keyboard trigger

**Rationale**:

- Radix UI provides WCAG-compliant tooltip primitives out of the box
- Keyboard activation (Enter/Space), Escape to close, auto-positioning
- `aria-describedby` automatically links tooltip content to trigger
- Already integrated in project via shadcn/ui

**Alternatives Considered**:

- Native `title` attribute: Rejected (not keyboard-accessible, poor mobile support)
- Custom tooltip with vanilla JS: Rejected (reinventing wheel, accessibility gaps)
- Dialog/Modal for help text: Rejected (too heavy-weight, disrupts flow)

**Implementation Pattern**:

```tsx
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";

<Tooltip>
  <TooltipTrigger asChild>
    <button type="button" aria-label="Why we ask this">
      <HelpCircle className="h-4 w-4" />
    </button>
  </TooltipTrigger>
  <TooltipContent>
    <p>{question.helpText}</p>
  </TooltipContent>
</Tooltip>;
```

**Best Practices Source**:

- Radix UI Tooltip documentation
- shadcn/ui component patterns
- WCAG SC 1.3.1 (Info and Relationships) - `aria-describedby` usage

---

### 4. Performance Optimization for Condition Evaluation

**Decision**: Memoize condition evaluation results + debounce answer changes

**Rationale**:

- Condition evaluation is O(n) for n questions; memoization prevents redundant calculations
- Debouncing prevents flickering when user rapidly changes answers (e.g., typing in text field)
- React.useMemo for computed visible questions list
- Pure evaluation function enables easy memoization

**Alternatives Considered**:

- Evaluate on every render: Rejected (unnecessary CPU cycles, potential jank)
- Web Worker for evaluation: Rejected (overkill for simple operators, serialization overhead)
- Lodash throttle: Rejected (debounce preferred for trailing evaluation)

**Implementation Pattern**:

```typescript
// Pure memoizable function
const evaluateCondition = memoize((condition, answers) => { ... })

// In component
const visibleQuestions = useMemo(() =>
  questions.filter(q =>
    !q.conditions || q.conditions.every(c => evaluateCondition(c, answers))
  ),
  [questions, answers]
)

// Debounced answer submission
const debouncedSubmit = useMemo(
  () => debounce((answer) => submitAnswer(answer), 200),
  []
)
```

**Best Practices Source**:

- React useMemo documentation
- Web.dev performance patterns
- MAA Constitution IV (Performance: <200ms conditional evaluation target)

---

### 5. Testing Strategies for Conditional UI

**Decision**: Three-tier testing - unit (evaluator), component (rendering), E2E (flows)

**Rationale**:

- Unit tests verify condition evaluation logic in isolation (fast, comprehensive edge cases)
- Component tests verify visibility behavior without full wizard state (focused, maintainable)
- E2E tests verify real user flows (answer trigger → questions appear → answers preserved)
- Aligns with MAA Constitution II (Test-First Development)

**Alternatives Considered**:

- E2E tests only: Rejected (slow, hard to debug, incomplete edge case coverage)
- Component tests only: Rejected (misses integration issues with state management)
- Manual testing: Rejected (non-repeatable, not test-first)

**Implementation Pattern**:

```typescript
// Unit test (conditionEvaluator.test.ts)
describe('evaluateCondition', () => {
  it('returns true when equals condition matches', () => {
    const condition = { fieldKey: 'income', operator: 'equals', value: '30000' }
    const answers = new Map([['income', '30000']])
    expect(evaluateCondition(condition, answers)).toBe(true)
  })
})

// Component test (ConditionalQuestionContainer.test.tsx)
it('hides question when condition not met', () => {
  const question = { key: 'q2', conditions: [{ fieldKey: 'q1', operator: 'equals', value: 'yes' }] }
  render(<ConditionalQuestionContainer question={question} answers={new Map([['q1', 'no']])} />)
  expect(screen.queryByText('Question 2')).not.toBeInTheDocument()
})

// E2E test (conditional-flow.e2e.test.tsx)
it('shows pregnancy questions when user selects pregnant', async () => {
  const user = userEvent.setup()
  render(<WizardPage />)
  await user.click(screen.getByLabelText('Are you pregnant?'))
  await user.click(screen.getByLabelText('Yes'))
  expect(await screen.findByText('When is your due date?')).toBeVisible()
})
```

**Best Practices Source**:

- React Testing Library guiding principles
- Kent C. Dodds "Write tests. Not too many. Mostly integration."
- MAA Constitution II (80%+ coverage for domain logic)

---

## Summary of Key Decisions

| Area                  | Decision                    | Primary Justification                              |
| --------------------- | --------------------------- | -------------------------------------------------- |
| **Conditional Logic** | Pure function + composition | Testability, separation of concerns                |
| **Accessibility**     | aria-live + semantic HTML   | WCAG 2.1 AA SC 4.1.3 compliance                    |
| **Tooltips**          | Radix UI (shadcn/ui)        | Built-in keyboard support, aria-describedby        |
| **Performance**       | useMemo + debounce          | <200ms evaluation target, no flickering            |
| **Testing**           | Unit + Component + E2E      | Constitution II compliance, comprehensive coverage |

## Open Questions

None. All research areas resolved. Ready for Phase 1 (data model and contracts).
