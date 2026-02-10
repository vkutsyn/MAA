# Environment Configuration Guide

**Feature**: 006-state-context-init  
**Date**: February 10, 2026  
**Purpose**: Production deployment configuration for State Context Initialization feature

## Overview

This document describes the environment variables and configuration settings required for deploying the State Context Initialization feature to production.

---

## Backend Configuration (MAA.API)

### Required Environment Variables

#### Database Connection

```bash
# PostgreSQL connection string
ConnectionStrings__DefaultConnection="Host=<db-host>;Port=5432;Database=<db-name>;Username=<db-user>;Password=<db-password>;SSL Mode=Require"
```

**Example** (Production):

```bash
ConnectionStrings__DefaultConnection="Host=maa-postgres.postgres.database.azure.com;Port=5432;Database=maa_prod;Username=maa_admin;Password=${DB_PASSWORD};SSL Mode=Require"
```

**Environment-Specific Values**:

- **Development**: `Host=127.0.0.1;Port=5432;Database=maa_dev;Username=maa;Password=devpass`
- **Staging**: `Host=maa-postgres-staging.postgres.database.azure.com;...`
- **Production**: `Host=maa-postgres.postgres.database.azure.com;...`

---

#### JWT Authentication

```bash
# JWT signing key (must be strong, 256-bit minimum)
Jwt__SecretKey="<strong-secret-key-from-keyvault>"

# JWT issuer (API identity)
Jwt__Issuer="maa-api"

# JWT audience (client identity)
Jwt__Audience="maa-client"

# Access token expiration (minutes)
Jwt__AccessTokenExpirationMinutes=60

# Refresh token expiration (days)
Jwt__RefreshTokenExpirationDays=7

# Max concurrent sessions per user
Jwt__MaxConcurrentSessions=3

# Refresh threshold (minutes before expiration)
Jwt__RefreshThresholdMinutes=5
```

**Security Notes**:

- `Jwt__SecretKey` MUST be stored in Azure Key Vault or equivalent secrets manager
- Never commit secrets to source control
- Rotate keys periodically (recommended: every 90 days)

---

#### Azure Key Vault (Optional, Recommended for Production)

```bash
# Azure Key Vault URI
Azure__KeyVault__Uri="https://maa-keyvault.vault.azure.net/"
```

**Usage**: If configured, the application will attempt to load secrets from Key Vault. If not configured, it falls back to configuration values (for dev/test environments).

---

#### Logging

```bash
# Log level (default: Information)
Logging__LogLevel__Default="Information"
Logging__LogLevel__MicrosoftAspNetCore="Warning"
Logging__LogLevel__MicrosoftEntityFrameworkCore="Warning"
```

**Environment-Specific Recommendations**:

- **Development**: `Debug` or `Information`
- **Staging**: `Information`
- **Production**: `Warning` (reduce noise, log only important events)

---

#### Session Settings

```bash
# Anonymous session timeout (minutes)
SessionSettings__AnonymousTimeoutMinutes=30

# Admin session timeout (hours)
SessionSettings__AdminTimeoutHours=8
```

**Notes**: Adjust based on user behavior and security requirements.

---

#### Swagger/OpenAPI (Disable in Production)

```bash
# Enable/disable Swagger UI
Swagger__Enabled=false

# API title
Swagger__Title="Medicaid Application Assistant API"

# API version
Swagger__Version="1.0.0"

# API description
Swagger__Description="API for handling Medicaid/CHIP applications"
```

**Recommendation**: Set `Swagger__Enabled=false` in production to prevent exposing API documentation publicly.

---

### Configuration File Templates

#### appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Error"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=maa-postgres.postgres.database.azure.com;Port=5432;Database=maa_prod;Username=maa_admin;Password=${DB_PASSWORD};SSL Mode=Require"
  },
  "Jwt": {
    "Issuer": "maa-api",
    "Audience": "maa-client",
    "SecretKey": "${JWT_SECRET_KEY}",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7,
    "MaxConcurrentSessions": 3,
    "RefreshThresholdMinutes": 5
  },
  "SessionSettings": {
    "AnonymousTimeoutMinutes": 30,
    "AdminTimeoutHours": 8
  },
  "Swagger": {
    "Enabled": false
  },
  "Azure": {
    "KeyVault": {
      "Uri": "https://maa-keyvault.vault.azure.net/"
    }
  }
}
```

**Note**: Replace `${DB_PASSWORD}` and `${JWT_SECRET_KEY}` with actual secrets from Key Vault or environment variables.

---

## Frontend Configuration (React App)

### Required Environment Variables

#### API Base URL

```bash
# Backend API base URL
VITE_API_BASE_URL="https://api.maa.example.com"
```

**Environment-Specific Values**:

- **Development**: `http://localhost:5000` or `http://localhost:5063`
- **Staging**: `https://api-staging.maa.example.com`
- **Production**: `https://api.maa.example.com`

---

#### Application Environment

```bash
# Application environment (development, staging, production)
VITE_APP_ENV="production"
```

**Usage**: Controls feature flags, error reporting verbosity, and debugging tools.

---

### Configuration File Templates

#### .env.production

```bash
# Backend API base URL
VITE_API_BASE_URL=https://api.maa.example.com

# Application environment
VITE_APP_ENV=production

# Enable error tracking (e.g., Sentry)
VITE_ENABLE_ERROR_TRACKING=true

# Sentry DSN (if using Sentry for error tracking)
VITE_SENTRY_DSN=https://your-sentry-dsn@sentry.io/project-id
```

**Build Command**:

```bash
npm run build --mode production
```

**Deployment**: Built assets in `dist/` directory can be served by any static file server (Nginx, Azure Static Web Apps, Cloudflare Pages, etc.).

---

## Database Migrations

### Apply Migrations (Production)

**Prerequisites**:

- PostgreSQL 16+ server running
- EF Core CLI installed: `dotnet tool install --global dotnet-ef`
- Connection string configured in environment or `appsettings.Production.json`

**Command**:

```bash
cd src/MAA.Infrastructure
dotnet ef database update --startup-project ../MAA.API --configuration Release
```

**Idempotency**: Migrations are idempotent. Running the same migration multiple times is safe (EF Core tracks applied migrations in `__EFMigrationsHistory` table).

---

### Seed State Configurations

**Prerequisites**:

- Database migrations applied
- `state-configs.json` file contains all 50 states + DC + territories

**Command** (manual seed via API):

```bash
POST /api/admin/seed-state-configs
Authorization: Bearer <admin-jwt-token>
```

**Or** (automatic seed on application startup):

- Configure `StateConfigurationSeeder` to run on startup in `Program.cs`:

```csharp
using var scope = app.Services.CreateScope();
var seeder = scope.ServiceProvider.GetRequiredService<StateConfigurationSeeder>();
await seeder.SeedAsync();
```

---

## Data Files

### ZIP Code Mapping Data

**File**: `src/MAA.Infrastructure/Data/zip-to-state.csv`

**Format**:

```csv
ZipCode,StateCode,City
90210,CA,Beverly Hills
10001,NY,New York
60601,IL,Chicago
...
```

**Source**: [SimpleMaps U.S. ZIP Codes Database](https://simplemaps.com/data/us-zips) (free tier: ~42,000 entries)

**Deployment**:

- Ensure `zip-to-state.csv` is copied to `{app-directory}/Data/` on deployment
- File is loaded into memory at application startup by `ZipCodeMappingService`
- Update quarterly to reflect USPS changes

---

### State Configuration Data

**File**: `src/MAA.Infrastructure/Data/state-configs.json`

**Format**: See `state-configs.json` for schema.

**Production Requirement**: File must contain configurations for:

- All 50 U.S. states
- District of Columbia (DC)
- U.S. territories (optional): Puerto Rico (PR), Guam (GU), U.S. Virgin Islands (VI), American Samoa (AS), Northern Mariana Islands (MP)

**Deployment**:

- Ensure `state-configs.json` is copied to `{app-directory}/Data/` on deployment
- File is read by `StateConfigurationSeeder` during initial database seeding
- Update annually or when state Medicaid rules change

---

## Verification Checklist

Before deploying to production, verify:

### Backend

- [ ] `ConnectionStrings__DefaultConnection` configured with production database
- [ ] `Jwt__SecretKey` stored in Azure Key Vault (not in appsettings.json)
- [ ] `Swagger__Enabled=false` in production
- [ ] Database migrations applied: `dotnet ef database update`
- [ ] State configurations seeded: verify `state_configurations` table has ≥50 rows
- [ ] ZIP code mapping loaded: verify application logs for "Loaded X ZIP code mappings"
- [ ] API endpoints respond: test `GET /api/health` or similar health check
- [ ] HTTPS enabled with valid SSL certificate

### Frontend

- [ ] `VITE_API_BASE_URL` points to production API
- [ ] Frontend built with `npm run build --mode production`
- [ ] Static assets served over HTTPS
- [ ] CORS configured correctly on backend to allow frontend origin
- [ ] Browser console shows no errors on page load

### Data

- [ ] `state-configs.json` contains all 50 states + DC (minimum)
- [ ] `zip-to-state.csv` is up-to-date (quarterly refresh recommended)
- [ ] Database backups configured (daily snapshots recommended)

### Security

- [ ] Secrets not committed to source control
- [ ] JWT secret key rotated and stored in Key Vault
- [ ] Database credentials use least-privilege principle
- [ ] Rate limiting enabled on API endpoints (if applicable)
- [ ] SQL injection prevention: Entity Framework parameterized queries (default)
- [ ] XSS prevention: React escapes output by default, no `dangerouslySetInnerHTML` in state context components

---

## Rollback Plan

If deployment fails or critical bugs are discovered:

1. **Backend Rollback**:
   - Redeploy previous stable version of MAA.API
   - Roll back database migrations (if necessary):
     ```bash
     dotnet ef database update <previous-migration-name> --startup-project ../MAA.API
     ```

2. **Frontend Rollback**:
   - Redeploy previous stable version of frontend build
   - Update `VITE_API_BASE_URL` to point to stable backend version

3. **Data Rollback**:
   - Restore database from snapshot (if data corruption occurred)
   - Re-seed state configurations if seeding failed

---

## Support & Troubleshooting

### Common Issues

#### "ZIP code not found" errors for valid ZIP codes

- **Cause**: `zip-to-state.csv` missing or incomplete
- **Fix**: Verify file exists in `{app-directory}/Data/` and contains ~42,000 entries

#### "State configuration not found" errors

- **Cause**: State configurations not seeded into database
- **Fix**: Run `StateConfigurationSeeder.SeedAsync()` manually or via admin API endpoint

#### API returns 500 errors for state context endpoints

- **Cause**: Database connection failure or missing migrations
- **Fix**: Verify `ConnectionStrings__DefaultConnection` is correct and migrations applied

#### Frontend shows "Network Error" when calling API

- **Cause**: CORS misconfiguration or incorrect `VITE_API_BASE_URL`
- **Fix**: Verify backend CORS policy allows frontend origin; verify API base URL

---

## Contact

For deployment assistance or configuration issues, contact:

- **DevOps Team**: devops@maa.example.com
- **Backend Lead**: backend-lead@maa.example.com
- **Frontend Lead**: frontend-lead@maa.example.com

---

**Document Version**: 1.0  
**Last Updated**: February 10, 2026  
**Reviewed By**: [Name, Role]
