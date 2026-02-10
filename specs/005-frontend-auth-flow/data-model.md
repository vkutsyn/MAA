# Data Model: Frontend Authentication Flow with Login/Registration

## Entities

### User

**Description**: A registered person with access to authenticated features.

**Fields**:

- `userId` (string, UUID)
- `email` (string)
- `fullName` (string)
- `role` (string)

**Validation Rules**:

- `email` must be a valid email format
- `fullName` required for registration

---

### Session Credential

**Description**: Short-lived credential proving the user is authenticated.

**Fields**:

- `accessToken` (string)
- `tokenType` (string, expected "Bearer")
- `expiresInSeconds` (number)

**Validation Rules**:

- `expiresInSeconds` > 0

---

### Session Renewal Credential

**Description**: Long-lived credential used to renew authentication.

**Fields**:

- `refreshToken` (string, opaque)

**Validation Rules**:

- Must be present for renewal attempts

---

### Active Session

**Description**: A concurrent session record returned when login is rejected due to max sessions.

**Fields**:

- `sessionId` (string, UUID)
- `device` (string)
- `ipAddress` (string)
- `loginTime` (string, ISO-8601)

---

### Auth State

**Description**: Frontend state representing authentication status.

**Fields**:

- `status` (enum: `unauthenticated`, `authenticating`, `authenticated`, `renewing`)
- `user` (User or null)
- `sessionCredential` (Session Credential or null)
- `lastError` (string or null)
- `returnPath` (string or null)

---

## Relationships

- A `User` can have multiple `Active Session` entries when concurrent sessions are in use.
- `Auth State` may include one `User` and one `Session Credential` at a time.

---

## State Transitions

- `unauthenticated` → `authenticating` on login attempt
- `authenticating` → `authenticated` on successful login
- `authenticating` → `unauthenticated` on login failure
- `authenticated` → `renewing` when session renewal is attempted
- `renewing` → `authenticated` on renewal success
- `renewing` → `unauthenticated` on renewal failure
- `authenticated` → `unauthenticated` on logout
