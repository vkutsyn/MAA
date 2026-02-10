# Tasks: State Context Initialization Step

**Input**: Design documents from `/specs/006-state-context-init/`
**Feature Branch**: `006-state-context-init`
**Prerequisites**: ✅ plan.md, ✅ spec.md, ✅ research.md, ✅ data-model.md, ✅ contracts/

**Tests**: Tests are NOT explicitly requested in the specification, so test tasks are included as optional (can be implemented incrementally).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `src/MAA.Domain/`, `src/MAA.Application/`, `src/MAA.Infrastructure/`, `src/MAA.API/`
- **Frontend**: `frontend/src/`
- **Tests**: `src/MAA.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and ZIP code data acquisition

- [x] T001 Download SimpleMaps ZIP code database or prepare ZIP-to-state mapping data source (~42,000 entries)
- [x] T002 [P] Create ZIP-to-state CSV file in src/MAA.Infrastructure/Data/zip-to-state.csv
- [x] T003 [P] Create state configurations seed data JSON in src/MAA.Infrastructure/Data/state-configs.json (50 states + DC sample data)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain entities and infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

**Constitution Alignment**: Infrastructure MUST support:

- Constitution I: Dependency injection, testable domain layer separation
- Constitution II: xUnit test framework configured, WebApplicationFactory for integration tests
- Constitution III: shadcn/ui components, React Hook Form, Zod validation
- Constitution IV: IMemoryCache for state configs, PostgreSQL connection pooling

### Backend Foundation

- [x] T004 [P] Create StateContext domain entity in src/MAA.Domain/StateContext/StateContext.cs with Create() and UpdateState() methods
- [x] T005 [P] Create StateConfiguration domain entity in src/MAA.Domain/StateContext/StateConfiguration.cs with factory method
- [x] T006 [P] Create ZipCodeValidator static class in src/MAA.Domain/StateContext/ZipCodeValidator.cs with IsValid() and Validate() methods
- [x] T007 [P] Create StateResolver class in src/MAA.Domain/StateContext/StateResolver.cs with Resolve() method
- [x] T008 [P] Create StateResolutionResult value object in src/MAA.Domain/StateContext/StateResolutionResult.cs
- [x] T009 Create Entity Framework Core configuration for StateContext in src/MAA.Infrastructure/StateContext/StateContextConfiguration.cs
- [x] T010 Create Entity Framework Core configuration for StateConfiguration in src/MAA.Infrastructure/StateContext/StateConfigurationConfiguration.cs
- [x] T011 Create EF Core migration for StateContexts and StateConfigurations tables in src/MAA.Infrastructure/Migrations/
- [x] T012 [P] Create ZipCodeMappingService in src/MAA.Infrastructure/StateContext/ZipCodeMappingService.cs to load ZIP-to-state CSV into Dictionary
- [x] T013 [P] Create StateConfigurationSeeder in src/MAA.Infrastructure/StateContext/StateConfigurationSeeder.cs to seed state configs from JSON
- [x] T014 Register ZipCodeMappingService as singleton in src/MAA.API/Program.cs DI container
- [x] T015 Apply EF Core migration to local PostgreSQL database (dotnet ef database update)

### Frontend Foundation

- [x] T016 [P] Create TypeScript interfaces for StateContext, StateConfiguration in frontend/src/features/state-context/types/stateContext.types.ts
- [x] T017 [P] Create API client module in frontend/src/features/state-context/api/stateContextApi.ts with axios calls
- [x] T018 [P] Create route component placeholder in frontend/src/routes/StateContextStep.tsx
- [x] T019 Add route /state-context to React Router configuration in frontend/src/App.tsx

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - ZIP Code Entry with State Auto-Detection (Priority: P1) 🎯 MVP

**Goal**: Users can enter their ZIP code, system auto-detects state, loads state configuration, persists to session, and navigates to wizard step 1

**Independent Test**: Enter ZIP "90210" → state detected as "CA" (California) → state config loaded → session contains StateContext → navigates to /wizard/step-1

### Tests for User Story 1 (OPTIONAL)

> **NOTE: Tests can be written incrementally alongside implementation**

- [ ] T020 [P] [US1] Unit test for ZipCodeValidator in src/MAA.Tests/Unit/StateContext/ZipCodeValidatorTests.cs (valid/invalid formats)
- [ ] T021 [P] [US1] Unit test for StateResolver in src/MAA.Tests/Unit/StateContext/StateResolverTests.cs (successful resolution, ZIP not found)
- [ ] T022 [P] [US1] Unit test for StateContext.Create() in src/MAA.Tests/Unit/StateContext/StateContextTests.cs
- [ ] T023 [P] [US1] Integration test for InitializeStateContextHandler in src/MAA.Tests/Integration/StateContext/InitializeStateContextHandlerTests.cs
- [ ] T024 [P] [US1] Integration test for StateContextController POST endpoint in src/MAA.Tests/Integration/StateContext/StateContextControllerTests.cs
- [ ] T025 [P] [US1] Frontend component test for ZipCodeForm in frontend/src/**tests**/features/state-context/ZipCodeForm.test.tsx
- [ ] T026 [P] [US1] Frontend hook test for useInitializeStateContext in frontend/src/**tests**/features/state-context/useStateContext.test.ts

### Backend Implementation for User Story 1

- [x] T027 [P] [US1] Create InitializeStateContextRequest DTO in src/MAA.Application/StateContext/DTOs/InitializeStateContextRequest.cs
- [x] T028 [P] [US1] Create StateContextDto in src/MAA.Application/StateContext/DTOs/StateContextDto.cs
- [x] T029 [P] [US1] Create StateConfigurationDto in src/MAA.Application/StateContext/DTOs/StateConfigurationDto.cs
- [x] T030 [P] [US1] Create StateContextResponse DTO in src/MAA.Application/StateContext/DTOs/StateContextResponse.cs
- [x] T031 [P] [US1] Create FluentValidation validator for InitializeStateContextRequest in src/MAA.Application/StateContext/Validators/InitializeStateContextRequestValidator.cs
- [x] T032 [US1] Create IStateContextRepository interface in src/MAA.Application/StateContext/IStateContextRepository.cs
- [x] T033 [US1] Create IStateConfigurationRepository interface in src/MAA.Application/StateContext/IStateConfigurationRepository.cs
- [x] T034 [US1] Implement StateContextRepository in src/MAA.Infrastructure/StateContext/StateContextRepository.cs
- [x] T035 [US1] Implement StateConfigurationRepository with IMemoryCache in src/MAA.Infrastructure/StateContext/StateConfigurationRepository.cs
- [x] T036 [US1] Create InitializeStateContextCommand in src/MAA.Application/StateContext/Commands/InitializeStateContextCommand.cs
- [x] T037 [US1] Create InitializeStateContextHandler in src/MAA.Application/StateContext/Commands/InitializeStateContextHandler.cs (inject repositories, ZipCodeMappingService)
- [x] T038 [US1] Create GetStateContextQuery in src/MAA.Application/StateContext/Queries/GetStateContextQuery.cs
- [x] T039 [US1] Create GetStateContextHandler in src/MAA.Application/StateContext/Queries/GetStateContextHandler.cs
- [x] T040 [US1] Create StateContextController in src/MAA.API/Controllers/StateContextController.cs with POST and GET endpoints
- [x] T041 [US1] Register repositories and handlers in src/MAA.API/Program.cs DI container
- [x] T042 [US1] Add Swagger annotations to StateContextController endpoints

### Frontend Implementation for User Story 1

- [x] T043 [P] [US1] Create useInitializeStateContext hook in frontend/src/features/state-context/hooks/useStateContext.ts using TanStack Query useMutation
- [x] T044 [P] [US1] Create useGetStateContext hook in frontend/src/features/state-context/hooks/useStateContext.ts using TanStack Query useQuery
- [x] T045 [P] [US1] Create Zod schema for ZIP code validation in frontend/src/features/state-context/components/ZipCodeForm.tsx
- [x] T046 [US1] Create ZipCodeForm component in frontend/src/features/state-context/components/ZipCodeForm.tsx with React Hook Form + Zod validation
- [x] T047 [US1] Create StateConfirmation component in frontend/src/features/state-context/components/StateConfirmation.tsx to display detected state
- [x] T048 [US1] Implement StateContextStep route component in frontend/src/routes/StateContextStep.tsx (integrate ZipCodeForm, StateConfirmation, navigation)
- [x] T049 [US1] Add navigation to /wizard/step-1 on successful state context initialization in StateContextStep.tsx
- [x] T050 [US1] Add error toast notifications for API failures using shadcn/ui Toast component
- [x] T051 [US1] Verify WCAG 2.1 AA compliance: proper labels, aria-describedby for errors, role="alert" for error messages

**Checkpoint**: User Story 1 is fully functional - users can enter ZIP, see detected state, proceed to wizard

---

## Phase 4: User Story 2 - State Override Option (Priority: P2)

**Goal**: Users can manually override auto-detected state for edge cases (recently moved, multi-state scenarios)

**Independent Test**: Enter ZIP "10001" → state detected as "NY" → click "Change State" → select "NJ" from dropdown → state updated to New Jersey → navigates to wizard

### Tests for User Story 2 (OPTIONAL)

- [ ] T052 [P] [US2] Integration test for UpdateStateContextHandler in src/MAA.Tests/Integration/StateContext/UpdateStateContextHandlerTests.cs
- [ ] T053 [P] [US2] Integration test for StateContextController PUT endpoint in src/MAA.Tests/Integration/StateContext/StateContextControllerTests.cs
- [ ] T054 [P] [US2] Frontend component test for StateOverride in frontend/src/**tests**/features/state-context/StateOverride.test.tsx

### Backend Implementation for User Story 2

- [x] T055 [P] [US2] Create UpdateStateContextRequest DTO in src/MAA.Application/StateContext/DTOs/UpdateStateContextRequest.cs
- [x] T056 [P] [US2] Create FluentValidation validator for UpdateStateContextRequest in src/MAA.Application/StateContext/Validators/UpdateStateContextRequestValidator.cs
- [x] T057 [US2] Create UpdateStateContextCommand in src/MAA.Application/StateContext/Commands/UpdateStateContextCommand.cs
- [x] T058 [US2] Create UpdateStateContextHandler in src/MAA.Application/StateContext/Commands/UpdateStateContextHandler.cs
- [x] T059 [US2] Add PUT endpoint to StateContextController in src/MAA.API/Controllers/StateContextController.cs

### Frontend Implementation for User Story 2

- [x] T060 [P] [US2] Create useUpdateStateContext hook in frontend/src/features/state-context/hooks/useStateContext.ts using TanStack Query useMutation
- [x] T061 [US2] Create StateOverride component in frontend/src/features/state-context/components/StateOverride.tsx with state selector dropdown (shadcn/ui Select)
- [x] T062 [US2] Integrate StateOverride component into StateContextStep.tsx (show after state detection)
- [x] T063 [US2] Implement state override logic: update StateContext, reload state configuration, display updated state
- [x] T064 [US2] Verify keyboard accessibility for state selector dropdown (Tab navigation, Enter to select)

**Checkpoint**: User Story 2 is complete - users can override detected state when needed

---

## Phase 5: User Story 3 - Invalid ZIP Code Handling (Priority: P3)

**Goal**: Users receive clear error messages for invalid ZIP codes and can correct them without losing progress

**Independent Test**: Enter invalid ZIP "1234" → see error "Please enter a valid 5-digit ZIP code" → correct to "90210" → error clears → proceeds normally

### Tests for User Story 3 (OPTIONAL)

- [ ] T065 [P] [US3] Unit test for ZipCodeValidator edge cases (empty string, letters, special chars) in ZipCodeValidatorTests.cs
- [ ] T066 [P] [US3] Integration test for error handling in InitializeStateContextHandler (ZIP not found scenario) in InitializeStateContextHandlerTests.cs
- [ ] T067 [P] [US3] Frontend validation test for invalid ZIP formats in ZipCodeForm.test.tsx

### Backend Implementation for User Story 3

- [x] T068 [US3] Create custom ValidationException in src/MAA.Application/Exceptions/ValidationException.cs (if not already exists)
- [x] T069 [US3] Add error handling for ZIP not found in InitializeStateContextHandler (throw ValidationException with message "ZIP code not found")
- [x] T070 [US3] Add global exception handling middleware for ValidationException in src/MAA.API/Middleware/ExceptionHandlingMiddleware.cs
- [x] T071 [US3] Create ErrorResponse DTO in src/MAA.Application/DTOs/ErrorResponse.cs with error/message/details fields
- [x] T072 [US3] Update StateContextController to return 400 Bad Request with ErrorResponse for validation failures

### Frontend Implementation for User Story 3

- [x] T073 [US3] Add inline error display for ZIP code validation in ZipCodeForm.tsx (error message below input, red border)
- [x] T074 [US3] Add error handling for "ZIP not found" API response in useInitializeStateContext hook
- [x] T075 [US3] Display API error messages in ZipCodeForm (not found, network error, server error differentiation)
- [x] T076 [US3] Implement error message clearing when user corrects invalid ZIP code
- [x] T077 [US3] Verify error messages are announced by screen readers (aria-live="polite", role="alert")

**Checkpoint**: User Story 3 is complete - all error scenarios handled gracefully

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements and validations that affect multiple user stories

### Performance Optimization

- [x] T078 [P] Verify state configuration caching with IMemoryCache (24h TTL) is working correctly
- [x] T079 [P] Verify ZIP-to-state mapping loaded into memory at startup (check application logs)
- [ ] T080 Test API endpoint performance: <1000ms p95 for POST /api/state-context (use local load testing or Swagger timing)

### Accessibility Validation

- [ ] T081 Run axe DevTools on /state-context route, verify zero accessibility violations
- [ ] T082 Test keyboard-only navigation: Tab through all inputs, Enter to submit, Escape to close modals
- [ ] T083 Test screen reader flow with NVDA or macOS VoiceOver (labels, error announcements, navigation)

### Responsive Design

- [ ] T084 [P] Test mobile layout (375px): ensure form elements stack vertically, touch targets ≥44×44px
- [ ] T085 [P] Test tablet layout (768px): verify form adapts appropriately
- [ ] T086 [P] Test desktop layout (1920px): ensure form doesn't stretch excessively

### Documentation & Code Quality

- [x] T087 [P] Update API documentation in contracts/state-context-api.yaml if any endpoints changed
- [x] T088 [P] Add XML documentation comments to all public methods in backend (StateContext entities, handlers, controllers)
- [x] T089 [P] Add JSDoc comments to all exported functions in frontend (API client, hooks, components)
- [x] T090 Run code formatters: dotnet format (backend), prettier (frontend)

### End-to-End Validation

- [x] T091 Run quickstart.md validation: follow all steps in quickstart.md, verify all commands execute successfully
- [x] T092 Manual E2E test: ZIP entry (90210) → state detected (CA) → click Continue → navigate to /wizard/step-1 (mock or placeholder)
- [x] T093 Manual E2E test: ZIP entry (10001) → state detected (NY) → override to NJ → confirm NJ selected → navigate to wizard
- [x] T094 Manual E2E test: Invalid ZIP (1234) → error message → correct to 90210 → error clears → proceed normally

### Deployment Preparation

- [x] T095 Verify EF Core migrations are idempotent (can run multiple times without errors)
- [x] T096 [P] Create database seed script for production (state configurations for all 50 states + DC + territories)
- [x] T097 [P] Verify environment variable configuration (API base URL, database connection string)
- [x] T098 Update CHANGELOG.md with feature description and breaking changes (if any)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately (data acquisition)
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories - MUST complete for MVP
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Builds on US1 UI but independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Enhances error handling for US1 and US2

### Within Each User Story

- Tests (if included) can be written alongside implementation or test-first (preferred)
- Domain entities before application handlers
- Application DTOs and commands before handlers
- Repositories before handlers that use them
- Backend API endpoints before frontend API client calls
- Frontend hooks before components that use them
- Core components before route integration

### Parallel Opportunities

**Phase 1 (Setup)**: All tasks [P] can run in parallel (T001, T002, T003)

**Phase 2 (Foundational)**:

- Backend domain entities (T004-T008) can all run in parallel
- EF Core configurations (T009, T010) can run in parallel after entities exist
- Infrastructure services (T012, T013) can run in parallel
- Frontend foundation (T016-T019) can all run in parallel

**Phase 3 (US1)**:

- All tests (T020-T026) can run in parallel
- Backend DTOs (T027-T030) can run in parallel
- Repositories (T032-T035) can run in parallel after interfaces defined
- Frontend hooks (T043-T044) can run in parallel
- Frontend components (T046-T047) can run in parallel after hooks exist

**Phase 4 (US2)**:

- All tests (T052-T054) can run in parallel
- Backend DTOs/validators (T055-T056) can run in parallel
- Frontend hook and component (T060-T061) can run in parallel

**Phase 5 (US3)**:

- All tests (T065-T067) can run in parallel
- Backend exception handling (T068-T072) can be sequential but quick
- Frontend error handling (T073-T077) can run sequentially

**Phase 6 (Polish)**:

- Performance tests (T078-T080) can run in parallel
- Accessibility tests (T081-T083) can run sequentially (manual)
- Responsive tests (T084-T086) can run in parallel
- Documentation (T087-T090) can all run in parallel
- E2E tests (T092-T094) should run sequentially (manual UX testing)
- Deployment prep (T095-T098) some can run in parallel

---

## Implementation Strategy

### MVP Scope (Minimum Viable Product)

To deliver a functional feature quickly, implement in this order:

1. **Phase 1**: Setup (required for data)
2. **Phase 2**: Foundational (required infrastructure)
3. **Phase 3**: User Story 1 ONLY (ZIP entry + auto-detect + navigation)
4. **Phase 6**: Essential polish (performance check, basic accessibility, E2E test for US1)

**MVP Deliverable**: Users can enter ZIP code, see detected state, and proceed to wizard. Estimated time: 8-10 hours.

### Incremental Delivery

After MVP is working:

1. **Add User Story 2** (state override) - enhances UX for edge cases - Estimated: +2-3 hours
2. **Add User Story 3** (error handling) - improves robustness - Estimated: +2 hours
3. **Complete Phase 6** (full polish, documentation, all E2E scenarios) - Estimated: +2-3 hours

**Full Feature Complete**: All 3 user stories + polish. Total estimated time: 14-18 hours.

---

## Progress Tracking

**Current Status**: Phase 6 Polish - Mostly Complete

**Completed Phases**:

- Phase 1: Setup ✅
- Phase 2: Foundational ✅
- Phase 3: User Story 1 ✅
- Phase 4: User Story 2 ✅
- Phase 5: User Story 3 ✅
- Phase 6: Polish (Partial - manual tests pending) ⏳

**Next Task**: T080-T086 (Manual testing: Performance, Accessibility, Responsive Design)

**Blockers**: None - Manual testing requires human QA tester

---

## Success Criteria

Before marking this feature complete, all of the following must be true:

✅ **User Story 1**: Users can enter valid ZIP, see detected state, navigate to wizard  
✅ **User Story 2**: Users can override detected state when needed  
✅ **User Story 3**: Invalid ZIP codes show clear error messages  
✅ **Performance**: <1000ms p95 for state initialization (T080)  
✅ **Accessibility**: Zero axe DevTools violations (T081)  
✅ **Responsive**: Works on mobile 375px, tablet 768px, desktop 1920px (T084-T086)  
✅ **Tests**: Key unit and integration tests passing (optional but recommended)  
✅ **E2E**: Manual end-to-end tests pass for all 3 user stories (T092-T094)  
✅ **Documentation**: API docs updated, code commented (T087-T089)  
✅ **Database**: Migrations applied, state configs seeded (T015, T096)

---

## Total Tasks: 98

- **Setup**: 3 tasks
- **Foundational**: 16 tasks (12 backend, 4 frontend)
- **User Story 1**: 32 tasks (7 tests, 16 backend, 9 frontend)
- **User Story 2**: 13 tasks (3 tests, 5 backend, 5 frontend)
- **User Story 3**: 13 tasks (3 tests, 5 backend, 5 frontend)
- **Polish**: 21 tasks (performance, accessibility, docs, E2E, deployment)

**MVP Tasks**: 19 (Phase 1 + Phase 2 + US1 core implementation, no tests)  
**Full Feature Tasks**: 98 (all phases including tests and polish)
