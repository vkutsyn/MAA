# Research & Decisions: Eligibility Question Definitions API

**Phase 0 Output** | **Date**: 2026-02-10

## Research Summary

The specification for the Eligibility Question Definitions API was comprehensive with no NEEDS CLARIFICATION markers. Research focused on confirming architectural fit within the existing MAA project structure and identifying best practices for conditional rule evaluation.

---

## Decisions & Rationale

### 1. Conditional Rule Evaluation Architecture
**Topic**: How to implement flexible conditional visibility rules  
**Decision**: Pure function-based evaluator in Domain layer (no database dependency)  
**Rationale**:
- Aligns with Constitution I: Domain logic testable in isolation
- Rules can be evaluated 100+ times per user session (front-end can do it without backend round-trips)
- Portable: Same rule evaluator can run in backend or frontend JavaScript
- Unit testable: No mocking required; pure function testing

**Alternatives Considered**:
- Scripting engine (Lua, embedded JavaScript): Adds complexity, security risk
- Database-driven evaluation: Performance penalty; rules centralized but inflexible
- Hard-coded if-else: Doesn't scale; requires code change per rule update

**Implementation Detail**: Boolean expression parser supporting:
- Variables: `{questionId} == 'value'` or `{questionId} > 3`
- Operators: `==`, `!=`, `>`, `<`, `>=`, `<=`, `IN`, `NOT IN`
- Logic: `AND`, `OR`, `NOT` with parentheses for grouping

---

### 2. Question Data Caching Strategy
**Topic**: Reducing database load for frequently-accessed question definitions  
**Decision**: Redis cache (24h TTL) keyed by `state:{code}:program:{code}`  
**Rationale**:
- Question definitions are immutable during user session
- Eliminates repeated DB queries for same state/program
- Simple cache key structure (no complex serialization)
- 24h TTL balances freshness vs performance (definitions change rarely)

**Alternatives Considered**:
- Application memory cache: Doesn't survive app restart; stale across load-balanced services
- Database query caching (EF Core 2nd level cache): Limited control; opaque invalidation
- No caching: Performance risk for high-volume states/programs

**Implementation Detail**: 
- Cache invalidated on (admin) question definition upload
- Fall-back: If cache misses, query database (atomic cache-aside pattern)

---

### 3. Circular Dependency Detection
**Topic**: Preventing impossible conditional logic (Q1 visible if Q2, Q2 visible if Q1)  
**Decision**: Topological sort at question registration; raise validation error if cycle detected  
**Rationale**:
- Prevents runtime confusion in frontend
- Catches invalid data early (admin workflow, not user session)
- Simple algorithmic approach (O(n log n) for 500 questions)

**Alternatives Considered**:
- Runtime detection: Too late; already rendered problematic UI
- Ignore: Silent failures; leads to stuck questions
- Restrict rules to directed acyclic graph only: Same outcome, clearer name

**Implementation Detail**: 
- Build dependency graph from ConditionalRules on app startup
- Use Kahn's algorithm to detect cycles
- Fail fast: If cycle detected, log and raise exception

---

### 4. State & Program Code Validation
**Topic**: Ensuring only valid state/program combinations are queried  
**Decision**: Master reference list (loaded on app startup); validate incoming requests  
**Rationale**:
- US state codes: 50 states + DC (immutable; ~52 options)
- Program codes: Maintained by MAA domain experts; updated infrequently
- Validation prevents querying non-existent combinations
- Immediate 400 Bad Request response improves API robustness

**Alternatives Considered**:
- External validation service: Latency penalty; dependency risk
- Skip validation: Allows orphaned questions; confusing error messages
- Database lookup: Unnecessary; reference data small enough for memory

**Implementation Detail**:
- Load state codes from configuration: `["CA", "TX", "NY", ...]`
- Load program codes from StateConfiguration entity (existing in project)
- Validate in controller before hitting handler

---

### 5. Question Metadata Completeness
**Topic**: What metadata fields to include in API response  
**Decision**: Include all fields needed for frontend rendering + validation  
**Rationale**:
- Frontend should render questions with zero additional requests
- Validation rules (regex, min/max) prevent duplicating logic
- Help text, field type, required flag: All needed for complete UX

**Alternatives Considered**:
- Minimal response (ID, text only): Requires additional backend calls for metadata
- Extensive response (internal fields, system flags): Bloats response; exposes implementation

**Implementation Detail**: 
- QuestionDto includes: ID, text, fieldType, validation, options, helpText, conditionalRuleId
- ConditionalRuleDto includes: ID, ruleExpression, human-readable description
- QuestionOptionDto includes: label, value, displayOrder

---

## Dependencies Verified

✅ **ASP.NET Core 8.0**: Already in use (MAA.API)  
✅ **Entity Framework Core 8**: Configured (SessionContext established)  
✅ **Swashbuckle**: OpenAPI generation already working  
✅ **React Query**: Frontend already uses for server state  
✅ **TypeScript 5.x**: Frontend type-safe  
✅ **Redis**: Optional for caching, available in Azure deployment  

---

## Open Questions Resolved

| Question | Resolution |
|----------|-----------|
| How to represent conditional rules? | Boolean expressions as strings (versioned separately from questions) |
| Who owns program code reference data? | Existing StateConfiguration entity; Question references program by code |
| Should rules be pre-evaluated by backend? | No; send rule definitions; frontend evaluates (cheaper, portable) |
| Version compatibility: in-progress sessions with updated questions? | Question definitions immutable during session (version timestamp in SessionData) |
| Cache invalidation timing? | On admin question upload; cache invalidated per updated state/program |

---

## Recommendations

1. **Add monitoring** for question retrieval latency (track against 200ms SLO)
2. **Implement cache warming** on app startup for top 10 state/program combinations
3. **Document rule expression syntax** for admin portal (who defines questions)
4. **Plan feedback loop**: Capture user confusion on conditional visibility (analytics)
5. **Consider i18n**: Question text should support multiple languages (future enhancement)

---

## Status

**Phase 0 Research**: ✅ COMPLETE  
**Unknowns Resolved**: 0 (spec was complete)  
**Design Decisions Documented**: 5 major  
**Ready for Phase 1 Design**: YES

**Next**: Proceed to creation of data-model.md, contracts/, and quickstart.md
