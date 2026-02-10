# Quickstart: Using Swagger for MAA API Development

**Purpose**: Help developers discover and test the API using Swagger UI  
**Audience**: Frontend developers, API consumers, integration partners  
**Last Updated**: February 10, 2026

## What is Swagger / OpenAPI?

Swagger is an interactive documentation tool that lets you:
- **View** all API endpoints and their parameters
- **Test** endpoints with real data directly from the browser
- **Understand** request/response schemas and validation rules
- **Download** the full API specification (JSON or YAML format)

This saves you time by replacing static documentation with a living, test-able spec that always matches the running API code.

---

## Accessing Swagger UI

### Development

When the MAA API is running locally in development mode:

```
http://localhost:5000/swagger
```

or

```
http://localhost:5000/api/docs
```

You'll see an interactive interface listing all available endpoints.

### Test Environment

```
https://maa-test.azurewebsites.net/swagger
```

### Production

**Note**: Swagger is disabled in production for security (no endpoint documentation exposed publicly). To access the API specification in production, use the `openapi.json` endpoint if enabled per deployment configuration.

---

## Using Swagger UI: Step-by-Step

### 1. View All Available Endpoints

On the Swagger landing page, you'll see all endpoints grouped by category (Sessions, Rules, Admin, etc.). Each endpoint shows:
- **HTTP Method** (GET, POST, PUT, DELETE)
- **Path** (e.g., `/api/sessions/{sessionId}`)
- **Brief Description**

### 2. Expand an Endpoint to See Details

Click any endpoint to expand it and see:

```
GET /api/sessions/{sessionId}
├─ Description: Retrieve a specific session and its metadata
├─ Parameters:
│  └─ sessionId (path/required): UUID of the session
├─ Security: Requires Bearer Token
├─ Request Example: GET /api/sessions/550e8400-e29b-41d4-a716-446655440000
├─ Responses:
│  ├─ 200 OK → Session object
│  ├─ 400 Bad Request → ValidationResult (invalid UUID)
│  ├─ 401 Unauthorized → ValidationResult (missing token)
│  ├─ 404 Not Found → ValidationResult (session doesn't exist)
│  └─ 500 Server Error → ValidationResult
└─ Schema: Shows the exact structure of the
Session object (all fields, types, required)
```

### 3. Understand the Schema

Click on a response type (e.g., "Session") to see the full data structure:

```json
{
  "sessionId": "string (UUID) - Unique identifier",
  "userId": "string (UUID) - User who owns this session",
  "status": "enum(draft, submitted, approved, rejected)",
  "startedAt": "datetime - ISO 8601 format",
  "lastActivityAt": "datetime",
  "createdAt": "datetime",
  "updatedAt": "datetime",
  "metadata": "object - Any additional properties"
}
```

This tells you exactly what fields exist, their types, and which are required.

### 4. Authenticate with JWT Token

To test endpoints that require authentication:

1. Click the green **"Authorize"** button at the top of Swagger
2. In the dialog, paste your JWT bearer token:
   ```
   eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```
3. Click **"Authorize"**
4. All subsequent testing requests will include the Authorization header

**Where to get a token?**
- For development/testing: Use the Auth API to login and receive a JWT
  ```
  POST /auth/login
  {
    "email": "test@example.com",
    "password": "password123"
  }
  ```
- Response includes `token` field – copy and paste into Swagger Authorize

### 5. Test an Endpoint with "Try It Out"

To execute a real request from the browser:

1. Expand an endpoint (e.g., `GET /api/sessions/{sessionId}`)
2. Click the **"Try it out"** button
3. Fill in required parameters:
   - For path params: replace `{sessionId}` with an actual UUID
   - For query params: enter filter values
   - For body: enter JSON payload
4. Click **"Execute"**
5. See the actual response from the server (200, 400, 404, etc.)

**Example**: Test retrieving a session
```
GET /api/sessions/550e8400-e29b-41d4-a716-446655440000

Response (200 OK):
{
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "status": "draft",
  ...
}
```

---

## Common Tasks

### Task: Find an Endpoint

**Goal**: I want to retrieve all answers for a session

**Solution**:
1. Look for "Sessions" section in Swagger
2. Search for "answers" in endpoints
3. Find: `GET /api/sessions/{sessionId}/answers`
4. Click to see parameters and schema

### Task: Understand Required Fields

**Goal**: What fields must I provide to create a session?

**Solution**:
1. Find endpoint: `POST /api/sessions`
2. Expand and scroll to "Request body"
3. Look for fields marked with **red asterisk (*)** = required
4. Example shows sample payload with all required fields

### Task: See All Possible Error Responses

**Goal**: What can go wrong when creating an answer?

**Solution**:
1. Find endpoint: `POST /api/sessions/{sessionId}/answers`
2. Expand "Responses" section
3. See all status codes:
   - **200** = Success (answer saved)
   - **400** = Validation error (invalid answer value)
   - **401** = Missing authentication
   - **404** = Session doesn't exist
4. Click each response to see example error payloads

### Task: Download the Full API Specification

**Goal**: I need the OpenAPI spec in JSON or YAML format for tools/scripts

**Solution**:
1. At the top of Swagger UI, click **"swagger.json"** or **"swagger.yaml"**
2. Browser shows the raw specification
3. Save as `.json` or `.yaml` file
4. Use with code generators:
   ```bash
   openapi-generator-cli generate -i swagger.json -g csharp -o MaaApiClient/
   ```

---

## Important Authentication Concepts

### JWT Bearer Token Format

When you click "Authorize" in Swagger, you're providing an **HTTP Bearer Token**:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

The token itself (after "Bearer ") is a JWT (JSON Web Token) containing:
- **sub** (subject): User ID
- **email**: User's email
- **role**: User's role (Admin, Reviewer, Analyst, Applicant)
- **exp** (expiration): When token expires (Unix timestamp)

**Token Expiration**: Tokens expire after a set time (usually 1 hour). If you get a 401 Unauthorized, your token may have expired – get a fresh one by logging in again.

### JWT in Code

If you're writing API client code (C#, JavaScript, Python):

```csharp
// Example: C# HttpClient
var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
var request = new HttpRequestMessage(HttpMethod.Get, "/api/sessions/123");
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
var response = await client.SendAsync(request);
```

---

## Troubleshooting

### "401 Unauthorized" Error

**Cause**: Missing or invalid JWT token

**Fix**:
1. Click "Authorize" button
2. Obtain a fresh token by logging in
3. Paste the NEW token and try again
4. Check that token isn't expired (older than 1 hour)

### "404 Not Found"

**Cause**: Resource doesn't exist or session ID is wrong

**Fix**:
1. Double-check session ID is correct UUID format
2. Create a session first (POST /api/sessions) to get a valid ID
3. Use that ID in subsequent requests

### "400 Bad Request"

**Cause**: Invalid data in request body

**Fix**:
1. Expand the endpoint and view "Request body" schema
2. Check which fields are required (marked with *)
3. Match the exact data types (string, number, boolean, etc.)
4. Review error message in response – it suggests what's wrong

Example fix:
```json
❌ Wrong:
{
  "questionId": 123,
  "answerValue": "45000"
}

✅ Correct:
{
  "questionId": "annual_income",
  "answerValue": 45000
}
```

### Swagger Won't Load

**Cause**: API server not running or Swagger disabled for environment

**Fix**:
1. Ensure API is running: `dotnet run --configuration Development`
2. Check URL matches your environment (localhost:5000 vs cloud URL)
3. Check appsettings.Development.json doesn't disable Swagger

---

## Tips for Effective API Testing in Swagger

1. **Test happy path first**: Create a session → Add answer → Retrieve it
2. **Test error cases**: Try invalid data, wrong IDs, missing auth
3. **Copy response data**: Use real responses from one test as input to the next
4. **Use browser DevTools**: Right-click → Inspect to see actual HTTP requests/responses
5. **Document your findings**: Note endpoints that differ from spec expectations
6. **Share examples**: If you find a useful endpoint combination, share the example with teammates

---

## API Response Structure

All endpoints follow a standard response pattern for errors:

```json
{
  "isValid": false,
  "code": "ERROR_CODE",
  "message": "Human-readable explanation",
  "errors": [
    {
      "field": "fieldName",
      "message": "What went wrong and how to fix it"
    }
  ]
}
```

Success responses return the resource directly (Session, SessionAnswer, etc.) without wrapping.

---

## Next Steps

1. ✅ Open Swagger UI: `/swagger`
2. ✅ Click "Authorize" and enter test JWT token
3. ✅ Expand `GET /api/sessions/{sessionId}`
4. ✅ Click "Try it out" with a valid session ID
5. ✅ Review the response structure
6. ✅ Test other endpoints (POST create, GET list, etc.)

For more detailed API specification, see [contracts/sessions-api.md](../contracts/sessions-api.md).

---

## References

- [OpenAPI Specification 3.0](https://spec.openapis.org/oas/v3.0.3) (official spec)
- [Swagger UI Documentation](https://github.com/swagger-api/swagger-ui) (tool documentation)
- [JWT.io](https://jwt.io) (JWT decoder – helpful for debugging tokens)
- MAA API Specification: See `data-model.md` for entity details
