# Implementation Plan: E2 - Rules Engine & State Data

**Branch**: `002-rules-engine` | **Date**: 2026-02-09 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-rules-engine/spec.md`

## Summary

Implement a deterministic, versioned rules engine that evaluates Medicaid eligibility for 5 pilot states (IL, CA, NY, TX, FL) based on user inputs (household size, income, age, pathway). The engine must return eligibility status, matched programs with confidence scoring, and plain-language explanations. Foundation for all downstream user-facing features (Wizard, Results, Document Management, Admin Rule Editor).

**Key Requirements**: Deterministic evaluation, multi-pathway support (MAGI, non-MAGI, SSI, aged, disability, pregnancy), FPL integration, rule versioning with effective dates, 8th-grade reading level explanations, ≤2 second evaluation time (p95).

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: ASP.NET Core 10, EF Core 10, Npgsql.EntityFrameworkCore.PostgreSQL, JSONLogic.Net, Azure.Identity, Azure.Security.KeyVault.Secrets  
**Storage**: PostgreSQL 16+ (JSONB for rule logic, indexed queries for state/program)  
**Testing**: xUnit, FluentAssertions, WebApplicationFactory, Testcontainers.PostgreSQL, Moq  
**Target Platform**: Linux containers (Docker) + Azure App Service  
**Project Type**: Backend API (Clean Architecture) - extends existing MAA.API solution
**Performance Goals**: Eligibility evaluation ≤2 seconds (p95), FPL lookups <10ms (cached), ≤1,000 concurrent evaluations  
**Constraints**: TLS 1.3 only, no PII in logs, deterministic output (same input = same output), rule versioning for audit  
**Scale/Scope**: 5 pilot states, 6+ Medicaid programs per state, ≥100 test scenarios covering edge cases, rule engine testable in isolation

## Constitution Check _(GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.)_

**Constitution Reference**: See [MAA Constitution](/.specify/memory/constitution.md) for full principle definitions.

### Principle Alignment Assessment

**I. Code Quality & Clean Architecture**

- [x] Domain logic isolated: Rules engine evaluates eligibility as pure function (inputs: rule, user data → output: eligibility result)
- [x] Dependencies explicitly injected: DI for repositories, services, Key Vault client via Program.cs
- [x] No classes exceed ~300 lines: Services split by responsibility (EligibilityEvaluator, ProgramMatcher, ExplanationGenerator)
- [x] DTO contracts explicit: EligibilityResult, MedicaidProgram, EligibilityRule, FPL, UserEligibilityInput all defined

**II. Test-First Development**

- [x] Test scenarios defined in spec: 7 user stories with 20+ acceptance scenarios
- [x] Unit tests 80%+: Evaluation engine (pure logic), FPL calculations, explanation generation
- [x] Integration tests: End-to-end flows for each state, rule versioning, multi-program matching
- [x] Async paths tested: Database lookups for rules/FPL, caching success/miss paths

**III. UX Consistency & Accessibility (if user-facing)**

- [x] Backend only (no UI): Explanations must meet 8th-grade readability (Flesch-Kincaid scoring automated)
- [x] Jargon-free requirement: Specification requires plain language definitions for MAGI, FPL, SSI
- [x] Error messages actionable: Validation errors list specific required fields
- [x] No custom components: Rules engine uses standard C# patterns

**IV. Performance & Scalability (if performance-sensitive)**

- [x] SLOs defined: ≤2 seconds (p95) evaluation, <10ms FPL lookup, 1,000 concurrent evaluations
- [x] Caching documented: In-memory cache for rules (invalidated on rule update), FPL tables (refreshed annually)
- [x] Database queries indexed: Indexes on (state_code, program_id), (year, household_size) for FPL
- [x] Load test target: 1,000 concurrent eligibility evaluations without degradation

**GATE STATUS**: ✅ **PASS** - All principles satisfied. No violations. Proceeding to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/002-rules-engine/
├── plan.md              # This file
├── research.md          # Phase 0: Pilot state rules analysis, FPL tables, rule engine library evaluation
├── data-model.md        # Phase 1: Domain entities, database schema, migration strategy
├── quickstart.md        # Phase 1: Dev environment setup, sample rule evaluation, testing guide
├── contracts/
│   └── rules-api.openapi.yaml  # Phase 1: API endpoints for rule management and evaluation
├── checklists/
│   └── requirements.md   # Quality validation (100% pass)
└── spec.md              # Feature specification (input)
```

### Source Code (repository root)

Extends existing clean architecture and integrates into MAA.API solution:

```text
src/
├── MAA.API/
│   ├── Controllers/             # NEW: RulesController.cs (rule queries)
│   └── Middleware/              # Existing: Exception handlers, auth
│
├── MAA.Application/
│   ├── Services/
│   │   └── EligibilityService.cs        # NEW: Orchestrates evaluation, matching, explanation
│   ├── Rules/
│   │   ├── Commands/                    # NEW: Admin rule submission (Phase 3)
│   │   └── Queries/                     # NEW: Fetch rules by state/program
│   ├── Eligibility/
│   │   ├── Handlers/
│   │   │   ├── EvaluateEligibilityHandler.cs   # NEW
│   │   │   └── GenerateExplanationHandler.cs   # NEW
│   │   └── DTOs/
│   │       ├── UserEligibilityInputDto.cs
│   │       ├── EligibilityResultDto.cs
│   │       └── ProgramMatchDto.cs
│   └── Validators/
│       └── EligibilityInputValidator.cs        # NEW: FluentValidation
│
├── MAA.Domain/
│   ├── Rules/                           # NEW: Domain entities
│   │   ├── MedicaidProgram.cs
│   │   ├── EligibilityRule.cs
│   │   ├── EligibilityResult.cs
│   │   ├── RuleEngine.cs               # Pure evaluation logic (no I/O)
│   │   └── Valueobjects/
│   │       ├── EligibilityStatus.cs
│   │       ├── ConfidenceScore.cs
│   │       └── RuleVersion.cs
│   ├── FederalPovertyLevel/             # NEW
│   │   ├── FPLTable.cs
│   │   └── FPLCalculator.cs            # Pure calculation logic
│   └── Exceptions/
│       └── EligibilityEvaluationException.cs   # NEW
│
├── MAA.Infrastructure/
│   ├── Data/
│   │   ├── Migrations/                  # NEW: InitializeRulesEngine.cs, SeedPilotStateRules.cs
│   │   ├── RuleRepository.cs            # NEW
│   │   ├── FPLRepository.cs             # NEW
│   │   └── SessionContext.cs            # EXISTING: Add DbSet<EligibilityRule>, DbSet<FPL>
│   ├── Encryption/
│   │   └── RuleEncryptor.cs             # NEW: Encrypt rule logic JSON (future)
│   └── Caching/
│       └── RuleCacheService.cs          # NEW: In-memory cache with invalidation
│
└── MAA.Tests/
    ├── Unit/
    │   ├── Rules/
    │   │   ├── RuleEngineTests.cs       # NEW: Pure logic tests (no DB)
    │   │   └── FPLCalculatorTests.cs    # NEW
    │   ├── Eligibility/
    │   │   ├── EligibilityEvaluatorTests.cs    # NEW: Full evaluation flow
    │   │   ├── ProgramMatcherTests.cs          # NEW: Multi-program scenarios
    │   │   └── ExplanationGeneratorTests.cs    # NEW: Plain-language output
    │   └── Validators/
    │       └── EligibilityInputValidatorTests.cs  # NEW
    │
    ├── Integration/
    │   └── RulesApiIntegrationTests.cs  # NEW: End-to-end evaluation via HTTP
    │
    ├── Contract/
    │   └── RulesApiContractTests.cs     # NEW: Validate API against OpenAPI spec
    │
    └── Data/
        ├── pilot-states-test-cases.json # NEW: IL, CA, NY, TX, FL test scenarios
        └── fpl-2026-test-data.json      # NEW: Sample FPL tables for testing
```

**Structure Decision**: Extend existing clean architecture (Domain/Application/Infrastructure/API layers). Rules engine integrates with Session context for eligibility queries. Minimizes refactoring of existing E1 code.

## Complexity Tracking

| Item | Status | Notes |
|------|--------|-------|
| No Constitution violations | ✅ Pass | All 4 principles satisfied with explicit planning |
| Rule engine library choice | TBD | JSONLogic.Net vs custom DSL evaluated in Phase 0 research |
| State-specific rule maintenance | Documented | 5 pilot states means 6+ rules per state (30+ rules total) - manageable at MVP scale |

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation                  | Why Needed         | Simpler Alternative Rejected Because |
| -------------------------- | ------------------ | ------------------------------------ |
| [e.g., 4th project]        | [current need]     | [why 3 projects insufficient]        |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient]  |
