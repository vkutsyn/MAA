# Tasks: Eligibility Evaluation Engine

**Input**: Design documents from `/specs/010-eligibility-evaluation-engine/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Required by Constitution II and feature spec; include unit, integration, and contract tests.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create eligibility feature folders in src/MAA.Domain/Eligibility/, src/MAA.Application/Eligibility/, src/MAA.Infrastructure/Eligibility/, src/MAA.API/Controllers/
- [x] T002 Create eligibility test folders in src/MAA.Tests/Unit/Eligibility/, src/MAA.Tests/Integration/Eligibility/, src/MAA.Tests/Contract/Eligibility/
- [x] T003 [P] Add eligibility API contract reference note in src/MAA.API/Controllers/EligibilityController.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

- [x] T004 [P] Create core domain models (RuleSetVersion, EligibilityRule, ProgramDefinition, FederalPovertyLevel) in src/MAA.Domain/Eligibility/
- [x] T005 [P] Create evaluation models (EligibilityRequest, EligibilityResult, ProgramMatch) in src/MAA.Domain/Eligibility/
- [x] T006 [P] Create repository interfaces (IRuleSetRepository, IFederalPovertyLevelRepository) in src/MAA.Domain/Eligibility/
- [x] T007 [P] Add EF Core configurations for eligibility entities in src/MAA.Infrastructure/Eligibility/
- [x] T008 Add DbSet registrations for eligibility entities in src/MAA.Infrastructure/SessionContext.cs
- [x] T009 Implement repositories in src/MAA.Infrastructure/Eligibility/RuleSetRepository.cs and src/MAA.Infrastructure/Eligibility/FederalPovertyLevelRepository.cs
- [x] T010 Add rule and FPL cache service in src/MAA.Application/Eligibility/RuleCacheService.cs
- [x] T011 Register eligibility services and repositories in src/MAA.API/Program.cs
- [x] T012 Create EF Core migration for eligibility entities in src/MAA.Infrastructure/Migrations/*_AddEligibilityRules.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Evaluate Eligibility For An Applicant (Priority: P1) 

**Goal**: Accept inputs and return status, matched programs, confidence score, and explanation deterministically.

**Independent Test**: Submit a fixed input set against a known rule set and verify the returned status, programs, confidence, and explanation.

### Tests for User Story 1 (Test-First)

- [x] T013 [P] [US1] Contract test for POST /eligibility/evaluate in src/MAA.Tests/Contract/Eligibility/EligibilityEvaluationContractTests.cs
- [x] T014 [P] [US1] Unit tests for EligibilityEvaluator baseline in src/MAA.Tests/Unit/Eligibility/EligibilityEvaluatorTests.cs
- [x] T015 [P] [US1] Unit tests for ConfidenceScoringPolicy baseline in src/MAA.Tests/Unit/Eligibility/ConfidenceScoringPolicyTests.cs
- [x] T016 [P] [US1] Integration test for evaluation endpoint in src/MAA.Tests/Integration/Eligibility/EligibilityEvaluationApiTests.cs

### Implementation for User Story 1

- [x] T017 [US1] Implement EligibilityEvaluator using JSONLogic in src/MAA.Domain/Eligibility/EligibilityEvaluator.cs
- [x] T018 [US1] Implement ConfidenceScoringPolicy in src/MAA.Domain/Eligibility/ConfidenceScoringPolicy.cs
- [x] T019 [US1] Add eligibility request and response DTOs in src/MAA.Application/Eligibility/Dtos/EligibilityEvaluateRequestDto.cs and src/MAA.Application/Eligibility/Dtos/EligibilityEvaluateResponseDto.cs
- [x] T020 [US1] Implement EvaluateEligibilityQuery handler in src/MAA.Application/Eligibility/Queries/EvaluateEligibilityQuery.cs
- [x] T021 [US1] Implement eligibility endpoint in src/MAA.API/Controllers/EligibilityController.cs
- [x] T022 [US1] Add validation and error mapping for invalid inputs in src/MAA.API/Controllers/EligibilityController.cs

**Checkpoint**: User Story 1 functional and testable independently

---

## Phase 4: User Story 2 - Evaluate Using Correct Rule Version (Priority: P2)

**Goal**: Apply the correct rule version for the evaluation effective date.

**Independent Test**: Use two rule versions with different effective dates and verify selection by date.

### Tests for User Story 2 (Test-First)

- [x] T023 [P] [US2] Unit tests for rule version selection in src/MAA.Tests/Unit/Eligibility/RuleSetVersionSelectorTests.cs
- [x] T024 [P] [US2] Integration test for effective-date selection in src/MAA.Tests/Integration/Eligibility/RuleVersionSelectionTests.cs

### Implementation for User Story 2

- [x] T025 [US2] Implement RuleSetVersionSelector in src/MAA.Domain/Eligibility/RuleSetVersionSelector.cs
- [x] T026 [US2] Update rule repository selection by effective date in src/MAA.Infrastructure/Eligibility/RuleSetRepository.cs
- [x] T027 [US2] Wire version selection into EligibilityEvaluator in src/MAA.Domain/Eligibility/EligibilityEvaluator.cs
- [x] T028 [US2] Map ruleVersionUsed in response in src/MAA.Application/Eligibility/Queries/EvaluateEligibilityQuery.cs

**Checkpoint**: User Story 2 functional and testable independently

---

## Phase 5: User Story 3 - Understand The Determination (Priority: P3)

**Goal**: Provide detailed explanations with met, unmet, and missing criteria.

**Independent Test**: Provide inputs that trigger met and unmet criteria and confirm explanation details.

### Tests for User Story 3 (Test-First)

- [x] T029 [P] [US3] Unit tests for ExplanationBuilder in src/MAA.Tests/Unit/Eligibility/ExplanationBuilderTests.cs
- [x] T030 [P] [US3] Integration test for explanation content in src/MAA.Tests/Integration/Eligibility/EligibilityExplanationTests.cs

### Implementation for User Story 3

- [x] T031 [US3] Add ExplanationItem model in src/MAA.Domain/Eligibility/ExplanationItem.cs
- [x] T032 [US3] Implement ExplanationBuilder with templates and glossary in src/MAA.Domain/Eligibility/ExplanationBuilder.cs
- [x] T033 [US3] Add readability and jargon checks in src/MAA.Domain/Eligibility/ExplanationReadability.cs
- [x] T034 [US3] Map explanation items to response in src/MAA.Application/Eligibility/Queries/EvaluateEligibilityQuery.cs
- [x] T035 [US3] Update response DTO to include explanation items in src/MAA.Application/Eligibility/Dtos/EligibilityEvaluateResponseDto.cs

**Checkpoint**: User Story 3 functional and testable independently

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T036 [P] Add performance timing logging for evaluation requests in src/MAA.API/Middleware/PerformanceLoggingMiddleware.cs
- [x] T037 [P] Update feature documentation status in docs/FEATURE_CATALOG.md
- [x] T038 Run quickstart validation checklist in specs/010-eligibility-evaluation-engine/VALIDATION_CHECKLIST.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories
- **User Stories (Phase 3+)**: Depend on Foundational phase completion
- **Polish (Final Phase)**: Depends on completion of desired user stories

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Foundational phase only
- **User Story 2 (P2)**: Depends on User Story 1 (rule selection extends evaluation)
- **User Story 3 (P3)**: Depends on User Story 1 (explanations extend evaluation)

### Dependency Graph

- Setup -> Foundational -> US1 -> {US2, US3} -> Polish

---

## Parallel Execution Examples

### User Story 1

- T013, T014, T015, T016 can run in parallel (tests)
- T017 and T018 can run in parallel (different files)

### User Story 2

- T023 and T024 can run in parallel (tests)

### User Story 3

- T029 and T030 can run in parallel (tests)
- T031 and T032 can run in parallel (different files)

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate User Story 1 independently (contract, unit, integration tests)

### Incremental Delivery

1. Add User Story 2 and validate effective-date selection
2. Add User Story 3 and validate explanation detail
3. Complete polish tasks and performance checks
