<!-- Sync Impact Report: Constitution v1.0.0 -->
<!-- 
  Changes from [Initial Ratification]:
  - NEW VERSION: 1.0.0 (Initial adoption for MAA project)
  - PRINCIPLES ADDED: 4 core principles (Code Quality, Testing Standards, UX Consistency, Performance)
  - SECTIONS ADDED: Security & Compliance, Integration Testing, Performance Standards
  - Templates Updated: ✅ spec-template.md, ✅ plan-template.md, ✅ tasks-template.md
  - Follow-up: None - constitution fully specified at ratification
-->

# Medicaid Application Assistant (MAA) Constitution

A governance framework for the MAA project emphasizing code quality, rigorous testing, accessible user experiences, and measurable performance standards.

## Core Principles

### I. Code Quality & Clean Architecture (MUST)

Every component MUST follow clean architecture principles ensuring testability, maintainability, and clear separation of concerns.

**Non-Negotiable Rules**:
- **Strict Layering**: Domain logic strictly separated from I/O (database, HTTP, file system)
  - `MAA.Domain/` contains pure business logic, decision trees, eligibility calculations
  - `MAA.Application/` contains use cases, handlers, command/query DTOs
  - `MAA.Infrastructure/` contains repository implementations, external service clients
  - `MAA.API/` contains only controller setup and middleware
  - Frontend code organized by feature modules, not by layer
- **Strong Typing**: All code MUST use TypeScript (frontend) and C# 13 (backend)
  - Avoid `any` type; use explicit union types or generics
  - Leverage null-safety features (C# nullable reference types, TypeScript strict null checks)
  - DTO contracts must be explicitly defined and version-tracked
- **No God Objects**: Classes/modules MUST have a single, clear responsibility
  - If a class exceeds ~300 lines of code, refactor immediately
  - Use composition over inheritance; favor interface segregation
- **Explicit Dependencies**: All dependencies MUST be injected, never resolved via service locator
  - Backend: Use ASP.NET Core dependency injection
  - Frontend: Pass props explicitly; use custom React hooks for shared logic
- **No Premature Optimization**: "Make it clear first, fast second" – YAGNI principle applies

**Rationale**: Clean architecture reduces bug surface area, enables parallel development, and ensures code changes don't cascade unpredictably. Accessibility and performance are emergent properties of good design.

---

### II. Test-First Development (MANDATORY)

All production code MUST be test-driven. Tests are written and approved BEFORE implementation begins. Red-Green-Refactor cycle strictly enforced.

**Non-Negotiable Rules**:
- **Pre-Implementation Tests**: Users approve test scenarios in spec → tests fail → implementation added → tests pass
  - Backend: xUnit tests with FluentAssertions, >80% coverage for Domain/Application layers
  - Frontend: Vitest + React Testing Library for hooks, components, utilities
  - All edge cases listed in spec MUST have corresponding test case(s)
- **Three Test Tiers**:
  - **Unit Tests**: Pure functions, domain logic, validation rules
    - Database access mocked or in-memory
    - External services mocked (Blob Storage, OCR, PDF generation)
  - **Integration Tests**: Multi-layer scenarios (handlers + repository, components + hooks)
    - Use WebApplicationFactory (backend) with test container PostgreSQL
    - Use mocked React Query cache (frontend)
  - **Contract Tests**: API endpoint validation against published OpenAPI schema
    - Verify request/response shape matches specification
    - Backend: Generated from Swashbuckle; Frontend: axios interceptor tests
- **Test Coverage Gates**: CI/CD MUST enforce minimum thresholds
  - Domain/Application: 80% coverage (line + branch)
  - Infrastructure/API Controllers: 60% coverage (mocked externals acceptable)
  - Frontend hooks/utilities: 75% coverage
  - Failing builds block merge to `main`
- **Testability Acceptance Criteria**:
  - Feature can be unit tested in isolation (no database, no HTTP calls required)
  - All async operations tested for success AND error paths
  - Accessibility assertions included on all UI tests (`screen.getByRole`, `aria-label` verification)

**Rationale**: Test-driven development prevents bugs at the source, documents expected behavior, and creates a safety net for refactoring. High coverage on domain logic (80%+) prevents regression in core eligibility rules.

---

### III. User Experience Consistency & Accessibility (WCAG 2.1 AA Mandatory)

Every user-facing component MUST be accessible, responsive, and communicate clearly in plain language. Consistency across all states and user journeys is non-negotiable.

**Non-Negotiable Rules**:
- **Accessibility (WCAG 2.1 AA)**: Every frontend feature MUST pass accessibility compliance
  - All interactive elements MUST have semantic HTML (`<button>`, `<nav>`, `<form>`, `<label>`)
  - All form inputs MUST have associated labels; all images MUST have alt text
  - Color MUST NOT be the only differentiator; use patterns, icons, text
  - Focus states MUST be visible; keyboard navigation MUST work without mouse
  - Screen reader testing REQUIRED for new wizard steps, results pages, document checklists
  - Run axe DevTools on every new page; zero accessibility violations before merge
- **Mobile-First Responsive Design**: All screens MUST work on mobile (375px) → tablet (768px) → desktop (1920px)
  - Breakpoints: `sm: 640px`, `md: 768px`, `lg: 1024px`, `xl: 1280px` (Tailwind defaults)
  - Touch targets ≥44×44px for mobile
  - Avoid hover-only interactions; use :active states for touch
- **Plain Language & Clarity**: All user-facing text MUST be clear, jargon-free, and tested with users
  - No medical/legal jargon without explanation (e.g., "MAGI" must be explained: "Modified Adjusted Gross Income")
  - Error messages MUST explain what went wrong AND how to fix it (not just "Invalid input")
  - Disclaimers and legal language MUST be visually distinct, concise, and copyable
  - Question phrasing MUST match the spec; deviation requires spec amendment
- **Consistent UI Components**: All interactive elements MUST use shadcn/ui + Tailwind CSS
  - No custom CSS for buttons, inputs, modals, dropdowns
  - Custom styling ONLY for layout/spacing; component behavior must match design system
  - Colors must align with design system (primary, secondary, danger, success, warning)
- **Actionable Feedback**: Results and messages MUST explain "why" clearly
  - Eligibility result MUST include: status, matched programs, explanation, next steps
  - Document checklist MUST show: required items, optional items, upload status
  - Errors MUST suggest remedies ("Try entering income once a month instead of variation")

**Rationale**: Accessible, consistent UX builds user trust and ensures the app serves all populations fairly. Plain language reduces support burden and increases application completion rates.

---

### IV. Performance & Scalability Requirements (Measurable Targets)

All code MUST meet explicit performance targets. Performance issues are treated as functional bugs.

**Non-Negotiable Rules**:
- **Response Time Targets**: All endpoints/interactions MUST meet these SLOs
  - Eligibility evaluation: ≤2 seconds (p95)
  - Document upload: ≤5 seconds for 15MB file (stream to blob storage)
  - Application packet generation: ≤10 seconds (server-side PDF + state forms)
  - Wizard step load: ≤500 milliseconds (client-side question tree + next questions rendered)
  - Admin page load: ≤3 seconds (cached rule data, paginated results)
  - Violations MUST be addressed before feature merge
- **Optimization Strategies** (in priority order):
  - Caching: Cache immutable data (FPL tables, state metadata, question taxonomy) in Redis with 24h TTL
  - Database: Index queries on `state_id`, `eligibility_status`, `created_at`; use JSONB efficiently
  - Frontend: Code-split by feature route; lazy-load admin portal code; React Query for server state
  - PDFs: Pre-compile Handlebars/QuestPDF templates; cache rendered forms per state/year
  - Async: Offload OCR, document virus scanning, regulation monitoring to background jobs (queued, not blocking)
- **Scalability Architecture**:
  - Backend: Stateless API; horizontal scaling via load balancer (Azure App Service native, AWS ECS Fargate, or k8s)
  - Database: Read replicas for analytics; connection pooling (Entity Framework Core pooling)
  - Frontend: Static hosting (Azure Static Web Apps, AWS S3 + CloudFront); CDN for assets
  - Concurrent users target: 1,000 simultaneous (test with load testing)
- **Monitoring & Alerting**:
  - Application Insights (Azure) OR CloudWatch (AWS) MUST track: response times (p50/p95/p99), error rates, database query times
  - Alert thresholds: ANY response > 5 seconds (p95), error rate > 0.1%, database query > 500ms
  - Monthly performance review: report p95 latencies by endpoint, identify bottlenecks

**Rationale**: Performance is a quality attribute. Slow features harm completion rates and user trust, especially for vulnerable populations with limited connectivity.

---

## Security & Compliance Architecture

### Authentication & Authorization
- **Anonymous Sessions**: Cookie-based (HttpOnly, Secure, SameSite=Strict)
- **Registered Users**: JWT (short-lived 1h) + Refresh Token (7 days)
- **Admin Users**: JWT with role claims (Admin, Reviewer, Analyst)
- **Authorization Enforcement**: Role-based policies enforced at middleware + handler level; never in SQL queries

### Data Protection at Rest & in Transit
- **At Rest**: Column-level encryption for sensitive fields (income, assets, disability, SSN)
  - PostgreSQL: pgcrypto extension for column encryption
  - Blob storage: Client-side encryption before upload
- **In Transit**: HTTPS (TLS 1.3) enforced on all endpoints; redirect HTTP → HTTPS
- **Secrets Management**: Azure Key Vault (Azure) or AWS Secrets Manager (AWS); never store secrets in config files
- **PII Retention**: Uploaded documents auto-expire after 90 days (configurable); user consent required for session save

### Input Validation & File Upload Security
- **Defense in Depth**: Validate on frontend (UX) + backend (security gate)
  - Frontend: Zod schemas in React Hook Form
  - Backend: FluentValidation + explicit ModelState checks
- **File Upload Constraints**:
  - Max 15MB per file; antivirus scan required (ClamAV or cloud service)
  - Validate magic bytes (not just file extension); reject executables
  - Store on separate CDN domain (not same-origin) to prevent XSS
- **SQL Injection Prevention**: NEVER concatenate SQL strings; use parameterized queries (Entity Framework Core)

### Audit & Compliance
- **Audit Logging**: All admin actions (rule approvals, content overrides) logged with user, timestamp, before/after state
  - Stored in PostgreSQL audit table (immutable)
  - Compliance analysts can review audit trail via admin portal
- **Regulation Compliance**: All generated documents include disclaimers; system MUST NOT provide legal advice
  - "This tool is for informational purposes; does not constitute legal advice; eligibility determined by state"

---

## Integration Testing Scope & Standards

### Focus Areas (MUST have integration tests)
- **Rules Engine**: Sample test cases for each state (income at threshold, edge cases, special programs)
  - Data source: `/tests/data/[state]-test-cases.json` with input scenarios and expected outputs
  - Test container PostgreSQL for full integration
- **Eligibility Evaluation Flow**: End-to-end from wizard answers → eligibility result → matched programs
  - Mock OCR but test document classification logic
- **Document Upload & Processing**: File upload → virus scan → OCR extraction → validation result
  - Use real Tesseract or mock with known outputs
- **Admin Approval Workflow**: Regulation change → AI summary → admin review → rule update → version incremented
- **API Contract**: All endpoints validate against OpenAPI spec; breaking changes caught in PR

### Standards
- Integration tests use repository pattern with in-memory/test database (no mocked repositories in integration level)
- Coverage target: All core user journeys plus error paths
- Test data organized by state and scenario (MAGI, non-MAGI, SSI, etc.)

---

## Development Workflow & Quality Gates

### Code Review Requirements
- Every PR MUST have:
  - ✅ Tests passing (unit + integration)
  - ✅ Accessibility review (frontend: axe DevTools)
  - ✅ Performance impact assessment (response times, bundle size)
  - ✅ Constitution compliance checklist verified
  - ✅ At least one approval from maintainers
- No merge without all checks passing; no exceptions without documented justification

### CI/CD Gates (Automated)
1. **Lint & Format**: ESLint + Prettier (frontend), dotnet format (backend) must pass
2. **Build**: `dotnet build` and `npm run build` must succeed (no warnings escalated to errors)
3. **Unit Tests**: `dotnet test` and `npm run test` with coverage reports
4. **Integration Tests**: WebApplicationFactory tests on test container PostgreSQL
5. **Accessibility**: axe DevTools CLI on all new React routes (zero violations)
6. **Performance**: Lighthouse CI check on frontend routes; flag if FCP > 3s or CLS > 0.1
7. **Constitution Validation**: Verify principle compliance in plan.md + spec.md before task creation

### Branch Strategy
- Main branch (`main`) is production-ready; deploy directly from `main` via Azure Pipelines or GitHub Actions
- Feature branches: `NNN-feature-name` (e.g., `001-eligibility-wizard`)
- Hotfix branches: `hotfix/YYYY-MM-DD-issue-description`
- All branches protected; require PR reviews and all checks passing

---

## Governance & Amendment Procedure

### Constitution Authority
This Constitution supersedes all other project guidance, best practices, and individual preferences. It is the source of truth for development standards.

### Versioning & Amendment Process
- **Version Format**: MAJOR.MINOR.PATCH (semantic versioning)
  - **MAJOR**: Incompatible principle change or removal (requires unanimous team agreement)
  - **MINOR**: New principle or substantive guidance expansion (requires lead approval + team consensus)
  - **PATCH**: Clarifications, wording fixes, non-semantic refinements (lead approval only)
- **Amendment Procedure**:
  1. Propose change with rationale in Constitution Issue (link to spec/PRD impact)
  2. Update Constitution markdown with new/amended principle text
  3. Identify all dependent templates that require updates (spec, plan, tasks templates)
  4. Update those templates inline and note in Sync Impact Report
  5. Document version bump reason and affected sections
  6. Require lead approval before merge
- **Compliance Review**: Quarterly (early Feb, May, Aug, Nov) to validate code adherence

### Template Synchronization
When the Constitution changes, these templates MUST be reviewed and updated:
- ✅ `.specify/templates/spec-template.md` – Reference Constitution principles in requirements section
- ✅ `.specify/templates/plan-template.md` – Include Constitution Check gate; verify no principle conflicts
- ✅ `.specify/templates/tasks-template.md` – Align task categorization with principle-driven activities

### Enforcement
- All PRs/reviews MUST verify Constitution compliance
- Principle violations MUST be addressed before merge; no exceptions without documented justification
- If complexity is justified (e.g., regulatory requirement), document rationale explicitly in code comments

### Runtime Guidance
For day-to-day implementation decisions and architecture guidance, refer to the technical stack documentation in [tech-stack.md](../tech-stack.md). The Constitution sets policy; tech-stack.md provides implementation patterns.

---

## Glossary & Principles Summary

| Principle | Key Requirement | Success Measure |
|-----------|-----------------|-----------------|
| **Code Quality** | Clean architecture, strong typing, single responsibility | Code review approval, <300 line classes, zero `any` types |
| **Testing Standards** | Test-first, 80%+ coverage, red-green-refactor | All PRs include tests; coverage reports reviewed |
| **UX Consistency** | WCAG 2.1 AA, mobile-first, plain language | Zero accessibility violations, all user journeys tested, user feedback positive |
| **Performance** | ≤2s eligibility, ≤5s uploads, ≤500ms interactions | Load test results, p95 latencies tracked, alerts trigger >5s |

---

**Version**: 1.0.0 | **Ratified**: 2026-02-08 | **Last Amended**: 2026-02-08
