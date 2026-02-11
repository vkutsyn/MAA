# Research: Eligibility Evaluation Engine

**Date**: 2026-02-11
**Feature**: 010-eligibility-evaluation-engine

## Decision 1: Declarative Rule Format And Storage

**Decision**: Use JSONLogic as the declarative rule format stored in PostgreSQL JSONB, with immutable rule versions and effective date ranges.

**Rationale**: JSONLogic.Net is an approved dependency and supports deterministic evaluation. JSONB storage aligns with existing architecture and enables versioned rule updates without code changes.

**Alternatives considered**: Custom C# expression evaluator; hybrid rule formats; full rules engine frameworks.

## Decision 2: Rule Version Selection Strategy

**Decision**: Select the most recent rule version with `effective_date` on or before the request's effective date, and record `rule_version_used` in the evaluation response.

**Rationale**: This ensures determinism, supports future-dated rules, and provides auditability without persisting evaluation results.

**Alternatives considered**: Version-only selection without effective dates; manual version overrides per request.

## Decision 3: Confidence Scoring Model

**Decision**: Compute confidence as $\mathrm{round}(100 \times C \times R)$ where $C$ is weighted data completeness and $R$ is rule certainty. Map status labels to score bands (Likely >= 85, Possibly 60-84, Unlikely < 60).

**Rationale**: The model is deterministic, explainable, and efficient. It reflects missing data and rule certainty without using probabilistic or ML approaches.

**Alternatives considered**: Bayesian/ML scoring; fixed buckets without a formula; Monte Carlo scoring.

## Decision 4: Explanation Generation Approach

**Decision**: Use a template-driven explanation builder with a controlled glossary and readability checks.

**Rationale**: Template-based explanations are deterministic and testable while meeting plain-language requirements. A glossary prevents unexplained jargon and enables consistent phrasing.

**Alternatives considered**: Generative AI explanations; rule-to-text without templates; AI post-editing of templates.

## Decision 5: Caching Strategy

**Decision**: Cache rule sets and FPL tables in-memory with optional Redis for shared cache, and invalidate on rule updates.

**Rationale**: This reduces evaluation latency and supports the p95 <= 2s SLO without adding persistence to evaluation requests.

**Alternatives considered**: No caching; database-only lookups for every evaluation.
