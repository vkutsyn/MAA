# Phase 1 Developer Quickstart: E1 Authentication & Session Management

**Status**: Ready  
**Target Audience**: Backend engineers implementing tasks T01-T42  
**Last Updated**: 2026-02-08

---

## Table of Contents

1. [Local Development Setup](#local-development-setup) (5 min)
2. [Project Structure](#project-structure) (5 min)
3. [Running Tests](#running-tests) (10 min)
4. [API Examples](#api-examples) (15 min)
5. [Debugging Tips](#debugging-tips) (10 min)
6. [Troubleshooting](#troubleshooting) (5 min)

---

## Local Development Setup

### Prerequisites

- **Docker Desktop**: For PostgreSQL + pgcrypto
- **.NET 10 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Visual Studio 2024** or **VS Code** with C# Dev Kit
- **Node.js 20+** (frontend testing, not needed for this sprint)
- **PostgreSQL client** (psql or Azure Data Studio)

### Step 1: Clone Repository

```bash
cd d:\Programming\Langate\MedicaidApplicationAssistant
git clone <repo-url>  # Already done
cd MedicaidApplicationAssistant
```

### Step 2: Start PostgreSQL

```bash
# Copy docker-compose (to be created in T01)
# For now, use Azure PostgreSQL development instance

# Alternative: Use local Docker
docker run -d \
  --name maa-postgres \
  -e POSTGRES_DB=maa_dev \
  -e POSTGRES_USER=devuser \
  -e POSTGRES_PASSWORD=devpass123 \
  -p 5432:5432 \
  postgres:16-alpine
```

### Step 3: Enable pgcrypto Extension

```bash
# Connect to the database
psql -U devuser -d maa_dev -h localhost -p 5432

# Enable pgcrypto
maa_dev=# CREATE EXTENSION IF NOT EXISTS pgcrypto;
maa_dev=# \dx  # Verify extension installed
```

### Step 4: Restore Dependencies & Configure

```bash
cd src/MAA.API

# Restore NuGet packages
dotnet restore

# Set user secrets (for local development)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=maa_dev;Username=devuser;Password=devpass123"
dotnet user-secrets set "AzureKeyVault:VaultUri" "https://maa-dev.vault.azure.net/"
dotnet user-secrets set "AzureKeyVault:TenantId" "<your-tenant-id>"
dotnet user-secrets set "AzureKeyVault:ClientId" "<your-client-id>"
dotnet user-secrets set "AzureKeyVault:ClientSecret" "<your-client-secret>"

# Or use appsettings.Development.json (check .gitignore to ensure secrets not committed)
cat > appsettings.Development.json << 'EOF'
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=maa_dev;Username=devuser;Password=devpass123"
  },
  "AzureKeyVault": {
    "VaultUri": "https://maa-dev.vault.azure.net/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
EOF
```

### Step 5: Run Migrations

```bash
cd src/MAA.API

# Create database & run migrations (auto-applied on startup via MigrateAsync)
dotnet build
dotnet ef database update

# Verify tables created
psql -U devuser -d maa_dev -h localhost -c "\dt"
```

### Step 6: Start the API

```bash
cd src/MAA.API
dotnet run

# Expected output:
# info: Microsoft.Hosting.Lifetime[14]
#   Now listening on: http://localhost:5000
#   Now listening on: https://localhost:5001
```

**Verify API is running**:
```bash
curl http://localhost:5000/api/health/live
# Expected: { "status": "alive", "uptime": 123.456 }
```

---

## Project Structure

```
src/
├── MAA.API/                              # ASP.NET Core Web API
│   ├── Program.cs                        # Service configuration, middleware setup
│   ├── appsettings.json                  # Configuration (non-secrets)
│   ├── Controllers/
│   │   ├── SessionsController.cs         # POST /api/sessions (T30-T32)
│   │   ├── SessionsAnswersController.cs  # POST /api/sessions/{id}/answers (T33-T35)
│   │   └── HealthController.cs           # GET /api/health/ready|live
│   ├── Middleware/
│   │   └── SessionMiddleware.cs          # Session extraction & validation (T20)
│   ├── Services/
│   │   ├── SessionService.cs             # Session business logic (T20-T22)
│   │   ├── EncryptionService.cs          # PII encryption/decryption (T23-T24)
│   │   ├── AnswerValidationService.cs    # Field validation rules (T25-T27)
│   │   └── KeyVaultService.cs            # Azure Key Vault integration (T25)
│   └── Exceptions/
│       ├── SessionExpiredException.cs
│       ├── ValidationException.cs
│       └── EncryptionException.cs
│
├── MAA.Application/                      # Use cases, CQRS commands
│   ├── Sessions/
│   │   ├── Commands/
│   │   │   ├── CreateSessionCommand.cs
│   │   │   ├── UpdateSessionStateCommand.cs
│   │   │   └── SaveAnswerCommand.cs
│   │   └── Queries/
│   │       ├── GetSessionQuery.cs
│   │       └── GetAnswersQuery.cs
│   └── Validators/
│       └── CreateSessionCommandValidator.cs
│
├── MAA.Domain/                           # Domain models (entities, value objects)
│   ├── Sessions/
│   │   ├── Session.cs                   # Entity (T10-T11)
│   │   ├── SessionAnswer.cs
│   │   ├── SessionState.cs              # Enum
│   │   ├── EncryptionKey.cs
│   │   └── ISessionRepository.cs        # Interface
│   └── Exceptions/
│
├── MAA.Infrastructure/                   # ORM, external services
│   ├── Data/
│   │   ├── SessionContext.cs            # EF Core DbContext (T12)
│   │   ├── Migrations/
│   │   │   ├── 001_InitialCreate.cs     # Create tables (T12)
│   │   │   └── 002_AddEncryptionKeys.cs
│   │   └── SessionRepository.cs         # Repository implementation
│   ├── Encryption/
│   │   ├── EncryptionProvider.cs        # pgcrypto wrapper (T23-T24)
│   │   └── KeyVaultProvider.cs
│   └── Persistence/
│       └── ChangeTracker.cs             # Audit logging
│
└── MAA.Tests/
    ├── Unit/
    │   ├── SessionServiceTests.cs       # Unit tests (80% coverage)
    │   └── EncryptionServiceTests.cs
    ├── Integration/
    │   ├── DatabaseFixture.cs           # Test container setup
    │   ├── SessionPersistenceTests.cs   # Integration tests (15%)
    │   └── EncryptionEndToEndTests.cs
    └── Contract/
        └── SessionApiContractTests.cs   # OpenAPI validation (5%)
```

**Key Files to Know**:

| File | Purpose | Task |
|------|---------|------|
| `MAA.Domain/Sessions/Session.cs` | Core session entity | T10 |
| `MAA.Infrastructure/Data/SessionContext.cs` | EF Core DbContext | T12 |
| `Migrations/001_InitialCreate.cs` | Database schema | T12 |
| `MAA.API/Middleware/SessionMiddleware.cs` | Request pipeline | T20 |
| `MAA.Application/Sessions/Commands/SaveAnswerCommand.cs` | Answer persistence | T28 |

---

## Running Tests

### Unit Tests (Fast Feedback)

```bash
cd src/MAA.Tests

# Run all unit tests
dotnet test --filter "Category!=Integration" -v normal

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover

# Expected: 80%+ line coverage on Domain & Application layers
# Coverage report: coverage.opencover.xml
```

**Example Unit Test**:
```csharp
[TestFixture]
public class SessionServiceTests
{
    private readonly ISessionService _service = new SessionService(/* mocks */);
    
    [Test]
    public async Task CreateSession_Returns_ValidSession()
    {
        // Arrange & Act
        var session = await _service.CreateSessionAsync("anonymous");
        
        // Assert
        Assert.That(session.State, Is.EqualTo(SessionState.Pending));
        Assert.That(session.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
    }
}
```

### Integration Tests (Real Database)

```bash
cd src/MAA.Tests

# Run integration tests only
dotnet test --filter "Category=Integration" -v normal

# Test container will auto-start PostgreSQL (5-10 sec first run)
# Expected: ~30 seconds for full integration suite
```

**Troubleshooting Integration Tests**:
- If Docker not running: `docker start maa-postgres` or `docker-compose up -d`
- If port 5432 busy: `lsof -i :5432` and stop conflicting process
- Test data isolation: Each test gets fresh database (via DatabaseFixture)

### Contract Tests (OpenAPI Validation)

```bash
# Generate OpenAPI schema from API
dotnet run --project src/MAA.API -- --generate-openapi > openapi.generated.json

# Validate against contract
dotnet test --filter "Category=Contract" -v normal
```

---

## API Examples

### Example 1: Create Anonymous Session

**Request**:
```bash
curl -X POST http://localhost:5000/api/sessions \
  -H "Content-Type: application/json" \
  -d '{ "sessionType": "anonymous" }' \
  -v  # Show headers
```

**Response**:
```http
HTTP/1.1 201 Created
Content-Type: application/json
Set-Cookie: session_id=a1b2c3d4-e5f6....; HttpOnly; Secure; SameSite=Strict

{
  "id": "a1b2c3d4-e5f6-4789-a123-b456c789d012",
  "state": "pending",
  "userId": null,
  "sessionType": "anonymous",
  "expiresAt": "2026-02-08T10:30:00Z",
  "inactivityTimeoutAt": "2026-02-08T10:30:00Z",
  "isRevoked": false,
  "createdAt": "2026-02-08T10:00:00Z",
  "updatedAt": "2026-02-08T10:00:00Z"
}
```

**Save session ID** (used in subsequent requests):
```bash
SESSION_ID="a1b2c3d4-e5f6-4789-a123-b456c789d012"
```

### Example 2: Update Session State

**Request** (transition pending → in_progress):
```bash
curl -X PATCH http://localhost:5000/api/sessions/$SESSION_ID \
  -H "Content-Type: application/json" \
  -H "Cookie: session_id=$SESSION_ID" \
  -d '{ "state": "in_progress" }'
```

**Response**:
```json
{
  "id": "a1b2c3d4-e5f6-4789-a123-b456c789d012",
  "state": "in_progress",
  "inactivityTimeoutAt": "2026-02-08T10:30:00Z",  // Timeout reset
  ...
}
```

### Example 3: Save Applicant Answers (Single)

**Request** (save income):
```bash
curl -X POST http://localhost:5000/api/sessions/$SESSION_ID/answers \
  -H "Content-Type: application/json" \
  -H "Cookie: session_id=$SESSION_ID" \
  -d '{
    "fieldKey": "income_annual_2025",
    "fieldType": "currency",
    "answer": 45000,
    "isPii": true
  }'
```

**Response**:
```json
{
  "id": "f1a2b3c4-d5e6-4789-a123-b456c789d012",
  "sessionId": "a1b2c3d4-e5f6-4789-a123-b456c789d012",
  "fieldKey": "income_annual_2025",
  "fieldType": "currency",
  "answer": 45000,  // Decrypted for display
  "isPii": true,
  "validationErrors": [],
  "createdAt": "2026-02-08T10:05:00Z",
  "updatedAt": "2026-02-08T10:05:00Z"
}
```

### Example 4: Save Multiple Answers (Batch)

**Request** (save household info):
```bash
curl -X POST http://localhost:5000/api/sessions/$SESSION_ID/answers \
  -H "Content-Type: application/json" \
  -H "Cookie: session_id=$SESSION_ID" \
  -d '{
    "answers": [
      {
        "fieldKey": "household_size",
        "fieldType": "number",
        "answer": 3,
        "isPii": false
      },
      {
        "fieldKey": "state_selected",
        "fieldType": "text",
        "answer": "IL",
        "isPii": false
      }
    ]
  }'
```

**Response**: Array of SessionAnswerResponse

### Example 5: Retrieve Decrypted Answers

**Request**:
```bash
curl -X GET http://localhost:5000/api/sessions/$SESSION_ID/answers \
  -H "Cookie: session_id=$SESSION_ID"
```

**Response**:
```json
{
  "sessionId": "a1b2c3d4-e5f6-4789-a123-b456c789d012",
  "answers": [
    {
      "fieldKey": "income_annual_2025",
      "fieldType": "currency",
      "answer": 45000,  // Decrypted
      "isPii": true,
      "validationErrors": []
    },
    {
      "fieldKey": "household_size",
      "fieldType": "number",
      "answer": 3,
      "isPii": false,
      "validationErrors": []
    }
  ],
  "decryptedAt": "2026-02-08T10:10:00Z"
}
```

### Example 6: Error Handling

**Request** (invalid state transition):
```bash
curl -X PATCH http://localhost:5000/api/sessions/$SESSION_ID \
  -H "Content-Type: application/json" \
  -H "Cookie: session_id=$SESSION_ID" \
  -d '{ "state": "completed" }'  # Invalid: can't go in_progress → completed
```

**Response**:
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "error": "InvalidStateTransition",
  "message": "Cannot transition from 'in_progress' to 'completed'. Allowed transitions: submitted, abandoned",
  "traceId": "0HN4EV5Q63P6P:00000001"
}
```

---

## Debugging Tips

### 1. Enable Detailed Logging

```csharp
// In Program.cs during development
builder.Services.AddLogging(config =>
    config.AddConsole()
           .SetMinimumLevel(LogLevel.Debug)
);
```

**Expected output**:
```
[DEBUG] SessionMiddleware: Extracted session_id from cookie: a1b2c3d4-...
[DEBUG] EncryptionService: Encrypting 155 bytes with key_version=1
[DEBUG] SessionRepository: Persisting session answers (3 records)
[INFO] SessionService: Session transitioned to in_progress
```

### 2. Database Inspection

```bash
# Connect to local PostgreSQL
psql -U devuser -d maa_dev -h localhost -c "SELECT * FROM sessions LIMIT 5"

# Check encryption keys
psql -U devuser -d maa_dev -h localhost -c "SELECT * FROM encryption_keys"

# Inspect session answers (encrypted data)
psql -U devuser -d maa_dev -h localhost -c "SELECT id, field_key, is_pii, LENGTH(answer_encrypted) FROM session_answers WHERE session_id='a1b2c3d4-...'"
```

### 3. Visual Studio Debugging

**Breakpoints**:
1. Set breakpoint in `SessionMiddleware.InvokeAsync()`
2. Make request: `curl http://localhost:5000/api/sessions/$SESSION_ID`
3. Debugger pauses; inspect context.Items["SessionId"]

**Watch Expressions**:
- `session.ExpiresAt - DateTime.UtcNow` (remaining timeout)
- `context.Request.Cookies["session_id"]` (session ID from cookie)

### 4. VS Code Debugging

**Launch Configuration** (`.vscode/launch.json`):
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/MAA.API/bin/Debug/net10.0/MAA.API.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/MAA.API",
      "console": "integratedTerminal"
    }
  ]
}
```

### 5. Monitor Azure Key Vault

```bash
# List recent access
az keyvault list-deleted --vault-name maa-dev

# Check key version
az keyvault key list --vault-name maa-dev --query "[].name"
```

---

## Troubleshooting

### Issue: "Connection refused" on localhost:5432

**Solution**:
```bash
# Check if PostgreSQL running
docker ps | grep postgres

# Start if not running
docker-compose up -d  # or docker start maa-postgres
```

### Issue: "pgcrypto extension not found"

**Solution**:
```bash
# Connect to database and enable
psql -U devuser -d maa_dev -h localhost
maa_dev=# CREATE EXTENSION IF NOT EXISTS pgcrypto;
maa_dev=# \dx  # Verify installed
```

### Issue: Tests timeout after 30 seconds

**Cause**: Test container PostgreSQL startup slow  
**Solution**:
```bash
# Pre-warm container before running tests
docker run postgres:16-alpine --version

# Or increase timeout in test configuration
dotnet test --timeout 60000  # 60 seconds
```

### Issue: "403 Forbidden" from Azure Key Vault

**Cause**: Authentication credentials invalid or missing permissions  
**Solution**:
```bash
# Verify credentials in appsettings.Development.json
dotnet user-secrets list

# Re-authenticate with Azure
az login
az account show
```

### Issue: Concurrent Write Conflict (409 response)

**Cause**: Two requests updated same session simultaneously  
**Solution** (application code):
```csharp
try
{
    await context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    // Retry with fresh session state
    var refreshed = await context.Sessions.FindAsync(sessionId);
    refreshed.State = SessionState.InProgress;
    await context.SaveChangesAsync();
}
```

---

## Next Steps

1. ✅ Complete local setup (Steps 1-6 above)
2. ✅ Run `dotnet test` to verify environment
3. 📋 Pick a task from [plan.md](./plan.md) (T01-T42)
4. 📋 Implement following Clean Architecture layers (Domain → Application → Infrastructure → API)
5. 📋 Write tests first (TDD per Constitution Principle II)
6. 📋 Create PR and request code review

**Questions?** 
- Check [research.md](./research.md) for design rationale
- Review [data-model.md](./data-model.md) for schema details
- See [contracts/sessions-api.openapi.yaml](./contracts/sessions-api.openapi.yaml) for API spec

---

## Resources

- **ASP.NET Core Docs**: https://docs.microsoft.com/en-us/aspnet/core/
- **EF Core**: https://docs.microsoft.com/en-us/ef/core/
- **PostgreSQL pgcrypto**: https://www.postgresql.org/docs/current/pgcrypto.html
- **Azure Key Vault**: https://docs.microsoft.com/en-us/azure/key-vault/
- **OWASP Session Management**: https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html
- **MAA Constitution**: [.specify/memory/constitution.md](.specify/memory/constitution.md)
