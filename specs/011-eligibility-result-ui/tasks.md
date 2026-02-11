# Tasks: Eligibility Result UI

**Input**: Design documents from `/specs/011-eligibility-result-ui/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Not requested in spec; no test tasks included.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create results feature barrel export in frontend/src/features/results/index.ts

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

- [x] T002 Define eligibility result DTO and view model types in frontend/src/features/results/types.ts
- [x] T003 Implement wizard answer to eligibility input mapper in frontend/src/features/results/eligibilityInputMapper.ts
- [x] T004 Implement eligibility results API client in frontend/src/features/results/eligibilityResultApi.ts
- [x] T005 Implement React Query hook for eligibility results in frontend/src/features/results/useEligibilityResult.ts

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - View eligibility status (Priority: P1) 🎯 MVP

**Goal**: Display the overall eligibility status prominently with clear loading, empty, and error states.

**Independent Test**: Load the results route with a mocked or live eligibility response and confirm the status summary renders correctly and fallback states appear when data is missing.

### Implementation for User Story 1

- [x] T006 [P] [US1] Build eligibility status card component in frontend/src/features/results/components/EligibilityStatusCard.tsx
- [x] T007 [US1] Build results page layout and loading/error/fallback states in frontend/src/features/results/EligibilityResultPage.tsx
- [x] T008 [US1] Create results route wrapper in frontend/src/routes/EligibilityResultRoute.tsx
- [x] T009 [US1] Register results route path in frontend/src/routes/index.tsx
- [x] T010 [US1] Navigate to results on wizard completion in frontend/src/features/wizard/WizardPage.tsx

**Checkpoint**: User Story 1 is fully functional and testable independently

---

## Phase 4: User Story 2 - Review program matches (Priority: P2)

**Goal**: Display matched programs with names and summaries, including a zero-matches state.

**Independent Test**: Provide a result with multiple matches and one with no matches; verify list ordering and zero-matches message.

### Implementation for User Story 2

- [x] T011 [P] [US2] Build program matches list component in frontend/src/features/results/components/ProgramMatchesCard.tsx
- [x] T012 [US2] Integrate program matches section into frontend/src/features/results/EligibilityResultPage.tsx

**Checkpoint**: User Stories 1 and 2 are independently functional

---

## Phase 5: User Story 3 - Understand the outcome (Priority: P3)

**Goal**: Display explanation bullets and a confidence indicator with plain-language labels.

**Independent Test**: Provide explanation bullets and varying confidence scores; verify label mapping and list rendering.

### Implementation for User Story 3

- [x] T013 [P] [US3] Build confidence indicator component in frontend/src/features/results/components/ConfidenceIndicator.tsx
- [x] T014 [P] [US3] Build explanation bullets component in frontend/src/features/results/components/ExplanationList.tsx
- [x] T015 [US3] Integrate confidence and explanation sections into frontend/src/features/results/EligibilityResultPage.tsx

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T016 [P] Update quickstart with results route details in specs/011-eligibility-result-ui/quickstart.md
- [x] T017 [P] Update feature catalog with Eligibility Result UI entry in docs/FEATURE_CATALOG.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - no dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - integrates with US1 layout
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - integrates with US1 layout

### Parallel Opportunities

- T002, T003, T004, T005 can run in parallel once Phase 1 completes.
- T006 can run in parallel with T007 only if using a placeholder component import.
- T011 and T013/T014 can run in parallel (separate components).

---

## Parallel Example: User Story 1

```bash
Task: "Build eligibility status card component in frontend/src/features/results/components/EligibilityStatusCard.tsx"
Task: "Build results page layout and loading/error/fallback states in frontend/src/features/results/EligibilityResultPage.tsx"
```

## Parallel Example: User Story 2

```bash
Task: "Build program matches list component in frontend/src/features/results/components/ProgramMatchesCard.tsx"
```

## Parallel Example: User Story 3

```bash
Task: "Build confidence indicator component in frontend/src/features/results/components/ConfidenceIndicator.tsx"
Task: "Build explanation bullets component in frontend/src/features/results/components/ExplanationList.tsx"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate User Story 1 independently

### Incremental Delivery

1. Setup + Foundational
2. User Story 1 (status) → validate
3. User Story 2 (program matches) → validate
4. User Story 3 (explanations + confidence) → validate
5. Polish updates
