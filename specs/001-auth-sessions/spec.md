# Feature Specification: Authentication & Session Management (E1)

**Feature Branch**: `001-auth-sessions`  
**Created**: 2026-02-08  
**Status**: Draft - Ready for Clarification  
**Input**: Epic E1 from MAA Implementation Plan

---

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Anonymous User Session (Priority: P1)

Users should be able to start the eligibility wizard without creating an account. Their session state (answers, documents) persists across page refreshes and can be resumed within the same session window.

**Why this priority**: Core MVP requirement; blocks all public features (wizard, results, documents)

**Independent Test**: User navigates to app → session created → refreshes page → session persists

**Acceptance Scenarios**:

1. **Given** a user visits the MAA home page for the first time, **When** the page loads, **Then** a session ID is generated and stored in HTTP-only secure cookie with SameSite=Strict
2. **Given** a user in an active session making requests, **When** each request is received, **Then** the 30-minute inactivity timer resets (sliding window)
3. **Given** a user's session with 31 minutes of inactivity, **When** they attempt to access /api/sessions/{id}, **Then** session is invalidated and HTTP 401 Unauthorized is returned
4. **Given** an admin user with 4 hours of inactivity, **When** they make a request, **Then** session remains valid (8-hour timeout applies)
5. **Given** an admin user with 9 hours of inactivity, **When** they attempt to access /api/admin/rules, **Then** session is invalidated and HTTP 401 Unauthorized is returned

---

### User Story 2 - Session Data Persistence (Priority: P1)

As the system, I need to store session state (wizard answers, document upload metadata, eligibility results) persistently so that users can resume interrupted applications and the system can reference their answers when generating results.

**Why this priority**: Enables wizard save/resume feature and critical for multi-step user journey

**Independent Test**: Wizard answers saved → page refresh → answers restored with values intact

**Acceptance Scenarios**:

1. **Given** a user completes wizard step 1 and submits answers (including income=$2100), **When** POST /api/sessions/{id}/answers is processed, **Then** income is encrypted (randomized) before database insert
2. **Given** encrypted income in database, **When** database is queried directly (SELECT income FROM answers), **Then** the value is unreadable ciphertext; repeated inserts of same income produce different ciphertexts
3. **Given** randomized-encrypted income, **When** the eligibility engine requests answers via /api/sessions/{id}/answers (decryption happens on application side), **Then** income is decrypted and returned plain value $2100
4. **Given** a user's SSN, **When** stored in database with deterministic encryption, **Then** same SSN always encrypts to same ciphertext (enables exact-match validation queries)
5. **Given** stored encrypted answers, **When** user refreshes page / returns after 1 hour, **Then** answers are still retrieved (session persists; decryption still works)

---

### User Story 3 - Role-Based Access Control (Priority: P1)

Admin endpoints must only be accessible to users with Admin, Reviewer, or Analyst roles. Anonymous public users must not be able to access admin endpoints.

**Why this priority**: Security blocker; prevents unauthorized access to rule management

**Independent Test**: Non-admin user POSTs to /api/admin/rules → 403 Forbidden

**Acceptance Scenarios**:

1. **Given** a request to POST /api/admin/rules/create without authorization header, **When** the request is processed, **Then** the system returns HTTP 403 Forbidden
2. **Given** a request with valid JWT but role="User", **When** /api/admin/rules is accessed, **Then** returns 403 Forbidden with message "Insufficient permissions"
3. **Given** a request with valid JWT and role="Admin", **When** /api/admin/rules is accessed, **Then** the request proceeds and returns 200 OK
4. **Given** a request with role="Reviewer", **When** accessing /api/admin/approval-queue, **Then** returns 200 OK; accessing /api/admin/users returns 403 (insufficient role)

---

### User Story 4 - Sensitive Data Encryption (Priority: P1)

Income, assets, SSN, and other PII must be encrypted at the database level. Even database administrators should not be able to read raw PII values without decryption keys.

**Why this priority**: Compliance + security; enables HIPAA/privacy compliance

**Independent Test**: User enters income=$2,100 → database stores encrypted value → application decrypts and displays correctly

**Acceptance Scenarios**:

1. **Given** a user submits household income=$2,100 via the wizard, **When** POST /api/sessions/{id}/answers is processed, **Then** income is encrypted before database insert
2. **Given** encrypted income in database, **When** database is queried directly (e.g., SELECT income FROM answers), **Then** the value is unreadable (encrypted ciphertext)
3. **Given** an encrypted income value, **When** the eligibility engine requests answers via /api/sessions/{id}/answers, **Then** the application decrypts and returns plain value $2,100
4. **Given** a database backup, **When** examined without decryption keys, **Then** all PII fields are unreadable

---

### Edge Cases

- **Session expires during form submission**: User is filling out quiz; 30-minute timer expires → next API request rejected with HTTP 401. Solution: Client shows "Your session expired after 30 minutes. Save your progress by taking a screenshot, then start a new eligibility check." (Phase 5: accounts allow resume)
- **Encryption key lost/rotated**: Old sessions encrypted with old key; new sessions with new key. Solution: Key versioning in database; decryption tries all active keys until one succeeds
- **Two simultaneous requests modify same session**: Race condition risk (e.g., two POST /api/sessions/{id}/answers calls). Solution: Optimistic locking using `version` column; last-write-wins acceptable (user's second answer overwrites first)
- **Admin user's role changes mid-session**: User is admin; admin revokes their Admin role. Solution: Session remains valid; token/permissions checked on next API call. Permissions not refreshed during session (acceptable for 1-hour window)
- **User checks login from 3 devices; tries to login from 4th device** (Phase 5): Max 3 sessions per user. Solution: 4th login request returns HTTP 409 Conflict with message "You're logged in on 3 devices. End another session to continue?" User gets list of active sessions (device type, IP, login time) and can revoke one

---

## Clarifications (Session 2026-02-08)

Following `/speckit.clarify` workflow, all material ambiguities have been resolved:

- **Q1: Session Timeout Strategy** → **A: Tiered Timeouts** — Public users 30 minutes; Admin/Reviewer/Analyst 8 hours (work day)
- **Q2: Encrypted Fields Scope** → **B: PII Only** — Encrypt income, assets, SSN, disability; leave demographics (age, household size, state) unencrypted
- **Q3: Encryption Type** → **B: Randomized** — Use randomized encryption for income/assets/disability (prevents pattern attacks); deterministic for SSN only (exact-match queries)
- **Q4: JWT Token Strategy** → **B: Standard (1h access + 7d refresh)** — Access token expires in 1 hour; refresh token valid 7 days in httpOnly cookie; auto-refresh on expiration
- **Q5: Multiple Concurrent Sessions** → **B: Max 3 per user** — Allow up to 3 concurrent sessions per registered user (Phase 5); users can view and revoke sessions remotely

---

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: System MUST create an anonymous session for each user visit and store session ID in HTTP-only, Secure, SameSite=Strict cookie
- **FR-002**: Session MUST persist user-entered answers, document metadata, and eligibility results in PostgreSQL JSONB format
- **FR-003**: Session timeout MUST be tiered based on user type:
  - **Public users (anonymous)**: 30 minutes inactivity → session invalidated (sliding window: inactivity counter resets on each request)
  - **Admin/Reviewer/Analyst users** (Phase 5): 8 hours inactivity → session invalidated
  - After timeout, user must start new session (public) or login again (admin)
- **FR-004**: Admin endpoints MUST enforce role-based access control (Admin, Reviewer, Analyst roles); reject requests without valid role; unauthenticated requests receive HTTP 403 Forbidden
- **FR-005**: Sensitive fields MUST be encrypted at-rest in PostgreSQL using pgcrypto or equivalent:
  - **Randomized encryption** (same plaintext → different ciphertext each time): Income, Assets, Disability status
  - **Deterministic encryption** (same plaintext → same ciphertext): SSN (for exact-match validation queries)
  - Demographics (age, household size, state, citizenship) NOT encrypted (needed for analytics/filtering without decryption)
- **FR-006**: Encryption keys MUST be stored in Azure Key Vault (not in config files or code); keys rotated annually
- **FR-007**: All sensitive data in transit MUST be encrypted (HTTPS/TLS 1.3 enforced; HTTP → HTTPS redirect; no data in logs or error messages)
- **FR-008** (Phase 5): Registered users MUST support multiple concurrent sessions (max 3 per user); users can view active sessions (device, IP, login time) and terminate remotely
- **FR-009** (Phase 5): JWT tokens for registered users:
  - Access token: 1 hour expiration; short-lived; invalidated on password change or explicit logout
  - Refresh token: 7 days expiration; stored in httpOnly Secure SameSite=Strict cookie; used to obtain new access token without re-authentication
  - Auto-refresh: When access token near expiration (<5 min), client auto-refreshes via /api/auth/refresh

### Constitution Compliance Requirements

- **CONST-I (Code Quality)**: Authentication handlers must be testable in isolation (no database calls in auth middleware unit tests)
- **CONST-II (Testing)**: Session creation, persistence, and encryption logic must have unit tests with ≥80% coverage; integration tests verify end-to-end flow
- **CONST-III (UX Consistency)**: Error messages for expired sessions must be clear and actionable ("Your session expired after 30 minutes. Start a new eligibility check.")
- **CONST-IV (Performance)**: Session lookup must complete in <50ms; token validation <50ms; decryption <100ms even for large encrypted payloads

### Key Entities _(include if feature involves data)_

- **Session**:
  - `id` (UUID, primary key)
  - `created_at` (timestamp)
  - `last_activity_at` (timestamp)
  - `user_id` (foreign key, nullable - for registered users in Phase 5)
  - `state_code` (VARCHAR, selected state)
  - `data` (JSONB, stores answers/results)
  - `is_active` (BOOLEAN)

- **User** (Phase 5 - User Accounts):
  - `id` (UUID)
  - `email` (encrypted VARCHAR)
  - `password_hash` (bcrypt hash)
  - `role` (ENUM: User, Admin, Reviewer, Analyst)
  - `created_at` (timestamp)

- **EncryptionKey**:
  - `id` (UUID)
  - `key_id` (reference to Azure Key Vault)
  - `rotation_date` (timestamp)
  - `is_active` (BOOLEAN)

---

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: Session creation latency ≤100ms (p95)
- **SC-002**: Session data persists across 1000+ concurrent users without data loss
- **SC-003**: Zero unauthorized access to admin endpoints (test: non-admin requests rejected 100% of time)
- **SC-004**: 100% of PII fields encrypted in production database
- **SC-005**: Session timeout enforced: sessions idle >30 minutes are inaccessible
- **SC-006**: Auth unit tests ≥80% code coverage (session creation, token validation, encryption/decryption)
- **SC-007**: No authentication-related security vulnerabilities in OWASP Top 10 (SQL injection, XSS, CSRF)

---

## Implementation Path Forward

**Status**: ✅ **Specification Clarified & Ready for Planning**

All material ambiguities have been resolved via `/speckit.clarify` (5 questions answered, 2026-02-08).

**Next Step**: Run `/speckit.plan` to create:

- Detailed implementation plan
- Task breakdown (T001, T002, etc.)
- API contract definitions
- Database schema design
- Test scenario details

**Estimated Effort**: 2-3 weeks (Phase 1 foundation feature)

---
