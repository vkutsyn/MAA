# Medicaid Application Assistant (MAA)

**Version**: 0.5.0 (In Development)  
**Backend**: ASP.NET Core 9.0 / C# 13  
**Frontend**: React 19 / TypeScript / Vite  
**Database**: PostgreSQL 15+  
**Documentation**: Swagger/OpenAPI 3.0

## Overview

The Medicaid Application Assistant (MAA) is a full-stack web application that helps individuals determine their Medicaid eligibility through an interactive wizard interface. The system evaluates eligibility based on state-specific rules, provides confidence-based results, and guides users through the application process.

### Current Status

**Implemented Features** (11 features completed):

- ✅ **Authentication & Sessions** - JWT-based auth, anonymous sessions, role-based access
- ✅ **State Context Initialization** - ZIP code-based state detection with manual override
- ✅ **Frontend Authentication Flow** - Login/registration pages with protected routes
- ✅ **Wizard Session API** - Session management, answer persistence, step tracking
- ✅ **Question Definitions API** - Dynamic question schema with conditional logic
- ✅ **Dynamic Question UI** - Interactive wizard with multiple input types
- ✅ **Eligibility Evaluation Engine** - Rules-based evaluation with confidence scoring
- ✅ **Eligibility Result UI** - Status display, program matches, explanations
- ✅ **API Documentation** - Swagger UI with comprehensive endpoint documentation
- ✅ **Rules Engine** - Configurable eligibility rules with state-specific logic
- ✅ **UI Implementation** - Responsive React interface with shadcn/ui components

### Architecture

**Full-Stack Application**:

- **Backend API**: RESTful .NET Core API following Clean Architecture principles
- **Frontend SPA**: React 19 single-page application with TypeScript
- **Database**: PostgreSQL with Entity Framework Core
- **State Management**: Zustand for frontend, session-based for backend
- **Validation**: FluentValidation (backend), Zod (frontend)
- **Testing**: xUnit (backend), Vitest & Testing Library (frontend)

## Getting Started

### Prerequisites

**Backend**:

- .NET 10 SDK
- PostgreSQL 15+ database
- IDE (Visual Studio 2022, VS Code with C# extension, or Rider)

**Frontend**:

- Node.js 20+ and npm
- Modern browser (Chrome, Firefox, Edge, Safari)

### Quick Start

#### 1. Clone and Setup Database

```bash
git clone <repository-url>
cd MedicaidApplicationAssistant
```

Update the database connection in [src/MAA.API/appsettings.Development.json](src/MAA.API/appsettings.Development.json):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=maa_dev;Username=your_user;Password=your_password"
  }
}
```

#### 2. Start Backend API

```bash
# Apply database migrations
cd src/MAA.Infrastructure
dotnet ef database update --startup-project ../MAA.API

# Run the API
cd ../MAA.API
dotnet run
```

The API will start at: `http://localhost:5008`

- Swagger UI: http://localhost:5008/swagger
- Health check: http://localhost:5008/health

#### 3. Start Frontend Application

```bash
# Install dependencies
cd frontend
npm install

# Start development server
npm run dev
```

The frontend will start at: `http://localhost:5173`

## Key Features

### User Flow

1. **State Selection** - Auto-detect state from ZIP code or manually select
2. **Eligibility Wizard** - Answer dynamic questions based on state rules
3. **Evaluation** - Rules engine evaluates eligibility with confidence scoring
4. **Results** - View eligibility status, matched programs, and explanations

### Technical Highlights

**Backend**:

- Clean Architecture with CQRS pattern
- Entity Framework Core with PostgreSQL
- JWT authentication with refresh tokens
- Comprehensive Swagger/OpenAPI documentation
- FluentValidation for request validation
- Structured logging with Serilog

**Frontend**:

- React 19 with TypeScript for type safety
- TanStack Query for server state management
- Zustand for client-side state
- shadcn/ui component library with Tailwind CSS
- React Hook Form with Zod validation
- Responsive design (mobile-first approach)

**Development Practices**:

- Test-Driven Development (TDD)
- Feature-based specification using speckit
- Version control with feature branches
- Comprehensive test coverage (unit, integration, E2E)
- WCAG 2.1 AA accessibility compliance

## API Documentation

### Swagger UI (Interactive Documentation)

The API documentation is **auto-generated from code comments** and available at:

```
http://localhost:5008/swagger (Development/Test only)
```

**Key Features**:

- ✅ All endpoints documented with descriptions and parameters
- ✅ Request/response schemas with field constraints
- ✅ "Try it out" capability to test endpoints directly
- ✅ JWT authentication support (use Authorize button)
- ✅ Always in sync with code (no manual maintenance required)

**Important**: When you modify a controller or DTO, documentation updates automatically on rebuild.

See [specs/003-add-swagger/quickstart.md](specs/003-add-swagger/quickstart.md) for detailed usage instructions.

### OpenAPI Specification

The OpenAPI 3.0 schema is available at:

```
http://localhost:5008/openapi/v1.json
http://localhost:5008/openapi/v1.yaml
```

This can be imported into tools like Postman, code generators (OpenAPI Generator, NSwag), or used for contract testing.

## Authentication

The API uses **JWT Bearer tokens** for authentication. Protected endpoints require an `Authorization` header:

```
Authorization: Bearer <your-jwt-token>
```

**Getting a Token**:

1. POST to `/api/auth/login` with credentials
2. Copy the returned JWT token
3. In Swagger UI, click "Authorize" button and paste token (prefix with "Bearer " is automatic)
4. All subsequent "Try it out" requests will include the token

**Anonymous Sessions**:

- The application supports anonymous sessions for users who haven't registered
- Session data is persisted and linked to users upon registration

See [docs/AUTHENTICATION.md](docs/AUTHENTICATION.md) for detailed authentication flows.

### Swagger UI (Interactive Documentation)

The API documentation is **auto-generated from code comments** and available at:

```
http://localhost:5008/swagger (Development/Test only)
```

**Key Features**:

- ✅ All endpoints documented with descriptions and parameters
- ✅ Request/response schemas with field constraints
- ✅ "Try it out" capability to test endpoints directly
- ✅ JWT authentication support (use Authorize button)
- ✅ Always in sync with code (no manual maintenance required)

**Important**: When you modify a controller or DTO, documentation updates automatically on rebuild. No separate step to update OpenAPI.json is needed.

See [specs/003-add-swagger/quickstart.md](specs/003-add-swagger/quickstart.md) for detailed usage instructions.

### OpenAPI Specification

The OpenAPI 3.0 schema is available at:

```
http://localhost:5008/openapi/v1.json
http://localhost:5008/openapi/v1.yaml
```

This can be imported into tools like Postman, code generators (OpenAPI Generator, NSwag), or used for contract testing.

**Generated Schema Location** (after build):

```
src/MAA.API/bin/{Configuration}/swagger.json
```

## Authentication

The API uses **JWT Bearer tokens** for authentication. Protected endpoints require an `Authorization` header:

```
Authorization: Bearer <your-jwt-token>
```

**Getting a Token**:

1. POST to `/api/auth/login` with credentials
2. Copy the returned JWT token
3. In Swagger UI, click "Authorize" button and paste token (prefix with "Bearer " is automatic)
4. All subsequent "Try it out" requests will include the token

See [docs/AUTHENTICATION.md](docs/AUTHENTICATION.md) for detailed authentication flows (if file exists after Phase 6 completion).

## Project Structure

```
MedicaidApplicationAssistant/
├── frontend/                    # React 19 frontend application
│   ├── src/
│   │   ├── components/          # Reusable UI components (shadcn/ui)
│   │   ├── features/            # Feature-specific components
│   │   ├── routes/              # React Router pages
│   │   ├── services/            # API client, auth service
│   │   ├── hooks/               # Custom React hooks (TanStack Query)
│   │   └── lib/                 # Utilities, types, validation schemas
│   ├── tests/                   # Vitest unit and E2E tests
│   └── package.json
├── src/                         # .NET Backend
│   ├── MAA.API/                 # ASP.NET Core Web API (controllers, middleware)
│   ├── MAA.Application/         # Application layer (CQRS handlers, DTOs, validators)
│   ├── MAA.Domain/              # Domain models (entities, value objects, interfaces)
│   ├── MAA.Infrastructure/      # Data access (EF Core, repositories, migrations)
│   └── MAA.Tests/               # xUnit tests (unit, integration, contract)
├── specs/                       # Feature specifications (generated by speckit)
│   ├── 001-auth-sessions/
│   ├── 002-rules-engine/
│   ├── 003-add-swagger/
│   ├── 004-ui-implementation/
│   ├── 005-frontend-auth-flow/
│   ├── 006-state-context-init/
│   ├── 007-wizard-session-api/
│   ├── 008-question-definitions-api/
│   ├── 009-dynamic-question-ui/
│   ├── 010-eligibility-evaluation-engine/
│   └── 011-eligibility-result-ui/
├── docs/                        # Project documentation
│   ├── prd.md                   # Product requirements
│   ├── tech-stack.md            # Technology decisions
│   ├── FEATURE_CATALOG.md       # Feature index
│   ├── ROADMAP_QUICK_REFERENCE.md # Development roadmap
│   ├── IMPLEMENTATION_PLAN.md   # Detailed implementation plan
│   ├── AUTHENTICATION.md        # Auth flows and security
│   ├── SWAGGER-MAINTENANCE.md   # API documentation guide
│   └── API-VERSIONING.md        # API versioning strategy
├── .specify/                    # Speckit templates and scripts
├── CHANGELOG.md                 # Release history
├── CONTRIBUTING.md              # Contribution guidelines
└── README.md                    # This file
```

## Testing

### Backend Tests

Run all backend tests:

```bash
cd src
dotnet test
```

Run specific test categories:

```bash
# Unit tests only
dotnet test --filter Category=Unit

# Integration tests only
dotnet test --filter Category=Integration

# Swagger/schema validation tests
dotnet test --filter FullyQualifiedName~Schemas
```

View test coverage:

```bash
# Coverage reports are generated in coverage/report/
# Open coverage/report/index.html in browser
```

### Frontend Tests

Run frontend tests:

```bash
cd frontend

# Run all tests
npm test

# Run with coverage
npm run test -- --coverage

# Run specific test file
npm test -- wizard.test.tsx

# Run in watch mode (development)
npm run test -- --watch
```

### Test Coverage Targets

- **Backend**: 80%+ for Domain/Application layers, 60%+ for API layer
- **Frontend**: 70%+ overall coverage with focus on critical user flows

## Configuration

### Backend Configuration

The API uses environment-based configuration ([src/MAA.API/appsettings.json](src/MAA.API/appsettings.json)):

- **Development**: Full Swagger UI enabled, detailed error messages, console logging
- **Test**: Swagger enabled for integration tests, test database
- **Production**: Swagger disabled, error details suppressed, production database

Key configuration sections:

- `ConnectionStrings`: PostgreSQL database connection
- `Jwt`: Authentication settings (secret, expiration, issuer)
- `Swagger`: OpenAPI documentation settings (title, version, enabled)
- `Logging`: Log levels per environment

### Frontend Configuration

Frontend environment variables ([frontend/.env](frontend/.env)):

- `VITE_API_BASE_URL`: Backend API base URL (default: `http://localhost:5008`)
- `VITE_APP_NAME`: Application name for display
- Development server runs on port 5173 by default

## Development Guidelines

### Code Quality Standards

**Backend**:

- Follow SOLID principles and Clean Architecture
- All controller methods and DTO properties MUST have XML comments
- Keep classes under 300 lines
- Use FluentValidation for all input validation
- Follow async/await patterns consistently

**Frontend**:

- Use TypeScript strict mode
- Follow React best practices (hooks, functional components)
- Implement proper error boundaries
- Use TanStack Query for server state
- Validate all forms with Zod schemas

### Testing Requirements

- **TDD Approach**: Write tests before implementation (red-green-refactor cycle)
- **Coverage Requirements**:
  - Backend: 80%+ Domain/Application layers, 60%+ API layer
  - Frontend: 70%+ overall coverage
- **Test Types**: Unit, integration, and E2E tests for critical flows

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed contribution guidelines and development workflow.

## Troubleshooting

### Backend Issues

**Swagger UI not loading**:

- Verify environment is Development or Test (Production disables Swagger)
- Check `appsettings.{Environment}.json` has `"Swagger": { "Enabled": true }`
- Ensure XML documentation is generated: check [MAA.API.csproj](src/MAA.API/MAA.API.csproj) for `<GenerateDocumentationFile>true</GenerateDocumentationFile>`

**Endpoints missing from Swagger**:

- Rebuild project: `dotnet build`
- Verify controller has `[ApiController]` and `[Route]` attributes
- Check controller methods are public and have HTTP verb attributes (`[HttpGet]`, `[HttpPost]`, etc.)

**Database migration issues**:

- Ensure PostgreSQL is running and credentials are correct
- Run `dotnet ef migrations list` to see applied migrations
- Use `dotnet ef database update` to apply pending migrations

### Frontend Issues

**API connection failures**:

- Verify backend is running at `http://localhost:5008`
- Check CORS settings in [Program.cs](src/MAA.API/Program.cs)
- Ensure `VITE_API_BASE_URL` environment variable is set correctly

**Build errors**:

- Delete `node_modules` and run `npm install` again
- Clear Vite cache: `rm -rf node_modules/.vite`
- Check TypeScript errors: `npm run build`

**Tests failing**:

- Ensure test database is set up (for E2E tests)
- Check that mock data is properly configured
- Run tests in verbose mode: `npm test -- --reporter=verbose`

See [docs/SWAGGER-MAINTENANCE.md](docs/SWAGGER-MAINTENANCE.md) and [CONTRIBUTING.md](CONTRIBUTING.md) for more troubleshooting tips.

## Documentation

### Project Documentation

- **[Product Requirements](docs/prd.md)** - Product vision and user requirements
- **[Technical Stack](docs/tech-stack.md)** - Technology choices and justification
- **[Feature Catalog](docs/FEATURE_CATALOG.md)** - Complete feature index
- **[Implementation Plan](docs/IMPLEMENTATION_PLAN.md)** - Detailed development roadmap
- **[Roadmap Quick Reference](docs/ROADMAP_QUICK_REFERENCE.md)** - Quick start guides by role
- **[Authentication Guide](docs/AUTHENTICATION.md)** - Auth flows and security patterns
- **[API Versioning](docs/API-VERSIONING.md)** - API versioning strategy
- **[Swagger Maintenance](docs/SWAGGER-MAINTENANCE.md)** - API documentation guide

### Feature Specifications

Each implemented feature has detailed specifications in the `specs/` directory:

- Specification document (user stories, requirements)
- Implementation plan (architecture, design decisions)
- Task breakdown (detailed implementation steps)
- Quickstart guide (testing, verification)
- Data models and API contracts

Example: [specs/009-dynamic-question-ui/](specs/009-dynamic-question-ui/)

## Contributing

We welcome contributions! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for:

- Development setup and workflow
- Code standards and best practices
- Testing guidelines
- Pull request process
- Branch strategy

## Release History

See [CHANGELOG.md](CHANGELOG.md) for detailed release notes and version history.

## Roadmap

**Current Sprint** (February 2026):

- ✅ Core eligibility evaluation flow complete
- ✅ Dynamic question wizard implemented
- ✅ Results display with confidence scoring
- 🚧 Polish and refinement phase

**Next Phase**:

- Document upload and validation
- PDF application packet generation
- Admin portal for rule management
- Additional state coverage

See [docs/ROADMAP_QUICK_REFERENCE.md](docs/ROADMAP_QUICK_REFERENCE.md) for detailed roadmap.

## License

[License information TBD]

## Contact

[Contact information TBD]

---

**Note**: This project uses [speckit](https://speckit.io) for feature specification and planning. All feature documentation is auto-generated and maintained in the `specs/` directory.
