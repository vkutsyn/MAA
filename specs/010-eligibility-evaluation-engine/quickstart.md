# Quickstart: Eligibility Evaluation Engine

**Feature**: 010-eligibility-evaluation-engine
**Date**: February 11, 2026
**Audience**: Backend developers implementing this feature

## Overview

This quickstart outlines the implementation order for the eligibility evaluation engine. Follow test-first development and ensure each phase passes its tests before moving on.

**Expected Time**: 12-16 hours (backend + tests)

---

## Prerequisites

- .NET 10 SDK installed
- PostgreSQL 16+ running locally or via Docker
- MAA repository cloned and up to date
- Feature branch `010-eligibility-evaluation-engine` checked out
- Reviewed [spec.md](spec.md), [research.md](research.md), and [data-model.md](data-model.md)

---

## Implementation Order

### Phase 1: Domain Layer (3-4 hours)

**Goal**: Implement pure rule evaluation logic and result construction.

1. Create domain models for `RuleSetVersion`, `EligibilityRule`, `ProgramDefinition`, and `FederalPovertyLevel`.
2. Implement `EligibilityEvaluator` using JSONLogic with deterministic evaluation.
3. Add `ConfidenceScoringPolicy` and `ExplanationBuilder` using template rules.

**Suggested files**:
- `src/MAA.Domain/Eligibility/RuleSetVersion.cs`
- `src/MAA.Domain/Eligibility/EligibilityRule.cs`
- `src/MAA.Domain/Eligibility/EligibilityEvaluator.cs`
- `src/MAA.Domain/Eligibility/ConfidenceScoringPolicy.cs`
- `src/MAA.Domain/Eligibility/ExplanationBuilder.cs`

**Tests**:
- Unit tests for rule selection, scoring, and explanation output.

---

### Phase 2: Application Layer (2-3 hours)

**Goal**: Add DTOs and use case handlers.

1. Command/query: `EvaluateEligibility` handler with input validation.
2. DTOs for request/response mapping and error handling.

**Suggested files**:
- `src/MAA.Application/Eligibility/Queries/EvaluateEligibilityQuery.cs`
- `src/MAA.Application/Eligibility/Dtos/EligibilityEvaluateRequestDto.cs`
- `src/MAA.Application/Eligibility/Dtos/EligibilityEvaluateResponseDto.cs`

**Tests**:
- Unit tests for handler behavior and validation errors.

---

### Phase 3: Infrastructure Layer (2-3 hours)

**Goal**: Add EF Core mappings and repositories for rule data.

1. Configure EF Core entities for rule sets, rules, programs, and FPL tables.
2. Implement repositories for rule retrieval and caching.
3. Add migrations for rule data tables.

**Suggested files**:
- `src/MAA.Infrastructure/Eligibility/RuleSetVersionConfiguration.cs`
- `src/MAA.Infrastructure/Eligibility/EligibilityRuleConfiguration.cs`
- `src/MAA.Infrastructure/Eligibility/EligibilityRuleRepository.cs`

**Tests**:
- Integration tests using PostgreSQL test container.

---

### Phase 4: API Layer (2-3 hours)

**Goal**: Expose REST endpoint and wire DI.

1. Add `EligibilityController` endpoint from [eligibility-evaluation-api.yaml](contracts/eligibility-evaluation-api.yaml).
2. Map validation and rule-not-found errors to 400/404 responses.
3. Ensure request logging and performance timing.

**Tests**:
- Integration tests for the API endpoint.
- Contract tests against OpenAPI schema.

---

## Commands

Run targeted tests as you go:

```bash
# Unit tests
cd src/MAA.Tests
dotnet test --filter "FullyQualifiedName~MAA.Tests.Unit.Eligibility"

# Integration tests
cd src/MAA.Tests
dotnet test --filter "FullyQualifiedName~MAA.Tests.Integration.Eligibility"
```

---

## Completion Checklist

- All functional requirements implemented
- Unit tests for evaluator, scoring, and explanations (>80% coverage)
- Integration and contract tests for API endpoint
- Performance targets validated (p95 <= 2s)
- OpenAPI schema matches implementation
