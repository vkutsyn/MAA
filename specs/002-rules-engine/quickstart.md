# Quick Start: Rules Engine & State Data (E2)

**Created**: 2026-02-09  
**Purpose**: Developer setup guide for implementing and testing the rules engine

---

## Prerequisites

**Tools**:
- .NET 10 SDK
- PostgreSQL 16+ (Docker or local)
- Visual Studio Code or Visual Studio 2024
- Postman or curl for API testing

**Projects**:
- MAA.API running on localhost:5000
- MAA.Tests project configured

---

## Local Setup

### Step 1: Prepare Database

**Option A: Docker PostgreSQL** (recommended)

```bash
docker run -d \
  --name maa-postgres \
  -e POSTGRES_USER=maa \
  -e POSTGRES_PASSWORD=devpass \
  -e POSTGRES_DB=maa_dev \
  -p 5432:5432 \
  postgres:16-alpine
```

**Option B: Local PostgreSQL**

```bash
# Ensure PostgreSQL 16+ running
psql -U postgres -c "CREATE DATABASE maa_dev;"
psql -U maa -d maa_dev -c "CREATE EXTENSION pgcrypto;"
```

### Step 2: Apply E2 Migrations

```bash
cd src/
dotnet ef database update --project MAA.Infrastructure/MAA.Infrastructure.csproj \
  -- --name "InitializeRulesEngine, SeedPilotStateRules, SeedFPLTables"
```

**Verify**:
```bash
dotnet ef migrations list
```

Should show all three migrations completed.

### Step 3: Seed Test Data

```bash
# Seed 5 pilot states with sample rules
dotnet run --project MAA.API -- seed-rules --states IL,CA,NY,TX,FL

# Verify
psql -U maa -d maa_dev -c "SELECT COUNT(*) FROM eligibility_rules;"
# Expected output: 30+ (6 rules per state × 5 states)
```

---

## Run Sample Evaluation

### Option A: Postman (Visual)

**Import OpenAPI**: [contracts/rules-api.openapi.yaml](./contracts/rules-api.openapi.yaml)

**Example Request**:
- **Endpoint**: `POST http://localhost:5000/api/rules/evaluate`
- **Body**:
```json
{
  "state_code": "IL",
  "household_size": 2,
  "monthly_income_cents": 210000,
  "age": 35,
  "has_disability": false,
  "is_pregnant": false,
  "receives_ssi": false,
  "is_citizen": true
}
```

**Expected Response**:
```json
{
  "evaluation_date": "2026-02-09T15:00:00Z",
  "status": "Likely Eligible",
  "matched_programs": [
    {
      "program_id": "uuid-1",
      "program_name": "MAGI Adult",
      "confidence_score": 95,
      "explanation": "Your income ($2,100/month) is below the MAGI limit of $2,435 for a household of 2 in Illinois."
    }
  ],
  "explanation": "Based on your answers, you are likely eligible for MAGI Adult Medicaid.",
  "rule_version_used": 1.0,
  "confidence_score": 95
}
```

### Option B: curl

```bash
curl -X POST http://localhost:5000/api/rules/evaluate \
  -H "Content-Type: application/json" \
  -d '{
    "state_code": "IL",
    "household_size": 2,
    "monthly_income_cents": 210000,
    "age": 35,
    "has_disability": false,
    "is_pregnant": false,
    "receives_ssi": false,
    "is_citizen": true
  }'
```

### Option C: Programmatic (C# Test)

```csharp
// In MAA.Tests
public class RulesQuickStartTests {
    [Fact]
    public async Task EvaluateEligibility_BasicCaseIL() {
        var input = new UserEligibilityInputDto {
            StateCode = "IL",
            HouseholdSize = 2,
            MonthlyIncomeCents = 210000, // $2,100
            Age = 35,
            HasDisability = false,
            IsPregnant = false,
            ReceivesSsi = false,
            IsCitizen = true
        };
        
        var result = await _eligibilityService.EvaluateAsync(input);
        
        Assert.NotNull(result);
        Assert.Equal("Likely Eligible", result.Status);
        Assert.Contains("MAGI Adult", result.MatchedPrograms[0].ProgramName);
    }
}
```

---

## Running Tests

### Unit Tests (No Database Required)

```bash
cd src/
dotnet test MAA.Tests/MAA.Tests.csproj -t "Rules" --filter="Category=Unit"
```

**Test Files**:
- `Unit/Rules/RuleEngineTests.cs` - Pure logic tests
- `Unit/Eligibility/EligibilityEvaluatorTests.cs` - Program matching
- `Unit/Eligibility/ExplanationGeneratorTests.cs` - Plain-language output

**Run time**: <2 seconds

### Integration Tests (With Database)

```bash
# Requires Docker running or PostgreSQL accessible
dotnet test MAA.Tests/MAA.Tests.csproj --filter="Category=Integration"
```

**Test Files**:
- `Integration/RulesApiIntegrationTests.cs` - End-to-end via HTTP
- `Integration/DatabaseFixture.cs` - Testcontainers setup

**Run time**: ~10-30 seconds (includes container startup first run)

### Contract Tests (API Validation)

```bash
dotnet test MAA.Tests/MAA.Tests.csproj --filter="Category=Contract"
```

**Test Files**:
- `Contract/RulesApiContractTests.cs` - Validates API matches OpenAPI spec

**Validates**:
- Request JSON matches schema
- Response structure matches EligibilityResultDto
- Status codes (200 OK, 400 Bad Request, 500 Error)

---

## Performance Validation

### Check Evaluation Latency

```bash
# Unit test with stopwatch
dotnet test MAA.Tests/MAA.Tests.csproj -t "RuleEngineTests_Performance"
```

**Acceptance Criteria**:
- Single evaluation: <100ms
- Batch 100 evaluations: <2 seconds total
- FPL lookup: <10ms

### Load Test (1000 Concurrent)

```bash
# Using k6 (https://k6.io)
k6 run tests/load/rules-concurrent.js \
  --vus 100 \
  --duration 30s \
  --rps 1000
```

**Expected**:
- p50: <200ms
- p95: <1000ms
- p99: <2000ms
- Error rate: 0%

---

## Troubleshooting

### Migration Fails

```bash
# Check migrations applied
dotnet ef migrations list --project MAA.Infrastructure

# If missing, reapply
dotnet ef database update --project MAA.Infrastructure
```

### Rules Not Found

```bash
# Verify seeding
psql -U maa -d maa_dev -c "SELECT COUNT(*) FROM medicaid_programs;"
psql -U maa -d maa_dev -c "SELECT COUNT(*) FROM eligibility_rules;"

# If empty, re-seed
dotnet run --project MAA.API -- seed-rules --states IL,CA,NY,TX,FL
```

### FPL Lookup Returns Null

```bash
# Check FPL tables
psql -U maa -d maa_dev -c "SELECT COUNT(*) FROM federal_poverty_levels WHERE year = 2026;"

# If empty, insert manually
psql -U maa -d maa_dev < scripts/seed-fpl-2026.sql
```

### Evaluation Returns "Unlikely Eligible" Unexpectedly

1. Check rule logic is valid JSON
2. Verify FPL calculation: monthly_income_cents / 100 <= (fpl_annual_cents / 100 / 12 * fpl_percentage / 100)
3. Test rule with known case: IL household of 1, income $750/month should be Likely Eligible (below 138% FPL)

---

## Development Workflow

### Adding New Rule

1. **Create Rule** in admin portal (Phase 3)
   - Program: Select existing (e.g., "IL MAGI Adult")
   - Rule Logic: Enter JSON condition
   - Effective Date: Set to today

2. **Test**:
```bash
# Unit test with new rule
cd src/MAA.Tests
dotnet test --filter="RuleEngineTests and NewRule"
```

3. **Verify** in integration test:
```bash
# Add sample to Integration/RulesApiIntegrationTests.cs
[Fact]
public async Task EvaluateEligibility_NewRule() { ... }

dotnet test MAA.Tests/MAA.Tests.csproj --filter="NewRule"
```

4. **Deploy**:
```bash
git add .
git commit -m "feat(rules): Add IL MAGI Adult 2026 update"
dotnet build
# Migrations applied automatically on app startup
```

### Updating FPL

1. **Download** 2026 FPL tables from HHS
2. **Insert** into database:
```bash
psql -U maa -d maa_dev < scripts/seed-fpl-2027.sql
```

3. **Test** with FPL-based scenarios:
```bash
# Unit test for FPL calculations
dotnet test MAA.Tests/MAA.Tests.csproj --filter="FPLCalculator"
```

---

## Next Steps

- **Phase 0**: Research tasks → `research.md`
- **Phase 1 Complete**: Data model → `data-model.md`, API contracts → `contracts/`
- **Phase 2**: Task breakdown → `/speckit.tasks` command

---

## Files in This Feature

```
specs/002-rules-engine/
├── spec.md                    # Feature specification (requirements)
├── plan.md                    # Implementation plan (timeline, scope)
├── research.md               # Phase 0 research tasks
├── data-model.md            # Phase 1 schema design (THIS FILE)
├── quickstart.md            # Phase 1 developer setup guide
├── contracts/
│   └── rules-api.openapi.yaml   # API specification (OpenAPI 3.0)
└── checklists/
    └── requirements.md      # Quality validation
```

---

## Support

For questions during implementation:
1. Reference `spec.md` for requirements
2. Check `data-model.md` for schema details
3. Run sample evaluation above to verify setup
4. Review test files for working examples

