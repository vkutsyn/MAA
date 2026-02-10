# Tasks: Frontend Authentication Flow with Login/Registration

**Input**: Design documents from `/specs/005-frontend-auth-flow/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Not explicitly requested; no test tasks included.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create auth feature scaffold in frontend/src/features/auth/index.ts
- [x] T002 Create auth route wrappers in frontend/src/routes/LoginRoute.tsx and frontend/src/routes/RegisterRoute.tsx
- [x] T003 Add auth routes to router in frontend/src/routes/index.tsx

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

- [x] T004 Implement auth API client wrappers in frontend/src/features/auth/authApi.ts
- [x] T005 Implement auth state store in frontend/src/features/auth/authStore.ts
- [x] T006 Implement single-flight session renewal helper in frontend/src/features/auth/authSession.ts
- [x] T007 Update axios interceptor to use auth session renewal and login redirect in frontend/src/lib/api.ts
- [x] T008 Add auth guard component or hook in frontend/src/features/auth/RequireAuth.tsx

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Login Page (Priority: P1) 🎯 MVP

**Goal**: Provide a login page that authenticates users and avoids unauthorized-response loops

**Independent Test**: User visits app → sees login page → enters credentials → successfully authenticates and navigates to the wizard

### Implementation for User Story 1

- [x] T009 [P] [US1] Create login validation schema in frontend/src/features/auth/loginSchema.ts
- [x] T010 [US1] Build login page UI in frontend/src/features/auth/LoginPage.tsx
- [x] T011 [US1] Wire login submission and error messaging in frontend/src/features/auth/LoginPage.tsx
- [x] T012 [US1] Implement login route wrapper in frontend/src/routes/LoginRoute.tsx
- [x] T013 [US1] Add post-login redirect handling in frontend/src/features/auth/authStore.ts

**Checkpoint**: User Story 1 is functional and independently testable

---

## Phase 4: User Story 2 - Registration Page (Priority: P1)

**Goal**: Enable new users to register and proceed to login with clear validation feedback

**Independent Test**: User clicks "Create Account" → fills registration form → account created → can immediately log in

### Implementation for User Story 2

- [x] T014 [P] [US2] Create registration validation schema in frontend/src/features/auth/registerSchema.ts
- [x] T015 [US2] Build registration page UI in frontend/src/features/auth/RegisterPage.tsx
- [x] T016 [US2] Wire registration submission, success messaging, and errors in frontend/src/features/auth/RegisterPage.tsx
- [x] T017 [US2] Implement registration route wrapper in frontend/src/routes/RegisterRoute.tsx
- [x] T018 [US2] Add login/register cross-links in frontend/src/features/auth/LoginPage.tsx and frontend/src/features/auth/RegisterPage.tsx

**Checkpoint**: User Story 2 is functional and independently testable

---

## Phase 5: User Story 3 - Protected Access (Priority: P1)

**Goal**: Protect wizard routes and prevent unauthorized requests from looping

**Independent Test**: Unauthenticated user tries to access the wizard → redirected to login → after login, accesses the wizard successfully

### Implementation for User Story 3

- [x] T019 [US3] Apply auth guard to wizard routes in frontend/src/routes/WizardRoute.tsx
- [x] T020 [US3] Apply auth guard to landing/resume route in frontend/src/routes/WizardLandingRoute.tsx
- [x] T021 [US3] Prevent resume flow when unauthenticated in frontend/src/features/wizard/useResumeWizard.ts

**Checkpoint**: User Story 3 is functional and independently testable

---

## Phase 6: User Story 4 - Session Renewal (Priority: P2)

**Goal**: Renew sessions automatically and retry failed requests when possible

**Independent Test**: User logs in → session expires → renewal attempted → if renewal succeeds, user continues; if it fails, user is redirected to login

### Implementation for User Story 4

- [x] T022 [US4] Implement renewal API call and error mapping in frontend/src/features/auth/authApi.ts
- [x] T023 [US4] Track renewal status and errors in frontend/src/features/auth/authStore.ts
- [x] T024 [US4] Retry failed requests after renewal in frontend/src/lib/api.ts

**Checkpoint**: User Story 4 is functional and independently testable

---

## Phase 7: User Story 5 - Logout Functionality (Priority: P2)

**Goal**: Allow users to log out and clear authentication state

**Independent Test**: User clicks "Logout" → session cleared → redirected to login → attempting to access protected areas requires re-authentication

### Implementation for User Story 5

- [x] T025 [US5] Implement logout API call and state reset in frontend/src/features/auth/authApi.ts and frontend/src/features/auth/authStore.ts
- [x] T026 [US5] Add logout UI in frontend/src/App.tsx
- [x] T027 [US5] Ensure logout redirects to login and clears return path in frontend/src/features/auth/authSession.ts

**Checkpoint**: User Story 5 is functional and independently testable

---

## Phase 8: User Story 6 - Remember User State (Priority: P3)

**Goal**: Restore authenticated state on page refresh or return visit

**Independent Test**: User logs in → refreshes page → remains authenticated without re-login prompt

### Implementation for User Story 6

- [x] T028 [US6] Create auth bootstrap hook in frontend/src/features/auth/useAuthBootstrap.ts
- [x] T029 [US6] Invoke bootstrap hook on app load in frontend/src/App.tsx
- [x] T030 [US6] Persist return path for post-login redirect in frontend/src/features/auth/authStore.ts

**Checkpoint**: User Story 6 is functional and independently testable

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T031 [P] Review auth UI copy for plain-language errors in frontend/src/features/auth/LoginPage.tsx and frontend/src/features/auth/RegisterPage.tsx
- [x] T032 [P] Validate quickstart steps and update notes in specs/005-frontend-auth-flow/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Foundational phase
- **User Story 2 (P1)**: Depends on Foundational phase
- **User Story 3 (P1)**: Depends on Foundational phase; pairs with US1/US2 for protected access flow
- **User Story 4 (P2)**: Depends on Foundational phase; independent of US2
- **User Story 5 (P2)**: Depends on Foundational phase; pairs with US1 for authenticated UI state
- **User Story 6 (P3)**: Depends on US4 for renewal behavior

### Parallel Opportunities

- Setup tasks T001-T003 can run in parallel with each other
- Foundational tasks T004-T008 can be split across API client, store, renewal, and guard modules
- US1 (T009-T013), US2 (T014-T018), and US3 (T019-T021) can proceed in parallel after Foundational
- Polish tasks T031-T032 can run in parallel after core stories are complete

---

## Parallel Example: User Story 1

```bash
Task: "Create login validation schema in frontend/src/features/auth/loginSchema.ts"
Task: "Build login page UI in frontend/src/features/auth/LoginPage.tsx"
```

---

## Parallel Example: User Story 2

```bash
Task: "Create registration validation schema in frontend/src/features/auth/registerSchema.ts"
Task: "Build registration page UI in frontend/src/features/auth/RegisterPage.tsx"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **Stop and validate** User Story 1 independently

### Incremental Delivery

1. Complete Setup + Foundational
2. Deliver User Story 1 → validate
3. Deliver User Story 2 → validate
4. Deliver User Story 3 → validate
5. Deliver User Story 4 → validate
6. Deliver User Story 5 → validate
7. Deliver User Story 6 → validate

### Parallel Team Strategy

1. Team completes Setup + Foundational together
2. After Foundational:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
3. Follow-up: Developer D can take User Story 4 and 5 in parallel
