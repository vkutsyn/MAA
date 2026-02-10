# Research: Eligibility Wizard UI

## Decision: Use React 19 + Vite 6 + shadcn/ui for the UI layer
- Rationale: Matches the approved tech stack and WCAG-focused component system; enables rapid iteration with TypeScript and Vite.
- Alternatives considered: Next.js app router (rejected for MVP simplicity), custom component library (rejected due to accessibility and consistency risks).

## Decision: Keep anonymous sessions via cookie-based session ID
- Rationale: Session middleware already validates `MAA_SessionId` cookies and resets inactivity timers; aligning UI with this avoids new auth flows.
- Alternatives considered: LocalStorage session ID header (rejected due to middleware cookie dependency), JWT-only access (rejected for anonymous flow).

## Decision: Add Set-Cookie on session creation response
- Rationale: Session middleware requires `MAA_SessionId`; the UI needs the cookie set by the API after `POST /api/sessions`.
- Alternatives considered: Client sets cookie manually (rejected due to HttpOnly requirement), embedding session ID in every request body (rejected for security and consistency).

## Decision: Fetch question taxonomy and state metadata from new API endpoints
- Rationale: UI requires state-specific, conditional question flow; rules engine should own taxonomy and drive rendering.
- Alternatives considered: Hardcoded question lists in UI (rejected due to maintainability and dependency on E2 rules engine).

## Decision: Persist answers through session answers API on each step
- Rationale: The backend already supports upserted answers and encrypted PII; enables refresh resume and server-side validation.
- Alternatives considered: Client-only persistence (rejected because refresh/restore is a core requirement and needs server storage).
