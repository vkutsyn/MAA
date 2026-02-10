# Phase 0 Research: Add Swagger to API Project

**Purpose**: Resolve technical unknowns identified in spec.md  
**Date**: February 10, 2026  
**Feature**: 003-add-swagger

## Research Tasks Completed

### 1. Technology Stack & Current State

**Decision**: ASP.NET Core 9.0 with Swashbuckle/Swagger (built-in OpenAPI support)

**Rationale**:

- MAA API already uses ASP.NET Core (confirmed in Program.cs examination)
- .NET 9 includes native OpenAPI support via `Microsoft.AspNetCore.OpenApi` package
- Swashbuckle is the industry standard for .NET API documentation
- No external dependencies required; minimal configuration

**Alternatives Considered**:

- NSwag: Alternative OpenAPI code generator, less common than Swashbuckle
- Manual OpenAPI schema: Would violate CONST-II (no automated documentation)
- API versioning tools: Separate from Swagger; not required for MVP

**Implementation Approach**:

- Use Swashbuckle `builder.Services.AddSwaggerGen()` in Program.cs
- Update appsettings to configure Swagger UI exposure in dev/test only
- Add Swashbuckle NuGet package for enhanced Swagger UI features (v6.x for .NET 9)

---

### 2. JWT Authentication Documentation

**Decision**: Implement JWT bearer token support in Swagger UI with authorization button

**Rationale**:

- Program.cs shows JWT authentication already configured
- Authorization header pattern is standard; no custom implementation needed
- Swagger UI "Authorize" button automatically supports bearer tokens via OpenAPI security scheme

**Alternatives Considered**:

- API key authentication: Not used; JWT is implemented
- OAuth2/OIDC: Not required; out of scope for MVP

**Implementation Approach**:

- Add security scheme definition to OpenAPI metadata (bearer JWT)
- Decorate protected endpoints with `[Authorize]` attribute (already present)
- Document authentication in auth controller XML comments

---

### 3. Endpoint Documentation Strategy

**Decision**: Use XML comments on controllers + FluentValidation metadata for automatic schema generation

**Rationale**:

- Swashbuckle automatically extracts XML documentation from methods
- FluentValidation rules (max length, regex, required) display in schema via Swashbuckle.AspNetCore.Filters
- Minimal code changes; documentation lives close to implementation

**Alternatives Considered**:

- Manual OpenAPI attributes: Verbose and unmaintainable; violates CONST-II
- Separate documentation files: Duplicates code; diverges over time

**Implementation Approach**:

- Add `<summary>`, `<param>`, `<returns>` XML comments to all controller methods
- Add response type attributes: `[ProducesResponseType(200)]`, `[ProducesResponseType(400)]`, etc.
- Include example values in response DTOs via attributes

---

### 4. Swagger UI Environment Configuration

**Decision**: Enable Swagger UI in Development and Test environments only; disable in Production via configuration

**Rationale**:

- Production security: Exposing endpoint documentation in production is minor risk but best practice is to limit it
- Development experience: Developers need full UI for discovery
- Testing: Integration tests need schema access for contract validation

**Alternatives Considered**:

- Always-on: Does not reflect typical production API design
- Hardcoded disabled: Reduces flexibility for different deployments

**Implementation Approach**:

- Check `builder.Environment.IsDevelopment()` before mapping Swagger routes
- Add appsettings entry `"Swagger:Enabled": true` for per-environment override
- Generate schema only; UI routing depends on environment check

---

### 5. OpenAPI Schema Version & Format

**Decision**: OpenAPI 3.0 specification (Swashbuckle default for .NET 9)

**Rationale**:

- OpenAPI 3.0 is industry standard (OpenAPI 3.1 exists but 3.0 has broader tooling support)
- Swashbuckle defaults to 3.0; no configuration needed
- Validation tools widely available; passes openapi-generator and swagger-editor validation

**Alternatives Considered**:

- Swagger 2.0 (OpenAPI 2.0): Outdated; deprecated in 2021
- OpenAPI 3.1: Newest; lacks full tooling maturity

**Implementation Approach**:

- Use Swashbuckle defaults; configure in AddSwaggerGen() with appropriate title, version, description

---

### 6. Response Type Documentation

**Decision**: Document all HTTP status codes explicitly (200, 400, 401, 403, 404, 500) per endpoint

**Rationale**:

- Developers need to know all possible outcomes
- Swashbuckle generates schema from `[ProducesResponseType(statusCode)]` attributes
- Spec requires "HTTP status codes... for each endpoint with descriptions"

**Alternatives Considered**:

- Generic responses: Unclear; violates SC-001 (100% coverage requirement)
- Implicit defaults: Swashbuckle infers from return types; explicit is clearer

**Implementation Approach**:

- Add `[ProducesResponseType(200, Type = typeof(ResponseDto))]` to all endpoints
- Create error response DTOs (ValidationErrorDto, UnauthorizedDto, NotFoundDto)
- Document each status code meaning in method's XML comment

---

### 7. API Versioning Plan

**Decision**: Start with single API version (v1.0.0); structure for future multi-version support via routing/middleware

**Rationale**:

- Spec lists "displays API version information" as P3 (nice-to-have, not P1)
- Multi-version APIs add complexity not needed for initial release
- Swashbuckle can support versioning via ApiVersion package if needed later

**Alternatives Considered**:

- Multi-version routes (v1/api/sessions, v2/api/sessions): Premature for MVP
- Header-based versioning: Complex; not needed yet

**Implementation Approach**:

- Display version in Swagger info section (e.g., "v1.0.0")
- Document in appsettings: `"Swagger:ApiVersion": "1.0.0"`
- Plan for `Microsoft.AspNetCore.Mvc.Versioning` package if multi-version needed

---

### 8. Schema Validation & Tooling

**Decision**: Use openapi-generator CLI and swagger-editor for schema validation in CI/CD

**Rationale**:

- SC-006 requires "OpenAPI schema must be valid and pass OpenAPI 3.0 validation with zero schema errors"
- github-actions or CI pipeline can call validators; fails build if schema invalid
- openapi-generator is gold standard for validating schemas

**Alternatives Considered**:

- Manual validation: Not repeatable; violates automation goal
- No validation: Risk of invalid schema breaking client generation

**Implementation Approach**:

- Add build step: `openapi-generator validate -i swagger.json`
- Output Swagger JSON/YAML in build artifacts
- Document in README how to download schema locally

---

## Summary of Technical Decisions

| Decision             | Choice                              | Rationale                                |
| -------------------- | ----------------------------------- | ---------------------------------------- |
| Framework            | ASP.NET Core 9 + Swashbuckle        | Already in use; native OpenAPI support   |
| Authentication       | JWT Bearer Token                    | Already configured; standard OAS support |
| Documentation Method | XML Comments + FluentValidation     | Low-maintenance; auto-generated          |
| Environment Scope    | Dev/Test only (configurable)        | Security best practice                   |
| OpenAPI Version      | 3.0 (Swashbuckle default)           | Industry standard                        |
| Status Codes         | All documented explicitly (200-500) | CONST-II: complete specification         |
| Versioning           | Single version (v1.0.0) for MVP     | Multi-version can be added later         |
| Validation           | CI/CD pipeline validation           | Ensures schema integrity                 |

## Risks & Mitigations

**Risk**: XML comments become outdated if not enforced in code review  
**Mitigation**: Treat missing documentation as code review blocker; add StyleCop/FxCop rules to enforce

**Risk**: Swashbuckle incompatibility with custom domain types (e.g., SessionData complex object)  
**Mitigation**: Test schema generation with complex types during Phase 1; create [SwaggerSchemaFilter] if needed

**Risk**: Performance impact of schema generation on API startup  
**Mitigation**: Schema generation happens at startup in-memory; measure and verify < 5ms overhead (CONST-IV)

**Risk**: JWT expiration in Swagger "Authorize" button may confuse developers  
**Mitigation**: Document in quickstart.md that bearer token supports stateless auth; no session state

---

## Conclusion

All technical clarifications resolved. Ready to proceed to Phase 1: Design (data-model.md, contracts/, quickstart.md).
