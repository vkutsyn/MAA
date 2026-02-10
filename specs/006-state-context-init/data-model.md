# Data Model: State Context Initialization Step

**Feature**: 006-state-context-init  
**Date**: February 10, 2026  
**Phase**: 1 (Design)

## Overview

This document defines the data entities, relationships, and validation rules for state context initialization. All entities follow clean architecture principles: domain logic is testable in isolation, DTOs are explicitly defined for API contracts, and repositories abstract data access.

---

## Domain Entities

### 1. StateContext

**Purpose**: Represents the established Medicaid jurisdiction context for a user's application session.

**Attributes**:

| Attribute        | Type     | Nullable | Description                                             |
| ---------------- | -------- | -------- | ------------------------------------------------------- |
| Id               | Guid     | No       | Primary key (auto-generated UUID)                       |
| SessionId        | Guid     | No       | Foreign key to Session entity                           |
| StateCode        | string   | No       | 2-letter state abbreviation (e.g., "CA", "NY")          |
| StateName        | string   | No       | Full state name (e.g., "California", "New York")        |
| ZipCode          | string   | No       | User-entered 5-digit ZIP code                           |
| IsManualOverride | bool     | No       | True if user manually selected state (vs auto-detected) |
| EffectiveDate    | DateTime | No       | UTC timestamp when state context was established        |
| CreatedAt        | DateTime | No       | Auto-set on creation (UTC)                              |
| UpdatedAt        | DateTime | Yes      | Auto-set on update (UTC)                                |

**Relationships**:

- **Session** (1:1): One StateContext belongs to one Session
- **StateConfiguration** (1:1): StateContext references one StateConfiguration by StateCode

**Validation Rules**:

- StateCode: Required, must match `^[A-Z]{2}$` (2 uppercase letters)
- StateName: Required, max length 50 characters
- ZipCode: Required, must match `^\d{5}$` (5 digits)
- EffectiveDate: Must be ≤ current UTC time (cannot be in future)

**Business Rules**:

- StateContext can be updated (e.g., user changes state), but never deleted without deleting Session
- If IsManualOverride = true, system skips ZIP-to-state validation on updates
- StateCode must exist in StateConfigurations table (foreign key constraint)

**Database Mapping** (Entity Framework Core):

```csharp
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

    // Navigation properties
    public Session Session { get; private set; } = null!;
    public StateConfiguration StateConfiguration { get; private set; } = null!;

    // Factory method (testable, no I/O)
    public static StateContext Create(Guid sessionId, string stateCode, string stateName,
        string zipCode, bool isManualOverride)
    {
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

    // Update method (testable)
    public void UpdateState(string stateCode, string stateName, bool isManualOverride)
    {
        StateCode = stateCode;
        StateName = stateName;
        IsManualOverride = isManualOverride;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

---

### 2. StateConfiguration

**Purpose**: Represents state-specific Medicaid program configuration and eligibility metadata.

**Attributes**:

| Attribute           | Type     | Nullable | Description                                                    |
| ------------------- | -------- | -------- | -------------------------------------------------------------- |
| StateCode           | string   | No       | Primary key: 2-letter state abbreviation                       |
| StateName           | string   | No       | Full state name                                                |
| MedicaidProgramName | string   | No       | State's Medicaid program name (e.g., "Medi-Cal", "MassHealth") |
| ConfigData          | string   | No       | JSONB: State-specific config (thresholds, documents, etc.)     |
| EffectiveDate       | DateTime | No       | Date this config version became effective                      |
| Version             | int      | No       | Version number (increments on updates)                         |
| IsActive            | bool     | No       | True if this is the active config for the state                |
| CreatedAt           | DateTime | No       | Auto-set on creation (UTC)                                     |
| UpdatedAt           | DateTime | Yes      | Auto-set on update (UTC)                                       |

**Relationships**:

- **StateContext** (1:many): One StateConfiguration is referenced by many StateContexts

**Validation Rules**:

- StateCode: Required, must match `^[A-Z]{2}$`
- StateName: Required, max length 50 characters
- MedicaidProgramName: Required, max length 100 characters
- ConfigData: Required, must be valid JSON
- Version: Must be > 0

**Business Rules**:

- Only one StateConfiguration per StateCode can have IsActive = true
- ConfigData structure is defined by application schema (see below)
- Updates create new version records (immutable history for audit)

**ConfigData JSON Schema**:

```json
{
  "stateCode": "CA",
  "stateName": "California",
  "medicaidProgramName": "Medi-Cal",
  "contactInfo": {
    "phone": "1-800-XXX-XXXX",
    "website": "https://www.dhcs.ca.gov/",
    "applicationUrl": "https://benefitscal.com/"
  },
  "eligibilityThresholds": {
    "fplPercentages": {
      "adults": 138,
      "children": 266,
      "pregnant": 213
    },
    "assetLimits": {
      "individual": 2000,
      "couple": 3000
    }
  },
  "requiredDocuments": [
    "Proof of identity",
    "Proof of California residency",
    "Income verification (pay stubs, tax returns)"
  ],
  "additionalNotes": "Medi-Cal uses MAGI (Modified Adjusted Gross Income) for most eligibility categories."
}
```

**Database Mapping** (Entity Framework Core):

```csharp
public class StateConfiguration
{
    public string StateCode { get; private set; } = string.Empty;
    public string StateName { get; private set; } = string.Empty;
    public string MedicaidProgramName { get; private set; } = string.Empty;
    public string ConfigData { get; private set; } = "{}";  // Stored as JSONB
    public DateTime EffectiveDate { get; private set; }
    public int Version { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<StateContext> StateContexts { get; private set; } = new List<StateContext>();

    // Factory method
    public static StateConfiguration Create(string stateCode, string stateName,
        string programName, string configData)
    {
        return new StateConfiguration
        {
            StateCode = stateCode,
            StateName = stateName,
            MedicaidProgramName = programName,
            ConfigData = configData,
            EffectiveDate = DateTime.UtcNow,
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

---

### 3. ZipCodeMapping

**Purpose**: Static lookup table mapping ZIP codes to state codes (used by StateResolver).

**Attributes**:

| Attribute   | Type    | Nullable | Description                            |
| ----------- | ------- | -------- | -------------------------------------- |
| ZipCode     | string  | No       | Primary key: 5-digit ZIP code          |
| StateCode   | string  | No       | 2-letter state abbreviation            |
| PrimaryCity | string  | Yes      | Primary city name (for display)        |
| County      | string  | Yes      | County name (for future use)           |
| Latitude    | decimal | Yes      | Latitude (for geo features, optional)  |
| Longitude   | decimal | Yes      | Longitude (for geo features, optional) |

**Relationships**: None (static reference data)

**Validation Rules**:

- ZipCode: Required, must match `^\d{5}$`
- StateCode: Required, must match `^[A-Z]{2}$`

**Business Rules**:

- Data loaded at application startup into in-memory cache (`Dictionary<string, string>`)
- Updates via CSV import (quarterly), not via API
- Multi-state ZIP codes: Choose primary state based on population density

**Database Mapping**: Optional (can be CSV/JSON file embedded in project)

- If database: Table with index on ZipCode (primary key)
- If static file: Load into `IMemoryCache` at startup

**Data Source**: SimpleMaps U.S. ZIP Codes Database (free, ~42,000 entries)

---

## Value Objects

### ZipCodeValidator

**Purpose**: Pure validation logic for ZIP code format (no I/O, testable in isolation).

**Methods**:

- `IsValid(string zipCode) : bool` - Returns true if ZIP is 5 digits
- `Validate(string zipCode) : ValidationResult` - Returns detailed validation result

**Validation Logic**:

```csharp
public static class ZipCodeValidator
{
    public static bool IsValid(string zipCode)
    {
        return !string.IsNullOrWhiteSpace(zipCode) &&
               zipCode.Length == 5 &&
               zipCode.All(char.IsDigit);
    }

    public static ValidationResult Validate(string zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
            return ValidationResult.Failure("ZIP code is required");

        if (zipCode.Length != 5)
            return ValidationResult.Failure("ZIP code must be exactly 5 digits");

        if (!zipCode.All(char.IsDigit))
            return ValidationResult.Failure("ZIP code must contain only numbers");

        return ValidationResult.Success();
    }
}
```

---

### StateResolver

**Purpose**: Pure logic for resolving state from ZIP code (uses ZipCodeMapping lookup).

**Methods**:

- `Resolve(string zipCode, Dictionary<string, string> mappings) : StateResolutionResult`

**Resolution Logic**:

```csharp
public class StateResolver
{
    public StateResolutionResult Resolve(string zipCode, Dictionary<string, string> mappings)
    {
        if (!ZipCodeValidator.IsValid(zipCode))
            return StateResolutionResult.Invalid("Invalid ZIP code format");

        if (mappings.TryGetValue(zipCode, out var stateCode))
            return StateResolutionResult.Success(stateCode);

        return StateResolutionResult.NotFound("ZIP code not found");
    }
}

public class StateResolutionResult
{
    public bool IsSuccess { get; }
    public string? StateCode { get; }
    public string? ErrorMessage { get; }

    public static StateResolutionResult Success(string stateCode)
        => new(true, stateCode, null);

    public static StateResolutionResult Invalid(string error)
        => new(false, null, error);

    public static StateResolutionResult NotFound(string error)
        => new(false, null, error);
}
```

---

## DTOs (Data Transfer Objects)

### StateContextDto

**Purpose**: API contract for StateContext entity (request/response).

```typescript
// TypeScript (frontend)
export interface StateContextDto {
  id: string; // UUID
  sessionId: string; // UUID
  stateCode: string; // 2-letter code
  stateName: string; // Full name
  zipCode: string; // 5 digits
  isManualOverride: boolean;
  effectiveDate: string; // ISO 8601 UTC
  createdAt: string; // ISO 8601 UTC
  updatedAt?: string; // ISO 8601 UTC
}
```

```csharp
// C# (backend)
public record StateContextDto(
    Guid Id,
    Guid SessionId,
    string StateCode,
    string StateName,
    string ZipCode,
    bool IsManualOverride,
    DateTime EffectiveDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
```

---

### InitializeStateContextRequest

**Purpose**: Request payload for POST /api/state-context endpoint.

```typescript
// TypeScript (frontend)
export interface InitializeStateContextRequest {
  sessionId: string; // UUID
  zipCode: string; // 5 digits
  stateCodeOverride?: string; // Optional: 2-letter code for manual override
}
```

```csharp
// C# (backend)
public record InitializeStateContextRequest(
    Guid SessionId,
    string ZipCode,
    string? StateCodeOverride = null
);
```

**Validation** (FluentValidation):

```csharp
public class InitializeStateContextRequestValidator : AbstractValidator<InitializeStateContextRequest>
{
    public InitializeStateContextRequestValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ZipCode).NotEmpty().Length(5).Matches(@"^\d{5}$");
        RuleFor(x => x.StateCodeOverride).Length(2).Matches(@"^[A-Z]{2}$").When(x => x.StateCodeOverride != null);
    }
}
```

---

### StateConfigurationDto

**Purpose**: API contract for StateConfiguration entity (response only).

```typescript
// TypeScript (frontend)
export interface StateConfigurationDto {
  stateCode: string;
  stateName: string;
  medicaidProgramName: string;
  contactInfo: {
    phone: string;
    website: string;
    applicationUrl: string;
  };
  eligibilityThresholds: {
    fplPercentages: {
      adults: number;
      children: number;
      pregnant: number;
    };
    assetLimits: {
      individual: number;
      couple: number;
    };
  };
  requiredDocuments: string[];
  additionalNotes: string;
}
```

```csharp
// C# (backend)
public record StateConfigurationDto(
    string StateCode,
    string StateName,
    string MedicaidProgramName,
    ContactInfoDto ContactInfo,
    EligibilityThresholdsDto EligibilityThresholds,
    string[] RequiredDocuments,
    string AdditionalNotes
);

public record ContactInfoDto(string Phone, string Website, string ApplicationUrl);
public record EligibilityThresholdsDto(FplPercentagesDto FplPercentages, AssetLimitsDto AssetLimits);
public record FplPercentagesDto(int Adults, int Children, int Pregnant);
public record AssetLimitsDto(int Individual, int Couple);
```

---

## Database Schema (PostgreSQL)

### StateContexts Table

```sql
CREATE TABLE "StateContexts" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "SessionId" UUID NOT NULL,
    "StateCode" VARCHAR(2) NOT NULL,
    "StateName" VARCHAR(50) NOT NULL,
    "ZipCode" VARCHAR(5) NOT NULL,
    "IsManualOverride" BOOLEAN NOT NULL DEFAULT false,
    "EffectiveDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL,

    CONSTRAINT "FK_StateContexts_Sessions" FOREIGN KEY ("SessionId") REFERENCES "Sessions"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_StateContexts_StateConfigurations" FOREIGN KEY ("StateCode") REFERENCES "StateConfigurations"("StateCode")
);

CREATE INDEX "IX_StateContexts_SessionId" ON "StateContexts"("SessionId");
CREATE INDEX "IX_StateContexts_StateCode" ON "StateContexts"("StateCode");
```

---

### StateConfigurations Table

```sql
CREATE TABLE "StateConfigurations" (
    "StateCode" VARCHAR(2) PRIMARY KEY,
    "StateName" VARCHAR(50) NOT NULL,
    "MedicaidProgramName" VARCHAR(100) NOT NULL,
    "ConfigData" JSONB NOT NULL,
    "EffectiveDate" TIMESTAMP NOT NULL,
    "Version" INTEGER NOT NULL DEFAULT 1,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NULL
);

CREATE INDEX "IX_StateConfigurations_IsActive" ON "StateConfigurations"("IsActive");
```

---

### ZipCodeMappings Table (Optional)

```sql
CREATE TABLE "ZipCodeMappings" (
    "ZipCode" VARCHAR(5) PRIMARY KEY,
    "StateCode" VARCHAR(2) NOT NULL,
    "PrimaryCity" VARCHAR(100) NULL,
    "County" VARCHAR(100) NULL,
    "Latitude" DECIMAL(9, 6) NULL,
    "Longitude" DECIMAL(9, 6) NULL
);

CREATE INDEX "IX_ZipCodeMappings_StateCode" ON "ZipCodeMappings"("StateCode");
```

---

## Entity Relationships Diagram

```
┌─────────────────┐         ┌─────────────────────┐
│    Sessions     │         │   StateContexts     │
├─────────────────┤         ├─────────────────────┤
│ Id (PK)         │◄────────┤ Id (PK)             │
│ UserId          │   1:1   │ SessionId (FK)      │
│ SessionData     │         │ StateCode (FK)      │
│ CreatedAt       │         │ StateName           │
│ ExpiresAt       │         │ ZipCode             │
└─────────────────┘         │ IsManualOverride    │
                            │ EffectiveDate       │
                            │ CreatedAt           │
                            └─────────────────────┘
                                      │
                                      │ N:1
                                      ▼
                            ┌───────────────────────┐
                            │ StateConfigurations   │
                            ├───────────────────────┤
                            │ StateCode (PK)        │
                            │ StateName             │
                            │ MedicaidProgramName   │
                            │ ConfigData (JSONB)    │
                            │ EffectiveDate         │
                            │ Version               │
                            │ IsActive              │
                            └───────────────────────┘
```

---

## State Transitions

**StateContext Lifecycle**:

1. **Created**: User submits ZIP code → StateContext created with stateCode, zipCode, IsManualOverride=false
2. **Updated** (optional): User overrides state → StateContext.UpdateState() called, IsManualOverride=true
3. **Persisted**: Each state stored in Session relationship
4. **Loaded**: Session resumed → StateContext loaded with Session
5. **Expired**: Session expires → StateContext cascade-deleted

**StateConfiguration Versioning**:

1. **Created**: Admin creates new state config → Version=1, IsActive=true
2. **Updated**: Admin updates config → New record created, Version++, old record IsActive=false
3. **Referenced**: StateContext always references active StateConfiguration by StateCode

---

## Validation Summary

| Entity                        | Validation Layer          | Rules                                                           |
| ----------------------------- | ------------------------- | --------------------------------------------------------------- |
| StateContext                  | Domain + FluentValidation | StateCode 2 chars, ZipCode 5 digits, EffectiveDate ≤ now        |
| StateConfiguration            | Domain + FluentValidation | StateCode 2 chars, ConfigData valid JSON                        |
| ZipCodeMapping                | Domain                    | ZipCode 5 digits, StateCode 2 chars                             |
| InitializeStateContextRequest | FluentValidation          | SessionId required, ZipCode 5 digits, StateCodeOverride 2 chars |

---

## Implementation Readiness

✅ **Entities defined** - StateContext, StateConfiguration, ZipCodeMapping  
✅ **Relationships mapped** - 1:1 Session→StateContext, N:1 StateContext→StateConfiguration  
✅ **Validation rules specified** - Format, length, business constraints  
✅ **DTOs defined** - C# and TypeScript contracts for API communication  
✅ **Database schema designed** - PostgreSQL tables, indexes, foreign keys

**Next**: Generate API contracts (OpenAPI spec), quickstart.md, update agent context
