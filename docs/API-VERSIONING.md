# API Versioning Strategy

**Last Updated**: February 10, 2026  
**Document**: 003-add-swagger - Phase 7 (T063)  
**Current API Version**: 1.0.0

## Overview

This document outlines the API versioning strategy for the Medicaid Application Assistant (MAA) API, including how versions are managed, deprecated, and migrated.

## Current State

### Version: 1.0.0 (February 2026)

- **Status**: Initial release (MVP)
- **Route Pattern**: `/api/v1/*` (future) or single version (current)
- **Backward Compatibility**: Not applicable - first version
- **Planned Until**: Minor versions (1.1.0, 1.2.0) maintain backward compatibility

## Versioning Scheme

### Semantic Versioning

The API follows **Semantic Versioning 2.0.0**:

```
MAJOR.MINOR.PATCH

Example: 1.0.0
├─ Major: 1  (Breaking changes - new endpoints removed, contracts changed)
├─ Minor: 0  (New features - backward compatible)
└─ Patch: 0  (Bug fixes - backward compatible)
```

### Version Scope

- **Version Number**: Applies to entire API (not per-endpoint)
- **Backward Compatibility**: Maintained across minor versions
- **Support Window**: TBD (recommend N+1 versions)

## Versioning Strategies (Future)

### Strategy 1: URL Path Versioning (Recommended)

**Pattern**: `/api/v{version}/resource`

**Example**:

- `GET /api/v1/sessions/{id}` (current - 1.0.0)
- `GET /api/v2/sessions/{id}` (new - 2.0.0, with breaking changes)

**Advantages**:

- Clear, visible in URL
- Easy to route to different handlers
- Browsers cache by URL

**Disadvantages**:

- More endpoints to maintain
- Code duplication if only minor changes

**Implementation**:

```csharp
// Program.cs
app.MapControllers(); // Uses [Route("api/v1/[controller]")] attributes

// SessionsController for v1
[ApiController]
[Route("api/v1/[controller]")]
public class SessionsController : ControllerBase { ... }

// SessionsControllerV2 for v2 (if breaking changes needed)
[ApiController]
[Route("api/v2/[controller]")]
public class SessionsControllerV2 : ControllerBase { ... }
```

### Strategy 2: Header-Based Versioning

**Pattern**: `Accept-Version: 1.0.0` header

**Example**:

```bash
GET /api/sessions/{id}
Accept-Version: 1.0.0
```

**Advantages**:

- Keeps URLs clean
- All versions on same route

**Disadvantages**:

- Less visible
- More complex routing logic
- Cache issues

### Strategy 3: Query Parameter Versioning

**Pattern**: `?api-version=1.0.0`

**Example**:

```bash
GET /api/sessions/{id}?api-version=1.0.0
```

**Disadvantages**:

- Easy to forget
- Cache issues
- Not REST-compliant

### Recommended: URL Path Versioning

The MAA API will use **URL path versioning** when v2 is needed:

- Current: `/api/v1/sessions`, `/api/v1/rules`, etc.
- Future: `/api/v2/...` (if breaking changes required)
- Old versions: Deprecated after support window, removed after 2-3 minor releases

---

## Backward Compatibility Policy

### Non-Breaking Changes (Minor Version)

These changes do **not** require a new major version:

✅ **Safe to Add** (Backward Compatible):

- New optional query parameters (default to sensible values)
- New optional request body fields (defaults applied)
- New response fields (clients ignore unknown fields)
- New endpoints (parallel to existing)
- Additional status codes (subsume old ones)

❌ **Breaking** (Requires New Major Version):

- Removing required fields
- Removing endpoints
- Changing field types
- Changing status codes (e.g., 404 → 410)
- Changing response structure

### Examples

**Minor Change (1.0.0 → 1.1.0)**:

```json
// 1.0.0 Response
{"sessionId": "...", "status": "draft"}

// 1.1.0 Response (backward compatible - client ignores new field)
{"sessionId": "...", "status": "draft", "expiresAt": "2026-02-10T..."}
```

**Breaking Change (1.x.x → 2.0.0)**:

```json
// 1.0.0 Request
{"fieldKey": "income", "fieldValue": "50000"}

// 2.0.0 Request (structure changed)  ← BREAKING
{"fieldName": "income", "value": 50000}
```

---

## Deprecation Process

### Timeline

When retiring an API version:

1. **Current (Months 1-6)**: Version 1.0.0 in production, actively maintained
2. **Deprecated (Months 6-12)**: Version 1.0.0 still works, but:
   - Mark endpoints with `@deprecated` annotation
   - Add deprecation warning headers
   - Notify clients in release notes
   - Documentation clearly states "deprecated"
3. **Sunset (Month 12+)**: Version 1.0.0 no longer supported
   - Returns 410 Gone (optionally)
   - Redirect to new version (optional)
   - Removed from infrastructure

### Deprecation Notification

**HTTP Header** (when endpoint deprecated):

```
Deprecation: true
Sunset: Fri, 13 Dec 2026 00:00:00 GMT
Link: </api/v2/sessions>; rel="successor-version"
```

**XML Documentation** (in code):

```csharp
/// <summary>
/// [DEPRECATED] Use /api/v2/sessions instead.
/// This endpoint will be removed on 2026-12-15.
/// </summary>
[Obsolete("Use SessionsControllerV2 instead. Removal date: 2026-12-15")]
[HttpGet("{sessionId}")]
public async Task<IActionResult> GetSession(Guid sessionId) { ... }
```

**Swagger Documentation**:

```csharp
// Swagger displays [Obsolete] marked endpoints with strikethrough
// and deprecation notice
```

---

## Version Management in Code

### Current Version Configuration

**appsettings.json**:

```json
{
  "Swagger": {
    "Version": "1.0.0",
    "Title": "Medicaid Application Assistant API",
    "Description": "API for Medicaid/CHIP applications"
  }
}
```

### Future Version Configuration

When v2 is needed:

```json
{
  "Swagger": {
    "Version": "2.0.0",
    "Title": "Medicaid Application Assistant API",
    "Description": "API for Medicaid/CHIP applications"
  }
}
```

**Program.cs** would add both v1 and v2 documentation:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    // v1 (deprecated after cutover)
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MAA API (v1 - Deprecated)",
        Version = "1.0.0",
        Description = "...",
        Deprecated = true
    });

    // v2 (current)
    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Medicaid Application Assistant API",
        Version = "2.0.0",
        Description = "..."
    });
});
```

---

## Client Migration Guide

### For Client Applications

When a new major version is released:

1. **Preparation Phase** (Month 1-6):
   - Continue using current version (v1)
   - Monitor deprecation notices in HTTP headers
   - Review v2 documentation

2. **Migration Phase** (Month 6-12):
   - Test against v2 in test environment
   - Update client code to use `/api/v2/` paths
   - Update token refresh logic if authentication changed
   - Test with production data (anonymized)

3. **Cutover Phase** (Month 12+):
   - Deploy v2 client code to production
   - Monitor for issues
   - v1 support ends

### Example Migration

**Before (v1)**:

```csharp
var client = new HttpClient { BaseAddress = new Uri("https://api.maa.gov/api/v1/") };
var response = await client.GetAsync("sessions/123");
```

**After (v2)**:

```csharp
var client = new HttpClient { BaseAddress = new Uri("https://api.maa.gov/api/v2/") };
var response = await client.GetAsync("sessions/123");
```

---

## Version Support Matrix

| Version | Status  | Released | Deprecated | Sunset | Supported |
| ------- | ------- | -------- | ---------- | ------ | --------- |
| 1.0.x   | Active  | Feb 2026 | TBD        | TBD    | ✅ Yes    |
| 2.0.x   | Planned | TBD      | TBD        | TBD    | Planned   |
| 3.0.x   | Future  | TBD      | TBD        | TBD    | Future    |

---

## Testing Across Versions

### Version-Specific Tests

```csharp
namespace MAA.Tests.Integration.Versioning;

[Collection("Integration")]
public class VersioningTests
{
    [Theory]
    [InlineData("/api/v1/sessions")]
    public async Task SessionsEndpoint_Available_InVersion(string path)
    {
        var response = await _client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V1Deprecated_ReturnsDeprecationHeader()
    {
        var response = await _client.GetAsync("/api/v1/sessions");
        // Add assertion for deprecation header when v1 is deprecated
        // response.Headers.Should().Contain("Deprecation", "true");
    }
}
```

---

## Related Documentation

- [appsettings.json](../../src/MAA.API/appsettings.json) - Version configuration
- [Program.cs](../../src/MAA.API/Program.cs) - Swagger setup with versioning
- [AUTHENTICATION.md](./AUTHENTICATION.md) - JWT token management (version-independent)
- [quickstart.md](../../specs/003-add-swagger/quickstart.md) - Swagger UI usage

---

## Decision Log

### February 10, 2026

**Decision**: Use semantic versioning with URL path strategy

**Rationale**:

1. Clear, visible versioning (URL path)
2. Easy routing and handler separation
3. Standard approach in RESTful APIs
4. Compatible with CDN caching

**Alternative Rejected**: Header-based versioning (too complex for initial release)

**Revision History**:

- 2026-02-10: Initial document (T063 - Phase 7 implementation)

---

## Contacts & Questions

- **API Team**: [TBD]
- **Deprecation Notices**: Posted in GitHub releases
- **Migration Support**: See CONTRIBUTING.md for contact info

---

**Note**: This document will be updated as versioning requirements evolve. Current API is at 1.0.0 (MVP) with no planned breaking changes for minor versions.
