# Implementation Plan: Eligibility Wizard UI

**Branch**: `004-ui-implementation` | **Date**: 2026-02-10 | **Spec**: [specs/004-ui-implementation/spec.md](specs/004-ui-implementation/spec.md)
**Input**: Feature specification from `/specs/004-ui-implementation/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement the Eligibility Wizard UI (landing, state selection, multi-step questionnaire) in React with session-backed persistence through existing session and answer APIs, and add question taxonomy and state lookup endpoints to support dynamic, state-specific flows.

## Technical Context

**Language/Version**: TypeScript 5.7 (React 19), C# 13 (.NET 10 API integration)  
**Primary Dependencies**: React 19, Vite 6, shadcn/ui + Tailwind CSS, React Hook Form + Zod, TanStack Query, Zustand  
**Storage**: PostgreSQL via API (sessions/answers); session cookie (`MAA_SessionId`) for client persistence  
**Testing**: Vitest + React Testing Library (frontend), xUnit (API contract tests)  
**Target Platform**: Modern browsers (mobile and desktop), backend Linux containers  
**Project Type**: Web application (frontend + backend)  
**Performance Goals**: Wizard step transitions p95 <= 500ms; first question render <= 1s after start  
**Constraints**: WCAG 2.1 AA, mobile-first (375px+), shadcn/ui components only, anonymous session timeout (30 min)  
**Scale/Scope**: MVP wizard flow for pilot states, ~1,000 concurrent users target

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

- [x] Design meets WCAG 2.1 AA (semantic HTML, keyboard nav, screen reader support)
- [x] Mobile-first responsive design (375px -> 1920px)
- [x] Plain language (no unexplained jargon; error messages actionable)
- [x] Consistent component usage (shadcn/ui + Tailwind); no custom component behavior

**IV. Performance & Scalability (if performance-sensitive)**

- [x] Response time SLOs defined (align with Constitution targets: <=2s eligibility, <=500ms interactions)
- [x] Caching strategy documented (React Query for server state; no new Redis usage in this phase)
- [x] Database queries indexed on common filters (no new DB queries added in this feature)
- [x] Load test target defined (1,000 concurrent users)

**⚠️ VIOLATIONS FOUND?** Document in "Complexity Tracking" section below with justification.

Post-Phase 1 re-check: PASS (no violations).

## Project Structure

### Documentation (this feature)

```text
specs/004-ui-implementation/
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
├── src/
│   ├── components/
│   ├── features/
│   │   └── wizard/
│   ├── hooks/
│   ├── lib/
│   └── routes/
└── tests/
```

**Structure Decision**: Use the existing backend layout under `src/` and add a Vite-powered React frontend in `frontend/` to keep UI concerns isolated while integrating with the API.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation                  | Why Needed         | Simpler Alternative Rejected Because |
| -------------------------- | ------------------ | ------------------------------------ |
| [e.g., 4th project]        | [current need]     | [why 3 projects insufficient]        |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient]  |
