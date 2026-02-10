# MedicaidApplicationAssistant Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-02-09

## Active Technologies
- C# 13 / .NET 10 + ASP.NET Core 10, EF Core 10, Npgsql.EntityFrameworkCore.PostgreSQL, JSONLogic.Net, Azure.Identity, Azure.Security.KeyVault.Secrets (002-rules-engine)
- PostgreSQL 16+ (JSONB for rule logic, indexed queries for state/program) (002-rules-engine)
- C# 13 / .NET 9 (ASP.NET Core) + Swashbuckle.AspNetCore v6.x (OpenAPI/Swagger NuGet package) (003-add-swagger)
- PostgreSQL (existing; not affected by Swagger) (003-add-swagger)

- C# 13 / .NET 10 + ASP.NET Core 10, EF Core 10, Npgsql.EntityFrameworkCore.PostgreSQL, Azure.Identity, Azure.Security.KeyVault.Secrets, IdentityModel (001-auth-sessions)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# 13 / .NET 10

## Code Style

C# 13 / .NET 10: Follow standard conventions

## Recent Changes
- 003-add-swagger: Added C# 13 / .NET 9 (ASP.NET Core) + Swashbuckle.AspNetCore v6.x (OpenAPI/Swagger NuGet package)
- 002-rules-engine: Added C# 13 / .NET 10 + ASP.NET Core 10, EF Core 10, Npgsql.EntityFrameworkCore.PostgreSQL, JSONLogic.Net, Azure.Identity, Azure.Security.KeyVault.Secrets

- 001-auth-sessions: Added C# 13 / .NET 10 + ASP.NET Core 10, EF Core 10, Npgsql.EntityFrameworkCore.PostgreSQL, Azure.Identity, Azure.Security.KeyVault.Secrets, IdentityModel

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
