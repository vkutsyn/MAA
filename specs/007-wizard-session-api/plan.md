# Implementation Plan: Eligibility Wizard Session API

**Branch**: `007-wizard-session-api` | **Date**: 2026-02-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-wizard-session-api/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Deliver backend API support for the multi-step eligibility wizard with auto-save, dynamic next-step navigation, and full session rehydration. Answers are persisted per step in PostgreSQL JSONB, navigation logic is evaluated in a pure domain service, and optimistic concurrency prevents conflicting updates.

## Technical Context

**Language/Version**: C# 13 on .NET 10 (backend API)  
**Primary Dependencies**: ASP.NET Core Web API, EF Core 10 (Npgsql), MediatR, FluentValidation  
**Storage**: PostgreSQL 16+ with JSONB answer storage  
**Testing**: xUnit, FluentAssertions, WebApplicationFactory integration tests  
**Target Platform**: Linux containers (ASP.NET Core)  
**Project Type**: web (backend + frontend in same repo)  
**Performance Goals**: save answer <200ms p95, next-step <150ms p95, restore session <500ms p95, step definition <100ms p95  
**Constraints**: optimistic concurrency, JSONB schema validation, no DB dependencies in navigation logic  
**Scale/Scope**: 1,000 concurrent users target; 8-15 wizard steps per session

## Constitution Check _(GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.)_

**Constitution Reference**: See [MAA Constitution](/.specify/memory/constitution.md) for full principle definitions.

### Principle Alignment Assessment

**I. Code Quality & Clean Architecture**

- [x] Domain logic can be isolated from I/O (testable without DB/HTTP)
- [x] Dependencies are explicitly injected (no service locator pattern)
- [x] No classes exceed ~300 lines; single responsibility clear
- [x] DTO contracts explicitly defined (backend) or type-safe (frontend)

**II. Test-First Development**

- [x] Test scenarios defined in spec before implementation begins
- [x] Unit tests target domain/application layers (80%+ coverage planned)
- [x] Integration tests cover cross-layer flows (handlers + database, components + hooks)
- [x] All async operations tested for success AND error paths

**III. UX Consistency & Accessibility (if user-facing)**

- [x] N/A - backend-only API feature (no UI changes)
- [x] N/A - backend-only API feature (no UI changes)
- [x] N/A - backend-only API feature (no UI changes)
- [x] N/A - backend-only API feature (no UI changes)

**IV. Performance & Scalability (if performance-sensitive)**

- [x] Response time SLOs defined (save <200ms, next-step <150ms, restore <500ms, step def <100ms)
- [x] Caching strategy documented (in-memory step definitions; Redis optional for immutable data)
- [x] Database queries indexed on `session_id`, `step_id`, `updated_at`
- [x] Load test target defined (1,000 concurrent users)

**⚠️ VIOLATIONS FOUND?** None.

## Project Structure

### Documentation (this feature)

```text
specs/007-wizard-session-api/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── MAA.API/
├── MAA.Application/
├── MAA.Domain/
├── MAA.Infrastructure/
└── MAA.Tests/

frontend/
└── src/
    ├── components/
    ├── features/
    ├── lib/
    └── routes/
```

**Structure Decision**: Web application layout with backend in `src/` and frontend in `frontend/`, matching the existing MAA solution structure.

## Complexity Tracking

No violations to justify for this feature.

## Post-Design Constitution Check

Re-check completed after Phase 1 design. No violations identified; all principles remain satisfied.
