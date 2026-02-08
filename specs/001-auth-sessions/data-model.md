# Phase 1 Design: Data Model - E1 Authentication & Session Management

**Status**: Design Complete — Ready for migration T12  
**Date**: 2026-02-08  
**Applies To**: [plan.md](./plan.md) Task T12-T15

---

## Domain Model Overview

Three core entities manage authentication and session state:

```
┌─────────────────────────────────────────────────────────────────┐
│ Session Domain                                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  User ◄─────────── Session ────────────► SessionAnswer        │
│  (1)   auth_user_id (M)  session_id (1)  (M)                   │
│                                                                 │
│  └─ EncryptionKey ◄─── Session (key_version)                   │
│     (indexes by version)                                       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Entity: Session

**Purpose**: Represents an anonymous or authenticated user's temporary session. Tracks timeout, permissions, and encryption key version.

**Columns**:

| Field                  | Type        | Constraints                     | Description                                                      |
| ---------------------- | ----------- | ------------------------------- | ---------------------------------------------------------------- |
| id                     | UUID        | PK                              | Session identifier; cryptographically random                     |
| state                  | VARCHAR(20) | FK(session_state)               | State machine: pending→in_progress→submitted→completed→abandoned |
| user_id                | UUID        | FK(users.id), NULL              | Applicant user (populated on login, Phase 5)                     |
| ip_address             | INET        | NOT NULL                        | Client IP (for anomaly detection, Phase 3)                       |
| user_agent             | TEXT        | NOT NULL                        | Browser user agent (device tracking)                             |
| session_type           | VARCHAR(50) | DEFAULT 'anonymous'             | 'anonymous' or 'authenticated' (Phase 5)                         |
| encryption_key_version | INT         | FK(encryption_keys.key_version) | Which key version encrypted session data                         |
| data                   | JSONB       | DEFAULT '{}'                    | Session-scoped data (timeout rules applied at app layer)         |
| expires_at             | TIMESTAMP   | NOT NULL                        | Absolute expiry (30 min for anonymous, 8 hr for admin)           |
| inactivity_timeout_at  | TIMESTAMP   | NOT NULL                        | Sliding window (reset on each action for Phase 5)                |
| last_activity_at       | TIMESTAMP   | DEFAULT NOW()                   | Track last request (for analytics, Phase 6)                      |
| is_revoked             | BOOLEAN     | DEFAULT FALSE                   | Explicit logout (Phase 5)                                        |
| created_at             | TIMESTAMP   | DEFAULT NOW()                   | Session creation                                                 |
| updated_at             | TIMESTAMP   | DEFAULT NOW()                   | Last update (for optimistic locking)                             |
| version                | INT         | DEFAULT 1                       | Optimistic lock version (concurrent write conflicts)             |

**Validation Rules**:

- `expires_at > NOW()` (always in the future)
- `inactivity_timeout_at > NOW()` (always in the future)
- `state` must be valid enum value
- `encryption_key_version` must reference active key (see EncryptionKey)

**Indexes**:

```sql
-- Performance: most queries filter by user_id
CREATE INDEX idx_session_user_id ON sessions(user_id) WHERE user_id IS NOT NULL;

-- Performance: cleanup tasks expire by expires_at
CREATE INDEX idx_session_expires_at ON sessions(expires_at);

-- Uniqueness: prevent session ID collisions
CREATE UNIQUE INDEX idx_session_id ON sessions(id);
```

**Relationships**:

- `sessions.user_id` → `users.id` (0..1 to 1, nullable until Phase 5)
- `sessions.encryption_key_version` → `encryption_keys.key_version` (many to 1)
- `sessions` ← `session_answers.session_id` (1 to many)

---

## Entity: SessionAnswer

**Purpose**: Stores applicant answers within a session. Supports both encrypted (PII) and plain text (non-sensitive) data. Dual storage for SSN (encrypted + hash for validation).

**Columns**:

| Field             | Type         | Constraints                     | Description                                                    |
| ----------------- | ------------ | ------------------------------- | -------------------------------------------------------------- |
| id                | UUID         | PK                              | Answer identifier                                              |
| session_id        | UUID         | FK(sessions.id)                 | Parent session                                                 |
| field_key         | VARCHAR(100) | NOT NULL                        | Questionnaire field identifier (e.g., "income_annual_2025")    |
| field_type        | VARCHAR(50)  | NOT NULL                        | Data type: 'text', 'number', 'date', 'currency', 'address'     |
| answer_plain      | TEXT         | NULL                            | Plaintext answer (for non-sensitive data)                      |
| answer_encrypted  | BYTEA        | NULL                            | Encrypted answer; randomized (e.g., income, disability status) |
| answer_hash       | BYTEA        | NULL                            | Deterministic hash for validation (e.g., SSN validation)       |
| key_version       | INT          | FK(encryption_keys.key_version) | Which key encrypted this answer                                |
| is_pii            | BOOLEAN      | DEFAULT FALSE                   | Personally identifiable? (income, SSN, address = TRUE)         |
| validation_errors | JSONB        | DEFAULT '[]'                    | Array of validation error messages from last save              |
| created_at        | TIMESTAMP    | DEFAULT NOW()                   | First submission                                               |
| updated_at        | TIMESTAMP    | DEFAULT NOW()                   | Last modification                                              |
| version           | INT          | DEFAULT 1                       | Optimistic lock (concurrent edits; last write wins)            |

**Validation Rules**:

- Exactly one of `answer_plain`, `answer_encrypted`, `answer_hash` is non-NULL (mutual exclusion)
- `is_pii=TRUE` → must be encrypted (not plain text)
- `is_pii=FALSE` → can be plain text (no encryption overhead)
- `key_version` must reference active key (EncryptionKey)

**Indexes**:

```sql
-- Performance: list answers by session
CREATE INDEX idx_answer_session_id ON session_answers(session_id);

-- Performance: validation by field (e.g., "find all SSN answers to check duplicates")
CREATE INDEX idx_answer_field_key ON session_answers(field_key, session_id);

-- Performance: SSN lookup (deterministic hash)
CREATE UNIQUE INDEX idx_answer_ssn_hash ON session_answers(answer_hash)
    WHERE field_key = 'ssn' AND answer_hash IS NOT NULL;
```

**Relationships**:

- `session_answers.session_id` → `sessions.id` (many to 1)
- `session_answers.key_version` → `encryption_keys.key_version` (many to 1)

---

## Entity: EncryptionKey

**Purpose**: Version control for encryption keys. Enables key rotation without decrypting all old data.

**Columns**:

| Field        | Type         | Constraints                       | Description                                                   |
| ------------ | ------------ | --------------------------------- | ------------------------------------------------------------- |
| key_version  | INT          | PK                                | Version sequence (1, 2, 3, ...)                               |
| key_id_vault | VARCHAR(256) | UNIQUE                            | Azure Key Vault key name (e.g., "maa-key-v001")               |
| algorithm    | VARCHAR(50)  | DEFAULT 'AES-256-GCM'             | Encryption algorithm (validated against supported list)       |
| is_active    | BOOLEAN      | DEFAULT TRUE                      | Can be used for NEW encryption operations                     |
| created_at   | TIMESTAMP    | DEFAULT NOW()                     | Key creation date                                             |
| rotated_at   | TIMESTAMP    | NULL                              | When this key was rotated INTO production                     |
| expires_at   | TIMESTAMP    | DEFAULT NOW() + INTERVAL '1 year' | Key retirement date (Phase 3+)                                |
| metadata     | JSONB        | DEFAULT '{}'                      | Additional context (key creation reason, migrated_from, etc.) |

**Validation Rules**:

- `key_version` starts at 1, increments by 1
- `is_active=TRUE` implies `expires_at > NOW()`
- `algorithm` must be in whitelist (AES-256-GCM, ChaCha20-Poly1305)
- `key_id_vault` must be valid Azure Key Vault key name format

**Unique Constraint**:

```sql
-- Only one "current" active key for a given algorithm
CREATE UNIQUE INDEX idx_active_key_per_algorithm
    ON encryption_keys(algorithm)
    WHERE is_active = TRUE;
```

**Relationships**:

- `sessions.encryption_key_version` → `encryption_keys.key_version` (many to 1)
- `session_answers.key_version` → `encryption_keys.key_version` (many to 1)

**Seed Data** (Migration T12):

```sql
INSERT INTO encryption_keys (key_version, key_id_vault, algorithm, is_active, rotated_at)
VALUES (1, 'maa-key-v001', 'AES-256-GCM', true, NOW());
```

---

## Session State Machine

Valid session states and transitions:

```
pending  ──→  in_progress  ──→  submitted  ──→  completed
   ↓              ↓
abandoned    abandoned
```

| State         | Meaning                                                              | Allowed Transitions                             | Timeout Behavior                                              |
| ------------- | -------------------------------------------------------------------- | ----------------------------------------------- | ------------------------------------------------------------- |
| `pending`     | Session created, applicant opening questionnaire                     | → in_progress                                   | Auto-abandoned if in_progress not reached within 5 min        |
| `in_progress` | Applicant actively filling out form                                  | → in_progress (no change), submitted, abandoned | Sliding window: 30 min inactivity (anonymous) or 8 hr (admin) |
| `submitted`   | Applicant submitted responses; waiting for eligibility determination | → completed, abandoned                          | Non-sliding: 24 hours absolute max (Phase 2 processing SLA)   |
| `completed`   | Eligibility determined; results shown to applicant                   | (terminal)                                      | Retained for 30 days (audit trail), then purged               |
| `abandoned`   | Applicant left or timed out                                          | (terminal)                                      | Purged after 7 days (privacy)                                 |

---

## Encryption Key Versioning Strategy

### Key Rotation Workflow

1. **Current State**: Key v1 is active

   ```sql
   SELECT * FROM encryption_keys WHERE is_active = TRUE;
   -- Result: key_version=1, is_active=TRUE
   ```

2. **New Key Generated** (e.g., quarterly)
   - Azure Key Vault generates new key (outside application)
   - New row added:
     ```sql
     INSERT INTO encryption_keys (key_version, key_id_vault, is_active)
     VALUES (2, 'maa-key-v002', FALSE);  -- Not active yet
     ```

3. **New Key Testing** (Phase 3+, optional)
   - Re-encryption job runs in background (doesn't block writes)
   - Old data still decryptable with v1
   - New writes use v1 (current active key)

4. **Activate New Key**
   - Deactivate old key:
     ```sql
     UPDATE encryption_keys SET is_active = FALSE WHERE key_version = 1;
     ```
   - Activate new key:
     ```sql
     UPDATE encryption_keys SET is_active = TRUE, rotated_at = NOW() WHERE key_version = 2;
     ```

5. **Decryption**
   - Application reads `key_version` stored with each encrypted value
   - Fetches appropriate key from Key Vault (cached 5 min per R3)
   - Decrypts with correct key

### Why Key Versioning Matters

- **Without versioning**: Rotating a key requires re-encrypting all data (hours/days downtime)
- **With versioning**: New key immediately usable; old data remains decryptable; no downtime
- **Crypto best practice**: Keys should be rotated quarterly; versioning enables this safely

---

## Encryption Strategy Detail: SSN Example

**Requirement** (from spec.md C2):

- SSN must be encrypted for storage
- SSN must be validatable (exact-match: "does this SN already exist?")
- SSN must be decryptable (show in results page)

**Solution: Dual Storage**

```sql
CREATE TABLE session_answers (
    id UUID PRIMARY KEY,
    session_id UUID REFERENCES sessions(id),
    field_key VARCHAR(100),  -- 'ssn'

    -- Randomized encryption (can display to user)
    answer_encrypted BYTEA NOT NULL,

    -- Deterministic hash (can search in SQL)
    answer_hash BYTEA NOT NULL UNIQUE,

    key_version INT REFERENCES encryption_keys(key_version),
    ...
);
```

**On Save (SSN = "123-45-6789")**:

```csharp
var answerEncrypted = EncryptionService.Encrypt(ssn, currentKey, randomIV: true);
var answerHash = HashService.Hmac(ssn, currentKey);  // Deterministic

context.SessionAnswers.Add(new SessionAnswer
{
    FieldKey = "ssn",
    AnswerEncrypted = answerEncrypted,    // Can decrypt later
    AnswerHash = answerHash,              // Can search/validate
    KeyVersion = 1
});
```

**On Validation (Is SSN "123-45-6789" already used?)**:

```sql
SELECT COUNT(*) FROM session_answers
WHERE field_key = 'ssn'
  AND answer_hash = hmac('123-45-6789', key_material)
  AND session_id != @currentSessionId;
```

**On Display (Show SSN in results)**:

```csharp
var answer = context.SessionAnswers.FirstAsync(x => x.FieldKey == "ssn");
var decrypted = EncryptionService.Decrypt(answer.AnswerEncrypted, key);
// Display: 123-45-6789
```

---

## Database Threading & Concurrency

### Optimistic Locking Pattern

**Problem**: Two concurrent requests updating same session/answer?

```
Thread A: Read session v=1, update expires_at, write v=1 → v=2
Thread B: Read session v=1, update state, write v=1 → v=2  [CONFLICT]
```

**Solution**: Version column + WHERE clause validation

```csharp
// EF Core with version column
var session = await context.Sessions
    .FirstAsync(s => s.Id == sessionId);

// Modify
session.State = SessionState.InProgress;

try
{
    // Only save if version hasn't changed
    context.Sessions.Update(session);
    await context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    // Retry or fail gracefully
    throw new SessionConcurrencyException("Session was modified by another request");
}
```

**Recommended Behavior** (spec.md C5: optimistic locking):

- On conflict: Last write wins (app overrides second update)
- Reasoning: Session state changes are idempotent (timeout rules apply regardless)
- Exception: Don't override state to "completed" if already "submitted" (use pessimistic lock for state transitions)

### Connection Pooling

```csharp
// Program.cs
builder.Services.AddDbContext<SessionContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.CommandTimeout(30)
    )
);

// Connection pooling (built-in, default: 25 connections)
// For 1000 concurrent users: need ~50 connections (2% of requests waiting)
// Configure if needed:
// services.AddDbContextPool<SessionContext>(30);  // 30 connections
```

---

## Data Retention & Cleanup

### Archive Strategy

| State                  | Retention | Action                                 | Timing                    |
| ---------------------- | --------- | -------------------------------------- | ------------------------- |
| `completed`            | 30 days   | Archive to cold storage (Phase 3+)     | Automatic nightly job     |
| `abandoned`            | 7 days    | Delete (privacy; audit via logs)       | Automatic nightly job T40 |
| `pending` (no timeout) | 5 minutes | Auto-abandon; then delete after 7 days | Automatic                 |

**Cleanup Query**:

```sql
-- Delete abandoned sessions older than 7 days
DELETE FROM sessions
WHERE state = 'abandoned'
  AND created_at < NOW() - INTERVAL '7 days';

-- Archive completed sessions older than 30 days
INSERT INTO session_archive (SELECT * FROM sessions WHERE state = 'completed' AND created_at < NOW() - INTERVAL '30 days');
DELETE FROM sessions WHERE state = 'completed' AND created_at < NOW() - INTERVAL '30 days';
```

**Scheduled Job** (T40):

```csharp
// Run nightly at 2 AM
public class SessionCleanupService
{
    public async Task CleanupAbandonedSessions()
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        await context.Database.ExecuteSqlAsync(
            "DELETE FROM sessions WHERE state = 'abandoned' AND created_at < {0}", cutoff);

        logger.LogInformation("Deleted abandoned sessions older than {cutoff}", cutoff);
    }
}
```

---

## Summary: Critical Design Decisions

1. ✅ **Dual encryption** (SSN randomized + hash) → Balances security + searchability
2. ✅ **Key versioning** → Safe key rotation without downtime
3. ✅ **Optimistic locking** → Concurrent updates without deadlocks
4. ✅ **Deterministic hashing** (HMAC) → UX validation without full decryption
5. ✅ **Connection pooling** → Handle 1000 concurrent users efficiently
6. ✅ **30/7-day retention** → Privacy + audit trail
7. ✅ **State machine** → Clear session lifecycle validation

**Ready for**: Migration (T12-T13), API contract design (contracts/), and implementation (T14-T42)
