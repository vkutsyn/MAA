# Implementation Plan: State Context Initialization Step

**Branch**: `006-state-context-init` | **Date**: February 10, 2026 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/006-state-context-init/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

This feature establishes Medicaid jurisdiction context before eligibility evaluation. Users enter their ZIP code, the system auto-detects their state, loads state-specific configuration, and persists the context in their session. This is the foundational step for the eligibility workflow—no eligibility determination occurs here, only context initialization.

## Technical Context

**Language/Version**: Backend: C# 13 (.NET 10), Frontend: TypeScript 5.7+ (React 19)
**Primary Dependencies**:

- Backend: ASP.NET Core Web API, Entity Framework Core 10, PostgreSQL driver (Npgsql), FluentValidation
- Frontend: React 19, Vite 6, shadcn/ui, TanStack Query v5, React Hook Form + Zod, React Router v6
  **Storage**: PostgreSQL 16+ (session persistence, state configuration storage)
  **Testing**:
- Backend: xUnit, FluentAssertions, Moq, WebApplicationFactory
- Frontend: Vitest, React Testing Library, @testing-library/jest-dom
  **Target Platform**: Web application (Linux server backend, browser-based frontend)
  **Project Type**: Web application (separate frontend + backend)
  **Performance Goals**:
- ZIP validation and state resolution: <100ms (p95)
- State configuration load: <500ms (p95)
- Total step completion: <1000ms from submission to navigation (p95)
  **Constraints**:
- WCAG 2.1 AA compliance required
- Must work on mobile (375px) → desktop (1920px)
- Session state must persist across page refreshes
- No eligibility calculation permitted in this step
  **Scale/Scope**:
- 1,000 concurrent users
- 50+ U.S. states/territories to support
- Session data stored in PostgreSQL

## Constitution Check _(GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.)_

**Constitution Reference**: See [MAA Constitution](/.specify/memory/constitution.md) for full principle definitions.

### Principle Alignment Assessment

**I. Code Quality & Clean Architecture**

- [x] Domain logic can be isolated from I/O (testable without DB/HTTP)
  - ZIP validation logic: pure functions (5 digits, numeric check)
  - State resolution logic: pure mapping function (ZIP → state code)
  - StateContext entity: plain DTOs with no I/O dependencies
- [x] Dependencies are explicitly injected (no service locator pattern)
  - Repository interfaces injected into handlers
  - Frontend: API client passed via React Query
- [x] No classes exceed ~300 lines; single responsibility clear
  - ZipCodeValidator: validation only
  - StateResolver: resolution only
  - StateConfigurationLoader: loading only
- [x] DTO contracts explicitly defined (backend) or type-safe (frontend)
  - Backend: StateContextDto, StateConfigurationDto defined
  - Frontend: TypeScript interfaces for all API contracts

**II. Test-First Development**

- [x] Test scenarios defined in spec before implementation begins
  - 3 user stories with detailed acceptance criteria
  - Edge cases enumerated
- [x] Unit tests target domain/application layers (80%+ coverage planned)
  - ZIP validation: format tests, boundary tests
  - State resolution: mapping coverage for all 50+ states
  - StateContext creation: validation tests
- [x] Integration tests cover cross-layer flows (handlers + database, components + hooks)
  - API endpoint → state resolution → session persistence
  - Frontend: ZIP input → API call → navigation flow
- [x] All async operations tested for success AND error paths
  - Session persistence: success, failure, timeout
  - State config loading: success, missing config, network error

**III. UX Consistency & Accessibility (if user-facing)**

- [x] Design meets WCAG 2.1 AA (semantic HTML, keyboard nav, screen reader support)
  - ZIP input field: proper `<label>` and `aria-describedby` for errors
  - Error messages: `role="alert"` for screen reader announcement
  - State selector (override): keyboard-accessible `<select>` or Radix Select
- [x] Mobile-first responsive design (375px → 1920px)
  - Form layout adapts to small screens
  - Touch targets ≥44×44px for mobile
- [x] Plain language (no unexplained jargon; error messages actionable)
  - "Please enter a valid 5-digit ZIP code" (clear, actionable)
  - "ZIP code not found. Please verify your ZIP code" (no technical jargon)
- [x] Consistent component usage (shadcn/ui + Tailwind); no custom component behavior
  - Input: shadcn/ui Input component
  - Button: shadcn/ui Button component
  - Select (override): shadcn/ui Select or native `<select>`

**IV. Performance & Scalability (if performance-sensitive)**

- [x] Response time SLOs defined (align with Constitution targets: ≤2s eligibility, ≤500ms interactions)
  - ZIP validation + state resolution: <100ms (p95)
  - State configuration load: <500ms (p95)
  - Total step completion: <1000ms (p95) from submission to navigation
- [x] Caching strategy documented (Redis for immutable data, React Query for server state)
  - State configuration: cached in-memory or Redis (24h TTL, immutable per release)
  - ZIP-to-state mapping: cached in-memory (static data)
  - Frontend: React Query caches state context for session duration
- [x] Database queries indexed on common filters (state_id, eligibility_status, created_at)
  - Session table indexed on session_id (primary key)
  - State configuration indexed on state_code (primary key)
- [x] Load test target defined (1,000 concurrent users or domain-specific metric)
  - 1,000 concurrent ZIP submissions without degradation
  - p95 latency remains <1000ms under load

**✅ NO VIOLATIONS** - All constitution principles aligned.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
# Backend (.NET 10)
src/
├── MAA.Domain/
│   ├── StateContext/                # NEW
│   │   ├── StateContext.cs         # Domain entity
│   │   ├── StateConfiguration.cs   # Domain entity
│   │   ├── ZipCodeValidator.cs     # Pure validation logic
│   │   └── StateResolver.cs        # Pure ZIP→state mapping
│   └── Sessions/                   # EXISTING
│       └── Session.cs              # Update to include StateContext
├── MAA.Application/
│   ├── StateContext/                # NEW
│   │   ├── Commands/
│   │   │   └── InitializeStateContext.cs  # Command + handler
│   │   ├── Queries/
│   │   │   └── GetStateConfiguration.cs   # Query + handler
│   │   └── DTOs/
│   │       ├── StateContextDto.cs
│   │       └── StateConfigurationDto.cs
│   └── Sessions/                   # EXISTING (may need updates)
├── MAA.Infrastructure/
│   ├── StateContext/                # NEW
│   │   ├── StateConfigurationRepository.cs  # EF Core repo
│   │   └── ZipCodeMappingService.cs         # Static or DB lookup
│   └── Sessions/                   # EXISTING
│       └── SessionRepository.cs    # Update for StateContext persistence
└── MAA.API/
    └── Controllers/
        └── StateContextController.cs       # NEW: POST /api/state-context

# Frontend (React 19 + TypeScript)
frontend/
├── src/
│   ├── features/
│   │   └── state-context/           # NEW
│   │       ├── components/
│   │       │   ├── ZipCodeForm.tsx       # Input form
│   │       │   ├── StateOverride.tsx     # Manual override UI
│   │       │   └── StateConfirmation.tsx # Display detected state
│   │       ├── hooks/
│   │       │   ├── useStateContext.ts    # API call + React Query
│   │       │   └── useZipValidation.ts   # Client-side validation
│   │       ├── api/
│   │       │   └── stateContextApi.ts    # API client functions
│   │       └── types/
│   │           └── stateContext.types.ts # TypeScript interfaces
│   └── routes/
│       └── StateContextStep.tsx     # NEW: Route component

# Tests
src/MAA.Tests/
├── Unit/
│   ├── StateContext/                # NEW
│   │   ├── ZipCodeValidatorTests.cs
│   │   ├── StateResolverTests.cs
│   │   └── StateContextTests.cs
│   └── ...
├── Integration/
│   ├── StateContext/                # NEW
│   │   ├── InitializeStateContextTests.cs  # Handler + repo
│   │   └── StateContextControllerTests.cs  # API endpoint
│   └── ...
└── E2E/                             # EXISTING
    └── StateContextFlowTests.cs     # NEW: Full user flow

frontend/src/__tests__/
├── features/
│   └── state-context/               # NEW
│       ├── ZipCodeForm.test.tsx
│       ├── useStateContext.test.ts
│       └── stateContextApi.test.ts
```

**Structure Decision**: Web application structure with separate backend (src/) and frontend (frontend/) directories. Backend follows clean architecture (Domain, Application, Infrastructure, API). Frontend uses feature-based organization. This aligns with existing MAA project structure.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**Status**: ✅ No violations - all constitution principles aligned

No additional complexity introduced beyond standard MAA architecture patterns (Clean Architecture, Repository pattern, React Query).
