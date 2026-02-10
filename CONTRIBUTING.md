# Contributing to Medicaid Application Assistant (MAA) API

Thank you for considering contributing to the MAA API! This document provides guidelines and best practices for contributors.

## Table of Contents

1. [Development Setup](#development-setup)
2. [Development Workflow](#development-workflow)
3. [API Documentation (Auto-Generated)](#api-documentation-auto-generated)
4. [Code Standards](#code-standards)
5. [Testing Guidelines](#testing-guidelines)
6. [Pull Request Process](#pull-request-process)
7. [Troubleshooting](#troubleshooting)

## Development Setup

### Prerequisites

- **.NET 9 SDK** (latest version)
- **PostgreSQL 13+** database
- **Git** for version control
- **IDE**: Visual Studio 2022, VS Code with C# extension, or JetBrains Rider

### Initial Setup

1. Fork and clone the repository
2. Configure database connection in `src/MAA.API/appsettings.Development.json`
3. Run migrations: `dotnet ef database update --startup-project src/MAA.API`
4. Build solution: `dotnet build`
5. Run tests: `dotnet test`
6. Start API: `dotnet run --project src/MAA.API`
7. Verify Swagger UI: Navigate to http://localhost:5008/swagger

## Development Workflow

### Branch Strategy

- **main**: Production-ready code (protected)
- **Feature branches**: `###-feature-name` (e.g., `003-add-swagger`)
- **Hotfix branches**: `hotfix-description`

### Making Changes

1. **Create feature branch** from `main`:

   ```bash
   git checkout -b 004-my-feature
   ```

2. **Write tests first** (TDD approach):
   - Unit tests for domain logic
   - Integration tests for API endpoints
   - Contract tests for request/response schemas

3. **Implement feature**:
   - Follow clean architecture principles
   - Keep classes under 300 lines
   - Use dependency injection

4. **Update documentation** (if needed):
   - XML comments on controllers/DTOs (required for Swagger)
   - Update README.md if adding new setup steps
   - Update specs/ folder if changing requirements

5. **Run full test suite**:

   ```bash
   dotnet build
   dotnet test
   ```

6. **Commit changes** with descriptive messages:

   ```bash
   git add .
   git commit -m "Add eligibility calculation feature

   - Implement income-based eligibility rules
   - Add unit tests for all income scenarios
   - Document EligibilityCalculator methods
   - Update Swagger documentation"
   ```

## API Documentation (Auto-Generated)

### Critical: Documentation Auto-Syncs with Code

**The API documentation is auto-generated from code comments.** When you modify a controller or DTO, documentation updates automatically on rebuild. **No manual OpenAPI.json maintenance is needed.**

### How to Add Documentation to Controllers

When creating or modifying an API endpoint:

**1. Add XML comments to controller methods:**

```csharp
/// <summary>
/// Retrieve a session by ID.
/// </summary>
/// <param name="sessionId">The unique session identifier (UUID format).</param>
/// <returns>The session object with all metadata and answers.</returns>
/// <response code="200">Session found and returned successfully.</response>
/// <response code="400">Invalid session ID format (must be UUID).</response>
/// <response code="401">Unauthorized - JWT token missing or invalid.</response>
/// <response code="404">Session not found.</response>
/// <response code="500">Server error occurred.</response>
[HttpGet("{sessionId}")]
[ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> GetSession(Guid sessionId)
{
    // implementation
}
```

**2. Add XML comments to DTO properties:**

```csharp
/// <summary>
/// Unique session identifier.
/// </summary>
/// <remarks>
/// Format: UUID v4
/// Constraints: Non-empty, immutable
/// Example: "550e8400-e29b-41d4-a716-446655440000"
/// </remarks>
public Guid SessionId { get; set; }
```

**3. Run build to regenerate documentation:**

```bash
dotnet build
```

The Swagger UI at http://localhost:5008/swagger will immediately reflect your changes. **No separate documentation update step is required.**
Resolve XML documentation warnings (CS1570) before opening a PR.

### Where Swagger Documentation is Generated

After build, the OpenAPI schema is available at:

- **Runtime**: http://localhost:5008/openapi/v1.json
- **Build output**: `src/MAA.API/bin/{Configuration}/swagger.json`

### XML Documentation Requirements

All controllers and DTOs **MUST** have:

- `<summary>` tags (brief description)
- `<param>` tags for all parameters
- `<returns>` tag for return values
- `<remarks>` tags for constraints, validation rules, examples
- `<response>` tags for each possible status code

**Why this matters**: Swagger extracts this documentation to generate the interactive API docs. Missing XML comments = missing Swagger documentation.

## Code Standards

### Architecture Principles

Follow the **MAA Constitution** (see `.specify/memory/constitution.md`):

1. **Clean Architecture**: Domain logic isolated from I/O
2. **Test-First Development**: Write tests before implementation
3. **UX Consistency**: Maintain consistent error handling and validation
4. **Performance**: Response times ≤2s for eligibility, ≤500ms for interactions

### Code Quality

- **Single Responsibility**: One class = one responsibility
- **Dependency Injection**: No service locator pattern
- **Explicit Contracts**: All DTOs explicitly defined
- **Immutability**: Prefer readonly/init properties where possible
- **Null Safety**: Enable nullable reference types, handle nulls explicitly

### Naming Conventions

- **Controllers**: `{EntityName}Controller.cs` (e.g., `SessionsController.cs`)
- **DTOs**: `{EntityName}Dto.cs` (e.g., `SessionDto.cs`)
- **Handlers**: `{Action}{Entity}Handler.cs` (e.g., `CreateSessionHandler.cs`)
- **Tests**: `{ClassName}Tests.cs` (e.g., `SessionServiceTests.cs`)

### File Organization

```
src/MAA.API/Controllers/           # HTTP endpoints (thin, delegate to handlers)
src/MAA.Application/               # Business logic (handlers, commands, queries)
src/MAA.Application/DTOs/          # Data transfer objects
src/MAA.Domain/                    # Domain models (entities, value objects)
src/MAA.Infrastructure/            # Data access (EF Core, repositories)
src/MAA.Tests/Unit/                # Unit tests (no I/O)
src/MAA.Tests/Integration/         # Integration tests (with database)
src/MAA.Tests/Contract/            # API contract tests
```

## Testing Guidelines

### Test Coverage Requirements

- **Domain/Application layers**: 80%+ coverage
- **API layer**: 60%+ coverage
- **Infrastructure layer**: Full integration test coverage

### Test Categories

**Unit Tests** (fast, no I/O):

```csharp
[Fact]
public void EligibilityCalculator_Should_Return_Eligible_When_Income_Below_Threshold()
{
    // Arrange
    var calculator = new EligibilityCalculator();
    var input = new UserEligibilityInputDto { Income = 20000, HouseholdSize = 2 };

    // Act
    var result = calculator.CalculateEligibility(input);

    // Assert
    result.IsEligible.Should().BeTrue();
}
```

**Integration Tests** (with database):

```csharp
[Fact]
public async Task PostSession_Should_Return_Created_With_SessionId()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new CreateSessionDto { UserId = Guid.NewGuid(), TimeoutMinutes = 30 };

    // Act
    var response = await client.PostAsJsonAsync("/api/sessions", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var session = await response.Content.ReadFromJsonAsync<SessionDto>();
    session.Should().NotBeNull();
    session!.SessionId.Should().NotBeEmpty();
}
```

**Contract Tests** (schema validation):

```csharp
[Fact]
public async Task GetSession_Response_Should_Match_SessionDto_Schema()
{
    // Verify response structure matches documented schema
}
```

### Running Tests

```bash
# All tests
dotnet test

# Specific category
dotnet test --filter Category=Unit
dotnet test --filter FullyQualifiedName~EligibilityCalculator

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Pull Request Process

### Before Submitting

**Checklist** (required):

- [ ] All tests pass: `dotnet test`
- [ ] Build succeeds with no new warnings: `dotnet build`
- [ ] Code follows architecture principles and naming conventions
- [ ] XML comments added to all public controllers/DTOs
- [ ] Swagger UI verified (if modifying endpoints): http://localhost:5000/swagger
- [ ] Integration tests added for new endpoints
- [ ] Unit tests added for new domain logic
- [ ] README.md updated if adding new features or setup steps
- [ ] Swagger schema validates: `.\.specify\scripts\powershell\swagger-validation.ps1` (if available)

### PR Title Format

```
[Feature-###] Brief description of change

Example: [Feature-004] Add eligibility calculation endpoint
```

### PR Description Template

```markdown
## Summary

Brief description of changes.

## Changes Made

- Added `EligibilityController` with `/api/eligibility/calculate` endpoint
- Implemented `EligibilityCalculator` domain service
- Added unit tests for income-based eligibility scenarios
- Updated Swagger documentation with XML comments

## Testing

- All unit tests pass (35 tests)
- Integration tests added for happy path and error cases (5 tests)
- Manually tested in Swagger UI: /api/eligibility/calculate endpoint

## Documentation

- [x] XML comments added to controller methods
- [x] DTO properties documented
- [x] Swagger UI verified working
- [x] README.md updated (if applicable)

## Related Issues

Closes #123
```

### Review Process

1. **Automated checks** (CI/CD):
   - Build succeeds
   - All tests pass
   - Code coverage meets thresholds

2. **Code review** (manual):
   - Follows architecture principles
   - Proper error handling
   - XML documentation complete

3. **Approval**: 1+ approvals required
4. **Merge**: Squash and merge to `main`

## Troubleshooting

### Common Issues

**Issue**: Swagger UI not showing my new endpoint

**Solution**:

1. Verify controller has `[ApiController]` and `[Route("api/[controller]")]` attributes
2. Verify method has HTTP verb attribute (`[HttpGet]`, `[HttpPost]`, etc.)
3. Rebuild project: `dotnet build`
4. Restart API: `dotnet run --project src/MAA.API`
5. Refresh Swagger UI: http://localhost:5000/swagger

---

**Issue**: XML documentation not appearing in Swagger

**Solution**:

1. Verify `MAA.API.csproj` has `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
2. Verify XML comments use triple-slash format: `///`
3. Rebuild project
4. Check XML file exists: `src/MAA.API/bin/Debug/net10.0/MAA.API.xml`

---

**Issue**: Tests failing locally but pass in CI

**Solution**:

1. Clean solution: `dotnet clean`
2. Restore packages: `dotnet restore`
3. Rebuild: `dotnet build`
4. Run tests: `dotnet test`
5. Check database state (integration tests may have stale data)

---

**Issue**: Database migrations failing

**Solution**:

1. Verify PostgreSQL is running
2. Check connection string in `appsettings.Development.json`
3. Drop and recreate database if needed
4. Re-run migrations: `dotnet ef database update --startup-project src/MAA.API`

## Questions?

If you have questions or need help:

1. Check [README.md](README.md) for general setup
2. Check [specs/003-add-swagger/quickstart.md](specs/003-add-swagger/quickstart.md) for Swagger usage
3. Review [docs/](docs/) folder for feature documentation
4. Open an issue on GitHub

---

**Remember**: API documentation auto-updates from code comments. Keep XML comments current, and Swagger stays in sync!
