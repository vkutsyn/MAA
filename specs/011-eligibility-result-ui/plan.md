# Implementation Plan: Eligibility Result UI

**Branch**: `011-eligibility-result-ui` | **Date**: February 11, 2026 | **Spec**: [specs/011-eligibility-result-ui/spec.md](specs/011-eligibility-result-ui/spec.md)
**Input**: Feature specification from `/specs/011-eligibility-result-ui/spec.md`

## Summary

Deliver a results view that surfaces eligibility status, matched programs, explanation bullets, and a confidence indicator. The UI will map the existing eligibility evaluation output from `POST /api/rules/evaluate` into view models and present it using shadcn/ui components with WCAG 2.1 AA compliance, plain-language labels, and stable loading/error states via React Query.

## Technical Context

**Language/Version**: TypeScript 5.7, React 19 (frontend)  
**Primary Dependencies**: Vite 6, shadcn/ui (Radix UI + Tailwind), TanStack React Query v5, Zustand (if needed), axios  
**Storage**: N/A (UI-only feature; data fetched from API)  
**Testing**: Vitest, React Testing Library  
**Target Platform**: Modern browsers (desktop and mobile)  
**Project Type**: Web application (frontend + backend)  
**Performance Goals**: Results view renders within 2 seconds of evaluation completion; UI interactions under 500ms  
**Constraints**: WCAG 2.1 AA, plain-language output, use shadcn/ui components only  
**Scale/Scope**: 1,000 concurrent users (per Constitution), single results view in wizard flow

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
- [x] Mobile-first responsive design (375px → 1920px)
- [x] Plain language (no unexplained jargon; error messages actionable)
- [x] Consistent component usage (shadcn/ui + Tailwind); no custom component behavior

**IV. Performance & Scalability (if performance-sensitive)**

- [x] Response time SLOs defined (align with Constitution targets: ≤2s eligibility, ≤500ms interactions)
- [x] Caching strategy documented (React Query for server state)
- [x] Database queries indexed on common filters (state_id, eligibility_status, created_at)
- [x] Load test target defined (1,000 concurrent users)

## Project Structure

### Documentation (this feature)

```text
specs/011-eligibility-result-ui/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
└── tasks.md
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
│   ├── hooks/
│   ├── lib/
│   └── routes/
└── tests/
```

**Structure Decision**: Web application (frontend + backend). UI feature lives under `frontend/src/features` with shared UI components from `frontend/src/components` and API types under `frontend/src/lib`.

## Phase 0: Outline & Research

**Completed artifacts**:

- [specs/011-eligibility-result-ui/research.md](specs/011-eligibility-result-ui/research.md)

**Key decisions**:

- Use React Query v5 for eligibility result fetching and caching.
- Use shadcn/ui primitives with semantic HTML for WCAG compliance.
- Map confidence scores to plain-language labels with a visual indicator.
- Reuse `POST /api/rules/evaluate` contract for eligibility results.

## Phase 1: Design & Contracts

**Completed artifacts**:

- [specs/011-eligibility-result-ui/data-model.md](specs/011-eligibility-result-ui/data-model.md)
- [specs/011-eligibility-result-ui/quickstart.md](specs/011-eligibility-result-ui/quickstart.md)
- [specs/011-eligibility-result-ui/contracts/eligibility-results.openapi.yaml](specs/011-eligibility-result-ui/contracts/eligibility-results.openapi.yaml)

**Design notes**:

- UI maps `EligibilityResultDto` into an `EligibilityResultView` model with derived confidence labels.
- Results view must handle partial data by showing a fallback message and preserving navigation.
- Program matches and explanation bullets render as semantic lists with labels and icons.

## Constitution Check (Post-Design)

- [x] All design artifacts align with clean architecture and type safety.
- [x] Test-first expectations are preserved with explicit scenarios in spec.
- [x] UI design patterns include WCAG 2.1 AA guidance and plain language.
- [x] Performance goals documented for results render and interactions.

## Phase 2: Planning

**Implementation outline (no tasks yet)**:

1. Add eligibility result types and mapping utilities in `frontend/src/lib`.
2. Implement data fetching hook using React Query v5 and JWT-aware API client.
3. Build results UI components (status card, program matches list, explanation bullets, confidence indicator).
4. Wire results UI into the wizard route/step and handle loading/error/empty states.
5. Add unit/component tests for rendering and a11y-focused assertions.
