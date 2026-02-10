# Implementation Plan: Dynamic Eligibility Question UI

**Branch**: `009-dynamic-question-ui` | **Date**: February 10, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/009-dynamic-question-ui/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

This feature implements a dynamic question rendering system for the Medicaid eligibility wizard that:
1. Renders questions dynamically from API-provided definitions
2. Evaluates conditional logic to show/hide questions based on user answers
3. Provides contextual "Why we ask this" tooltips for transparency

The implementation is frontend-focused, leveraging existing QuestionDto types that already include `helpText` and `conditions` properties. The primary work involves building React components for conditional rendering, tooltip UI, and testable logic functions for condition evaluation.

## Technical Context

**Language/Version**: TypeScript 5.7, React 19  
**Primary Dependencies**: React Router 7.13, Tanstack Query 5.90, Zustand 5.0, Zod 4.3, React Hook Form 7.71, shadcn/ui (Radix UI), Tailwind CSS 3.4  
**Storage**: Frontend state managed via Zustand; server state cached via Tanstack Query; answers persisted to backend via `/api/session-answers`  
**Testing**: Vitest + React Testing Library + @testing-library/jest-dom for unit and component tests  
**Target Platform**: Web browsers (Chrome, Firefox, Safari, Edge), responsive 375px → 1920px  
**Project Type**: Web application (frontend feature within existing React SPA)  
**Performance Goals**: 
  - Question rendering: <1 second for sets up to 50 questions
  - Conditional evaluation: <200ms after answer change
  - Tooltip display: <100ms on interaction
  - Zero layout shift when questions appear/disappear
**Constraints**: 
  - WCAG 2.1 AA compliance mandatory (keyboard nav, screen readers, color contrast)
  - Must work with existing wizard state management (Zustand store)
  - Must integrate with existing question API (`/api/questions/{state}`)
**Scale/Scope**: 
  - Expected 10-30 questions per state
  - Up to 5 conditional rules per question
  - Support 1,000+ concurrent wizard sessions

## Constitution Check _(GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.)_

**Constitution Reference**: See [MAA Constitution](/.specify/memory/constitution.md) for full principle definitions.

### Principle Alignment Assessment

**I. Code Quality & Clean Architecture**

- [x] Domain logic can be isolated from I/O (testable without DB/HTTP)
  - Conditional evaluation logic will be pure functions accepting question conditions and answer map, returning boolean visibility
  - No I/O dependencies in condition evaluation
- [x] Dependencies are explicitly injected (no service locator pattern)
  - React components receive question data via props or Zustand store hooks
  - API clients injected via React Query hooks
- [x] No classes exceed ~300 lines; single responsibility clear
  - Separate components: QuestionRenderer, ConditionalQuestionContainer, QuestionTooltip, ConditionEvaluator utility
  - Each component has single responsibility (rendering, evaluation, tooltip display)
- [x] DTO contracts explicitly defined (backend) or type-safe (frontend)
  - QuestionDto, QuestionCondition, QuestionOption types already defined in `types.ts`
  - All props interfaces will use strict TypeScript (no `any`)

**II. Test-First Development**

- [x] Test scenarios defined in spec before implementation begins
  - 13 acceptance scenarios across 3 user stories + 7 edge cases documented in spec.md
- [x] Unit tests target domain/application layers (80%+ coverage planned)
  - Unit tests for condition evaluator (pure logic, 85%+ coverage target)
  - Component tests for QuestionRenderer, ConditionalQuestionContainer, QuestionTooltip
- [x] Integration tests cover cross-layer flows (handlers + database, components + hooks)
  - E2E tests for conditional question flows (answer trigger → dependent questions appear)
  - Integration tests for tooltip interactions with screen readers
- [x] All async operations tested for success AND error paths
  - Question fetching via Tanstack Query tested for success, error, loading states
  - Answer submission tested for success and validation error paths

**III. UX Consistency & Accessibility (if user-facing)**

- [x] Design meets WCAG 2.1 AA (semantic HTML, keyboard nav, screen reader support)
  - Tooltips use Radix UI Popover (keyboard-accessible, aria-describedby, focus management)
  - Conditional questions use semantic HTML with proper aria-live regions for dynamic content
  - Help icons visible and keyboard-accessible (tab navigation, Enter/Space activation)
- [x] Mobile-first responsive design (375px → 1920px)
  - Existing wizard layout already responsive; new components will inherit breakpoints
  - Touch targets ≥44×44px for help icons on mobile
- [x] Plain language (no unexplained jargon; error messages actionable)
  - "Why we ask this" text provided by backend question definitions (already plain language)
  - Error messages for validation will remain actionable (already implemented)
- [x] Consistent component usage (shadcn/ui + Tailwind); no custom component behavior
  - Tooltips use shadcn/ui Tooltip or Popover component (Radix UI primitives)
  - No custom tooltip behavior; standard shadcn/ui patterns

**IV. Performance & Scalability (if performance-sensitive)**

- [x] Response time SLOs defined (align with Constitution targets: ≤2s eligibility, ≤500ms interactions)
  - Question rendering: <1s (Constitution target: ≤500ms for wizard steps) ✓ Acceptable
  - Conditional evaluation: <200ms (Constitution target: interactions ≤500ms) ✓ Passes
  - Tooltip display: <100ms (Constitution target: interactions ≤500ms) ✓ Passes
- [x] Caching strategy documented (Redis for immutable data, React Query for server state)
  - Question definitions cached via Tanstack Query with 5-minute staleTime
  - Backend already caches question definitions via QuestionDefinitionsCache (Redis)
- [x] Database queries indexed on common filters (state_id, eligibility_status, created_at)
  - N/A for this feature (frontend-only; no new database queries)
- [x] Load test target defined (1,000 concurrent users or domain-specific metric)
  - Frontend is stateless; supports 1,000 concurrent sessions (align with Constitution target)

**⚠️ VIOLATIONS FOUND?** None. All principles pass.

## Project Structure

### Documentation (this feature)

```text
specs/009-dynamic-question-ui/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── question-ui-flows.md  # User flow diagrams for conditional logic
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
frontend/
├── src/
│   ├── features/
│   │   └── wizard/
│   │       ├── WizardPage.tsx                  # [EXISTING] Main wizard orchestrator
│   │       ├── WizardStep.tsx                  # [EXISTING] Individual question display
│   │       ├── types.ts                        # [EXISTING] QuestionDto, QuestionCondition
│   │       ├── store.ts                        # [EXISTING] Zustand wizard state
│   │       ├── questionApi.ts                  # [EXISTING] Question fetching
│   │       ├── answerApi.ts                    # [EXISTING] Answer submission
│   │       │
│   │       ├── QuestionRenderer.tsx            # [NEW] Dynamic question type rendering
│   │       ├── ConditionalQuestionContainer.tsx # [NEW] Handles conditional visibility
│   │       ├── QuestionTooltip.tsx             # [NEW] "Why we ask this" help component
│   │       ├── conditionEvaluator.ts           # [NEW] Pure logic for condition evaluation
│   │       └── useConditionalQuestions.ts      # [NEW] Hook for managing conditional state
│   │
│   └── components/
│       └── ui/
│           └── tooltip.tsx                     # [EXISTING] shadcn/ui Tooltip component
│
└── tests/
    ├── features/
    │   └── wizard/
    │       ├── conditionEvaluator.test.ts      # [NEW] Unit tests for condition logic
    │       ├── QuestionRenderer.test.tsx       # [NEW] Component tests
    │       ├── ConditionalQuestionContainer.test.tsx # [NEW] Component tests
    │       ├── QuestionTooltip.test.tsx        # [NEW] Accessibility tests
    │       └── conditional-flow.e2e.test.tsx   # [NEW] E2E conditional flow tests
    │
    └── integration/
        └── wizard-integration.test.tsx         # [EXISTING] Wizard flow tests (extend)
```

**Structure Decision**: This is a frontend-only feature within the existing web application. All new code goes into `frontend/src/features/wizard/` alongside existing wizard components. No backend changes required (question definitions API already supports `helpText` and `conditions` properties).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations found. All Constitution principles are satisfied.
