<#
.SYNOPSIS
    Validates OpenAPI/Swagger schema for compliance with OpenAPI 3.0 specification.

.DESCRIPTION
    This script validates the generated swagger.json file to ensure it conforms
    to the OpenAPI 3.0 specification. It checks for:
    - Valid JSON structure
    - Required OpenAPI fields (openapi, info, paths)
    - Schema version compliance (3.0.x)
    - Proper endpoint documentation
    - Security scheme definitions (if authentication is used)

    Intended for CI/CD pipeline integration to catch schema errors before deployment.

.PARAMETER SchemaPath
    Path to the swagger.json file to validate.
    Default: src/MAA.API/bin/Debug/net10.0/swagger.json

.PARAMETER Strict
    Enable strict validation mode (fail on warnings).
    Default: false

.PARAMETER OutputFormat
    Output format for validation results: Text, Json, or Xml.
    Default: Text

.EXAMPLE
    .\swagger-validation.ps1
    Validates the default swagger.json file.

.EXAMPLE
    .\swagger-validation.ps1 -SchemaPath "src/MAA.API/bin/Release/net10.0/swagger.json" -Strict
    Validates release build schema with strict mode enabled.

.EXAMPLE
    .\swagger-validation.ps1 -OutputFormat Json
    Validates and outputs results in JSON format for CI/CD parsing.

.NOTES
    Author: MAA Development Team
    Created: February 10, 2026
    Feature: 003-add-swagger (Phase 5 - T045: CI/CD schema validation)
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$SchemaPath = "src/MAA.API/bin/Debug/net10.0/swagger.json",

    [Parameter(Mandatory = $false)]
    [switch]$Strict = $false,

    [Parameter(Mandatory = $false)]
    [ValidateSet("Text", "Json", "Xml")]
    [string]$OutputFormat = "Text"
)

# Script configuration
$ErrorActionPreference = "Stop"
$validationErrors = @()
$validationWarnings = @()
$validationInfo = @()

# Helper function: Add validation error
function Add-ValidationError {
    param([string]$message)
    $script:validationErrors += $message
    Write-Host "[ERROR] $message" -ForegroundColor Red
}

# Helper function: Add validation warning
function Add-ValidationWarning {
    param([string]$message)
    $script:validationWarnings += $message
    Write-Host "[WARNING] $message" -ForegroundColor Yellow
}

# Helper function: Add validation info
function Add-ValidationInfo {
    param([string]$message)
    $script:validationInfo += $message
    Write-Host "[INFO] $message" -ForegroundColor Cyan
}

# Main validation logic
function Test-SwaggerSchema {
    Write-Host "`n===== OpenAPI Schema Validation =====" -ForegroundColor Green
    Write-Host "" -ForegroundColor Gray

    # Step 1: Check file exists
    if (-not (Test-Path $SchemaPath)) {
        Add-ValidationError "Swagger schema file not found at: $SchemaPath"
        Add-ValidationInfo "Ensure API project is built: dotnet build src/MAA.API/MAA.API.csproj"
        return $false
    }

    Add-ValidationInfo "Schema file found: $SchemaPath"
    Add-ValidationInfo "File size: $((Get-Item $SchemaPath).Length) bytes"

    # Step 2: Parse JSON
    try {
        $schema = Get-Content $SchemaPath -Raw | ConvertFrom-Json
        Add-ValidationInfo "JSON structure valid"
    }
    catch {
        Add-ValidationError "Invalid JSON structure: $($_.Exception.Message)"
        return $false
    }

    # Step 3: Validate OpenAPI version
    if (-not $schema.openapi) {
        Add-ValidationError "Missing 'openapi' field (required)"
    }
    elseif (-not $schema.openapi.StartsWith("3.0")) {
        Add-ValidationWarning "OpenAPI version is $($schema.openapi). Expected 3.0.x for broadest compatibility."
    }
    else {
        Add-ValidationInfo "OpenAPI version: $($schema.openapi)"
    }

    # Step 4: Validate info section
    if (-not $schema.info) {
        Add-ValidationError "Missing 'info' section (required)"
    }
    else {
        if (-not $schema.info.title) {
            Add-ValidationError "Missing 'info.title' field (required)"
        }
        else {
            Add-ValidationInfo "API Title: $($schema.info.title)"
        }

        if (-not $schema.info.version) {
            Add-ValidationError "Missing 'info.version' field (required)"
        }
        else {
            Add-ValidationInfo "API Version: $($schema.info.version)"
        }

        if (-not $schema.info.description) {
            Add-ValidationWarning "Missing 'info.description' (recommended for documentation)"
        }
    }

    # Step 5: Validate paths section
    if (-not $schema.paths) {
        Add-ValidationError "Missing 'paths' section (required - no endpoints documented)"
    }
    else {
        $endpointCount = ($schema.paths.PSObject.Properties | Measure-Object).Count
        
        if ($endpointCount -eq 0) {
            Add-ValidationError "No endpoints defined in 'paths' section"
        }
        else {
            Add-ValidationInfo "Total endpoints documented: $endpointCount"

            # Check for undocumented endpoints
            $undocumentedCount = 0
            foreach ($path in $schema.paths.PSObject.Properties) {
                foreach ($method in $path.Value.PSObject.Properties) {
                    if (-not $method.Value.summary -and -not $method.Value.description) {
                        $undocumentedCount++
                        Add-ValidationWarning "Endpoint $($path.Name) [$($method.Name.ToUpper())] has no summary or description"
                    }
                }
            }

            if ($undocumentedCount -eq 0) {
                Add-ValidationInfo "All endpoints have documentation"
            }
            else {
                Add-ValidationWarning "$undocumentedCount endpoint(s) lack documentation"
            }
        }
    }

    # Step 6: Validate components/schemas section
    if ($schema.components -and $schema.components.schemas) {
        $schemaCount = ($schema.components.schemas.PSObject.Properties | Measure-Object).Count
        Add-ValidationInfo "Total schemas defined: $schemaCount"

        # Check for schemas without descriptions
        $undocumentedSchemaCount = 0
        foreach ($schemaItem in $schema.components.schemas.PSObject.Properties) {
            if (-not $schemaItem.Value.description) {
                $undocumentedSchemaCount++
                Add-ValidationWarning "Schema '$($schemaItem.Name)' has no description"
            }
        }

        if ($undocumentedSchemaCount -eq 0) {
            Add-ValidationInfo "All schemas have descriptions"
        }
    }
    else {
        Add-ValidationWarning "No 'components.schemas' section found (DTOs may not be documented)"
    }

    # Step 7: Validate security schemes (if authentication is used)
    if ($schema.components -and $schema.components.securitySchemes) {
        $securityCount = ($schema.components.securitySchemes.PSObject.Properties | Measure-Object).Count
        Add-ValidationInfo "Security schemes defined: $securityCount"

        foreach ($securityScheme in $schema.components.securitySchemes.PSObject.Properties) {
            Add-ValidationInfo "  - $($securityScheme.Name): $($securityScheme.Value.type)"
        }
    }
    else {
        Add-ValidationInfo "No security schemes defined (API may be public or authentication not documented)"
    }

    # Step 8: Check for common issues
    # Issue: No response types documented
    $endpointsWithoutResponses = 0
    if ($schema.paths) {
        foreach ($path in $schema.paths.PSObject.Properties) {
            foreach ($method in $path.Value.PSObject.Properties) {
                if (-not $method.Value.responses) {
                    $endpointsWithoutResponses++
                    Add-ValidationWarning "Endpoint $($path.Name) [$($method.Name.ToUpper())] has no responses documented"
                }
            }
        }
    }

    if ($endpointsWithoutResponses -eq 0 -and $schema.paths) {
        Add-ValidationInfo "All endpoints have response types documented"
    }

    # Final result
    Write-Host "`n=====================================" -ForegroundColor Gray
    Write-Host "Validation Summary" -ForegroundColor Green
    Write-Host "  Errors:   $($validationErrors.Count)" -ForegroundColor $(if ($validationErrors.Count -eq 0) { "Green" } else { "Red" })
    Write-Host "  Warnings: $($validationWarnings.Count)" -ForegroundColor $(if ($validationWarnings.Count -eq 0) { "Green" } else { "Yellow" })
    Write-Host "  Info:     $($validationInfo.Count)" -ForegroundColor Cyan

    # Determine pass/fail
    $hasErrors = $validationErrors.Count -gt 0
    $hasWarnings = $validationWarnings.Count -gt 0

    if ($hasErrors) {
        Write-Host "`n[FAIL] VALIDATION FAILED" -ForegroundColor Red
        Write-Host "Schema has critical errors. Fix errors before deploying." -ForegroundColor Red
        return $false
    }
    elseif ($Strict -and $hasWarnings) {
        Write-Host "`n[FAIL] VALIDATION FAILED (Strict Mode)" -ForegroundColor Yellow
        Write-Host "Schema has warnings. Strict mode requires no warnings." -ForegroundColor Yellow
        return $false
    }
    else {
        Write-Host "`n[PASS] VALIDATION PASSED" -ForegroundColor Green
        if ($hasWarnings) {
            Write-Host "Schema is valid but has warnings. Consider addressing warnings for better documentation." -ForegroundColor Yellow
        }
        else {
            Write-Host "Schema is valid and complete." -ForegroundColor Green
        }
        return $true
    }
}

# Format output based on OutputFormat parameter
function Format-ValidationOutput {
    param([bool]$isValid)

    if ($OutputFormat -eq "Json") {
        $result = @{
            isValid  = $isValid
            errors   = $validationErrors
            warnings = $validationWarnings
            info     = $validationInfo
        } | ConvertTo-Json -Depth 5

        Write-Output $result
    }
    elseif ($OutputFormat -eq "Xml") {
        $xmlHeader = "<?xml version='1.0' encoding='UTF-8'?>"
        $xmlRoot = "<ValidationResult>"
        $xmlIsValid = "  <IsValid>$isValid</IsValid>"
        $xmlErrorsOpen = "  <Errors count='$($validationErrors.Count)'>"
        
        Write-Output $xmlHeader
        Write-Output $xmlRoot
        Write-Output $xmlIsValid
        Write-Output $xmlErrorsOpen
        
        foreach ($error in $validationErrors) {
            $escapedError = [System.Security.SecurityElement]::Escape($error)
            Write-Output "    <Error>$escapedError</Error>"
        }
        
        Write-Output "  </Errors>"
        Write-Output "  <Warnings count='$($validationWarnings.Count)'>"
        
        foreach ($warning in $validationWarnings) {
            $escapedWarning = [System.Security.SecurityElement]::Escape($warning)
            Write-Output "    <Warning>$escapedWarning</Warning>"
        }
        
        Write-Output "  </Warnings>"
        Write-Output "</ValidationResult>"
    }
    # Text format already printed during validation
}

# Execute validation
try {
    $isValid = Test-SwaggerSchema
    Format-ValidationOutput -isValid $isValid

    # Exit with appropriate code for CI/CD
    if ($isValid) {
        exit 0
    }
    else {
        exit 1
    }
}
catch {
    Write-Host "`nValidation script error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    exit 2
}
