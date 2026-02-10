# Quickstart: Eligibility Wizard Session API

**Feature**: 007-wizard-session-api  
**Date**: February 10, 2026  
**Audience**: Backend developers implementing this feature

## Overview

This quickstart outlines the implementation order for the Eligibility Wizard Session API. Follow test-first development and ensure each phase passes its tests before moving on.

**Expected Time**: 10-14 hours (backend + tests)

---

## Prerequisites

- .NET 10 SDK installed
- PostgreSQL 16+ running locally or via Docker
- MAA repository cloned and up to date
- Feature branch `007-wizard-session-api` checked out
- Reviewed [spec.md](spec.md), [research.md](research.md), and [data-model.md](data-model.md)

---

## Implementation Order

### Phase 1: Domain Layer (2-3 hours)

**Goal**: Implement pure domain logic for step definitions, navigation, and validation.

1. Create domain models for `WizardSession`, `StepAnswer`, and `StepProgress`.
2. Add `IStepDefinitionProvider` and `StepNavigationEngine` for conditional navigation.
3. Implement `IStepAnswerValidator` per step definition (start with 1-2 sample steps).

**Suggested files**:
- `src/MAA.Domain/Wizard/WizardSession.cs`
- `src/MAA.Domain/Wizard/StepAnswer.cs`
- `src/MAA.Domain/Wizard/StepProgress.cs`
- `src/MAA.Domain/Wizard/StepDefinition.cs`
- `src/MAA.Domain/Wizard/StepNavigationEngine.cs`

**Tests**:
- Unit tests for navigation rules and validation.

---

### Phase 2: Application Layer (2-3 hours)

**Goal**: Add command/query handlers and DTOs.

1. Command: `SaveStepAnswer` (validates, persists, updates progress, invalidates downstream).
2. Query: `GetWizardSessionState` (rehydrates session + answers + progress).
3. Query: `GetNextStep` (returns next step definition and progress info).

**Suggested files**:
- `src/MAA.Application/Wizard/Commands/SaveStepAnswerCommand.cs`
- `src/MAA.Application/Wizard/Queries/GetWizardSessionStateQuery.cs`
- `src/MAA.Application/Wizard/Queries/GetNextStepQuery.cs`
- `src/MAA.Application/Wizard/Dtos/*.cs`

**Tests**:
- Unit tests for command/query handlers using in-memory repositories.

---

### Phase 3: Infrastructure Layer (2-3 hours)

**Goal**: EF Core mappings and repositories.

1. Add EF Core configurations for `WizardSession`, `StepAnswer`, `StepProgress`.
2. Add repository implementations and migrations.
3. Configure JSONB mapping for `answer_data` and concurrency tokens.

**Suggested files**:
- `src/MAA.Infrastructure/Wizard/WizardSessionConfiguration.cs`
- `src/MAA.Infrastructure/Wizard/StepAnswerConfiguration.cs`
- `src/MAA.Infrastructure/Wizard/StepProgressConfiguration.cs`
- `src/MAA.Infrastructure/Wizard/WizardRepository.cs`

**Tests**:
- Integration tests with PostgreSQL test container.

---

### Phase 4: API Layer (1-2 hours)

**Goal**: Expose REST endpoints and wire DI.

1. Add `WizardSessionController` with endpoints from [wizard-session-api.yaml](contracts/wizard-session-api.yaml).
2. Map `DbUpdateConcurrencyException` to 409 responses.
3. Ensure authorization/expired session checks (401/403).

**Tests**:
- Integration tests for all endpoints.
- Contract tests against OpenAPI schema.

---

## Commands

Run targeted tests as you go:

```bash
# Unit tests
cd src/MAA.Tests
dotnet test --filter "FullyQualifiedName~MAA.Tests.Unit.Wizard"

# Integration tests
cd src/MAA.Tests
dotnet test --filter "FullyQualifiedName~MAA.Tests.Integration.Wizard"
```

---

## Completion Checklist

- All functional requirements implemented
- Unit tests for navigation and validation (>90% coverage)
- Integration and contract tests for API endpoints
- Performance targets validated (p95)
- Updated OpenAPI schema matches implementation

---

## Quickstart Validation Checklist

- [x] Domain models and navigation services implemented
- [x] Application handlers and DTOs added
- [x] EF migration created for wizard tables
- [x] API endpoints wired and DI updated
- [x] Unit, contract, and integration tests added
