# Research: Eligibility Wizard Session API

**Date**: 2026-02-10

## Decision 1: Store step answers in JSONB with relational keys

**Decision**: Persist answers in a `StepAnswers` table with relational columns (`session_id`, `step_id`, `status`, timestamps) and a JSONB `answer_data` column. Enforce a unique constraint on (`session_id`, `step_id`) and add B-tree indexes on `session_id`, `step_id`, and `updated_at`.

**Rationale**: This structure keeps fast session rehydration and step lookups while allowing flexible schemas per step without frequent migrations. JSONB is well supported in PostgreSQL and aligns with the spec requirement for flexible answer storage.

**Alternatives considered**:
- Per-step relational tables (rigid, higher migration cost).
- EAV schema (complex queries, weaker validation semantics).
- Single JSON blob per session (harder to update per-step and to support concurrency).

## Decision 2: Optimistic concurrency using Postgres row version tokens

**Decision**: Use EF Core optimistic concurrency with PostgreSQL row version tokens (mapping `xmin` as a `uint` concurrency token) for both `WizardSession` and `StepAnswer` updates. Clients include the last seen version; on conflict, return HTTP 409 with current state and version.

**Rationale**: Row version tokens provide low-overhead concurrency control without long-lived locks, aligning with multi-tab and long-lived wizard sessions. EF Core raises `DbUpdateConcurrencyException`, which maps cleanly to 409 responses.

**Alternatives considered**:
- Pessimistic locks (risk of long-held locks in user-driven flows).
- Application-managed version GUIDs (more code and failure modes).
- Distributed locks (unnecessary complexity for this scope).

## Decision 3: Step definitions and navigation logic in pure domain services

**Decision**: Store step definitions in code or JSON config files and load them via an `IStepDefinitionProvider`. Evaluate navigation in a pure `StepNavigationEngine` service in the domain/application layer, with no database dependencies.

**Rationale**: Keeps navigation logic deterministic and fully unit testable, complying with Clean Architecture and Constitution I. Also reduces latency by keeping definitions in memory.

**Alternatives considered**:
- Database-driven step definitions (more flexibility but adds runtime I/O and coupling).
- External CMS for step configuration (out of scope and operationally heavier).

## Decision 4: Conditional logic evaluation via JsonLogic

**Decision**: Represent conditional step logic as JsonLogic expressions evaluated against the current answer snapshot.

**Rationale**: JsonLogic is already part of the approved stack and provides a structured, testable way to express conditional navigation without custom DSL complexity.

**Alternatives considered**:
- Custom rule DSL (more development and maintenance).
- C# expression trees (less portable and harder to serialize).

## Decision 5: Validation via step-specific validators with schema versioning

**Decision**: Implement `IStepAnswerValidator` per step and persist `schema_version` alongside each answer. Validate with FluentValidation using the step definition schema and reject unknown schema versions.

**Rationale**: Per-step validators enforce strong typing while still allowing flexible JSONB storage. Schema versioning enables safe changes to step definitions over time.

**Alternatives considered**:
- Pure JSON Schema validation for all steps (harder to integrate with existing domain rules).
- No versioning (risk of invalidating past answers when definitions change).
