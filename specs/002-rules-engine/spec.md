# Feature Specification: Rules Engine & State Data (E2)

**Feature Branch**: `002-rules-engine`  
**Created**: 2026-02-09  
**Status**: Draft - Ready for Planning  
**Input**: "Rules Engine & State Data: Implement deterministic, versioned rules engine evaluating Medicaid eligibility for 5 pilot states (IL, CA, NY, TX, FL). Support MAGI, non-MAGI, SSI, aged, disability pathways. Return eligibility status, matched programs, and plain-language explanations."

---

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Basic Eligibility Evaluation (Priority: P1)

The system must evaluate a user's Medicaid eligibility based on their answers (household size, income, age, state) and return a clear yes/no result with explanation. This is the core capability that all other features depend on.

**Why this priority**: Blocks all user-facing features; cannot have an eligibility wizard without evaluation logic

**Independent Test**: Provide sample input (state=IL, household=2, income=$2000/month, age=35) → system returns eligibility status and explanation

**Acceptance Scenarios**:

1. **Given** a user in Illinois with household size 2 and monthly income $2,000, **When** the system evaluates eligibility, **Then** it returns "Likely Eligible" for MAGI Adult Medicaid with explanation "Your income ($24,000/year) is below the MAGI limit of $24,353 for a household of 2"
2. **Given** a user in California with monthly income $5,000 exceeding all thresholds, **When** evaluation is performed, **Then** system returns "Unlikely Eligible" with explanation "Your income ($60,000/year) exceeds the threshold for all California Medicaid programs"
3. **Given** the same user data evaluated twice, **When** no rules have changed between evaluations, **Then** both evaluations return identical results (deterministic requirement)
4. **Given** a user with age 66 in Texas, **When** evaluation considers age-based programs, **Then** system matches to Aged Medicaid category and applies age-specific income limits
5. **Given** incomplete user data (missing income), **When** evaluation is attempted, **Then** system returns validation error with message "Cannot evaluate eligibility: income is required"

---

### User Story 2 - Program Matching & Multi-Program Results (Priority: P1)

Users may qualify for multiple Medicaid programs simultaneously (e.g., both MAGI Adult and pregnancy-related). The system must identify all matching programs, rank by confidence, and explain each match.

**Why this priority**: Real-world eligibility is complex; users need to know ALL programs they might qualify for

**Independent Test**: User data qualifies for 2+ programs → system returns all matches with confidence scores

**Acceptance Scenarios**:

1. **Given** a pregnant 25-year-old in Florida with income at 150% FPL, **When** system evaluates eligibility, **Then** it returns matches for both "Pregnancy-Related Medicaid" (95% confidence) and "MAGI Adult" (90% confidence)
2. **Given** a 70-year-old with disability in New York with income below SSI limit, **When** evaluated, **Then** system matches "Aged Medicaid" and "Disabled Medicaid" with explanations for each
3. **Given** multiple program matches, **When** results are returned, **Then** they are sorted by confidence score descending (highest confidence first)
4. **Given** a user who is potentially eligible but with incomplete documentation assumptions, **When** evaluated, **Then** confidence scores are reduced (e.g., 75% instead of 95%) with explanation "Confidence reduced: disability status not confirmed"

---

### User Story 3 - State-Specific Rule Evaluation (Priority: P1)

Each of the 5 pilot states (IL, CA, NY, TX, FL) has different Medicaid rules, income thresholds, and program offerings. The system must apply the correct state's rules based on user selection.

**Why this priority**: Core MVP requirement; wrong state rules = incorrect eligibility determination

**Independent Test**: Same user profile evaluated in different states → different results reflecting state-specific rules

**Acceptance Scenarios**:

1. **Given** a user with identical profile (household size 3, income $35,000/year) evaluated in Illinois vs Texas, **When** system applies state-specific MAGI thresholds, **Then** results differ based on each state's FPL percentage limits (IL: 138% FPL, TX: 133% FPL, etc.)
2. **Given** a user in California requesting evaluation, **When** system loads rules, **Then** it fetches only California-specific rules and FPL adjustments
3. **Given** a state that offers a unique program (e.g., Texas Health Steps), **When** user qualifies based on age/income, **Then** system includes state-specific program in results
4. **Given** a user switches state selection mid-session, **When** re-evaluated, **Then** system applies new state's rules and previous results are replaced with state-appropriate matches

---

### User Story 4 - Plain-Language Explanation Generation (Priority: P1)

Users need to understand WHY they are or aren't eligible. The system must generate human-readable explanations without jargon, referencing actual dollar amounts and thresholds.

**Why this priority**: UX requirement per Constitution III; trust depends on transparency

**Independent Test**: Evaluation result includes plain-language explanation with concrete numbers from user's data

**Acceptance Scenarios**:

1. **Given** a user with income $2,100 and threshold $2,500, **When** result is generated, **Then** explanation includes actual values: "Your monthly income of $2,100 is below the limit of $2,500 for your household size"
2. **Given** a user who is ineligible due to income, **When** explanation is generated, **Then** it includes how much they exceed by: "Your annual income ($45,000) exceeds the limit by $5,000 for Illinois Adult Medicaid"
3. **Given** a user who qualifies due to categorical eligibility (disability), **When** explanation is generated, **Then** it states: "You qualify for Disabled Medicaid regardless of income because you receive SSI benefits"
4. **Given** technical rule terms like "MAGI" or "FPL", **When** used in explanations, **Then** they are either replaced with plain language or include inline definitions: "Modified Adjusted Gross Income (MAGI)"
5. **Given** a user denied for multiple reasons, **When** explanation is generated, **Then** system lists all disqualifying factors clearly: "You are unlikely eligible because: (1) income exceeds limit, (2) not a state resident"

---

### User Story 5 - Federal Poverty Level (FPL) Table Integration (Priority: P1)

Medicaid thresholds are based on Federal Poverty Level percentages that vary by household size and year. The system must store and correctly apply FPL tables.

**Why this priority**: FPL is foundational to income calculations; incorrect FPL = incorrect eligibility

**Independent Test**: User income is correctly compared to FPL-based threshold for their household size

**Acceptance Scenarios**:

1. **Given** a household size of 4 in 2026 and income $35,000, **When** system evaluates against 138% FPL, **Then** it uses 2026 FPL table ($31,200 for family of 4) and calculates 138% = $43,056, determining user is under threshold
2. **Given** FPL tables updated for new year, **When** evaluation uses effective date, **Then** system automatically applies correct year's table based on current date or application date
3. **Given** different household sizes (1, 2, 3, 4, 5, 6, 7, 8+), **When** FPL threshold is calculated, **Then** system uses correct per-person increment for sizes above 8
4. **Given** a state with FPL adjustments (Alaska, Hawaii have higher FPL), **When** those states are added, **Then** system supports state-specific FPL multipliers

---

### User Story 6 - Eligibility Pathway Identification (Priority: P2)

Medicaid has different eligibility pathways (MAGI, non-MAGI, SSI-linked, aged, disability, pregnancy). The system must determine which pathway(s) apply to a user based on their characteristics.

**Why this priority**: Pathway determines which rules to evaluate; necessary for accurate matching

**Independent Test**: User characteristics route to correct pathway (e.g., age 67 → Aged pathway)

**Acceptance Scenarios**:

1. **Given** a user age 35 with no disability or pregnancy, **When** system determines pathways, **Then** it evaluates MAGI Adult pathway only
2. **Given** a user age 68, **When** system determines pathways, **Then** it evaluates Aged Medicaid pathway (non-MAGI) instead of MAGI
3. **Given** a user reporting current pregnancy, **When** evaluation is performed, **Then** system evaluates both Pregnancy-Related Medicaid and standard MAGI pathways
4. **Given** a user reporting SSI receipt, **When** evaluation is performed, **Then** system applies categorical eligibility and bypasses income checks for Disabled Medicaid
5. **Given** a user with multiple applicable pathways (e.g., aged + disabled), **When** system evaluates, **Then** it checks all applicable pathways and returns best matches

---

### User Story 7 - Rule Versioning Foundation (Priority: P2)

Rules change over time due to legislation. The system must support basic versioning to track when rules were active and allow future effective-date scheduling (full versioning workflow is Phase 3).

**Why this priority**: Sets foundation for admin rule management; ensures audit trail

**Independent Test**: Rule has version number and effective date; old data evaluated with rules active at that time

**Acceptance Scenarios**:

1. **Given** a rule created on 2026-01-01, **When** stored in database, **Then** it has version number (v1.0) and effective_date (2026-01-01)
2. **Given** a rule update on 2026-03-01, **When** both old and new rules exist, **Then** evaluations before 2026-03-01 use v1.0, evaluations after use v2.0
3. **Given** an admin creates a rule with future effective date (2026-06-01), **When** evaluation runs on 2026-05-15, **Then** system uses current active rule, not future-dated rule
4. **Given** a need to audit past evaluations, **When** querying historical data, **Then** system records which rule version was used for each evaluation

---

### Edge Cases

- **User at exact income threshold**: If user's income exactly equals threshold (e.g., $2,500 and threshold = $2,500), system treats as "at or below" and marks eligible (standard Medicaid interpretation)
- **Household size = 0 or negative**: Validation rejects household size < 1 with clear error message
- **Income = 0**: System accepts $0 income as valid (user may be unemployed); evaluates against $0 threshold comparisons
- **User reports multiple states**: Validation requires single state selection; if user moves between states during application, they must select current state of residence
- **Rule conflict**: If multiple rules for same state/program have overlapping effective dates, system flags error and requires admin resolution before evaluation
- **Missing FPL table for current year**: System falls back to previous year's table and logs warning; prompts admin to update FPL data
- **Disabled user with high income**: Categorical eligibility via SSI overrides income limits; user may still qualify for Disabled Medicaid even with income above standard thresholds
- **Program has no current active rule**: System returns "Program evaluation unavailable" and logs issue for admin review
- **User enters income in annual vs monthly**: Frontend normalizes to monthly; rules evaluation assumes monthly income format; annual-to-monthly conversion documented in rules metadata

---

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: System MUST evaluate Medicaid eligibility based on user inputs (state, household size, income, age, disability status, pregnancy, citizenship) and return eligibility status (Likely Eligible, Possibly Eligible, Unlikely Eligible)
- **FR-002**: System MUST support 5 pilot states: Illinois (IL), California (CA), New York (NY), Texas (TX), Florida (FL)
- **FR-003**: System MUST evaluate eligibility across multiple pathways: MAGI (Modified Adjusted Gross Income), non-MAGI (Aged, Blind, Disabled), SSI-linked, pregnancy-related
- **FR-004**: System MUST be deterministic: identical inputs evaluated at the same point in time MUST produce identical outputs (same eligibility status, programs, explanations)
- **FR-005**: System MUST return matched Medicaid programs with confidence scores (0-100%) ranked by likelihood of eligibility
- **FR-006**: System MUST generate plain-language explanations for eligibility results that:
  - Reference user's actual data values (income amounts, thresholds)
  - Explain why user is or isn't eligible
  - Avoid jargon or define technical terms inline (e.g., "MAGI (Modified Adjusted Gross Income)")
  - Are readable at 8th-grade level or below
- **FR-007**: System MUST store and apply Federal Poverty Level (FPL) tables by year and household size; FPL thresholds MUST update annually
- **FR-008**: System MUST calculate income thresholds as percentage of FPL (e.g., 138% FPL for MAGI Adult) based on household size
- **FR-009**: System MUST apply state-specific rules; rules for one state MUST NOT affect evaluations for other states
- **FR-010**: System MUST validate user inputs before evaluation and return clear error messages for invalid data (e.g., "Household size must be at least 1")
- **FR-011**: System MUST support rule versioning with effective dates; evaluations MUST use rules active on evaluation date or application date
- **FR-012**: System MUST track which rule version was applied to each evaluation for audit purposes
- **FR-013**: System MUST support categorical eligibility (e.g., SSI recipients automatically qualify for Disabled Medicaid regardless of income)
- **FR-014**: System MUST handle multiple program matches by returning all qualifying programs, not just the first match
- **FR-015**: System MUST distinguish between "income too high" vs "other disqualifying factors" in explanations
- **FR-016**: System MUST support asset tests for non-MAGI pathways (Aged, Disabled) where applicable by state
- **FR-017**: System MUST return eligibility results in ≤2 seconds (p95) to meet Constitution IV performance requirement

### Constitution Compliance Requirements

- **CONST-I (Code Quality)**: Eligibility evaluation logic MUST be testable in isolation without database or HTTP dependencies; rules engine should accept rule objects and user data objects as pure inputs
- **CONST-II (Testing Standards)**: All functional requirements MUST have corresponding unit tests; integration tests MUST cover end-to-end evaluation flows for each pilot state; test data MUST include edge cases (exact threshold, $0 income, maximum household size)
- **CONST-III (UX Consistency)**: Plain-language explanations MUST be tested with readability tools (Flesch-Kincaid 8th grade or below); explanations MUST NOT use unexplained acronyms or jargon
- **CONST-IV (Performance)**: Eligibility evaluation MUST complete in ≤2 seconds (p95); FPL lookups MUST be cached or indexed for <10ms access time; rule evaluations MUST log performance metrics for monitoring

### Key Entities _(mandatory for this feature)_

- **EligibilityResult**:
  - `status` (enum: Likely Eligible, Possibly Eligible, Unlikely Eligible)
  - `matched_programs` (list of matched programs with confidence scores)
  - `explanation` (plain-language text)
  - `evaluation_date` (timestamp)
  - `rule_version_used` (reference to rule version)

- **MedicaidProgram**:
  - `program_id` (UUID)
  - `state_code` (VARCHAR: IL, CA, NY, TX, FL)
  - `program_name` (VARCHAR: MAGI Adult, Aged Medicaid, Disabled, Pregnancy-Related, etc.)
  - `eligibility_pathway` (enum: MAGI, Non-MAGI-Aged, Non-MAGI-Disabled, SSI-Linked, Pregnancy)
  - `description` (plain-language program summary)

- **EligibilityRule**:
  - `rule_id` (UUID)
  - `state_code` (VARCHAR)
  - `program_id` (foreign key to MedicaidProgram)
  - `rule_logic` (JSON: stores evaluation logic - conditions, thresholds, calculations)
  - `version` (DECIMAL: v1.0, v1.1, v2.0)
  - `effective_date` (DATE: when rule becomes active)
  - `end_date` (DATE: nullable, when rule is superseded)
  - `created_by` (reference to admin user)

- **FederalPovertyLevel**:
  - `fpl_id` (UUID)
  - `year` (INTEGER: 2026, 2027, etc.)
  - `household_size` (INTEGER: 1-8+)
  - `annual_amount` (DECIMAL: poverty level in dollars)
  - `state_code` (VARCHAR: nullable for state-specific adjustments like AK, HI)

- **UserEligibilityInput**:
  - `state_code` (VARCHAR)
  - `household_size` (INTEGER)
  - `monthly_income` (DECIMAL)
  - `age` (INTEGER: nullable)
  - `has_disability` (BOOLEAN: nullable)
  - `is_pregnant` (BOOLEAN: nullable)
  - `receives_ssi` (BOOLEAN: nullable)
  - `is_citizen` (BOOLEAN)
  - `asset_amount` (DECIMAL: nullable, for non-MAGI pathways)

- **ProgramMatch**:
  - `program_id` (reference to MedicaidProgram)
  - `confidence_score` (INTEGER: 0-100)
  - `explanation` (TEXT: why user matches this program)
  - `disqualifying_factors` (list: reasons if confidence < 100%)

---

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Eligibility evaluation completes in ≤2 seconds (p95) for all 5 pilot states with standard user inputs
- **SC-002**: System produces identical results when evaluating same user data multiple times (100% deterministic)
- **SC-003**: All plain-language explanations score at 8th-grade reading level or below on Flesch-Kincaid scale
- **SC-004**: System correctly evaluates 100+ test cases covering all pilot states, pathways, and edge cases without errors
- **SC-005**: FPL threshold calculations accurate to the penny for household sizes 1-8+ based on 2026 FPL tables
- **SC-006**: Multi-program scenarios return all qualifying programs (0 missed matches in test suite)
- **SC-007**: Rule versioning prevents future-dated rules from affecting current evaluations (100% compliance)
- **SC-008**: Unit test coverage ≥80% for domain logic (evaluation engine, program matching, explanation generation)
- **SC-009**: Integration tests validate end-to-end evaluation for each of 5 pilot states with real-world scenarios
- **SC-010**: System supports 1,000 concurrent eligibility evaluations without performance degradation (load testing validation)
- **SC-011**: Zero jargon terms used without inline definitions in explanations (manual review + automated checks)
- **SC-012**: Categorical eligibility (SSI) correctly bypasses income checks 100% of time for applicable programs

---

## Implementation Path Forward

**Status**: ✅ **Specification Complete - Ready for Planning**

**Next Steps**: Run `/speckit.plan` to create:
- Detailed implementation plan
- Task breakdown (T001, T002, etc.)
- Data model designs
- Rule evaluation logic architecture
- Test scenario matrices

**Dependencies**:
- Authentication & Session Management (E1) - provides session context for evaluations
- Database schema migrations
- Access to official Medicaid rules documentation for 5 pilot states

**Estimated Effort**: 4-5 weeks (per IMPLEMENTATION_PLAN.md)

**Blockers for Downstream Features**:
- E4: Eligibility Wizard (needs question taxonomy from rules)
- E5: Results Display (needs evaluation output)
- E6: Document Management (needs program matching results)
- E8: Admin Rule Editor (needs rule data model)

---

## Notes for Planning Phase

### Research Required Before Implementation

1. **Pilot State Rules Analysis**: Gather official Medicaid eligibility documentation for IL, CA, NY, TX, FL
   - MAGI income thresholds (% of FPL)
   - Non-MAGI Aged/Disabled income and asset limits
   - Pregnancy-related eligibility expansions
   - State-specific programs and categoricals

2. **FPL Tables**: Obtain official 2026 Federal Poverty Level tables from HHS
   - 48 contiguous states baseline
   - Alaska and Hawaii adjustments (if including these states later)

3. **Rule Representation Format**: Determine JSON structure for rule logic
   - Consider JSONLogic.Net or custom DSL
   - Balance between expressiveness and simplicity
   - Example rule structure for income threshold check

4. **Explanation Template Design**: Create templates for common eligibility scenarios
   - Template variables: {income}, {threshold}, {program_name}, {household_size}
   - Conditional phrases based on eligibility outcome
   - Jargon dictionary for term replacement

### Technical Decisions Required

1. **Rule Engine Library**: Evaluate options:
   - JSONLogic.Net (JSON-based rule evaluation)
   - Custom C# expression evaluator
   - Hybrid approach (simple rules in config, complex rules in code)

2. **Caching Strategy**: FPL tables and rules should be cached
   - In-memory cache with invalidation on rule updates
   - Cache lifetime: rules/FPL tables change infrequently

3. **Explanation Generation**: Template-based vs. generative
   - Start with template-based for MVP (predictable, testable)
   - Generative AI for explanations in Phase 4+

### Constitution Alignment Verification

- ✅ **Code Quality (CONST-I)**: Evaluation engine will be pure business logic with injected dependencies (no direct DB access in evaluator)
- ✅ **Testing (CONST-II)**: Each pilot state gets ≥10 test scenarios; edge cases covered; unit + integration tests planned
- ✅ **UX Consistency (CONST-III)**: Plain-language requirement explicitly defined; readability metrics will be automated
- ✅ **Performance (CONST-IV)**: 2-second SLO defined; caching strategy planned; load testing in acceptance criteria

## Post-MVP State Expansion Roadmap (Phase 11+)

**Scope**: Adding new states beyond 5 pilot states (IL, CA, NY, TX, FL) to rules engine

### Process for Adding a New State

1. **Research Phase**:
   - Gather official Medicaid eligibility documentation for target state
   - Map state-specific programs (e.g., "Texas Health Steps" unique to TX)
   - Document state-specific income thresholds and asset limits
   - Identify unique eligibility pathways (pregnant adults, veterans, etc.)

2. **Database Extension** (No schema changes; data-only updates):
   - Create MedicaidProgram records for new state (INSERT into medicaid_programs table)
   - Define EligibilityRule records with JSONLogic for each program (INSERT into eligibility_rules)
   - Add FPL adjustments if state requires multiplier (e.g., Alaska 1.25×, Hawaii 1.15×) to federal_poverty_levels table
   - Create new migration file: `AddStateXXRules.cs`

3. **Validation & Testing**:
   - Create ≥10 test scenarios for new state (edge cases, multi-program matches, asset limits)
   - Validate plain-language explanations use state-specific terminology (e.g., "Texas" not generic location)
   - Run integration tests: POST /api/rules/evaluate with new state_code returns correct results
   - Verify determinism: same input evaluated twice → identical results

4. **Quality Gates Before Production**:
   - Code review: rules approved by domain expert (Medicaid administrator)
   - Contract test: validate API responses against OpenAPI schema
   - Load test: new state doesn't degrade performance (p95 evaluation latency ≤2 sec maintained)
   - Documentation: update state list in spec.md and deployment guides

5. **Deployment**:
   - Deploy migration (creates state rules in production database)
   - Deploy updated RuleCacheService (refreshes cache to include new state)
   - Announce state availability in API documentation
   - Monitor evaluation latency and cache hit rates

### Effort Estimate per State
- Research: 1-2 weeks (gathering official documentation)
- Implementation: 2-3 days (17 rules × 5 programs per state, testing)
- Validation: 1 week (QA, domain expert review)
- **Total**: 2-3 weeks per state

### Scalability Considerations
- **Database**: Current schema supports unlimited states (no state_code enum limit)
- **Caching**: Per-state caching strategy (key: `rules:STATE_CODE`) supports future expansion without performance penalty
- **Maintenance**: Rules updated via admin portal (Phase 11+); no code deployment needed for rule logic changes
- **Governance**: Each state's rules owned by state-specific Medicaid office (future feature: per-state admin roles)
