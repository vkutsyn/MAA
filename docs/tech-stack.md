# Technical Stack & Architecture Decisions

**Project**: MAA (Medicaid Application Assistant)  
**Date**: 2026-02-08  
**Status**: Approved  
**Scope**: Project-wide (applies to all features)  
**References**: [Constitution](../.specify/memory/constitution.md) | [Requirements](../doc/requirements.md)

---

## Technology Stack (Approved)

### Backend Stack

**Framework**: .NET 10
- **Language**: C# 13
- **API Framework**: ASP.NET Core Web API (Minimal APIs or Controllers)
- **Runtime**: .NET 10 runtime on Linux containers

**Rationale**:
- .NET 10 is the latest stable release (November 2025)
- High performance, cross-platform
- Strong typing aligns with Constitution I (Code Quality)
- Excellent observability and telemetry support (Constitution IV)
- C# 13 features improve code expressiveness and null safety

### Frontend Stack

**Framework**: React 19+ with TypeScript 5+
- **Language**: TypeScript 5.7+
- **Build Tool**: Vite 6.x
- **UI Library**: shadcn/ui (Radix UI primitives + Tailwind CSS)
- **State Management**: Zustand or React Context (start simple)
- **Forms**: React Hook Form + Zod validation
- **Data Fetching**: TanStack Query (React Query v5)

**Rationale**:
- React 19 latest features (automatic batching, Server Components support)
- TypeScript 5.7+ provides enhanced type inference and null safety
- Vite 6.x offers fast dev experience and optimized builds
- shadcn/ui is accessible (WCAG 2.1 AA requirement) and customizable
- Zustand is lightweight; scale to Redux Toolkit only if needed (YAGNI)
- React Hook Form + Zod aligns with Constitution I (explicit validation)

---

## Core Dependencies

### Backend (.NET)

```xml
<ItemGroup>
  <!-- Web API -->
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.*" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="7.0.*" />
  
  <!-- Database -->
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.*" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.*" />
  
  <!-- Authentication/Authorization -->
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.*" />
  <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.*" />
  
  <!-- Configuration -->
  <PackageReference Include="Microsoft.Extensions.Configuration.AzureKeyVault" Version="10.0.*" />
  
  <!-- Validation -->
  <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.*" />
  
  <!-- Rules Engine -->
  <PackageReference Include="JsonLogic.Net" Version="1.0.*" />
  
  <!-- PDF Generation -->
  <PackageReference Include="QuestPDF" Version="2025.1.*" />
  
  <!-- OCR -->
  <PackageReference Include="Tesseract" Version="5.2.*" />
  
  <!-- Observability -->
  <PackageReference Include="Serilog.AspNetCore" Version="10.0.*" />
  <PackageReference Include="Serilog.Sinks.Console" Version="6.0.*" />
  <PackageReference Include="Serilog.Sinks.ApplicationInsights" Version="5.0.*" />
  
  <!-- Testing -->
  <PackageReference Include="xUnit" Version="2.9.*" />
  <PackageReference Include="FluentAssertions" Version="7.0.*" />
  <PackageReference Include="Moq" Version="4.20.*" />
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.*" />
</ItemGroup>
```

### Frontend (React TS)

```json
{
  "dependencies": {
    "react": "^19.0.0",
    "react-dom": "^19.0.0",
    "react-router-dom": "^6.26.0",
    "typescript": "^5.7.0",
    
    "@radix-ui/react-dialog": "^1.1.0",
    "@radix-ui/react-dropdown-menu": "^2.1.0",
    "@radix-ui/react-select": "^2.1.0",
    "@radix-ui/react-toast": "^1.2.0",
    "@radix-ui/react-progress": "^1.1.0",
    
    "tailwindcss": "^3.4.0",
    "class-variance-authority": "^0.7.0",
    "clsx": "^2.1.0",
    "tailwind-merge": "^2.5.0",
    
    "react-hook-form": "^7.53.0",
    "zod": "^3.24.0",
    "@hookform/resolvers": "^3.9.0",
    
    "@tanstack/react-query": "^5.59.0",
    "zustand": "^5.0.0",
    
    "axios": "^1.7.0",
    "date-fns": "^4.1.0"
  },
  "devDependencies": {
    "@types/react": "^19.0.0",
    "@types/react-dom": "^19.0.0",
    "@vitejs/plugin-react": "^4.3.0",
    "vite": "^6.0.0",
    
    "vitest": "^2.1.0",
    "@testing-library/react": "^16.0.0",
    "@testing-library/jest-dom": "^6.6.0",
    "@testing-library/user-event": "^14.5.0",
    
    "eslint": "^9.0.0",
    "@typescript-eslint/eslint-plugin": "^8.0.0",
    "@typescript-eslint/parser": "^8.0.0",
    "prettier": "^3.3.0"
  }
}
```

---

## Infrastructure Stack

### Database
**Primary**: PostgreSQL 16+
- JSONB for rules, answers, snapshots
- Full-text search for regulation monitoring
- PostGIS extension (future: geographic state lookups)

**Rationale**: JSONB aligns with clarifications (nested_json, db_jsonb). Open-source, ACID compliant, excellent JSON support.

### Blob Storage
**Choice**: Azure Blob Storage OR AWS S3
- Document uploads (PDF, JPEG, PNG)
- Application packet PDFs
- Regulation monitoring snapshots

**Rationale**: Cost-effective, durable, integrates with virus scanning services.

### Caching
**Choice**: Redis 7+
- Session state (if not using database sessions)
- Eligibility result caching (short TTL)
- Rate limiting

**Rationale**: Fast, simple, widely supported. Optional for MVP - add when performance testing shows need.

### Authentication
**Choice**: .NET Identity + JWT
- Optional accounts (align with clarifications: optional_account_for_resume)
- JWT for stateless API auth
- Cookie-based sessions for anonymous users

**Rationale**: Built-in, secure, testable. Avoid external auth providers for MVP (simplicity principle).

---

## Development Tools

### Version Control
- **Git** with conventional commits
- **Branching**: Feature branches per Speckit (e.g., `001-maa-core-system`)

### CI/CD
- **Backend**: .NET SDK 10.0, `dotnet test`, `dotnet publish`
- **Frontend**: Node.js 22 LTS, `npm run test`, `npm run build`
- **Linting**: ESLint + Prettier (frontend), dotnet format (backend)
- **Gates**: All tests pass, no lint errors, Constitution checklist validated

### Containerization
- **Docker** for local dev and deployment
- `docker-compose.yml` for local full-stack setup

### Observability
- **Logging**: Serilog (backend), console.log wrapper (frontend)
- **Metrics**: Application Insights OR Prometheus/Grafana
- **Tracing**: OpenTelemetry (future)

---

## Architecture Patterns

### Backend Patterns

**1. Clean Architecture (Layered)**
```
MAA.API/              # ASP.NET Core Web API (Controllers, Middleware)
MAA.Application/      # Business logic, Use Cases, DTOs
MAA.Domain/           # Entities, Domain logic, Interfaces
MAA.Infrastructure/   # EF Core, Repositories, External services
MAA.Tests/            # Unit, Integration, Contract tests
```

**Rationale**: Aligns with Constitution I (pure domain logic, separation of I/O). Testable, maintainable.

**2. CQRS-Lite (Commands/Queries)**
- Commands: Mutate state (e.g., SaveAnswer, ApproveRule)
- Queries: Read state (e.g., GetEligibilityResult, GetDocumentChecklist)
- No event sourcing (YAGNI). Simple command/query handlers.

**Rationale**: Clear separation encourages testability. Aligned with Constitution II (deterministic, testable).

**3. Mediator Pattern**
- Use MediatR for command/query dispatch
- Keeps controllers thin, logic in handlers

**4. Repository Pattern**
- Abstract database access behind interfaces
- EF Core as implementation
- Enable mocking for unit tests

### Frontend Patterns

**1. Feature-Based Structure**
```
src/
├── features/
│   ├── wizard/         # Eligibility questionnaire
│   ├── documents/      # Document upload and validation
│   ├── results/        # Eligibility results display
│   ├── admin/          # Admin portal (rules, monitoring)
│   └── shared/         # Shared components
├── lib/                # Utilities, API client, types
├── hooks/              # Custom React hooks
└── components/ui/      # shadcn/ui components
```

**Rationale**: Feature-based is more maintainable than layer-based for UIs. Aligns with user stories.

**2. Custom Hooks for Business Logic**
- `useWizard()` - Question flow, state management
- `useEligibility()` - API calls with React Query
- `useDocumentUpload()` - Upload, validation, progress

**Rationale**: Encapsulation, reusability, testability.

**3. Server State vs UI State**
- Server state: React Query (eligibility, documents, rules)
- UI state: Zustand or Context (wizard step, form drafts)

**Rationale**: React Query handles caching, refetching. Zustand is simple for local UI state.

---

## Data Flow Architecture

### Eligibility Evaluation Flow
```
User → React Form → API /api/eligibility/evaluate
                         ↓
                    EligibilityHandler
                         ↓
                    RulesEngine (JSONLogic)
                         ↓
                    EligibilityResult + Explanation
                         ↓
                    Response → React Query Cache → UI
```

### Document Upload Flow
```
User → File Input → API /api/documents/upload
                         ↓
                    Blob Storage (S3/Azure)
                         ↓
                    Async: Virus Scan + OCR
                         ↓
                    Document Entity (metadata in DB)
                         ↓
                    WebSocket/Polling → UI Update
```

### Regulation Monitoring Flow
```
Scheduled Job → Crawler Service
                    ↓
               Download State PDFs
                    ↓
               Diff Detector
                    ↓
               AI Summarizer (future: Azure OpenAI)
                    ↓
               RegulationChangeLog (pending approval)
                    ↓
               Admin Portal Alert
```

---

## Key Architectural Decisions

### Decision 1: Web App (Not Mobile Native)
**Choice**: Responsive web app, mobile-first CSS  
**Alternative Rejected**: React Native  
**Rationale**: Spec states "web-first, mobile-responsive". Single codebase, faster to market. WCAG compliance easier with web.

### Decision 2: Monolith First
**Choice**: Single .NET backend (not microservices)  
**Rationale**: YAGNI. MVP doesn't need distributed complexity. Eligibility, documents, admin are tightly coupled. Migrate to services later if needed.

### Decision 3: Database-Backed Sessions
**Choice**: PostgreSQL for anonymous sessions  
**Alternative**: Redis sessions  
**Rationale**: Simplicity. Postgres handles JSONB session data well. Redis optional for caching layer later.

### Decision 4: Server-Side PDF Generation
**Choice**: QuestPDF in .NET backend  
**Alternative**: Client-side with jsPDF  
**Rationale**: Server-side ensures consistent rendering, easier to template, no client dependency. Aligns with Constitution I (backend owns business logic).

### Decision 5: No Real-Time Collaboration
**Choice**: Simple request/response API  
**Alternative**: SignalR for real-time  
**Rationale**: MVP doesn't require real-time. WebSockets/polling sufficient for async document processing updates.

---

## Performance Targets

| Operation | Target | Strategy |
|-----------|--------|----------|
| Eligibility evaluation | ≤2s | Cache FPL tables, optimize JSONLogic, index DB queries |
| Document upload | ≤5s | Stream to blob storage, async processing |
| Packet generation | ≤10s | Pre-compiled PDF templates, cached state data |
| Wizard interaction | ≤500ms | Lightweight question graph, minimal backend calls |
| Concurrent users | 1,000 | Stateless API, horizontal scaling, DB connection pooling |

---

## Security Architecture

### Authentication Flow
```
Anonymous User → Cookie-based session (HttpOnly, Secure, SameSite)
Registered User → JWT (short-lived, 1h) + Refresh Token (7d)
Admin User → JWT + Role Claims (Analyst, Reviewer, Admin)
```

### Authorization
- **Middleware**: Role-based authorization on admin endpoints
- **Policies**: `[Authorize(Roles = "Admin")]`, `[Authorize(Policy = "CanApproveRules")]`

### Data Protection
- **At Rest**: Column-level encryption for sensitive fields (income, assets, disability)
- **In Transit**: HTTPS enforced (TLS 1.3)
- **Secrets**: Azure Key Vault or AWS Secrets Manager (not appsettings.json)

### Input Validation
- **Frontend**: Zod schemas, React Hook Form validation
- **Backend**: FluentValidation, ModelState checks
- **Defense in Depth**: Never trust client, validate server-side

### File Upload Security
- **Antivirus**: Integrate ClamAV or cloud service (Azure Defender, AWS GuardDuty)
- **File Type**: Validate magic bytes (not just extension)
- **Size Limits**: 15MB per clarifications, enforced in middleware

---

## Testing Strategy (Constitution II Compliance)

### Backend Tests

**Unit Tests** (xUnit + FluentAssertions)
- Domain entities (rules evaluation logic)
- Command/query handlers
- Validation logic
- Target: 80%+ coverage for domain/application layers

**Integration Tests** (WebApplicationFactory)
- API endpoints with in-memory DB or test containers
- Rules engine with sample test cases
- Document upload flow

**Contract Tests**
- API contract validation (OpenAPI spec)
- External service mocks (Azure Blob, OCR services)

### Frontend Tests

**Unit Tests** (Vitest + React Testing Library)
- Custom hooks (useWizard, useEligibility)
- Utility functions (form validation, date formatting)

**Component Tests**
- Wizard step rendering with mock data
- Form validation behavior
- Accessibility (aria labels, keyboard nav)

**E2E Tests** (Playwright - post-MVP)
- Full user journey: wizard → results → documents → packet
- Admin rule approval workflow

### Test Data
- Store test scenarios in `/tests/data/` (JSON files)
- One file per state (e.g., `illinois-test-cases.json`)
- Include edge cases from spec (income at threshold, variable income)

---

## Deployment Architecture (MVP)

### Option 1: Azure
```
Frontend: Azure Static Web Apps (React SPA)
Backend: Azure App Service (Linux, .NET 10)
Database: Azure Database for PostgreSQL (Flexible Server)
Blob Storage: Azure Blob Storage
Secrets: Azure Key Vault
Monitoring: Application Insights
```

### Option 2: AWS
```
Frontend: S3 + CloudFront
Backend: ECS Fargate (Docker containers)
Database: RDS PostgreSQL
Blob Storage: S3
Secrets: AWS Secrets Manager
Monitoring: CloudWatch + X-Ray
```

### Recommendation: Azure
**Rationale**: Tighter integration with .NET tooling, App Service simplifies deployment, Application Insights is excellent.

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-02-08 | Initial approved stack with .NET 10, React 19 |

---

**Approved By**: Development Team  
**Date**: 2026-02-08  
**Version**: 1.0
