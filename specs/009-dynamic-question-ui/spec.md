# Feature Specification: Dynamic Eligibility Question UI

**Feature Branch**: `009-dynamic-question-ui`  
**Created**: February 10, 2026  
**Status**: Done (skipped T039, T040, T045, T046, T048 per /speckit.close)  
**Input**: User description: "Dynamic Eligibility Question UI - Render questions dynamically, support conditional questions, include 'Why we ask this' tooltips"

## User Scenarios & Testing _(mandatory)_

### User Story 1 - View and Answer Basic Questions (Priority: P1)

A user navigates to the eligibility wizard and sees a series of questions that they need to answer to determine their Medicaid eligibility. Questions are presented one at a time or in logical groups, with clear labels and input fields appropriate to the question type.

**Why this priority**: This is the foundation of the entire eligibility determination process. Without the ability to view and answer questions, no eligibility assessment can occur.

**Independent Test**: Can be fully tested by loading the wizard page, verifying questions render from API data, entering answers, and confirming answers are captured correctly. Delivers a functional questionnaire interface.

**Acceptance Scenarios**:

1. **Given** the user has started an eligibility session, **When** they navigate to the wizard, **Then** they see the first question or group of questions rendered with appropriate input controls
2. **Given** a question is displayed, **When** the user enters a valid answer, **Then** the answer is captured and the user can proceed to the next question
3. **Given** a question has multiple choice options, **When** the user views the question, **Then** all available options are displayed clearly with radio buttons or checkboxes as appropriate
4. **Given** a question requires text input, **When** the user types their response, **Then** the input is captured and displayed back to them in real-time
5. **Given** the user has answered a question, **When** they return to a previous question, **Then** their previously entered answer is displayed

---

### User Story 2 - Conditional Questions Based on Previous Answers (Priority: P2)

As a user answers questions, additional questions may appear or disappear based on their responses. For example, if a user indicates they are pregnant, pregnancy-related questions should appear. If they answer "no" to having children, child-related questions should not be shown.

**Why this priority**: This ensures users only answer relevant questions, reducing cognitive load and completion time. It's essential for a good user experience but the questionnaire can function without it.

**Independent Test**: Can be tested by answering a question that triggers conditional logic and verifying that dependent questions appear/disappear correctly. Delivers adaptive questionnaire flow.

**Acceptance Scenarios**:

1. **Given** a user answers a trigger question with a value that matches a condition, **When** the page updates, **Then** conditional questions that depend on that answer appear below
2. **Given** a user has answered a trigger question and seen conditional questions, **When** they change their answer to a value that doesn't match the condition, **Then** the conditional questions are hidden or removed
3. **Given** multiple questions have conditional dependencies, **When** the user answers questions, **Then** the correct set of questions is displayed based on all answered conditions
4. **Given** a conditional question has been answered and then hidden, **When** it becomes visible again, **Then** the previously entered answer is retained
5. **Given** a user submits their answers, **When** the data is processed, **Then** only answers to visible questions are included in the final submission

---

### User Story 3 - Contextual Help via Tooltips (Priority: P3)

Users may not understand why certain personal information is being requested. Each question includes a "Why we ask this" tooltip or help icon that users can click or hover over to see an explanation of why the information is needed and how it will be used.

**Why this priority**: This builds trust and transparency but is not critical to core functionality. Users can still complete the questionnaire without tooltips.

**Independent Test**: Can be tested by clicking/hovering on help icons and verifying explanatory text appears. Delivers enhanced user understanding and trust.

**Acceptance Scenarios**:

1. **Given** a question is displayed, **When** the user views the question, **Then** a help icon or "Why we ask this" indicator is visible next to or near the question text
2. **Given** a help icon is visible, **When** the user clicks or hovers over it, **Then** a tooltip or popover displays the explanation text
3. **Given** a tooltip is open, **When** the user clicks outside of it or clicks the icon again, **Then** the tooltip closes
4. **Given** multiple questions are visible, **When** the user opens a tooltip for one question, **Then** only that question's tooltip is displayed
5. **Given** a tooltip is displayed, **When** the user is using a screen reader, **Then** the tooltip content is announced appropriately

---

### Edge Cases

- What happens when a user's answer to a trigger question is cleared/deleted? (Conditional questions should be hidden unless a default "unanswered" state triggers them)
- How does the system handle circular dependencies in conditional logic? (Should be prevented at question definition time, not runtime)
- What happens when a question has both conditional logic AND validation rules that conflict? (Validation only applies to visible questions)
- How does the system handle rapid answer changes that trigger/untrigger conditions quickly? (Should debounce or queue condition checks to avoid flickering)
- What happens when a user navigates backward to a question that triggered conditional questions they've already answered? (Preserve answers unless the condition is invalidated)
- How are conditional questions handled for keyboard-only users? (Focus management must move appropriately when questions appear/disappear)
- What happens if question definitions are malformed or missing required fields? (System should log error and display fallback message, not crash)

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: System MUST render questions dynamically from question definition data provided by the API
- **FR-002**: System MUST support multiple question types including text input, number input, date picker, single-select (radio), multi-select (checkbox), and dropdown
- **FR-003**: System MUST evaluate conditional logic rules after each answer change and show/hide dependent questions accordingly
- **FR-004**: System MUST display a "Why we ask this" help indicator for each question that has explanatory text defined
- **FR-005**: System MUST show tooltip or popover content when the user interacts with the help indicator
- **FR-006**: System MUST validate user input according to question validation rules before allowing progression
- **FR-007**: System MUST preserve user answers when navigating forward and backward through the questionnaire
- **FR-008**: System MUST only submit answers for questions that were visible when answered
- **FR-009**: System MUST display questions in the order specified by the question definition data
- **FR-010**: System MUST provide clear visual feedback when conditional questions appear or disappear
- **FR-011**: System MUST handle focus management appropriately when questions are dynamically added or removed
- **FR-012**: System MUST support keyboard navigation through all question types and help indicators

### Constitution Compliance Requirements

- **CONST-I**: All conditional logic evaluation functions MUST be testable in isolation without UI components
- **CONST-II**: All functional requirements MUST have corresponding test scenarios (unit tests for logic, E2E tests for user flows)
- **CONST-III**: User-facing UI MUST support WCAG 2.1 AA accessibility including keyboard navigation, screen reader support, sufficient color contrast, and focus indicators
- **CONST-IV**: Question rendering MUST complete within 1 second even for complex question sets with multiple conditional branches

### Key Entities _(include if feature involves data)_

- **Question**: Represents a single eligibility question with properties including unique identifier, question text, question type, validation rules, help text, and conditional display rules
- **Condition**: Rule that determines whether a question should be displayed, based on answers to other questions (includes trigger question ID, comparison operator, and expected value)
- **Answer**: User's response to a specific question, linked to both the question ID and the current session
- **ValidationRule**: Specification for validating user input (e.g., required, min/max length, pattern matching, date range)

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Users can complete a full eligibility questionnaire with all visible questions answered within 10 minutes on average
- **SC-002**: Question rendering completes within 1 second of page load for question sets up to 50 questions
- **SC-003**: Conditional questions appear within 200 milliseconds of the triggering answer being entered
- **SC-004**: 95% of users successfully submit valid questionnaires on their first attempt without validation errors blocking submission
- **SC-005**: Tooltip interactions have zero impact on form completion time (users should not be slowed down by tooltip loading)
- **SC-006**: 100% of questions and help tooltips are accessible via keyboard-only navigation
- **SC-007**: Zero runtime errors occur when rendering valid question definition data from the API
- **SC-008**: Users who trigger conditional questions and then change their trigger answers experience smooth transitions with no visual glitches or flickering
