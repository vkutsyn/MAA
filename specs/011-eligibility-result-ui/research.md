# Phase 0 Research - Eligibility Result UI

## Decision 1: Fetch eligibility results with React Query v5

**Decision**: Use TanStack React Query v5 for eligibility result fetching and caching, gating queries on auth/token readiness, and using explicit loading/error states.

**Rationale**:

- Aligns with approved stack for server state management.
- Provides consistent loading/error states and caching to avoid UI flicker.
- Supports stale time and background refetch patterns appropriate for stable results.

**Alternatives considered**:

- Direct fetch with local component state (less consistent, more boilerplate).
- Global state store (Zustand/Context) for server state (less suited to caching and refetch).

## Decision 2: Use shadcn/ui primitives for results UI

**Decision**: Build the results UI using shadcn/ui primitives (Card, Badge, Progress) with semantic HTML structure and WCAG 2.1 AA affordances.

**Rationale**:

- Consistency with the project design system and Constitution requirements.
- Accessible primitives that are easier to validate for keyboard and screen reader usage.
- Faster implementation while avoiding custom behavior.

**Alternatives considered**:

- Custom component implementations (risk of WCAG regressions).
- Third-party component library outside shadcn/ui (design inconsistency).

## Decision 3: Confidence indicator labeling and thresholds

**Decision**: Map the numeric confidence score (0-100) to plain-language labels and show both label and numeric score with a visual indicator.

**Rationale**:

- Avoids reliance on color alone for meaning.
- Improves comprehension and trust in the determination.
- Keeps language plain and avoids implying certainty.

**Alternatives considered**:

- Numeric-only display (harder to interpret quickly).
- Three-tier labels (less precise for borderline scores).

## Decision 4: Use existing rules evaluation contract

**Decision**: Use the existing `POST /api/rules/evaluate` endpoint contract for eligibility results and map the response to UI view models.

**Rationale**:

- Aligns with the prior eligibility evaluation engine feature output.
- Avoids adding new backend endpoints for a UI-only change.
- Ensures the UI reflects the same data used in backend integration tests.

**Alternatives considered**:

- Add a new `GET /api/eligibility/result` endpoint (backend change not required for this UI feature).
- Persist results to session storage and read from a session endpoint (more backend coupling).
