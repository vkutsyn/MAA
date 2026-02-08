# Implementation Plan: E1 - Authentication & Session Management

**Branch**: `001-auth-sessions` | **Date**: 2026-02-08 | **Spec**: [spec.md](./spec.md)  
**Status**: Planning Phase → Ready for Phase 0 Research  
**Input**: Clarified specification from `/speckit.clarify` (all 5 ambiguities resolved 2026-02-08)

---

## Summary

**Feature**: Authentication & Session Management (E1) - Foundation epic enabling all public and admin features

**Scope**: 
- Anonymous session creation, persistence, and timeout management (30-min sliding window)
- Role-based access control for admin endpoints (Admin, Reviewer, Analyst roles)
- Sensitive data encryption (randomized for income/assets/disability; deterministic for SSN)
- JWT tokens for Phase 5 registered users (1h access + 7d refresh)
- Multi-device session support (max 3 concurrent sessions per user)

**Technical Approach**: 
- ASP.NET Core 10 with dependency injection
- PostgreSQL JSONB for session storage
- pgcrypto (PostgreSQL) for field-level encryption
- Azure Key Vault for encryption key management
- IdentityModel for JWT token handling

**MVP Blocking**: YES — E1 must complete before E4 (Wizard UI), E5 (Results), E7 (Admin Portal)

---

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: 
- ASP.NET Core 10 (authentication middleware, dependency injection)
- Entity Framework Core 10 (session/user persistence)
- Npgsql.EntityFrameworkCore.PostgreSQL (PostgreSQL driver)
- IdentityModel (JWT token generation/validation)
- Azure.Identity + Azure.Security.KeyVault.Secrets (Key Vault integration)

**Storage**: PostgreSQL 16+ with pgcrypto extension (SQL-level encryption)

**Testing**: xUnit + FluentAssertions (unit tests), WebApplicationFactory with test containers (integration tests)

**Target Platform**: Linux containers (Docker) + Azure App Service

**Performance Goals**: 
- Session lookup: <50ms (p95)
- Token validation: <50ms (p95)
- Encryption/decryption: <100ms per field (p95)
- Auth middleware overhead: <5ms per request

**Constraints**:
- Zero tolerance for data leaks (PII encrypted always)
- Session data must survive application restart (database-backed)
- Key rotation must not break old sessions (versioned encryption keys)
- OWASP Top 10 compliance (no SQL injection, XSS, CSRF, hardcoded secrets)

**Scale/Scope**: 1,000 concurrent sessions; support multi-device login

---

## Constitution Check

**GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.**

### Principle Alignment Assessment

✅ **I. Code Quality & Clean Architecture**
- [x] Auth handlers testable in isolation (session creation, token validation, encryption/decryption unit testable without database)
- [x] Dependencies explicitly injected (no service locators; IAuthenticationService, IEncryptionService, ISessionRepository interfaces)
- [x] No God Objects (separate Session handler, Token handler, Encryption handler; each <300 lines)
- [x] DTO contracts explicitly defined (SessionResponseDto, TokenResponseDto, encrypted field metadata)
- [x] No premature optimization (YAGNI: sliding window timeout implemented simply; no complex state machines)

✅ **II. Test-First Development**
- [x] Acceptance scenarios in spec testable as unit/integration tests
- [x] Target coverage ≥80% Domain/Application layers (session logic, token generation, encryption core)
- [x] All edge cases have corresponding test scenario (session expiration, key rotation, concurrency, role changes)
- [x] Async operations tested for success & error paths (e.g., Azure Key Vault failure → fallback / error response)

✅ **III. UX Consistency & Accessibility**
- [x] Not directly user-facing (API-only); no WCAG compliance needed
- [x] Error messages clear & actionable ("Your session expired after 30 minutes. Start a new eligibility check.")
- [x] Session invalid scenarios handled gracefully (HTTP 401 with message, not silent failures)

✅ **IV. Performance & Scalability**
- [x] Response time SLOs explicit (<50ms session lookup, <50ms token validation, <100ms encryption)
- [x] Caching strategy: Encryption keys cached (5-min TTL) to avoid Key Vault lookup per encrypt/decrypt
- [x] Database: Indexed queries on session.id, user.email, active_sessions.user_id
- [x] Load test target: 1,000 concurrent sessions; horizontally scalable (stateless API, database backend)
- [x] No hard-coded limits that will cause bottlenecks (connection pooling configured)

✅ **Constitution overall**: GATE PASSES — No violations. All principles aligned.

---

## Project Structure

### Documentation (this feature)

```
specs/001-auth-sessions/
├── spec.md              # Feature specification (already created, clarified)
├── plan.md              # This file (implementation plan)
├── research.md          # Phase 0: Research & decision support (TO CREATE)
├── data-model.md        # Phase 1: Domain entities & database schema (TO CREATE)
├── quickstart.md        # Phase 1: Developer quickstart guide (TO CREATE)
├── contracts/
│   ├── auth-api.openapi.yml     # OpenAPI contract for auth endpoints (TO CREATE)
│   └── session-schema.json      # Session JSONB schema definition (TO CREATE)
└── tasks.md             # Phase 2: Detailed task breakdown (TO CREATE)
```

### Source Code (repository root)

```
# Backend
backend/src/
├── MAA.Domain/
│   ├── Entities/
│   │   ├── Session.cs           (domain entity, no dependencies)
│   │   ├── User.cs              (domain entity, user roles)
│   │   └── EncryptionKey.cs     (domain entity, key versioning)
│   ├── Interfaces/
│   │   ├── ISessionRepository.cs
│   │   ├── ITokenProvider.cs
│   │   ├── IEncryptionService.cs
│   │   └── IKeyVaultClient.cs
│   └── ValueObjects/
│       ├── SessionId.cs
│       ├── EncryptedValue.cs
│       └── UserId.cs

├── MAA.Application/
│   ├── Commands/
│   │   ├── CreateSessionCommand.cs
│   │   ├── CreateUserCommand.cs (Phase 5)
│   │   └── InvalidateSessionCommand.cs
│   ├── Queries/
│   │   ├── GetSessionQuery.cs
│   │   ├── ValidateTokenQuery.cs
│   │   └── GetUserQuery.cs (Phase 5)
│   ├── Handlers/
│   │   ├── CreateSessionHandler.cs
│   │   ├── ValidateTokenHandler.cs
│   │   └── EncryptSensitiveDataHandler.cs
│   └── DTOs/
│       ├── SessionResponseDto.cs
│       ├── TokenResponseDto.cs
│       └── UserDto.cs (Phase 5)

├── MAA.Infrastructure/
│   ├── Persistence/
│   │   ├── SessionRepository.cs  (EF Core, PostgreSQL)
│   │   ├── UserRepository.cs     (Phase 5)
│   │   ├── SessionContext.cs     (DbContext setup, encryption config)
│   │   └── Migrations/
│   │       ├── 001_CreateSessionTable.cs
│   │       ├── 002_CreateUserTable.cs (Phase 5)
│   │       └── 003_AddEncryptionKeyTracking.cs
│   ├── Security/
│   │   ├── EncryptionService.cs  (pgcrypto wrapper, randomized/deterministic modes)
│   │   ├── KeyVaultClient.cs     (Azure Key Vault integration)
│   │   ├── JwtTokenProvider.cs   (JWT generation/validation)
│   │   └── PasswordHasher.cs     (Phase 5, bcrypt)
│   ├── Middleware/
│   │   ├── AuthenticationMiddleware.cs    (session validation, JWT parsing)
│   │   ├── RoleAuthorizationMiddleware.cs (role-based access control)
│   │   └── SessionTimeoutMiddleware.cs    (sliding window timeout logic)
│   └── ExternalServices/
│       └── KeyVaultService.cs

├── MAA.API/
│   ├── Controllers/
│   │   ├── SessionController.cs   (POST /api/sessions, GET /api/sessions/{id})
│   │   ├── AuthController.cs      (POST /api/auth/login, /refresh, /logout - Phase 5)
│   │   └── AdminController.cs     (GET /api/admin/rules - role-protected)
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs (401/403 responses)
│   └── Startup.cs                 (DI setup, auth middleware registration)

└── MAA.Tests/
    ├── Unit/
    │   ├── Domain/
    │   │   ├── SessionTests.cs
    │   │   └── UserTests.cs (Phase 5)
    │   ├── Application/
    │   │   ├── CreateSessionHandlerTests.cs
    │   │   ├── ValidateTokenHandlerTests.cs
    │   │   └── EncryptionServiceTests.cs
    │   └── Infrastructure/
    │       ├── EncryptionServiceTests.cs
    │       ├── JwtTokenProviderTests.cs
    │       └── KeyVaultClientTests.cs
    ├── Integration/
    │   ├── SessionPersistenceTests.cs   (WebApplicationFactory with test container PostgreSQL)
    │   ├── RoleAuthorizationTests.cs
    │   ├── EncryptionEndToEndTests.cs
    │   └── TimeoutManagementTests.cs
    └── Contract/
        └── AuthApiContractTests.cs     (validate endpoints match OpenAPI spec)
```

**Structure Rationale**: Clean architecture (Domain → Application → Infrastructure → API) enables:
1. Domain logic testable without dependencies (EncryptionService tested with mock keys)
2. Repositories abstractable (test with in-memory, integration with PostgreSQL)
3. Middleware stackable (each responsibility isolated)

---

## Complexity Tracking

**Constitution violations**: NONE

**Justified complexity decisions**:
- **Dual encryption modes** (randomized + deterministic): Needed to balance security (pattern attack prevention) with functionality (SSN exact-match validation). Documented in FR-005.
- **Tiered session timeouts** (30-min public, 8-hour admin): Needed for distinct security postures. Public users pose integrity risk; admin users are trusted. Justified in FR-003.
- **Key versioning in DB**: Needed for rolling key rotation without session invalidation. Documented in edge case handling.
- **Sliding window timeout**: Implemented to match common UX (users expect 30 min from last activity, not 30 min total). Alternative (fixed lifetime) would be simpler but poorer UX; recommendation: sliding window is justified.

---

## Phase 0: Research & Context Building

**Objective**: Resolve any NEEDS CLARIFICATION markers; document technology choices; surface constraints

### Research Questions (5 total)

### ❓ **R1: ASP.NET Core Authentication Middleware - Best Practices**
**Status**: RESEARCH NEEDED  
**Applies To**: AuthenticationMiddleware.cs, DI setup, token refresh flow  
**Key Unknowns**:
- Proper way to implement custom session middleware in .NET 10?
- How to integrate Azure AD Identity with ASP.NET Core? (Not using for MVP, but want pattern for Phase 5+)
- Custom cookie persistence strategy in ASP.NET Core (OWASP session fixation prevention)?

**Research Output Expected**: 
- Code pattern for custom session middleware (session ID generation, cookie creation, validation)
- Token refresh endpoint pattern (when to issue new JWT; when to reject refresh?)
- Session fixation attack mitigation (regenerate session ID on privilege escalation - Phase 5 importance)

---

### ❓ **R2: PostgreSQL pgcrypto - Randomized vs. Deterministic Encryption Performance**
**Status**: RESEARCH NEEDED  
**Applies To**: EncryptionService.cs, database schema design  
**Key Unknowns**:
- Performance benchmarks: randomized `encrypt()` vs. deterministic (keyed hash)?
- How to store deterministic hash for SSN comparison without full decryption?
- Key derivation in pgcrypto: using one master key or per-field keys?
- Can pgcrypto handle 1000 concurrent encryptions without contention?

**Research Output Expected**:
- Benchmark: randomized encrypt/decrypt on 15MB of income data (target <100ms)
- Schema pattern: separate column for SSN hash (deterministic, indexed) vs. encrypted SSN (randomized)
- Load test plan: simulate 1000 concurrent encryption operations

---

### ❓ **R3: Azure Key Vault Integration with .NET - Key Rotation Strategy**
**Status**: RESEARCH NEEDED  
**Applies To**: KeyVaultClient.cs, key rotation process  
**Key Unknowns**:
- How to implement key rotation without decrypting all existing sessions?
- Caching strategy: what TTL prevents excessive Key Vault API calls while staying secure?
- Fallback when Key Vault is unavailable: fail-safe or cache behavior?
- Cost implications: Key Vault API calls scale with concurrent users?

**Research Output Expected**:
- Key versioning pattern: store key_id in session record; rotation adds new key_id; old keys remain until expiration
- Caching strategy: 5-minute TTL on encryption keys in in-memory IMemoryCache (balances staleness vs. API calls)
- Backup: if Key Vault unavailable, use cached keys (with alert to ops team)

---

### ❓ **R4: JWT Token Storage & CSRF Protection in SPA (Phase 5)**
**Status**: RESEARCH NEEDED (Phase 5 planning, but decision needed now)  
**Applies To**: JwtTokenProvider.cs, Phase 5 SPA integration  
**Key Unknowns**:
- Should access token be stored in JavaScript-accessible location (localStorage) or httpOnly cookie?
- CSRF mitigation: if stored in httpOnly cookie, how to present token to JavaScript?
- Refresh token auto-renewal: trigger on page load, on timer, or on 401 response?
- Attack surface: what's the threat model for stolen refresh tokens vs. stolen access tokens?

**Research Output Expected**:
- Recommendation: httpOnly cookies for both access + refresh (requires custom header injection via fetch interceptor)
- CSRF mitigation: SameSite=Strict cookie attribute + X-CSRF-Token header validation on state-changing requests
- Refresh strategy: on 401 response (client receives 401, calls /api/auth/refresh, retries original request)

---

### ❓ **R5: xUnit + Entity Framework Core Testing - Test Container PostgreSQL Setup**
**Status**: RESEARCH NEEDED  
**Applies To**: Integration test infrastructure, SessionPersistenceTests.cs  
**Key Unknowns**:
- How to set up test container PostgreSQL for each integration test run?
- Should each test use fresh database (isolated) or shared database (faster)?
- How to test pgcrypto encryption in test environment (does test PostgreSQL have pgcrypto)?
- Performance: how long do integration tests take with database per-test pattern?

**Research Output Expected**:
- Test container pattern: Testcontainers.PostgreSql NuGet package, spin up per test class (fixture-based)
- Migration strategy: auto-run migrations on test DB startup
- pgcrypto setup: verify test PostgreSQL has pgcrypto extension; load fixtures
- Estimated integration test suite runtime: <5 minutes for 20+ tests

---

### Research Decisions to Lock In

**Decision D1: Encryption Key Management**  
**Options**: 
- A. One master key in Key Vault; derive per-field keys
- B. Separate key per field type (income key, SSN key, disability key) in Key Vault
- C. Single key for all randomized encryption; separate key for deterministic hashing

**Recommendation**: **Option C** — Simpler. One randomized master key for all PII; one deterministic key for SSN hashing. Reduces Key Vault complexity.

---

**Decision D2: Session Concurrency Handling**  
**Options**:
- A. Optimistic locking (version column; last-write-wins)
- B. Pessimistic locking (row-level lock during update)
- C. Event sourcing (immutable session history)

**Recommendation**: **Option A** — Optimistic locking. User's second answer in wizard overwrites first (rare conflict). Simple to implement; aligns with REST semantics.

---

**Decision D3: Password Hashing Algorithm (Phase 5)**  
**Options**:
- A. bcrypt (industry standard; slow by design)
- B. PBKDF2 (built into ASP.NET Identity)
- C. Argon2 (newer, more resistant to GPU attacks)

**Recommendation**: **Option A — bcrypt** — Built into ASP.NET; hardened against GPU attacks; sufficient for MVP. Argon2 optional for Phase 6+ if threat model changes.

---

**All research decisions documented for Phase 0 → Phase 1 transition**

---

## Phase 1: Design & Contracts

### Data Model (data-model.md to create)

**Entities to Design**:
- Session (anonymous sessions, Phase 1)
- User (registered users, Phase 5)
- EncryptionKey (versioned encryption keys)
- SessionActivityLog (audit trail, optional Phase 3)

**Key Decisions**:
- Session.data stored as JSONB (flexible schema; supports nested answers, results)
- Sensitive fields encrypted at column level (income→income_encrypted, ssn→ssn_encrypted)
- Unique constraints on: Session.id, User.email (unique + case-insensitive)
- Indexes on: Session.created_at, Session.last_activity_at, User.email

---

### API Contracts (contracts/ directory to create)

**Endpoints**:
- **POST /api/sessions** — Create anonymous session; return session ID in cookie + response body
- **GET /api/sessions/{id}** — Retrieve session data (decrypted); validate session active
- **POST /api/sessions/{id}/answers** — Save wizard answers to session; encrypt sensitive fields
- **DELETE /api/sessions/{id}** — Invalidate session (logout)
- **POST /api/auth/login** — Create user session with JWT (Phase 5)
- **POST /api/auth/refresh** — Refresh access token (Phase 5)
- **POST /api/auth/logout** — Invalidate all user sessions (Phase 5)
- **GET /api/admin/rules** — Admin endpoint (role-protected; returns 403 if not Admin/Reviewer)

**OpenAPI Schema**: contracts/auth-api.openapi.yml (to create)

---

### Developer Quickstart (quickstart.md to create)

**Contents**:
- Local development setup (Docker Compose with PostgreSQL)
- Running tests (xUnit, integration tests with test containers)
- Debugging auth middleware
- Encryption key setup (local development uses hardcoded test key; production uses Key Vault)
- API testing examples (curl commands, Postman collection)

---

## Phase 2: Implementation Task Breakdown

**Tasks will be detailed in tasks.md (to create)**

**High-Level Task Categories**:

### T0x: Setup & Infrastructure (4 tasks)
- T01: Create .NET 10 project structure (Domain, Application, Infrastructure, API layers)
- T02: Set up PostgreSQL database; initialize migrations framework
- T03: Configure dependency injection (Autofac or built-in DI container)
- T04: Set up xUnit + test infrastructure (WebApplicationFactory, test containers)

### T1x: Domain & Data Model (6 tasks)
- T10: Create Session, User, EncryptionKey domain entities
- T11: Create ISessionRepository, ITokenProvider, IEncryptionService, IKeyVaultClient interfaces
- T12: Implement PostgreSQL session storage (EF Core migrations, DbContext)
- T13: Design and validate JSONB schema for session.data
- T14: Set up encryption key versioning schema
- T15: Unit tests for domain entities

### T2x: Authentication & Encryption (8 tasks)
- T20: Implement EncryptionService (randomized + deterministic modes)
- T21: Integrate Azure Key Vault client
- T22: Implement JwtTokenProvider (token generation, validation, refresh logic - Phase 5 ready)
- T23: Implement password hasher (Phase 5, bcrypt)
- T24: Session creation handler (CreateSessionCommand → session persisted, ID returned)
- T25: Session validation middleware (check active, timeout, role-based)
- T26: Integration tests for encryption/decryption end-to-end
- T27: Performance benchmarking (encryption <100ms, session lookup <50ms)

### T3x: API Endpoints (6 tasks)
- T30: SessionController (POST /api/sessions, GET /api/sessions/{id}, POST /api/sessions/{id}/answers, DELETE)
- T31: AuthController (POST /api/auth/login, /refresh, /logout - Phase 5 stubs only)
- T32: Admin middleware (role-based checks, 403 responses)
- T33: Exception handling middleware (401/403 error formatting)
- T34: Contract tests (API matches OpenAPI schema)
- T35: Integration tests (full wizard → store answers → retrieve flow)

### T4x: Deployment & Monitoring (3 tasks)
- T40: Docker setup (Dockerfile, Docker Compose for dev)
- T41: Azure App Service configuration + Key Vault secrets setup
- T42: Observability: session metrics, error logging, performance monitoring

**Estimated Effort**: 90-110 story points (2-3 weeks, 3-4 engineers)

---

## Success Criteria (End-of-Phase-1)

✅ **Technical**:
- [ ] Anonymous sessions created and persisted (MVP)
- [ ] Session timeout enforced: 30-min (public), 8-hour (admin)
- [ ] Sensitive fields encrypted randomized; SSN deterministic; demographics unencrypted
- [ ] All auth logic unit tests pass; ≥80% coverage (Domain + Application layers)
- [ ] Integration tests pass with test container PostgreSQL
- [ ] API contract tests validate OpenAPI spec compliance
- [ ] Performance benchmarks meet SLOs (<50ms session lookup, <100ms encryption)

✅ **Process**:
- [ ] All Constitution principles verified (code quality, testing, UX, performance)
- [ ] Code review approved (2+ team leads)
- [ ] CI/CD pipeline green (build, lint, test, contract validation)
- [ ] No OWASP Top 10 vulnerabilities (security scan)

✅ **Documentation**:
- [ ] OpenAPI spec (contracts/auth-api.openapi.yml) complete
- [ ] Quickstart guide for developers
- [ ] Inline code comments explaining encryption strategy & edge cases
- [ ] ADR (Architecture Decision Record) documenting tiered timeouts, encryption choices

---

## Timeline & Sequencing

```
Week 1 (3 days complete, due 2/12):
  T01-T04: Setup & infrastructure
  T10-T11: Domain entities & interfaces

Week 2:
  T12-T15: Data model & migrations
  T20-T23: Encryption, KeyVault, JWT (Phase 5 ready)

Week 3:
  T24-T27: Session handlers & integration tests
  T30-T35: API endpoints & contracts

Week 4 (1-2 days):
  T40-T42: Deploy, monitoring, final review
```

**Critical Path**: T01 → T02 → T12 → T20 → T24 → T30 (sequential; no parallelization)

---

## Dependencies & Blockers

**Blocks**: E4 (Wizard UI), E5 (Results), E6 (Documents), E7 (Admin Portal), E8 (Rule Management)  
**Blocked By**: None (independent infrastructure)

**External Dependencies**:
- Azure subscription + Key Vault access
- PostgreSQL 16+ with pgcrypto extension
- .NET 10 SDK
- Test containers (Testcontainers.PostgreSql NuGet)

---

## Risks & Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|-----------|
| Key Vault availability down (network/auth issues) | Session creation blocked | Medium | Implement 5-min key cache; alert if stale |
| Encryption performance fails SLO (<100ms) | Page load slow; user churn | Medium | Early perf testing (Week 1/2); profile bottlenecks |
| pgcrypto not available in prod PostgreSQL | Deploy fails | Low | Verify during DB provisioning (T02) |
| JWT token refresh loop (endless 401) | Users stuck | Low | Comprehensive refresh logic tests; timeout handling |
| Concurrent session writes cause conflicts | Data consistency issues | Low | Optimistic locking + test concurrency scenarios |

---

**Phase 0 Input**: Clarified specification (spec.md)  
**Phase 1 Output**: research.md, data-model.md, contracts/, quickstart.md, tasks.md (created next)  
**Phase 2 Output**: Implemented, tested, deployed E1 feature  

**Status**: ✅ **Ready for Phase 0 Research Document Creation**

**Next Command**: Generate Phase 0 research.md, Phase 1 data-model.md, contracts, quickstart.md
