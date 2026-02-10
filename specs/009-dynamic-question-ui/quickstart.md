# Quickstart Guide: Dynamic Eligibility Question UI

**Feature**: 009-dynamic-question-ui  
**Date**: February 10, 2026  
**Audience**: Developers implementing this feature

## Overview

This guide provides step-by-step instructions for implementing dynamic question rendering with conditional logic and tooltips in the Medicaid eligibility wizard.

**Prerequisites**:
- Feature branch `009-dynamic-question-ui` checked out
- Frontend development environment set up (`cd frontend && npm install`)
- Backend running locally (question definitions API available)

**Development Approach**: Test-First Development (TDD)
1. Write tests based on acceptance criteria
2. Run tests (expect failures)
3. Implement minimum code to pass tests
4. Refactor while keeping tests green

---

## Phase 1: Condition Evaluator (Pure Logic)

### Step 1.1: Write Unit Tests

**File**: `frontend/tests/features/wizard/conditionEvaluator.test.ts`

```typescript
import { describe, it, expect } from 'vitest'
import { evaluateCondition, computeVisibility } from '@/features/wizard/conditionEvaluator'
import { QuestionCondition, QuestionDto } from '@/features/wizard/types'

describe('evaluateCondition', () => {
  const answers = new Map([
    ['income', '30000'],
    ['age', '35'],
    ['is_pregnant', 'true'],
  ])

  describe('equals operator', () => {
    it('returns true when answer matches value', () => {
      const condition: QuestionCondition = {
        fieldKey: 'income',
        operator: 'equals',
        value: '30000'
      }
      expect(evaluateCondition(condition, answers)).toBe(true)
    })

    it('returns false when answer does not match', () => {
      const condition: QuestionCondition = {
        fieldKey: 'income',
        operator: 'equals',
        value: '40000'
      }
      expect(evaluateCondition(condition, answers)).toBe(false)
    })
  })

  describe('gt operator', () => {
    it('returns true when answer is greater than value', () => {
      const condition: QuestionCondition = {
        fieldKey: 'age',
        operator: 'gt',
        value: '30'
      }
      expect(evaluateCondition(condition, answers)).toBe(true)
    })
  })

  describe('missing answer', () => {
    it('returns false when question not answered', () => {
      const condition: QuestionCondition = {
        fieldKey: 'not_answered',
        operator: 'equals',
        value: 'yes'
      }
      expect(evaluateCondition(condition, answers)).toBe(false)
    })
  })

  // Add tests for: not_equals, gte, lt, lte, includes
})

describe('computeVisibility', () => {
  it('includes questions without conditions', () => {
    const questions: QuestionDto[] = [
      { key: 'q1', label: 'Question 1', type: 'string', required: true }
    ]
    const result = computeVisibility(questions, new Map())
    expect(result.visibleQuestionKeys.has('q1')).toBe(true)
  })

  it('includes questions when all conditions pass', () => {
    const questions: QuestionDto[] = [
      { 
        key: 'q2', 
        label: 'Question 2', 
        type: 'string', 
        required: true,
        conditions: [
          { fieldKey: 'q1', operator: 'equals', value: 'yes' }
        ]
      }
    ]
    const answers = new Map([['q1', 'yes']])
    const result = computeVisibility(questions, answers)
    expect(result.visibleQuestionKeys.has('q2')).toBe(true)
  })

  it('excludes questions when any condition fails', () => {
    const questions: QuestionDto[] = [
      { 
        key: 'q3', 
        label: 'Question 3', 
        type: 'string', 
        required: true,
        conditions: [
          { fieldKey: 'q1', operator: 'equals', value: 'yes' },
          { fieldKey: 'q2', operator: 'equals', value: 'no' }
        ]
      }
    ]
    const answers = new Map([['q1', 'yes'], ['q2', 'yes']])
    const result = computeVisibility(questions, answers)
    expect(result.visibleQuestionKeys.has('q3')).toBe(false)
  })
})
```

**Run Tests**: `npm test conditionEvaluator.test.ts` (should fail - not implemented)

---

### Step 1.2: Implement Condition Evaluator

**File**: `frontend/src/features/wizard/conditionEvaluator.ts`

```typescript
import { QuestionCondition, QuestionDto } from './types'

export type AnswerMap = Map<string, string>

export interface VisibilityState {
  visibleQuestionKeys: Set<string>
  evaluatedAt: number
}

/**
 * Evaluates a single condition against answers.
 * Returns false if answer missing or condition fails.
 */
export function evaluateCondition(
  condition: QuestionCondition,
  answers: AnswerMap
): boolean {
  const answer = answers.get(condition.fieldKey)
  
  if (answer === undefined) {
    console.warn(`Condition references unanswered question: ${condition.fieldKey}`)
    return false
  }

  switch (condition.operator) {
    case 'equals':
      return answer === condition.value
    
    case 'not_equals':
      return answer !== condition.value
    
    case 'gt':
      return Number(answer) > Number(condition.value)
    
    case 'gte':
      return Number(answer) >= Number(condition.value)
    
    case 'lt':
      return Number(answer) < Number(condition.value)
    
    case 'lte':
      return Number(answer) <= Number(condition.value)
    
    case 'includes':
      return answer.includes(condition.value)
    
    default:
      console.error(`Unknown operator: ${condition.operator}`)
      return false
  }
}

/**
 * Computes which questions should be visible based on conditions and answers.
 */
export function computeVisibility(
  questions: QuestionDto[],
  answers: AnswerMap
): VisibilityState {
  const visibleKeys = new Set<string>()

  questions.forEach(question => {
    // No conditions = always visible
    if (!question.conditions || question.conditions.length === 0) {
      visibleKeys.add(question.key)
      return
    }

    // All conditions must pass (AND logic)
    const allConditionsPass = question.conditions.every(condition =>
      evaluateCondition(condition, answers)
    )

    if (allConditionsPass) {
      visibleKeys.add(question.key)
    }
  })

  return {
    visibleQuestionKeys: visibleKeys,
    evaluatedAt: Date.now()
  }
}
```

**Run Tests**: `npm test conditionEvaluator.test.ts` (should pass)

---

## Phase 2: QuestionTooltip Component

### Step 2.1: Install shadcn/ui Tooltip

```bash
cd frontend
npx shadcn-ui@latest add tooltip
```

This creates `frontend/src/components/ui/tooltip.tsx` with Radix UI primitives.

---

### Step 2.2: Write Component Tests

**File**: `frontend/tests/features/wizard/QuestionTooltip.test.tsx`

```typescript
import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QuestionTooltip } from '@/features/wizard/QuestionTooltip'

describe('QuestionTooltip', () => {
  it('renders help icon with accessible label', () => {
    render(<QuestionTooltip questionKey="q1" helpText="This is help text" />)
    expect(screen.getByRole('button', { name: 'Why we ask this' })).toBeInTheDocument()
  })

  it('shows tooltip on click', async () => {
    const user = userEvent.setup()
    render(<QuestionTooltip questionKey="q1" helpText="Test help text" />)
    
    const trigger = screen.getByRole('button', { name: 'Why we ask this' })
    await user.click(trigger)
    
    expect(screen.getByText('Test help text')).toBeVisible()
  })

  it('closes tooltip on Escape key', async () => {
    const user = userEvent.setup()
    render(<QuestionTooltip questionKey="q1" helpText="Test help text" />)
    
    const trigger = screen.getByRole('button', { name: 'Why we ask this' })
    await user.click(trigger)
    expect(screen.getByText('Test help text')).toBeVisible()
    
    await user.keyboard('{Escape}')
    expect(screen.queryByText('Test help text')).not.toBeInTheDocument()
  })

  it('is keyboard accessible', async () => {
    const user = userEvent.setup()
    render(<QuestionTooltip questionKey="q1" helpText="Keyboard test" />)
    
    await user.tab() // Focus trigger
    expect(screen.getByRole('button', { name: 'Why we ask this' })).toHaveFocus()
    
    await user.keyboard('{Enter}') // Open tooltip
    expect(screen.getByText('Keyboard test')).toBeVisible()
  })
})
```

**Run Tests**: `npm test QuestionTooltip.test.tsx` (should fail)

---

### Step 2.3: Implement QuestionTooltip

**File**: `frontend/src/features/wizard/QuestionTooltip.tsx`

```typescript
import { HelpCircle } from 'lucide-react'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'

interface QuestionTooltipProps {
  questionKey: string
  helpText: string
  triggerLabel?: string
}

export function QuestionTooltip({
  questionKey,
  helpText,
  triggerLabel = 'Why we ask this'
}: QuestionTooltipProps) {
  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <button
            type="button"
            aria-label={triggerLabel}
            className="ml-2 inline-flex items-center text-muted-foreground hover:text-foreground transition-colors"
          >
            <HelpCircle className="h-4 w-4" />
          </button>
        </TooltipTrigger>
        <TooltipContent className="max-w-sm">
          <p className="text-sm">{helpText}</p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}
```

**Run Tests**: `npm test QuestionTooltip.test.tsx` (should pass)

---

## Phase 3: ConditionalQuestionContainer Component

### Step 3.1: Write Component Tests

**File**: `frontend/tests/features/wizard/ConditionalQuestionContainer.test.tsx`

```typescript
import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { ConditionalQuestionContainer } from '@/features/wizard/ConditionalQuestionContainer'
import { QuestionDto } from '@/features/wizard/types'

describe('ConditionalQuestionContainer', () => {
  const baseQuestion: QuestionDto = {
    key: 'test_q',
    label: 'Test Question',
    type: 'string',
    required: true
  }

  it('renders children when no conditions', () => {
    const answers = new Map()
    render(
      <ConditionalQuestionContainer question={baseQuestion} answers={answers}>
        <div>Test Content</div>
      </ConditionalQuestionContainer>
    )
    expect(screen.getByText('Test Content')).toBeInTheDocument()
  })

  it('renders children when all conditions pass', () => {
    const question = {
      ...baseQuestion,
      conditions: [
        { fieldKey: 'trigger', operator: 'equals' as const, value: 'yes' }
      ]
    }
    const answers = new Map([['trigger', 'yes']])
    
    render(
      <ConditionalQuestionContainer question={question} answers={answers}>
        <div>Conditional Content</div>
      </ConditionalQuestionContainer>
    )
    expect(screen.getByText('Conditional Content')).toBeInTheDocument()
  })

  it('does not render children when condition fails', () => {
    const question = {
      ...baseQuestion,
      conditions: [
        { fieldKey: 'trigger', operator: 'equals' as const, value: 'yes' }
      ]
    }
    const answers = new Map([['trigger', 'no']])
    
    render(
      <ConditionalQuestionContainer question={question} answers={answers}>
        <div>Should Not Appear</div>
      </ConditionalQuestionContainer>
    )
    expect(screen.queryByText('Should Not Appear')).not.toBeInTheDocument()
  })
})
```

**Run Tests**: `npm test ConditionalQuestionContainer.test.tsx` (should fail)

---

### Step 3.2: Implement ConditionalQuestionContainer

**File**: `frontend/src/features/wizard/ConditionalQuestionContainer.tsx`

```typescript
import { ReactNode } from 'react'
import { QuestionDto } from './types'
import { AnswerMap, evaluateCondition } from './conditionEvaluator'

interface ConditionalQuestionContainerProps {
  question: QuestionDto
  answers: AnswerMap
  children: ReactNode
}

export function ConditionalQuestionContainer({
  question,
  answers,
  children
}: ConditionalQuestionContainerProps) {
  // No conditions = always visible
  if (!question.conditions || question.conditions.length === 0) {
    return <>{children}</>
  }

  // Check if all conditions pass
  const isVisible = question.conditions.every(condition =>
    evaluateCondition(condition, answers)
  )

  if (!isVisible) {
    return null
  }

  return (
    <div
      role="region"
      aria-live="polite"
      aria-atomic="false"
      aria-relevant="additions"
    >
      {children}
    </div>
  )
}
```

**Run Tests**: `npm test ConditionalQuestionContainer.test.tsx` (should pass)

---

## Phase 4: Integrate into Wizard

### Step 4.1: Update Zustand Store

**File**: `frontend/src/features/wizard/store.ts`

Add answer map and visibility state:

```typescript
import { AnswerMap, VisibilityState, computeVisibility } from './conditionEvaluator'

interface WizardStore {
  // ... existing state ...
  
  // New state
  answerMap: AnswerMap
  visibilityState: VisibilityState | null
  
  // New actions
  updateAnswerMap: (fieldKey: string, value: string) => void
  recomputeVisibility: () => void
}

export const useWizardStore = create<WizardStore>((set, get) => ({
  // ... existing state ...
  
  answerMap: new Map(),
  visibilityState: null,
  
  updateAnswerMap: (fieldKey, value) => {
    set(state => ({
      answerMap: new Map(state.answerMap).set(fieldKey, value)
    }))
    get().recomputeVisibility()
  },
  
  recomputeVisibility: () => {
    const { questions, answerMap } = get()
    if (questions.length > 0) {
      const newVisibility = computeVisibility(questions, answerMap)
      set({ visibilityState: newVisibility })
    }
  }
}))
```

---

### Step 4.2: Update WizardPage

**File**: `frontend/src/features/wizard/WizardPage.tsx`

Add conditional rendering and tooltips:

```typescript
import { useEffect, useMemo } from 'react'
import { ConditionalQuestionContainer } from './ConditionalQuestionContainer'
import { computeVisibility } from './conditionEvaluator'

export function WizardPage() {
  // ... existing code ...
  
  const answerMap = useWizardStore(state => state.answerMap)
  const recomputeVisibility = useWizardStore(state => state.recomputeVisibility)
  
  // Compute visible questions
  const visibleQuestions = useMemo(() => {
    const visibility = computeVisibility(questions, answerMap)
    return questions.filter(q => visibility.visibleQuestionKeys.has(q.key))
  }, [questions, answerMap])
  
  // Recompute when questions loaded
  useEffect(() => {
    if (questions.length > 0) {
      recomputeVisibility()
    }
  }, [questions, recomputeVisibility])
  
  // ... rest of component ...
  
  return (
    <div className="mx-auto max-w-2xl space-y-6">
      {/* ... existing header ... */}
      
      {visibleQuestions.map(question => (
        <ConditionalQuestionContainer
          key={question.key}
          question={question}
          answers={answerMap}
        >
          <WizardStep 
            question={question}
            onNext={handleNext}
            onBack={handleBack}
          />
        </ConditionalQuestionContainer>
      ))}
    </div>
  )
}
```

---

### Step 4.3: Enhance WizardStep with Tooltips

**File**: `frontend/src/features/wizard/WizardStep.tsx`

Add QuestionTooltip to question labels:

```typescript
import { QuestionTooltip } from './QuestionTooltip'

export function WizardStep({ question, onNext, onBack }: WizardStepProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center">
          {question.label}
          {question.helpText && (
            <QuestionTooltip 
              questionKey={question.key}
              helpText={question.helpText}
            />
          )}
        </CardTitle>
      </CardHeader>
      <CardContent>
        {/* ... existing input rendering ... */}
      </CardContent>
    </Card>
  )
}
```

---

## Phase 5: Testing & Validation

### Step 5.1: Run All Tests

```bash
npm test
```

**Expected**:
- ✓ Unit tests (conditionEvaluator): 90%+ coverage
- ✓ Component tests (QuestionTooltip, ConditionalContainer): 85%+ coverage
- ✓ Integration tests: Wizard flows work with conditional logic

---

### Step 5.2: Manual Testing Checklist

**Conditional Logic**:
- [ ] Answer trigger question → dependent questions appear
- [ ] Change trigger answer → dependent questions disappear
- [ ] Answers to conditional questions preserved when hidden then re-shown
- [ ] Multiple conditions (AND logic) work correctly

**Tooltips**:
- [ ] Help icon visible on questions with helpText
- [ ] Click help icon → tooltip appears
- [ ] Press Escape → tooltip closes
- [ ] Click outside → tooltip closes
- [ ] Keyboard navigation works (Tab, Enter, Escape)

**Accessibility**:
- [ ] Screen reader announces new questions (test with NVDA/JAWS)
- [ ] Keyboard-only navigation complete (no mouse required)
- [ ] Color contrast meets WCAG 2.1 AA (use axe DevTools)
- [ ] Focus management correct when questions appear/disappear

---

### Step 5.3: Performance Testing

Use Chrome DevTools Performance profiler:

```bash
npm run dev
# Navigate to wizard
# Open DevTools → Performance tab
# Record interaction
# Change trigger answer (should trigger conditional evaluation)
# Stop recording
```

**Check**:
- Conditional evaluation < 200ms
- No layout thrashing
- React renders optimized (minimal re-renders)

---

## Phase 6: Documentation & Handoff

### Step 6.1: Update README

Add section to frontend README documenting new components:

```markdown
## Dynamic Questions

The wizard supports conditional questions that appear/disappear based on user answers.

### Components

- `ConditionalQuestionContainer`: Wraps questions with conditional logic
- `QuestionTooltip`: Displays "Why we ask this" help text
- `conditionEvaluator.ts`: Pure functions for condition evaluation

### Usage

Questions with `conditions` property will only display when all conditions pass.
```

---

### Step 6.2: Commit Changes

```bash
git add .
git commit -m "feat(wizard): implement dynamic question rendering with tooltips

- Add condition evaluator with full operator support
- Add QuestionTooltip component with keyboard accessibility
- Add ConditionalQuestionContainer for visibility logic
- Integrate conditional rendering into WizardPage
- Add comprehensive unit, component, and E2E tests

Closes #009-dynamic-question-ui
```

---

### Step 6.3: Create Pull Request

**Title**: `feat(wizard): Dynamic Eligibility Question UI`

**Description**:
```
## Summary
Implements dynamic question rendering with conditional logic and tooltips.

## Changes
- ✅ Conditional question visibility based on user answers
- ✅ "Why we ask this" tooltips with keyboard support
- ✅ WCAG 2.1 AA accessibility (aria-live, semantic HTML)
- ✅ Performance optimized (<200ms condition evaluation)

## Testing
- Unit tests: 92% coverage (conditionEvaluator)
- Component tests: 87% coverage
- E2E tests: All P1 and P2 user scenarios
- Manual accessibility testing with NVDA

## Constitution Compliance
- ✅ Principle I: Pure condition evaluation logic (testable)
- ✅ Principle II: Test-first development (all tests green)
- ✅ Principle III: WCAG 2.1 AA compliant, keyboard accessible
- ✅ Principle IV: Performance targets met (<200ms evaluation)

Closes #009
```

---

## Troubleshooting

### Tests Failing

**Issue**: "Cannot find module '@/features/wizard/conditionEvaluator'"

**Fix**: Check `tsconfig.json` has path alias:
```json
{
  "compilerOptions": {
    "paths": {
      "@/*": ["./src/*"]
    }
  }
}
```

---

### Tooltip Not Opening

**Issue**: Tooltip doesn't appear on click

**Fix**: Ensure TooltipProvider wraps the entire tree or individual Tooltip components.

---

### Conditional Questions Flickering

**Issue**: Questions flash when typing in trigger field

**Fix**: Add debounce to answer submission:
```typescript
const debouncedUpdate = useMemo(
  () => debounce((key, value) => updateAnswerMap(key, value), 200),
  []
)
```

---

## Next Steps

After this feature is complete:

1. **Phase 2**: Create tasks.md using `/speckit.tasks` command
2. **Implementation**: Break down into atomic tasks
3. **Testing**: Maintain >80% test coverage
4. **Review**: Get PR approved by maintainers
5. **Deploy**: Merge to main, deploy to staging

---

## Resources

- [Spec Document](./spec.md) - Full requirement specification
- [Data Model](./data-model.md) - Type definitions and state structures
- [UI Flows](./contracts/ui-flows.md) - Component contracts and user flows
- [Research](./research.md) - Architecture decisions and alternatives
- [MAA Constitution](/.specify/memory/constitution.md) - Project standards

**Status**: Ready for `/speckit.tasks` command to generate task breakdown.
