# Implementation Plan: E1 - Authentication & Session Management

**Branch**: `001-auth-sessions` | **Date**: 2026-02-09 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-auth-sessions/spec.md`

## Summary

Deliver anonymous session creation/persistence, role-based admin access control, and at-rest encryption for PII using PostgreSQL + pgcrypto, with Azure Key Vault for key management. Provide Phase 5 scaffolding for JWT auth (access + refresh) and concurrent session management (max 3 per user), while meeting performance SLOs and Constitution testing requirements.

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: ASP.NET Core 10, EF Core 10, Npgsql.EntityFrameworkCore.PostgreSQL, Azure.Identity, Azure.Security.KeyVault.Secrets, IdentityModel  
**Storage**: PostgreSQL 16+ with pgcrypto extension  
**Testing**: xUnit, FluentAssertions, WebApplicationFactory, Testcontainers.PostgreSql  
**Target Platform**: Linux containers (Docker) + Azure App Service  
**Project Type**: Backend API (Clean Architecture)  
**Performance Goals**: session lookup <50ms p95, token validation <50ms p95, decrypt <100ms per field p95  
**Constraints**: TLS 1.3 only, Key Vault for secrets, no PII in logs, strict layering  
**Scale/Scope**: 1,000 concurrent sessions, Phase 5 multi-device sessions (max 3)

## Constitution Check _(GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.)_

**Constitution Reference**: See [MAA Constitution](/.specify/memory/constitution.md) for full principle definitions.

### Principle Alignment Assessment

**I. Code Quality & Clean Architecture**

- [x] Domain logic isolated from I/O (Domain and Application layers testable without DB/HTTP)
- [x] Dependencies explicitly injected (DI for repositories, services, clients)
- [x] No classes exceed ~300 lines; single responsibility enforced
- [x] DTO contracts explicitly defined (Session/Answer/Auth DTOs)

**II. Test-First Development**

- [x] Spec-defined scenarios drive unit/integration tests
- [x] Unit tests target domain/application layers (80%+ coverage planned)
- [x] Integration tests cover handler + repository flows via Testcontainers
- [x] Async success + failure paths tested (Key Vault outages, timeout paths)

**III. UX Consistency & Accessibility (if user-facing)**

- [x] API-only feature; user-facing message for expiration is clear and actionable

**IV. Performance & Scalability (if performance-sensitive)**

- [x] Response time SLOs defined and benchmarked (T27)
- [x] Indexes and JSONB access patterns defined in data model
- [x] Load testing targets 1,000 concurrent sessions (SC-002)

**GATE**: PASS

## Project Structure

### Documentation (this feature)

```text
specs/001-auth-sessions/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── sessions-api.openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── MAA.API/
│   ├── Controllers/
│   ├── Middleware/
│   └── Program.cs
├── MAA.Application/
│   ├── Sessions/
│   └── Services/
├── MAA.Domain/
│   └── Sessions/
├── MAA.Infrastructure/
│   ├── Data/
│   ├── Encryption/
│   └── Security/
└── MAA.Tests/
    ├── Unit/
    ├── Integration/
    └── Contract/
```

**Structure Decision**: Clean Architecture layout in `src/` with API, Application, Domain, Infrastructure, and Tests.

## Complexity Tracking

No Constitution violations.

## Phase 0 Output

- [research.md](./research.md) completed; no unresolved clarifications.

## Phase 1 Output

- [data-model.md](./data-model.md) for entity/schema design
- [contracts/sessions-api.openapi.yaml](./contracts/sessions-api.openapi.yaml)
- [quickstart.md](./quickstart.md)

## Phase 1 Constitution Re-Check

All principles still satisfied; no deviations introduced by design artifacts.
