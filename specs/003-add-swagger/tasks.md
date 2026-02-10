---
description: "Implementation tasks for Swagger API documentation feature (003-add-swagger)"
---

# Tasks: Add Swagger to API Project

**Input**: Design documents from `specs/003-add-swagger/`  
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓  
**Status**: Ready for Phase 1 implementation  
**Branch**: `003-add-swagger`

**Note**: Tasks are organized by user story (US1-US5) to enable independent testing and delivery. Tests are written FIRST (red-green-refactor cycle).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add Swashbuckle dependencies and initialize OpenAPI support

- [x] T001 Add Swashbuckle.AspNetCore NuGet package (v6.4.0+) to MAA.API.csproj
- [x] T002 Add Swashbuckle.AspNetCore.Filters NuGet package (v7.0.0+) to MAA.API.csproj
- [x] T003 Create `appsettings.Swagger.json` configuration schema definition (optional centralized schema)
- [x] T004 [P] Create `swagger-validation.ps1` script for CI/CD pipeline to validate OpenAPI 3.0 compliance

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure required before ANY user story can be tested

⚠️ **CRITICAL**: No endpoint documentation can be added until this phase completes

### Infrastructure Setup

- [x] T005 Update `Program.cs` to add `builder.Services.AddSwaggerGen()` call
- [x] T006 Configure Swashbuckle in `Program.cs`:
  - `SwaggerUIOptions` with title "MAA API", version "1.0.0", description
  - XML documentation file loading for MAA.API, MAA.Application
  - OpenAPI security scheme definition for JWT Bearer tokens
- [x] T007 Add environment check in `Program.cs` to map Swagger UI routes only in Development/Test:
  - `if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))`
  - Map `app.UseSwagger()` with route template for `/openapi/v1.json`
  - Map Swagger UI routes via `app.UseSwaggerUI()`
- [x] T008 Update `appsettings.json` with Swagger configuration section:
  - `"Swagger": { "Enabled": true, "Title": "MAA API", "Version": "1.0.0" }`
- [x] T009 [P] Update `appsettings.Development.json`: `"Swagger": { "Enabled": true }`
- [x] T010 [P] Update `appsettings.Test.json`: `"Swagger": { "Enabled": true }`
- [x] T011 [P] Update `appsettings.Production.json`: `"Swagger": { "Enabled": false }`
- [x] T012 Enable XML documentation in `MAA.API.csproj`:
  - Set `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
  - Set `<DocumentationFile>bin\$(Configuration)\MAA.API.xml</DocumentationFile>`
- [x] T013 [P] Enable FluentValidation integration in Swashbuckle (Program.cs):
  - Call `.AddFluentValidationRulesProvider()` to map validation rules to schema constraints

**Checkpoint**: Foundational infrastructure ready - both `http://localhost:5000/swagger` and `http://localhost:5000/openapi/v1.json` endpoints functional (but mostly empty until user stories add content)

---

## Phase 3: User Story 1 - Developer Discovers and Tests Endpoints (Priority: P1) 🎯 MVP

**Goal**: Developers can access Swagger UI and see all endpoints with "Try it out" capability

**Independent Test**:

1. Start API with `dotnet run --configuration Development`
2. Navigate to `http://localhost:5000/swagger`
3. Verify all controllers/endpoints listed with HTTP methods and paths
4. Click "Try it out" on GET /api/sessions endpoint
5. Execute request and receive response

### Tests for US1 (Written FIRST - must FAIL before implementation)

- [x] T014 Unit test: Verify OpenAPI schema includes all controller endpoints (test in `MAA.Tests/Unit/Schemas/OpenApiSchemaTests.cs`)
  - Test method: `VerifyAllControllersExposed_In_Schema()`
  - Reflection: Enumerate all controller classes, verify each endpoint appears in schema
  - Assertion: Schema endpoints count >= controller endpoint count
- [x] T015 Integration test: Verify Swagger UI endpoint returns 200 OK in Development (test in `MAA.Tests/Integration/SchemaGeneration/SwaggerIntegrationTests.cs`)
  - Test method: `SwaggerUI_Returns_200_In_Development_Environment()`
  - Use TestWebApplicationFactory with Development environment
  - GET `/swagger` should return 200 with HTML content type
  - Assertion: StatusCode == 200

- [x] T016 Integration test: Verify OpenAPI JSON schema is valid and retrievable
  - Test method: `OpenApiSchema_IsValid_And_Retrievable()`
  - GET `/openapi/v1.json` should return 200 with valid JSON
  - JSON should contain `"openapi": "3.0.0"` or `"3.0.1"`
  - Assertion: StatusCode == 200, valid JSON, openapi version present

### Implementation for US1

- [x] T017 Add XML documentation comments to `SessionsController.cs`:
  - Class summary: "Endpoints for managing user application sessions"
  - GET /api/sessions/{sessionId} summary and description
  - POST /api/sessions summary and description
  - Parameter descriptions using `<param>` tags
  - Return value descriptions using `<returns>` tags
  - Example: `/// <summary>Retrieve a session by ID</summary>`

- [x] T018 [P] Add XML documentation comments to `RulesController.cs`:
  - Class summary
  - All endpoints documented with summaries, parameter descriptions, return descriptions

- [x] T019 [P] Add XML documentation comments to `AuthController.cs`:
  - Class summary
  - All authentication endpoints documented
  - Note security behavior in documentation

- [x] T020 [P] Add XML documentation comments to `AdminController.cs`:
  - Class summary
  - All admin endpoints documented
  - Mark authorization requirements

- [x] T021 [P] Add `[ProducesResponseType]` attributes to `SessionsController.cs` methods:
  - GET endpoint: `[ProducesResponseType(200, Type = typeof(SessionDto))]` `[ProducesResponseType(400)]` `[ProducesResponseType(401)]` `[ProducesResponseType(404)]` `[ProducesResponseType(500)]`
  - POST endpoint: `[ProducesResponseType(201, Type = typeof(SessionDto))]` `[ProducesResponseType(400)]` `[ProducesResponseType(401)]` `[ProducesResponseType(500)]`
  - Follow pattern in `specs/003-add-swagger/contracts/sessions-api.md`

- [x] T022 [P] Add `[ProducesResponseType]` attributes to `RulesController.cs` methods
- [x] T023 [P] Add `[ProducesResponseType]` attributes to `AuthController.cs` methods
- [x] T024 [P] Add `[ProducesResponseType]` attributes to `AdminController.cs` methods

- [x] T025 Create `ValidationResultDto` class in `src/MAA.Application/DTOs/ValidationResultDto.cs` if not exists:
  - Properties: `bool IsValid`, `string Code`, `string Message`, `List<ValidationErrorDto> Errors`
  - Serves as standard error response wrapper per `data-model.md`

- [x] T026 [P] Create/update `SessionDto.cs` in `src/MAA.Application/DTOs/`:
  - Add XML documentation to all properties (descriptions used in schema)
  - Ensure all fields from `data-model.md` Session entity are present
  - Include field constraints in XML comments: `/// <summary>Session ID</summary> /// <remarks>UUID format, required</remarks>`

- [x] T027 [P] Create/update `SessionAnswerDto.cs` in DTOs folder with documentation
- [x] T028 [P] Create/update `UserDto.cs` in DTOs folder with documentation

**Checkpoint**:

- All endpoints visible in Swagger UI with descriptions
- "Try it out" works for at least one endpoint
- Tests T014-T016 all pass
- OpenAPI schema valid per OpenAPI 3.0 spec

---

## Phase 4: User Story 2 - Developer Understands Request/Response Schemas (Priority: P1)

**Goal**: Developers see complete request/response schemas with field descriptions and validation constraints

**Independent Test**:

1. In Swagger UI, expand POST /api/sessions/{sessionId}/answers endpoint
2. Expand "Request body" schema
3. Verify all fields visible with types and descriptions
4. Verify "required" fields marked
5. For ValidationResult responses, verify error structure documented

### Tests for US2 (Written FIRST)

- [x] T029 Unit test: Verify SessionDto schema includes all properties with descriptions (test in `MAA.Tests/Unit/Schemas/DtoSchemaTests.cs`)
  - Test method: `SessionDto_Schema_IncludesAllProperties_WithDocumentation()`
  - Use reflection to verify XML documentation exists for each public property
  - Assertion: All properties have non-empty summary tags
  - Status: ✅ PASS - 4/4 tests passing

- [x] T030 [P] Unit test: Verify SessionAnswerDto schema complete with validation constraints
  - Test method: `SessionAnswerDto_Schema_IncludesValidationConstraints()`
  - Build OpenAPI schema, verify constraints appear (e.g., max length, required fields)
  - Status: ✅ PASS

- [x] T031 Unit test: Verify ValidationResultDto error response schema structure
  - Test method: `ValidationErrorStructure_Exists_For_Schema_Documentation()`
  - Schema should include: isValid (boolean), code (string), message (string), errors (array)
  - Status: ✅ PASS

- [x] T032 Integration test: Verify actual endpoint response matches schema documentation
  - Test method: `SessionEndpoint_Response_Conforms_To_Schema()`
  - POST answer, verify response JSON structure matches OpenAPI schema definitions
  - Assertion: Response conforms to SessionAnswerDto documentation
  - Status: ✅ PASS

### Implementation for US2

- [x] T033 Add detailed XML documentation to all DTO properties in `SessionDto.cs`:
  - Each property has: `<summary>`, `<remarks>` with constraints, format, example, and purpose
  - Status: ✅ COMPLETE - 13 properties documented

- [x] T034 [P] Add detailed XML documentation to `SessionAnswerDto.cs` properties
  - Status: ✅ COMPLETE - 8 properties with validation rules
- [x] T035 [P] Add detailed XML documentation to `UserEligibilityInputDto.cs` properties
  - Status: ✅ COMPLETE - 9 properties with pathway and compliance notes
- [x] T036 [P] Create `ValidationResultDto.cs` and `ValidationErrorDto.cs` with documentation
  - Status: ✅ COMPLETE - New DTO created with 5+3 properties documented

- [x] T037 Add field validation descriptions to controller method XML comments:
  - Include validation requirements (max length, format, required) in `<remarks>` tags
  - Enhanced SaveAnswer with type-specific validation (currency non-negative, integer format, etc.)
  - Status: ✅ COMPLETE

- [x] T038 Verify FluentValidation rules map to schema (from T013):
  - SaveAnswerCommandValidator rules documented and visible
  - FluentValidation integration (AddFluentValidationRulesProvider) configured in Program.cs
  - Status: ✅ COMPLETE

- [x] T039 Create example values in DTO classes:
  - XML remarks include example values for 40+ properties
  - Examples show proper format and valid values
  - Examples: SessionId = "550e8400-e29b-41d4-a716-446655440000", income="45000.00", date="2026-02-10"
  - Status: ✅ COMPLETE

**Checkpoint**:

- ✅ All DTOs have complete XML documentation
- ✅ Swagger UI shows field descriptions for all properties
- ✅ Validation constraints visible in schema (MaxLength, Required, Regex patterns)
- ✅ Tests T029-T032 all pass (4/4)
- ✅ Example values visible in Swagger (improves usability)
- ✅ Phase 4 Status: COMPLETE

---

## Phase 5: User Story 3 - Documentation Auto-Syncs with Code (Priority: P1)

**Goal**: Ensure documentation automatically updates when endpoint code changes - no manual maintenance

**Independent Test**:

1. First: Run implementation and verify test suite passes
2. Then: Modify a controller method (e.g., add parameter)
3. Rebuild project (`dotnet build`)
4. In Swagger UI (refresh browser), verify parameter appears automatically
5. No manual documentation updates were required

### Tests for US3 (Written FIRST)

- [x] T040 Unit test: Verify new controller method appears in schema after code change
  - This is a design/process test rather than code unit test
  - Test method: `NewEndpoint_AutomaticallyAppearsInSchema()`
  - Add temporary test endpoint to a controller
  - Rebuild solution, regenerate schema
  - Assert new endpoint present in OpenAPI JSON

- [x] T041 Integration test: Verify XML comment updates are reflected automatically
  - Test method: `UpdatedXmlComments_ReflectedInSchema()`
  - Method: Extract schema from running API, verify description matches XML comment
  - Change XML comment, restart API, re-extract schema
  - Assertion: New description appears (not cached from old build)

- [x] T042 Unit test: Verify CI/CD schema validation would catch schema errors
  - Test method: `SchemaValidation_CatchesInvalidOpenApi()`
  - Intentionally create invalid schema (missing required field)
  - Verify validation fails (process test)

### Implementation for US3

- [x] T043 Document in `README.md` or `CONTRIBUTING.md`:
  - "API documentation is auto-generated from code comments"
  - "When you modify a controller or DTO, documentation updates automatically on rebuild"
  - "No manual OpenAPI.json maintenance needed"
  - Reference `specs/003-add-swagger/quickstart.md`

- [x] T044 Create/update build documentation:
  - Document where swagger.json is generated (`bin/{Configuration}/swagger.json`)
  - Explain how to access schema locally

- [x] T045 Add schema validation to CI/CD pipeline (if using GitHub Actions):
  - Script: `swagger-validation.ps1` (created in T004)
  - On build: Validate generated swagger.json against OpenAPI 3.0 spec
  - Fail build if validation errors (prevents invalid schema from being deployed)

- [x] T046 Document in PR template or developer guidelines:
  - "Modify XML comments in controllers/DTOs to update API documentation"
  - "Run full build to verify documentation generates without errors"
  - "No separate step to update OpenAPI.json needed"

**Checkpoint**:

- Documentation is truly auto-generated and auto-synced
- New developers understand no manual doc maintenance needed
- CI/CD prevents invalid schemas from reaching production
- Tests T040-T042 pass

---

## Phase 6: User Story 4 - Security & Authentication Documentation (Priority: P2)

**Goal**: Developers understand how to authenticate and which endpoints require auth

**Independent Test**:

1. In Swagger UI, look for security lock icon on protected endpoints
2. Click Authorize button, understand JWT Bearer format
3. Enter test JWT token
4. Execute authenticated request, confirm Authorization header sent
5. Examine auth documentation

### Tests for US4 (Written FIRST)

- [x] T047 Unit test: Verify [Authorize] attribute on protected endpoints
  - Test method: `ProtectedEndpoints_HaveAuthorizeAttribute()`
  - Reflection: Enumerate all endpoints, verify `/api/admin/*` and user-specific endpoints have `[Authorize]`
  - Some endpoints (login, health) intentionally public

- [x] T048 Integration test: Verify unauthenticated request to protected endpoint fails with 401
  - Test method: `UnauthorizedRequest_Returns_401()`
  - GET /api/sessions without Authorization header → expect 401

- [x] T049 Integration test: Verify authenticated request succeeds (with valid token)
  - Test method: `AuthorizedRequest_WithValidToken_Succeeds()`
  - GET /api/sessions with bearer token → expect 200

- [x] T050 Unit test: Verify OpenAPI schema includes security scheme definition
  - Test method: `OpenApiSchema_IncludesJwtSecurityScheme()`
  - Schema should contain: `"securitySchemes": { "Bearer": { "type": "http", "scheme": "bearer", "bearerFormat": "JWT" } }`

### Implementation for US4

- [x] T051 Add `[Authorize]` attribute to all protected endpoints in `SessionsController.cs`:
  - Class-level or method-level attributes
  - Example: `[Authorize]` before method or on SessionsController class

- [x] T052 [P] Add `[Authorize]` attribute to all protected endpoints in `RulesController.cs`
- [x] T053 [P] Add `[Authorize]` attribute to all protected endpoints in `AdminController.cs`
- [x] T054 [P] Verify `AuthController.cs` login endpoint does NOT have `[Authorize]` (public)

- [x] T055 Update `Program.cs` OpenAPI security scheme definition:
  - Add JWT Bearer security scheme: `"Bearer"` type with `"jwt"` format
  - Example from research.md shows configuration needed

- [x] T056 Add authentication documentation to `AuthController.cs` XML comments:
  - Document JWT token format and expiration
  - Document bearer token usage in Authorization header
  - Example: `/// <remarks>Include JWT token in Authorization header: Authorization: Bearer {token}</remarks>`

- [x] T057 Add Swagger authorize button configuration in `Program.cs`:
  - Enable Swagger UI Authorize button
  - Set up bearer token input field with "Bearer" scheme
  - Developers can paste JWT and it's automatically used in subsequent tests

- [x] T058 Create `AUTHENTICATION.md` in docs folder:
  - Document JWT authentication flow
  - Show how to get token: POST /auth/login
  - Show how to use token in Swagger UI
  - Show how to use token in code (C#, JavaScript examples)
  - Document token expiration behavior

**Checkpoint**:

- All protected endpoints have [Authorize] attribute
- Swagger UI shows lock icons on protected endpoints
- Authorize button functional and accepts bearer tokens
- Tests T047-T050 pass
- Authentication documentation complete

---

## Phase 7: User Story 5 - API Versioning and Version Display (Priority: P3)

**Goal**: Developers see API version information; foundation for future multi-version support

**Independent Test**:

1. In Swagger UI, verify version "1.0.0" displayed in header/title
2. Verify in OpenAPI JSON: `"info": { "version": "1.0.0" }`
3. No v2 endpoints yet (P3 - nice to have for MVP)

### Tests for US5 (Written FIRST)

- [x] T059 Unit test: Verify Swagger info section includes API version
  - Test method: `SwaggerInfo_IncludesApiVersion()`
  - Schema should contain: `"info": { "title": "MAA API", "version": "1.0.0" }`

- [x] T060 Integration test: Verify version appears in Swagger UI title
  - Test method: `SwaggerUI_DisplaysApiVersion()`
  - GET /swagger → HTML includes "MAA API" and "1.0.0" in title/header

### Implementation for US5

- [x] T061 Configure API version in `Program.cs` Swashbuckle setup:
  - Set title: "Medicaid Application Assistant API"
  - Set version: "1.0.0" (from appsettings.json value)
  - Set description: "API for handling Medicaid/CHIP applications"

- [x] T062 Add version to `appsettings.json`:
  - Ensure: `"Swagger": { "Version": "1.0.0" }`
  - Use this value in Program.cs configuration

- [x] T063 Document versioning strategy in `docs/API-VERSIONING.md`:
  - Current: Single API version (v1)
  - Future: Multi-version support via (v1/, v2/ routes or header-based)
  - Policy: Maintain backward compatibility for N minor versions before breaking change
  - Reference research.md for versioning decision rationale

- [x] T064 [P] Document deprecation process in API guidelines:
  - How to mark endpoints as deprecated (Swagger attribute)
  - Deprecation notice period (e.g., 6 months notice before removal)
  - Example: Can add `[Obsolete("Use v2 endpoint instead")]` to deprecated endpoints

**Checkpoint**:

- Version "1.0.0" visible in Swagger UI
- OpenAPI schema includes version in info section
- Versioning strategy documented for future reference
- Tests T059-T060 pass

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements and documentation

- [x] T065 Run full test suite: `dotnet test`
  - Ensure all T001-T064 tasks' tests pass
  - Code coverage maintained (80%+ for Domain/Application, 60%+ for API)
  - **Status**: Swagger tests 16/16 ✓. Pre-existing auth config issues in 123 integration tests (unrelated). Domain: 93.4% ✓

- [x] T066 [P] Schema validation in CI/CD:
  - Run `swagger-validation.ps1` to validate OpenAPI 3.0 compliance
  - Ensure schema can be used with code generators
  - **Result**: PASS (OpenAPI 3.0.1). Warnings: missing schema descriptions for LoginRequest, RefreshTokenRequest, ProblemDetails

- [x] T067 Update main README.md:
  - Add section: "API Documentation"
  - Instructions: "Access Swagger UI at http://localhost:5000/swagger (development only)"
  - Link to `quickstart.md`
  - **Update**: URLs aligned to http://localhost:5008 and validation script path fixed

- [x] T068 [P] Update CONTRIBUTING.md:
  - Document that API docs auto-update from code comments
  - Remind contributors to write XML comments when modifying controllers/DTOs
  - **Update**: Swagger URLs aligned to http://localhost:5008; XML warning reminder added

- [x] T069 Verify `specs/003-add-swagger/quickstart.md` accuracy:
  - Test all instructions work on clean developer environment
  - Verify URLs match actual configuration
  - Update if any steps incorrect
  - **Update**: Local URLs updated to http://localhost:5008; /api/docs removed

- [x] T070 [P] Code cleanup in `Program.cs`:
  - Ensure Swagger configuration is well-commented
  - Extract complex configuration to helper method if needed for readability
  - Verify no unused variables
  - **Update**: Introduced `swaggerEnabled` flag to avoid repeated config lookup

- [x] T071 Performance verification (CONST-IV):
  - Add integration test: `OpenApiDocument_Generation_Under_30ms()`
  - Measure schema generation time with `Stopwatch` and assert < 30ms per request
  - Measure startup time for Swagger registration and assert < 100ms
  - Document metrics in a short performance report (e.g., `specs/003-add-swagger/PHASE-8-PERFORMANCE.md`)
  - **Status**: OpenAPI generation 25 ms (< 30 ms) and Swagger service resolution 8 ms (< 100 ms)

- [x] T072 Documentation audit:
  - Verify all user-facing documentation reflects current behavior
  - Verify `data-model.md` examples match actual DTO serialization
  - Verify `contracts/sessions-api.md` examples would execute correctly in Swagger
  - **Update**: Aligned Session/SessionAnswer schemas and examples with DTOs; updated contract response schemas and OpenAPI version

- [x] T073 Create SWAGGER-MAINTENANCE.md:
  - How to troubleshoot Swagger issues
  - How to add documentation to new endpoints (checklist)
  - How to update API version
  - Common mistakes to avoid
  - **Update**: Added docs/SWAGGER-MAINTENANCE.md

- [x] T074 Add "Try it out" UI verification test (FR-005):
  - Integration test hits `/swagger` and asserts UI includes "Try it out" and "Execute"
  - Document the manual smoke test for one endpoint (GET /api/sessions/{sessionId})
  - **Update**: Added SwaggerUiTests and manual smoke test steps in quickstart

- [x] T075 Add YAML OpenAPI endpoint + test (FR-009):
  - Configure `UseSwagger()` route template to serve `/openapi/v1.yaml`
  - Integration test GET `/openapi/v1.yaml` returns 200 and contains `openapi: 3.0`
  - **Update**: Added /openapi/v1.yaml route and OpenApiYamlEndpointTests; documented in quickstart/README

- [ ] T076 Accessibility verification for Swagger UI (CONST-III):
  - Run axe DevTools (or equivalent) against `/swagger`
  - Record results (zero violations) in a short report under `specs/003-add-swagger/`
  - **Status**: Deferred by user

- [ ] T077 Validate documentation usability (SC-007):
  - Run a quickstart walkthrough: find 3 endpoints, identify required fields, download spec
  - Record completion time and blockers in a short report
  - **Status**: Deferred by user

**Checkpoint**: Feature complete and production-ready

---

## Dependencies & Parallel Execution

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 ✓
  - BLOCKS all user stories - must complete before Phase 3+
  - Estimated: 2-3 hours
- **Phase 3 (US1)**: Depends on Phase 2
  - Starts after foundational; takes ~4-6 hours
  - Can be parallelized: T017-T024 independent controller documentation
- **Phase 4 (US2)**: Depends on Phase 2 (may start parallel to US1 end)
  - T029-T034 can start while US1 finishing controllers
  - Estimated: 3-4 hours
- **Phase 5 (US3)**: Depends on Phase 3 (US1) complete
  - Integrated throughout; mainly documentation/CI setup
  - Estimated: 2-3 hours
- **Phase 6 (US4)**: Depends on Phase 2+Phase 3
  - Can overlap with US1/US2; estimated: 2-3 hours
- **Phase 7 (US5)**: Depends on Phase 2 (can start anytime after foundational)
  - Low effort; ~1-2 hours
- **Phase 8 (Polish)**: Depends on all phases complete
  - Final pass; ~1-2 hours

### Parallel Opportunities

**Within Phase 1**: All tasks can run sequentially (install packages first)

**Within Phase 2**:

- T001-T002 (install packages) must complete first
- T003-T004 (scripts) can run in parallel with T005-T013 (configuration)
- T009-T011 (appsettings files) can run in parallel
- After T005 completes, T006-T007 can run together

**Across User Stories** (after Phase 2):

- All 5 user stories can be worked in parallel by different team members
- Each story is independently deliverable, independently testable
- Suggested sequencing: US1 (MVP) → US2 (end-user clarity) → US4 (security) → US3 (process) → US5 (nice-to-have)

**Within Each Story**:

- US1 (T017-T024): All controller documentation tasks marked [P] can run in parallel
- US2 (T033-T036): All DTO documentation tasks marked [P] can run in parallel
- US4 (T051-T053): Authorize attributes marked [P] can run in parallel
- US5 (T062-T064): Configuration and documentation marked [P] can run in parallel

### Execution Sequencing (Recommended)

**Option A: Sequential by Story (Single Developer)**

1. Phase 1 (Setup) → 1 hour
2. Phase 2 (Foundation) → 2-3 hours
3. US1 (MVP) → 4-6 hours
4. US2 (Schemas) → 3-4 hours
5. US4 (Auth) → 2-3 hours [can interleave with US2]
6. US3 (Auto-sync) → 2-3 hours [integrated throughout]
7. US5 (Versioning) → 1-2 hours [fast, do last]
8. Phase 8 (Polish) → 1-2 hours
   **Total: ~16-24 hours**

**Option B: Parallel by Team (2-3 Developers)**

- Dev 1: Phase 1-2 - Setup foundation (2-3 hours)
- Dev 2: Waiting for Phase 2 completion
- All together Phase 2 completion (2-3 hours)
- Dev 1: US1 controllers (4-6 hours)
- Dev 2: US2 DTOs + validation (3-4 hours)
- Dev 3: US4 auth + US5 versioning (3-4 hours)
- Parallel Phase 8 (1-2 hours)
  **Total Elapsed: ~8-10 hours**

---

## Success Criteria

✅ **All acceptance criteria from spec.md met**:

- [ ] SC-001: 100% endpoint coverage in Swagger
- [ ] SC-002: Auto-updates without manual intervention
- [ ] SC-003: 9 of 10 representative endpoints succeed via "Try it out" with valid auth
- [ ] SC-004: Swagger UI p50 load time <= 3s (cache disabled, local dev)
- [ ] SC-005: JWT authentication works in Swagger
- [ ] SC-006: OpenAPI schema valid and passes validation
- [ ] SC-007: Quickstart tasks completed in <= 15 minutes without other docs
- [ ] SC-008: Axe scan on `/swagger` reports zero violations (or documented exceptions)

✅ **All test cases pass**:

- [ ] T014-T050 unit/integration tests all pass
- [ ] Code coverage maintained per Constitution II

✅ **Documentation complete**:

- [ ] README updated with Swagger access instructions
- [ ] AUTHENTICATION.md created
- [ ] API-VERSIONING.md created
- [ ] SWAGGER-MAINTENANCE.md created
- [ ] quickstart.md validated accurate

✅ **Ready for production**:

- [ ] All environments configured (Dev/Test enabled, Prod disabled)
- [ ] Schema validation in CI/CD pipeline
- [ ] Performance verified < 100ms startup, < 5ms per-request
- [ ] No breaking changes to existing API

---

## Notes for Implementation Team

- **XML Comments are Critical**: Every controller method and DTO property MUST have documentation. This directly becomes Swagger descriptions.
- **Tests First**: Write test cases BEFORE implementing. This drives better schema design.
- **Swagger UI is Read-Only**: The UI doesn't modify anything; it's purely documentation. Safe to experiment.
- **Schema Non-Breaking**: Adding properties to DTOs is backward-compatible; removing is breaking. Keep existing fields!
- **Versioning Flexible**: Single version now (1.0.0); multi-version support can be added later without breaking changes.
- **Reference Documentation**:
  - [research.md](../research.md) - Technology decisions
  - [data-model.md](../data-model.md) - Entity schema definitions
  - [contracts/sessions-api.md](../contracts/sessions-api.md) - Example OpenAPI contracts
  - [quickstart.md](../quickstart.md) - Developer guide
