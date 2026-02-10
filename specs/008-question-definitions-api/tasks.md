---
description: "Task list for Eligibility Question Definitions API"
---

# Tasks: Eligibility Question Definitions API

**Input**: Design documents from /specs/008-question-definitions-api/
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/questions-api.openapi.yaml

**Tests**: Included (spec requires unit + integration coverage per CONST-II).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project configuration and feature wiring

- [X] T001 Add question definitions cache settings in src/MAA.API/appsettings.json and src/MAA.API/appsettings.Development.json
- [X] T002 Add DI registrations for question definition services in src/MAA.API/Program.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before any user story work begins

- [X] T003 [P] Add Question, ConditionalRule, QuestionOption, and QuestionFieldType in src/MAA.Domain/Question.cs, src/MAA.Domain/ConditionalRule.cs, and src/MAA.Domain/QuestionOption.cs
- [X] T004 Add DbSets and model configuration wiring in src/MAA.Infrastructure/Data/SessionContext.cs
- [X] T005 Add EF Core migration for question tables in src/MAA.Infrastructure/Migrations/*_AddQuestionDefinitions.cs
- [X] T006 Add IQuestionRepository interface in src/MAA.Application/Interfaces/IQuestionRepository.cs
- [X] T007 Add QuestionRepository implementation in src/MAA.Infrastructure/Repositories/QuestionRepository.cs
- [X] T008 Add Redis-backed cache service for question definitions in src/MAA.Infrastructure/Caching/QuestionDefinitionsCache.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Retrieve Questions by State and Program (Priority: P1) MVP

**Goal**: Provide a state/program questions endpoint that returns ordered question definitions for rendering.

**Independent Test**: Call GET /api/questions/{stateCode}/{programCode} with valid/invalid inputs and verify response status + question list ordering.

### Tests for User Story 1

- [X] T009 [P] [US1] Add contract tests for GET /api/questions/{stateCode}/{programCode} in src/MAA.Tests/Contract/QuestionsApiContractTests.cs
- [X] T010 [P] [US1] Add repository integration tests for state/program retrieval in src/MAA.Tests/Integration/QuestionRepositoryTests.cs
- [X] T011 [P] [US1] Add handler tests for validation and not-found cases in src/MAA.Tests/Application/GetQuestionDefinitionsHandlerTests.cs

### Implementation for User Story 1

- [X] T012 [P] [US1] Add request/response DTOs in src/MAA.Application/DTOs/QuestionDtos.cs
- [X] T013 [US1] Implement GetQuestionDefinitionsHandler in src/MAA.Application/Handlers/GetQuestionDefinitionsHandler.cs
- [X] T014 [US1] Implement state/program validator in src/MAA.Application/Validation/StateProgramValidator.cs
- [X] T015 [US1] Implement repository query for base questions in src/MAA.Infrastructure/Repositories/QuestionRepository.cs
- [X] T016 [US1] Add QuestionsController endpoint in src/MAA.API/Controllers/QuestionsController.cs
- [X] T017 [US1] Add audit logging for access in src/MAA.API/Controllers/QuestionsController.cs

**Checkpoint**: User Story 1 functional and independently testable

---

## Phase 4: User Story 2 - Support Conditional Question Visibility (Priority: P1)

**Goal**: Provide conditional rule definitions and evaluation helpers so the wizard can decide visibility.

**Independent Test**: Fetch questions and evaluate rules against sample answer sets to confirm visibility results.

### Tests for User Story 2

- [X] T018 [P] [US2] Add unit tests for rule evaluator in src/MAA.Tests/Domain/ConditionalRuleEvaluatorTests.cs
- [X] T019 [P] [US2] Add frontend rule evaluation tests in frontend/tests/lib/evaluateConditionalRules.test.ts

### Implementation for User Story 2

- [X] T020 [P] [US2] Implement rule parser/evaluator in src/MAA.Domain/Rules/ConditionalRuleEvaluator.cs
- [X] T021 [US2] Add circular dependency detection in src/MAA.Application/Validation/ConditionalRuleValidator.cs
- [X] T022 [US2] Wire rule validation into handler in src/MAA.Application/Handlers/GetQuestionDefinitionsHandler.cs
- [X] T023 [US2] Add frontend evaluator helper in frontend/src/lib/evaluateConditionalRules.ts

**Checkpoint**: Conditional visibility rules evaluate correctly and are usable client-side

---

## Phase 5: User Story 3 - Retrieve Question Details with Metadata (Priority: P2)

**Goal**: Return complete metadata (options, help text, validation) required for UI rendering.

**Independent Test**: Retrieve questions and verify metadata fields and option lists are present and correctly shaped.

### Tests for User Story 3

- [X] T024 [P] [US3] Add metadata mapping tests in src/MAA.Tests/Application/QuestionMetadataMappingTests.cs
- [X] T025 [P] [US3] Add frontend hook tests for metadata consumption in frontend/tests/hooks/useQuestions.test.tsx

### Implementation for User Story 3

- [X] T026 [P] [US3] Extend DTOs for metadata and options in src/MAA.Application/DTOs/QuestionDtos.cs
- [X] T027 [US3] Include options and rules in repository query in src/MAA.Infrastructure/Repositories/QuestionRepository.cs
- [X] T028 [US3] Map metadata/options in handler in src/MAA.Application/Handlers/GetQuestionDefinitionsHandler.cs
- [X] T029 [US3] Add question API service and hook in frontend/src/services/questionService.ts and frontend/src/hooks/useQuestions.ts
- [X] T030 [US3] Add QuestionsLoader component in frontend/src/components/QuestionsLoader.tsx

**Checkpoint**: Complete metadata is available and used by the frontend

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T031 [P] Update feature catalog and API docs in docs/FEATURE_CATALOG.md and docs/SWAGGER-MAINTENANCE.md
- [X] T032 [P] Add performance test scenario in src/MAA.LoadTests/QuestionDefinitionsLoadTest.cs
- [X] T033 [P] Add response caching headers in src/MAA.API/Controllers/QuestionsController.cs

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (Phase 1) -> Foundational (Phase 2) -> User Stories (Phases 3-5) -> Polish (Phase 6)

### User Story Dependencies

- US1 (P1) can start after Foundational and is MVP
- US2 (P1) can start after Foundational; depends on US1 endpoint for data retrieval but remains independently testable with sample payloads
- US3 (P2) can start after Foundational; extends US1 data shape

---

## Parallel Execution Examples

### User Story 1

- T009 and T010 can run in parallel (separate test files)
- T012 and T014 can run in parallel (DTOs and validator)

### User Story 2

- T018 and T019 can run in parallel (backend and frontend tests)
- T020 and T023 can run in parallel (backend evaluator and frontend helper)

### User Story 3

- T024 and T025 can run in parallel (backend and frontend tests)
- T026 and T029 can run in parallel (DTOs and frontend service/hook)

---

## Implementation Strategy

### MVP First (US1)

1. Complete Phase 1 and Phase 2
2. Implement and test US1 (Phase 3)
3. Validate endpoint behavior and ordering

### Incremental Delivery

1. Add US2 for conditional visibility
2. Add US3 for full metadata fidelity
3. Finish Polish tasks

---

## Notes

- [P] tasks are parallelizable with no shared file dependencies
- Each story is independently testable per the acceptance scenarios in spec.md
- All tasks include explicit file paths for direct execution
