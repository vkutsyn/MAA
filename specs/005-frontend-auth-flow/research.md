# Research: Frontend Authentication Flow with Login/Registration

## Decision 1: Use existing backend auth contract (register, login, refresh, logout)

**Decision**: Align frontend auth flows to the current backend endpoints for registration, login, refresh, and logout.

**Rationale**:

- The backend already exposes stable endpoints and response shapes for auth.
- Using existing contracts minimizes backend changes and risk.

**Alternatives considered**:

- Implement a new custom auth endpoint set for frontend-only use (rejected: unnecessary divergence from existing backend contract).

---

## Decision 2: Centralize unauthorized handling with a single-flight session renewal

**Decision**: Route unauthorized responses through a single renewal attempt, then redirect to login if renewal fails.

**Rationale**:

- Prevents retry loops seen when unauthorized responses trigger repeated requests.
- Single-flight renewal avoids multiple parallel renewals when several requests fail at once.

**Alternatives considered**:

- Immediate redirect to login on any unauthorized response (rejected: degrades user experience when session can be renewed).
- Per-request renewal attempts without coordination (rejected: risks thundering herd of renewals).

---

## Decision 3: Keep route-level guarding consistent with existing frontend patterns

**Decision**: Implement auth checks via feature hooks and route wrappers consistent with current wizard flow guards.

**Rationale**:

- The current app uses feature-level hooks for session resume and route validation.
- Minimizes architectural change while introducing login/registration screens.

**Alternatives considered**:

- Introduce global route loaders for auth (rejected: not currently used; would require broader refactor).
- Move all routing to a new auth provider context (rejected: heavier change than needed for this feature).

---

## Decision 4: Use existing form and UI conventions

**Decision**: Build login and registration forms with existing UI and form libraries per tech stack guidance.

**Rationale**:

- Ensures WCAG 2.1 AA compliance via consistent, accessible components.
- Aligns with project stack decisions and reduces rework.

**Alternatives considered**:

- Custom form controls and validation logic (rejected: increases accessibility risk and inconsistency).
