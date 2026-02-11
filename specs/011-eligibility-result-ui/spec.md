# Feature Specification: Eligibility Result UI

**Feature Branch**: `011-eligibility-result-ui`  
**Created**: February 11, 2026  
**Status**: Implemented  
**Input**: User description: "Eligibility Result UI - pay attention on 010-eligibility-evaluation-engine as previous step UI: - Eligibility status - Program matches - Explanation bullets - Confidence indicator"

## User Scenarios & Testing _(mandatory)_

### User Story 1 - View eligibility status (Priority: P1)

As an applicant, I want to see my overall eligibility status after completing the questionnaire so I can understand the outcome immediately.

**Why this priority**: This is the primary result users are waiting for and is the core value of the evaluation step.

**Independent Test**: Can be fully tested by providing a completed evaluation result and confirming the status display renders correctly.

**Acceptance Scenarios**:

1. **Given** an evaluation result is available for the current session, **When** the applicant opens the results view, **Then** the eligibility status is shown in a clear, prominent summary.
2. **Given** an evaluation result is not yet available, **When** the applicant opens the results view, **Then** a clear in-progress state is shown with guidance on what to do next.

---

### User Story 2 - Review program matches (Priority: P2)

As an applicant, I want to see which programs match my situation so I can focus on the most relevant options.

**Why this priority**: Program matches drive next steps and help users act on the result.

**Independent Test**: Can be tested by providing a result with multiple matches and verifying the list and ordering display.

**Acceptance Scenarios**:

1. **Given** an evaluation result includes one or more program matches, **When** the results view loads, **Then** the program matches list is displayed with each program name and a brief summary.
2. **Given** an evaluation result includes no program matches, **When** the results view loads, **Then** a clear zero-matches message is displayed with next-step guidance.

---

### User Story 3 - Understand the outcome (Priority: P3)

As an applicant, I want explanation bullets and a confidence indicator so I understand why the outcome was reached and how certain it is.

**Why this priority**: Explanations build trust and help users determine whether to review or update answers.

**Independent Test**: Can be tested by providing explanation data and a confidence level and verifying the display and wording.

**Acceptance Scenarios**:

1. **Given** an evaluation result includes explanation bullets, **When** the results view loads, **Then** the top explanation bullets are displayed in plain language.
2. **Given** the evaluation confidence is low, **When** the results view loads, **Then** the confidence indicator communicates the low confidence with a clear label and description.

---

### Edge Cases

- What happens when the evaluation result is partially available (status present, matches missing)?
- How does the system handle unusually long explanation bullet lists?
- What happens when confidence is unavailable or outside expected ranges?

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: The system MUST display the latest eligibility status for the current session using clear, user-friendly labels.
- **FR-002**: The system MUST display program matches with a name and brief summary for each match.
- **FR-003**: The system MUST display a clear zero-matches state when no programs match.
- **FR-004**: The system MUST display explanation bullets that summarize the top reasons driving the outcome.
- **FR-005**: The system MUST display a confidence indicator with a plain-language label and short description of its meaning.
- **FR-006**: The system MUST handle missing or partial result data by showing a clear fallback message without blocking navigation.
- **FR-007**: The results view MUST render within 2 seconds after the evaluation result becomes available for at least 95% of sessions.
- **FR-008**: User-facing results content MUST support WCAG 2.1 AA accessibility, including keyboard navigation and screen reader readability.

### Constitution Compliance Requirements

- **CONST-I**: All domain entities and complex logic MUST be testable in isolation (no database, HTTP, or I/O dependencies).
- **CONST-II**: All functional requirements MUST have corresponding test scenarios (unit, integration, or E2E).
- **CONST-III**: User-facing features MUST support WCAG 2.1 AA accessibility; include keyboard navigation, screen reader support, color-blind safe designs.
- **CONST-IV**: Features with latency impact MUST define explicit SLOs (response time targets from Constitution IV).

### Key Entities _(include if feature involves data)_

- **Eligibility Result**: The overall outcome for the session, including status, program matches, explanations, confidence, and evaluation timestamp.
- **Program Match**: A candidate program deemed relevant, including program name, short summary, and match priority.
- **Explanation Item**: A plain-language reason that contributed to the outcome, with supporting context.
- **Confidence Indicator**: A level (e.g., high, medium, low) and a short description of what the level means.

### Assumptions

- The evaluation result is produced by the prior eligibility evaluation step and is available in the session context.
- Program match summaries are provided in plain language suitable for end users.
- The results view is accessible from the existing wizard flow without adding new navigation paths.

### Dependencies

- Eligibility evaluation outputs from the prior step are available and up to date for the current session.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: 90% of usability test participants can identify their eligibility status and top program match within 30 seconds.
- **SC-002**: 95% of result views render within 2 seconds after evaluation completion.
- **SC-003**: 85% of users can correctly explain at least one reason for their outcome after reading the explanation bullets.
- **SC-004**: Support requests about "unclear eligibility results" decrease by 30% within 60 days of release.
