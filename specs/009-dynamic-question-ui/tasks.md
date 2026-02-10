# Tasks: Dynamic Eligibility Question UI

**Input**: Design documents from `/specs/009-dynamic-question-ui/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Feature**: Dynamic question rendering with conditional logic and "Why we ask this" tooltips

**Tests**: Tests are REQUIRED per Constitution II (Test-First Development) and explicitly requested in spec.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Web application structure:

- Frontend: `frontend/src/`, `frontend/tests/`
- Backend: No changes (API already supports required fields)

---

## Phase 1: Setup (Minimal - Existing Infrastructure)

**Purpose**: Install required UI dependencies for tooltips

- [x] T001 Install shadcn/ui Tooltip component via `npx shadcn-ui@latest add tooltip` in frontend directory

---

## Phase 2: Foundational (TypeScript Types Extension)

**Purpose**: Extend existing types to support conditional rendering state

**⚠️ CRITICAL**: Must be complete before user story implementation begins

**Constitution Alignment**:

- Constitution I: Type safety, pure data structures
- Constitution II: Test types available for all tests

- [x] T002 [P] Add AnswerMap type definition to frontend/src/features/wizard/conditionEvaluator.ts
- [x] T003 [P] Add VisibilityState interface to frontend/src/features/wizard/conditionEvaluator.ts
- [x] T004 Extend WizardStore interface in frontend/src/features/wizard/store.ts to include answerMap and visibilityState

**Checkpoint**: Types ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - View and Answer Basic Questions (Priority: P1) 🎯 MVP

**Goal**: Ensure existing wizard properly renders questions dynamically from API with appropriate input controls

**Independent Test**: Load wizard page, verify questions render from API, enter answers, confirm captured correctly

**Note**: Much of this is already implemented. Tasks focus on validation and enhancement for dynamic rendering.

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T005 [P] [US1] Unit test for question type rendering in frontend/tests/features/wizard/questionTypeRendering.test.tsx
- [x] T006 [P] [US1] Component test for answer persistence in frontend/tests/features/wizard/answerPersistence.test.tsx
- [x] T007 [P] [US1] E2E test for basic question flow in frontend/tests/features/wizard/basicQuestionFlow.e2e.test.tsx

### Implementation for User Story 1

- [x] T008 [P] [US1] Verify WizardStep.tsx handles all question types (text, number, date, select, multiselect, currency, integer, boolean, dropdown) in frontend/src/features/wizard/WizardStep.tsx
- [x] T009 [US1] Enhance WizardPage.tsx to ensure questions render in API-specified order in frontend/src/features/wizard/WizardPage.tsx
- [x] T010 [US1] Verify answer preservation on navigation in useWizardNavigator.ts in frontend/src/features/wizard/useWizardNavigator.ts
- [x] T011 [US1] Add validation for question rendering performance (<1 second for 50 questions) in frontend/src/features/wizard/perf.ts

**Checkpoint**: At this point, User Story 1 should be fully functional - questions render dynamically, answers persist, all input types supported

---

## Phase 4: User Story 2 - Conditional Questions Based on Previous Answers (Priority: P2)

**Goal**: Implement conditional visibility logic so questions appear/disappear based on user answers

**Independent Test**: Answer trigger question, verify dependent questions appear/disappear, confirm answers preserved when toggling

### Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T012 [P] [US2] Unit test for evaluateCondition function (all operators) in frontend/tests/features/wizard/conditionEvaluator.test.ts
- [x] T013 [P] [US2] Unit test for computeVisibility function in frontend/tests/features/wizard/conditionEvaluator.test.ts
- [x] T014 [P] [US2] Component test for ConditionalQuestionContainer visibility in frontend/tests/features/wizard/ConditionalQuestionContainer.test.tsx
- [x] T015 [P] [US2] E2E test for conditional question appearance in frontend/tests/features/wizard/conditionalAppearance.e2e.test.tsx
- [x] T016 [P] [US2] E2E test for conditional question answer preservation in frontend/tests/features/wizard/conditionalAnswerPersistence.e2e.test.tsx
- [x] T017 [P] [US2] Performance test for condition evaluation (<200ms) in frontend/tests/features/wizard/conditionPerformance.test.ts

### Implementation for User Story 2

- [x] T018 [P] [US2] Implement evaluateCondition function with all operators (equals, not_equals, gt, gte, lt, lte, includes) in frontend/src/features/wizard/conditionEvaluator.ts
- [x] T019 [P] [US2] Implement computeVisibility function in frontend/src/features/wizard/conditionEvaluator.ts
- [x] T020 [US2] Create ConditionalQuestionContainer component with aria-live support in frontend/src/features/wizard/ConditionalQuestionContainer.tsx
- [x] T021 [US2] Add answerMap state and updateAnswerMap action to Zustand store in frontend/src/features/wizard/store.ts
- [x] T022 [US2] Add recomputeVisibility action to Zustand store in frontend/src/features/wizard/store.ts
- [x] T023 [US2] Integrate ConditionalQuestionContainer into WizardPage with useMemo for visibility computation in frontend/src/features/wizard/WizardPage.tsx
- [x] T024 [US2] Add debouncing for answer changes to prevent flickering in frontend/src/features/wizard/WizardPage.tsx
- [x] T025 [US2] Implement focus management for appearing/disappearing questions in frontend/src/features/wizard/ConditionalQuestionContainer.tsx
- [x] T026 [US2] Add visual transition effects for conditional questions in frontend/src/features/wizard/ConditionalQuestionContainer.tsx

**Checkpoint**: At this point, User Stories 1 AND 2 should both work - conditional questions appear/disappear smoothly, answers preserved, <200ms evaluation

---

## Phase 5: User Story 3 - Contextual Help via Tooltips (Priority: P3)

**Goal**: Add "Why we ask this" help tooltips to questions with keyboard support and screen reader compatibility

**Independent Test**: Click/hover help icon, verify tooltip appears with explanation, test keyboard navigation and screen reader

### Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T027 [P] [US3] Component test for QuestionTooltip rendering in frontend/tests/features/wizard/QuestionTooltip.test.tsx
- [x] T028 [P] [US3] Accessibility test for tooltip keyboard navigation in frontend/tests/features/wizard/QuestionTooltip.test.tsx
- [x] T029 [P] [US3] Accessibility test for tooltip screen reader support in frontend/tests/features/wizard/QuestionTooltip.test.tsx
- [x] T030 [P] [US3] E2E test for tooltip interaction flow in frontend/tests/features/wizard/tooltipInteraction.e2e.test.tsx
- [x] T031 [P] [US3] Performance test for tooltip display (<100ms) in frontend/tests/features/wizard/tooltipPerformance.test.ts

### Implementation for User Story 3

- [x] T032 [P] [US3] Create QuestionTooltip component using shadcn/ui Tooltip primitives in frontend/src/features/wizard/QuestionTooltip.tsx
- [x] T033 [P] [US3] Add HelpCircle icon import from lucide-react to QuestionTooltip in frontend/src/features/wizard/QuestionTooltip.tsx
- [x] T034 [US3] Integrate QuestionTooltip into WizardStep component (conditional on helpText presence) in frontend/src/features/wizard/WizardStep.tsx
- [x] T035 [US3] Ensure tooltip trigger is keyboard accessible (Tab, Enter, Escape) in frontend/src/features/wizard/QuestionTooltip.tsx
- [x] T036 [US3] Add aria-label "Why we ask this" to tooltip trigger button in frontend/src/features/wizard/QuestionTooltip.tsx
- [x] T037 [US3] Verify color contrast meets WCAG 2.1 AA (4.5:1 ratio) in frontend/src/features/wizard/QuestionTooltip.tsx
- [x] T038 [US3] Add touch target sizing (≥44×44px) for mobile in frontend/src/features/wizard/QuestionTooltip.tsx

**Checkpoint**: All user stories should now be independently functional - tooltips work, keyboard accessible, screen reader compatible

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and final validation

- [x] T039 [P] Run axe DevTools accessibility audit on wizard page and fix violations (skipped per /speckit.close)
- [x] T040 [P] Run Lighthouse performance audit on wizard page and optimize if needed (skipped per /speckit.close)
- [x] T041 [P] Add TypeScript strict mode checks and fix any type errors
- [x] T042 [P] Update frontend README with conditional questions documentation in frontend/README.md
- [x] T043 [P] Add code comments for complex condition evaluation logic in conditionEvaluator.ts
- [x] T044 Create PR description using template from quickstart.md
- [x] T045 Validate all acceptance scenarios from spec.md manually (skipped per /speckit.close)
- [x] T046 Run full test suite (`npm test`) and ensure 80%+ coverage (skipped per /speckit.close)
- [x] T047 Verify Constitution compliance checklist from plan.md
- [x] T048 Run quickstart.md validation steps (skipped per /speckit.close)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup (T001) - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - User Story 1 (Phase 3) can start after Foundational
  - User Story 2 (Phase 4) can start after Foundational (independent of US1)
  - User Story 3 (Phase 5) can start after Foundational (independent of US1/US2)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1) - Phase 3**:
  - Depends on: Foundational (Phase 2)
  - Blocks: None (other stories are independent)
  - Can start: After T002-T004 complete
- **User Story 2 (P2) - Phase 4**:
  - Depends on: Foundational (Phase 2)
  - Integrates with: US1 (WizardPage), but independently testable
  - Can start: After T002-T004 complete (can run parallel with US1 if team capacity allows)
- **User Story 3 (P3) - Phase 5**:
  - Depends on: Foundational (Phase 2), shadcn/ui Tooltip (T001)
  - Integrates with: US1 (WizardStep), but independently testable
  - Can start: After T001-T004 complete (can run parallel with US1/US2 if team capacity allows)

### Within Each User Story Phase

**Critical TDD Order**:

1. Write tests FIRST (all test tasks marked [P] can run in parallel)
2. Run tests (expect failures)
3. Implement code (implementation tasks in sequence or marked [P] for parallel)
4. Run tests (expect passes)
5. Refactor while keeping tests green

**User Story 1 Implementation Order**:

- T005-T007 (tests) - ALL FIRST, can be done in parallel
- T008-T011 (implementation) - T008-T010 can be parallel, T011 after T008-T010

**User Story 2 Implementation Order**:

- T012-T017 (tests) - ALL FIRST, can be done in parallel
- T018-T019 (pure logic) - can be parallel
- T020-T022 (components/state) - can be parallel
- T023-T026 (integration) - must be sequential after T020-T022

**User Story 3 Implementation Order**:

- T027-T031 (tests) - ALL FIRST, can be done in parallel
- T032-T033 (component creation) - can be parallel
- T034-T038 (integration & polish) - sequential after T032-T033

### Parallel Opportunities by Role

**If you have 2 developers**:

- After Foundational (T002-T004):
  - Dev 1: User Story 2 (P2) - Conditional logic (highest complexity)
  - Dev 2: User Story 3 (P3) - Tooltips (medium complexity)
  - After both complete: Together on User Story 1 validation (P1)

**If you have 3 developers**:

- After Foundational (T002-T004):
  - Dev 1: User Story 1 (P1) - Validation tasks
  - Dev 2: User Story 2 (P2) - Conditional logic
  - Dev 3: User Story 3 (P3) - Tooltips

**If you have 1 developer**:

- Recommended order: Phase 2 → Phase 4 (US2) → Phase 5 (US3) → Phase 3 (US1 validation) → Phase 6
- Rationale: Implement high-value features (conditional logic, tooltips) before validation tasks

### Task Dependencies Graph

```
T001 (shadcn install)
  │
  ├─→ T002,T003,T004 (Foundational types) ─────┐
  │                                              │
  └─────────────────────────────────────────────┤
                                                 │
  ┌──────────────────────────────────────────────┘
  │
  ├─→ Phase 3 (US1): T005-T007 → T008-T011
  │
  ├─→ Phase 4 (US2): T012-T017 → [T018,T019,T020,T021,T022] → T023-T026
  │
  └─→ Phase 5 (US3): T027-T031 → [T032,T033] → T034-T038
        │
        └─→ When all US complete → Phase 6: T039-T048 (can be parallel except T044,T047,T048)
```

### Estimated Completion Times (1 developer, normal pace)

- **Phase 1 (Setup)**: 5 minutes
- **Phase 2 (Foundational)**: 30 minutes
- **Phase 3 (US1)**: 2 hours (mostly validation of existing code + tests)
- **Phase 4 (US2)**: 6 hours (pure logic + component + integration + tests)
- **Phase 5 (US3)**: 3 hours (component + integration + accessibility tests)
- **Phase 6 (Polish)**: 2 hours (audits + documentation + validation)

**Total**: ~14 hours (1-2 days for single developer)

**MVP** (Just US2 + US3): ~10 hours

---

## Implementation Strategy

### Recommended Approach: TDD + Incremental Delivery

1. **Day 1 Morning**:
   - Complete Phase 1-2 (Setup + Foundational)
   - Start Phase 4 (US2) tests (T012-T017)
2. **Day 1 Afternoon**:
   - Complete Phase 4 (US2) implementation (T018-T026)
   - Test conditional questions work end-to-end
3. **Day 2 Morning**:
   - Complete Phase 5 (US3) tests (T027-T031)
   - Complete Phase 5 (US3) implementation (T032-T038)
   - Test tooltips work with conditional questions
4. **Day 2 Afternoon**:
   - Complete Phase 3 (US1) validation (T005-T011)
   - Complete Phase 6 (Polish) (T039-T048)
   - Submit PR

### Alternative: Feature Flags (for continuous deployment)

If deploying incrementally to production:

1. **Release 1**: Phase 2 (types only) - no user-facing changes
2. **Release 2**: Phase 3 (US1 validation) - ensure existing flow works
3. **Release 3**: Phase 4 (US2) behind feature flag - conditional questions
4. **Release 4**: Phase 5 (US3) - tooltips
5. **Release 5**: Phase 6 (polish) + remove feature flags

---

## Validation Checklist

Before marking this feature complete, ensure:

- [ ] All 48 tasks completed
- [ ] All tests passing (`npm test` shows green)
- [ ] Code coverage ≥80% for new files (conditionEvaluator, ConditionalQuestionContainer, QuestionTooltip)
- [ ] All 15 acceptance scenarios from spec.md pass manual testing
- [ ] All 7 edge cases from spec.md handled correctly
- [ ] All 12 functional requirements (FR-001 through FR-012) satisfied
- [ ] All 4 Constitution compliance requirements (CONST-I through CONST-IV) verified
- [ ] All 8 success criteria (SC-001 through SC-008) met and measured
- [ ] Accessibility audit (axe DevTools) shows 0 violations
- [ ] Performance targets met (<1s rendering, <200ms evaluation, <100ms tooltip)
- [ ] Keyboard navigation works (Tab, Enter, Escape, Space)
- [ ] Screen reader announces conditional questions (tested with NVDA or JAWS)
- [ ] Mobile responsive (tested at 375px, 768px, 1920px)
- [ ] PR description complete per quickstart.md template
- [ ] Documentation updated (README, code comments)

---

## Notes

- **Tests First**: Constitution II requires test-first development. Write all test tasks in a phase before implementation tasks.
- **No Backend Changes**: This is frontend-only. Backend API already supports `helpText` and `conditions` properties in QuestionDto.
- **Accessibility Critical**: WCAG 2.1 AA compliance is mandatory (Constitution III). Use axe DevTools and manual screen reader testing.
- **Performance SLOs**: <1s rendering, <200ms evaluation, <100ms tooltips per Constitution IV. Monitor with Chrome DevTools Performance profiler.
- **Parallel Opportunities**: Within each phase, tasks marked [P] can run in parallel. User stories can be worked on in parallel by different developers after Foundational phase.
- **MVP Scope**: Minimum viable product is US2 (conditional questions) + US3 (tooltips). US1 is validation of existing functionality.

---

**Total Tasks**: 48 (Setup: 1, Foundational: 3, US1: 7, US2: 15, US3: 12, Polish: 10)

**Parallelizable Tasks**: 29 tasks marked [P] (60% can run in parallel with proper team coordination)

**Estimated Effort**: 14 hours (single developer) | 8 hours (2 developers) | 5 hours (3 developers)
