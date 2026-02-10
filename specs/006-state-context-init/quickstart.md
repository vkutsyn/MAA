# Quickstart: State Context Initialization Step

**Feature**: 006-state-context-init  
**Date**: February 10, 2026  
**Audience**: Developers implementing this feature

## Overview

This quickstart guide provides step-by-step instructions for implementing the State Context Initialization Step feature. Follow the implementation order to ensure dependencies are met and tests pass incrementally.

**Expected Time**: 12-16 hours for full implementation (backend + frontend + tests)

---

## Prerequisites

Before starting implementation, ensure you have:

- ✅ `.NET 10 SDK` installed (backend)
- ✅ `Node.js 22 LTS` installed (frontend)
- ✅ `PostgreSQL 16+` running locally or via Docker
- ✅ MAA repository cloned and up to date with `main` branch
- ✅ Feature branch `006-state-context-init` checked out
- ✅ Reviewed [spec.md](./spec.md), [research.md](./research.md), and [data-model.md](./data-model.md)

---

## Implementation Order

Follow this sequence to build incrementally with passing tests at each step:

### Phase 1: Domain Layer (Backend) - 2-3 hours

**Goal**: Implement pure domain logic (no I/O) with full unit test coverage

#### Step 1.1: Create Domain Entities

**Files to create**:

- `src/MAA.Domain/StateContext/StateContext.cs`
- `src/MAA.Domain/StateContext/StateConfiguration.cs`
- `src/MAA.Domain/StateContext/ZipCodeValidator.cs`
- `src/MAA.Domain/StateContext/StateResolver.cs`

**Implementation notes**:

- `StateContext`: Plain entity with factory method `Create()` and `UpdateState()` method
- `ZipCodeValidator`: Static class with `IsValid(string)` and `Validate(string)` methods
- `StateResolver`: Class with `Resolve(string, Dictionary<string, string>)` method
- All logic must be testable without database or HTTP calls

**Example** (`StateContext.cs`):

```csharp
namespace MAA.Domain.StateContext;

public class StateContext
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string StateCode { get; private set; } = string.Empty;
    public string StateName { get; private set; } = string.Empty;
    public string ZipCode { get; private set; } = string.Empty;
    public bool IsManualOverride { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Factory method
    public static StateContext Create(Guid sessionId, string stateCode,
        string stateName, string zipCode, bool isManualOverride)
    {
        // Validation here
        return new StateContext
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            StateCode = stateCode,
            StateName = stateName,
            ZipCode = zipCode,
            IsManualOverride = isManualOverride,
            EffectiveDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Update method
    public void UpdateState(string stateCode, string stateName, bool isManualOverride)
    {
        StateCode = stateCode;
        StateName = stateName;
        IsManualOverride = isManualOverride;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

#### Step 1.2: Write Unit Tests for Domain Logic

**Files to create**:

- `src/MAA.Tests/Unit/StateContext/StateContextTests.cs`
- `src/MAA.Tests/Unit/StateContext/ZipCodeValidatorTests.cs`
- `src/MAA.Tests/Unit/StateContext/StateResolverTests.cs`

**Test coverage targets**:

- `ZipCodeValidator`: 100% (simple validation logic)
- `StateResolver`: 100% (mapping logic)
- `StateContext`: 95%+ (factory, update, validation)

**Example test**:

```csharp
[Fact]
public void Create_WithValidInputs_ShouldReturnStateContext()
{
    // Arrange
    var sessionId = Guid.NewGuid();
    var stateCode = "CA";
    var stateName = "California";
    var zipCode = "90210";

    // Act
    var stateContext = StateContext.Create(sessionId, stateCode, stateName, zipCode, false);

    // Assert
    stateContext.Should().NotBeNull();
    stateContext.StateCode.Should().Be(stateCode);
    stateContext.ZipCode.Should().Be(zipCode);
    stateContext.IsManualOverride.Should().BeFalse();
}
```

**Run tests**:

```bash
cd src/MAA.Tests
dotnet test --filter "FullyQualifiedName~MAA.Tests.Unit.StateContext"
```

---

### Phase 2: Database Layer (Backend) - 2-3 hours

**Goal**: Set up database schema and configure Entity Framework Core mappings

#### Step 2.1: Create EF Core Configuration

**Files to create**:

- `src/MAA.Infrastructure/StateContext/StateContextConfiguration.cs`
- `src/MAA.Infrastructure/StateContext/StateConfigurationConfiguration.cs`

**Implementation notes**:

- Use Fluent API for entity configuration
- Define primary keys, foreign keys, indexes
- Configure JSONB column for `StateConfiguration.ConfigData`

**Example** (`StateContextConfiguration.cs`):

```csharp
namespace MAA.Infrastructure.StateContext;

public class StateContextConfiguration : IEntityTypeConfiguration<Domain.StateContext.StateContext>
{
    public void Configure(EntityTypeBuilder<Domain.StateContext.StateContext> builder)
    {
        builder.ToTable("StateContexts");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.StateCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(sc => sc.ZipCode)
            .IsRequired()
            .HasMaxLength(5);

        builder.HasOne(sc => sc.Session)
            .WithOne()
            .HasForeignKey<Domain.StateContext.StateContext>(sc => sc.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sc => sc.SessionId);
        builder.HasIndex(sc => sc.StateCode);
    }
}
```

#### Step 2.2: Create EF Core Migration

**Commands**:

```bash
cd src/MAA.Infrastructure
dotnet ef migrations add AddStateContext --startup-project ../MAA.API
dotnet ef database update --startup-project ../MAA.API
```

**Verify migration**:

- Check migrations folder for `YYYYMMDDHHMMSS_AddStateContext.cs`
- Verify SQL creates tables: `StateContexts`, `StateConfigurations`
- Run migration against local PostgreSQL database

#### Step 2.3: Seed State Configuration Data

**Files to create**:

- `src/MAA.Infrastructure/StateContext/StateConfigurationSeeder.cs`
- `src/MAA.Infrastructure/Data/state-configs.json` (sample data for 50 states)

**Implementation notes**:

- Create JSON file with state configurations for all 50 states + DC
- Seed data in `OnModelCreating()` or via migration script
- Mark all as `IsActive = true`, `Version = 1`

**Example seed data** (`state-configs.json`):

```json
[
  {
    "stateCode": "CA",
    "stateName": "California",
    "medicaidProgramName": "Medi-Cal",
    "configData": {
      "contactInfo": {
        "phone": "1-800-952-5253",
        "website": "https://www.dhcs.ca.gov/"
      },
      "eligibilityThresholds": { "fplPercentages": { "adults": 138 } }
    }
  }
  // ... 49 more states
]
```

#### Step 2.4: Load ZIP Code Mapping Data

**Files to create**:

- `src/MAA.Infrastructure/StateContext/ZipCodeMappingService.cs`
- `src/MAA.Infrastructure/Data/zip-to-state.csv` or API integration

**Implementation notes**:

- Download SimpleMaps ZIP code database (free tier: ~42,000 ZIPs)
- Parse CSV at application startup into `Dictionary<string, string>`
- Cache in `IMemoryCache` for fast lookups
- Fallback to "not found" if ZIP missing

**Example**:

```csharp
public class ZipCodeMappingService
{
    private readonly Dictionary<string, string> _mappings;

    public ZipCodeMappingService()
    {
        _mappings = LoadMappings(); // Parse CSV
    }

    public string? GetStateCode(string zipCode)
    {
        return _mappings.TryGetValue(zipCode, out var stateCode) ? stateCode : null;
    }
}
```

---

### Phase 3: Application Layer (Backend) - 3-4 hours

**Goal**: Implement command/query handlers with business logic and validation

#### Step 3.1: Create DTOs and Validators

**Files to create**:

- `src/MAA.Application/StateContext/DTOs/StateContextDto.cs`
- `src/MAA.Application/StateContext/DTOs/StateConfigurationDto.cs`
- `src/MAA.Application/StateContext/DTOs/InitializeStateContextRequest.cs`
- `src/MAA.Application/StateContext/Validators/InitializeStateContextRequestValidator.cs`

**Implementation notes**:

- Use `record` types for DTOs (immutable)
- Use FluentValidation for request validation
- Validate ZIP format, state code format, session existence

**Example validator**:

```csharp
public class InitializeStateContextRequestValidator : AbstractValidator<InitializeStateContextRequest>
{
    public InitializeStateContextRequestValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ZipCode).NotEmpty().Length(5).Matches(@"^\d{5}$");
        RuleFor(x => x.StateCodeOverride)
            .Length(2).Matches(@"^[A-Z]{2}$")
            .When(x => x.StateCodeOverride != null);
    }
}
```

#### Step 3.2: Implement Command Handlers

**Files to create**:

- `src/MAA.Application/StateContext/Commands/InitializeStateContextCommand.cs`
- `src/MAA.Application/StateContext/Commands/InitializeStateContextHandler.cs`
- `src/MAA.Application/StateContext/Commands/UpdateStateContextCommand.cs`
- `src/MAA.Application/StateContext/Commands/UpdateStateContextHandler.cs`

**Implementation notes**:

- Use MediatR for command handling
- Inject `IStateContextRepository`, `IStateConfigurationRepository`, `ZipCodeMappingService`
- Handle errors: ZIP not found, session not found, state config missing

**Example handler**:

```csharp
public class InitializeStateContextHandler : IRequestHandler<InitializeStateContextCommand, StateContextDto>
{
    private readonly IStateContextRepository _repository;
    private readonly ZipCodeMappingService _zipService;

    public InitializeStateContextHandler(IStateContextRepository repository, ZipCodeMappingService zipService)
    {
        _repository = repository;
        _zipService = zipService;
    }

    public async Task<StateContextDto> Handle(InitializeStateContextCommand request, CancellationToken cancellationToken)
    {
        // Resolve state from ZIP
        var stateCode = request.StateCodeOverride ?? _zipService.GetStateCode(request.ZipCode)
            ?? throw new ValidationException("ZIP code not found");

        // Create state context
        var stateContext = StateContext.Create(request.SessionId, stateCode, stateName, request.ZipCode,
            request.StateCodeOverride != null);

        // Save to repository
        await _repository.AddAsync(stateContext, cancellationToken);

        // Return DTO
        return MapToDto(stateContext);
    }
}
```

#### Step 3.3: Write Integration Tests for Handlers

**Files to create**:

- `src/MAA.Tests/Integration/StateContext/InitializeStateContextTests.cs`
- `src/MAA.Tests/Integration/StateContext/UpdateStateContextTests.cs`

**Test scenarios**:

- ✅ Valid ZIP code → StateContext created with correct state
- ✅ Manual override → StateContext created with override state
- ✅ Invalid ZIP format → ValidationException thrown
- ✅ ZIP not found → ValidationException thrown
- ✅ Session not found → NotFoundException thrown

**Run tests**:

```bash
dotnet test --filter "FullyQualifiedName~MAA.Tests.Integration.StateContext"
```

---

### Phase 4: API Layer (Backend) - 2 hours

**Goal**: Expose REST API endpoints with Swagger documentation

#### Step 4.1: Create API Controller

**Files to create**:

- `src/MAA.API/Controllers/StateContextController.cs`

**Implementation notes**:

- Use ASP.NET Core Web API controllers
- Inject `IMediator` for command/query dispatch
- Add Swagger annotations for documentation
- Handle exceptions via global exception middleware

**Example controller**:

```csharp
[ApiController]
[Route("api/state-context")]
public class StateContextController : ControllerBase
{
    private readonly IMediator _mediator;

    public StateContextController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(StateContextResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Initialize([FromBody] InitializeStateContextRequest request)
    {
        var command = new InitializeStateContextCommand(request.SessionId, request.ZipCode, request.StateCodeOverride);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { sessionId = result.SessionId }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(StateContextResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get([FromQuery] Guid sessionId)
    {
        var query = new GetStateContextQuery(sessionId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
```

#### Step 4.2: Test API Endpoints

**Files to create**:

- `src/MAA.Tests/Integration/StateContext/StateContextControllerTests.cs`

**Test scenarios** (using `WebApplicationFactory`):

- ✅ POST /api/state-context → 201 Created with StateContextDto
- ✅ GET /api/state-context?sessionId=xxx → 200 OK with StateContextDto
- ✅ POST with invalid ZIP → 400 Bad Request with error details
- ✅ PUT /api/state-context → 200 OK with updated StateContextDto

**Run tests**:

```bash
dotnet test --filter "FullyQualifiedName~MAA.Tests.Integration.StateContext.StateContextControllerTests"
```

**Manual test with Swagger**:

1. Run `dotnet run --project src/MAA.API`
2. Navigate to `http://localhost:5000/swagger`
3. Test POST /api/state-context with sample payload
4. Verify response matches OpenAPI spec

---

### Phase 5: Frontend Implementation - 4-5 hours

**Goal**: Build React UI for ZIP code entry, state detection, and override

#### Step 5.1: Create API Client

**Files to create**:

- `frontend/src/features/state-context/api/stateContextApi.ts`
- `frontend/src/features/state-context/types/stateContext.types.ts`

**Implementation notes**:

- Use `axios` for HTTP requests
- Define TypeScript interfaces matching backend DTOs
- Export API functions for React Query hooks

**Example** (`stateContextApi.ts`):

```typescript
import axios from "axios";
import type {
  InitializeStateContextRequest,
  StateContextResponse,
} from "../types/stateContext.types";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api";

export const stateContextApi = {
  initialize: async (
    request: InitializeStateContextRequest,
  ): Promise<StateContextResponse> => {
    const response = await axios.post(`${API_BASE_URL}/state-context`, request);
    return response.data;
  },

  get: async (sessionId: string): Promise<StateContextResponse> => {
    const response = await axios.get(`${API_BASE_URL}/state-context`, {
      params: { sessionId },
    });
    return response.data;
  },

  validateZip: async (
    zipCode: string,
  ): Promise<{ isValid: boolean; stateCode?: string }> => {
    const response = await axios.post(
      `${API_BASE_URL}/state-context/validate-zip`,
      { zipCode },
    );
    return response.data;
  },
};
```

#### Step 5.2: Create React Query Hooks

**Files to create**:

- `frontend/src/features/state-context/hooks/useStateContext.ts`
- `frontend/src/features/state-context/hooks/useZipValidation.ts`

**Implementation notes**:

- Use TanStack Query (`useMutation`, `useQuery`)
- Handle loading, error, and success states
- Cache state context data for session duration

**Example hook**:

```typescript
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { stateContextApi } from "../api/stateContextApi";
import type { InitializeStateContextRequest } from "../types/stateContext.types";

export const useInitializeStateContext = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: InitializeStateContextRequest) =>
      stateContextApi.initialize(request),
    onSuccess: (data) => {
      // Cache the state context
      queryClient.setQueryData(
        ["stateContext", data.stateContext.sessionId],
        data,
      );
    },
  });
};
```

#### Step 5.3: Build UI Components

**Files to create**:

- `frontend/src/features/state-context/components/ZipCodeForm.tsx`
- `frontend/src/features/state-context/components/StateConfirmation.tsx`
- `frontend/src/features/state-context/components/StateOverride.tsx`

**Implementation notes**:

- Use shadcn/ui components (Input, Button, Select)
- Use React Hook Form + Zod for validation
- Ensure WCAG 2.1 AA compliance (labels, aria-describedby, keyboard nav)

**Example** (`ZipCodeForm.tsx`):

```tsx
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { useInitializeStateContext } from "../hooks/useStateContext";

const zipCodeSchema = z.object({
  zipCode: z.string().regex(/^\d{5}$/, "ZIP code must be 5 digits"),
});

export const ZipCodeForm = ({ sessionId }: { sessionId: string }) => {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(zipCodeSchema),
  });

  const { mutate, isLoading } = useInitializeStateContext();

  const onSubmit = (data: { zipCode: string }) => {
    mutate({ sessionId, zipCode: data.zipCode });
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <label htmlFor="zipCode">Enter your ZIP code</label>
      <Input
        id="zipCode"
        {...register("zipCode")}
        maxLength={5}
        aria-describedby={errors.zipCode ? "zipCode-error" : undefined}
      />
      {errors.zipCode && (
        <span id="zipCode-error" role="alert" className="text-red-600">
          {errors.zipCode.message}
        </span>
      )}
      <Button type="submit" disabled={isLoading}>
        {isLoading ? "Loading..." : "Continue"}
      </Button>
    </form>
  );
};
```

#### Step 5.4: Create Route Component

**Files to create**:

- `frontend/src/routes/StateContextStep.tsx`

**Implementation notes**:

- Integrate all components into a single page
- Handle navigation to `/wizard/step-1` on success
- Show loading spinner during state initialization

**Example**:

```tsx
import { ZipCodeForm } from "@/features/state-context/components/ZipCodeForm";
import { useNavigate } from "react-router-dom";

export const StateContextStep = () => {
  const navigate = useNavigate();
  const sessionId = useSessionStore((state) => state.sessionId);

  const onSuccess = () => {
    navigate("/wizard/step-1");
  };

  return (
    <div className="container mx-auto max-w-md py-8">
      <h1 className="text-2xl font-bold mb-4">
        Where are you applying for Medicaid?
      </h1>
      <ZipCodeForm sessionId={sessionId} onSuccess={onSuccess} />
    </div>
  );
};
```

#### Step 5.5: Add Route to Router

**Files to modify**:

- `frontend/src/App.tsx` or router configuration file

**Add route**:

```tsx
<Route path="/state-context" element={<StateContextStep />} />
```

#### Step 5.6: Write Frontend Tests

**Files to create**:

- `frontend/src/__tests__/features/state-context/ZipCodeForm.test.tsx`
- `frontend/src/__tests__/features/state-context/useStateContext.test.ts`

**Test scenarios**:

- ✅ Render ZIP code form with label and input
- ✅ Show validation error for invalid ZIP format
- ✅ Call API on form submission with valid ZIP
- ✅ Navigate to next step on successful submission
- ✅ Show error message if API call fails

**Run tests**:

```bash
cd frontend
npm test src/__tests__/features/state-context
```

---

### Phase 6: End-to-End Testing - 1-2 hours

**Goal**: Validate complete user flow from ZIP entry to wizard navigation

#### Step 6.1: Write E2E Test

**Files to create**:

- `src/MAA.Tests/E2E/StateContextFlowTests.cs` (backend E2E)
- `frontend/cypress/e2e/state-context-flow.cy.ts` (if using Cypress)

**Test scenarios**:

- ✅ User enters valid ZIP → sees detected state → clicks Continue → navigates to wizard
- ✅ User enters invalid ZIP → sees error message → corrects ZIP → continues
- ✅ User overrides detected state → sees updated state → continues

**Example** (Playwright/Cypress):

```typescript
describe("State Context Initialization Flow", () => {
  it("should initialize state context with valid ZIP code", () => {
    cy.visit("/state-context");
    cy.get('input[id="zipCode"]').type("90210");
    cy.get('button[type="submit"]').click();
    cy.url().should("include", "/wizard/step-1");
  });

  it("should show error for invalid ZIP code", () => {
    cy.visit("/state-context");
    cy.get('input[id="zipCode"]').type("1234");
    cy.get('button[type="submit"]').click();
    cy.contains("ZIP code must be 5 digits").should("be.visible");
  });
});
```

---

## Verification Checklist

Before marking the feature complete, verify:

### Backend

- [ ] All unit tests pass (`dotnet test --filter "Unit.StateContext"`)
- [ ] All integration tests pass (`dotnet test --filter "Integration.StateContext"`)
- [ ] API endpoints documented in Swagger (`/swagger` endpoint)
- [ ] Database migrations applied without errors
- [ ] State configuration data seeded (50 states + DC)
- [ ] ZIP code mapping data loaded (in-memory cache)
- [ ] Performance SLO met: <1000ms (p95) for state initialization

### Frontend

- [ ] All component tests pass (`npm test`)
- [ ] ZIP code form renders with proper labels and ARIA attributes
- [ ] Validation errors display inline and announce to screen readers
- [ ] State override UI is accessible via keyboard
- [ ] Navigation to wizard works on successful submission
- [ ] Error handling for network failures displays toast notifications
- [ ] Responsive design works on mobile (375px), tablet (768px), desktop (1920px)

### E2E

- [ ] Complete user flow test passes (ZIP entry → state detection → wizard)
- [ ] Invalid ZIP handling test passes
- [ ] State override test passes

### Constitution Compliance

- [ ] All domain logic testable in isolation (no I/O in domain entities)
- [ ] 80%+ code coverage on Domain/Application layers
- [ ] WCAG 2.1 AA compliance verified with axe DevTools
- [ ] Performance targets met (<1000ms p95)

---

## Troubleshooting

### Common Issues

**Issue**: `dotnet ef migrations add` fails with "No DbContext found"
**Solution**: Ensure `MAA.API` is set as startup project: `--startup-project ../MAA.API`

**Issue**: ZIP code mapping returns null for valid ZIP
**Solution**: Verify CSV file is loaded correctly; check file path and parsing logic

**Issue**: Frontend shows CORS error when calling API
**Solution**: Add CORS policy in `MAA.API/Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

**Issue**: React Query hooks show stale data
**Solution**: Invalidate cache after mutations:

```typescript
queryClient.invalidateQueries(["stateContext", sessionId]);
```

---

## Next Steps

After completing this feature:

1. **Demo to team**: Show ZIP entry, state detection, manual override flows
2. **Merge to main**: Create PR, ensure CI/CD passes, request code review
3. **Monitor performance**: Check Application Insights for p95 latency (<1000ms target)
4. **Gather feedback**: User testing on state override UX
5. **Plan next feature**: Eligibility Wizard Step 1 (depends on StateContext)

---

## Resources

- [Feature Spec](./spec.md)
- [Research Decisions](./research.md)
- [Data Model](./data-model.md)
- [API Contract](./contracts/state-context-api.yaml)
- [MAA Constitution](/.specify/memory/constitution.md)
- [Tech Stack Documentation](/docs/tech-stack.md)
