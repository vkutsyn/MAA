# JWT Authentication Guide - MAA API

**Last Updated**: February 10, 2026  
**API Version**: 1.0.0  
**Feature**: 003-add-swagger - Phase 6 (T058)

## Overview

The Medicaid Application Assistant (MAA) API uses **JWT (JSON Web Token)** bearer authentication to secure protected endpoints. This document explains how to authenticate, use tokens in Swagger UI, and integrate authentication in your applications.

## Table of Contents

1. [Authentication Flow](#authentication-flow)
2. [Getting a JWT Token](#getting-a-jwt-token)
3. [Using Tokens in Swagger UI](#using-tokens-in-swagger-ui)
4. [Using Tokens in Code](#using-tokens-in-code)
5. [Token Expiration & Refresh](#token-expiration--refresh)
6. [Troubleshooting](#troubleshooting)

---

## Authentication Flow

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Database

    Client->>API: POST /api/auth/register (email, password)
    API->>Database: Create user record
    API-->>Client: 201 Created (userId)

    Client->>API: POST /api/auth/login (email, password)
    API->>Database: Verify credentials
    API-->>Client: 200 OK (accessToken, refreshToken)

    Client->>API: GET /api/sessions/{id}<br/>Authorization: Bearer {accessToken}
    API->>API: Validate JWT token
    API-->>Client: 200 OK (session data)

    Note over Client,API: Token expires after 15 minutes

    Client->>API: POST /api/auth/refresh<br/>refreshToken in cookie
    API->>Database: Verify refresh token
    API-->>Client: 200 OK (new accessToken)
```

### Security Model

- **Public Endpoints**: `/api/auth/register`, `/api/auth/login`, `/health/*`
- **Protected Endpoints**: All `/api/sessions`, `/api/rules`, `/api/admin` endpoints
- **Token Type**: Bearer JWT (JSON Web Token)
- **Token Lifetime**: 
  - Access Token: 15 minutes
  - Refresh Token: 7 days
- **Max Concurrent Sessions**: 3 per user

---

## Getting a JWT Token

### Step 1: Register a New User

**Endpoint**: `POST /api/auth/register`

**Request**:
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "fullName": "John Doe"
}
```

**Response (201 Created)**:
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Password Requirements**:
- Minimum 8 characters
- Must contain uppercase, lowercase, number, and special character

### Step 2: Login to Get Token

**Endpoint**: `POST /api/auth/login`

**Request**:
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK)**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "7d8f9a0b-1c2d-3e4f-5a6b-7c8d9e0f1a2b",
  "expiresIn": 900,
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com"
}
```

**Notes**:
- `token`: Access token (short-lived, 15 minutes)
- `refreshToken`: Used to get new access token (stored in HTTP-only cookie)
- `expiresIn`: Token lifetime in seconds (900 = 15 minutes)

### Step 3: Use Token in Requests

Include the access token in the `Authorization` header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Using Tokens in Swagger UI

### Interactive Testing with Swagger

1. **Navigate to Swagger UI**:
   ```
   http://localhost:5000/swagger
   ```

2. **Login to Get Token**:
   - Expand the `/api/auth/login` endpoint
   - Click "Try it out"
   - Enter your credentials:
     ```json
     {
       "email": "user@example.com",
       "password": "SecurePassword123!"
     }
     ```
   - Click "Execute"
   - Copy the `token` value from the response

3. **Authorize Swagger**:
   - Click the **"Authorize"** button at the top right of Swagger UI
   - In the "Value" field, paste your token:
     ```
     Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
     ```
     (Note: "Bearer " prefix is added automatically by Swagger UI)
   - Click "Authorize"
   - Click "Close"

4. **Test Protected Endpoints**:
   - Expand any protected endpoint (e.g., `/api/sessions`)
   - Click "Try it out"
   - Enter parameters
   - Click "Execute"
   - The Authorization header is automatically included

5. **Visual Indicator**:
   - Protected endpoints show a 🔒 lock icon
   - After authorization, the lock appears **unlocked** (🔓)
   - Swagger automatically includes your token in all requests

---

## Using Tokens in Code

### C# / .NET

**Using HttpClient**:

```csharp
var client = new HttpClient();
client.BaseAddress = new Uri("http://localhost:5000");

// Login to get token
var loginRequest = new { email = "user@example.com", password = "SecurePassword123!" };
var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

// Use token in subsequent requests
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", loginResult.Token);

// Call protected endpoint
var sessionResponse = await client.GetAsync("/api/sessions/550e8400-e29b-41d4-a716-446655440000");
var session = await sessionResponse.Content.ReadFromJsonAsync<SessionDto>();
```

### JavaScript / Fetch API

**Browser Fetch**:

```javascript
// Login to get token
const loginResponse = await fetch('http://localhost:5000/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'SecurePassword123!'
  })
});

const { token } = await loginResponse.json();

// Use token in subsequent requests
const sessionResponse = await fetch(
  'http://localhost:5000/api/sessions/550e8400-e29b-41d4-a716-446655440000',
  {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  }
);

const session = await sessionResponse.json();
```

### cURL / Command Line

**cURL Example**:

```bash
# 1. Login to get token
TOKEN=$(curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"SecurePassword123!"}' \
  | jq -r '.token')

# 2. Use token in request
curl -X GET http://localhost:5000/api/sessions/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer $TOKEN"
```

### Python / Requests

**Python Example**:

```python
import requests

# Login to get token
login_response = requests.post(
    'http://localhost:5000/api/auth/login',
    json={'email': 'user@example.com', 'password': 'SecurePassword123!'}
)
login_data = login_response.json()
token = login_data['token']

# Use token in subsequent requests
headers = {'Authorization': f'Bearer {token}'}
session_response = requests.get(
    'http://localhost:5000/api/sessions/550e8400-e29b-41d4-a716-446655440000',
    headers=headers
)
session = session_response.json()
```

---

## Token Expiration & Refresh

### Access Token Expiration

- **Lifetime**: 15 minutes
- **Behavior**: After expiration, protected endpoints return **401 Unauthorized**
- **Error Response**:
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
    "title": "Unauthorized",
    "status": 401,
    "detail": "Token has expired"
  }
  ```

### Refreshing Tokens

Instead of re-prompting users for credentials, use the **refresh token** to get a new access token:

**Endpoint**: `POST /api/auth/refresh`

**Request**:
```json
{
  "refreshToken": "7d8f9a0b-1c2d-3e4f-5a6b-7c8d9e0f1a2b"
}
```

**Response (200 OK)**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900
}
```

**Implementation Pattern**:

```csharp
public async Task<string> EnsureValidTokenAsync(string currentToken, string refreshToken)
{
    // Try request with current token
    var response = await CallApiAsync(currentToken);

    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
        // Token expired - refresh it
        var newToken = await RefreshTokenAsync(refreshToken);
        return newToken;
    }

    return currentToken;
}
```

### Logout

**Endpoint**: `POST /api/auth/logout`

**Request** (with Authorization header):
```
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
  "message": "Logged out successfully"
}
```

**Effect**:
- Invalidates refresh token
- Ends current session
- Access token remains valid until expiration (15 min) but can't be refreshed

---

## Troubleshooting

### Common Issues

#### 1. **401 Unauthorized on Protected Endpoint**

**Cause**: No token provided or invalid token

**Solution**:
```bash
# Verify token is included in Authorization header
curl -X GET http://localhost:5000/api/sessions/{id} \
  -H "Authorization: Bearer {token}" \
  -v
```

Check for:
- Authorization header present
- Bearer prefix included
- Token not expired (< 15 minutes old)

#### 2. **Swagger "Authorize" Button Not Working**

**Cause**: Token format incorrect

**Solution in Swagger UI**:
- Click "Authorize"
- Paste token **without** "Bearer " prefix (Swagger adds it automatically)
- Click "Authorize" then "Close"
- Lock icon (🔒) should change to unlocked (🔓)

#### 3. **Token Expired Error**

**Response**:
```json
{
  "status": 401,
  "detail": "Token has expired"
}
```

**Solution**: Use refresh token to get new access token:
```bash
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"{your-refresh-token}"}'
```

#### 4. **Max Concurrent Sessions (409 Conflict)**

**Response**:
```json
{
  "error": "Maximum concurrent sessions reached (3)",
  "activeSessions": [
    { "sessionId": "...", "startedAt": "2026-02-10T10:00:00Z", "ipAddress": "192.168.1.1" }
  ]
}
```

**Solution**: Logout from an existing session or wait for session timeout

#### 5. **Invalid Credentials (Login Failed)**

**Response (401 Unauthorized)**:
```json
{
  "error": "Invalid email or password"
}
```

**Solution**:
- Verify email is correct (case-insensitive)
- Verify password matches registration
- Check if account exists (try registering)

#### 6. **Token Missing "Bearer" Prefix**

**Incorrect**:
```
Authorization: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Correct**:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Solution**: Always include "Bearer " prefix (note the space after "Bearer")

---

## Security Best Practices

### For Developers

1. **Store Tokens Securely**:
   - Never commit tokens to version control
   - Use environment variables or secure storage (KeyVault, etc.)
   - Don't log tokens in application logs

2. **Token Lifetime**:
   - Access tokens expire in 15 minutes (short-lived by design)
   - Refresh tokens expire in 7 days
   - Implement automatic token refresh before expiration

3. **HTTPS Only**:
   - Use HTTPS in production (tokens transmitted over insecure HTTP can be intercepted)
   - Development: HTTP acceptable for localhost testing

4. **Logout on User Action**:
   - Call `/api/auth/logout` when user explicitly logs out
   - Clear stored tokens from client-side storage

### For Frontend Applications

**Recommended Token Storage**:
- **Web Apps**: HTTP-only cookies (managed by API) for refresh tokens
- **Single-Page Apps (SPA)**: Memory or sessionStorage for access tokens
- **Mobile Apps**: Secure storage (Keychain on iOS, Keystore on Android)

**Do NOT**:
- Store tokens in localStorage (vulnerable to XSS attacks)
- Store tokens in plain text files
- Include tokens in URL query parameters

---

## Token Structure (JWT)

**JWT Format**: `header.payload.signature`

**Example Token**:
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI1NTBlODQwMC1lMjliLTQxZDQtYTcxNi00NDY2NTU0NDAwMDAiLCJlbWFpbCI6InVzZXJAZXhhbXBsZS5jb20iLCJyb2xlIjoiVXNlciIsImV4cCI6MTY0NDUxMjAwMH0.signature
```

**Decoded Payload**:
```json
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",  // User ID
  "email": "user@example.com",                    // Email
  "role": "User",                                 // User role
  "exp": 1644512000,                              // Expiration timestamp (Unix epoch)
  "iat": 1644511100,                              // Issued at timestamp
  "nbf": 1644511100                               // Not before timestamp
}
```

**Validation**: Tokens are cryptographically signed - tampering invalidates the signature

---

## API Reference Quick Links

- **Register**: `POST /api/auth/register`
- **Login**: `POST /api/auth/login`
- **Refresh Token**: `POST /api/auth/refresh`
- **Logout**: `POST /api/auth/logout`
- **Protected Endpoints**: All `/api/sessions/*`, `/api/rules/*`, `/api/admin/*`

See [Swagger UI](http://localhost:5000/swagger) for interactive API documentation.

---

## Related Documentation

- [README.md](../README.md) - Project setup and overview
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Development guidelines
- [quickstart.md](../specs/003-add-swagger/quickstart.md) - Swagger UI usage guide
- [MAA Constitution](../.specify/memory/constitution.md) - Security principles

---

**Questions or Issues?**
- Check Swagger UI for endpoint documentation
- Review troubleshooting section above
- Open an issue on GitHub
