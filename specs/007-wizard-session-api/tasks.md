---
description: "Task list for Eligibility Wizard Session API"
---

# Tasks: Eligibility Wizard Session API

**Input**: Design documents from /specs/007-wizard-session-api/
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Included (spec mandates test scenarios and test-first development).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: [ID] [P?] [Story] Description

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create wizard feature folder structure in src/MAA.Domain/Wizard/, src/MAA.Application/Wizard/, src/MAA.Infrastructure/Wizard/, src/MAA.Tests/Wizard/
- [X] T002 Add wizard feature namespace marker file in src/MAA.Domain/Wizard/WizardNamespace.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Add wizard enums and status types in src/MAA.Domain/Wizard/WizardEnums.cs
- [X] T004 [P] Add wizard repository interfaces in src/MAA.Application/Wizard/Repositories/IWizardSessionRepository.cs and src/MAA.Application/Wizard/Repositories/IStepAnswerRepository.cs
- [X] T005 [P] Add shared wizard DTO base types in src/MAA.Application/Wizard/DTOs/WizardDtos.cs
- [X] T006 Update DbContext to register wizard entities in src/MAA.Infrastructure/Data/SessionContext.cs
- [X] T007 Add concurrency exception mapping middleware in src/MAA.API/Middleware/ConcurrencyExceptionMiddleware.cs
- [X] T008 Wire wizard services and middleware in src/MAA.API/Program.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Save Progress and Resume Later (Priority: P1) MVP

**Goal**: Persist per-step answers and restore full wizard session state by sessionId.

**Independent Test**: Submit answers for 3 steps, close session, reopen with same sessionId, verify answers restored and wizard resumes at step 4.

### Tests for User Story 1

- [X] T009 [P] [US1] Contract tests for save/restore endpoints in src/MAA.Tests/Contracts/WizardSessionContractTests.cs
- [X] T010 [P] [US1] Integration test for save and resume flow in src/MAA.Tests/Integration/Wizard/WizardSessionResumeTests.cs
- [X] T011 [P] [US1] Unit tests for step answer validation in src/MAA.Tests/Unit/Wizard/StepAnswerValidatorTests.cs

### Implementation for User Story 1

- [X] T012 [P] [US1] Create WizardSession domain model in src/MAA.Domain/Wizard/WizardSession.cs
- [X] T013 [P] [US1] Create StepAnswer domain model in src/MAA.Domain/Wizard/StepAnswer.cs
- [X] T014 [P] [US1] Create StepProgress domain model in src/MAA.Domain/Wizard/StepProgress.cs
- [X] T015 [US1] Add EF Core config for WizardSession in src/MAA.Infrastructure/Wizard/WizardSessionConfiguration.cs
- [X] T016 [US1] Add EF Core config for StepAnswer in src/MAA.Infrastructure/Wizard/StepAnswerConfiguration.cs
- [X] T017 [US1] Add EF Core config for StepProgress in src/MAA.Infrastructure/Wizard/StepProgressConfiguration.cs
- [X] T018 [US1] Implement wizard repositories in src/MAA.Infrastructure/Wizard/WizardSessionRepository.cs and src/MAA.Infrastructure/Wizard/StepAnswerRepository.cs
- [X] T019 [US1] Add migration for wizard tables in src/MAA.Infrastructure/Migrations/
- [X] T020 [US1] Add SaveStepAnswer command and handler in src/MAA.Application/Wizard/Commands/SaveStepAnswerCommand.cs and src/MAA.Application/Wizard/Commands/SaveStepAnswerHandler.cs
- [X] T021 [US1] Add GetWizardSessionState query and handler in src/MAA.Application/Wizard/Queries/GetWizardSessionStateQuery.cs and src/MAA.Application/Wizard/Queries/GetWizardSessionStateHandler.cs
- [X] T022 [US1] Add GetStepAnswers query and handler in src/MAA.Application/Wizard/Queries/GetStepAnswersQuery.cs and src/MAA.Application/Wizard/Queries/GetStepAnswersHandler.cs
- [X] T023 [US1] Add request/response DTOs in src/MAA.Application/Wizard/DTOs/SaveStepAnswerDtos.cs and src/MAA.Application/Wizard/DTOs/WizardSessionStateDtos.cs
- [X] T024 [US1] Add validators for wizard requests in src/MAA.Application/Wizard/Validators/SaveStepAnswerRequestValidator.cs and src/MAA.Application/Wizard/Validators/GetWizardSessionStateValidator.cs
- [X] T025 [US1] Implement save/restore endpoints in src/MAA.API/Controllers/WizardSessionController.cs

**Checkpoint**: User Story 1 is fully functional and testable independently

---

## Phase 4: User Story 2 - Dynamic Step Navigation Based on Answers (Priority: P2)

**Goal**: Calculate and return the next step definition based on previous answers and navigation rules.

**Independent Test**: Answer Yes/No to household size > 1 and verify next step changes accordingly.

### Tests for User Story 2

- [X] T026 [P] [US2] Unit tests for navigation engine in src/MAA.Tests/Unit/Wizard/StepNavigationEngineTests.cs
- [X] T027 [P] [US2] Contract test for next-step endpoint in src/MAA.Tests/Contracts/WizardNextStepContractTests.cs
- [X] T028 [P] [US2] Integration test for dynamic navigation in src/MAA.Tests/Integration/Wizard/WizardNextStepTests.cs

### Implementation for User Story 2

- [X] T029 [P] [US2] Add step definition models in src/MAA.Domain/Wizard/StepDefinition.cs and src/MAA.Domain/Wizard/StepNavigationRule.cs
- [X] T030 [P] [US2] Implement IStepDefinitionProvider with in-memory definitions in src/MAA.Domain/Wizard/StepDefinitionProvider.cs and src/MAA.Domain/Wizard/Definitions/eligibility-steps.json
- [X] T031 [US2] Implement StepNavigationEngine with JsonLogic evaluation in src/MAA.Domain/Wizard/StepNavigationEngine.cs
- [X] T032 [US2] Add GetNextStep query and handler in src/MAA.Application/Wizard/Queries/GetNextStepQuery.cs and src/MAA.Application/Wizard/Queries/GetNextStepHandler.cs
- [X] T033 [US2] Add step definition DTOs in src/MAA.Application/Wizard/DTOs/StepDefinitionDtos.cs
- [X] T034 [US2] Implement next-step endpoint in src/MAA.API/Controllers/WizardSessionController.cs

**Checkpoint**: User Story 2 is independently functional and testable

---

## Phase 5: User Story 3 - Review and Modify Previous Steps (Priority: P3)

**Goal**: Allow users to review prior steps, edit answers, and invalidate dependent steps.

**Independent Test**: Complete steps 1-5, edit step 2, verify step 3 is marked requires_revalidation and prompted next.

### Tests for User Story 3

- [X] T035 [P] [US3] Unit tests for downstream invalidation rules in src/MAA.Tests/Unit/Wizard/StepInvalidationTests.cs
- [X] T036 [P] [US3] Contract test for step detail endpoint in src/MAA.Tests/Contracts/WizardStepDetailContractTests.cs
- [X] T037 [P] [US3] Integration test for edit and revalidate flow in src/MAA.Tests/Integration/Wizard/WizardStepReviewTests.cs

### Implementation for User Story 3

- [X] T038 [US3] Implement downstream invalidation in src/MAA.Application/Wizard/Commands/SaveStepAnswerHandler.cs
- [X] T039 [US3] Add GetStepDetail query and handler in src/MAA.Application/Wizard/Queries/GetStepDetailQuery.cs and src/MAA.Application/Wizard/Queries/GetStepDetailHandler.cs
- [X] T040 [US3] Add step detail DTOs in src/MAA.Application/Wizard/DTOs/StepDetailDtos.cs
- [X] T041 [US3] Implement step detail endpoint in src/MAA.API/Controllers/WizardSessionController.cs
- [X] T042 [US3] Extend repositories for invalidation and step detail lookups in src/MAA.Infrastructure/Wizard/WizardSessionRepository.cs

**Checkpoint**: All user stories are independently functional and testable

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T043 [P] Update feature catalog and roadmap docs in docs/FEATURE_CATALOG.md and docs/ROADMAP_QUICK_REFERENCE.md
- [X] T044 Run quickstart validation checklist in specs/007-wizard-session-api/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (Phase 1): No dependencies - can start immediately
- Foundational (Phase 2): Depends on Setup completion - BLOCKS all user stories
- User Stories (Phase 3+): Depend on Foundational completion; proceed in priority order P1 -> P2 -> P3
- Polish (Final Phase): Depends on desired user stories being complete

### User Story Dependencies

- User Story 1 (P1): Starts after Foundational; no dependencies on other stories
- User Story 2 (P2): Starts after Foundational; depends on US1 persistence and validation components
- User Story 3 (P3): Starts after Foundational; depends on US1 save/update flow

### Dependency Graph

- Setup -> Foundational -> US1 -> US2 -> US3 -> Polish

---

## Parallel Execution Examples

### User Story 1

- T009 and T010 and T011 can run in parallel (different test files)
- T012 and T013 and T014 can run in parallel (separate domain models)

### User Story 2

- T026 and T027 and T028 can run in parallel (different test files)
- T029 and T030 can run in parallel (separate domain/config files)

### User Story 3

- T035 and T036 and T037 can run in parallel (different test files)

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Stop and validate User Story 1 independently

### Incremental Delivery

1. Setup + Foundational -> Foundation ready
2. Add User Story 1 -> Test independently -> Deploy/Demo
3. Add User Story 2 -> Test independently -> Deploy/Demo
4. Add User Story 3 -> Test independently -> Deploy/Demo
5. Polish

### Parallel Team Strategy

1. Team completes Setup + Foundational together
2. After Foundational:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
