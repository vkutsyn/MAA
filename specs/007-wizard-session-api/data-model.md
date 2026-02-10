# Data Model: Eligibility Wizard Session API

**Date**: 2026-02-10

## Overview

This feature introduces wizard session state tracking, per-step answers, and step progress status. Step definitions and navigation rules remain configuration-driven and are not stored in the database.

## Entities

### WizardSession

Represents the overall wizard state for a session.

**Fields**:
- `id` (uuid, PK)
- `session_id` (uuid, FK to Session)
- `current_step_id` (text, required)
- `last_activity_at` (timestamptz, required)
- `created_at` (timestamptz, required)
- `updated_at` (timestamptz, nullable)
- `version` (uint, concurrency token)

**Relationships**:
- One `WizardSession` has many `StepAnswer` records.
- One `WizardSession` has many `StepProgress` records.

**Indexes/Constraints**:
- Unique index on `session_id` (one wizard session per auth session).
- Index on `last_activity_at` for cleanup/reporting.

---

### StepAnswer

Represents a saved answer for a single wizard step.

**Fields**:
- `id` (uuid, PK)
- `session_id` (uuid, FK to WizardSession)
- `step_id` (text, required)
- `answer_data` (jsonb, required)
- `schema_version` (text, required)
- `status` (enum: `draft`, `submitted`)
- `submitted_at` (timestamptz, required)
- `updated_at` (timestamptz, nullable)
- `version` (uint, concurrency token)

**Relationships**:
- Many `StepAnswer` records belong to one `WizardSession`.

**Indexes/Constraints**:
- Unique index on (`session_id`, `step_id`) to support upserts.
- Index on `session_id` for session rehydration.
- Optional GIN index on `answer_data` if querying inside JSONB becomes necessary.

---

### StepProgress

Tracks completion status independently from answer storage.

**Fields**:
- `id` (uuid, PK)
- `session_id` (uuid, FK to WizardSession)
- `step_id` (text, required)
- `status` (enum: `not_started`, `in_progress`, `completed`, `requires_revalidation`)
- `last_updated_at` (timestamptz, required)
- `version` (uint, concurrency token)

**Relationships**:
- Many `StepProgress` records belong to one `WizardSession`.

**Indexes/Constraints**:
- Unique index on (`session_id`, `step_id`).
- Index on (`session_id`, `status`) to identify incomplete steps.

---

### StepDefinition (Configuration)

Represents the schema and metadata for a wizard step (defined in code or JSON config).

**Fields**:
- `step_id` (text, required)
- `title` (text, required)
- `description` (text, optional)
- `fields` (array of field definitions)
- `validation_rules` (array of validation rules)
- `visibility_rule` (JsonLogic expression)
- `next_step_rules` (ordered list of conditional navigation rules)

**Storage**: In-memory definitions loaded at startup, not persisted in the database.

---

### StepNavigationRule (Configuration)

Defines conditional navigation between steps.

**Fields**:
- `from_step_id` (text, required)
- `condition` (JsonLogic expression)
- `to_step_id` (text, required)

**Storage**: Included within step definitions or a centralized navigation map.

## Validation Rules

- `session_id` must exist and be active; expired sessions reject updates (401/403).
- `step_id` must match a known step definition.
- `answer_data` must conform to the step schema and `schema_version`.
- `status` transitions must be valid (see below).

## State Transitions

### StepProgress status

- `not_started` -> `in_progress`
- `in_progress` -> `completed`
- `completed` -> `requires_revalidation` (when upstream answers change)
- `requires_revalidation` -> `in_progress` (user revisits and edits)
- `in_progress` -> `completed` (after revalidation)

### StepAnswer status

- `draft` -> `submitted`
- `submitted` -> `submitted` (overwrite on resubmission)

## Notes

- `WizardSession.current_step_id` is updated after successful answer submission and next-step calculation.
- Downstream invalidation updates affected `StepProgress` rows to `requires_revalidation`.
- Concurrency tokens are exposed in API responses and required for updates.
