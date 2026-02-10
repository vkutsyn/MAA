# Changelog

All notable changes to the Medicaid Application Assistant (MAA) project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

#### Feature 006: State Context Initialization Step (2026-02-10)

- **State Context Initialization**: Users can now enter their ZIP code to establish Medicaid jurisdiction context before beginning the eligibility evaluation.

  **Backend (C# / .NET 10)**:
  - Added `StateContext` domain entity to track user's state selection and ZIP code
  - Added `StateConfiguration` domain entity for state-specific Medicaid program settings
  - Added `ZipCodeValidator` for validating 5-digit U.S. ZIP codes
  - Added `StateResolver` for ZIP-to-state mapping resolution
  - Added `ZipCodeMappingService` singleton for in-memory ZIP code lookups (~42,000 entries)
  - Added `StateConfigurationSeeder` for seeding state configurations from JSON
  - Added Entity Framework Core migrations for `state_contexts` and `state_configurations` tables
  - Added `StateContextController` with POST, GET, and PUT endpoints:
    - `POST /api/state-context` - Initialize state context from ZIP code
    - `GET /api/state-context?sessionId={id}` - Retrieve state context for session
    - `PUT /api/state-context` - Update state context (manual override)
  - Added command handlers:
    - `InitializeStateContextHandler` - Creates state context from ZIP code
    - `UpdateStateContextHandler` - Updates state context with manual override
  - Added query handler: `GetStateContextHandler` - Retrieves state context
  - Added FluentValidation validators for state context requests
  - Added repositories:
    - `StateContextRepository` - EF Core repository for state contexts
    - `StateConfigurationRepository` - Cached repository for state configurations (IMemoryCache, 24h TTL)
  - Added XML documentation comments to all public APIs

  **Frontend (React 19 / TypeScript)**:
  - Added `/state-context` route for ZIP code entry step
  - Added `StateContextStep` page component with ZIP code form and state confirmation
  - Added `ZipCodeForm` component with React Hook Form + Zod validation
  - Added `StateConfirmation` component to display detected state
  - Added `StateOverride` component for manual state selection (dropdown with all 50 states + DC)
  - Added TanStack Query hooks:
    - `useInitializeStateContext` - Mutate hook for initializing state context
    - `useGetStateContext` - Query hook for fetching state context
    - `useUpdateStateContext` - Mutate hook for updating state context
  - Added API client functions for state context endpoints
  - Added TypeScript type definitions for state context DTOs
  - Added error handling with inline field errors and toast notifications (shadcn/ui)
  - Added WCAG 2.1 AA compliance features:
    - Proper ARIA labels and descriptions
    - Error messages announced to screen readers (`aria-live`, `role="alert"`)
    - Keyboard-accessible form controls
  - Added responsive design support (mobile 375px → desktop 1920px)
  - Added JSDoc documentation for all exported functions

  **Data**:
  - Added `state-configs.json` with sample state configurations (10 states)
  - Added ZIP-to-state mapping data source (SimpleMaps database integration)
  - Database tables indexed on `session_id`, `state_code` for performance

  **Performance**:
  - ZIP validation: <1ms (regex-based, no I/O)
  - State resolution: <10ms (in-memory dictionary lookup)
  - API endpoint: <1000ms p95 (including database round-trip)
  - State configurations cached in-memory (24h TTL) to eliminate repeated DB queries

  **Testing**:
  - Added E2E test scenarios document (`E2E-TEST-SCENARIOS.md`)
  - Manual test scenarios for:
    - Valid ZIP code entry (US1)
    - State override functionality (US2)
    - Invalid ZIP code handling (US3)
    - Accessibility compliance (keyboard navigation, screen reader)
    - Responsive design (mobile, tablet, desktop)
    - Performance validation (<1000ms p95)

### Changed

- Updated `Session` entity to support one-to-one relationship with `StateContext`
- Updated React Router configuration to include `/state-context` route
- Updated DI container registration in `Program.cs` to include state context services

### Technical Debt

- State configurations currently limited to 10 sample states; production deployment requires all 50 states + DC + territories
- Frontend linting configuration needs migration to ESLint 9.x flat config format
- Prettier not yet integrated into frontend workflow

### Breaking Changes

None. This is a new feature with no impact on existing functionality.

---

## Previous Releases

### [0.4.0] - Feature 005: Frontend Authentication Flow (2026-02-07)

- Added React-based authentication UI
- Added JWT token management
- Added login/register pages
- Added session persistence with localStorage
- Added authenticated route protection

### [0.3.0] - Feature 004: UI Implementation (2026-02-05)

- Added shadcn/ui component library integration
- Added Tailwind CSS styling framework
- Added React Hook Form integration
- Added Zod validation schemas
- Added responsive design patterns

### [0.2.0] - Feature 003: Swagger/OpenAPI Documentation (2026-02-03)

- Added Swashbuckle integration for API documentation
- Added Swagger UI at `/swagger`
- Added OpenAPI specification generation
- Added XML documentation comments

### [0.1.0] - Feature 002: Rules Engine (2026-02-01)

- Added MAGI-based eligibility evaluation engine
- Added Federal Poverty Level (FPL) threshold calculations
- Added state-specific rule loading
- Added eligibility result DTOs
- Added adult, child, and pregnant eligibility categories

### [0.0.1] - Feature 001: Auth & Sessions (2026-01-25)

- Initial project setup
- Added JWT-based authentication
- Added session management with PostgreSQL
- Added user registration and login
- Added encrypted session storage
- Added session timeout handling

---

## Release Notes Template

### [Version] - YYYY-MM-DD

#### Added

- New features

#### Changed

- Changes to existing features

#### Deprecated

- Features marked for removal

#### Removed

- Features removed from project

#### Fixed

- Bug fixes

#### Security

- Security patches
