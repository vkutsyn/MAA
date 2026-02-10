---
description: "Task list for Eligibility Wizard UI implementation"
---

# Tasks: Eligibility Wizard UI

**Input**: Design documents from `/specs/004-ui-implementation/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Not included (no explicit TDD/test task request in spec).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create Vite React TypeScript app in frontend/ (frontend/package.json, frontend/vite.config.ts, frontend/tsconfig.json, frontend/src/main.tsx, frontend/src/App.tsx)
- [X] T002 [P] Configure Tailwind + shadcn/ui base styles in frontend/tailwind.config.ts, frontend/postcss.config.cjs, frontend/src/index.css
- [X] T003 [P] Add router scaffold in frontend/src/routes/index.tsx and wire it in frontend/src/main.tsx

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**Constitution Alignment**: Supports testability, accessibility, and performance constraints via shared services and typed contracts.

- [X] T004 Update anonymous session creation to allow unauthenticated access and set `MAA_SessionId` cookie in src/MAA.API/Controllers/SessionsController.cs
- [X] T005 [P] Add state metadata DTOs in src/MAA.Application/Eligibility/DTOs/StateInfoDto.cs and src/MAA.Application/Eligibility/DTOs/StateLookupDto.cs
- [X] T006 [P] Add question taxonomy DTOs in src/MAA.Application/Eligibility/DTOs/QuestionDto.cs and src/MAA.Application/Eligibility/DTOs/QuestionSetDto.cs
- [X] T007 Add state metadata service in src/MAA.Application/Eligibility/Services/StateMetadataService.cs
- [X] T008 Add question taxonomy service in src/MAA.Application/Eligibility/Services/QuestionTaxonomyService.cs
- [X] T009 Add states endpoints controller in src/MAA.API/Controllers/StatesController.cs
- [X] T010 Add questions endpoint controller in src/MAA.API/Controllers/QuestionsController.cs
- [ ] T011 [P] Add frontend API client with credentials in frontend/src/lib/api.ts
- [ ] T008 Add question taxonomy service in src/MAA.Application/Eligibility/Services/QuestionTaxonomyService.cs
- [ ] T009 Add states endpoints controller in src/MAA.API/Controllers/StatesController.cs
- [ ] T010 Add questions endpoint controller in src/MAA.API/Controllers/QuestionsController.cs
- [ ] T011 [P] Add frontend API client with credentials in frontend/src/lib/api.ts
- [ ] T012 [P] Add wizard types and DTO mapping in frontend/src/features/wizard/types.ts
- [ ] T013 Add wizard state store in frontend/src/features/wizard/store.ts
- [ ] T014 Add session bootstrap hook in frontend/src/features/wizard/useSession.ts

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Start Eligibility Check (Priority: P1) 🎯 MVP

**Goal**: Deliver a landing page with state selection that starts the wizard.

**Independent Test**: Open the UI, select a state, click Start, and see the first wizard question with initialized progress.

### Implementation for User Story 1

- [ ] T015 [US1] Build landing page layout in frontend/src/features/wizard/LandingPage.tsx
- [ ] T016 [US1] Wire landing route in frontend/src/routes/WizardLandingRoute.tsx
- [ ] T017 [US1] Implement state selector UI in frontend/src/features/wizard/StateSelector.tsx
- [ ] T018 [US1] Connect state list/lookup APIs in frontend/src/features/wizard/stateApi.ts
- [ ] T019 [US1] Implement Start action (create session + set state) in frontend/src/features/wizard/useStartWizard.ts
- [ ] T020 [US1] Render first question and progress initialization in frontend/src/features/wizard/WizardStep.tsx and frontend/src/features/wizard/WizardProgress.tsx

**Checkpoint**: User Story 1 should be functional and testable independently

---

## Phase 4: User Story 2 - Complete a Multi-Step Flow (Priority: P2)

**Goal**: Enable multi-step navigation with answer persistence and refresh restore.

**Independent Test**: Answer a question, navigate forward/back, refresh, and confirm answers and step restore.

### Implementation for User Story 2

- [ ] T021 [US2] Implement next/back navigation in frontend/src/features/wizard/WizardNavigator.tsx
- [ ] T022 [US2] Persist answers on step advance in frontend/src/features/wizard/answerApi.ts
- [ ] T023 [US2] Restore answers and last step on refresh in frontend/src/features/wizard/useResumeWizard.ts
- [ ] T024 [US2] Implement conditional flow evaluation in frontend/src/features/wizard/flow.ts

**Checkpoint**: User Stories 1 and 2 should both work independently

---

## Phase 5: User Story 3 - Accessible and Mobile-Friendly Experience (Priority: P3)

**Goal**: Ensure keyboard navigation, screen reader support, and mobile usability.

**Independent Test**: Complete the flow using keyboard only and verify layout on a 375px viewport without horizontal scroll.

### Implementation for User Story 3

- [ ] T025 [US3] Add keyboard navigation and focus management in frontend/src/features/wizard/a11y.ts
- [ ] T026 [US3] Add semantic labels and ARIA attributes in frontend/src/features/wizard/LandingPage.tsx, frontend/src/features/wizard/StateSelector.tsx, frontend/src/features/wizard/WizardStep.tsx
- [ ] T027 [US3] Ensure mobile responsive layout and touch targets in frontend/src/features/wizard/WizardLayout.tsx and frontend/src/index.css
- [ ] T028 [US3] Add inline help and validation messaging components in frontend/src/features/wizard/HelpText.tsx and frontend/src/features/wizard/ValidationMessage.tsx

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T029 [P] Add step transition timing helper in frontend/src/features/wizard/perf.ts
- [ ] T030 [P] Update docs/FEATURE_CATALOG.md to link the E4 spec at specs/004-ui-implementation/spec.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: Depend on Foundational completion; can proceed in parallel after that
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2)
- **User Story 2 (P2)**: Can start after Foundational (Phase 2), integrates with US1 UI state
- **User Story 3 (P3)**: Can start after Foundational (Phase 2), applies to existing UI components

### Parallel Opportunities

- Setup tasks marked [P] can run in parallel
- Foundational tasks marked [P] can run in parallel
- After Foundational, different user stories can be worked in parallel by different team members

---

## Parallel Example: User Story 1

```bash
Task: "Implement state selector UI in frontend/src/features/wizard/StateSelector.tsx"
Task: "Connect state list/lookup APIs in frontend/src/features/wizard/stateApi.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL)
3. Complete Phase 3: User Story 1
4. Validate User Story 1 independently

### Incremental Delivery

1. Setup + Foundational
2. User Story 1 -> Validate
3. User Story 2 -> Validate
4. User Story 3 -> Validate
5. Polish & cross-cutting tasks
