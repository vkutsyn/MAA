# MedicaidApplicationAssistant Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-02-09

## Active Technologies
- Backend: C# 13 (.NET 10), Frontend: TypeScript 5.7+ (React 19) (006-state-context-init)
- PostgreSQL 16+ (session persistence, state configuration storage) (006-state-context-init)
- C# 13 on .NET 10 (backend API) + ASP.NET Core Web API, EF Core 10 (Npgsql), MediatR, FluentValidation (007-wizard-session-api)
- PostgreSQL 16+ with JSONB answer storage (007-wizard-session-api)
- C# 13 (backend), TypeScript 5.x with React 18.x (frontend) (008-question-definitions-api)
- PostgreSQL 15.x (existing `SessionContext` extended with new tables) (008-question-definitions-api)

- C# 13 / .NET 10 + ASP.NET Core 10, EF Core 10, Npgsql.EntityFrameworkCore.PostgreSQL, JSONLogic.Net, Azure.Identity, Azure.Security.KeyVault.Secrets (002-rules-engine)
- PostgreSQL 16+ (JSONB for rule logic, indexed queries for state/program) (002-rules-engine)
- C# 13 / .NET 9 (ASP.NET Core) + Swashbuckle.AspNetCore v6.x (OpenAPI/Swagger NuGet package) (003-add-swagger)
- PostgreSQL (existing; not affected by Swagger) (003-add-swagger)
- TypeScript 5.7 (React 19), C# 13 (.NET 10 API integration) + React 19, Vite 6, shadcn/ui + Tailwind CSS, React Hook Form + Zod, TanStack Query, Zustand (004-ui-implementation)
- PostgreSQL via API (sessions/answers); session cookie (`MAA_SessionId`) for client persistence (004-ui-implementation)
- TypeScript 5.7+, React 19+ + React Router, Axios, React Hook Form, Zod, shadcn/ui, Tailwind CSS (005-frontend-auth-flow)
- N/A (frontend-only; no new persistent data stores) (005-frontend-auth-flow)

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
- 008-question-definitions-api: Added C# 13 (backend), TypeScript 5.x with React 18.x (frontend)
- 007-wizard-session-api: Added C# 13 on .NET 10 (backend API) + ASP.NET Core Web API, EF Core 10 (Npgsql), MediatR, FluentValidation
- 006-state-context-init: Added Backend: C# 13 (.NET 10), Frontend: TypeScript 5.7+ (React 19)


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
