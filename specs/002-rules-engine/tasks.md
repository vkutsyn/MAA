# Phase 2 Task Breakdown: E2 - Rules Engine & State Data

**Generated**: 2026-02-09  
**Status**: Ready for Implementation  
**Feature Branch**: `002-rules-engine`  
**Reference Docs**: [spec.md](./spec.md), [plan.md](./plan.md), [data-model.md](./data-model.md), [research.md](./research.md)

---

## Task Summary

- **Total Tasks**: 75 tasks (4 remediation additions from consistency analysis)
- **Phase 1 (Setup)**: 7 tasks (T001-T007)
- **Phase 2 (Foundational)**: 11 tasks (T008-T018)
- **Phase 3 (US1: Basic Eligibility Evaluation)**: 12 tasks (T019-T030)
- **Phase 4 (US2: Program Matching & Asset Evaluation)**: 13 tasks (T031-T043) - *Added: T031a, T034a, T036a*
- **Phase 5 (US3: State-Specific Rules)**: 8 tasks (T044-T051)
- **Phase 6 (US4: Plain-Language Explanations)**: 9 tasks (T052-T060)
- **Phase 7 (US5: FPL Integration)**: 8 tasks (T061-T068)
- **Phase 8 (US6: Eligibility Pathway Identification)**: [P] 3 tasks (T069-T071) ✓ Parallelizable with Phase 3
- **Phase 9 (US7: Rule Versioning)**: Integrated into foundational tasks
- **Phase 10 (Performance & Load Testing)**: 1 task (T075) - *Added: Critical for SC-010*

**Note on Parallelization**: 
- Tasks marked [P] can execute in parallel (different files, no dependencies on incomplete tasks)
- US3-US7 tasks show [P] parallelization opportunities within their stories
- US1-US3 must complete before US4-US7 begin (progressive integration)

---

## Phase 1: Setup & Project Initialization

- [ ] T001 Create rules engine feature branch and verify .csproj dependencies in solution
- [ ] T002 Add NuGet dependency: JSONLogic.Net to MAA.API.csproj
- [ ] T003 Create folder structure for new domain model: src/MAA.Domain/Rules/
- [ ] T004 Create folder structure for new application services: src/MAA.Application/Eligibility/
- [ ] T005 Create folder structure for new infrastructure: src/MAA.Infrastructure/Data/Rules/
- [ ] T006 Create folder structure for new controllers: src/MAA.API/Controllers/RulesController.cs location prepared
- [ ] T007 Create folder structure for new tests: src/MAA.Tests/Unit/Rules/, Integration/, Contract/

---

## Phase 2: Foundational Infrastructure (Blocking Prerequisites)

### Database Schema & Migrations

- [ ] T008 [P] Create migration InitializeRulesEngine.cs in src/MAA.Infrastructure/Migrations/ with MedicaidProgram, EligibilityRule, FPL tables (5x standard PostgreSQL design pattern from existing E1)
- [ ] T009 [P] Create migration SeedPilotStateRules.cs to seed 5 pilot states (IL, CA, NY, TX, FL) with base program definitions (~30 programs total)
- [ ] T010 [P] Create migration SeedFPLTables.cs to insert 2026 Federal Poverty Level baseline and state adjustments

### Domain Entities

- [ ] T011 [P] Create src/MAA.Domain/Rules/MedicaidProgram.cs with attributes: program_id, state_code, program_name, program_code, eligibility_pathway enum, description
- [ ] T012 [P] Create src/MAA.Domain/Rules/EligibilityRule.cs with attributes: rule_id, program_id, state_code, rule_name, version, rule_logic (JSON), effective_date, end_date, created_by, created_at, updated_at
- [ ] T013 [P] Create src/MAA.Domain/Rules/FederalPovertyLevel.cs with attributes: fpl_id, year, household_size, annual_income_cents, state_code, adjustment_multiplier
- [ ] T014 [P] Create src/MAA.Domain/Rules/Exceptions/EligibilityEvaluationException.cs custom exception

### Value Objects & DTOs

- [ ] T015 [P] Create src/MAA.Domain/Rules/ValueObjects/EligibilityStatus.cs enum (Likely Eligible, Possibly Eligible, Unlikely Eligible)
- [ ] T016 [P] Create src/MAA.Domain/Rules/ValueObjects/ConfidenceScore.cs value object (0-100 validation)
- [ ] T017 [P] Create src/MAA.Application/Eligibility/DTOs/UserEligibilityInputDto.cs with validation attributes
- [ ] T018 [P] Create src/MAA.Application/Eligibility/DTOs/EligibilityResultDto.cs and ProgramMatchDto.cs with nested structure

---

## Phase 3: US1 - Basic Eligibility Evaluation (P1)

**Story Goal**: System must evaluate a user's Medicaid eligibility based on household size, income, age, state and return clear yes/no result with explanation

**Independent Test Criteria**: 
- Provide sample input (state=IL, household=2, income=$2000/month) → returns eligibility status (Likely/Possibly/Unlikely) with explanation
- Same input evaluated twice → identical output (determinism)
- Invalid input (household_size=0) → validation error "Household size must be at least 1"

**Tests Included**: ✅ Full unit, integration, and contract test coverage

### Core Evaluation Logic

- [ ] T019 Create src/MAA.Domain/Rules/RuleEngine.cs pure function: evaluate(rule: EligibilityRule, input: UserEligibilityInput) → EvaluationResult (no I/O, no dependencies)
- [ ] T020 [P] Create src/MAA.Domain/Rules/FPLCalculator.cs pure function: calculateThreshold(fplAmount: decimal, percentage: int, householdSize: int) → decimal (no database access)
- [ ] T021 [P] Create src/MAA.Application/Eligibility/Handlers/EvaluateEligibilityHandler.cs orchestrator that fetches rule, calls RuleEngine, returns result
- [ ] T022 [P] Create src/MAA.Application/Eligibility/Validators/EligibilityInputValidator.cs with FluentValidation rules: state_code in [IL,CA,NY,TX,FL], household_size 1-8, is_citizen required
- [ ] T023 [P] Create src/MAA.Infrastructure/Data/Rules/RuleRepository.cs with methods: GetActiveRuleByProgram(stateCode, programId), GetRulesByState(stateCode)
- [ ] T024 [P] Create src/MAA.Infrastructure/Data/Rules/FPLRepository.cs with methods: GetFPLByYearAndHouseholdSize(year, size), GetFPLForState(year, size, stateCode)
- [ ] T025 [P] Create src/MAA.Infrastructure/Caching/RuleCacheService.cs in-memory cache: rules cached for 1 hour with manual invalidation on update

### Unit Tests for US1

- [ ] T026 Create src/MAA.Tests/Unit/Rules/RuleEngineTests.cs with 8+ test cases (UNIT LAYER: in-memory, no DB/HTTP):
  - Income below threshold → Likely Eligible
  - Income above threshold → Unlikely Eligible
  - Income exactly at threshold → Likely Eligible
  - **Determinism test (unit layer)**: Same user data evaluated twice in-memory → identical output (validates RuleEngine pure function)
  - Missing required field → throws validation exception
  - $0 income → evaluated against $0 threshold

- [ ] T027 [P] Create src/MAA.Tests/Unit/Rules/FPLCalculatorTests.cs with 6+ test cases:
  - Calculate 138% FPL for household of 2 (2026)
  - Calculate 138% FPL for household of 8+ (per-person increment)
  - Exact threshold matching
  - FPL lookup with null state code (baseline)

- [ ] T028 [P] Create src/MAA.Tests/Unit/Eligibility/EligibilityEvaluatorTests.cs with 5+ test cases:
  - End-to-end evaluation (fetch rule, calculate threshold, evaluate)
  - Invalid state code → throws exception
  - Missing rule for program → throws exception with helpful message

### Integration Tests for US1

- [ ] T029 Create src/MAA.Tests/Integration/RulesApiIntegrationTests.cs with 8+ test cases (INTEGRATION LAYER: via HTTP with real database):
  - Evaluate with IL rules via HTTP POST /api/rules/evaluate
  - Evaluate with CA rules via HTTP POST /api/rules/evaluate
  - Evaluate with TX rules (different threshold) → different result
  - Missing rule for state → 404 Not Found
  - Invalid JSON → 400 Bad Request
  - Performance: evaluation completes in <2 seconds (p95)
  - **Determinism test (integration layer)**: Same user data submitted twice via HTTP with database persistence → identical output (validates end-to-end system determinism across layers)

### Contract Tests for US1

- [ ] T030 Create src/MAA.Tests/Contract/RulesApiContractTests.cs validating API against OpenAPI spec:
  - POST /api/rules/evaluate request body matches UserEligibilityInputDto schema
  - POST /api/rules/evaluate response matches EligibilityResultDto schema
  - All required fields present in request and response
  - Status codes: 200 (success), 400 (invalid input), 404 (state not found)

---

## Phase 4: US2 - Program Matching & Multi-Program Results (P1)

**Story Goal**: For users qualifying for multiple programs, system identifies all matches, ranks by confidence, and explains each

**Independent Test Criteria**:
- User data qualifying for 2+ programs → returns all matches with confidence scores
- Results sorted by confidence descending
- Each match includes program-specific explanation

**Tests Included**: ✅ Full unit, integration, and contract test coverage

### Asset Evaluation Logic (CRITICAL FOR FR-016, SC-006)

- [ ] T031a [P] Create src/MAA.Domain/Rules/AssetEvaluator.cs pure function: EvaluateAssets(assets: decimal, pathway: EligibilityPathway, state: string, currentYear: int) → (isEligible: bool, reason: string) for non-MAGI Aged/Disabled asset limits per state

### Multi-Program Matching Logic

- [ ] T031 Create src/MAA.Domain/Rules/ProgramMatcher.cs: findMatchingPrograms(input: UserEligibilityInput, allPrograms: List<EligibilityRule>) → List<ProgramMatch> (pure function)
- [ ] T032 [P] Create src/MAA.Application/Eligibility/Handlers/ProgramMatchingHandler.cs that orchestrates: fetch all rules for state, evaluate each, collect matches, sort by confidence
- [ ] T033 [P] Create src/MAA.Domain/Rules/ConfidenceScorer.cs pure function: scoreConfidence(matchingFactors: List<string>, disqualifyingFactors: List<string>) → int (0-100) and confidence level

### Unit Tests for US2 & Asset Evaluation

- [ ] T034a [P] Create src/MAA.Tests/Unit/Rules/AssetEvaluatorTests.cs with 6+ test cases:
  - Assets below state limit for Aged pathway → eligible
  - Assets at state limit for Aged pathway → eligible
  - Assets above state limit for Aged pathway → ineligible with reason "Assets exceed $X limit for Aged Medicaid"
  - Different state (IL vs CA) has different limits → both evaluated correctly
  - Assets at boundary value (exact limit) → eligible

- [ ] T034 Create src/MAA.Tests/Unit/Rules/ProgramMatcherTests.cs with 8+ test cases:
  - User qualifies for MAGI Adult only → 1 match returned
  - User qualifies for MAGI Adult + Pregnancy-Related → 2 matches, sorted by confidence (95%, 85%)
  - User qualifies for multiple pathways (Aged + Disabled) → all matches returned
  - No programs match → empty list returned (not null)
  - Confidence scores: high (95%) for certain match, medium (75%) for uncertain, low (25%) for possible

- [ ] T035 [P] Create src/MAA.Tests/Unit/Rules/ConfidenceScorerTests.cs with 6+ test cases:
  - All factors present → 95% confidence
  - Missing one factor → 75% confidence
  - Multiple disqualifying factors → 25% confidence
  - Categorical eligibility (SSI) → 95% even without income verification

### Integration Tests for US2 & Asset Evaluation

- [ ] T036a [P] Create src/MAA.Tests/Integration/RulesApiIntegrationTests.cs (extension) with 3+ test cases for asset evaluation:
  - 70-year-old Aged pathway with assets below state limit → eligible
  - 65-year-old Disabled pathway with assets exceeding limit → ineligible, explanation includes asset reason
  - Asset test failure supersedes income eligibility (disqualifying factor)

- [ ] T036 Create src/MAA.Tests/Integration/RulesApiIntegrationTests.cs (extension) with 6+ test cases:
  - Pregnant 25-year-old with income at 150% FPL → matches MAGI Adult + Pregnancy-Related
  - 70-year-old with disability → matches Aged + Disabled Medicaid
  - Results sorted by confidence descending
  - Each match includes explanation specific to program

### Contract Tests for US2

- [ ] T037 Create src/MAA.Tests/Contract/RulesApiContractTests.cs (extension) validating asset evaluation in contract:
  - disqualifying_factors array includes asset-related reasons where applicable

- [ ] T038 Create src/MAA.Tests/Contract/RulesApiContractTests.cs (extension) validating:
  - matched_programs array present in response
  - Each object in array includes: program_id, program_name, confidence_score, explanation
  - confidence_score is 0-100 integer
  - Results sorted by confidence_score descending

---

## Phase 5: US3 - State-Specific Rule Evaluation (P1)

**Story Goal**: Each of 5 pilot states has different rules; system applies correct state's rules based on user selection

**Independent Test Criteria**:
- Same user profile evaluated in IL vs TX → different results (reflecting state-specific thresholds)
- State selection persists through evaluation → no cross-state rule mixing

### State-Specific Integration

- [ ] T044 Create src/MAA.Application/Eligibility/Services/StateRuleLoader.cs: loadRulesForState(stateCode: string) → List<EligibilityRule> (with cache)
- [ ] T045 [P] Create src/MAA.Infrastructure/Data/Rules/StateRuleRepository.cs: GetAllProgramsForState(stateCode), GetProgramsWithRules(stateCode, effectiveDate)
- [ ] T046 [P] Extend RuleCacheService with state-scoped cache: rules cached by state_code key, invalidated per-state

### Unit Tests for US3

- [ ] T047 Create src/MAA.Tests/Unit/Eligibility/StateRuleLoaderTests.cs with 6+ test cases:
  - Load IL rules → only IL programs returned
  - Load CA rules → only CA programs returned
  - Invalid state code → throws exception with "State not found" message
  - Cache hit: second call returns from cache

### Integration Tests for US3

- [ ] T048 Create src/MAA.Tests/Integration/RulesApiIntegrationTests.cs (extension) with 6+ test cases:
  - Same user (household 3, $35k/year) in IL → passes threshold
  - Same user in TX (different threshold: 133% vs 138% FPL) → different result
  - CA evaluation uses only CA programs
  - Switching states mid-session → new evaluation uses new state rules, old results replaced

### Contract Tests for US3

- [ ] T049 [P] Create state-specific test data: src/MAA.Tests/Data/pilot-states-test-cases.json with IL, CA, NY, TX, FL scenarios
- [ ] T050 [P] Extend RulesApiContractTests.cs validating state_code parameter validation in requests

---

## Phase 6: US4 - Plain-Language Explanation Generation (P1)

**Story Goal**: Users understand WHY they are/aren't eligible with jargon-free explanations using concrete numbers

**Independent Test Criteria**:
- Explanation includes actual user data values (income $2,100, threshold $2,500)
- No unexplained jargon; MAGI → "Modified Adjusted Gross Income (MAGI)"
- Reading level ≤ 8th grade (Flesch-Kincaid automated check)

### Explanation Generation Logic

- [ ] T051 Create src/MAA.Domain/Rules/ExplanationGenerator.cs pure function with multiple methods:
  - GenerateEligibilityExplanation(result: EligibilityResult, input: UserEligibilityInput) → string
  - GenerateProgramExplanation(program: ProgramMatch, userIncome: decimal, threshold: decimal) → string
  - GenerateDisqualifyingFactorsExplanation(factors: List<string>) → string
- [ ] T052 [P] Create src/MAA.Domain/Rules/JargonDefinition.cs static dictionary mapping: MAGI → "Modified Adjusted Gross Income", FPL → "Federal Poverty Level", SSI → "Social Security Income" (≥10 acronyms minimum)
- [ ] T053 [P] Create src/MAA.Application/Eligibility/Validators/ReadabilityValidator.cs: ScoreReadability(text: string) → FleschKincaidScore, IsBelow8thGrade() method

### Unit Tests for US4

- [ ] T054 Create src/MAA.Tests/Unit/Rules/ExplanationGeneratorTests.cs with 10+ test cases:
  - Income below threshold → explanation includes actual values: "Your monthly income of $2,100 is below the limit of $2,500"
  - Income above threshold → includes overage: "Your annual income ($45,000) exceeds the limit by $5,000"
  - Categorical eligibility (SSI) → explains bypass: "You qualify for Disabled Medicaid because you receive SSI benefits"
  - Pregnancy as qualifying factor → explains route
  - Multiple disqualifying factors → lists all factors clearly: "(1) income exceeds limit, (2) not state resident"
  - Jargon test: no unexplained acronyms in output

- [ ] T055 [P] Create src/MAA.Tests/Unit/Rules/JargonDefinitionTests.cs with 4+ test cases:
  - MAGI maps to correct definition
  - All acronyms in specification have definitions
  - Definition lookup returns formatted string with term + definition

- [ ] T056 [P] Create src/MAA.Tests/Unit/Eligibility/ReadabilityValidatorTests.cs with 5+ test cases:
  - Simple explanation → scores 8th grade or below
  - Complex explanation → requires simplification
  - ReadabilityValidator flags explanations exceeding target

### Integration Tests for US4

- [ ] T057 Create src/MAA.Tests/Integration/RulesApiIntegrationTests.cs (extension) with 6+ test cases:
  - Evaluate IL scenario → explanation includes concrete income values
  - Evaluate pregnancy scenario → includes pregnancy-specific explanation
  - Evaluate SSI scenario → explains categorical eligibility bypass
  - All explanations scored ≤ 8th grade reading level

### Contract Tests for US4

- [ ] T058 [P] Extend RulesApiContractTests.cs validating explanation field:
  - explanation field present in all responses
  - explanation is non-empty string
  - explanation does not contain unexplained jargon

---

## Phase 7: US5 - Federal Poverty Level (FPL) Table Integration (P1)

**Story Goal**: System stores and correctly applies FPL tables (updated annually) to calculate income thresholds

**Independent Test Criteria**:
- User income correctly compared to FPL-based threshold
- FPL lookup for household size 1-8+ accurate to penny
- System switches to new year's FPL automatically

### FPL Management

- [ ] T059 Create src/MAA.Application/Eligibility/Services/FPLThresholdCalculator.cs: CalculateThreshold(fplAmount: decimal, percentage: int) → decimal, GetFPLForYear(year: int, householdSize: int, stateCode?: string) → FPL using per-person increment formula for household 8+
- [ ] T060 [P] Extend FPLRepository.cs with: GetCurrentYearFPL(householdSize, stateCode), GetFPLRange(year, householdSizes: List<int>), HasFPLForYear(year)
- [ ] T061 [P] Create src/MAA.Infrastructure/Caching/FPLCacheService.cs: In-memory cache for FPL tables with 1-year TTL, refresh on year boundary

### Unit Tests for US5

- [ ] T062 Create src/MAA.Tests/Unit/Eligibility/FPLThresholdCalculatorTests.cs with 10+ test cases:
  - Calculate 138% FPL for household 4 with 2026 baseline → uses correct amount
  - Calculate for household 8+ with per-person increment → correct calculation
  - Alaska adjustment (1.25x multiplier) → correct adjusted amount
  - Hawaii adjustment (1.15x multiplier) → correct adjusted amount
  - Edge case: household size exact at boundary (8 vs 8+)
  - Missing FPL for year → throws exception with helpful message
  - Precision test: calculation accurate to penny ($X.XX)

### Integration Tests for US5

- [ ] T063 Create src/MAA.Tests/Integration/RulesApiIntegrationTests.cs (extension) with 7+ test cases:
  - Household 4 with income $35k vs 138% FPL threshold → correct eligibility determination
  - Household 1 evaluation uses correct 2026 FPL (baseline)
  - Alaska state evaluation uses adjusted FPL (1.25x)
  - Boundary test: income exactly at FPL percentage → marked eligible
  - FPL cache hit: second lookup same household/year → uses cache
  - Performance: FPL lookup completes in <10ms

### Contract Tests for US5

- [ ] T064 [P] Create src/MAA.Tests/Data/fpl-2026-test-data.json with 8 household sizes (1-8+) and baseline FPL amounts
- [ ] T065 [P] Extend RulesApiContractTests.cs validating FPL endpoint: GET /api/fpl?year=2026&state_code=IL returns correctly structured FPL data

---

## Phase 8: US6 - Eligibility Pathway Identification (P2)

**Story Goal**: System determines which eligibility pathway(s) apply (MAGI, non-MAGI, SSI, aged, disability, pregnancy) based on user characteristics

**Independent Test Criteria**:
- User age 35, no disability → routes to MAGI pathway only
- User age 68 → routes to Aged pathway
- User reporting pregnancy → evaluates Pregnancy-Related pathway

### Eligibility Pathway Detection Logic

- [ ] T066 [P] Create src/MAA.Domain/Rules/PathwayIdentifier.cs pure function: DetermineApplicablePathways(input: UserEligibilityInput) → List<EligibilityPathway> enum
- [ ] T067 [P] Create src/MAA.Domain/Rules/PathwayRouter.cs: RouteToPrograms(pathways: List<EligibilityPathway>, state: string) → List<MedicaidProgram> (filter programs by pathway)
- [ ] T068 [P] Create src/MAA.Application/Eligibility/Services/PathwayEvaluationService.cs orchestrator integrating pathway identification with rule evaluation

### Unit Tests for Phase 8

- [ ] T069 Create src/MAA.Tests/Unit/Rules/PathwayIdentifierTests.cs with 8+ test cases:
  - Age 35, no disability → MAGI pathway
  - Age 68 → Aged pathway (non-MAGI)
  - Pregnancy reported → Pregnancy-Related pathway + MAGI
  - SSI receipt reported → SSI-Linked pathway
  - Disability reported + working age → Disabled pathway
  - Multiple pathways: age 68 + disability → Aged + Disabled pathways
  - Age boundary: 64 vs 65 year old → different pathway routing

### Integration Tests for Phase 8

- [ ] T070 Create src/MAA.Tests/Integration/RulesApiIntegrationTests.cs (extension) with 5+ test cases:
  - 35-year-old evaluation → MAGI rules applied
  - 68-year-old evaluation in IL → Aged Medicaid rules applied (higher asset limits, different income calculation)
  - Pregnant 25-year-old → both MAGI and Pregnancy pathways evaluated, results sorted

### Contract Tests for Phase 8

- [ ] T071 [P] Extend RulesApiContractTests.cs validating eligibility pathway field in matched_programs responses

---

## Phase 9: US7 - Rule Versioning Foundation (P2)

**Story Goal**: Basic versioning foundation set; track when rules were active and allow future effective-date scheduling

**Integration Note**: **Implemented across foundational and all user story phases** via:
- T008: Migration includes version + effective_date + end_date columns
- T012: EligibilityRule entity includes version, effective_date, end_date
- T023: RuleRepository.GetActiveRuleByProgram filters by effective_date
- T029, T048, T057, T063: Integration tests verify rule version recorded in evaluations

### Unit Tests for Versioning

- [ ] T072 Create src/MAA.Tests/Unit/Rules/RuleVersioningTests.cs with 8+ test cases:
  - Rule with effective_date in past → marked as active
  - Rule with effective_date in future → not used for current evaluation
  - Rule with end_date in past → marked as superseded, not used
  - Future-dated rule does not affect current evaluation (state=IL, two rules with different effective dates)
  - Historical evaluation with old rule versions: query previous evaluation → returns rule version that was active at that time
  - Rule version field populated correctly (v1.0, v1.1, v2.0)
  - Audit trail: evaluation result includes rule_version_used

### Integration Tests for Versioning

- [ ] T073 Create src/MAA.Tests/Integration/RulesApiIntegrationTests.cs (extension) with 5+ test cases:
  - Create rule v1.0 effective 2026-01-01, evaluate on 2026-01-15 → uses v1.0
  - Create rule v2.0 effective 2026-06-01, evaluate on 2026-05-15 → still uses v1.0
  - Query rule versioning metadata: GET /api/rules?state=IL&program=MAGI_ADULT returns all versions with dates

### Contract Tests for Versioning

- [ ] T074 [P] Extend RulesApiContractTests.cs validating:
  - rule_version_used field populated in EligibilityResultDto
  - EligibilityRule response includes version, effective_date, end_date fields

---

## Phase 10: Performance & Load Testing (CRITICAL)

**Story Goal**: Validate system meets CONST-IV performance targets (SC-010) under production-like load

### Load Testing

- [ ] T075 Create load test script (k6 or Apache JMeter) targeting 1,000 concurrent POST /api/rules/evaluate requests:
  - Target: ≤2 seconds (p95) latency per evaluation
  - Ramp-up: 100 users/sec for 30 seconds (reach 1,000 concurrent)
  - Duration: 5 minutes sustained load
  - Success criteria: 0 errors, p95 latency ≤2 sec, p99 < 3 sec
  - Measure: Cache hit rates (rules, FPL), database query performance, thread pool utilization
  - Results: Generate performance report showing bottleneck analysis
  - Link to: SC-010, CONST-IV (Performance requirement)

---

## Dependencies & Completion Order

### Blockers (Must complete before user-facing features)

1. **Phase 1 Setup** → All phases depend on this
2. **Phase 2 Foundational** → All user story phases depend on database schema and entities
3. **Phase 3 (US1)** → Must complete before Phase 4-7; enables basic evaluation
4. **Phase 4-7 (US2-US5)** → Can start in parallel after Phase 3, but Phase 5 (FPL) should complete early to support all evaluations
5. **Phase 8-9 (US6-US7)** → Integration with Phase 3-7 outcomes; lower priority P2

### Parallelization Opportunities Within Stories

- **Phase 3**: T019-T025 (logic creation) can execute in parallel; T026-T030 (tests) in parallel
- **Phase 4**: T031-T033 (logic) parallel; T034-T043 (tests) parallel; T031a asset logic parallel before T031-T033 starts
- **Phase 5-7**: Similar pattern; logic tasks parallel, tests parallel
- **Phase 8**: T066-T068 logic tasks fully parallel (no interdependencies)
- **Phase 10**: T075 load testing runs after Phase 3-9 baseline complete (validates all integration)

### Cross-Story Dependencies

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundational: Entities, Migrations, DTOs)
    ↓
Phase 3 (US1: Basic Evaluation) ←→ Phase 5 (US5: FPL) - both needed for accurate thresholds
    ↓
Phase 4 (US2: Multi-Program) + Asset Evaluation - depends on US1 core logic
    ↓
Phase 6 (US4: Explanations) - depends on US1-US2 results
Phase 8 (US6: Eligibility Pathways) - can start after Phase 3, improves routing
Phase 9 (US7: Versioning) - integrated into all phases
    ↓
Phase 10 (Performance & Load Testing) - runs after all integration complete, validates SC-010
```

---

## Success Criteria (Phase 10 Complete)

- ✅ All 75 tasks completed and committed to 002-rules-engine branch (4 remediation additions from consistency analysis)
- ✅ Core evaluation logic (RuleEngine, AssetEvaluator, FPLCalculator, ProgramMatcher) pure functions with 0 dependencies
- ✅ All 7 user stories have independent test criteria verified
- ✅ Unit test coverage ≥80% for domain logic (rules, calculations, matching, assets)
- ✅ Integration tests cover all 5 pilot states with real-world scenarios
- ✅ Contract tests validate API against OpenAPI spec (4 endpoints, all DTOs)
- ✅ Performance targets met: eligibility evaluation ≤2 seconds (p95), FPL lookup <10ms, 1000 concurrent load test passing
- ✅ Constitutional compliance: Code Quality ✅, Testing ✅, UX ✅, Performance ✅ (all verified via SC-001-SC-012)
- ✅ Documentation complete: Explanations readable at 8th-grade level, no unexplained jargon
- ✅ Asset evaluation logic complete for non-MAGI Aged/Disabled pathways (FR-016, SC-006)
- ✅ Ready for Phase 11: Production deployment (E1 + E2 integration, MVP launch)

---

## Phase 0: Research Prerequisites (BLOCKING GATE - Must Complete Before Phase 1 Starts)

**Status**: ✅ COMPLETE - 2026-02-09  
**Deliverables Gate**: All 4 research items complete. Phase 1 ready to begin.

**Research Task 1**: ✅ Gather official Medicaid eligibility documentation for 5 pilot states (IL, CA, NY, TX, FL)
- Status: COMPLETE
- Deliverable: [phase-0-deliverables/R1-pilot-state-rules-2026.md](./phase-0-deliverables/R1-pilot-state-rules-2026.md)
- Summary: Comprehensive rules for IL, CA, NY, TX, FL including MAGI/Non-MAGI pathways, income thresholds, categorical eligibility, asset limits

**Research Task 2**: ✅ Obtain 2026 FPL tables from HHS (baseline + state adjustments)
- Status: COMPLETE
- Deliverable: [phase-0-deliverables/fpl-2026-test-data.json](./phase-0-deliverables/fpl-2026-test-data.json)
- Summary: 2026 FPL schema with household sizes 1-8+, common thresholds (138%, 150%, 160%, 200%, 213% FPL), state adjustments for AK/HI

**Research Task 3**: ✅ Finalize rule engine library decision (JSONLogic.Net vs custom DSL)
- Status: COMPLETE - JSONLogic.Net RECOMMENDED
- Deliverable: [phase-0-deliverables/R3-rule-engine-library-decision.md](./phase-0-deliverables/R3-rule-engine-library-decision.md)
- Summary: Evaluation matrix comparing JSONLogic.Net (WINNER) vs Custom C# DSL. Recommendation based on admin editability, determinism, performance, time-to-market

**Research Task 4**: ✅ Design explanation templates with jargon dictionary
- Status: COMPLETE
- Deliverable: [phase-0-deliverables/R4-explanation-templates-jargon-dictionary.md](./phase-0-deliverables/R4-explanation-templates-jargon-dictionary.md)
- Summary: 5 explanation templates (Likely Eligible, Possibly Eligible, Unlikely Eligible, Categorical, Multi-Program), 12-term jargon dictionary, readability guidelines (Flesch-Kincaid ≤8th grade)

---

## Next Steps (After Phase 0 - NOW READY)

1. **✅ Phase 0 Research Execution COMPLETE** (Finished 2026-02-09):
   - ✅ Research Task 1: Medicaid rules for IL, CA, NY, TX, FL (DELIVERED)
   - ✅ Research Task 2: 2026 FPL tables from HHS (DELIVERED)
   - ✅ Research Task 3: Rule engine library decision - JSONLogic.Net selected (DELIVERED)
   - ✅ Research Task 4: Explanation templates and jargon dictionary (DELIVERED)
   - **STATUS**: All research prerequisites satisfied. Phase 1 READY TO BEGIN.

2. **Immediate Next**: Begin Phase 1 (Setup & Project Initialization, T001-T007)
   - Verify dependencies in .csproj files
   - Create folder structure per plan.md
   - Add JSONLogic.Net NuGet package (per R3 recommendation)

3. **Phase 2+ Seeding** (After Phase 1 setup complete):
   - Populate MedicaidProgram with 5 states × 6+ programs = 30+ programs
   - Populate EligibilityRule with JSONLogic rule definitions per R1
   - Seed FPL tables with R2 data (2026 baseline + AK/HI adjustments)
   - Validate seeding with sample evaluations from R4 templates

4. **System Integration** (After E2 complete):
   - Integrate E2 eligibility engine with E1 session context
   - Add evaluation results to Session.SessionData
   - Wire E2 API into MAA.API Pipeline (register services, middleware, routes)
   - Full system integration tests (E1 + E2)

5. **MVP Release** (After E2 + E3 Document Storage complete):
   - Implement E4 Eligibility Wizard (uses E2 engine for routing questions)
   - Implement E5 Results Display (shows E2 evaluation output + E3 document management)
   - Launch MVP to pilot user group (public beta)

