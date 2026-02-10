# Feature Specification: Eligibility Question Definitions API

**Feature Branch**: `008-question-definitions-api`  
**Created**: 2026-02-10  
**Status**: Draft  
**Input**: User description: "Eligibility Question Definitions API - Return state- and program-specific questions, Support conditional visibility"

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Retrieve Questions by State and Program (Priority: P1)

As a frontend application, I need to fetch the question definitions for a specific state and program combination so that I can render the correct eligibility assessment flow for the user.

**Why this priority**: This is the core functionality of the API - without the ability to fetch state/program-specific questions, the entire eligibility wizard cannot function. P1 is critical for MVP.

**Independent Test**: Can be fully tested by calling the API with a state code and program code and verifying that the correct questions are returned and can be rendered without any conditional logic evaluation.

**Acceptance Scenarios**:

1. **Given** a valid state code (e.g., "CA") and program code (e.g., "MEDI-CAL"), **When** requesting questions, **Then** the API returns all questions applicable to that state/program combination with their metadata
2. **Given** a state/program combination with no questions defined, **When** requesting questions, **Then** the API returns an empty list without error
3. **Given** an invalid state code or program code, **When** requesting questions, **Then** the API returns a 400 Bad Request with an appropriate error message

---

### User Story 2 - Support Conditional Question Visibility (Priority: P1)

As a wizard interface, I need to know which questions should be shown or hidden based on previous user answers so that I can guide users through a personalized eligibility flow without asking irrelevant questions.

**Why this priority**: Conditional visibility is essential for user experience - it ensures questions are only asked when relevant. This combined with Story 1 represents the complete core feature. P1 for MVP.

**Independent Test**: Can be fully tested by fetching questions, then evaluating conditional rules against sample answer sets to determine which questions should be visible, independent of the answer-saving mechanism.

**Acceptance Scenarios**:

1. **Given** a question with a conditional visibility rule (e.g., "show if household_size > 3"), **When** provided answer data meeting that condition, **Then** the question is marked as visible
2. **Given** a question with a conditional visibility rule, **When** provided answer data not meeting that condition, **Then** the question is marked as hidden
3. **Given** a question with no conditional rules, **When** evaluating visibility, **Then** the question is always marked as visible
4. **Given** multiple nested conditional rules, **When** evaluating visibility, **Then** all conditions are evaluated correctly with proper boolean logic (AND/OR)

---

### User Story 3 - Retrieve Question Details with Metadata (Priority: P2)

As a frontend developer, I need to access complete question metadata (field type, validation rules, options, help text) when rendering questions so that the UI can properly display different question types (text, select, checkbox, etc.) with appropriate validation and guidance.

**Why this priority**: While P1 stories handle the basic retrieval and conditional logic, this P2 story ensures the API provides all necessary metadata for proper UI rendering. This enables feature-complete implementation.

**Independent Test**: Can be fully tested by retrieving question definitions and verifying the presence and structure of metadata fields (type, validation, options) without integrating with the save/resume mechanism.

**Acceptance Scenarios**:

1. **Given** a text-type question, **When** retrieving question details, **Then** metadata includes input type, min/max length, and validation patterns if applicable
2. **Given** a select-type question, **When** retrieving question details, **Then** metadata includes the list of available options with labels and codes
3. **Given** a question with help text or additional guidance, **When** retrieving details, **Then** the help text is included in the response
4. **Given** a question with validation requirements, **When** retrieving details, **Then** validation rules are specified in a format that the frontend can use for client-side validation

---

### Edge Cases

- What happens when a state/program combination is deprecated or no longer supported?
- How does the system handle questions with circular conditional dependencies (e.g., Q1 visible if Q2 answered, Q2 visible if Q1 answered)?
- What occurs when question definitions are updated while a user is mid-flow - should the wizard continue with cached definitions or reload?
- How are state-specific variations handled (e.g., different question order, different options) for the same program?

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: System MUST provide an endpoint that accepts a state code and program code as parameters and returns all question definitions for that combination
- **FR-002**: System MUST include question metadata in responses including: question ID, display text, field type (text, select, checkbox, etc.), and any validation constraints
- **FR-003**: System MUST support conditional visibility rules defined as expressions that evaluate against previous answer data
- **FR-004**: System MUST return conditional visibility rules in a format that can be evaluated client-side (rule definitions, not pre-evaluated results)
- **FR-005**: System MUST validate state and program codes and return appropriate error responses (400 Bad Request) for invalid inputs
- **FR-006**: System MUST handle select/dropdown questions with option lists including both display labels and internal codes
- **FR-007**: System MUST support optional question help text, validation patterns, and field constraints in metadata
- **FR-008**: System MUST maintain backwards compatibility when question definitions are updated so that in-progress sessions can continue with current definitions
- **FR-009**: System MUST log access to question definition endpoints for audit purposes

### Constitution Compliance Requirements

- **CONST-I**: Question definition retrieval logic MUST be testable in isolation (can test conditional evaluation without database calls)
- **CONST-II**: All functional requirements (FR-001 through FR-009) MUST have corresponding unit and integration test scenarios
- **CONST-IV**: Question retrieval endpoint MUST respond within 200ms at the 95th percentile for state/program combinations with up to 500 questions

### Key Entities

- **Question**: Represents a single eligibility question with metadata including ID, text, field type, and validation rules
- **ConditionalRule**: Defines visibility logic for a question based on answers to other questions (reference to other question IDs and condition expressions)
- **QuestionOption**: Represents a selectable option for select/dropdown/checkbox type questions with label and value code
- **Program**: Represents an eligibility program (e.g., MEDI-CAL, CalFresh) with associated questions and state-specific variations

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: API endpoint returns question definitions for a state/program combination in under 200ms (95th percentile) for typical request volumes
- **SC-002**: 100% of defined state/program combinations return complete question definitions with all required metadata fields
- **SC-003**: Conditional visibility rules are correctly evaluated for 100% of test cases covering single, multiple, and nested conditions
- **SC-004**: Frontend can render all returned question types (text, select, checkbox, etc.) without additional backend calls or data transformation
- **SC-005**: API gracefully handles edge cases (invalid state codes, deprecated programs) with appropriate error responses
- **SC-006**: Audit logs capture 100% of question definition access requests for compliance and troubleshooting

## Assumptions

- State codes follow standard US state abbreviations (CA, TX, NY, etc.)
- Program codes are defined in a master list maintained separately
- Conditional visibility rules use a simple expression format that can be evaluated client-side (not Turing-complete)
- Question definitions are versioned and updates don't require schema changes to previously answered sessions
- Frontend has capability to evaluate conditional expressions client-side for visibility determination
