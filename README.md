# Medicaid Application Assistant (MAA) API

**Version**: 1.0.0  
**Framework**: ASP.NET Core 9.0 / C# 13  
**Database**: PostgreSQL  
**Documentation**: Swagger/OpenAPI 3.0

## Overview

The Medicaid Application Assistant (MAA) API is a web service that handles eligibility determination, application sessions, and rules-based evaluation for Medicaid/CHIP applications. It provides a RESTful API for frontend applications to manage user sessions, process eligibility rules, and store application data.

## Getting Started

### Prerequisites

- .NET 9 SDK
- PostgreSQL 13+ database
- IDE (Visual Studio 2022, Visual Studio Code, or Rider)

### Running Locally

1. **Clone the repository**

   ```bash
   git clone <repository-url>
   cd MedicaidApplicationAssistant
   ```

2. **Configure database connection**
   Update `src/MAA.API/appsettings.Development.json` with your PostgreSQL connection string:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=maa_dev;Username=your_user;Password=your_password"
     }
   }
   ```

3. **Apply database migrations**

   ```bash
   cd src/MAA.Infrastructure
   dotnet ef database update --startup-project ../MAA.API
   ```

4. **Run the API**

   ```bash
   cd ../MAA.API
   dotnet run
   ```

5. **Access the API**
   - **Swagger UI**: http://localhost:5000/swagger
   - **OpenAPI spec**: http://localhost:5000/openapi/v1.json
   - **Health check**: http://localhost:5000/health

## API Documentation

### Swagger UI (Interactive Documentation)

The API documentation is **auto-generated from code comments** and available at:

```
http://localhost:5000/swagger (Development/Test only)
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
http://localhost:5000/openapi/v1.json
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
├── src/
│   ├── MAA.API/              # ASP.NET Core Web API (controllers, Program.cs)
│   ├── MAA.Application/      # Application layer (handlers, DTOs, validators)
│   ├── MAA.Domain/           # Domain models (entities, value objects)
│   ├── MAA.Infrastructure/   # Data access (EF Core, repositories)
│   └── MAA.Tests/            # Unit and integration tests (xUnit)
├── specs/                    # Feature specifications and planning docs
├── docs/                     # High-level documentation (PRD, architecture)
└── README.md                 # This file
```

## Testing

Run all tests:

```bash
dotnet test
```

Run specific test categories:

```bash
# Unit tests
dotnet test --filter Category=Unit

# Integration tests
dotnet test --filter Category=Integration

# Swagger/schema tests
dotnet test --filter FullyQualifiedName~Schemas
```

## Development Guidelines

- **API Documentation**: All controller methods and DTO properties MUST have XML comments (`/// <summary>`, `/// <remarks>`)
- **TDD Approach**: Write tests before implementation (red-green-refactor cycle)
- **Code Quality**: Follow SOLID principles, keep classes under 300 lines
- **Test Coverage**: 80%+ for Domain/Application layers, 60%+ for API layer

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed contribution guidelines.

## Configuration

The API uses environment-based configuration:

- **Development**: Full Swagger UI enabled, detailed error messages
- **Test**: Swagger enabled for integration tests, test database
- **Production**: Swagger disabled, error details suppressed, production database

Key configuration sections in `appsettings.json`:

- `ConnectionStrings`: Database connection
- `Jwt`: Authentication settings (secret, expiration, issuer)
- `Swagger`: OpenAPI documentation settings (title, version, enabled)
- `Logging`: Log levels per environment

## Troubleshooting

### Swagger UI not loading

- Verify environment is Development or Test (Production disables Swagger)
- Check `appsettings.{Environment}.json` has `"Swagger": { "Enabled": true }`
- Ensure XML documentation is generated: check `MAA.API.csproj` for `<GenerateDocumentationFile>true</GenerateDocumentationFile>`

### Endpoints missing from Swagger

- Rebuild project: `dotnet build`
- Verify controller has `[ApiController]` and `[Route]` attributes
- Check controller methods are public and have HTTP verb attributes (`[HttpGet]`, `[HttpPost]`, etc.)

### Schema validation errors

- Run validation script: `.\.specify\scripts\powershell\swagger-validation.ps1`
- Ensure all DTO properties have XML documentation
- Verify `[ProducesResponseType]` attributes are present on controller methods

See [docs/SWAGGER-MAINTENANCE.md](docs/SWAGGER-MAINTENANCE.md) for more troubleshooting tips (available after Phase 8 completion).

## License

[License information TBD]

## Contact

[Contact information TBD]

---

**Auto-Generated Documentation**: Remember, API documentation is automatically generated from your code. Keep XML comments up to date, and Swagger will stay in sync!
