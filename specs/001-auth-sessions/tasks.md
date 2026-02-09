# Tasks: Authentication & Session Management (E1)

**Input**: Design documents from `/specs/001-auth-sessions/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: REQUIRED by Constitution II and spec acceptance criteria.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and baseline tooling

- [ ] T001 Verify solution structure and project references in src/MAA.slnx
- [ ] T002 Configure local settings template in src/MAA.API/appsettings.json (no secrets)
- [ ] T003 [P] Confirm test project packages and target framework in src/MAA.Tests/MAA.Tests.csproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure required by all user stories

- [ ] T004 Implement SessionContext and migrations baseline in src/MAA.Infrastructure/Data/SessionContext.cs and src/MAA.Infrastructure/Migrations/
- [ ] T005 [P] Implement repositories for sessions and answers in src/MAA.Infrastructure/Data/SessionRepository.cs and src/MAA.Infrastructure/Data/SessionAnswerRepository.cs
- [ ] T006 [P] Add DI registrations for repositories/services in src/MAA.API/Program.cs
- [ ] T007 [P] Add global exception middleware skeleton in src/MAA.API/Middleware/GlobalExceptionHandlerMiddleware.cs
- [ ] T008 [P] Add DTOs and mapping profile in src/MAA.Application/Sessions/DTOs/ and src/MAA.Application/Mappings/SessionMappingProfile.cs
- [ ] T009 [P] Add Testcontainers fixture in src/MAA.Tests/Integration/DatabaseFixture.cs

**Checkpoint**: Foundation ready for user stories.

---

## Phase 3: User Story 1 - Anonymous User Session (Priority: P1) 🎯 MVP

**Goal**: Anonymous users get a session cookie and can resume within the session window.

**Independent Test**: User navigates to app → session created → refresh → session persists.

### Tests for User Story 1

- [x] T010 [P] [US1] Unit tests for SessionService create/validate/timeout in src/MAA.Tests/Unit/Sessions/SessionServiceTests.cs (assert expired message "Your session expired after 30 minutes. Start a new eligibility check.")
- [x] T011 [P] [US1] Contract tests for POST/GET /api/sessions in src/MAA.Tests/Contract/SessionApiContractTests.cs
- [x] T012 [P] [US1] Integration test for session creation and persistence in src/MAA.Tests/Integration/SessionApiIntegrationTests.cs (assert expired message "Your session expired after 30 minutes. Start a new eligibility check.")

### Implementation for User Story 1

- [x] T013 [US1] Implement SessionService in src/MAA.Application/Services/SessionService.cs
- [x] T014 [US1] Implement CreateSessionCommand handler in src/MAA.Application/Sessions/Commands/CreateSessionCommand.cs
- [x] T015 [US1] Implement SessionMiddleware with sliding timeout, cookie lookup, and expired message "Your session expired after 30 minutes. Start a new eligibility check." in src/MAA.API/Middleware/SessionMiddleware.cs
- [x] T016 [US1] Implement SessionsController POST/GET in src/MAA.API/Controllers/SessionsController.cs
- [x] T017 [US1] Wire middleware order and DI for session flow in src/MAA.API/Program.cs

**Checkpoint**: Anonymous session creation works independently.

---

## Phase 4: User Story 2 - Session Data Persistence (Priority: P1)

**Goal**: Session answers persist and can be retrieved after refresh.

**Independent Test**: Wizard answers saved → refresh → answers restored.

### Tests for User Story 2

- [x] T018 [P] [US2] Unit tests for SessionDataSchema validation in src/MAA.Tests/Unit/Sessions/SessionDataSchemaTests.cs
- [x] T019 [P] [US2] Integration test for save/retrieve answers in src/MAA.Tests/Integration/SessionApiIntegrationTests.cs

### Implementation for User Story 2

- [x] T020 [US2] Implement SaveAnswerCommand handler in src/MAA.Application/Sessions/Commands/SaveAnswerCommand.cs
- [x] T021 [US2] Implement GetAnswersQuery in src/MAA.Application/Sessions/Queries/GetAnswersQuery.cs
- [x] T022 [US2] Implement SessionAnswersController endpoints in src/MAA.API/Controllers/SessionAnswersController.cs
- [x] T023 [US2] Add answer validation rules in src/MAA.Application/Sessions/Validators/SaveAnswerCommandValidator.cs

**Checkpoint**: Session answers persist and are retrievable independently.

---

## Phase 5: User Story 3 - Role-Based Access Control (Priority: P1)

**Goal**: Admin endpoints are restricted to Admin/Reviewer/Analyst roles.

**Independent Test**: Non-admin POST /api/admin/rules → 403 Forbidden.

### Tests for User Story 3

- [x] T024 [P] [US3] Unit tests for AdminRoleMiddleware in src/MAA.Tests/Unit/Middleware/AdminRoleMiddlewareTests.cs
- [x] T025 [P] [US3] Contract test for admin endpoints in src/MAA.Tests/Contract/AdminApiContractTests.cs

### Implementation for User Story 3

- [x] T026 [US3] Implement AdminRoleMiddleware in src/MAA.API/Middleware/AdminRoleMiddleware.cs
- [x] T027 [US3] Implement AdminController stub endpoints in src/MAA.API/Controllers/AdminController.cs
- [x] T028 [US3] Wire RBAC middleware in src/MAA.API/Program.cs

**Checkpoint**: Admin endpoints reject non-admin users independently.

---

## Phase 6: User Story 4 - Sensitive Data Encryption (Priority: P1)

**Goal**: PII is encrypted at rest with randomized encryption and SSN deterministic hash.

**Independent Test**: Income stored encrypted, decrypts correctly on retrieval.

### Tests for User Story 4

- [x] T029 [P] [US4] Unit tests for EncryptionService in src/MAA.Tests/Unit/Encryption/EncryptionServiceTests.cs
- [x] T030 [P] [US4] Integration tests for encryption roundtrip and key rotation in src/MAA.Tests/Integration/EncryptionEndToEndTests.cs

### Implementation for User Story 4

- [x] T031 [US4] Implement EncryptionService in src/MAA.Infrastructure/Encryption/EncryptionService.cs
- [x] T032 [US4] Implement KeyVaultClient in src/MAA.Infrastructure/Security/KeyVaultClient.cs
- [x] T033 [US4] Enable pgcrypto extension in migrations in src/MAA.Infrastructure/Migrations/
- [x] T034 [US4] Apply encryption in SaveAnswerCommand flow in src/MAA.Application/Sessions/Commands/SaveAnswerCommand.cs
- [x] T035 [US4] Handle EncryptionException in src/MAA.API/Middleware/GlobalExceptionHandlerMiddleware.cs

**Checkpoint**: PII encryption is enforced and verified independently.

---

## Phase 7: User Story 5 - Registered User Sessions (Phase 5) (Priority: P2)

**Goal**: JWT login/refresh/logout with max 3 concurrent sessions per user.

**Independent Test**: Fourth login returns 409 with active session list; refresh works.

### Tests for User Story 5

- [x] T036 [P] [US5] Unit tests for JwtTokenProvider in src/MAA.Tests/Unit/Security/JwtTokenProviderTests.cs
- [x] T037 [P] [US5] Integration tests for login/refresh/logout in src/MAA.Tests/Integration/AuthApiIntegrationTests.cs

### Implementation for User Story 5

- [x] T038 [US5] Implement JwtTokenProvider in src/MAA.Infrastructure/Security/JwtTokenProvider.cs
- [x] T039 [US5] Implement AuthController endpoints in src/MAA.API/Controllers/AuthController.cs
- [x] T040 [US5] Enforce max 3 sessions and list/revoke endpoints in src/MAA.Application/Services/SessionService.cs and src/MAA.API/Controllers/AuthController.cs
- [x] T041 [US5] Implement JWT auto-refresh middleware in src/MAA.API/Middleware/JwtRefreshMiddleware.cs
- [x] T042 [US5] Extend OpenAPI contract for auth endpoints in specs/001-auth-sessions/contracts/sessions-api.openapi.yaml

**Checkpoint**: Registered user sessions are enforced and testable independently.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Performance, security, and documentation alignment

- [ ] T043 [P] Run OWASP ZAP baseline and document results in docs/SECURITY.md
- [ ] T044 [P] Add CI security scan step in .github/workflows/security-scan.yml
- [ ] T045 [P] Load test 1000 concurrent sessions and record results in docs/PERFORMANCE.md
- [ ] T046 [P] Update quickstart with auth endpoints and session limits in specs/001-auth-sessions/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Setup
- **User Stories (Phase 3-7)**: Depend on Foundational
- **Polish (Phase 8)**: Depends on all user stories

### User Story Dependencies

- **US1**: Depends on Foundational only
- **US2**: Depends on Foundational, can run parallel to US1
- **US3**: Depends on Foundational, can run parallel to US1/US2
- **US4**: Depends on Foundational, can run parallel to US1/US2/US3
- **US5**: Depends on Foundational and builds on auth primitives

### Within Each User Story

- Tests must be written first
- Models before services
- Services before endpoints

---

## Parallel Execution Examples

### User Story 1

- T010 [US1] Unit tests for SessionService in src/MAA.Tests/Unit/Sessions/SessionServiceTests.cs
- T011 [US1] Contract tests for /api/sessions in src/MAA.Tests/Contract/SessionApiContractTests.cs
- T012 [US1] Integration test for session persistence in src/MAA.Tests/Integration/SessionApiIntegrationTests.cs

### User Story 4

- T029 [US4] Unit tests for EncryptionService in src/MAA.Tests/Unit/Encryption/EncryptionServiceTests.cs
- T030 [US4] Integration tests for encryption roundtrip in src/MAA.Tests/Integration/EncryptionEndToEndTests.cs
- T033 [US4] Enable pgcrypto extension in src/MAA.Infrastructure/Migrations/

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2
2. Complete Phase 3 (US1)
3. Validate US1 independently

### Incremental Delivery

1. US1 → validate
2. US2 → validate
3. US3 → validate
4. US4 → validate
5. US5 → validate
6. Polish & cross-cutting
