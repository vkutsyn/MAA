# Swagger Maintenance Guide

This guide explains how to keep Swagger/OpenAPI documentation accurate and reliable.

## Quick Links

- Swagger UI: http://localhost:5008/swagger
- OpenAPI JSON: http://localhost:5008/openapi/v1.json
- Validation script: `./swagger-validation.ps1`

## Checklist: Add or Update an Endpoint

1. Add XML comments to the controller action and DTOs.
2. Add `[ProducesResponseType]` attributes for all expected status codes.
3. Ensure the endpoint is public and has an HTTP verb attribute (`[HttpGet]`, `[HttpPost]`, etc.).
4. If the endpoint requires auth, add `[Authorize]`.
5. Run `dotnet build` to regenerate XML docs.
6. Validate the schema: `./swagger-validation.ps1`.

## Troubleshooting

### Swagger UI not loading

- Confirm the API is running in Development or Test.
- Check `appsettings.{Environment}.json` for `"Swagger": { "Enabled": true }`.
- Verify `options.RouteTemplate = "openapi/{documentName}.json"` and `options.RoutePrefix = "swagger"` in Program.cs.

### Endpoint missing from Swagger

- Ensure `[ApiController]` and a route are present on the controller.
- Ensure action methods are public and not generic.
- Confirm XML comments are valid and compile without CS1570 warnings.

### Schema validation warnings

- Add or fix XML comments for DTO properties.
- Ensure response types are explicit and documented.
- Re-run `./swagger-validation.ps1` to verify.

## Updating API Version

1. Update `appsettings.json`:
   - `Swagger:Version`
   - `Swagger:Title` (if needed)
2. Update the Swagger doc registration in Program.cs:
   - `options.SwaggerDoc("v1", new OpenApiInfo { Version = "<new>" })`
3. Confirm the UI endpoint and OpenAPI JSON still load.

## Common Mistakes

- Invalid XML comments (CS1570 warnings).
- Missing `[ProducesResponseType]` attributes.
- Swagger disabled in environment settings.
- Incorrect route prefix or OpenAPI route template.
- Unhandled DTO changes without updated XML comments.

## Endpoint Notes

- Question definitions: ensure `GET /api/questions/{stateCode}/{programCode}` documents `GetQuestionsResponse` with `conditionalRules` and `options` populated per the OpenAPI contract in specs/008-question-definitions-api/contracts/questions-api.openapi.yaml.
