# Data Model: Add Swagger to API Project

**Purpose**: Define data structures and relationships for Swagger integration  
**Phase**: 1 (Design)  
**Status**: Complete

## Overview

The Swagger integration doesn't introduce new entities to the domain model. Instead, it documents and exposes existing entities and their relationships. This document maps all entities that appear in API responses and defines their schemas for OpenAPI documentation.

## Entities & Schemas

### 1. Session (Core Entity)

**Purpose**: Represents a user's application session with all session state and metadata

**Attributes**:

- `id` (string, UUID): Unique session identifier
- `state` (enum: "draft", "submitted", "approved", "rejected"): Current session state
- `userId` (string, UUID, nullable): Reference to user who owns this session (null for anonymous)
- `ipAddress` (string): Client IP address
- `userAgent` (string): Browser user agent string
- `sessionType` (enum: "anonymous", "authenticated"): Session authentication type
- `encryptionKeyVersion` (integer): Encryption key version used for session data
- `expiresAt` (datetime, ISO 8601): Absolute expiry time
- `inactivityTimeoutAt` (datetime, ISO 8601): Sliding inactivity timeout
- `lastActivityAt` (datetime, ISO 8601): Most recent interaction timestamp
- `isRevoked` (boolean): Whether the session is revoked
- `createdAt` (datetime, ISO 8601): Database record creation time
- `updatedAt` (datetime, ISO 8601): Last modification time
- `isValid` (boolean): Computed validity flag
- `minutesUntilExpiry` (integer): Computed minutes until expiry
- `minutesUntilInactivityTimeout` (integer): Computed minutes until inactivity timeout

**Relationships**:

- `userId` → User (many sessions per user)
- Session → SessionAnswer (one-to-many: a session has multiple answers)

**Validation Rules**:

- `id` must be non-empty UUID
- `state` must be one of allowed enum values
- `ipAddress` must be non-empty string
- `userAgent` must be non-empty string
- `expiresAt` and `inactivityTimeoutAt` must be in the future at creation
- `lastActivityAt` must be <= `expiresAt`

**Example**:

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "state": "draft",
  "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
  "sessionType": "anonymous",
  "encryptionKeyVersion": 1,
  "expiresAt": "2026-02-10T18:30:00Z",
  "inactivityTimeoutAt": "2026-02-10T11:00:00Z",
  "lastActivityAt": "2026-02-10T10:45:00Z",
  "isRevoked": false,
  "createdAt": "2026-02-10T10:30:00Z",
  "updatedAt": "2026-02-10T10:45:00Z",
  "isValid": true,
  "minutesUntilExpiry": 480,
  "minutesUntilInactivityTimeout": 15
}
```

---

### 2. SessionAnswer (Response Data)

**Purpose**: Stores a single answer to a question within a session

**Attributes**:

- `id` (string, UUID): Unique identifier
- `sessionId` (string, UUID): Reference to parent session
- `fieldKey` (string): Question identifier from taxonomy
- `fieldType` (enum: "currency", "integer", "string", "boolean", "date", "text")
- `answerValue` (string, nullable): User's response (plain text representation)
- `isPii` (boolean): Whether the field contains PII
- `keyVersion` (integer): Encryption key version used
- `validationErrors` (string, nullable): JSON string array of validation errors
- `createdAt` (datetime, ISO 8601): When answer was first provided
- `updatedAt` (datetime, ISO 8601): When answer was last changed

**Relationships**:

- `sessionId` → Session (answers belong to one session)

**Validation Rules**:

- `id`, `sessionId`, `fieldKey` must be non-empty
- `answerValue` must not be null (empty string valid)
- `fieldType` must be one of allowed enum values
- Max length for answer values: 10000 characters (type-specific)

**Example**:

```json
{
  "id": "a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6",
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "fieldKey": "income_annual",
  "fieldType": "currency",
  "answerValue": "45000.00",
  "isPii": true,
  "keyVersion": 1,
  "validationErrors": null,
  "createdAt": "2026-02-10T11:00:00Z",
  "updatedAt": "2026-02-10T11:00:00Z"
}
```

---

### 3. SessionData (Export Format)

**Purpose**: Represents complete session export including answers and metadata for archival or integration
**Note**: Not currently exposed via public API endpoints; internal representation only

**Attributes**:

- `sessionId` (string): Session identifier
- `userId` (string): User identifier
- `exportedAt` (datetime): When export was generated
- `sessionMetadata` (object): Session-level metadata
- `answers` (array of SessionAnswer): All answers in this session
- `eligibilityResults` (object): Cached eligibility determination results

**Relationships**:

- Aggregates Session and SessionAnswer data
- References User for context

**Validation Rules**:

- `exportedAt` must not be in future
- `answers` array must not be empty
- All SessionAnswer items within must be valid per SessionAnswer schema

**Example**:

```json
{
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "exportedAt": "2026-02-10T15:00:00Z",
  "sessionMetadata": {
    "startedAt": "2026-02-10T10:30:00Z"
  },
  "answers": [
    {
      "answerId": "a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6",
      "sessionId": "550e8400-e29b-41d4-a716-446655440000",
      "questionId": "income_annual",
      "answerValue": 45000,
      "dataType": "number"
    }
  ],
  "eligibilityResults": {
    "status": "potentially_eligible",
    "programs": ["medicaid", "chip"]
  }
}
```

---

### 4. ValidationResult (Response DTO)

**Purpose**: Report validation or eligibility check outcome

**Attributes**:

- `isValid` (boolean): Whether validation passed
- `code` (string): Machine-readable error/success code
- `message` (string): Human-readable explanation
- `errors` (array of ValidationError): Detailed errors (if validation failed)
- `data` (object, optional): Success data (e.g., eligibility determination)

**Nested: ValidationError**:

- `field` (string): Which field has error (e.g., "income")
- `message` (string): What's wrong and how to fix it

**Relationships**:

- Response wrapper for validation operations
- May contain eligibility results or program matches

**Example**:

```json
{
  "isValid": false,
  "code": "VALIDATION_ERROR",
  "message": "Validation failed. See errors for details.",
  "errors": [
    {
      "field": "income",
      "message": "Income must be a positive number. Entered: -5000"
    },
    {
      "field": "dependents",
      "message": "Number of dependents must be 0 or positive"
    }
  ]
}
```

---

### 5. EncryptionKey (Security Entity)

**Purpose**: Manages encryption keys with rotation policy

**Attributes**:

- `keyId` (string, UUID): Unique key identifier
- `algorithm` (string): Encryption algorithm (e.g., "AES-256-GCM")
- `createdAt` (datetime): When key was generated
- `expiresAt` (datetime, nullable): When key expires and must be rotated
- `isActive` (boolean): Whether this key is used for new encryptions
- `metadata` (object): Key version, rotation policy info

**Relationships**:

- Infrastructure entity; not exposed in user-facing API
- Manages sensitive data encryption at rest

**Validation Rules**:

- `keyId` must be UUID
- `expiresAt` must be > `createdAt` if set
- Only one key may have `isActive = true` at a time

**Example** (internal only):

```json
{
  "keyId": "encryption-key-2026-feb",
  "algorithm": "AES-256-GCM",
  "createdAt": "2026-01-15T00:00:00Z",
  "expiresAt": "2026-04-15T00:00:00Z",
  "isActive": true,
  "metadata": {
    "version": 1,
    "rotationPolicy": "quarterly"
  }
}
```

---

### 6. User (Authentication Entity)

**Purpose**: Represents application users with role-based access control

**Attributes**:

- `userId` (string, UUID): Unique user identifier
- `email` (string): User's email address (unique)
- `role` (enum: "Admin", "Reviewer", "Analyst", "Applicant"): Authorization role
- `createdAt` (datetime): Account creation time
- `lastSignedInAt` (datetime, nullable): Most recent login

**Relationships**:

- User → Session (one-to-many)
- User → Role permissions (authorization context for API endpoints)

**Validation Rules**:

- `userId` must be UUID
- `email` must be valid email format
- `role` must be one of allowed enum values
- `lastSignedInAt` must be <= current time if set

**Example**:

```json
{
  "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "email": "user@example.com",
  "role": "Applicant",
  "createdAt": "2026-01-01T10:00:00Z",
  "lastSignedInAt": "2026-02-10T09:30:00Z"
}
```

---

## Relationship Diagram

```
┌─────────────────┐
│      User       │
│  (Authentication)
└────────┬────────┘
         │ 1:many
         │
    ┌────▼─────────────┐
    │     Session      │
    │  (Core Entity)   │
    └────┬──────┬──────┘
         │      │
         │      └──► SessionData (export)
         │ 1:many
    ┌────▼─────────────────────┐
    │   SessionAnswer          │
    │  (Question Responses)    │
    └──────────────────────────┘

┌─────────────────────┐
│  EncryptionKey      │
│  (Infrastructure)   │
│  [Not API-Exposed]  │
└─────────────────────┘

┌──────────────────────┐
│  ValidationResult    │
│  (Response DTO)      │
└──────────────────────┘
```

---

## OpenAPI Schema Mapping

All entities above will be documented in OpenAPI with:

**Session → components/schemas/Session**

- Documented in GET /api/sessions/{sessionId}
- Documented in POST /api/sessions

**SessionAnswer → components/schemas/SessionAnswer**

- Documented in POST /api/sessions/{sessionId}/answers
- Documented in GET /api/sessions/{sessionId}/answers

**SessionData → components/schemas/SessionData**

- Documented in GET /api/sessions/{sessionId}/export
- POST /api/sessions/import

**ValidationResult → components/schemas/ValidationResult**

- Used in all mutation responses (POST, PUT, DELETE)
- Error responses (400, 401, 403, 404, 500)

**User → components/schemas/User**

- Documented in GET /api/users/me
- Implied from JWT token payload

---

## CONST-II Testing Requirements

All entities must be testable in isolation:

1. **Entity Serialization Tests**: Verify JSON serialization/deserialization
2. **Validation Tests**: Verify validation rules (required fields, max lengths, formats)
3. **Schema Tests**: Verify OpenAPI schema is generated correctly for each entity
4. **Relationship Tests**: Verify object nesting in complex entities like SessionData

Example test structure:

```
MAA.Tests/
├── Unit/
│  └── Schemas/
│     ├── SessionSchemaTests.cs
│     ├── SessionAnswerSchemaTests.cs
│     ├── SessionDataSchemaTests.cs
│     ├── ValidationResultSchemaTests.cs
│     └── UserSchemaTests.cs
└── Integration/
   └── SchemaGeneration/
      └── SwaggerSchemaGenerationTests.cs
```

---

## Notes

- **EncryptionKey** is internal infrastructure and not exposed in OpenAPI
- **ValidationResult** is a response wrapper, not a domain entity
- All entities support JSON serialization for REST API
- Swagger will auto-generate schema from C# DTO classes
- Field descriptions come from XML comments in DTO classes
