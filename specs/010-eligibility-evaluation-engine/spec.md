# Feature Specification: Eligibility Evaluation Engine

**Feature Branch**: `010-eligibility-evaluation-engine`  
**Created**: 2026-02-11  
**Status**: Draft  
**Input**: User description: "Eligibility Evaluation Engine. Inputs: State, Wizard answers, Effective date. Outputs: Eligibility status (Likely / Possibly / Unlikely), Matched programs, Confidence score, Explanation. Requirements: Declarative rules, Versioned, Stateless."

## User Scenarios & Testing _(mandatory)_

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.

  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Evaluate Eligibility For An Applicant (Priority: P1)

Intake staff or an automated workflow submits a state, wizard answers, and an effective date to receive an eligibility assessment with supporting details.

**Why this priority**: This is the core business value and enables downstream guidance to the applicant.

**Independent Test**: Submit a fixed input set against a known rule set and verify the returned status, programs, confidence, and explanation.

**Acceptance Scenarios**:

1. **Given** a valid state, complete wizard answers, and an effective date, **When** eligibility is evaluated, **Then** the response includes status, matched programs, confidence score, and explanation.
2. **Given** identical inputs and the same applicable rule version, **When** eligibility is evaluated twice, **Then** the results are identical.

---

### User Story 2 - Evaluate Using Correct Rule Version (Priority: P2)

Policy staff or QA runs evaluations for a past or future effective date and expects the rules in force on that date to be applied.

**Why this priority**: Correct eligibility depends on policy timing and reduces incorrect determinations.

**Independent Test**: Use two rule versions with different effective dates and verify that the evaluation selects the correct version based on the input date.

**Acceptance Scenarios**:

1. **Given** multiple rule versions with different effective dates, **When** an evaluation is run for a date within a specific version's range, **Then** results reflect that version.
2. **Given** an evaluation date after the newest version's effective date, **When** eligibility is evaluated, **Then** the newest version is used.

---

### User Story 3 - Understand The Determination (Priority: P3)

Support staff or QA reviews the explanation to understand which rules led to the determination and which data was missing or conflicting.

**Why this priority**: Explanations build trust and reduce rework or support escalations.

**Independent Test**: Provide inputs that trigger a mix of met and unmet criteria and verify the explanation lists them clearly.

**Acceptance Scenarios**:

1. **Given** an input set with both met and unmet criteria, **When** eligibility is evaluated, **Then** the explanation identifies decisive met rules and unmet or missing data.

---

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

- Unsupported state provided.
- Missing or invalid effective date.
- Wizard answers incomplete or contain unknown fields.
- No programs match any rules.
- Conflicting rules that would otherwise produce multiple statuses.

## Requirements _(mandatory)_

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.

  Note: All requirements must align with MAA Constitution principles:
  - Constitution I (Code Quality): Entities, DTOs, or complex logic must be testable in isolation
  - Constitution II (Testing): Every requirement must have corresponding test scenario(s)
  - Constitution III (UX Consistency): User-facing requirements must include WCAG 2.1 AA compliance
  - Constitution IV (Performance): Requirements with performance impact must include explicit SLOs
-->

### Functional Requirements

- **FR-001**: System MUST accept inputs of state, wizard answers, and effective date for each eligibility evaluation.
- **FR-002**: System MUST evaluate eligibility using declarative rule sets scoped to the input state.
- **FR-003**: System MUST apply the rule version that is effective on the input date (most recent version on or before the date).
- **FR-004**: System MUST return a status of Likely, Possibly, or Unlikely for each evaluation.
- **FR-005**: System MUST return matched programs, including zero programs when none match.
- **FR-006**: System MUST return a confidence score as an integer percentage from 0 to 100.
- **FR-007**: System MUST return a human-readable explanation that summarizes decisive rules and missing or conflicting data.
- **FR-008**: System MUST be stateless, storing no request data or results beyond the response.
- **FR-009**: System MUST be deterministic for identical inputs and applicable rule version.
- **FR-010**: System MUST validate inputs and return clear error messages for unsupported states, invalid dates, or malformed answers.
- **FR-011**: For performance-sensitive usage, 95% of evaluations MUST return results within 2 seconds and 99% within 5 seconds.

#### Acceptance Criteria

- **AC-001**: Given a valid state, answers, and effective date, the system accepts the request and evaluates eligibility.
- **AC-002**: Given two rule versions with different effective dates, the system uses the version effective on the input date.
- **AC-003**: Every successful evaluation returns status, matched programs (possibly empty), confidence score, and explanation.
- **AC-004**: Given the same inputs and rule version, repeated evaluations return identical results.
- **AC-005**: Given unsupported state, invalid date, or malformed answers, the system returns a clear validation error.
- **AC-006**: In a performance test, 95% of evaluations complete within 2 seconds and 99% within 5 seconds.

### Constitution Compliance Requirements

- **CONST-I**: All domain entities and complex logic MUST be testable in isolation (no database, HTTP, or I/O dependencies)
- **CONST-II**: All functional requirements MUST have corresponding test scenarios (unit, integration, or E2E)
- **CONST-IV**: Features with latency impact MUST define explicit SLOs (response time targets from Constitution IV)

### Key Entities _(include if feature involves data)_

- **EligibilityRequest**: Input bundle containing state, effective date, and wizard answers.
- **EligibilityResult**: Output bundle with status, matched programs, confidence score, and explanation.
- **RuleSetVersion**: Versioned rule collection with an effective date range and state scope.
- **ProgramMatch**: Program identifier and any associated match notes.
- **ExplanationItem**: Human-readable reason describing why a rule was met or not met.

## Assumptions

- Wizard answers are provided in a consistent schema for evaluation.
- Confidence scores are interpreted as percentages from 0 to 100.
- A single evaluation uses one rule version per state and effective date.

## Dependencies

- Access to the current and historical rule sets for each supported state.
- A maintained mapping of programs to their rule criteria.

## Out Of Scope

- Enrollment decisions, benefits issuance, or document verification.
- Long-term storage of individual evaluation results.

## Success Criteria _(mandatory)_

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: 95% of eligibility evaluations complete within 2 seconds and 99% within 5 seconds.
- **SC-002**: 100% of responses include status, matched programs, confidence score, and explanation.
- **SC-003**: For a published regression suite, 100% of cases match expected status and program matches.
- **SC-004**: Repeated evaluations with identical inputs and rule version produce identical results 100% of the time.
