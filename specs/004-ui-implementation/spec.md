# Feature Specification: Eligibility Wizard UI

**Feature Branch**: `004-ui-implementation`  
**Created**: February 10, 2026  
**Status**: Implemented (Phases 1-6 Complete)  
**Input**: User description: "I want to start feature with UI - analyze current docks, other feature state and specify UI implementation"

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Start Eligibility Check (Priority: P1)

As a prospective applicant, I can start the eligibility flow from a landing page, select my state, and begin the first question so I know the process is tailored to my location.

**Why this priority**: This is the entry point to the MVP and unlocks all downstream steps.

**Independent Test**: Can be fully tested by opening the UI, selecting a state, and reaching the first wizard step with state-specific context.

**Acceptance Scenarios**:

1. **Given** the landing page, **When** I select a state (auto-detect or manual), **Then** I see a confirmation of the selected state and a Start action.
2. **Given** a selected state, **When** I start the wizard, **Then** the first question renders with progress initialized.

---

### User Story 2 - Complete a Multi-Step Flow (Priority: P2)

As a user, I can move through a multi-step questionnaire with progress and backtracking so I can complete the flow without losing answers.

**Why this priority**: The wizard is the primary UI experience and must support a full completion path.

**Independent Test**: Can be tested by answering a minimal set of questions, moving forward and back, and confirming answers persist within the same session.

**Acceptance Scenarios**:

1. **Given** I am in the wizard, **When** I answer a question and move to the next step, **Then** the answer is retained when I return to the previous step.
2. **Given** I am mid-flow, **When** I refresh the page, **Then** the wizard restores my last completed step and answers for the current session.

---

### User Story 3 - Accessible and Mobile-Friendly Experience (Priority: P3)

As a user on any device, I can complete the wizard using keyboard or touch and understand all prompts and errors.

**Why this priority**: The product targets a wide audience and must be accessible and usable on mobile.

**Independent Test**: Can be tested via keyboard-only navigation and a mobile viewport walkthrough.

**Acceptance Scenarios**:

1. **Given** the wizard, **When** I navigate with keyboard only, **Then** I can complete each step and submit without a mouse.
2. **Given** a mobile viewport, **When** I progress through steps, **Then** all content remains readable without horizontal scrolling.

---

### Edge Cases

- Session expires mid-flow and the user is prompted to restart with a clear explanation.
- State auto-detect fails and the user can still manually choose a state.
- Required field validation fails and the error is shown inline with a clear fix.
- Network interruption occurs and the user sees a retry message without losing completed answers.

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: The system MUST provide a landing experience with a clear call-to-action to start eligibility.
- **FR-002**: The system MUST support state selection with auto-detect and manual override, and confirm the selected state to the user.
- **FR-003**: The system MUST render a multi-step questionnaire with conditional question flow based on prior answers.
- **FR-004**: The system MUST preserve answers when navigating forward/back within the same session.
- **FR-005**: The system MUST restore the most recent step and answers after a page refresh within the same session.
- **FR-006**: The system MUST provide inline help text for "why we ask" and validation errors in plain language.
- **FR-007**: The system MUST meet WCAG 2.1 AA accessibility requirements for the wizard UI.
- **FR-008**: The system MUST support mobile layouts without horizontal scrolling at common phone widths.
- **FR-009**: Step transitions MUST complete in under 500 ms for 95% of interactions under normal load.

### Constitution Compliance Requirements

- **CONST-I**: All domain entities and complex logic MUST be testable in isolation (no database, HTTP, or I/O dependencies)
- **CONST-II**: All functional requirements MUST have corresponding test scenarios (unit, integration, or E2E)
- **CONST-III** (if UI): User-facing features MUST support WCAG 2.1 AA accessibility; include keyboard navigation, screen reader support, color-blind safe designs
- **CONST-IV** (if performance-sensitive): Features with latency impact MUST define explicit SLOs (response time targets from Constitution IV)

### Key Entities _(include if feature involves data)_

- **Wizard Session**: Represents an in-progress eligibility flow, tied to a single user session and state.
- **State Selection**: Captures the chosen state and any auto-detect metadata.
- **Question**: A prompt with type, validation rules, and conditional logic.
- **Answer**: A user response linked to a question and session.
- **Progress State**: Current step index, total steps, and completion percentage.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Users can reach the first wizard question from the landing page in under 30 seconds.
- **SC-002**: 95% of step transitions complete in under 500 ms.
- **SC-003**: Wizard completion rate is at least 70% for the MVP flow (measured as: users who complete all questions / users who start the wizard by clicking "Start Eligibility Check").
- **SC-004**: WCAG 2.1 AA scan reports zero violations for the wizard UI.
- **SC-005**: 90% of users can complete the wizard flow in under 5 minutes on a standard connection (defined as: 4G LTE with 5 Mbps download, 2 Mbps upload, 50ms latency).

## Assumptions

- Anonymous sessions are available and remain valid long enough for a typical wizard completion.
- Eligibility rules and question taxonomy for pilot states exist and are stable.
- The UI does not include account creation or long-term save/resume in this phase.

## Dependencies

- Authentication and session management must be available for anonymous flows (E1).
- Rules engine provides question taxonomy and eligibility evaluation inputs (E2).

## Out of Scope

- Eligibility results presentation (E5).
- Document upload and checklist experience (E6).
- Admin portal and rule management UI (E7/E8).
