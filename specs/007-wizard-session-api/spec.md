# Feature Specification: Eligibility Wizard Session API

**Feature Branch**: `007-wizard-session-api`  
**Created**: 2026-02-10  
**Status**: Draft  
**Input**: User description: "Eligibility Wizard Session API - Purpose: Support multi-step wizard flow with save & resume. Behavior: Save step answers, Return next step definition, Rehydrate wizard state"

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Save Progress and Resume Later (Priority: P1)

A user completes several wizard steps but needs to stop and return later. The system saves their progress automatically, and when they return (even days later), they resume exactly where they left off with all previous answers intact.

**Why this priority**: Save & resume is the core value proposition. Without it, users lose progress, creating frustration and incomplete applications. This is the MVP that makes the wizard practical for long forms.

**Independent Test**: Can be fully tested by submitting answers for 3 steps, closing the session, reopening with the same sessionId, and verifying all answers are restored and the wizard resumes at step 4.

**Acceptance Scenarios**:

1. **Given** user has completed steps 1-3 of the wizard, **When** user closes browser and returns 2 hours later with same sessionId, **Then** user sees step 4 with all previous answers accessible
2. **Given** user has saved answers for step 2, **When** user submits an answer for step 2 again, **Then** the previous answer is overwritten with the new value
3. **Given** user session expires (30 days inactive), **When** user attempts to resume, **Then** system requires starting a new session and warns that previous progress was lost

---

### User Story 2 - Dynamic Step Navigation Based on Answers (Priority: P2)

The wizard flow adapts based on user answers. For example, if user indicates they have dependents, the next step asks for dependent details. If no dependents, that step is skipped. The system determines the next step dynamically.

**Why this priority**: Dynamic navigation prevents users from seeing irrelevant questions, improving completion rates and user satisfaction. This is essential for a good wizard UX but depends on having step completion (P1) working first.

**Independent Test**: Can be tested by answering "Yes" to "Do you have children?" and verifying the next step is "Child Information", then testing with "No" and verifying the system skips to "Income Verification" instead.

**Acceptance Scenarios**:

1. **Given** user answers "Yes" to household size > 1, **When** requesting next step, **Then** system returns "Household Members" step definition
2. **Given** user answers "No" to household size > 1, **When** requesting next step, **Then** system returns "Income Summary" step (skipping household members)
3. **Given** user is on step 10 of 12, **When** all required answers are complete, **Then** next step indicates "Review & Submit" as final step

---

### User Story 3 - Review and Modify Previous Steps (Priority: P3)

Users can navigate back to any previously completed step to review or change their answers. When they modify an answer that affects downstream steps, the system invalidates those subsequent steps and requires re-answering.

**Why this priority**: Users make mistakes or remember additional information. Being able to go back improves data accuracy. This is valuable but not essential for basic wizard flow.

**Independent Test**: Complete steps 1-5, navigate back to step 2, change answer from "Single" to "Married", and verify step 3 (which asks about spouse) now requires re-answering and is marked incomplete.

**Acceptance Scenarios**:

1. **Given** user is on step 5, **When** user requests to view step 2, **Then** system returns step 2 definition with previously saved answer
2. **Given** user modifies answer on step 2 that affects step 3 logic, **When** requesting next step after saving, **Then** system marks step 3 as "requires re-validation" and directs user there
3. **Given** user is on final review page, **When** user clicks edit on step 3, **Then** system navigates to step 3 in edit mode with current answer pre-filled

---

### Edge Cases

- What happens when session expires mid-wizard? System should detect expired session and prevent saving, notifying user to restart
- How does system handle concurrent updates (user opens wizard in two tabs)? Use optimistic locking with version numbers to detect conflicts
- What if a step definition changes after user completes it? System should re-validate answers against current step schema on resume
- How are incomplete steps tracked? Maintain step completion status (not_started, in_progress, completed) separate from answer storage
- What if user skips required validation? API must enforce validation server-side; never trust client-side validation alone

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: System MUST save user answers for each wizard step with sessionId, stepId, and timestamp
- **FR-002**: System MUST return the next step definition based on current position and previous answers (dynamic navigation logic)
- **FR-003**: System MUST restore complete wizard state when given a sessionId, including all saved answers, current step, and completion status
- **FR-004**: System MUST track step completion status independently (not_started, in_progress, completed, requires_revalidation)
- **FR-005**: System MUST validate answer data types and constraints before persisting (e.g., required fields, format validation)
- **FR-006**: System MUST support conditional step logic (e.g., "show step 5 only if answer to step 2 equals 'yes'")
- **FR-007**: System MUST allow users to navigate to any previously visited step and modify answers
- **FR-008**: System MUST invalidate downstream steps when a dependency answer changes (e.g., changing "married" to "single" invalidates spouse details)
- **FR-009**: System MUST persist answers immediately upon submission (auto-save per step, not per wizard completion)
- **FR-010**: System MUST prevent saving to expired or invalid sessions (return 401 Unauthorized or 403 Forbidden)
- **FR-011**: API MUST return step definitions including field schemas, validation rules, and display metadata (labels, help text, field types)
- **FR-012**: System MUST support answer retrieval for specific step or all steps for a session
- **FR-013**: System MUST detect and handle concurrent modification conflicts using optimistic locking (version numbers)

### Constitution Compliance Requirements

- **CONST-I**: All domain entities (WizardSession, StepAnswer, StepDefinition) and navigation logic MUST be testable in isolation without database dependencies
- **CONST-II**: All functional requirements MUST have corresponding test scenarios (unit tests for navigation logic, integration tests for API endpoints, E2E tests for full wizard flow)
- **CONST-III**: Not applicable - this is a backend API feature with no direct UI component
- **CONST-IV**: Features with latency impact MUST define explicit SLOs:
  - Answer save endpoint: <200ms p95
  - Get next step: <150ms p95 (includes navigation logic execution)
  - Restore session state: <500ms p95 (includes loading all answers and current position)
  - Step definition retrieval: <100ms p95 (should be cached/optimized)

### Key Entities

- **WizardSession**: Represents the overall wizard state for a session
  - Attributes: SessionId (FK to existing Session), CurrentStepId, CompletedSteps[], LastActivityAt, Version (optimistic locking)
  - Relationships: Has many StepAnswers, belongs to Session (from feature 001)

- **StepAnswer**: Represents a user's answer to a specific wizard step
  - Attributes: SessionId, StepId, AnswerData (JSONB), SubmittedAt, Status (draft/submitted), Version
  - Relationships: Belongs to WizardSession

- **StepDefinition**: Represents the schema and metadata for a wizard step (can be defined in code/config, not necessarily a database entity)
  - Attributes: StepId, Title, Fields[], ValidationRules[], ConditionalLogic[], NextStepLogic
  - Note: May be defined in configuration files or code rather than database tables

- **StepNavigationRule**: Defines conditional logic for determining the next step
  - Attributes: FromStepId, Condition (expression evaluating previous answers), ToStepId
  - Example: "IF step_2.hasChildren == true THEN goto step_3_children ELSE goto step_5_income"

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Users can save progress at any step and successfully resume from the exact same position 100% of the time
- **SC-002**: API responds to answer submission within 200ms at 95th percentile under load (100 concurrent users)
- **SC-003**: Session state restoration (GET /wizard-session/{sessionId}) completes within 500ms at 95th percentile
- **SC-004**: Next step calculation (POST /wizard-session/next-step) returns correct step based on logic within 150ms at 95th percentile
- **SC-005**: System detects and prevents 100% of concurrent modification conflicts through versioning
- **SC-006**: All step answers are persisted with zero data loss (verified through transaction integrity)
- **SC-007**: Dynamic step navigation correctly skips/includes steps based on previous answers in 100% of test scenarios

### Quality Metrics

- Unit test coverage for navigation logic: >90%
- Integration test coverage for API endpoints: 100% of endpoints
- Zero SQL injection or XSS vulnerabilities in answer storage/retrieval
- All step validation rules enforced server-side (client-side validation is UI convenience only)

## Assumptions

- The wizard step definitions and navigation rules are defined in the .NET application (not user-configurable through UI)
- Session management infrastructure from feature 001 (auth-sessions) is already in place and working
- State context from feature 006 (state-context-init) has been established before wizard begins
- Answer data is stored as JSONB to support flexible step schemas without frequent schema migrations
- Users interact with wizard through a single-page application (SPA) that makes API calls per step
- Session timeout and expiration handling is managed by existing session infrastructure (feature 001)
- The wizard has a defined set of steps (estimated 8-15 steps for Medicaid eligibility), not infinite
- Frontend will implement optimistic UI updates with server-side validation as source of truth

## Dependencies

- **Feature 001** (auth-sessions): Required - wizard sessions are tied to existing session infrastructure
- **Feature 006** (state-context-init): Required - state context must be established before wizard begins to ensure correct state-specific questions
- PostgreSQL with JSONB support: Required - for flexible answer storage
- Existing API infrastructure: Required - JWT authentication, CORS, middleware

## Out of Scope

- User interface / frontend wizard components (separate frontend feature)
- PDF generation or form export functionality
- Email notifications for incomplete sessions
- Administrative tools to view/edit user wizard progress
- Wizard branching preview or visualization tools
- Multi-language support for step definitions
- Offline mode or Progressive Web App (PWA) capabilities
- Wizard template system for creating new wizard types
- Bulk answer import/export functionality
- Integration with external data sources for pre-filling answers
